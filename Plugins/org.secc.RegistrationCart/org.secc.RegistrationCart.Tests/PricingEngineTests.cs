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

public class PricingEngineTests
{
    private readonly PricingEngine _engine = new();
    private static readonly DateTime Today = new(2026, 6, 24);

    // ---- helpers -------------------------------------------------------

    private static CartLineItem Line(int offeringId, string offeringTitle, string registrant,
        decimal basePrice, params decimal[] addOnPrices)
    {
        var line = new CartLineItem
        {
            OfferingId = offeringId,
            Offering = new Offering { Id = offeringId, Title = offeringTitle },
            Registrant = new Registrant { FirstName = registrant, LastName = "" },
            OfferingVariant = new OfferingVariant { Name = $"{offeringTitle} variant" },
            SnapshotBasePrice = basePrice
        };
        foreach (var p in addOnPrices)
            line.AddOns.Add(new CartLineItemAddOn
            {
                AddOnItem = new AddOnItem { Name = $"Add {p}", Price = p },
                SnapshotPrice = p
            });
        return line;
    }

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

    // ---- tests ---------------------------------------------------------

    [Fact]
    public void Line_total_is_base_plus_addons()
    {
        var cart = CartWith(new[] { Line(1, "Adventure", "Sam", 250m, 35m, 25m) });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(310m, priced.Lines[0].LineTotal);
        Assert.Equal(310m, priced.Subtotal);
        Assert.Equal(310m, priced.Total);
    }

    [Fact]
    public void Subtotal_sums_all_lines_across_registrants()
    {
        var cart = CartWith(new[]
        {
            Line(1, "Adventure", "Sam", 250m),
            Line(2, "Sports", "Alex", 220m, 25m),
        });

        var priced = _engine.Price(cart, Today);

        Assert.Equal(495m, priced.Subtotal);
    }

    [Fact]
    public void Order_percent_discount_applies_to_subtotal()
    {
        var code = new DiscountCode { Code = "EARLYBIRD", Type = DiscountType.Percent, Value = 10m, Scope = DiscountScope.Order };
        var cart = CartWith(new[] { Line(1, "Adventure", "Sam", 300m) }, code);

        var priced = _engine.Price(cart, Today);

        Assert.Equal(30m, priced.DiscountTotal);
        Assert.Equal(270m, priced.Total);
    }

    [Fact]
    public void Order_amount_discount_subtracts_flat_value()
    {
        var code = new DiscountCode { Code = "SAVE25", Type = DiscountType.Amount, Value = 25m, Scope = DiscountScope.Order };
        var cart = CartWith(new[] { Line(1, "Adventure", "Sam", 300m) }, code);

        var priced = _engine.Price(cart, Today);

        Assert.Equal(275m, priced.Total);
    }

    [Fact]
    public void LineItem_amount_discount_applies_per_line()
    {
        var code = new DiscountCode { Code = "SIBLING", Type = DiscountType.Amount, Value = 20m, Scope = DiscountScope.LineItem };
        var cart = CartWith(new[]
        {
            Line(1, "Adventure", "Sam", 250m),
            Line(2, "Sports", "Alex", 220m),
        }, code);

        var priced = _engine.Price(cart, Today);

        Assert.Equal(40m, priced.DiscountTotal); // $20 x 2 lines
        Assert.Equal(430m, priced.Total);
    }

    [Fact]
    public void Offering_scoped_discount_only_touches_matching_offering()
    {
        var code = new DiscountCode { Code = "ADVENTURE15", Type = DiscountType.Percent, Value = 15m, Scope = DiscountScope.Offering, OfferingId = 1 };
        var cart = CartWith(new[]
        {
            Line(1, "Adventure", "Sam", 300m),  // matches -> 15% of 300 = 45
            Line(2, "Sports", "Alex", 200m),    // not matched
        }, code);

        var priced = _engine.Price(cart, Today);

        Assert.Equal(45m, priced.DiscountTotal);
        Assert.Equal(455m, priced.Total);
    }

    [Fact]
    public void Discounts_never_drive_total_below_zero()
    {
        var huge = new DiscountCode { Code = "MEGA", Type = DiscountType.Amount, Value = 1000m, Scope = DiscountScope.Order };
        var cart = CartWith(new[] { Line(1, "Adventure", "Sam", 250m) }, huge);

        var priced = _engine.Price(cart, Today);

        Assert.Equal(250m, priced.DiscountTotal); // clamped to subtotal
        Assert.Equal(0m, priced.Total);
    }

    [Fact]
    public void Multiple_discounts_stack_but_clamp_in_aggregate()
    {
        var pct = new DiscountCode { Code = "HALF", Type = DiscountType.Percent, Value = 50m, Scope = DiscountScope.Order };
        var flat = new DiscountCode { Code = "BIG", Type = DiscountType.Amount, Value = 200m, Scope = DiscountScope.Order };
        var cart = CartWith(new[] { Line(1, "Adventure", "Sam", 300m) }, pct, flat);

        var priced = _engine.Price(cart, Today);

        // 50% = 150 applied first, leaving 150; flat 200 clamps to remaining 150.
        Assert.Equal(300m, priced.DiscountTotal);
        Assert.Equal(0m, priced.Total);
    }

    [Fact]
    public void Expired_discount_is_ignored()
    {
        var expired = new DiscountCode
        {
            Code = "OLD", Type = DiscountType.Percent, Value = 10m, Scope = DiscountScope.Order,
            ValidTo = new DateTime(2026, 1, 1)
        };
        var cart = CartWith(new[] { Line(1, "Adventure", "Sam", 300m) }, expired);

        var priced = _engine.Price(cart, Today);

        Assert.Empty(priced.Discounts);
        Assert.Equal(300m, priced.Total);
    }
}
