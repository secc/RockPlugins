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
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Domain.Entities;
using org.secc.RegistrationCart.Domain.Pricing;
using Xunit;

namespace org.secc.RegistrationCart.Tests;

/// <summary>
/// How each applied discount is split across lines. Checkout posts one transaction detail per
/// line at <c>LineTotal - DiscountFor(line)</c> to that line's account, so the split decides
/// which accounts a discount comes out of.
/// </summary>
public class DiscountAllocationTests
{
    private readonly PricingEngine _engine = new();
    private static readonly DateTime Today = new(2026, 6, 24);

    // ---- helpers -------------------------------------------------------

    private static CartLineItem Line(int id, int offeringId, decimal basePrice, params decimal[] addOnPrices) =>
        LineWithFees(id, offeringId, basePrice, addOnPrices.Select(p => (p, false)).ToArray());

    /// <summary>A line whose add-ons say whether their Rock fee allows discounts.</summary>
    private static CartLineItem LineWithFees(int id, int offeringId, decimal basePrice, params (decimal Price, bool DiscountApplies)[] addOns)
    {
        var line = new CartLineItem
        {
            Id = id,
            OfferingId = offeringId,
            Offering = new Offering { Id = offeringId, Title = $"Offering {offeringId}" },
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

    private static DiscountCode Code(string code, DiscountScope scope, DiscountType type, decimal value, int? offeringId = null) =>
        new() { Code = code, Scope = scope, Type = type, Value = value, OfferingId = offeringId };

    private static Cart CartWith(IEnumerable<CartLineItem> lines, params DiscountCode[] codes)
    {
        var cart = new Cart();
        foreach (var l in lines) cart.LineItems.Add(l);
        foreach (var c in codes)
            cart.Discounts.Add(new CartDiscount
            {
                DiscountCode = c,
                SnapshotValue = c.Value,
                SnapshotType = c.Type,
                SnapshotScope = c.Scope,
                SnapshotOfferingId = c.OfferingId
            });
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
        Assert.All(priced.Discounts, d => Assert.True(d.Allocations.Sum(a => a.Amount) <= d.Amount));
    }

    // ---- scope ---------------------------------------------------------

    [Fact]
    public void Offering_code_comes_off_matching_lines_only()
    {
        var cart = CartWith(new[]
        {
            Line(1, offeringId: 1, 300m),
            Line(2, offeringId: 2, 200m),
            Line(3, offeringId: 1, 100m),
        }, Code("ADV15", DiscountScope.Offering, DiscountType.Percent, 15m, offeringId: 1));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(60m, priced.DiscountTotal);
        Assert.Equal(45m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2));
        Assert.Equal(15m, priced.DiscountFor(3));
        Assert.Equal(new[] { "ADV15" }, priced.CodesFor(1));
        Assert.Empty(priced.CodesFor(2));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Offering_flat_amount_is_per_matching_line()
    {
        var cart = CartWith(new[]
        {
            Line(1, offeringId: 1, 300m),
            Line(2, offeringId: 2, 200m),
            Line(3, offeringId: 1, 100m),
        }, Code("ADV25", DiscountScope.Offering, DiscountType.Amount, 25m, offeringId: 1));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(25m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2));
        Assert.Equal(25m, priced.DiscountFor(3));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Order_percent_splits_by_line_total_to_the_cent()
    {
        var cart = CartWith(new[]
        {
            Line(1, 1, 33.33m),
            Line(2, 2, 33.33m),
            Line(3, 3, 33.34m),
        }, Code("TENOFF", DiscountScope.Order, DiscountType.Percent, 10m));

        var priced = _engine.Price(cart, Today);

        // 333.3, 333.3, 333.4 cents: the spare cent goes to the largest remainder.
        Assert.Equal(3.33m, priced.DiscountFor(1));
        Assert.Equal(3.33m, priced.DiscountFor(2));
        Assert.Equal(3.34m, priced.DiscountFor(3));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Order_amount_split_gives_a_tied_spare_cent_to_the_earlier_line()
    {
        var cart = CartWith(new[]
        {
            Line(1, 1, 50m),
            Line(2, 2, 50m),
            Line(3, 3, 50m),
        }, Code("TEN", DiscountScope.Order, DiscountType.Amount, 10m));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(3.34m, priced.DiscountFor(1));
        Assert.Equal(3.33m, priced.DiscountFor(2));
        Assert.Equal(3.33m, priced.DiscountFor(3));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Add_ons_count_toward_a_line_share()
    {
        var cart = CartWith(new[]
        {
            Line(1, 1, 100m, 100m), // 200 with its add-on
            Line(2, 2, 200m),
        }, Code("HALF", DiscountScope.Order, DiscountType.Percent, 50m));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(100m, priced.DiscountFor(1));
        Assert.Equal(100m, priced.DiscountFor(2));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Free_line_gets_no_share_of_an_order_discount()
    {
        var cart = CartWith(new[]
        {
            Line(1, 1, 0m),
            Line(2, 2, 100m),
        }, Code("TENOFF", DiscountScope.Order, DiscountType.Percent, 10m));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(0m, priced.DiscountFor(1));
        Assert.Empty(priced.CodesFor(1));
        Assert.Equal(10m, priced.DiscountFor(2));
        AssertPostsExactly(priced);
    }

    // ---- caps and stacking (a code never takes more than its own lines have) ----

    [Fact]
    public void Per_line_amount_takes_at_most_each_line_and_nothing_moves()
    {
        var cart = CartWith(new[]
        {
            Line(1, 1, 10m),
            Line(2, 2, 100m),
        }, Code("SIBLING", DiscountScope.LineItem, DiscountType.Amount, 20m));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(30m, priced.DiscountTotal);  // $10 (all of the cheap line) + $20
        Assert.Equal(10m, priced.DiscountFor(1)); // can't go below zero
        Assert.Equal(20m, priced.DiscountFor(2)); // and the rest doesn't land here
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Offering_code_worth_more_than_its_lines_takes_only_those_lines()
    {
        var cart = CartWith(new[]
        {
            Line(1, offeringId: 1, 50m),
            Line(2, offeringId: 2, 200m),
        }, Code("ADV100", DiscountScope.Offering, DiscountType.Amount, 100m, offeringId: 1));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(50m, priced.DiscountTotal);
        Assert.Equal(50m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2)); // another camp's line (and account) gets nothing
        Assert.Empty(priced.CodesFor(2));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Offering_amount_is_capped_line_by_line()
    {
        var cart = CartWith(new[]
        {
            Line(1, offeringId: 1, 50m),
            Line(2, offeringId: 1, 30m),
            Line(3, offeringId: 2, 200m),
        }, Code("ADV100", DiscountScope.Offering, DiscountType.Amount, 100m, offeringId: 1));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(80m, priced.DiscountTotal); // each Camp A line, not $200 spread over the cart
        Assert.Equal(50m, priced.DiscountFor(1));
        Assert.Equal(30m, priced.DiscountFor(2));
        Assert.Equal(0m, priced.DiscountFor(3));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Stacked_codes_post_against_what_earlier_codes_left()
    {
        var cart = CartWith(new[]
        {
            Line(1, offeringId: 1, 200m),
            Line(2, offeringId: 2, 100m),
        },
        Code("HALF", DiscountScope.Order, DiscountType.Percent, 50m),
        Code("ADV50", DiscountScope.Offering, DiscountType.Percent, 50m, offeringId: 1));

        var priced = _engine.Price(cart, Today);

        // HALF: 100 off line 1, 50 off line 2. ADV50: 50% of 200 = 100, all of line 1's remainder.
        Assert.Equal(250m, priced.DiscountTotal);
        Assert.Equal(200m, priced.DiscountFor(1));
        Assert.Equal(50m, priced.DiscountFor(2));
        Assert.Equal(new[] { "HALF", "ADV50" }, priced.CodesFor(1));
        Assert.Equal(new[] { "HALF" }, priced.CodesFor(2));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Discounts_that_round_never_go_past_the_subtotal()
    {
        // 33.335% rounds to 33.34 off; the flat code then takes the 66.66 left, in cents.
        var cart = CartWith(new[] { Line(1, 1, 100m) },
            Code("ODD", DiscountScope.Order, DiscountType.Percent, 33.335m),
            Code("BIG", DiscountScope.Order, DiscountType.Amount, 1000m));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(100m, priced.DiscountTotal);
        Assert.Equal(66.66m, priced.Discounts.Single(d => d.Code == "BIG").Amount);
        Assert.Equal(0m, priced.Total);
        Assert.Equal(100m, priced.DiscountFor(1));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Random_carts_always_post_exactly()
    {
        var random = new Random(20261013);
        for (var n = 0; n < 500; n++)
        {
            var lines = Enumerable.Range(1, random.Next(1, 6))
                .Select(id => Line(id, random.Next(1, 4), random.Next(0, 50001) / 100m,
                    Enumerable.Range(0, random.Next(0, 3)).Select(_ => random.Next(0, 10001) / 100m).ToArray()))
                .Select(l => random.Next(0, 5) == 0 ? WaitListed(l) : l)
                .ToList();
            var codes = Enumerable.Range(0, random.Next(0, 4))
                .Select(i =>
                {
                    var scope = (DiscountScope)random.Next(0, 3);
                    var type = (DiscountType)random.Next(0, 2);
                    var value = type == DiscountType.Percent ? random.Next(1, 101) : random.Next(1, 30001) / 100m;
                    return Code($"C{i}", scope, type, value, scope == DiscountScope.Offering ? random.Next(1, 4) : null);
                })
                .ToArray();

            AssertPostsExactly(_engine.Price(CartWith(lines, codes), Today));
        }
    }

    // ---- wait list ---------------------------------------------------

    private static CartLineItem WaitListed(CartLineItem line)
    {
        line.IsWaitList = true;
        return line;
    }

    [Fact]
    public void Wait_listed_line_costs_nothing_now()
    {
        var cart = CartWith(new[]
        {
            Line(1, 1, 300m, 35m),
            WaitListed(Line(2, 1, 300m, 35m)),
        });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(335m, priced.Total);
        var waiting = priced.Lines.Single(l => l.LineItemId == 2);
        Assert.True(waiting.IsWaitList);
        Assert.Equal(0m, waiting.LineTotal);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Per_camper_code_does_not_count_wait_listed_campers()
    {
        var cart = CartWith(new[]
        {
            Line(1, 1, 300m),
            WaitListed(Line(2, 1, 300m)),
        }, Code("SIBLING", DiscountScope.LineItem, DiscountType.Amount, 20m));

        var priced = _engine.Price(cart, Today);

        // $20 for the one camper who pays now, not $40 squeezed onto that camper.
        Assert.Equal(20m, priced.DiscountTotal);
        Assert.Equal(20m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2));
        Assert.Empty(priced.CodesFor(2));
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Offering_percent_code_skips_wait_listed_campers()
    {
        var cart = CartWith(new[]
        {
            Line(1, offeringId: 1, 200m),
            WaitListed(Line(2, offeringId: 1, 200m)),
        }, Code("ADV15", DiscountScope.Offering, DiscountType.Percent, 15m, offeringId: 1));

        var priced = _engine.Price(cart, Today);

        Assert.Equal(30m, priced.DiscountTotal);
        Assert.Equal(30m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2));
        AssertPostsExactly(priced);
    }

    // ---- the line's own template code (financial assistance) ------------

    private static CartLineItem WithOwnCode(CartLineItem line, string code, decimal amount, decimal percentage = 0m)
    {
        line.TemplateDiscountCode = code;
        line.TemplateDiscountAmount = amount;
        line.TemplateDiscountPercentage = percentage;
        return line;
    }

    [Fact]
    public void Own_code_comes_off_its_line_only()
    {
        var cart = CartWith(new[]
        {
            WithOwnCode(Line(1, 1, 300m, 35m), "FA-ABC123", 50m),
            Line(2, 1, 300m),
        });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(50m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.DiscountFor(2));
        Assert.Equal(new[] { "FA-ABC123" }, priced.CodesFor(1));
        Assert.Equal(585m, priced.Total);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Cart_codes_never_stack_on_a_line_with_its_own_code()
    {
        var cart = CartWith(new[]
        {
            WithOwnCode(Line(1, 1, 300m), "FA-ABC123", 50m),
            Line(2, 1, 200m),
        },
        Code("HALF", DiscountScope.Order, DiscountType.Percent, 50m),
        Code("SIBLING", DiscountScope.LineItem, DiscountType.Amount, 20m),
        Code("BIG", DiscountScope.Order, DiscountType.Amount, 1000m));

        var priced = _engine.Price(cart, Today);

        // HALF is half of line 2 only, SIBLING counts line 2 only, BIG clamps to what line 2 has left.
        Assert.Equal(50m, priced.DiscountFor(1));
        Assert.Equal(new[] { "FA-ABC123" }, priced.CodesFor(1));
        Assert.Equal(200m, priced.DiscountFor(2));
        Assert.Equal(100m, priced.Discounts.Single(d => d.Code == "HALF").Amount);
        Assert.Equal(20m, priced.Discounts.Single(d => d.Code == "SIBLING").Amount);
        Assert.Equal(80m, priced.Discounts.Single(d => d.Code == "BIG").Amount);
        Assert.Equal(250m, priced.Total);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Own_code_is_capped_at_the_line_total()
    {
        // "More" assistance: the template cost less $20, on a line whose fee was waived.
        var cart = CartWith(new[] { WithOwnCode(Line(1, 1, 280m), "FA-MORE01", 380m) });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(280m, priced.DiscountFor(1));
        Assert.Equal(0m, priced.Total);
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Own_percentage_code_skips_fees_that_do_not_allow_discounts()
    {
        var cart = CartWith(new[] { WithOwnCode(Line(1, 1, 300m, 33.33m), "FA-PCT", 0m, 0.25m) });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(75m, priced.DiscountFor(1)); // 25% of the 300 base; the fee's Discount Applies is off (Rock's default)
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Own_percentage_code_includes_fees_that_allow_discounts()
    {
        var cart = CartWith(new[] { WithOwnCode(LineWithFees(1, 1, 300m, (33.33m, true)), "FA-PCT", 0m, 0.25m) });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(83.33m, priced.DiscountFor(1)); // 25% of 333.33, rounded
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Own_flat_code_does_not_take_fees_that_do_not_allow_discounts()
    {
        // As Rock charges it: a flat template discount is capped at the base plus discountable fees.
        var cart = CartWith(new[] { WithOwnCode(LineWithFees(1, 1, 280m, (50m, false)), "FA-MORE01", 380m) });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(280m, priced.DiscountFor(1));
        Assert.Equal(50m, priced.Total); // the bus fee is still due
        AssertPostsExactly(priced);
    }

    [Fact]
    public void Wait_listed_line_takes_nothing_off_now_even_with_its_own_code()
    {
        var cart = CartWith(new[] { WaitListed(WithOwnCode(Line(1, 1, 300m), "FA-ABC123", 50m)) });

        var priced = _engine.Price(cart, Today);

        Assert.Empty(priced.Discounts);
        Assert.Equal(0m, priced.Total);
    }

    [Fact]
    public void Random_carts_with_own_codes_always_post_exactly()
    {
        var random = new Random(20261014);
        for (var n = 0; n < 500; n++)
        {
            var lines = Enumerable.Range(1, random.Next(1, 6))
                .Select(id => Line(id, random.Next(1, 4), random.Next(0, 50001) / 100m,
                    Enumerable.Range(0, random.Next(0, 3)).Select(_ => random.Next(0, 10001) / 100m).ToArray()))
                .Select(l => random.Next(0, 3) == 0
                    ? WithOwnCode(l, $"FA-{l.Id}", random.Next(0, 2) == 0 ? random.Next(0, 60001) / 100m : 0m, random.Next(0, 2) == 0 ? random.Next(0, 101) / 100m : 0m)
                    : l)
                .Select(l => random.Next(0, 5) == 0 ? WaitListed(l) : l)
                .ToList();
            var codes = Enumerable.Range(0, random.Next(0, 4))
                .Select(i =>
                {
                    var scope = (DiscountScope)random.Next(0, 3);
                    var type = (DiscountType)random.Next(0, 2);
                    var value = type == DiscountType.Percent ? random.Next(1, 101) : random.Next(1, 30001) / 100m;
                    return Code($"C{i}", scope, type, value, scope == DiscountScope.Offering ? random.Next(1, 4) : null);
                })
                .ToArray();

            var priced = _engine.Price(CartWith(lines, codes), Today);

            AssertPostsExactly(priced);
            foreach (var line in lines.Where(l => l.TemplateDiscountCode is not null))
                Assert.All(priced.CodesFor(line.Id), c => Assert.Equal(line.TemplateDiscountCode, c));
        }
    }

    // ---- allocator -----------------------------------------------------

    [Fact]
    public void Capped_line_share_is_re_split_among_lines_with_room()
    {
        var shares = DiscountAllocator.Distribute(1000, new[] { 1m, 1m, 1m }, new long[] { 100, 1000, 1000 });

        Assert.Equal(new long[] { 100, 450, 450 }, shares);
    }

    [Fact]
    public void Nothing_is_allocated_to_lines_the_discount_does_not_apply_to()
    {
        var shares = DiscountAllocator.Allocate(1000, new[] { 1m, 0m }, new long[] { 300, 200 });

        Assert.Equal(new long[] { 300, 0 }, shares);
    }
}
