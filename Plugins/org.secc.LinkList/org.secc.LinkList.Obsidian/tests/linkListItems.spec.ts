import { hoistFeatured, clearOtherFeatured, isFeaturedLink, groupIntoSections, sectionNestingMeta } from "../src/linkListItems";
import type { LinkItemBag } from "../src/types";

function link(guid: string, over: Partial<LinkItemBag> = {}): LinkItemBag {
    return { guid, itemType: "link", text: guid, url: `https://x.test/${guid}`, ...over };
}

function section(guid: string, over: Partial<LinkItemBag> = {}): LinkItemBag {
    return { guid, itemType: "section", text: guid, ...over };
}

describe("isFeaturedLink", () => {
    it("is true only for featured link rows", () => {
        expect(isFeaturedLink(link("a", { isFeatured: true }))).toBe(true);
        expect(isFeaturedLink(link("a", { isFeatured: false }))).toBe(false);
        // a section flagged featured is never treated as featured
        expect(isFeaturedLink({ guid: "s", itemType: "section", isFeatured: true })).toBe(false);
    });
});

describe("hoistFeatured", () => {
    it("moves the featured link to the front, preserving the rest of the order", () => {
        const items = [link("a"), link("b", { isFeatured: true }), link("c")];
        const out = hoistFeatured(items);
        expect(out.map(i => i.guid)).toEqual(["b", "a", "c"]);
    });

    it("returns a copy unchanged when nothing is featured", () => {
        const items = [link("a"), link("b"), link("c")];
        const out = hoistFeatured(items);
        expect(out.map(i => i.guid)).toEqual(["a", "b", "c"]);
        expect(out).not.toBe(items); // copy, not mutated
    });

    it("does not mutate the input array", () => {
        const items = [link("a"), link("b", { isFeatured: true })];
        hoistFeatured(items);
        expect(items.map(i => i.guid)).toEqual(["a", "b"]);
    });

    it("hoists only the first featured row when (wrongly) more than one is set", () => {
        const items = [link("a"), link("b", { isFeatured: true }), link("c", { isFeatured: true })];
        const out = hoistFeatured(items);
        expect(out[0].guid).toBe("b");
    });
});

describe("clearOtherFeatured", () => {
    it("clears featured on every row except the kept one (radio-like)", () => {
        const items = [link("a", { isFeatured: true }), link("b", { isFeatured: true }), link("c")];
        const out = clearOtherFeatured(items, 1);
        expect(out[0].isFeatured).toBe(false);
        expect(out[1].isFeatured).toBe(true);
        expect(out[2].isFeatured).toBeFalsy();
    });

    it("leaves untouched rows referentially identical (only changed rows are new objects)", () => {
        const items = [link("a", { isFeatured: true }), link("b", { isFeatured: true })];
        const out = clearOtherFeatured(items, 1);
        expect(out[1]).toBe(items[1]); // kept row not rebuilt
        expect(out[0]).not.toBe(items[0]); // cleared row is a new object
    });
});

function label(b: ReturnType<typeof groupIntoSections>[number]): string {
    return b.kind === "section" ? `section:${b.section.guid}` : `item:${b.item.guid}`;
}

describe("groupIntoSections (explicit nesting)", () => {
    it("interleaves top-level items with sections; nested links join, non-nested close", () => {
        const items = [
            link("a"), section("S1"), link("b", { indentLevel: 1 }), link("c"),
            section("S2"), link("d", { indentLevel: 1 }), link("e")
        ];
        const blocks = groupIntoSections(items);
        expect(blocks.map(label)).toEqual(["item:a", "section:S1", "item:c", "section:S2", "item:e"]);
        expect((blocks[1] as { children: { guid: string }[] }).children.map(i => i.guid)).toEqual(["b"]);
        expect((blocks[3] as { children: { guid: string }[] }).children.map(i => i.guid)).toEqual(["d"]);
    });

    it("a non-nested link right after a section is top-level, not a child", () => {
        const blocks = groupIntoSections([section("S"), link("x")]);
        expect(blocks.map(label)).toEqual(["section:S", "item:x"]);
        expect((blocks[0] as { children: unknown[] }).children).toHaveLength(0);
    });

    it("a separator closes the open section (later nested links go top-level)", () => {
        const blocks = groupIntoSections([
            section("S"), link("a", { indentLevel: 1 }),
            { guid: "sep", itemType: "separator" }, link("b", { indentLevel: 1 })
        ]);
        expect(blocks.map(label)).toEqual(["section:S", "item:sep", "item:b"]);
        expect((blocks[0] as { children: { guid: string }[] }).children.map(i => i.guid)).toEqual(["a"]);
    });

    it("hoists the featured link to the first top-level block", () => {
        const items = [section("S1"), link("b", { indentLevel: 1 }), link("feat", { isFeatured: true })];
        const blocks = groupIntoSections(hoistFeatured(items));
        expect(blocks[0]).toMatchObject({ kind: "item", item: { guid: "feat" } });
        expect(blocks[1].kind).toBe("section");
        expect((blocks[1] as { children: { guid: string }[] }).children.map(i => i.guid)).toEqual(["b"]);
    });
});

describe("sectionNestingMeta", () => {
    it("marks nested links as children; canNest tracks the open section; non-nested closes it", () => {
        const items = [
            link("a"), section("S"), link("b", { indentLevel: 1 }), link("c"), link("d", { indentLevel: 1 })
        ];
        expect(sectionNestingMeta(items)).toEqual([
            { isChild: false, canNest: false }, // a — pre-section
            { isChild: false, canNest: false }, // S — section header
            { isChild: true, canNest: true },   // b — nested, open
            { isChild: false, canNest: true },  // c — non-nested closes the group
            { isChild: false, canNest: false }  // d — group closed, nothing to nest into
        ]);
    });
});
