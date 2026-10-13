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
namespace org.secc.RegistrationCart.Domain.Entities;

/// <summary>
/// The person being registered (a child or adult), as seen by the pricing engine.
/// Built from the Rock-side CartRegistrant by Services/CartPricing.
/// </summary>
public class Registrant
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// Drives which variants/add-ons are eligible. Derived from the Rock Person's
    /// grade (see Services/GradeBands).
    /// </summary>
    public GradeBand GradeBand { get; set; } = GradeBand.None;

    /// <summary>Linked Rock person alias.</summary>
    public int? RockPersonAliasId { get; set; }

    public string DisplayName => $"{FirstName} {LastName}".Trim();

    public ICollection<CartLineItem> LineItems { get; set; } = new List<CartLineItem>();
}

/// <summary>
/// A shopping cart belonging to a family/account. Holds every registrant + line item
/// for a single checkout. Pure pricing model; the persisted cart is Model/Cart.
/// </summary>
public class Cart
{
    public int Id { get; set; }

    /// <summary>Owning Rock family, when known.</summary>
    public int? RockFamilyId { get; set; }
    public int? OwnerPersonAliasId { get; set; }

    public CartStatus Status { get; set; } = CartStatus.Open;

    /// <summary>
    /// Stable key generated once per cart. Not used by the Rock plugin, which uses the
    /// cart row (Model/Cart.Status) as its idempotency record.
    /// </summary>
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Registrant> Registrants { get; set; } = new List<Registrant>();
    public ICollection<CartLineItem> LineItems { get; set; } = new List<CartLineItem>();
    public ICollection<CartDiscount> Discounts { get; set; } = new List<CartDiscount>();
}

/// <summary>
/// One registrant registered for one offering variant, plus their chosen add-ons.
/// Prices are snapshotted at add-to-cart time so later catalog price changes don't
/// affect in-flight carts.
/// </summary>
public class CartLineItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int RegistrantId { get; set; }
    public Registrant Registrant { get; set; } = null!;

    public int OfferingId { get; set; }
    public Offering Offering { get; set; } = null!;

    public int OfferingVariantId { get; set; }
    public OfferingVariant OfferingVariant { get; set; } = null!;

    /// <summary>Variant base price captured when the item was added to the cart.</summary>
    public decimal SnapshotBasePrice { get; set; }

    /// <summary>On the wait list: priced at zero and left out of discounts until a spot opens.</summary>
    public bool IsWaitList { get; set; }

    public ICollection<CartLineItemAddOn> AddOns { get; set; } = new List<CartLineItemAddOn>();
}

/// <summary>A selected add-on on a line item.</summary>
public class CartLineItemAddOn
{
    public int Id { get; set; }
    public int CartLineItemId { get; set; }
    public CartLineItem CartLineItem { get; set; } = null!;

    public int AddOnItemId { get; set; }
    public AddOnItem AddOnItem { get; set; } = null!;

    /// <summary>Add-on price captured when the item was added to the cart.</summary>
    public decimal SnapshotPrice { get; set; }
}

/// <summary>
/// One audit record per checkout attempt (success or failure). Written regardless of
/// outcome so ops can reconstruct what happened for any cart.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public int? CartId { get; set; }
    public string Action { get; set; } = "";     // "checkout", "refund", etc.
    public string Outcome { get; set; } = "";    // "Success" | "Failure"
    public string? Detail { get; set; }           // JSON error detail on failure
    public string? RockRefs { get; set; }         // JSON: { transactionId, registrationIds }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>A discount code applied to a cart.</summary>
public class CartDiscount
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int DiscountCodeId { get; set; }
    public DiscountCode DiscountCode { get; set; } = null!;

    /// <summary>
    /// Discount values snapshotted at apply-time so later admin changes don't affect
    /// carts already in progress.
    /// </summary>
    public decimal SnapshotValue { get; set; }
    public DiscountType SnapshotType { get; set; }
    public DiscountScope SnapshotScope { get; set; }
    public int? SnapshotOfferingId { get; set; }
}
