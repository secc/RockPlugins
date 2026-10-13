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
namespace org.secc.RegistrationCart.Domain.Pricing;

/// <summary>A priced add-on selection on a line item.</summary>
public record PricedAddOn(int AddOnItemId, string Name, decimal Price);

/// <summary>A fully priced cart line: a registrant + variant + chosen add-ons.</summary>
public record PricedLineItem(
    int LineItemId,
    int RegistrantId,
    string RegistrantName,
    int OfferingId,
    string OfferingTitle,
    string VariantName,
    decimal BasePrice,
    IReadOnlyList<PricedAddOn> AddOns)
{
    public decimal AddOnTotal => AddOns.Sum(a => a.Price);
    public decimal LineTotal => BasePrice + AddOnTotal;
}

/// <summary>A discount that was successfully applied, with the amount it took off.</summary>
public record AppliedDiscount(string Code, decimal Amount);

/// <summary>The full priced result for a cart: lines, discounts, and totals.</summary>
public record PricedCart(
    IReadOnlyList<PricedLineItem> Lines,
    IReadOnlyList<AppliedDiscount> Discounts)
{
    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
    public decimal DiscountTotal => Discounts.Sum(d => d.Amount);

    /// <summary>Never goes below zero, regardless of how aggressive the discounts are.</summary>
    public decimal Total => Math.Max(0m, Subtotal - DiscountTotal);

    public static readonly PricedCart Empty =
        new(Array.Empty<PricedLineItem>(), Array.Empty<AppliedDiscount>());
}
