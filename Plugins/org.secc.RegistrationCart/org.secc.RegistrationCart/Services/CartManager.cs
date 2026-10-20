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
            return CartGraph( OpenCarts() ).FirstOrDefault();
        }

        /// <summary>
        /// The id of the person's open cart, or null. Loads no entities, so a checkout can claim
        /// the cart first and then read its contents fresh.
        /// </summary>
        public int? GetOpenCartId()
        {
            return OpenCarts().Select( c => ( int? ) c.Id ).FirstOrDefault();
        }

        private IQueryable<Cart> OpenCarts()
        {
            var aliasIds = AliasIds();
            return new CartService( _rockContext ).Queryable()
                .Where( c => c.Status == CartStatus.Open && aliasIds.Contains( c.OwnerPersonAliasId ) )
                .OrderByDescending( c => c.Id );
        }

        /// <summary>
        /// The person's checkouts that took, or may have taken, a payment without finishing
        /// (<see cref="CheckoutRules.IsPendingCheckout"/>), newest first. The pages warn the payer
        /// not to pay again until the office has checked.
        /// </summary>
        public List<Cart> GetPendingCheckouts()
        {
            var aliasIds = AliasIds();
            return new CartService( _rockContext ).Queryable().AsNoTracking()
                .Where( c => aliasIds.Contains( c.OwnerPersonAliasId )
                    && ( c.Status == CartStatus.Processing || ( c.Status == CartStatus.CheckedOut && c.FinancialTransactionId == null ) ) )
                .OrderByDescending( c => c.Id )
                .ToList()
                .Where( c => CheckoutRules.IsPendingCheckout( c.Status, c.FinancialTransactionId.HasValue, c.LastCheckoutError ) )
                .ToList();
        }

        /// <summary>All the person's alias ids.</summary>
        private List<int> AliasIds()
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

            return aliasIds;
        }

        /// <summary>
        /// Why the camper can't be registered for the option again, or null: they're on a
        /// checkout whose payment hasn't been confirmed, or already registered for it (wait list
        /// included) while duplicate registrations are prevented (<see cref="CartSettings"/>).
        /// </summary>
        /// <param name="cartId">The cart asking, which doesn't count against itself.</param>
        public static string DuplicateRegistration( RockContext rockContext, int personId, int registrationInstanceId, string instanceName, string camperName, int cartId )
        {
            var pendingLines = new CartLineItemService( rockContext ).Queryable().AsNoTracking()
                .Where( l => l.RegistrationInstanceId == registrationInstanceId
                    && l.CartId != cartId
                    && l.CartRegistrant.PersonAlias.PersonId == personId
                    && ( l.Cart.Status == CartStatus.Processing || ( l.Cart.Status == CartStatus.CheckedOut && l.Cart.FinancialTransactionId == null ) ) )
                .Select( l => new { l.Cart.Status, l.Cart.FinancialTransactionId, l.Cart.LastCheckoutError } )
                .ToList();
            if ( pendingLines.Any( c => CheckoutRules.IsPendingCheckout( c.Status, c.FinancialTransactionId.HasValue, c.LastCheckoutError ) ) )
            {
                return $"{camperName} is on an earlier checkout for {instanceName} whose payment hasn't been confirmed. Please contact the office before registering them again.";
            }

            if ( CartSettings.PreventDuplicateRegistrations && new RegistrationRegistrantService( rockContext ).Queryable()
                .Any( r => r.Registration.RegistrationInstanceId == registrationInstanceId && !r.Registration.IsTemporary && r.PersonAlias.PersonId == personId ) )
            {
                return $"{camperName} is already registered for {instanceName}.";
            }

            return null;
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
                // Not the add-ons' fee items: one staff deleted would drop its add-on from the
                // join. Add-ons carry their snapshot name and price.
                .Include( c => c.LineItems.Select( l => l.AddOns ) )
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

        /// <summary>What adding or changing a cart line came to.</summary>
        public class SaveLineResult
        {
            /// <summary>The saved line, or null when refused.</summary>
            public CartLineItem Line { get; set; }

            public string ErrorMessage { get; set; }

            /// <summary>Only a new signature is missing (the option or answers changed from what was signed).</summary>
            public bool NeedsSignature { get; set; }

            /// <summary>
            /// Where the line goes (wait list or seat), when that was worked out. The page shows the
            /// form for that placement, so a camper who just got a seat sees the questions
            /// wait-listed registrants skip.
            /// </summary>
            public bool? IsWaitList { get; set; }
        }

        /// <summary>
        /// Adds a registrant-for-instance line with the chosen fee items, enforcing ownership,
        /// grade-band eligibility, uniqueness, capacity, Rock's fee rules and the template's form
        /// questions. Prices are snapshotted.
        /// </summary>
        public SaveLineResult AddLineItem( int personAliasId, int registrationInstanceId, IEnumerable<int> feeItemIds, Dictionary<Guid, object> fieldValues, string signatureData, Guid? discountCodeWorkflowGuid ) =>
            SaveLineItem( null, personAliasId, registrationInstanceId, feeItemIds, fieldValues, signatureData, discountCodeWorkflowGuid, save: true );

        /// <summary>
        /// Changes a line in the open cart before checkout: its option (within the same template),
        /// fee items and form answers, under the same rules as adding it. The camper can't change.
        /// Unchanged choices keep their snapshot prices. The signature carries over only while the
        /// option and answers are the ones it was signed for; the discount code workflow runs again
        /// only when they changed.
        /// </summary>
        public SaveLineResult UpdateLineItem( int lineItemId, int personAliasId, int registrationInstanceId, IEnumerable<int> feeItemIds, Dictionary<Guid, object> fieldValues, string signatureData, Guid? discountCodeWorkflowGuid ) =>
            SaveLineItem( lineItemId, personAliasId, registrationInstanceId, feeItemIds, fieldValues, signatureData, discountCodeWorkflowGuid, save: true );

        /// <summary>
        /// Everything adding or changing the line would check except the signature, without saving:
        /// the page asks before the family signs, so a signature isn't spent on answers the cart
        /// would refuse.
        /// </summary>
        public SaveLineResult CheckLineItem( int? lineItemId, int personAliasId, int registrationInstanceId, IEnumerable<int> feeItemIds, Dictionary<Guid, object> fieldValues ) =>
            SaveLineItem( lineItemId, personAliasId, registrationInstanceId, feeItemIds, fieldValues, null, null, save: false );

        /// <summary>A line in the person's open cart, or null.</summary>
        public CartLineItem GetOpenLine( int lineItemId ) =>
            GetOpenCart()?.LineItems.FirstOrDefault( l => l.Id == lineItemId );

        private SaveLineResult SaveLineItem( int? lineItemId, int personAliasId, int registrationInstanceId, IEnumerable<int> feeItemIds, Dictionary<Guid, object> fieldValues, string signatureData, Guid? discountCodeWorkflowGuid, bool save )
        {
            var result = new SaveLineResult();
            Cart cart;
            CartLineItem existing = null;
            if ( lineItemId.HasValue )
            {
                cart = GetOpenCart();
                existing = cart?.LineItems.FirstOrDefault( l => l.Id == lineItemId.Value );
                if ( existing == null )
                {
                    return Refuse( result, "That registration is no longer in your cart." );
                }
            }
            else
            {
                cart = save ? GetOrCreateOpenCart() : GetOpenCart();
            }

            var registrantPerson = ResolveRegisterablePerson( personAliasId );
            if ( registrantPerson == null )
            {
                return Refuse( result, "That person is not in your family." );
            }

            if ( existing != null && registrantPerson.PrimaryAliasId != existing.CartRegistrant.PersonAliasId )
            {
                return Refuse( result, "The person on a registration can't be changed. Remove it and add the other person instead." );
            }

            var catalog = new CatalogService( _rockContext );
            var option = catalog.GetOpenInstance( registrationInstanceId, out var template );
            if ( option == null )
            {
                return Refuse( result, "That option is no longer open for registration." );
            }

            if ( existing != null && template.Template.Id != existing.RegistrationTemplateId )
            {
                return Refuse( result, "A registration can only move to another option of the same registration." );
            }

            var band = GradeBands.ForPerson( registrantPerson );
            if ( !Eligibility.IsVariantEligible( new global::org.secc.RegistrationCart.Domain.Entities.OfferingVariant { EligibleGradeBands = option.Bands }, band ) )
            {
                return Refuse( result, $"{registrantPerson.FullName} ({GradeBands.Friendly( band )}) is not eligible for {option.Instance.Name}." );
            }

            // The camper's cart registrant, if they have one already; a new one is saved with the line.
            var registrant = existing?.CartRegistrant ?? cart?.Registrants.FirstOrDefault( r => r.PersonAliasId == registrantPerson.PrimaryAliasId );
            var camperName = registrant?.DisplayName ?? registrantPerson.FullName;
            var otherLineId = lineItemId ?? 0;
            if ( registrant != null && cart.LineItems.Any( l => l.CartRegistrantId == registrant.Id && l.RegistrationInstanceId == option.Instance.Id && l.Id != otherLineId ) )
            {
                return Refuse( result, $"{camperName} is already in your cart for {option.Instance.Name}." );
            }

            var duplicate = DuplicateRegistration( _rockContext, registrantPerson.Id, option.Instance.Id, option.Instance.Name, camperName, cart?.Id ?? 0 );
            if ( duplicate != null )
            {
                return Refuse( result, duplicate );
            }

            // Seat or wait list, counted the way checkout counts: this cart's lines for the option
            // in cart order (a new line last, an edited one keeps its place). A full camp takes the
            // camper on its wait list when Rock's template allows one.
            var sameOption = existing != null && existing.RegistrationInstanceId == option.Instance.Id;
            var probe = new CartLineItem { Id = existing?.Id ?? 0, RegistrationInstanceId = option.Instance.Id };
            var optionLines = cart?.LineItems.Where( l => l.RegistrationInstanceId == option.Instance.Id && l.Id != probe.Id ).ToList() ?? new List<CartLineItem>();
            var seatPlan = new SeatPlanner( _rockContext ).Plan( cart?.Id ?? 0, optionLines.Concat( new[] { probe } ) );
            if ( !seatPlan.TryGetValue( probe, out var place ) || place == SeatPlace.Refused )
            {
                return Refuse( result, $"{option.Instance.Name} is full." );
            }

            result.IsWaitList = place == SeatPlace.WaitList;

            // Locked fields keep the value on file: that's what the document, the discount code
            // workflow and checkout all see.
            var forms = new RegistrationForms( _rockContext );
            var groupId = catalog.GetLinkedGroupId( option.Instance.Id );
            var answers = forms.Normalize( template.Template, registrantPerson, groupId, RegistrationForms.TemplateAnswers( template.Template, fieldValues ) );
            var addOns = new List<CartLineItemAddOn>();
            var requested = new HashSet<int>( feeItemIds ?? Enumerable.Empty<int>() );

            // Items with a Maximum Usage in Rock: what's left for this camper, after the cart's
            // other campers for the option.
            var remaining = new FeeItemUsage( _rockContext ).Remaining( new[] { option.Instance }, cart?.Id )[option.Instance.Id];
            var otherLines = cart?.LineItems.Where( l => l.RegistrationInstanceId == option.Instance.Id && l.Id != otherLineId ).ToList() ?? new List<CartLineItem>();
            bool SoldOut( CatalogService.CatalogFeeItem item ) =>
                remaining.TryGetValue( item.Item.Id, out var left ) && left - otherLines.Sum( l => l.AddOns.Count( a => a.RegistrationTemplateFeeItemId == item.Item.Id ) ) <= 0;

            foreach ( var fee in template.Fees )
            {
                var eligibleItems = fee.Items.Where( i => Eligibility.IsAddOnEligible( new global::org.secc.RegistrationCart.Domain.Entities.AddOnItem { EligibleGradeBands = i.Bands }, band ) ).ToList();
                var availableItems = eligibleItems.Where( i => !SoldOut( i ) ).ToList();
                var chosen = fee.Items.Where( i => requested.Contains( i.Item.Id ) ).ToList();

                // Rock's required single-item fees are mandatory: always include them (while
                // there are any left).
                if ( fee.IsLocked && availableItems.Any() && chosen.Count == 0 )
                {
                    chosen = new List<CatalogService.CatalogFeeItem> { availableItems.First() };
                }

                // As Rock: a required fee whose items are all sold out stops being required.
                if ( fee.IsRequired && availableItems.Any() && chosen.Count == 0 )
                {
                    return Refuse( result, $"Please choose an option for \"{fee.Fee.Name}\"." );
                }

                if ( fee.Mode == SelectionMode.Single && chosen.Count > 1 )
                {
                    return Refuse( result, $"Only one option may be chosen for \"{fee.Fee.Name}\"." );
                }

                foreach ( var item in chosen )
                {
                    if ( !eligibleItems.Contains( item ) )
                    {
                        return Refuse( result, $"\"{item.Item.Name}\" is not available for {camperName}." );
                    }

                    if ( SoldOut( item ) )
                    {
                        return Refuse( result, $"\"{item.Item.Name}\" is sold out." );
                    }

                    // An item the line already had keeps the price it was added at.
                    var kept = existing?.AddOns.FirstOrDefault( a => a.RegistrationTemplateFeeItemId == item.Item.Id );
                    addOns.Add( new CartLineItemAddOn
                    {
                        RegistrationTemplateFeeItemId = item.Item.Id,
                        SnapshotName = kept?.SnapshotName ?? item.Item.Name.Truncate( 200 ),
                        SnapshotPrice = kept?.SnapshotPrice ?? item.Item.Cost
                    } );
                }
            }

            // The template's questions, for where the camper is placed: never trust the browser's
            // idea of which ones it had to ask.
            var answerProblems = forms.CheckAnswers( template.Template, registrantPerson, groupId, answers, place == SeatPlace.WaitList );
            if ( answerProblems.Any() )
            {
                return Refuse( result, string.Join( " ", answerProblems ) );
            }

            if ( !save )
            {
                return result;
            }

            // A template that requires its document signed inline: the line must carry a signature
            // made for this option and these answers (the line's earlier one carries over while
            // they still match).
            var signatures = new RegistrationSignatures( _rockContext );
            string signature = null;
            if ( signatures.RequiredDocument( template.Template ) != null )
            {
                signature = signatureData.IsNotNullOrWhiteSpace() ? signatureData : existing?.SignatureData;
                if ( !signatures.IsUsable( template.Template, registrantPerson, signature, option.Instance.Id, answers ) )
                {
                    result.NeedsSignature = true;
                    return Refuse( result, "Please sign the document before adding to the cart." );
                }
            }

            var answersChanged = existing == null || !sameOption
                || RegistrationForms.AnswersKey( template.Template, answers ) != RegistrationForms.AnswersKey( template.Template, RegistrationForms.FromJson( existing.FieldValuesJson ) );

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
            }

            var line = existing ?? new CartLineItem
            {
                CartId = cart.Id,
                CartRegistrant = registrant,
                RegistrationTemplateId = template.Template.Id
            };
            var leftInstanceId = existing != null && !sameOption ? existing.RegistrationInstanceId : ( int? ) null;
            line.SnapshotBasePrice = sameOption ? existing.SnapshotBasePrice : option.Price;
            line.IsWaitList = place == SeatPlace.WaitList;
            line.RegistrationInstanceId = option.Instance.Id;
            line.FieldValuesJson = RegistrationForms.ToJson( answers );
            line.SignatureData = signature;

            // The cart's other campers for the option, and for the option this line left, take
            // their places again (one behind it on the wait list may move up).
            ApplyPlan( seatPlan, optionLines );
            if ( leftInstanceId.HasValue )
            {
                Replace( cart, leftInstanceId.Value, line.Id );
            }

            if ( existing != null )
            {
                new CartLineItemAddOnService( _rockContext ).DeleteRange( existing.AddOns.ToList() );
            }

            foreach ( var addOn in addOns )
            {
                line.AddOns.Add( addOn );
            }

            // The discount code workflow (financial assistance) runs once the line is otherwise
            // valid, and again only when the option or answers changed. Its code is this line's
            // alone; cart codes never stack on it.
            if ( answersChanged )
            {
                var applied = DiscountCodeWorkflow.Run( discountCodeWorkflowGuid, option.Instance.Id, _person, registrantPerson, answers, line.SnapshotBasePrice + addOns.Sum( a => a.SnapshotPrice ) );
                line.TemplateDiscountCode = applied?.Code;
                line.TemplateDiscountAmount = applied?.Amount ?? 0m;
                line.TemplateDiscountPercentage = applied?.Percentage ?? 0m;
            }

            if ( existing == null )
            {
                new CartLineItemService( _rockContext ).Add( line );
            }

            _rockContext.SaveChanges();
            result.Line = line;
            return result;
        }

        private static SaveLineResult Refuse( SaveLineResult result, string message )
        {
            result.ErrorMessage = message;
            return result;
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
            var instanceId = line.RegistrationInstanceId;
            new CartLineItemService( _rockContext ).Delete( line );
            Replace( cart, instanceId, lineItemId );
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

        /// <summary>
        /// Places the cart's lines for <paramref name="instanceId"/> again, without the line
        /// <paramref name="leavingLineId"/> that left it (unsaved). Lines that would be refused
        /// keep their place; checkout reports them.
        /// </summary>
        private void Replace( Cart cart, int instanceId, int leavingLineId )
        {
            var lines = cart.LineItems.Where( l => l.RegistrationInstanceId == instanceId && l.Id != leavingLineId ).ToList();
            if ( lines.Any() )
            {
                ApplyPlan( new SeatPlanner( _rockContext ).Plan( cart.Id, lines ), lines );
            }
        }

        private static void ApplyPlan( Dictionary<CartLineItem, SeatPlace> plan, IEnumerable<CartLineItem> lines )
        {
            foreach ( var line in lines )
            {
                if ( plan.TryGetValue( line, out var place ) && place != SeatPlace.Refused )
                {
                    line.IsWaitList = place == SeatPlace.WaitList;
                }
            }
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

            // An offering code whose registration template was deleted applies to nothing.
            if ( !discount.IsValidOn( RockDateTime.Today ) || ( discount.Scope == DiscountScope.Offering && !discount.RegistrationTemplateId.HasValue ) )
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
