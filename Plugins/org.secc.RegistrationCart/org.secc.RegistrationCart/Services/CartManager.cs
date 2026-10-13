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
using System.Data.Entity;
using System.Linq;
using org.secc.RegistrationCart.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Domain.Pricing;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// All cart mutations, scoped to one RockContext and one signed-in person. Every
    /// method re-validates ownership and eligibility server side; never trust the client.
    /// </summary>
    public class CartManager
    {
        private static readonly EligibilityService Eligibility = new EligibilityService();

        private readonly RockContext _rockContext;
        private readonly Person _person;

        public CartManager( RockContext rockContext, Person person )
        {
            _rockContext = rockContext ?? throw new ArgumentNullException( nameof( rockContext ) );
            _person = person ?? throw new ArgumentNullException( nameof( person ) );
        }

        #region Cart lookup

        /// <summary>The person's open cart with its full graph, or null.</summary>
        public Cart GetOpenCart()
        {
            // Query aliases explicitly: the current person may have been loaded by another
            // context, so its navigation collections can't be relied on.
            var aliasIds = new PersonAliasService( _rockContext ).Queryable()
                .Where( a => a.PersonId == _person.Id )
                .Select( a => a.Id )
                .ToList();
            if ( _person.PrimaryAliasId.HasValue && !aliasIds.Contains( _person.PrimaryAliasId.Value ) )
            {
                aliasIds.Add( _person.PrimaryAliasId.Value );
            }

            return CartGraph( new CartService( _rockContext ).Queryable() )
                .Where( c => c.Status == CartStatus.Open && aliasIds.Contains( c.OwnerPersonAliasId ) )
                .OrderByDescending( c => c.Id )
                .FirstOrDefault();
        }

        public Cart GetOrCreateOpenCart()
        {
            var cart = GetOpenCart();
            if ( cart != null )
            {
                return cart;
            }

            if ( !_person.PrimaryAliasId.HasValue )
            {
                throw new InvalidOperationException( "The current person has no primary alias." );
            }

            cart = new Cart { OwnerPersonAliasId = _person.PrimaryAliasId.Value, Status = CartStatus.Open };
            new CartService( _rockContext ).Add( cart );
            _rockContext.SaveChanges();
            return GetOpenCart();
        }

        public static IQueryable<Cart> CartGraph( IQueryable<Cart> query ) =>
            query
                .Include( c => c.Registrants )
                .Include( c => c.LineItems.Select( l => l.CartRegistrant ) )
                .Include( c => c.LineItems.Select( l => l.RegistrationTemplate ) )
                .Include( c => c.LineItems.Select( l => l.RegistrationInstance ) )
                .Include( c => c.LineItems.Select( l => l.AddOns.Select( a => a.RegistrationTemplateFeeItem ) ) )
                .Include( c => c.Discounts.Select( d => d.DiscountCode ) );

        public PricedCart Price( Cart cart ) => CartPricing.Price( cart );

        #endregion

        #region Family

        /// <summary>People the current person may register: themselves plus family members.</summary>
        public List<Person> GetRegisterablePeople()
        {
            var people = new PersonService( _rockContext )
                .GetFamilyMembers( _person.Id, includeSelf: true )
                .Select( gm => gm.Person )
                .Where( p => !p.IsDeceased )
                .Distinct()
                .ToList();

            if ( !people.Any( p => p.Id == _person.Id ) )
            {
                people.Add( _person );
            }

            return people
                .OrderBy( p => p.Id == _person.Id ? 0 : 1 )
                .ThenBy( p => p.AgeClassification == AgeClassification.Adult ? 0 : 1 )
                .ThenBy( p => p.GradeOffset ?? int.MaxValue )
                .ThenBy( p => p.FirstName )
                .ToList();
        }

        /// <summary>Is <paramref name="personAliasId"/> the current person or one of their family?</summary>
        public Person ResolveRegisterablePerson( int personAliasId )
        {
            var person = new PersonAliasService( _rockContext ).GetPerson( personAliasId );
            if ( person == null )
            {
                return null;
            }

            return GetRegisterablePeople().Any( p => p.Id == person.Id ) ? person : null;
        }

        /// <summary>
        /// Creates a new child in the current person's family. Returns the new person.
        /// </summary>
        public Person AddChildToFamily( string firstName, string lastName, GradeBand band, string errorPrefix, out string errorMessage )
        {
            errorMessage = null;
            firstName = ( firstName ?? "" ).Trim();
            lastName = ( lastName ?? "" ).Trim();
            if ( firstName.IsNullOrWhiteSpace() || lastName.IsNullOrWhiteSpace() )
            {
                errorMessage = "First and last name are required.";
                return null;
            }

            var family = _person.GetFamily( _rockContext );
            if ( family == null )
            {
                errorMessage = "You are not in a family in Rock, so a child cannot be added.";
                return null;
            }

            var childRole = GroupTypeCache.GetFamilyGroupType().Roles
                .FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() );
            if ( childRole == null )
            {
                errorMessage = "Rock's family child role could not be found.";
                return null;
            }

            var child = new Person
            {
                FirstName = firstName,
                LastName = lastName,
                RecordTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ),
                RecordStatusValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() ),
                ConnectionStatusValueId = _person.ConnectionStatusValueId,
                IsEmailActive = true,
                Gender = Gender.Unknown
            };
            if ( band != GradeBand.None )
            {
                child.GradeOffset = GradeBands.GradeOffsetFor( band );
            }

            PersonService.AddPersonToFamily( child, true, family.Id, childRole.Id, _rockContext );
            _rockContext.SaveChanges();
            return child;
        }

        #endregion

        #region Line items

        /// <summary>
        /// Adds a registrant-for-instance line with the chosen fee items, enforcing ownership,
        /// grade-band eligibility, uniqueness, capacity and Rock's fee rules. Prices are snapshotted.
        /// </summary>
        public CartLineItem AddLineItem( int personAliasId, int registrationInstanceId, IEnumerable<int> feeItemIds, out string errorMessage )
        {
            errorMessage = null;
            var cart = GetOrCreateOpenCart();

            var registrantPerson = ResolveRegisterablePerson( personAliasId );
            if ( registrantPerson == null )
            {
                errorMessage = "That person is not in your family.";
                return null;
            }

            var catalog = new CatalogService( _rockContext );
            var option = catalog.GetOpenInstance( registrationInstanceId, out var template );
            if ( option == null )
            {
                errorMessage = "That option is no longer open for registration.";
                return null;
            }

            var band = GradeBands.ForPerson( registrantPerson );
            if ( !Eligibility.IsVariantEligible( new global::org.secc.RegistrationCart.Domain.Entities.OfferingVariant { EligibleGradeBands = option.Bands }, band ) )
            {
                errorMessage = $"{registrantPerson.FullName} ({GradeBands.Friendly( band )}) is not eligible for {option.Instance.Name}.";
                return null;
            }

            var registrant = cart.Registrants.FirstOrDefault( r => r.PersonAliasId == registrantPerson.PrimaryAliasId );
            if ( registrant == null )
            {
                registrant = new CartRegistrant
                {
                    CartId = cart.Id,
                    PersonAliasId = registrantPerson.PrimaryAliasId.Value,
                    FirstName = registrantPerson.NickName.IsNotNullOrWhiteSpace() ? registrantPerson.NickName : registrantPerson.FirstName,
                    LastName = registrantPerson.LastName,
                    GradeBand = band
                };
                new CartRegistrantService( _rockContext ).Add( registrant );
                _rockContext.SaveChanges();
            }

            var lineService = new CartLineItemService( _rockContext );
            if ( lineService.Queryable().Any( l => l.CartRegistrantId == registrant.Id && l.RegistrationInstanceId == option.Instance.Id ) )
            {
                errorMessage = $"{registrant.DisplayName} is already registered for {option.Instance.Name}.";
                return null;
            }

            if ( option.SpotsRemaining.HasValue && option.SpotsRemaining.Value <= 0 )
            {
                errorMessage = $"{option.Instance.Name} is full.";
                return null;
            }

            var line = new CartLineItem
            {
                CartId = cart.Id,
                CartRegistrantId = registrant.Id,
                RegistrationTemplateId = template.Template.Id,
                RegistrationInstanceId = option.Instance.Id,
                SnapshotBasePrice = option.Price
            };

            var requested = new HashSet<int>( feeItemIds ?? Enumerable.Empty<int>() );
            foreach ( var fee in template.Fees )
            {
                var eligibleItems = fee.Items.Where( i => Eligibility.IsAddOnEligible( new global::org.secc.RegistrationCart.Domain.Entities.AddOnItem { EligibleGradeBands = i.Bands }, band ) ).ToList();
                var chosen = fee.Items.Where( i => requested.Contains( i.Item.Id ) ).ToList();

                // Rock's required single-item fees are mandatory: always include them.
                if ( fee.IsLocked && eligibleItems.Any() && chosen.Count == 0 )
                {
                    chosen = new List<CatalogService.CatalogFeeItem> { eligibleItems.First() };
                }

                if ( fee.IsRequired && eligibleItems.Any() && chosen.Count == 0 )
                {
                    errorMessage = $"Please choose an option for \"{fee.Fee.Name}\".";
                    return null;
                }

                if ( fee.Mode == SelectionMode.Single && chosen.Count > 1 )
                {
                    errorMessage = $"Only one option may be chosen for \"{fee.Fee.Name}\".";
                    return null;
                }

                foreach ( var item in chosen )
                {
                    if ( !eligibleItems.Contains( item ) )
                    {
                        errorMessage = $"\"{item.Item.Name}\" is not available for {registrant.DisplayName}.";
                        return null;
                    }

                    line.AddOns.Add( new CartLineItemAddOn
                    {
                        RegistrationTemplateFeeItemId = item.Item.Id,
                        SnapshotName = item.Item.Name.Truncate( 200 ),
                        SnapshotPrice = item.Item.Cost
                    } );
                }
            }

            lineService.Add( line );
            _rockContext.SaveChanges();
            return line;
        }

        public bool RemoveLineItem( int lineItemId )
        {
            var cart = GetOpenCart();
            var line = cart?.LineItems.FirstOrDefault( l => l.Id == lineItemId );
            if ( line == null )
            {
                return false;
            }

            var registrantId = line.CartRegistrantId;
            new CartLineItemService( _rockContext ).Delete( line );
            _rockContext.SaveChanges();

            if ( !new CartLineItemService( _rockContext ).Queryable().Any( l => l.CartRegistrantId == registrantId ) )
            {
                var registrantService = new CartRegistrantService( _rockContext );
                var registrant = registrantService.Get( registrantId );
                if ( registrant != null )
                {
                    registrantService.Delete( registrant );
                    _rockContext.SaveChanges();
                }
            }

            return true;
        }

        #endregion

        #region Discounts

        /// <summary>Returns null on success, otherwise a message for the shopper.</summary>
        public string ApplyDiscount( string code )
        {
            var cart = GetOrCreateOpenCart();
            var normalized = ( code ?? "" ).Trim().ToUpperInvariant();
            if ( normalized.IsNullOrWhiteSpace() )
            {
                return "Please enter a code.";
            }

            var discount = new DiscountCodeService( _rockContext ).Queryable()
                .FirstOrDefault( d => d.Code.ToUpper() == normalized );
            if ( discount == null )
            {
                return "That code isn't recognised.";
            }

            if ( !discount.IsValidOn( RockDateTime.Today ) )
            {
                return "That code isn't valid right now.";
            }

            if ( cart.Discounts.Any( d => d.DiscountCodeId == discount.Id ) )
            {
                return "That code is already applied.";
            }

            new CartDiscountService( _rockContext ).Add( new CartDiscount
            {
                CartId = cart.Id,
                DiscountCodeId = discount.Id,
                SnapshotValue = discount.Value,
                SnapshotType = discount.DiscountType,
                SnapshotScope = discount.Scope,
                SnapshotRegistrationTemplateId = discount.RegistrationTemplateId
            } );
            _rockContext.SaveChanges();
            return null;
        }

        public bool RemoveDiscount( int cartDiscountId )
        {
            var cart = GetOpenCart();
            var cd = cart?.Discounts.FirstOrDefault( d => d.Id == cartDiscountId );
            if ( cd == null )
            {
                return false;
            }

            new CartDiscountService( _rockContext ).Delete( cd );
            _rockContext.SaveChanges();
            return true;
        }

        #endregion


    }
}
