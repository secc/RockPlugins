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
using System;
using System.Collections.Generic;
using Rock.ViewModels.Controls;

// Bags/boxes cross the wire to the Obsidian (Vue) blocks. Enums travel as ints so the
// TypeScript side stays independent of the C# enum types; see src/shared/enums.partial.ts.
namespace org.secc.RegistrationCart.ViewModels
{
    #region Shared

    public class NavigationBag
    {
        public bool IsSignedIn { get; set; }
        public string LoginUrl { get; set; }
        public string CatalogUrl { get; set; }
        public string CartUrl { get; set; }
        public string CheckoutUrl { get; set; }
        public int CartItemCount { get; set; }
    }

    public class PricedCartBag
    {
        public List<PricedLineBag> Lines { get; set; } = new List<PricedLineBag>();
        public List<AppliedDiscountBag> Discounts { get; set; } = new List<AppliedDiscountBag>();
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }

    public class PricedLineBag
    {
        public int LineItemId { get; set; }
        public int RegistrantId { get; set; }
        public string RegistrantName { get; set; }
        public string OfferingTitle { get; set; }
        public string VariantName { get; set; }
        /// <summary>Event dates, e.g. "Jul 5–9, 2027" (empty when unknown).</summary>
        public string DateText { get; set; }
        public decimal BasePrice { get; set; }
        public List<PricedAddOnBag> AddOns { get; set; } = new List<PricedAddOnBag>();
        public decimal LineTotal { get; set; }
    }

    public class PricedAddOnBag
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class AppliedDiscountBag
    {
        public int CartDiscountId { get; set; }
        public string Code { get; set; }
        public decimal Amount { get; set; }
    }

    #endregion

    #region Catalog

    public class CatalogInitializationBox
    {
        public NavigationBag Navigation { get; set; }
        public string Heading { get; set; }
        public string Intro { get; set; }
        public List<OfferingSummaryBag> Offerings { get; set; } = new List<OfferingSummaryBag>();
    }

    /// <summary>A Rock RegistrationTemplate as a catalog card.</summary>
    public class OfferingSummaryBag
    {
        public string IdKey { get; set; }
        public string DetailUrl { get; set; }
        /// <summary>Rock category name (e.g. "Camps"), shown as the card's pill.</summary>
        public string OfferingTypeText { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string DateText { get; set; }
        public decimal MinPrice { get; set; }
        public int OptionCount { get; set; }
        public List<string> CampusNames { get; set; } = new List<string>();
    }

    #endregion

    #region Offering detail

    public class OfferingDetailInitializationBox
    {
        public NavigationBag Navigation { get; set; }
        public OfferingBag Offering { get; set; }
        public List<RegistrantOptionBag> People { get; set; } = new List<RegistrantOptionBag>();
        public bool CanAddFamilyMembers { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>A Rock RegistrationTemplate with its open instances (options) and fees (add-ons).</summary>
    public class OfferingBag
    {
        public string IdKey { get; set; }
        public string OfferingTypeText { get; set; }
        public string RegistrantTerm { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string DateText { get; set; }
        public List<VariantBag> Variants { get; set; } = new List<VariantBag>();
        public List<AddOnGroupBag> AddOnGroups { get; set; } = new List<AddOnGroupBag>();
    }

    /// <summary>A Rock RegistrationInstance.</summary>
    public class VariantBag
    {
        /// <summary>RegistrationInstance id.</summary>
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal BasePrice { get; set; }
        public int? CampusId { get; set; }
        public string CampusName { get; set; }
        /// <summary>Event dates, e.g. "Jul 5–9, 2027" (empty when unknown).</summary>
        public string SessionText { get; set; }
        /// <summary>"Register by Jun 30" (empty when the window has no end).</summary>
        public string DeadlineText { get; set; }
        public int EligibleGradeBands { get; set; }
        public int? SpotsRemaining { get; set; }
    }

    /// <summary>A Rock RegistrationTemplateFee.</summary>
    public class AddOnGroupBag
    {
        /// <summary>RegistrationTemplateFee id.</summary>
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsRequired { get; set; }
        /// <summary>Required single-item fee: always included, shown checked and disabled.</summary>
        public bool IsLocked { get; set; }
        public int SelectionMode { get; set; }
        public List<AddOnItemBag> Items { get; set; } = new List<AddOnItemBag>();
    }

    /// <summary>A Rock RegistrationTemplateFeeItem.</summary>
    public class AddOnItemBag
    {
        /// <summary>RegistrationTemplateFeeItem id.</summary>
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int EligibleGradeBands { get; set; }
    }

    public class RegistrantOptionBag
    {
        public int PersonAliasId { get; set; }
        public string FullName { get; set; }
        public int GradeBand { get; set; }
        public string GradeText { get; set; }
        public bool IsSelf { get; set; }
        public bool IsInCart { get; set; }
    }

    public class AddToCartRequestBag
    {
        public int PersonAliasId { get; set; }
        public int RegistrationInstanceId { get; set; }
        public List<int> FeeItemIds { get; set; } = new List<int>();
    }

    public class AddFamilyMemberRequestBag
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int GradeBand { get; set; }
    }

    #endregion

    #region Cart + checkout

    public class CartInitializationBox
    {
        public NavigationBag Navigation { get; set; }
        public PricedCartBag Cart { get; set; }
    }

    public class DiscountResultBag
    {
        public string ErrorMessage { get; set; }
        public PricedCartBag Cart { get; set; }
    }

    public class CheckoutInitializationBox
    {
        public NavigationBag Navigation { get; set; }
        public PricedCartBag Cart { get; set; }

        /// <summary>Rock-hosted payment control (when the gateway supports Obsidian).</summary>
        public GatewayControlBag GatewayControl { get; set; }

        /// <summary>Fallback: direct gotnpgateway/My Well tokenizer settings.</summary>
        public TokenizerSettingsBag Tokenizer { get; set; }

        public bool IsGatewayConfigured { get; set; }
        public string PayerName { get; set; }
        public string PayerEmail { get; set; }
        public List<string> Problems { get; set; } = new List<string>();
    }

    public class TokenizerSettingsBag
    {
        public string ScriptUrl { get; set; }
        public string GatewayUrl { get; set; }
        public string PublicKey { get; set; }
    }

    public class CheckoutRequestBag
    {
        public string GatewayToken { get; set; }
    }

    public class CheckoutResultBag
    {
        public bool Success { get; set; }
        public bool AlreadyProcessed { get; set; }
        public bool IsDeclined { get; set; }
        public string ErrorMessage { get; set; }
        public int? TransactionId { get; set; }
        public decimal Total { get; set; }
        /// <summary>One line per registration, e.g. "Riley Admin — Adventure Camp: Elementary Adventure · Jul 5–9, 2027".</summary>
        public List<string> ItemDescriptions { get; set; } = new List<string>();
    }

    #endregion
}
