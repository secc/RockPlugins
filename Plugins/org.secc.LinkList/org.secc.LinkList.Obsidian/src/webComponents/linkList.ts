import type { LinkItemBag, LinkListBag } from "../types";
import {
    backgroundImageUrl,
    headerBackgroundImageUrl,
    headerImageUrl,
    safeColor,
    safeTarget,
    safeText,
    safeUrl,
    vimeoEmbedUrl,
    youTubeEmbedUrl
} from "../linkListParity";
import { groupIntoSections, hoistFeatured, isFeaturedLink, type SectionBlock } from "../linkListItems";

// <secc-link-list> — the single, self-contained renderer for a Link List, used
// BOTH on external embeds (Webflow, self-fetches by slug) and in-Rock (the Viewer
// block hands it a pre-fetched bag via the `listData` property + the `manual`
// attribute). The whole design (global header -> hero -> featured -> intro ->
// accordion sections -> footer -> global footer) ships inside the shadow root, so
// it renders identically wherever it's hosted and nothing else needs to be on the
// page. Styling is driven only by what's shipped here (no host CSS dependency);
// per-list / preset effective* colors override the design defaults via CSS vars.

// Canonical identifier form is lowercase (matches the server's slug
// normalization); resolveIdentifier() lowercases before this test, which
// also covers numeric ids and GUIDs (hex + dashes).
const IDENTIFIER_PATTERN = /^[a-z0-9-]+$/;
const OBSERVED_ATTRIBUTES = [ "item-id", "slug-from-path", "slug-path-index", "base-url", "manual" ];

// Adobe Fonts kit (IvyJournal) — loaded only when the org enables it (licensed
// domains). Inter + Cormorant (the serif fallback) always load from Google.
const INTER_IMPORT = "@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');";
const CORMORANT_IMPORT = "@import url('https://fonts.googleapis.com/css2?family=Cormorant+Garamond:wght@300;400;500;600&display=swap');";
const IVYJOURNAL_IMPORT = "@import url('https://use.typekit.net/cjf8ejp.css');";

const CHEVRON_SVG = `<svg viewBox="0 0 24 24" aria-hidden="true"><polyline points="6 9 12 15 18 9"></polyline></svg>`;

class SeccLinkListElement extends HTMLElement {
    private root: ShadowRoot;
    private abortController: AbortController | null = null;
    private loadToken = 0;
    private baseUrl = "";
    private injectedData: LinkListBag | null = null;

    public static get observedAttributes(): string[] {
        return OBSERVED_ATTRIBUTES;
    }

    constructor() {
        super();
        this.root = this.attachShadow( { mode: "open" } );
    }

    /** In-Rock path: the Viewer block hands the (auth-resolved) bag directly. */
    public set listData( value: LinkListBag | null ) {
        this.injectedData = value;
        if ( this.isConnected ) {
            if ( value ) {
                this.renderList( value );
            }
            else {
                this.renderError( "Link list not found." );
            }
        }
    }

    public get listData(): LinkListBag | null {
        return this.injectedData;
    }

    private get isManual(): boolean {
        return ( this.getAttribute( "manual" ) || "" ).toLowerCase() === "true" || this.hasAttribute( "manual" );
    }

    public connectedCallback(): void {
        if ( this.isManual ) {
            // Host will inject data via `listData`; render it if already set,
            // otherwise show loading and wait (do NOT self-fetch).
            if ( this.injectedData ) {
                this.renderList( this.injectedData );
            }
            else {
                this.renderLoading();
            }
            return;
        }
        this.renderLoading();
        void this.load();
    }

    public disconnectedCallback(): void {
        this.abortController?.abort();
        this.abortController = null;
    }

    public attributeChangedCallback( _name: string, oldValue: string | null, newValue: string | null ): void {
        if ( oldValue === newValue || !this.isConnected || this.isManual ) {
            return;
        }
        this.renderLoading();
        void this.load();
    }

