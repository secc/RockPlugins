// Pure theming helpers for the editor (WS3): preset = base, per-list color
// overrides win, "Custom" when an override diverges from the preset. Kept free
// of Vue so they can be unit-tested directly.

import type { LinkListBag, DesignOptionBag } from "./types";

export type ColorField =
    | "contentTextColor"
    | "backgroundColor"
    | "buttonColor"
    | "buttonTextColor"
    | "featuredButtonColor"
    | "featuredButtonTextColor"
    | "titleColor";

// Drives effectiveColor / isCustomTheme / applyDesignChange — adding a color
// field is mechanical: list it here (WS3 was built so WS10's featured colors and
// WS7's title color just extend this array).
export const COLOR_FIELDS: ColorField[] = [
    "contentTextColor", "backgroundColor", "buttonColor", "buttonTextColor",
    "featuredButtonColor", "featuredButtonTextColor", "titleColor"
];

export const CUSTOM_DESIGN = "__custom__";

export function findPreset(designs: DesignOptionBag[] | undefined, designId: string | null | undefined): DesignOptionBag | undefined {
    if (!designId) {
        return undefined;
    }
    return (designs || []).find(d => d.value === designId);
}

/** The color shown in a field: per-list override, else the selected preset's value. */
export function effectiveColor(bag: LinkListBag, designs: DesignOptionBag[] | undefined, field: ColorField): string {
    return bag[field] || findPreset(designs, bag.designId)?.[field] || "";
}

/** True when any color override diverges from the selected preset (or there's no preset but explicit colors are set). */
export function isCustomTheme(bag: LinkListBag, designs: DesignOptionBag[] | undefined): boolean {
    const preset = findPreset(designs, bag.designId);
    return COLOR_FIELDS.some(f => {
        const override = bag[f];
        const presetVal = preset?.[f] || "";
        return !!override && override !== presetVal;
    });
}

/** The value the preset dropdown should show: the design id, or the Custom sentinel. */
export function designSelectionValue(bag: LinkListBag, designs: DesignOptionBag[] | undefined): string {
    return isCustomTheme(bag, designs) ? CUSTOM_DESIGN : (bag.designId || "");
}

/**
 * Applies a preset-dropdown change to the bag (mutates it):
 * - "Custom": seed overrides from the current effective colors, drop the preset.
 * - a preset (or blank): clear overrides so the colors inherit cleanly.
 */
export function applyDesignChange(bag: LinkListBag, designs: DesignOptionBag[] | undefined, value: string): void {
    if (value === CUSTOM_DESIGN) {
        for (const f of COLOR_FIELDS) {
            bag[f] = effectiveColor(bag, designs, f);
        }
        bag.designId = null;
        return;
    }
    bag.designId = value || null;
    for (const f of COLOR_FIELDS) {
        bag[f] = "";
    }
}
