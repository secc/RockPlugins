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
using org.secc.RegistrationCart.Domain.Pricing;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

/// <summary>
/// Each cart line is one registration with at most one discount code, its registration
/// template's, applied as Rock applies a template discount. Checkout posts one transaction
/// detail per line at <c>LineTotal - DiscountFor(line)</c>, so a code must never reach another line.
/// </summary>
public class PricingEngineTests
{
    private readonly PricingEngine _engine = new();

    // ---- helpers -------------------------------------------------------

    private static CartLineItem Line(int id, decimal basePrice, params decimal[] addOnPrices) =>
        LineWithFees(id, basePrice, addOnPrices.Select(p => (p, false)).ToArray());

    /// <summary>A line whose add-ons say whether their Rock fee allows discounts.</summary>
    private static CartLineItem LineWithFees(int id, decimal basePrice, params (decimal Price, bool DiscountApplies)[] addOns)
    {
        var line = new CartLineItem
        {
            Id = id,
            OfferingId = 1,
            Offering = new Offering { Id = 1, Title = "Camp" },
            Registrant = new Registrant { FirstName = $"Kid {id}", LastName = "" },
            OfferingVariant = new OfferingVariant { Name = "Week 1" },
            SnapshotBasePrice = basePrice
        };
        foreach (var (p, discountApplies) in addOns)
            line.AddOns.Add(new CartLineItemAddOn
            {
                AddOnItem = new AddOnItem { Name = $"Add {p}", Price = p },
                SnapshotPrice = p,
                DiscountApplies = discountApplies
            });
        return line;
    }

    private static CartLineItem WithCode(CartLineItem line, string code, decimal amount, decimal percentage = 0m)
    {
        line.TemplateDiscountCode = code;
        line.TemplateDiscountAmount = amount;
        line.TemplateDiscountPercentage = percentage;
        return line;
    }

    private static CartLineItem WaitListed(CartLineItem line)
    {
        line.IsWaitList = true;
        return line;
    }

    private static Cart CartWith(params CartLineItem[] lines)
    {
        var cart = new Cart();
        foreach (var l in lines) cart.LineItems.Add(l);
        return cart;
    }

    /// <summary>What checkout relies on: no line goes below zero and the postings add up to the charge.</summary>
    private static void AssertPostsExactly(PricedCart priced)
    {
        foreach (var line in priced.Lines)
        {
            var discount = priced.DiscountFor(line.LineItemId);
            Assert.InRange(discount, 0m, line.LineTotal);
            Assert.Equal(discount, Math.Round(discount, 2));
        }

        Assert.Equal(priced.Total, priced.Lines.Sum(l => l.LineTotal - priced.DiscountFor(l.LineItemId)));
        Assert.All(priced.Discounts, d => Assert.Equal(d.Amount, d.Allocations.Single().Amount));
    }

    // ---- totals --------------------------------------------------------

    [Fact]
    public void Line_total_is_base_plus_addons()
    {
        var priced = _engine.Price(CartWith(Line(1, 250m, 35m, 25m)));

        Assert.Equal(310m, priced.Lines[0].LineTotal);
        Assert.Equal(310m, priced.Subtotal);
        Assert.Equal(310m, priced.Total);
        Assert.Empty(priced.Discounts);
    }

    [Fact]
    public void Subtotal_sums_all_lines_across_registrants()
    {
        var priced = _engine.Price(CartWith(Line(1, 250m), Line(2, 220m, 25m)));

        Assert.Equal(495m, priced.Subtotal);
    }

    // ---- one code per registration ------------------------------------