    private async load(): Promise<void> {
        this.abortController?.abort();
        this.abortController = new AbortController();
        const token = ++this.loadToken;

        const baseUrl = this.getAttribute( "base-url" ) || window.location.origin;
        this.baseUrl = baseUrl;
        const identifier = this.resolveIdentifier();

        if ( !identifier ) {
            this.renderError( "No identifier provided. Set item-id or slug-from-path=true." );
            return;
        }
        if ( identifier.length > 200 ) {
            this.renderError( "Identifier too long." );
            return;
        }
        if ( !IDENTIFIER_PATTERN.test( identifier ) ) {
            this.renderError( "Invalid identifier." );
            return;
        }

        try {
            const url = `${baseUrl.replace( /\/$/, "" )}/api/secc/linklist/${encodeURIComponent( identifier )}`;
            const response = await fetch( url, { signal: this.abortController.signal } );
            if ( token !== this.loadToken ) {
                return;
            }
            if ( !response.ok ) {
                this.renderError( response.status === 404 ? "Link list not found." : `Unable to load list (${response.status}).` );
                return;
            }
            const list = await response.json() as LinkListBag;
            if ( token !== this.loadToken ) {
                return;
            }
            this.renderList( list );
        }
        catch ( err ) {
            if ( ( err as DOMException )?.name === "AbortError" ) {
                return;
            }
            this.renderError( "Unable to load link list." );
        }
    }

    private resolveIdentifier(): string | null {
        // Lowercase = the canonical form the server stores/resolves; a
        // mixed-case slug in a shared URL still loads.
        const explicit = this.getAttribute( "item-id" );
        if ( explicit ) {
            return explicit.trim().toLowerCase();
        }
        const fromPath = ( this.getAttribute( "slug-from-path" ) || "" ).toLowerCase() === "true";
        if ( !fromPath ) {
            return null;
        }
        const parts = window.location.pathname.split( "/" ).filter( p => !!p );
        if ( parts.length === 0 ) {
            return null;
        }
        const configuredIndex = Number.parseInt( this.getAttribute( "slug-path-index" ) || "0", 10 );
        if ( configuredIndex > 0 && configuredIndex <= parts.length ) {
            return parts[configuredIndex - 1].toLowerCase();
        }
        return parts[parts.length - 1].toLowerCase();
    }

    // -----------------------------------------------------------------------
    // Render
    // -----------------------------------------------------------------------

