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
using System.Collections.Generic;
using System.Linq;
using org.secc.RegistrationCart.Domain.Entities;

namespace org.secc.RegistrationCart.Domain.Pricing;

/// <summary>
/// Computes line-item and total pricing for a cart, including discount codes and how each
/// discount splits across the lines it applies to (<see cref="DiscountAllocator"/>).
/// Pure: it reads a <see cref="Cart"/> (with navigation properties loaded) plus the
/// set of applied <see cref="CartDiscount"/>s and returns a <see cref="PricedCart"/>.
/// Prices are read from cart snapshots, not live catalog values, so admin catalog changes
/// never retroactively alter an in-flight cart.
/// No persistence, no Rock — fully unit testable.
/// </summary>
public class PricingEngine
{
    /// <summary>
    /// Price the cart as of <paramref name="asOf"/> (used to validate discount windows).
    /// Discount codes that aren't valid on that date are ignored. A code never takes more than
    /// is left of the lines it applies to: what it can't use is lost, not moved onto other lines.
    /// </summary>
    public PricedCart Price(Cart cart, DateTime asOf)
    {
        var items = cart.LineItems.ToList();
        var lines = items.Select(BuildLine).ToList();
        var discounts = new List<AppliedDiscount>();
        var lineRoom = lines.Select(l => ToCents(l.LineTotal)).ToArray(); // per line, what discounts haven't taken yet

        // A line with its template's own code takes that code first and no other: one code
        // per line, never stacked with the cart's codes. As Rock applies a template discount:
        // a percentage of the base price and the fees whose "Discount Applies" is set, and a
        // flat amount capped at the same.
        var hasOwnCode = items.Select(li => !string.IsNullOrWhiteSpace(li.TemplateDiscountCode)).ToArray();
        for (var i = 0; i < lines.Count; i++)
        {
            if (!hasOwnCode[i] || lines[i].IsWaitList)
                continue;

            var item = items[i];
            var discountable = DiscountableTotal(item);
            var raw = item.TemplateDiscountPercentage > 0m
                ? discountable * item.TemplateDiscountPercentage
                : item.TemplateDiscountAmount;
            var cents = Math.Min(Math.Min(ToCents(Math.Max(0m, raw)), ToCents(discountable)), lineRoom[i]);
            if (cents == 0)
                continue;

            var amount = cents / 100m;
            discounts.Add(new AppliedDiscount(item.TemplateDiscountCode!, amount, new[] { new DiscountAllocation(lines[i].LineItemId, amount) }));
            lineRoom[i] -= cents;
        }

        // Wait-listed lines owe nothing now and take no discount.
        var takesCartCodes = lines.Select((l, i) => !l.IsWaitList && !hasOwnCode[i]).ToArray();
        var payable = lines.Where((l, i) => takesCartCodes[i]).ToList();
        foreach (var cd in cart.Discounts)
        {
            // Check live validity window (IsActive, ValidFrom, ValidTo) — intentional.
            // Admin can deactivate a code and it stops working even on open carts.
            // The discount *amount* comes from the snapshot captured at apply-time.
            if (cd.DiscountCode is null || !cd.DiscountCode.IsValidOn(asOf))
                continue;

            var weights = AllocationWeights(cd, lines, takesCartCodes);
            var room = CartCodeRoom(lineRoom, takesCartCodes);
            var cents = IsPerLineAmount(cd)
                // A flat amount per camper (or per camper of the offering): each takes up to the
                // amount, and no more than its line has left.
                ? weights.Select((w, i) => w > 0m ? Math.Min(ToCents(cd.SnapshotValue), room[i]) : 0L).ToArray()
                // A percentage, or a flat amount off the order: split over the lines it applies
                // to, never more than they have left.
                : DiscountAllocator.Allocate(ToCents(Round(Math.Max(0m, ComputeDiscount(cd, payable)))), weights, room);

            var allocations = new List<DiscountAllocation>();
            for (var i = 0; i < lines.Count; i++)
            {
                if (cents[i] == 0)
                    continue;
                allocations.Add(new DiscountAllocation(lines[i].LineItemId, cents[i] / 100m));
                lineRoom[i] -= cents[i];
            }

            if (allocations.Any())
                discounts.Add(new AppliedDiscount(cd.DiscountCode.Code, allocations.Sum(a => a.Amount), allocations));
        }

        return new PricedCart(lines, discounts);
    }

