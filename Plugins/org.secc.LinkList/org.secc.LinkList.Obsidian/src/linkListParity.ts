// Shared display + safe-render helpers for the Link List renderers (the
// in-Rock Vue viewer and the standalone web component). Centralizes video
// URL/ID normalization, Rock image URLs, and the HTML/URL/text/color
// sanitizers so both renderers behave identically and can't drift.
//
// Production data stores header-video fields as EITHER a full URL or a bare
// id (per the WS2.5 audit), so each extractor accepts both forms. The field
// itself identifies the platform (HeaderVideo = YouTube, HeaderVideoVimeoId =
// Vimeo), so no cross-platform disambiguation is needed.

const GUID_PATTERN = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

// ---------------------------------------------------------------------------
// Safe-render helpers (shared by both renderers).
//
// NOTE: freeform intro/footer HTML is sanitized SERVER-SIDE in
// LinkListService.BuildBag (Rock's HtmlSanitizer), so every consumer - viewer,
// editor, and the public REST endpoint - receives clean HTML. The renderers
// therefore render those blobs as-is. The helpers below guard the STRUCTURED
// link-row fields (url / target / text / color) the renderers build markup from.
// ---------------------------------------------------------------------------

/** HTML-escapes a plain-text value for safe interpolation into markup. */
export function safeText(value: string | null | undefined): string {
    if (!value) {
        return "";
    }
    return String(value)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;");
}

/**
 * Returns a validated href (raw, not HTML-escaped), blocking
 * javascript:/data:/vbscript:/file: schemes. Callers interpolating into raw
 * markup must additionally pass the result through safeText(); Vue's :href
 * binding escapes on its own, so the viewer uses it directly.
 */
export function safeUrl(value: string | null | undefined): string {
    if (!value) {
        return "#";
    }
    const trimmed = String(value).trim();
    if (/^\s*(javascript|data|vbscript|file):/i.test(trimmed)) {
        return "#";
    }
    return trimmed;
}

/** Clamps a link target to the safe set. */
export function safeTarget(value: string | null | undefined): string {
    const allowed = new Set(["_self", "_blank", "_parent", "_top"]);
    return value && allowed.has(value) ? value : "_self";
}

/** Accepts hex, rgb()/rgba(), and bare color keywords; else null. */
export function safeColor(value: string | null | undefined): string | null {
    if (!value) {
        return null;
    }
    const trimmed = String(value).trim();
    if (/^#[0-9a-fA-F]{3,8}$/.test(trimmed)
        || /^rgb(a)?\([\d\s,.%]+\)$/.test(trimmed)
        || /^[a-z]+$/i.test(trimmed)) {
        return trimmed;
    }
    return null;
}


/** Extracts a YouTube video id from a watch/short/youtu.be URL or a bare id. */
export function extractYouTubeId(value: string | null | undefined): string | null {
    if (!value) {
        return null;
    }
    const v = value.trim();
    let m = v.match(/[?&]v=([A-Za-z0-9_-]+)/);
    if (m) {
        return m[1];
    }
    m = v.match(/youtu\.be\/([A-Za-z0-9_-]+)/i);
    if (m) {
        return m[1];
    }
    m = v.match(/youtube\.com\/(?:embed|shorts|v)\/([A-Za-z0-9_-]+)/i);
    if (m) {
        return m[1];
    }
    if (/^[A-Za-z0-9_-]{6,15}$/.test(v)) {
        return v;
    }
    return null;
}

/** Extracts a Vimeo numeric id from any vimeo.com/* URL or a bare numeric id. */
export function extractVimeoId(value: string | null | undefined): string | null {
    if (!value) {
        return null;
    }
    const v = value.trim();
    if (/^\d+$/.test(v)) {
        return v;
    }
    const m = v.match(/vimeo\.com\/(?:.*\/)?(\d+)/i);
    if (m) {
        return m[1];
    }
    return null;
}

/** Embeddable iframe src for a YouTube header video, or null. */
export function youTubeEmbedUrl(value: string | null | undefined): string | null {
    const id = extractYouTubeId(value);
    return id ? `https://www.youtube.com/embed/${id}` : null;
}

/** Embeddable iframe src for a Vimeo header video, or null. */
export function vimeoEmbedUrl(value: string | null | undefined): string | null {
    const id = extractVimeoId(value);
    return id ? `https://player.vimeo.com/video/${id}` : null;
}

/** Rock /getimage.ashx URL for a BinaryFile guid, or null when not a guid. */
function getImageUrl(baseUrl: string, guid: string | null | undefined, query: string): string | null {
    if (!guid || !GUID_PATTERN.test(guid.trim())) {
        return null;
    }
    const root = (baseUrl || "").replace(/\/$/, "");
    return `${root}/getimage.ashx?guid=${encodeURIComponent(guid.trim())}&${query}`;
}

/** Header image URL (legacy params: w=500&h=500&mode=crop). */
export function headerImageUrl(baseUrl: string, guid: string | null | undefined): string | null {
    return getImageUrl(baseUrl, guid, "w=500&h=500&mode=crop");
}

/** Full-bleed background image URL (legacy param: w=2400). */
export function backgroundImageUrl(baseUrl: string, guid: string | null | undefined): string | null {
    return getImageUrl(baseUrl, guid, "w=2400");
}

/** Header-band background image URL (WS11; wide, like the full background: w=2400). */
export function headerBackgroundImageUrl(baseUrl: string, guid: string | null | undefined): string | null {
    return getImageUrl(baseUrl, guid, "w=2400");
}
