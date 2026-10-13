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
    /// Discount codes that aren't valid on that date are ignored.
    /// </summary>
    public PricedCart Price(Cart cart, DateTime asOf)
    {
        var lines = cart.LineItems.Select(BuildLine).ToList();

        var discounts = new List<AppliedDiscount>();
        var remaining = lines.Sum(l => l.LineTotal); // discounts can't exceed this in aggregate
        var lineRoom = lines.Select(l => ToCents(l.LineTotal)).ToArray(); // per line, what discounts haven't taken yet
        foreach (var cd in cart.Discounts)
        {
            // Check live validity window (IsActive, ValidFrom, ValidTo) — intentional.
            // Admin can deactivate a code and it stops working even on open carts.
            // The discount *amount* comes from the snapshot captured at apply-time.
            if (cd.DiscountCode is null || !cd.DiscountCode.IsValidOn(asOf))
                continue;

            var amount = ComputeDiscount(cd, lines);
            amount = Math.Min(amount, remaining); // never over-discount the cart
            if (amount <= 0)
                continue;

            var applied = Round(amount);
            var cents = DiscountAllocator.Allocate(ToCents(applied), AllocationWeights(cd, lines), lineRoom);
            var allocations = new List<DiscountAllocation>();
            for (var i = 0; i < lines.Count; i++)
            {
                if (cents[i] == 0)
                    continue;
                allocations.Add(new DiscountAllocation(lines[i].LineItemId, cents[i] / 100m));
                lineRoom[i] -= cents[i];
            }

            discounts.Add(new AppliedDiscount(cd.DiscountCode.Code, applied, allocations));
            remaining -= amount;
        }

        return new PricedCart(lines, discounts);
    }

    /// <summary>
    /// How one discount is spread over the lines, mirroring how <see cref="ComputeDiscount"/>
    /// arrived at its amount: percent discounts and order-level amounts in proportion to each
    /// line's total, per-line amounts evenly; offering-scoped codes only over matching lines.
    /// </summary>
    private static decimal[] AllocationWeights(CartDiscount cd, IReadOnlyList<PricedLineItem> lines) =>
        lines.Select(l =>
        {
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

    private static PricedLineItem BuildLine(CartLineItem li)
    {
        var addOns = li.AddOns
            .Where(a => a.AddOnItem is not null)
            .Select(a => new PricedAddOn(a.AddOnItemId, a.AddOnItem.Name, a.SnapshotPrice))
            .ToList();

        return new PricedLineItem(
            li.Id,
            li.RegistrantId,
            li.Registrant?.DisplayName ?? "",
            li.OfferingId,
            li.Offering?.Title ?? li.OfferingVariant?.Offering?.Title ?? "",
            li.OfferingVariant?.Name ?? "",
            li.SnapshotBasePrice,
            addOns);
    }

    /// <summary>Compute the raw discount amount for one applied code against the priced lines.</summary>
    private static decimal ComputeDiscount(CartDiscount cd, IReadOnlyList<PricedLineItem> lines)
    {
        switch (cd.SnapshotScope)
        {
            case DiscountScope.Order:
                var subtotal = lines.Sum(l => l.LineTotal);
                return cd.SnapshotType == DiscountType.Percent
                    ? subtotal * cd.SnapshotValue / 100m
                    : cd.SnapshotValue;

            case DiscountScope.LineItem:
                return cd.SnapshotType == DiscountType.Percent
                    ? lines.Sum(l => l.LineTotal) * cd.SnapshotValue / 100m
                    : cd.SnapshotValue * lines.Count; // flat amount per line item

            case DiscountScope.Offering:
                var matching = lines.Where(l => MatchesOffering(l, cd)).ToList();
                var matchBase = matching.Sum(l => l.LineTotal);
                return cd.SnapshotType == DiscountType.Percent
                    ? matchBase * cd.SnapshotValue / 100m
                    : cd.SnapshotValue * matching.Count;

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