    /// <summary>A flat amount per line item, or per line item of one offering.</summary>
    private static bool IsPerLineAmount(CartDiscount cd) =>
        cd.SnapshotType == DiscountType.Amount && cd.SnapshotScope != DiscountScope.Order;

    /// <summary>What a template discount applies to, as in Rock: the base price and the fees that allow discounts.</summary>
    private static decimal DiscountableTotal(CartLineItem item) =>
        item.SnapshotBasePrice + item.AddOns.Where(a => a.AddOnItem is not null && a.DiscountApplies).Sum(a => a.SnapshotPrice);

    /// <summary>
    /// Which lines a discount applies to and how it is spread over them, mirroring how
    /// <see cref="ComputeDiscount"/> arrived at its amount: percent discounts and order-level
    /// amounts in proportion to each line's total, per-line amounts evenly; offering-scoped codes
    /// only over matching lines. Lines that don't take cart codes (wait-listed, or with their own
    /// code) weigh nothing, and a line that weighs nothing takes none of the discount.
    /// </summary>
    private static decimal[] AllocationWeights(CartDiscount cd, IReadOnlyList<PricedLineItem> lines, bool[] takesCartCodes) =>
        lines.Select((l, i) =>
        {
            if (!takesCartCodes[i])
                return 0m;

            var byTotal = cd.SnapshotType == DiscountType.Percent || cd.SnapshotScope == DiscountScope.Order;
            var weight = byTotal ? l.LineTotal : 1m;
            switch (cd.SnapshotScope)
            {
                case DiscountScope.Order:
                case DiscountScope.LineItem:
                    return weight;
                case DiscountScope.Offering:
                    return MatchesOffering(l, cd) ? weight : 0m;
                default:
                    return 0m;
            }
        }).ToArray();

    /// <summary>What cart codes may still take from each line: nothing from a line they don't apply to.</summary>
    private static long[] CartCodeRoom(long[] lineRoom, bool[] takesCartCodes) =>
        lineRoom.Select((room, i) => takesCartCodes[i] ? room : 0L).ToArray();

    private static PricedLineItem BuildLine(CartLineItem li)
    {
        // A wait-listed line owes nothing now; Rock charges it when the camper comes off the list.
        var addOns = li.AddOns
            .Where(a => a.AddOnItem is not null)
            .Select(a => new PricedAddOn(a.AddOnItemId, a.AddOnItem.Name, li.IsWaitList ? 0m : a.SnapshotPrice))
            .ToList();

        return new PricedLineItem(
            li.Id,
            li.RegistrantId,
            li.Registrant?.DisplayName ?? "",
            li.OfferingId,
            li.Offering?.Title ?? li.OfferingVariant?.Offering?.Title ?? "",
            li.OfferingVariant?.Name ?? "",
            li.IsWaitList ? 0m : li.SnapshotBasePrice,
            addOns,
            li.IsWaitList);
    }

    /// <summary>
    /// The raw amount of a percentage code, or of a flat amount off the order, against the lines
    /// that take cart codes (flat per-line amounts are worked out line by line in <see cref="Price"/>).
    /// </summary>
    private static decimal ComputeDiscount(CartDiscount cd, IReadOnlyList<PricedLineItem> lines)
    {
        switch (cd.SnapshotScope)
        {
            case DiscountScope.Order:
                return cd.SnapshotType == DiscountType.Percent
                    ? lines.Sum(l => l.LineTotal) * cd.SnapshotValue / 100m
                    : cd.SnapshotValue;

            case DiscountScope.LineItem:
                return lines.Sum(l => l.LineTotal) * cd.SnapshotValue / 100m;

            case DiscountScope.Offering:
                return lines.Where(l => MatchesOffering(l, cd)).Sum(l => l.LineTotal) * cd.SnapshotValue / 100m;

            default:
                return 0m;
        }
    }

    private static bool MatchesOffering(PricedLineItem line, CartDiscount cd) =>
        cd.SnapshotOfferingId.HasValue && line.OfferingId == cd.SnapshotOfferingId.Value;

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static long ToCents(decimal value) =>
        (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
}
