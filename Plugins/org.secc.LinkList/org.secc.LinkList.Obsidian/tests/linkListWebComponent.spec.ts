/**
 * @jest-environment jsdom
 *
 * Render tests for the <secc-link-list> web component — the single shipped
 * renderer (WS7 design). Mounts the element with a mocked fetch and asserts the
 * rendered shadow-DOM markup, so the round-2 features (WS9 subtitle/description,
 * WS10 featured, WS11 header band, WS12 global header/footer) and the WS7 design
 * (accordion grouping, fonts) are covered the way they appear on a real embed.
 */
import "../src/webComponents/linkList";
import type { LinkListBag } from "../src/types";

function baseBag(over: Partial<LinkListBag> = {}): LinkListBag {
    return { slug: "demo", title: "Demo", isPublic: true, items: [], ...over };
}

function mockFetch(bag: LinkListBag): void {
    (global as unknown as { fetch: unknown }).fetch = jest.fn(() => Promise.resolve({
        ok: true,
        status: 200,
        json: () => Promise.resolve(bag)
    }));
}

async function flush(): Promise<void> {
    await Promise.resolve();
    await Promise.resolve();
    await new Promise(resolve => setTimeout(resolve, 0));
    await Promise.resolve();
}

async function mount(bag: LinkListBag, attrs: Record<string, string> = {}): Promise<HTMLElement> {
    mockFetch(bag);
    const el = document.createElement("secc-link-list");
    el.setAttribute("item-id", "demo");
    el.setAttribute("base-url", "https://rock.example.org");
    for (const [k, v] of Object.entries(attrs)) {
        el.setAttribute(k, v);
    }
    document.body.appendChild(el);
    await flush();
    return el;
}

function html(el: HTMLElement): string {
    return el.shadowRoot?.innerHTML ?? "";
}

afterEach(() => {
    document.body.innerHTML = "";
});

describe("WS9 — subtitle + description", () => {
    it("renders a link subtitle (part hook + text)", async () => {
        const el = await mount(baseBag({
            items: [{ guid: "1", itemType: "link", text: "Give", url: "https://x.test", subtitle: "Tap to donate", order: 0 }]
        }));
        const out = html(el);
        expect(out).toContain('part="link-subtitle"');
        expect(out).toContain("Tap to donate");
        expect(out).toContain("Give");
    });

    it("renders a section description inside its accordion", async () => {
        const el = await mount(baseBag({
            items: [{ guid: "2", itemType: "section", text: "Connect", description: "Ways to plug in", order: 0 }]
        }));
        const out = html(el);
        expect(out).toContain('part="section-description"');
        expect(out).toContain("Ways to plug in");
        expect(out).toContain("Connect");
    });

    it("escapes subtitle text (no HTML injection)", async () => {
        const el = await mount(baseBag({
            items: [{ guid: "1", itemType: "link", text: "Give", url: "https://x.test", subtitle: "<img src=x onerror=alert(1)>", order: 0 }]
        }));
        const out = html(el);
        expect(out).not.toContain("<img src=x");
        expect(out).toContain("&lt;img");
    });
});

describe("WS10 — featured button", () => {
    it("hoists the featured link to the top, marks it featured, applies featured color var", async () => {
        const el = await mount(baseBag({
            effectiveFeaturedButtonColor: "#ff6b35",
            items: [
                { guid: "1", itemType: "link", text: "First", url: "https://a.test", order: 0 },
                { guid: "2", itemType: "link", text: "Star", url: "https://b.test", isFeatured: true, order: 1 }
            ]
        }));
        const out = html(el);
        expect(out).toContain("link-btn featured");
        expect(out).toContain("--ll-feat-bg:#ff6b35");
        expect(out.indexOf("Star")).toBeGreaterThan(-1);
        expect(out.indexOf("Star")).toBeLessThan(out.indexOf("First"));
    });

    it("renders no featured pill when nothing is featured", async () => {
        const el = await mount(baseBag({
            items: [{ guid: "1", itemType: "link", text: "Plain", url: "https://a.test", order: 0 }]
        }));
        expect(html(el)).not.toContain("link-btn featured");
    });
});

