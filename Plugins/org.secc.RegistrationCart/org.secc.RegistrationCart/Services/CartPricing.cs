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
using System.Linq;
using org.secc.RegistrationCart.Domain.Pricing;
using DomainEntities = global::org.secc.RegistrationCart.Domain.Entities;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Adapts Rock cart entities to the pure, unit-tested org.secc.RegistrationCart.Domain pricing
    /// engine. The engine only reads snapshot values, so this is a straight projection.
    /// </summary>
    public static class CartPricing
    {
        private static readonly PricingEngine Engine = new PricingEngine();

        public static PricedCart Price( Model.Cart cart )
        {
            if ( cart == null )
            {
                return PricedCart.Empty;
            }

            var domainCart = new DomainEntities.Cart { Id = cart.Id };

            foreach ( var line in cart.LineItems.OrderBy( l => l.CartRegistrant?.LastName ).ThenBy( l => l.CartRegistrant?.FirstName ).ThenBy( l => l.Id ) )
            {
                var dl = new DomainEntities.CartLineItem
                {
                    Id = line.Id,
                    CartId = cart.Id,
                    RegistrantId = line.CartRegistrantId,
                    Registrant = new DomainEntities.Registrant
                    {
                        Id = line.CartRegistrantId,
                        FirstName = line.CartRegistrant?.FirstName ?? "",
                        LastName = line.CartRegistrant?.LastName ?? "",
                        GradeBand = line.CartRegistrant?.GradeBand ?? global::org.secc.RegistrationCart.Domain.GradeBand.None,
                        RockPersonAliasId = line.CartRegistrant?.PersonAliasId
                    },
                    OfferingId = line.RegistrationTemplateId,
                    Offering = new DomainEntities.Offering { Id = line.RegistrationTemplateId, Title = line.RegistrationTemplate?.Name ?? "" },
                    OfferingVariantId = line.RegistrationInstanceId,
                    OfferingVariant = new DomainEntities.OfferingVariant { Id = line.RegistrationInstanceId, Name = line.RegistrationInstance?.Name ?? "" },
                    SnapshotBasePrice = line.SnapshotBasePrice,
                    IsWaitList = line.IsWaitList,
                    TemplateDiscountCode = line.TemplateDiscountCode,
                    TemplateDiscountAmount = line.TemplateDiscountAmount,
                    TemplateDiscountPercentage = line.TemplateDiscountPercentage
                };

                foreach ( var addOn in line.AddOns )
                {
                    dl.AddOns.Add( new DomainEntities.CartLineItemAddOn
                    {
                        Id = addOn.Id,
                        AddOnItemId = addOn.RegistrationTemplateFeeItemId,
                        AddOnItem = new DomainEntities.AddOnItem { Id = addOn.RegistrationTemplateFeeItemId, Name = addOn.SnapshotName ?? addOn.RegistrationTemplateFeeItem?.Name ?? "" },
                        SnapshotPrice = addOn.SnapshotPrice,
                        DiscountApplies = addOn.DiscountApplies
                    } );
                }

                domainCart.LineItems.Add( dl );
            }

            foreach ( var d in cart.Discounts )
            {
                var code = d.DiscountCode;
                domainCart.Discounts.Add( new DomainEntities.CartDiscount
                {
                    Id = d.Id,
                    DiscountCodeId = d.DiscountCodeId,
                    DiscountCode = code == null ? null : new DomainEntities.DiscountCode
                    {
                        Id = code.Id,
                        Code = code.Code,
                        IsActive = code.IsActive,
                        ValidFrom = code.ValidFrom,
                        ValidTo = code.ValidTo
                    },
                    SnapshotValue = d.SnapshotValue,
                    SnapshotType = d.SnapshotType,
                    SnapshotScope = d.SnapshotScope,
                    SnapshotOfferingId = d.SnapshotRegistrationTemplateId
                } );
            }

            return Engine.Price( domainCart, Rock.RockDateTime.Today );
        }
    }
}