    [Fact]
    public void A_code_comes_off_its_own_line_only()
    {
        var priced = _engine.Price(CartWith(WithCode(Line(1, 300m, 35m), "FA-ABC123", 50m), Line(2, 300m)));

        Assert.Equal(50m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2));
        Assert.Equal("FA-ABC123", priced.Lines[0].DiscountCode);
        Assert.Null(priced.Lines[1].DiscountCode);
        Assert.Equal(585m, priced.Total);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void A_shared_code_on_several_lines_applies_to_each_line_on_its_own()
    {
        // A staff code typed on two campers' registrations: 50% of each, never pooled.
        var priced = _engine.Price(CartWith(
            WithCode(Line(1, 360m), "26CAMPSTAFF50", 0m, 0.5m),
            WithCode(Line(2, 320m), "26CAMPSTAFF50", 0m, 0.5m),
            Line(3, 305m)));

        Assert.Equal(180m, priced.DiscountFor(1));
        Assert.Equal(160m, priced.DiscountFor(2));
        Assert.Equal(0m, priced.DiscountFor(3));
        Assert.Equal(2, priced.Discounts.Count);
        Assert.Equal(645m, priced.Total);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void A_flat_code_worth_more_than_its_line_does_not_reach_another_line()
    {
        var priced = _engine.Price(CartWith(WithCode(Line(1, 50m), "SAVE100", 100m), Line(2, 200m)));

        Assert.Equal(50m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2));
        Assert.Equal(200m, priced.Total);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void A_flat_code_is_capped_at_the_line_total()
    {
        // "More" assistance: the template cost less $20, on a line whose fee was waived.
        var priced = _engine.Price(CartWith(WithCode(Line(1, 280m), "FA-MORE01", 380m)));

        Assert.Equal(280m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.Total);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void A_full_percentage_code_makes_the_line_free()
    {
        var priced = _engine.Price(CartWith(WithCode(LineWithFees(1, 360m, (40m, true)), "26HSCSTAFF100", 0m, 1m)));

        Assert.Equal(400m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.Total);
    }

    [Fact]
    public void A_percentage_code_skips_fees_that_do_not_allow_discounts()
    {
        var priced = _engine.Price(CartWith(WithCode(Line(1, 300m, 33.33m), "FA-PCT", 0m, 0.25m)));

        Assert.Equal(75m, priced.DiscountFor(1)); // 25% of the 300 base; the fee's Discount Applies is off (Rock's default)
        AssertPostsExactly(priced);
    }

    [Fact]
    public void A_percentage_code_includes_fees_that_allow_discounts()
    {
        var priced = _engine.Price(CartWith(WithCode(LineWithFees(1, 300m, (33.33m, true)), "FA-PCT", 0m, 0.25m)));

        Assert.Equal(83.33m, priced.DiscountFor(1)); // 25% of 333.33, rounded
        AssertPostsExactly(priced);
    }

    [Fact]
    public void A_flat_code_does_not_take_fees_that_do_not_allow_discounts()
    {
        // As RegistrationEntry charges it: a flat template discount comes off the base, then the
        // fees that allow discounts.
        var priced = _engine.Price(CartWith(WithCode(LineWithFees(1, 280m, (50m, false)), "FA-MORE01", 380m)));

        Assert.Equal(280m, priced.DiscountFor(1));
        Assert.Equal(50m, priced.Total); // the bus fee is still due
        AssertPostsExactly(priced);
    }

    [Fact]
    public void A_code_worth_nothing_stays_on_the_line_but_takes_nothing()
    {
        // e.g. a code whose Maximum Registrants is 0: Rock records it but the registrant pays in full.
        var priced = _engine.Price(CartWith(WithCode(Line(1, 300m), "NOREG", 0m)));

        Assert.Equal("NOREG", priced.Lines[0].DiscountCode);
        Assert.Empty(priced.Discounts);
        Assert.Equal(300m, priced.Total);
    }

    [Fact]
    public void Wait_listed_line_costs_nothing_now_and_takes_no_discount()
    {
        var priced = _engine.Price(CartWith(WaitListed(WithCode(Line(1, 300m, 25m), "FA-ABC123", 50m)), Line(2, 200m)));

        Assert.Equal(0m, priced.Lines[0].LineTotal);
        Assert.Null(priced.Lines[0].DiscountCode);
        Assert.Empty(priced.Discounts);
        Assert.Equal(200m, priced.Total);
    }

    [Fact]
    public void Random_carts_always_post_exactly()
    {
        var random = new Random(20261106);
        for (var n = 0; n < 500; n++)
        {
            var lines = Enumerable.Range(1, random.Next(1, 6))
                .Select(id => LineWithFees(id, random.Next(0, 50001) / 100m,
                    Enumerable.Range(0, random.Next(0, 3)).Select(_ => (random.Next(0, 10001) / 100m, random.Next(0, 2) == 0)).ToArray()))
                .Select(l => random.Next(0, 3) == 0
                    ? l
                    : WithCode(l, random.Next(0, 2) == 0 ? "STAFF50" : $"FA-{l.Id}",
                        random.Next(0, 2) == 0 ? random.Next(0, 60001) / 100m : 0m,
                        random.Next(0, 2) == 0 ? random.Next(0, 101) / 100m : 0m))
                .Select(l => random.Next(0, 5) == 0 ? WaitListed(l) : l)
                .ToArray();

            var priced = _engine.Price(CartWith(lines));

            AssertPostsExactly(priced);
            foreach (var d in priced.Discounts)
            {
                var line = lines.Single(l => l.Id == d.Allocations.Single().LineItemId);
                Assert.Equal(line.TemplateDiscountCode, d.Code);
                Assert.False(line.IsWaitList);
            }
        }
    }
}
