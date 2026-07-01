// Pure item-list helpers shared by the editor and both renderers (WS10 featured
// button). Kept free of Vue/DOM so they can be unit-tested directly.

import type { LinkItemBag } from "./types";

/** True when a row is the featured link (only link rows can be featured). */
export function isFeaturedLink(item: LinkItemBag): boolean {
    return !!item.isFeatured && (item.itemType ?? "link") === "link";
}

/**
 * Render-time hoist: returns a NEW array with the single featured link moved to
 * the front and everything else left in its existing order. Does not mutate, and
 * never moves the row in the persisted data (WS6 reorder + Order stay intact).
 * If more than one row is flagged (shouldn't happen — the editor + server enforce
 * one), only the first is hoisted.
 */
export function hoistFeatured(items: LinkItemBag[]): LinkItemBag[] {
    const idx = items.findIndex(isFeaturedLink);
    if (idx < 0) {
        return items.slice();
    }
    const copy = items.slice();
    const [featured] = copy.splice(idx, 1);
    return [featured, ...copy];
}

/**
 * Radio-like single-select: returns a NEW array where every row EXCEPT the one at
 * keepIndex has its featured flag cleared. Used by the editor when a row is turned
 * featured, so at most one stays featured.
 */
export function clearOtherFeatured(items: LinkItemBag[], keepIndex: number): LinkItemBag[] {
    return items.map((it, i) => (i !== keepIndex && it.isFeatured ? { ...it, isFeatured: false } : it));
}

/** True when a link carries the "nested" flag (WS7 fix 5 reuses IndentLevel: >=1 = nested). */
export function isNestedLink(item: LinkItemBag): boolean {
    return (item.itemType ?? "link") === "link" && (item.indentLevel ?? 0) >= 1;
}

export interface SectionBlock {
    kind: "section";
    section: LinkItemBag;
    children: LinkItemBag[];
}

export interface ItemBlock {
    kind: "item";
    item: LinkItemBag;
}

export type RenderBlock = SectionBlock | ItemBlock;

/**
 * Render-time grouping (WS7 fix 5 — explicit membership). Walks the flat ordered
 * list and produces ordered blocks so top-level items INTERLEAVE with sections:
 * - a `section` row opens a group (accordion) at its position;
 * - a link with the nested flag set, while a section group is open, joins that
 *   section's children;
 * - a NON-nested link renders top-level at its position and CLOSES the open group
 *   (a section's children are the contiguous run of nested links right after it);
 * - a separator renders top-level and closes the open group;
 * - a link before any open section is always top-level.
 * Pure, non-mutating; grouping is render-only (Order/array unchanged). Call AFTER
 * hoistFeatured so the featured link lands as the first top-level block.
 */
export function groupIntoSections(items: LinkItemBag[]): RenderBlock[] {
    const blocks: RenderBlock[] = [];
    let current: SectionBlock | null = null;
    for (const it of items) {
        const type = it.itemType ?? "link";
        if (type === "section") {
            current = { kind: "section", section: it, children: [] };
            blocks.push(current);
        }
        else if (type === "separator") {
            current = null;
            blocks.push({ kind: "item", item: it });
        }
        else if (current && (it.indentLevel ?? 0) >= 1) {
            current.children.push(it);
        }
        else {
            blocks.push({ kind: "item", item: it });
            current = null; // a non-nested link closes the open section group
        }
    }
    return blocks;
}

export interface NestMeta {
    /** Effectively nested under an open section (indented in the editor). */
    isChild: boolean;
    /** A section group is open at this row, so it can be nested into it. */
    canNest: boolean;
}

/**
 * Per-index nesting meta for the editor, mirroring groupIntoSections exactly so the
 * editor's visual nesting matches the renderer. `isChild` = the row is a nested link
 * inside a currently-open section; `canNest` = a section group is open here (a link
 * can be toggled into it).
 */
export function sectionNestingMeta(items: LinkItemBag[]): NestMeta[] {
    const out: NestMeta[] = [];
    let open = false;
    for (const it of items) {
        const type = it.itemType ?? "link";
        if (type === "section") {
            out.push({ isChild: false, canNest: false });
            open = true;
        }
        else if (type === "separator") {
            out.push({ isChild: false, canNest: false });
            open = false;
        }
        else {
            const nested = (it.indentLevel ?? 0) >= 1;
            out.push({ isChild: open && nested, canNest: open });
            if (open && !nested) {
                open = false;
            }
        }
    }
    return out;
}