    private renderList( list: LinkListBag ): void {
        const sorted = ( list.items || [] ).slice().sort( ( a, b ) => ( a.order || 0 ) - ( b.order || 0 ) );
        // Featured is hoisted to the front, then the flat list becomes ordered
        // blocks: top-level items (incl. the featured pill) interleaved with
        // section accordions (WS7 fix 5). renderTopLink adds the featured styling.
        const blocks = groupIntoSections( hoistFeatured( sorted ) );

        const bg = safeColor( list.effectiveBackgroundColor );
        const fg = safeColor( list.effectiveContentTextColor );
        const btn = safeColor( list.effectiveButtonColor );
        const btnText = safeColor( list.effectiveButtonTextColor );
        const featBtn = safeColor( list.effectiveFeaturedButtonColor );
        const featBtnText = safeColor( list.effectiveFeaturedButtonTextColor );
        const title = safeColor( list.effectiveTitleColor );

        const bgImage = backgroundImageUrl( this.baseUrl, list.backgroundImage );
        const headerBgImage = headerBackgroundImageUrl( this.baseUrl, list.headerBackgroundImage );
        const headerImg = headerImageUrl( this.baseUrl, list.headerImage );
        const videoEmbed = youTubeEmbedUrl( list.headerVideo ) || vimeoEmbedUrl( list.headerVideoVimeoId );

        const hostVars = [
            // A full background image suppresses the background color (legacy).
            bg && !bgImage ? `--ll-bg:${bg};` : "",
            fg ? `--ll-fg:${fg};` : "",
            btn ? `--ll-btn-bg:${btn};` : "",
            btnText ? `--ll-btn-fg:${btnText};` : "",
            featBtn ? `--ll-feat-bg:${featBtn};` : "",
            featBtnText ? `--ll-feat-fg:${featBtnText};` : "",
            title ? `--ll-title:${title};` : ""
        ].join( "" );

        const serifStack = list.useIvyJournalFont
            ? "'ivyjournal', 'Cormorant Garamond', Georgia, serif"
            : "'Cormorant Garamond', Georgia, serif";
        const fontImports = [
            INTER_IMPORT,
            CORMORANT_IMPORT,
            list.useIvyJournalFont ? IVYJOURNAL_IMPORT : ""
        ].join( "\n" );

        // --- Hero band ---
        const heroBgMarkup = headerBgImage
            ? `<div class="hero-bg" style="background-image:url('${safeText( headerBgImage )}');"></div>`
            : "";
        const titleText = safeText( list.customTitle || list.title || list.slug || "Link List" );
        const heroLogo = headerImg
            ? `<img class="hero-logo${list.roundHeaderImage ? " round" : ""}" part="header-image" src="${safeText( headerImg )}" alt="${titleText}" />`
            : "";
        const heroTitle = list.hideTitle ? "" : `<div class="hero-title">${titleText}</div>`;
        const heroVideo = videoEmbed
            ? `<div class="hero-video" part="header-video"><iframe src="${safeText( videoEmbed )}" frameborder="0" allow="autoplay; fullscreen; picture-in-picture" allowfullscreen></iframe></div>`
            : "";
        // Header media: image and video are mutually exclusive — video wins. Media
        // sits above the title.
        const heroMedia = videoEmbed ? heroVideo : heroLogo;
        // WS7 fix 9: the intro lives INSIDE the header band (after the title) so the
        // header background spans it too; its default color is the title color.
        const introMarkup = list.introContent ? `<div class="intro" part="intro">${list.introContent}</div>` : "";
        const heroMarkup = ( heroBgMarkup || heroMedia || heroTitle || introMarkup )
            ? `<header class="hero${headerBgImage ? " has-band" : ""}" part="hero">${heroBgMarkup}<div class="hero-content">${heroMedia}${heroTitle}${introMarkup}</div></header>`
            : "";

        // --- Content: ordered blocks (top-level pills + section accordions), with
        //     the hoisted featured link as the first top-level block. ---
        const blocksMarkup = blocks
            .map( ( b, i ) => b.kind === "section" ? this.renderAccordion( b, i ) : this.renderTopItem( b.item ) )
            .join( "" );
        const footerMarkup = list.footerContent ? `<div class="ll-footer" part="footer">${list.footerContent}</div>` : "";

        const globalHeaderMarkup = list.globalHeaderContent
            ? `<div class="global-header" part="global-header">${list.globalHeaderContent}</div>`
            : "";
        const globalFooterMarkup = list.globalFooterContent
            ? `<div class="global-footer" part="global-footer">${list.globalFooterContent}</div>`
            : "";

        const rootStyle = bgImage
            ? ` style="background-image:url('${safeText( bgImage )}');background-size:cover;background-position:center;"`
            : "";

        this.root.innerHTML = `
<style>
${fontImports}
:host { display: block; width: 100vw; max-width: 100vw; margin-left: calc(50% - 50vw); margin-right: calc(50% - 50vw); --ll-serif: ${serifStack}; ${hostVars} }
${this.styles()}
</style>
<div class="ll-root" part="wrapper"${rootStyle}>
    ${globalHeaderMarkup}
    ${heroMarkup}
    <main class="content" part="content">
        ${blocksMarkup}
        ${footerMarkup}
    </main>
    ${globalFooterMarkup}
</div>`;

        this.wireAccordions();

        this.dispatchEvent( new CustomEvent( "secc-link-list-loaded", {
            detail: { slug: list.slug, title: list.title },
            bubbles: true,
            composed: true
        } ) );
    }

    /** A top-level row: separator -> rule; featured/normal link -> pill. */
    private renderTopItem( item: LinkItemBag ): string {
        if ( ( item.itemType ?? "link" ) === "separator" ) {
            return `<div class="rule" part="separator"></div>`;
        }
        return this.renderTopLink( item );
    }

    private renderTopLink( item: LinkItemBag ): string {
        const featuredClass = isFeaturedLink( item ) ? " featured" : "";
        const label = safeText( item.text || item.url || "Link" );
        const sub = item.subtitle ? `<div class="btn-sub" part="link-subtitle">${safeText( item.subtitle )}</div>` : "";
        const url = safeUrl( item.url );
        const target = safeTarget( item.target );
        const rel = target === "_blank" ? ` rel="noopener noreferrer"` : "";
        return `<a class="link-btn${featuredClass}" part="link" href="${safeText( url )}" target="${target}"${rel}>`
            + `<div class="btn-body"><div class="btn-label">${label}</div>${sub}</div>`
            + `<span class="btn-arrow" aria-hidden="true">&rsaquo;</span></a>`;
    }