describe("WS11 — header background band", () => {
    const imgGuid = "C9F920B9-B462-447B-ABC8-CE150A135F8D";

    it("renders the hero-bg with the wide image when set", async () => {
        const el = await mount(baseBag({ headerBackgroundImage: imgGuid }));
        const out = html(el);
        expect(out).toContain('class="hero-bg"');
        expect(out).toContain(`getimage.ashx?guid=${imgGuid}&amp;w=2400`);
    });

    it("renders a hero (title) but no hero-bg element when unset", async () => {
        const el = await mount(baseBag({ title: "My List" }));
        const out = html(el);
        expect(out).toContain('class="hero"');
        expect(out).not.toContain('class="hero-bg"');
    });
});

describe("WS12 — global header/footer", () => {
    it("renders global header above and footer below, only when present", async () => {
        const el = await mount(baseBag({
            globalHeaderContent: "<div>GLOBAL TOP</div>",
            globalFooterContent: "<div>GLOBAL BOTTOM</div>",
            items: [{ guid: "1", itemType: "link", text: "L", url: "https://a.test", order: 0 }]
        }));
        const out = html(el);
        expect(out).toContain('part="global-header"');
        expect(out).toContain("GLOBAL TOP");
        expect(out).toContain('part="global-footer"');
        expect(out).toContain("GLOBAL BOTTOM");
        expect(out.indexOf("GLOBAL TOP")).toBeLessThan(out.indexOf('part="content"'));
        expect(out.indexOf("GLOBAL BOTTOM")).toBeGreaterThan(out.indexOf('part="content"'));
    });

    it("omits global header/footer when not provided", async () => {
        const out = html(await mount(baseBag()));
        expect(out).not.toContain('part="global-header"');
        expect(out).not.toContain('part="global-footer"');
    });
});

describe("WS7 — accordion sections", () => {
    it("groups a section's following links into its accordion; pre-section links stay top-level", async () => {
        const el = await mount(baseBag({
            items: [
                { guid: "t", itemType: "link", text: "TopLink", url: "https://t.test", order: 0 },
                { guid: "s", itemType: "section", text: "Connect", isSectionCollapsed: false, order: 1 },
                { guid: "c1", itemType: "link", text: "Campus", url: "https://c.test", indentLevel: 1, order: 2 },
                { guid: "c2", itemType: "link", text: "Groups", url: "https://g.test", indentLevel: 1, order: 3 }
            ]
        }));
        const out = html(el);
        // TopLink is a top-level pill before the accordion element (match the
        // rendered class attr, not the bare word which also appears in the CSS).
        expect(out.indexOf("TopLink")).toBeLessThan(out.indexOf('class="accordion'));
        expect(out).toContain("accord-heading");
        // child links live inside the accordion (after the heading)
        expect(out.indexOf("Connect")).toBeLessThan(out.indexOf("Campus"));
        expect(out).toContain("Campus");
        expect(out).toContain("Groups");
    });

    it("opens by default when not collapsed; closed when IsSectionCollapsed", async () => {
        const open = html(await mount(baseBag({
            items: [{ guid: "s", itemType: "section", text: "Open", isSectionCollapsed: false, order: 0 }]
        })));
        expect(open).toContain('class="accordion open"');
        expect(open).toContain('aria-expanded="true"');

        document.body.innerHTML = "";
        const closed = html(await mount(baseBag({
            items: [{ guid: "s", itemType: "section", text: "Shut", isSectionCollapsed: true, order: 0 }]
        })));
        expect(closed).toContain('class="accordion"');
        expect(closed).not.toContain('class="accordion open"');
        expect(closed).toContain('aria-expanded="false"');
    });
});

describe("WS7 fix5 — render order + header media precedence", () => {
    const imgGuid = "C9F920B9-B462-447B-ABC8-CE150A135F8D";

    it("renders intro before the featured button", async () => {
        const el = await mount(baseBag({
            introContent: "<p>INTRO TEXT</p>",
            items: [{ guid: "f", itemType: "link", text: "FeatLink", url: "https://f.test", isFeatured: true, order: 0 }]
        }));
        const out = html(el);
        expect(out.indexOf("INTRO TEXT")).toBeLessThan(out.indexOf("FeatLink"));
    });

    it("shows the header video instead of the header image when both are set", async () => {
        const el = await mount(baseBag({
            headerImage: imgGuid,
            headerVideo: "https://youtu.be/Q4_gpN1bjmU"
        }));
        const out = html(el);
        expect(out).toContain("youtube.com/embed/Q4_gpN1bjmU");
        expect(out).not.toContain('class="hero-logo');
    });
});

