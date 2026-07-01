import {
    effectiveColor,
    isCustomTheme,
    designSelectionValue,
    applyDesignChange,
    CUSTOM_DESIGN
} from "../src/linkListTheme";
import type { LinkListBag, DesignOptionBag } from "../src/types";

const designs: DesignOptionBag[] = [
    {
        value: "preset-a",
        text: "Preset A",
        contentTextColor: "#111111",
        backgroundColor: "#222222",
        buttonColor: "#333333",
        buttonTextColor: "#444444",
        featuredButtonColor: "#555555",
        featuredButtonTextColor: "#666666",
        titleColor: "#777777"
    }
];

function bag(over: Partial<LinkListBag> = {}): LinkListBag {
    return { title: "", slug: "", isPublic: true, items: [], ...over };
}

describe("effectiveColor", () => {
    it("returns the override when set", () => {
        expect(effectiveColor(bag({ designId: "preset-a", backgroundColor: "#abcabc" }), designs, "backgroundColor")).toBe("#abcabc");
    });
    it("falls back to the preset when the override is empty", () => {
        expect(effectiveColor(bag({ designId: "preset-a" }), designs, "backgroundColor")).toBe("#222222");
    });
    it("is empty when there is neither override nor preset", () => {
        expect(effectiveColor(bag(), designs, "backgroundColor")).toBe("");
    });
    it("resolves the WS10 featured-button colors from the preset", () => {
        expect(effectiveColor(bag({ designId: "preset-a" }), designs, "featuredButtonColor")).toBe("#555555");
        expect(effectiveColor(bag({ designId: "preset-a" }), designs, "featuredButtonTextColor")).toBe("#666666");
    });
    it("lets a featured-color override win over the preset", () => {
        expect(effectiveColor(bag({ designId: "preset-a", featuredButtonColor: "#abcdef" }), designs, "featuredButtonColor")).toBe("#abcdef");
    });
    it("resolves the WS7 title color from the preset and lets an override win", () => {
        expect(effectiveColor(bag({ designId: "preset-a" }), designs, "titleColor")).toBe("#777777");
        expect(effectiveColor(bag({ designId: "preset-a", titleColor: "#010203" }), designs, "titleColor")).toBe("#010203");
    });
});

describe("isCustomTheme", () => {
    it("is false for a preset with no overrides", () => {
        expect(isCustomTheme(bag({ designId: "preset-a" }), designs)).toBe(false);
    });
    it("is false when an override equals the preset value", () => {
        expect(isCustomTheme(bag({ designId: "preset-a", backgroundColor: "#222222" }), designs)).toBe(false);
    });
    it("is true when an override diverges from the preset", () => {
        expect(isCustomTheme(bag({ designId: "preset-a", backgroundColor: "#999999" }), designs)).toBe(true);
    });
    it("is true when colors are set without a preset", () => {
        expect(isCustomTheme(bag({ backgroundColor: "#999999" }), designs)).toBe(true);
    });
    it("flips to custom when a featured-button color diverges from the preset", () => {
        expect(isCustomTheme(bag({ designId: "preset-a", featuredButtonColor: "#000000" }), designs)).toBe(true);
        expect(isCustomTheme(bag({ designId: "preset-a", featuredButtonColor: "#555555" }), designs)).toBe(false);
    });
});

describe("designSelectionValue", () => {
    it("returns the design id for a pure preset", () => {
        expect(designSelectionValue(bag({ designId: "preset-a" }), designs)).toBe("preset-a");
    });
    it("returns Custom when an override diverges", () => {
        expect(designSelectionValue(bag({ designId: "preset-a", buttonColor: "#000000" }), designs)).toBe(CUSTOM_DESIGN);
    });
});

describe("applyDesignChange", () => {
    it("picking a preset sets designId and clears overrides", () => {
        const b = bag({ designId: null, backgroundColor: "#999999" });
        applyDesignChange(b, designs, "preset-a");
        expect(b.designId).toBe("preset-a");
        expect(b.backgroundColor).toBe("");
    });
    it("choosing Custom seeds overrides from effective colors and clears designId", () => {
        const b = bag({ designId: "preset-a" });
        applyDesignChange(b, designs, CUSTOM_DESIGN);
        expect(b.designId).toBeNull();
        expect(b.backgroundColor).toBe("#222222");
        expect(b.contentTextColor).toBe("#111111");
        // and now it reads as custom
        expect(isCustomTheme(b, designs)).toBe(true);
    });
});
