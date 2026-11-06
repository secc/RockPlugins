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
/// Computes line-item and total pricing for a cart. Every line is its own Rock registration and
/// takes at most one discount code: its registration template's code (typed by the family,
/// auto-applied by the template, or made by the discount code workflow), applied the way Rock
/// applies a template discount. A code never reaches another line.
/// Pure: it reads a <see cref="Cart"/> (with navigation properties loaded) and returns a
/// <see cref="PricedCart"/>. Prices and code values are read from cart snapshots, not live
/// Rock values, so admin changes never retroactively alter an in-flight cart.
/// No persistence, no Rock — fully unit testable.
/// </summary>
public class PricingEngine
{
    /// <summary>Price the cart.</summary>
    public PricedCart Price(Cart cart)
    {
        var items = cart.LineItems.ToList();
        var lines = items.Select(BuildLine).ToList();
        var discounts = new List<AppliedDiscount>();

        for (var i = 0; i < lines.Count; i++)
        {
            var item = items[i];

            // Wait-listed lines owe nothing now and take no discount (the code stays on the
            // registration for Rock to apply when a spot opens).
            if (lines[i].IsWaitList || string.IsNullOrWhiteSpace(item.TemplateDiscountCode))
                continue;

            var amount = DiscountFor(item);
            if (amount == 0m)
                continue;

            discounts.Add(new AppliedDiscount(item.TemplateDiscountCode!, amount, new[] { new DiscountAllocation(lines[i].LineItemId, amount) }));
        }

        return new PricedCart(lines, discounts);
    }

    /// <summary>
    /// What a line's template code takes off, as Rock applies a template discount: a percentage
    /// of the base price and of the fees whose "Discount Applies" is set, or a flat amount capped
    /// at the same. Fees that don't allow discounts stay due. Rounded to the cent.
    /// </summary>
    public static decimal DiscountFor(CartLineItem item)
    {
        var discountable = DiscountableTotal(item);
        var raw = item.TemplateDiscountPercentage > 0m
            ? discountable * item.TemplateDiscountPercentage
            : item.TemplateDiscountAmount;
        return Math.Min(Round(Math.Max(0m, raw)), Round(discountable));
    }

    /// <summary>What a template discount applies to, as in Rock: the base price and the fees that allow discounts.</summary>
    private static decimal DiscountableTotal(CartLineItem item) =>
        item.SnapshotBasePrice + item.AddOns.Where(a => a.AddOnItem is not null && a.DiscountApplies).Sum(a => a.SnapshotPrice);

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
            li.IsWaitList,
            li.IsWaitList ? null : NullIfBlank(li.TemplateDiscountCode));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