describe("WS7 fix7 — title color", () => {
    it("applies effectiveTitleColor to the hero title via the --ll-title var", async () => {
        const el = await mount(baseBag({ title: "My List", effectiveTitleColor: "#abcdef" }));
        const out = html(el);
        expect(out).toContain("--ll-title:#abcdef");
        expect(out).toContain("var(--ll-title");
    });
});

describe("WS7 fix8 — accordion themed card", () => {
    it("card surface is driven by the button-color var; inner pills by the bg var", async () => {
        const el = await mount(baseBag({
            effectiveButtonColor: "#0c1116",
            effectiveBackgroundColor: "#f5f4f2",
            items: [
                { guid: "s", itemType: "section", text: "Explore", isSectionCollapsed: false, order: 0 },
                { guid: "c", itemType: "link", text: "Child", url: "https://c.test", indentLevel: 1, order: 1 }
            ]
        }));
        const out = html(el);
        // theme vars present + accordion/child rendered
        expect(out).toContain("--ll-btn-bg:#0c1116");
        expect(out).toContain('class="accordion open"');
        expect(out).toContain("accord-link");
        // themed-card CSS: card surface uses the button var, inner pill the bg var
        expect(out).toContain("background:var(--ll-btn-bg, var(--white))");
        expect(out).toContain("background:var(--ll-bg, var(--page-bg))");
    });
});

describe("WS7 fix5 — explicit section membership", () => {
    it("nests a flagged link but keeps a non-flagged link after the section top-level", async () => {
        const el = await mount(baseBag({
            items: [
                { guid: "s", itemType: "section", text: "Connect", isSectionCollapsed: false, order: 0 },
                { guid: "in", itemType: "link", text: "InsideLink", url: "https://in.test", indentLevel: 1, order: 1 },
                { guid: "out", itemType: "link", text: "OutsideLink", url: "https://out.test", order: 2 }
            ]
        }));
        const out = html(el);
        // nested link is inside the accordion body; non-nested renders top-level after it
        expect(out).toContain("accord-link");
        expect(out).toContain("InsideLink");
        expect(out).toContain("OutsideLink");
        // OutsideLink comes after the accordion (its own top-level pill), not inside accord-links
        expect(out.indexOf("InsideLink")).toBeLessThan(out.indexOf("OutsideLink"));
        expect(out.indexOf("accord-link")).toBeLessThan(out.indexOf("OutsideLink"));
    });
});

describe("WS7 fix9 — intro inside the header band", () => {
    it("renders the intro inside the hero (after hero start, before content)", async () => {
        const el = await mount(baseBag({
            title: "My List",
            introContent: "<p>INTRO TEXT</p>",
            items: [{ guid: "f", itemType: "link", text: "FeatLink", url: "https://f.test", isFeatured: true, order: 0 }]
        }));
        const out = html(el);
        const hero = out.indexOf('part="hero"');
        const intro = out.indexOf('part="intro"');
        const content = out.indexOf('part="content"');
        expect(hero).toBeGreaterThan(-1);
        expect(intro).toBeGreaterThan(hero);
        expect(intro).toBeLessThan(content);
        expect(out).toContain("var(--ll-title"); // intro default color = title color
    });
});

describe("WS7 — fonts", () => {
    it("loads the IvyJournal Adobe kit only when the org enables it", async () => {
        const on = html(await mount(baseBag({ useIvyJournalFont: true })));
        expect(on).toContain("use.typekit.net/cjf8ejp");

        document.body.innerHTML = "";
        const off = html(await mount(baseBag({ useIvyJournalFont: false })));
        expect(off).not.toContain("use.typekit.net/cjf8ejp");
        // fallback serif still loads
        expect(off).toContain("Cormorant+Garamond");
    });
});