    /** A section group -> a collapsible accordion. */
    private renderAccordion( group: SectionBlock, index: number ): string {
        const open = !group.section.isSectionCollapsed;
        const bodyId = `ll-acc-${index}`;
        const heading = safeText( group.section.text || "" );
        const desc = group.section.description
            ? `<p class="accord-desc" part="section-description">${safeText( group.section.description )}</p>`
            : "";
        const links = group.children.map( child => this.renderAccordChild( child ) ).join( "" );
        return `<div class="accordion${open ? " open" : ""}" part="section">`
            + `<button class="accord-trigger" type="button" aria-expanded="${open}" aria-controls="${bodyId}">`
            + `<span class="accord-heading">${heading}</span>`
            + `<span class="accord-chevron">${CHEVRON_SVG}</span></button>`
            + `<div class="accord-body" id="${bodyId}"><div class="accord-divider"></div>`
            + `<div class="accord-inner">${desc}<div class="accord-links">${links}</div></div></div></div>`;
    }

    private renderAccordChild( item: LinkItemBag ): string {
        if ( ( item.itemType ?? "link" ) === "separator" ) {
            return `<div class="rule" part="separator"></div>`;
        }
        const label = safeText( item.text || item.url || "Link" );
        const sub = item.subtitle ? `<span class="accord-link-sub" part="link-subtitle">${safeText( item.subtitle )}</span>` : "";
        const url = safeUrl( item.url );
        const target = safeTarget( item.target );
        const rel = target === "_blank" ? ` rel="noopener noreferrer"` : "";
        return `<a class="accord-link" part="link" href="${safeText( url )}" target="${target}"${rel}>`
            + `<span class="accord-link-body"><span class="accord-link-label">${label}</span>${sub}</span>`
            + `<span class="accord-link-arrow" aria-hidden="true">&rsaquo;</span></a>`;
    }

    /** Independent toggle per accordion (not single-open). */
    private wireAccordions(): void {
        const triggers = this.root.querySelectorAll<HTMLButtonElement>( ".accord-trigger" );
        triggers.forEach( trigger => {
            trigger.addEventListener( "click", () => {
                const accordion = trigger.closest( ".accordion" );
                if ( !accordion ) {
                    return;
                }
                const open = accordion.classList.toggle( "open" );
                trigger.setAttribute( "aria-expanded", String( open ) );
            } );
        } );
    }

    private renderLoading(): void {
        this.root.innerHTML = `
<style>:host{display:block;font-family:'Inter',system-ui,sans-serif;}.loading{padding:24px;text-align:center;opacity:.55;font-size:.8rem;letter-spacing:.05em;}</style>
<slot name="loading"><div class="loading" part="loading">Loading…</div></slot>`;
    }

    private renderError( message: string ): void {
        this.dispatchEvent( new CustomEvent( "secc-link-list-error", {
            detail: { message },
            bubbles: true,
            composed: true
        } ) );
        this.root.innerHTML = `
<style>:host{display:block;font-family:'Inter',system-ui,sans-serif;}.error{margin:16px;border:1px solid #d9534f;border-radius:10px;padding:14px;color:#a94442;background:#f7e9e9;font-size:.85rem;}</style>
<slot name="error"><div class="error" part="error" data-message="${safeText( message )}">${safeText( message )}</div></slot>`;
    }

