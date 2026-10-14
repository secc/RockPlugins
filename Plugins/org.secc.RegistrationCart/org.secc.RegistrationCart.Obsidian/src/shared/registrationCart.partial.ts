// Shared helpers for the Registration Cart Obsidian blocks. Enum values mirror
// org.secc.RegistrationCart.Domain (C#) and travel over the wire as plain ints.

import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";
import { toCurrencyOrNull } from "@Obsidian/Utility/numberUtils";
import { FilterExpressionType } from "@Obsidian/Core/Reporting/filterExpressionType";
import { Guid } from "@Obsidian/Types";
import { ComparisonValue } from "@Obsidian/Types/Reporting/comparisonValue";
import { getFieldType } from "@Obsidian/Utility/fieldTypes";
import { areEqual } from "@Obsidian/Utility/guid";
import { RegistrationEntryFormFieldBag } from "@Obsidian/ViewModels/Blocks/Event/RegistrationEntry/registrationEntryFormFieldBag";
import { RegistrationEntryVisibilityBag } from "@Obsidian/ViewModels/Blocks/Event/RegistrationEntry/registrationEntryVisibilityBag";

export const GradeBand = {
    None: 0,
    Elementary: 1,
    MiddleSchool: 2,
    HighSchool: 4,
    All: 7,
    Adults: 8,
    Everyone: 15
} as const;

export const SelectionMode = {
    Single: 0,
    Multi: 1
} as const;

export const DiscountType = {
    Amount: 0,
    Percent: 1
} as const;

export const DiscountScope = {
    Order: 0,
    LineItem: 1,
    Offering: 2
} as const;

/** A registrant with band `band` may pick something whose eligibility flags overlap it. */
export function isEligible(eligibleFlags: number, band: number): boolean {
    return band !== 0 && (eligibleFlags & band) !== 0;
}

export function gradeBandText(band: number): string {
    switch (band) {
        case GradeBand.Elementary: return "Elementary";
        case GradeBand.MiddleSchool: return "Middle School";
        case GradeBand.HighSchool: return "High School";
        case GradeBand.Adults: return "Adult";
        case GradeBand.All: return "All students";
        case GradeBand.Everyone: return "Everyone";
        default: return "Grade not set";
    }
}

export function gradeBandFlagsText(flags: number): string {
    if (flags === GradeBand.Everyone) return "Everyone";
    if (flags === GradeBand.All) return "All students";
    const parts: string[] = [];
    if (flags & GradeBand.Elementary) parts.push("Elementary");
    if (flags & GradeBand.MiddleSchool) parts.push("Middle School");
    if (flags & GradeBand.HighSchool) parts.push("High School");
    if (flags & GradeBand.Adults) parts.push("Adults");
    return parts.length ? parts.join(", ") : "Nobody";
}

/** Grades a new child can be created with. */
export const gradeBandOptions: ListItemBag[] = [
    { value: String(GradeBand.Elementary), text: "Elementary (K–5)" },
    { value: String(GradeBand.MiddleSchool), text: "Middle School (6–8)" },
    { value: String(GradeBand.HighSchool), text: "High School (9–12)" }
];

/** Eligibility bands an admin can grant. */
export const eligibilityOptions: ListItemBag[] = [
    ...gradeBandOptions,
    { value: String(GradeBand.Adults), text: "Adults" }
];

/** Bit flags → ["1","2"] for a CheckBoxList. */
export function flagsToList(flags: number): string[] {
    return [GradeBand.Elementary, GradeBand.MiddleSchool, GradeBand.HighSchool, GradeBand.Adults]
        .filter(b => (flags & b) !== 0)
        .map(b => String(b));
}

/** ["1","2"] → bit flags. */
export function listToFlags(values: string[] | null | undefined): number {
    return (values ?? []).reduce((acc, v) => acc | (parseInt(v, 10) || 0), 0);
}

/** A placeholder icon for a catalog card without an image, picked from the Rock category name. */
export function offeringEmoji(categoryText: string | null | undefined): string {
    const t = (categoryText ?? "").toLowerCase();
    if (t.includes("camp")) return "🏕️";
    if (t.includes("retreat")) return "⛰️";
    if (t.includes("mission") || t.includes("trip")) return "✈️";
    if (t.includes("class") || t.includes("study")) return "📚";
    if (t.includes("event") || t.includes("conference")) return "🎟️";
    return "📋";
}

export function money(value: number | null | undefined): string {
    const n = value ?? 0;
    return toCurrencyOrNull(n) ?? `$${n.toFixed(2)}`;
}

/** Navigate the browser (block actions never redirect server-side). */
export function goTo(url: string | null | undefined): void {
    if (url) {
        window.location.href = url;
    }
}

/**
 * The generated bag types allow null/undefined strings, but Rock's text inputs bind to
 * plain strings. Editable form rows use this shape (null/undefined string → "").
 */
export type WithStrings<T> = { [K in keyof T]-?: NonNullable<T[K]> extends string ? string : Exclude<T[K], undefined> };

/** "Riley Admin" → "R" for avatar chips (first-name initial reads cleaner than two letters). */
export function initials(name: string | null | undefined): string {
    const first = (name ?? "").trim().split(/\s+/)[0] ?? "";
    return first ? first[0].toUpperCase() : "?";
}

// #region Registration form visibility (Rock's RegistrationEntry rules)

/** Whether a registration form field shows, given the answers so far (Rock's field visibility rules). */
export function isFieldVisible(field: RegistrationEntryFormFieldBag, fieldValues: Record<Guid, unknown>, formFields: RegistrationEntryFormFieldBag[]): boolean {
    const rules = field.visibilityRules ?? [];
    switch (field.visibilityRuleType) {
        case FilterExpressionType.GroupAll:
            return rules.every(rule => isRuleMet(rule, fieldValues, formFields));
        case FilterExpressionType.GroupAllFalse:
            return rules.every(rule => !isRuleMet(rule, fieldValues, formFields));
        case FilterExpressionType.GroupAny:
            return rules.some(rule => isRuleMet(rule, fieldValues, formFields));
        case FilterExpressionType.GroupAnyFalse:
            return rules.some(rule => !isRuleMet(rule, fieldValues, formFields));
    }

    return true;
}

function isRuleMet(rule: RegistrationEntryVisibilityBag, fieldValues: Record<Guid, unknown>, formFields: RegistrationEntryFormFieldBag[]): boolean {
    const value = fieldValues[rule.comparedToRegistrationTemplateFormFieldGuid] || "";
    if (typeof value !== "string" || !rule.comparisonValue) {
        return false;
    }

    const comparedTo = formFields.find(f => areEqual(f.guid, rule.comparedToRegistrationTemplateFormFieldGuid));
    const fieldType = comparedTo?.attribute?.fieldTypeGuid ? getFieldType(comparedTo.attribute.fieldTypeGuid) : null;
    if (!fieldType) {
        return false;
    }

    const comparison: ComparisonValue = {
        value: rule.comparisonValue.value ?? "",
        comparisonType: rule.comparisonValue.comparisonType
    };

    return fieldType.doesValueMatchFilter(value, comparison, comparedTo?.attribute?.configurationValues ?? {});
}

// #endregion
