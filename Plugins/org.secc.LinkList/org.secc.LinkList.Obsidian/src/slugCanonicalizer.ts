// Slug canonicalization for the editor, mirroring the server's
// LinkListService.CanonicalizeSlug byte for byte. Both are in turn a mirror of
// Rock's internal ContentChannelItemSlugService.MakeSlugValid, with one
// deliberate difference: the dash collapse runs AFTER the charset strip so the
// result is a FIXED POINT of MakeSlugValid (Rock's own order turns
// "rock & roll" into "rock--roll", which a second pass changes to "rock-roll").
// Rock re-canonicalizes whatever it is handed on save, and only a fixed point
// guarantees the slug the editor shows is the slug that gets stored.
//
// Any change here MUST be made in LinkListService.CanonicalizeSlug too; the
// parity table in tests/slugCanonicalizer.spec.ts matches the C# theory in
// SlugValidationTests case for case.

/** Longest slug Rock will store (ContentChannelItemSlug.Slug is nvarchar(200)). */
export const SLUG_MAX_LENGTH = 200;

/** What a STORED slug may look like (mirrors LinkListService.IsValidSlug). */
export const SLUG_PATTERN = /^[a-z0-9-]+$/;

// The entities Rock maps to a dash. Typed input never contains them, but pasted
// text can, and the server handles them - so the editor must agree.
const DASH_ENTITIES = ["&nbsp;", "&#160;", "&ndash;", "&#8211;", "&mdash;", "&#8212;"];

/**
 * Converts arbitrary text (a typed slug or a list title) into the exact slug
 * Rock will store: lowercased, spaces/underscores/dash-entities turned into
 * dashes, everything else removed, dash runs collapsed, trailing dashes
 * trimmed, capped at SLUG_MAX_LENGTH. Leading dashes are kept, matching Rock.
 * Returns "" when nothing usable survives; callers treat that as "no slug".
 */
export function canonicalizeSlug(value: string | null | undefined): string {
    let slug = (value ?? "").trim().toLowerCase();

    for (const entity of DASH_ENTITIES) {
        slug = slug.split(entity).join("-");
    }

    slug = slug.replace(/_/g, "-").replace(/ /g, "-");
    slug = slug.replace(/[^a-z0-9 -]/g, "");
    slug = slug.replace(/-+/g, "-");

    return slug.slice(0, SLUG_MAX_LENGTH).replace(/-+$/, "");
}