    /** The shipped design (lifted from se-church-links_7.html), with theming
     *  effective* colors as overridable CSS-var fallbacks. */
    private styles(): string {
        return `
*,*::before,*::after { box-sizing: border-box; margin: 0; padding: 0; }
.ll-root {
    --white:#FFFFFF; --page-bg:#F5F4F2; --black:#0F0F0F; --charcoal:#1C1C1C;
    --dark-gray:#3A3A3A; --mid-gray:#888888; --light-gray:#D4D2CE; --hairline:#E4E2DE;
    --radius-btn:100px; --radius-accord:10px;
    --shadow-card:0 1px 3px rgba(0,0,0,.07),0 1px 2px rgba(0,0,0,.04);
    --shadow-hover:0 6px 20px rgba(0,0,0,.11),0 2px 6px rgba(0,0,0,.05);
    font-family:'Inter',system-ui,-apple-system,sans-serif;
    font-size: 17px;
    background: var(--ll-bg, var(--page-bg));
    background-size: cover; background-position: center;
    color: var(--ll-fg, var(--black));
    min-height: 100vh; width: 100%;
    display: flex; flex-direction: column; align-items: center;
    padding-bottom: 56px;
}
/* Global header/footer wrappers + the admin-authored chrome classes (top-nav,
   nav-logo, foot, socials, s-btn) so the WS12 HTML renders per the mockup.
   Social icons are <img>/font-icons (inline <svg> is stripped by the sanitizer). */
.global-header { width:100%; font-size:.95rem; }
.top-nav { width:100%; background:var(--white); border-bottom:1px solid var(--hairline); height:60px; display:flex; align-items:center; justify-content:center; position:sticky; top:0; z-index:100; box-shadow:0 1px 0 var(--hairline); }
.nav-logo-link { display:flex; align-items:center; text-decoration:none; }
.nav-logo-img, .nav-logo { height:30px; width:auto; display:block; }
.global-footer { width:100%; margin-top:28px; display:flex; flex-direction:column; align-items:center; gap:12px; font-size:.95rem; }
.foot { display:flex; flex-direction:column; align-items:center; gap:12px; }
.foot-logo-img, .foot-logo { height:28px; width:auto; display:block; opacity:.45; transition:opacity .18s; }
.foot-logo-img:hover, .foot-logo:hover { opacity:.8; }
.foot-copy { font-size:.78rem; color:var(--mid-gray); letter-spacing:.05em; text-align:center; }
.socials { display:flex; justify-content:center; gap:10px; }
.s-btn { width:40px; height:40px; border-radius:50%; background:var(--white); border:1px solid var(--hairline); box-shadow:var(--shadow-card); display:flex; align-items:center; justify-content:center; text-decoration:none; color:var(--dark-gray); transition:background .18s,border-color .18s,color .18s,transform .14s; }
.s-btn:hover { background:var(--black); border-color:var(--black); color:var(--white); transform:translateY(-2px); }
.s-btn img, .s-btn svg, .s-btn i { width:16px; height:16px; font-size:16px; }
.hero {
    width:100%; background:transparent; display:flex; flex-direction:column;
    align-items:center; text-align:center; padding:52px 24px 24px; gap:12px;
    position:relative; overflow:hidden;
}
/* Dark band treatment ONLY when a header background image is set. */
.hero.has-band { background:var(--charcoal); }
.hero-bg {
    position:absolute; inset:0; background-size:cover; background-position:center 62%;
    filter:grayscale(100%) contrast(1.1) brightness(.45); z-index:0;
}
.hero-bg::after {
    content:""; position:absolute; inset:0;
    background:linear-gradient(160deg, rgba(20,18,14,.5) 0%, rgba(28,28,20,.35) 100%);
}
.hero-content { position:relative; z-index:1; display:flex; flex-direction:column; align-items:center; gap:12px; width:100%; max-width:480px; }
.hero-logo { width:min(55%,280px); height:auto; display:block; margin:0 auto; }
.hero-logo.round { width:min(55%,280px); aspect-ratio:1/1; height:auto; border-radius:50%; object-fit:cover; }
.hero-title { font-family:var(--ll-serif); font-weight:100; font-style:normal; font-size:clamp(2rem,6vw,3rem); color:var(--ll-title, var(--ll-fg, var(--black))); line-height:1.08; letter-spacing:.02em; }
.hero-video { position:relative; width:100%; padding-bottom:56.25%; }
.hero-video iframe { position:absolute; inset:0; width:100%; height:100%; border:0; }
.content { width:100%; max-width:480px; padding:16px 20px 0; display:flex; flex-direction:column; align-items:center; gap:14px; }
/* Intro sits balanced between the title and the first button (Fix 6). */
.intro { width:100%; font-size:1.15rem; line-height:1.65; color:var(--ll-title, var(--ll-fg, var(--black))); margin:0; }
.link-btn {
    width:100%; display:flex; align-items:center; justify-content:center; gap:8px;
    padding:17px 28px; background:var(--ll-btn-bg, var(--white));
    border:1px solid var(--hairline); border-radius:var(--radius-btn);
    box-shadow:var(--shadow-card); text-decoration:none; color:var(--ll-btn-fg, var(--charcoal));
    transition:box-shadow .18s, background .18s, border-color .18s, transform .14s;
}
.link-btn:hover { box-shadow:var(--shadow-hover); border-color:var(--light-gray); transform:translateY(-2px); }
.link-btn.featured { background:var(--ll-feat-bg, var(--charcoal)); border-color:var(--ll-feat-bg, var(--charcoal)); }
.link-btn.featured .btn-label { color:var(--ll-feat-fg, var(--white)); }
.link-btn.featured .btn-sub { color:rgba(255,255,255,.42); }
.link-btn.featured .btn-arrow { color:rgba(255,255,255,.25); }
.link-btn.featured:hover { filter:brightness(1.12); box-shadow:0 8px 28px rgba(0,0,0,.22); }
.btn-body { display:flex; flex-direction:column; align-items:center; gap:3px; }
.btn-label { font-weight:600; font-size:.95rem; letter-spacing:.13em; text-transform:uppercase; color:inherit; line-height:1.1; }
.btn-sub { font-weight:400; font-size:.85rem; color:var(--mid-gray); letter-spacing:.01em; }
.btn-arrow { font-size:1.3rem; color:var(--light-gray); line-height:1; margin-left:4px; transition:transform .18s,color .18s; }
.link-btn:hover .btn-arrow { transform:translateX(2px); color:var(--mid-gray); }
.rule { width:100%; height:1px; background:var(--hairline); margin:4px 0 0; }
/* WS7 fix 8 (supersedes fix 4): accordions are themed cards. The card surface uses
   the button color with button-text-color heading/chevron/description; inner pills
   invert to the page background color with content-text-color text. Inner-pill bg
   falls back to the page-bg default when a background IMAGE suppresses --ll-bg. */
.accordion { width:100%; background:var(--ll-btn-bg, var(--white)); color:var(--ll-btn-fg, var(--charcoal)); border:1px solid var(--hairline); border-radius:var(--radius-accord); box-shadow:var(--shadow-card); overflow:hidden; }
.accord-trigger { width:100%; display:flex; align-items:center; justify-content:space-between; padding:18px 22px; background:none; border:none; cursor:pointer; gap:12px; color:inherit; }
.accord-trigger:hover .accord-chevron { opacity:.95; }
.accord-heading { font-family:var(--ll-serif); font-weight:100; font-style:normal; font-size:1.4rem; color:inherit; letter-spacing:.02em; text-align:left; }
.accord-chevron { width:20px; height:20px; color:inherit; opacity:.6; transition:transform .26s ease,opacity .18s; flex-shrink:0; }
.accord-chevron svg { width:100%; height:100%; fill:none; stroke:currentColor; stroke-width:1.5; stroke-linecap:round; stroke-linejoin:round; }
.accordion.open .accord-chevron { transform:rotate(180deg); opacity:.95; }
.accord-body { max-height:0; overflow:hidden; transition:max-height .32s cubic-bezier(.4,0,.2,1); }
.accordion.open .accord-body { max-height:1600px; }
.accord-divider { height:1px; background:currentColor; opacity:.18; margin:0 22px; }
.accord-inner { padding:16px 22px 20px; }
.accord-desc { font-weight:400; font-size:.95rem; color:inherit; opacity:.8; line-height:1.6; margin-bottom:12px; text-align:center; }
.accord-links { display:flex; flex-direction:column; gap:10px; }
.accord-link { width:100%; display:flex; align-items:center; justify-content:center; gap:8px; padding:15px 24px; background:var(--ll-bg, var(--page-bg)); border:1px solid rgba(0,0,0,.10); border-radius:var(--radius-btn); text-decoration:none; color:var(--ll-fg, var(--charcoal)); transition:box-shadow .18s,transform .14s; }
.accord-link:hover { box-shadow:var(--shadow-hover); transform:translateY(-1px); }
.accord-link-body { display:flex; flex-direction:column; align-items:center; gap:3px; }
.accord-link-label { font-weight:600; font-size:.9rem; letter-spacing:.12em; text-transform:uppercase; color:inherit; line-height:1.1; }
.accord-link-sub { font-weight:400; font-size:.8rem; color:inherit; opacity:.7; }
.accord-link-arrow { font-size:1.2rem; color:inherit; opacity:.5; margin-left:4px; transition:transform .18s,opacity .18s; }
.accord-link:hover .accord-link-arrow { transform:translateX(2px); opacity:.8; }
.ll-footer { width:100%; max-width:480px; margin-top:24px; font-size:1.05rem; line-height:1.6; color:var(--mid-gray); text-align:center; }
@media (max-width:520px){ .hero-title{ font-size:2.2rem; } }`;
    }
}

if ( !customElements.get( "secc-link-list" ) ) {
    customElements.define( "secc-link-list", SeccLinkListElement );
}
