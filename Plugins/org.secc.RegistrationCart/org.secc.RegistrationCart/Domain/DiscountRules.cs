// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
#nullable enable
using System;

namespace org.secc.RegistrationCart.Domain;

/// <summary>
/// The terms of one Rock registration template discount (RegistrationTemplateDiscount) that
/// decide whether a code can be used: what Rock's RegistrationEntry checks when a code is
/// entered or auto-applied.
/// </summary>
public record TemplateDiscountTerms(
    string Code,
    decimal DiscountPercentage,
    decimal DiscountAmount,
    int? MaxUsage = null,
    int? MinRegistrants = null,
    int? MaxRegistrants = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    bool AutoApply = false);

/// <summary>Why a template discount code can't be used on a registration.</summary>
public enum DiscountProblem
{
    None = 0,
    /// <summary>No discount with that code on the registration's template.</summary>
    NotFound = 1,
    /// <summary>The code needs more registrants on the registration than it has.</summary>
    TooFewRegistrants = 2,
    /// <summary>Before the code's start date.</summary>
    NotYetAvailable = 3,
    /// <summary>After the code's end date.</summary>
    Expired = 4,
    /// <summary>The code's Maximum Usage for the instance has been reached.</summary>
    UsedUp = 5
}

/// <summary>
/// Template discount code rules, as Rock's RegistrationEntry applies them (SECC's fork,
/// lbDiscountApply_Click and AutoApplyDiscounts). Each cart line is one registration with one
/// registrant and at most one code; a shared code (e.g. a staff code) may be used on several
/// lines, and each use counts toward its Maximum Usage.
/// </summary>
public static class DiscountRules
{
    /// <summary>Every registration the cart makes has exactly one registrant.</summary>
    public const int RegistrantsPerRegistration = 1;

    /// <summary>
    /// Whether <paramref name="discount"/> may be used on a registration on
    /// <paramref name="today"/>, when the instance already has <paramref name="timesUsed"/>
    /// registrations with the code (Rock counts per registration instance; the cart adds its own
    /// lines for the instance that use it). Checked in Rock's order.
    /// </summary>
    public static DiscountProblem Check(TemplateDiscountTerms? discount, DateTime today, int timesUsed)
    {
        if (discount is null)
            return DiscountProblem.NotFound;

        if (discount.MinRegistrants.HasValue && RegistrantsPerRegistration < discount.MinRegistrants.Value)
            return DiscountProblem.TooFewRegistrants;

        if (discount.StartDate.HasValue && today < discount.StartDate.Value)
            return DiscountProblem.NotYetAvailable;

        if (discount.EndDate.HasValue && today > discount.EndDate.Value)
            return DiscountProblem.Expired;

        if (discount.MaxUsage.HasValue && timesUsed >= discount.MaxUsage.Value)
            return DiscountProblem.UsedUp;

        return DiscountProblem.None;
    }

    /// <summary>
    /// Whether the code's value applies to the registration's one registrant. Rock applies a code
    /// to the first Maximum Registrants registrants only, so a Maximum Registrants of 0 leaves
    /// the code on the registration but takes nothing off.
    /// </summary>
    public static bool AppliesToRegistrant(TemplateDiscountTerms discount) =>
        !discount.MaxRegistrants.HasValue || discount.MaxRegistrants.Value >= RegistrantsPerRegistration;

    /// <summary>Codes match the way Rock matches them: trimmed, ignoring case.</summary>
    public static bool SameCode(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>The message the family sees, worded as Rock's RegistrationEntry words it.</summary>
    public static string Message(DiscountProblem problem, string code, string discountCodeTerm, int? minRegistrants = null)
    {
        var term = string.IsNullOrWhiteSpace(discountCodeTerm) ? "Discount Code" : discountCodeTerm;
        return problem switch
        {
            DiscountProblem.NotFound => $"'{code}' is not a valid {term}.",
            DiscountProblem.TooFewRegistrants => $"The '{code}' {term} requires at least {minRegistrants} registrants.",
            DiscountProblem.NotYetAvailable => $"The '{code}' {term} is not available yet.",
            DiscountProblem.Expired => $"The '{code}' {term} has expired.",
            DiscountProblem.UsedUp => $"The '{code}' {term} is no longer available.",
            _ => ""
        };
    }
}
