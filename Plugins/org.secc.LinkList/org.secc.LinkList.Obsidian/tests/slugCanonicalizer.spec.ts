import { canonicalizeSlug, SLUG_MAX_LENGTH, SLUG_PATTERN } from "../src/slugCanonicalizer";

// The table below is the C#<->TS parity contract: every case here also appears in
// SlugValidationTests.CanonicalizeSlug_Produces_The_Text_Rock_Stores. If one side
// changes, change both - the editor must show the slug the server will store.

describe("canonicalizeSlug", () => {
    it.each([
        ["my-list", "my-list"],
        ["My-List", "my-list"],
        ["  MY-LIST  ", "my-list"],
        ["Give Now", "give-now"],
        ["  My_List  ", "my-list"],
        ["Summer Camp 2026", "summer-camp-2026"],
        ["give--now", "give-now"],
        ["a&nbsp;b", "a-b"],
        ["a&#8212;b", "a-b"],
        ["foo-", "foo"],
        ["-foo", "-foo"],
        ["café!", "caf"],
        ["list/../etc", "listetc"],
        ["<script>", "script"],
        ["-", ""],
        ["---", ""],
        ["&", ""],
        ["!!!", ""],
        ["   ", ""],
        ["", ""]
    ])("%j -> %j", (input, expected) => {
        expect(canonicalizeSlug(input)).toBe(expected);
    });

    it("treats null and undefined as empty", () => {
        expect(canonicalizeSlug(null)).toBe("");
        expect(canonicalizeSlug(undefined)).toBe("");
    });

    it("collapses dashes left behind by stripped characters", () => {
        // Rock's own MakeSlugValid collapses before stripping and would yield
        // "rock--roll"; we strip first so the result is a fixed point.
        expect(canonicalizeSlug("Rock & Roll")).toBe("rock-roll");
    });

    it("caps length, trimming a dash left at the cut", () => {
        expect(canonicalizeSlug("a".repeat(SLUG_MAX_LENGTH + 1))).toBe("a".repeat(SLUG_MAX_LENGTH));
        expect(canonicalizeSlug("a".repeat(SLUG_MAX_LENGTH - 1) + "-b")).toBe("a".repeat(SLUG_MAX_LENGTH - 1));
    });

    it.each([
        "Rock & Roll",
        "Give Now",
        "give--now",
        "  My_List  ",
        "-foo-",
        "café!",
        "a&mdash;b",
        "-",
        ""
    ])("is a fixed point for %j", input => {
        // Rock re-canonicalizes on save, so canonical text must survive a second
        // pass unchanged or the editor shows a slug that isn't what gets stored.
        const once = canonicalizeSlug(input);
        expect(canonicalizeSlug(once)).toBe(once);
    });

    it.each([
        "Rock & Roll",
        "Summer Camp 2026",
        "give--now",
        "-foo-"
    ])("produces a storable slug for %j", input => {
        const slug = canonicalizeSlug(input);
        expect(slug).not.toBe("");
        expect(SLUG_PATTERN.test(slug)).toBe(true);
    });
});
