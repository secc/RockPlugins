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
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using org.secc.RegistrationCart.Model;
using Rock;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Tasks;
using Rock.Web.Cache;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Domain.Pricing;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Turns an open cart into money and registrations:
    ///   0. claims the cart (Open → Processing) with one conditional UPDATE, places seats and
    ///      checks everything that could stop the registrations saving (answers, signed
    ///      documents),
    ///   1. charges the gateway token once,
    ///   2. in one transaction: creates one Rock Registration (+ registrant, fees, registrant
    ///      answers and signed document) per line item, records one FinancialTransaction
    ///      (batched) with a detail line per registration, and closes the cart,
    ///   3. then, each step on its own: the signed documents' PDFs, the answers written to the
    ///      people, and what Rock does after a registration is saved.
    /// The cart row is the idempotency record: only the request that claims the cart can
    /// charge it, and the cart goes back to Open only when nothing was charged
    /// (<see cref="CheckoutRules.StatusAfterStop"/>). Only what must stand with the payment is
    /// in the transaction, so a failure after the charge can't leave money with nothing in Rock.
    /// </summary>
    public class CheckoutProcessor
    {
        public class Result
        {
            public bool Success { get; set; }
            public bool AlreadyProcessed { get; set; }
            public bool IsDeclined { get; set; }
            /// <summary>The cart no longer matched what the shopper was shown; it was not charged and is open again.</summary>
            public bool CartChanged { get; set; }
            public string ErrorMessage { get; set; }
            public int? TransactionId { get; set; }
            public List<int> RegistrationIds { get; set; } = new List<int>();
            public decimal Total { get; set; }
            public List<string> ItemDescriptions { get; set; } = new List<string>();
            /// <summary>Why the cart can't check out as it is (nothing was charged), by line.</summary>
            public List<CheckoutProblem> Problems { get; set; } = new List<CheckoutProblem>();
        }

        private readonly RockContext _rockContext;

        public CheckoutProcessor( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>Site root for links in the confirmation and notification emails (as Rock's RegistrationEntry passes it).</summary>
        public string AppRoot { get; set; }

        /// <summary>Theme root for the confirmation and notification emails.</summary>
        public string ThemeRoot { get; set; }

        /// <summary>
        /// Problems that stop this cart checking out. Empty = OK. Re-checks at checkout what held
        /// when each line was added: the instance and its template are active and shown in the
        /// cart, registration is open, the instance has an account, each line's extras are still
        /// offered, and its answers are complete for where it is placed (a camper who came off the
        /// wait list owes the answers wait-listed registrants skip) and are ones Rock will save.
        /// Place the cart's seats (<see cref="SeatPlanner"/>) first.
        /// </summary>
        public List<CheckoutProblem> Validate( Cart cart )
        {
            var problems = new List<CheckoutProblem>();
            if ( cart == null || cart.LineItems.Count == 0 )
            {
                problems.Add( new CheckoutProblem( "The cart is empty." ) );
                return problems;
            }

            var now = RockDateTime.Now;
            var instanceIds = cart.LineItems.Select( l => l.RegistrationInstanceId ).Distinct().ToList();
            var priced = CartPricing.Price( cart );

            var instances = new RegistrationInstanceService( _rockContext ).Queryable()
                .Include( i => i.RegistrationTemplate )
                .Where( i => instanceIds.Contains( i.Id ) )
                .ToDictionary( i => i.Id );
            var templateIds = instances.Values.Select( i => i.RegistrationTemplateId ).Distinct().ToList();
            var templateVisibility = new TemplateSettingService( _rockContext ).Queryable()
                .Where( s => templateIds.Contains( s.RegistrationTemplateId ) )
                .ToDictionary( s => s.RegistrationTemplateId, s => s.IsVisible );
            var hiddenTemplateIds = templateIds
                .Where( id => !( templateVisibility.TryGetValue( id, out var isVisible ) ? isVisible : CartSettings.DefaultVisibility ) )
                .ToList();
            var hiddenInstanceIds = new InstanceSettingService( _rockContext ).Queryable()
                .Where( s => instanceIds.Contains( s.RegistrationInstanceId ) && !s.IsVisible )
                .Select( s => s.RegistrationInstanceId )
                .ToList();

            foreach ( var instanceId in instanceIds )
            {
                var lineIds = cart.LineItems.Where( l => l.RegistrationInstanceId == instanceId ).Select( l => l.Id ).ToList();
                string problem = null;
                if ( !instances.TryGetValue( instanceId, out var instance ) )
                {
                    var name = cart.LineItems.First( l => l.RegistrationInstanceId == instanceId ).RegistrationInstance?.Name;
                    problem = $"\"{name}\" no longer exists in Rock.";
                }
                else if ( !instance.IsActive || !instance.RegistrationTemplate.IsActive )
                {
                    problem = $"\"{instance.Name}\" is no longer active.";
                }
                else if ( hiddenTemplateIds.Contains( instance.RegistrationTemplateId ) || hiddenInstanceIds.Contains( instance.Id ) )
                {
                    problem = $"\"{instance.Name}\" is no longer offered here.";
                }
                else if ( !CheckoutRules.IsWindowOpen( instance.StartDateTime, instance.EndDateTime, now ) )
                {
                    problem = instance.StartDateTime > now
                        ? $"Registration for \"{instance.Name}\" hasn't opened yet."
                        : $"Registration for \"{instance.Name}\" has closed.";
                }
                else if ( instance.AccountId == null && lineIds.Any( id => priced.Lines.Any( l => l.LineItemId == id && l.LineTotal - priced.DiscountFor( id ) > 0m ) ) )
                {
                    // Only something to pay needs an account (free events often have none).
                    problem = $"Registration instance \"{instance.Name}\" has no financial account. An administrator must set one in Rock.";
                }

                if ( problem != null )
                {
                    problems.Add( new CheckoutProblem( problem, lineIds ) );
                }
            }

            // Extras staff deleted or turned off since they were added: the line must be edited.
            var feeItemIds = cart.LineItems.SelectMany( l => l.AddOns ).Select( a => a.RegistrationTemplateFeeItemId ).Distinct().ToList();
            var offeredFeeItemIds = new RegistrationTemplateFeeItemService( _rockContext ).Queryable()
                .Where( i => feeItemIds.Contains( i.Id ) && i.IsActive && i.RegistrationTemplateFee.IsActive )
                .Select( i => i.Id )
                .ToList();
            foreach ( var line in cart.LineItems )
            {
                foreach ( var addOn in line.AddOns.Where( a => !offeredFeeItemIds.Contains( a.RegistrationTemplateFeeItemId ) ) )
                {
                    problems.Add( new CheckoutProblem( $"\"{addOn.SnapshotName}\" is no longer offered for {line.CartRegistrant?.DisplayName ?? "a camper"}. Please edit the registration.", new[] { line.Id } ) );
                }
            }

            // Extras with a Maximum Usage in Rock: the cart's campers can't take more than are left.
            var feeItemsRemaining = new FeeItemUsage( _rockContext ).Remaining( instances.Values, cart.Id );
            foreach ( var instanceLines in cart.LineItems.Where( l => feeItemsRemaining.ContainsKey( l.RegistrationInstanceId ) ).GroupBy( l => l.RegistrationInstanceId ) )
            {
                foreach ( var item in feeItemsRemaining[instanceLines.Key] )
                {
                    var holding = instanceLines.Where( l => l.AddOns.Any( a => a.RegistrationTemplateFeeItemId == item.Key ) ).ToList();
                    if ( holding.Count <= item.Value )
                    {
                        continue;
                    }

                    var name = holding[0].AddOns.First( a => a.RegistrationTemplateFeeItemId == item.Key ).SnapshotName;
                    problems.Add( new CheckoutProblem( item.Value == 0
                        ? $"\"{name}\" is sold out. Please edit the registration."
                        : $"Only {item.Value} \"{name}\" left, but your cart has {holding.Count}. Please edit a registration.",
                        holding.Select( l => l.Id ) ) );
                }
            }

            // Each camper's answers, for where they are placed now, and the document they signed
            // for them.
            var signatures = new RegistrationSignatures( _rockContext );
            var forms = new RegistrationForms( _rockContext );
            var catalog = new CatalogService( _rockContext );
            var personAliasService = new PersonAliasService( _rockContext );
            foreach ( var line in cart.LineItems )
            {
                if ( !instances.TryGetValue( line.RegistrationInstanceId, out var instance ) || line.CartRegistrant == null )
                {
                    continue;
                }

                var person = personAliasService.GetPerson( line.CartRegistrant.PersonAliasId );
                var duplicate = person != null ? CartManager.DuplicateRegistration( _rockContext, person.Id, instance.Id, instance.Name, line.CartRegistrant.DisplayName, cart.Id ) : null;
                if ( duplicate != null )
                {
                    problems.Add( new CheckoutProblem( duplicate, new[] { line.Id } ) );
                    continue;
                }

                var answerProblems = forms.CheckAnswers( instance.RegistrationTemplate, person, catalog.GetLinkedGroupId( instance.Id ), RegistrationForms.FromJson( line.FieldValuesJson ), line.IsWaitList );
                if ( answerProblems.Any() )
                {
                    problems.Add( new CheckoutProblem( $"Please finish {Possessive( line.CartRegistrant.FirstName )} form for \"{instance.Name}\": {string.Join( " ", answerProblems )}", new[] { line.Id } ) );
                }
                else if ( signatures.RequiredDocument( instance.RegistrationTemplate ) != null
                    && !signatures.IsUsable( instance.RegistrationTemplate, person, line.SignatureData, instance.Id, RegistrationForms.FromJson( line.FieldValuesJson ) ) )
                {
                    problems.Add( new CheckoutProblem( $"{Possessive( line.CartRegistrant.FirstName )} document for \"{instance.Name}\" needs to be signed again. Please edit the registration.", new[] { line.Id } ) );
                }
            }

            return problems;
        }

        /// <summary>
        /// Checks out the cart with id <paramref name="cartId"/>. The cart must not already be
        /// loaded in this processor's RockContext: it is read only after it has been claimed, so
        /// what gets charged and registered is what the cart held at that moment.
        /// </summary>
        /// <param name="expectedTotal">The total the shopper was shown. If the claimed cart prices
        /// to anything else, nothing is charged and the cart opens again.</param>
        public Result Process( int cartId, decimal expectedTotal, Person payer, FinancialGateway financialGateway, string gatewayToken, string batchNamePrefix )
        {
            if ( _rockContext.ChangeTracker.Entries<Cart>().Any( e => e.Entity.Id == cartId ) )
            {
                throw new InvalidOperationException( "The cart must be read after it is claimed, but it is already loaded in this RockContext." );
            }

            var result = new Result();
            var claimed = false;
            Cart cart;
            PricedCart priced;
            IGatewayComponent gateway = null;
            ReferencePaymentInfo paymentInfo = null;
            Dictionary<int, RegistrationSignatures.PreparedDocument> documents;

            // Nothing below can have charged the card until the gateway call, so any failure
            // here hands a claimed cart back.
            try
            {
                // Lock the cart's instances while claiming and validating, so two checkouts
                // can't both count the same last seat. The lock is short: once this cart is
                // Processing, every later check counts its lines as taken, so it isn't held
                // through the gateway call.
                var lockedInstanceIds = new CartLineItemService( _rockContext ).Queryable()
                    .Where( l => l.CartId == cartId )
                    .Select( l => l.RegistrationInstanceId )
                    .Distinct()
                    .ToList();

                using ( var seatLock = SeatLock.Acquire( _rockContext, lockedInstanceIds ) )
                {
                    if ( seatLock == null )
                    {
                        result.ErrorMessage = "Checkout is busy right now. Please try again in a moment.";
                        return result;
                    }

                    // ---- 0. Claim ----------------------------------------------------
                    // Two requests (two tabs, a retry after a timeout) can both get here with
                    // the same open cart. Only the one that moves it Open → Processing may go
                    // on to charge.
                    if ( !TryClaim( cartId ) )
                    {
                        return ClaimLost( cartId );
                    }

                    claimed = true;
                    cart = CartManager.CartGraph( new CartService( _rockContext ).Queryable() ).FirstOrDefault( c => c.Id == cartId );
                    if ( cart == null )
                    {
                        result.ErrorMessage = CartUnavailableMessage;
                        return result;
                    }

                    if ( !cart.LineItems.Any() )
                    {
                        result.CartChanged = true;
                        return Stop( cart, ChargeOutcome.NotAttempted, result, "Your cart is empty." );
                    }

                    // A line for another instance was added after the lock was taken.
                    if ( cart.LineItems.Any( l => !lockedInstanceIds.Contains( l.RegistrationInstanceId ) ) )
                    {
                        result.CartChanged = true;
                        return Stop( cart, ChargeOutcome.NotAttempted, result, "Your cart changed while checking out. Please review it and try again." );
                    }

                    // Seats first: where a camper is placed decides which answers they owe.
                    var seats = new SeatPlanner( _rockContext ).Place( cart );
                    var problems = seats.Problems.Concat( Validate( cart ) ).ToList();
                    if ( problems.Any() )
                    {
                        // Stop saves the new placement; the page shows the cart as it is now.
                        result.Problems = problems;
                        result.CartChanged = seats.Changes.Any();
                        return Stop( cart, ChargeOutcome.NotAttempted, result, string.Join( " ", problems.Select( p => p.Message ).Distinct() ) );
                    }

                    // A camper moved onto or off the wait list, so the total changed: save the new
                    // placement (Stop saves) and have the shopper confirm the new total.
                    if ( seats.Changes.Any() )
                    {
                        result.CartChanged = true;
                        return Stop( cart, ChargeOutcome.NotAttempted, result,
                            string.Join( " ", seats.Changes ) + $" Your total is now {CartPricing.Price( cart ).Total.FormatAsCurrency()}. Please review it and pay again." );
                    }
                }

                priced = CartPricing.Price( cart );
                result.Total = priced.Total;

                // Charge only the amount the shopper agreed to. The cart can change after the
                // page loaded (edits in another tab, a discount code that has since expired).
                if ( !CheckoutRules.TotalMatches( expectedTotal, priced.Total ) )
                {
                    result.CartChanged = true;
                    return Stop( cart, ChargeOutcome.NotAttempted, result,
                        $"Your cart changed since this page loaded. The total is now {priced.Total.FormatAsCurrency()}. Please review it and try again.",
                        $"Cart changed: shopper was shown {expectedTotal:0.00}, cart priced to {priced.Total:0.00}." );
                }

                // The signed documents, rendered and checked now: once the card is charged the
                // registrations must save, so nothing that can fail (the document's Lava, a value
                // Rock refuses) is left until after the charge.
                documents = PrepareDocuments( cart, payer );
                var documentProblems = documents
                    .Where( d => d.Value?.Error != null )
                    .Select( d => new CheckoutProblem( $"{Possessive( cart.LineItems.First( l => l.Id == d.Key ).CartRegistrant?.FirstName )} signed document: {d.Value.Error} Please edit the registration.", new[] { d.Key } ) )
                    .ToList();
                if ( documentProblems.Any() )
                {
                    result.Problems = documentProblems;
                    return Stop( cart, ChargeOutcome.NotAttempted, result, string.Join( " ", documentProblems.Select( p => p.Message ) ) );
                }

                var dateTexts = new CatalogService( _rockContext ).GetEventDateTexts( cart.LineItems.Select( l => l.RegistrationInstanceId ) );
                var lineInstance = cart.LineItems.ToDictionary( l => l.Id, l => l.RegistrationInstanceId );
                result.ItemDescriptions = priced.Lines.Select( l =>
                {
                    var date = lineInstance.TryGetValue( l.LineItemId, out var iid ) && dateTexts.TryGetValue( iid, out var dt ) && dt.IsNotNullOrWhiteSpace() ? " · " + dt : "";
                    return $"{l.RegistrantName} — {l.OfferingTitle}: {l.VariantName}{date}{( l.IsWaitList ? " (wait list)" : "" )}";
                } ).ToList();

                if ( priced.Total > 0m )
                {
                    if ( financialGateway == null )
                    {
                        return Stop( cart, ChargeOutcome.NotAttempted, result, "No payment gateway is configured on the checkout block." );
                    }

                    if ( gatewayToken.IsNullOrWhiteSpace() )
                    {
                        return Stop( cart, ChargeOutcome.NotAttempted, result, "The payment form did not return a payment token. Please try again." );
                    }

                    gateway = financialGateway.GetGatewayComponent() as IGatewayComponent;
                    if ( gateway == null )
                    {
                        return Stop( cart, ChargeOutcome.NotAttempted, result, "The configured payment gateway is not available." );
                    }

                    paymentInfo = new ReferencePaymentInfo
                    {
                        ReferenceNumber = gatewayToken,
                        Amount = priced.Total,
                        Description = Summary( priced ),
                        FirstName = payer.FirstName,
                        LastName = payer.LastName,
                        Email = payer.Email,
                        TransactionTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_EVENT_REGISTRATION.AsGuid() )
                    };
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( new Exception( $"Registration Cart: checkout of cart {cartId} failed before the payment was taken.", ex ) );
                if ( claimed )
                {
                    StopOnFreshContext( cartId, ChargeOutcome.NotAttempted, "Checkout failed before payment: " + ex.Message );
                }

                result.ErrorMessage = "Something went wrong. You have not been charged. Please try again.";
                return result;
            }

            // ---- 1. Charge -------------------------------------------------------
            FinancialTransaction transaction = null;
            if ( priced.Total > 0m )
            {
                string errorMessage;
                try
                {
                    transaction = Charge( gateway, financialGateway, paymentInfo, out errorMessage );
                }
                catch ( Exception ex )
                {
                    // No answer from the gateway, so the card may have been charged. The cart
                    // stays claimed so a retry can't charge it again before someone checks.
                    ExceptionLogService.LogException( new Exception( $"Registration Cart: the gateway charge for cart {cart.Id} failed without an answer; the payment outcome is unknown.", ex ) );
                    return Stop( cart, ChargeOutcome.Unknown, result,
                        "We couldn't confirm whether your payment went through. Please contact the office before trying again.",
                        "Payment outcome unknown (gateway error): " + ex.Message );
                }

                if ( transaction == null )
                {
                    result.IsDeclined = true;
                    return Stop( cart, ChargeOutcome.Declined, result,
                        errorMessage.IsNullOrWhiteSpace() ? "Payment was declined. Please try a different card." : errorMessage );
                }
            }

            // ---- 2 + 3. Registrations, transaction, close cart ---------------------
            // Once money has been taken the cart must close whatever happens: a retry must
            // never charge twice.
            try
            {
                _rockContext.WrapTransaction( () =>
                {
                    var registrationIds = CreateRegistrations( cart, payer, priced, documents );
                    result.RegistrationIds = registrationIds.Values.ToList();

                    if ( transaction != null )
                    {
                        SaveTransaction( transaction, cart, payer, priced, registrationIds, financialGateway, gateway, paymentInfo, batchNamePrefix );
                        cart.FinancialTransactionId = transaction.Id;
                        result.TransactionId = transaction.Id;
                    }

                    cart.Status = CartStatus.CheckedOut;
                    cart.CheckedOutDateTime = RockDateTime.Now;
                    cart.LastCheckoutError = null;
                    _rockContext.SaveChanges();
                } );

                result.Success = true;
            }
            catch ( Exception ex )
            {
                var outcome = transaction != null ? ChargeOutcome.Charged : ChargeOutcome.NotAttempted;
                ExceptionLogService.LogException( new Exception( transaction != null
                    ? $"Registration Cart: payment {transaction.TransactionCode} succeeded for cart {cart.Id} but registration creation failed."
                    : $"Registration Cart: registration creation failed for cart {cart.Id}; nothing was charged.", ex ) );

                // A charged cart closes with the error kept so support can reconcile; a cart that
                // cost nothing opens again.
                StopOnFreshContext( cart.Id, outcome, ( transaction != null
                    ? "Payment " + transaction.TransactionCode + " succeeded but registrations could not be created: "
                    : "Registrations could not be created (nothing was charged): " ) + ex.Message );

                result.ErrorMessage = transaction != null
                    ? PaidButNotRegisteredMessage
                    : "We could not create your registrations. You have not been charged. Please try again or contact the office.";
                return result;
            }

            // ---- 4. Documents' PDFs, the people's answers, what Rock does after saving ----
            var lines = cart.LineItems
                .Where( l => l.RegistrationId.HasValue )
                .ToDictionary( l => l.RegistrationId.Value );
            RunPostSave( result.RegistrationIds, payer, lines );

            return result;
        }

        /// <summary>
        /// Once the registrations are committed, for each one: makes its signed documents' PDFs
        /// (and sends the completion email that carries them), writes the person's answers, then
        /// does what Rock's own RegistrationEntry block does after saving a new registration:
        /// registrar history and notes, places registrants (not wait-listed) in the instance's
        /// linked group, queues the confirmation email and the staff notification, and launches
        /// the template's registrant workflow and the template and instance registration
        /// workflows. Each step runs on its own context and a failure is logged, never thrown: the
        /// registrations and the payment stand, and one step failing doesn't skip the others.
        /// </summary>
        private void RunPostSave( IEnumerable<int> registrationIds, Person payer, IDictionary<int, CartLineItem> linesByRegistration )
        {
            foreach ( var registrationId in registrationIds )
            {
                var line = linesByRegistration.GetValueOrNull( registrationId );
                var answers = RegistrationForms.FromJson( line?.FieldValuesJson );

                // The documents were saved with the registration; the maintenance job retries a PDF that fails here.
                PostSaveStep( registrationId, "the signed document's PDF", ( rockContext, registration, settings ) =>
                {
                    var signatures = new RegistrationSignatures( rockContext );
                    foreach ( var documentId in registration.Registrants.Where( r => r.SignatureDocumentId.HasValue ).Select( r => r.SignatureDocumentId.Value ).Distinct().ToList() )
                    {
                        signatures.Complete( documentId );
                    }
                } );

                // The answers stay on the cart line, so support can apply them by hand if this fails.
                PostSaveStep( registrationId, "writing the answers to the person", ( rockContext, registration, settings ) =>
                {
                    var forms = new RegistrationForms( rockContext );
                    foreach ( var registrant in registration.Registrants.Where( r => r.PersonAlias?.Person != null ) )
                    {
                        forms.ApplyToPerson( registration.RegistrationInstance.RegistrationTemplate, registrant.PersonAlias.Person, answers );
                    }
                } );

                PostSaveStep( registrationId, "the registrar's notes and history", ( rockContext, registration, settings ) =>
                    registration.SavePersonNotesAndHistory( payer, payer.PrimaryAliasId, new List<int>() ) );

                PostSaveStep( registrationId, "placing the registrants in the group", ( rockContext, registration, settings ) =>
                    AddRegistrantsToGroup( rockContext, settings, registration, answers ) );

                PostSaveStep( registrationId, "the confirmation email", ( rockContext, registration, settings ) =>
                    new ProcessSendRegistrationConfirmation.Message { RegistrationId = registration.Id, AppRoot = AppRoot, ThemeRoot = ThemeRoot }.Send() );

                PostSaveStep( registrationId, "the staff notification", ( rockContext, registration, settings ) =>
                    new ProcessSendRegistrationNotification.Message { RegistrationId = registration.Id, AppRoot = AppRoot, ThemeRoot = ThemeRoot }.Send() );

                PostSaveStep( registrationId, "the registrant workflow", ( rockContext, registration, settings ) =>
                {
                    foreach ( var registrant in registration.Registrants.Where( r => r.PersonAlias?.Person != null ) )
                    {
                        var parameters = new Dictionary<string, string>
                        {
                            { "RegistrationId", registration.Id.ToString() },
                            { "RegistrationRegistrantId", registrant.Id.ToString() }
                        };
                        registrant.LaunchWorkflow( settings.RegistrantWorkflowTypeId, registration.ToString(), parameters, null );
                    }
                } );

                // Each registration workflow on its own (e.g. financial assistance review), so one failing doesn't stop the next.
                PostSaveStep( registrationId, "the registration workflows", ( rockContext, registration, settings ) =>
                {
                    foreach ( var workflowTypeId in settings.WorkflowTypeIds )
                    {
                        try
                        {
                            registration.LaunchWorkflow( workflowTypeId, registration.ToString(), null, null );
                        }
                        catch ( Exception ex )
                        {
                            ExceptionLogService.LogException( new Exception( $"Registration Cart: registration workflow type {workflowTypeId} failed to launch for registration {registration.Id}.", ex ) );
                        }
                    }
                } );
            }
        }

        /// <summary>
        /// One post-save step for one registration, read fresh on its own context so a failure
        /// (logged with the registration) can't stop the steps after it or leave half-saved
        /// changes for them to trip over.
        /// </summary>
        private static void PostSaveStep( int registrationId, string step, Action<RockContext, Registration, RegistrationSettings> action )
        {
            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var registration = new RegistrationService( rockContext ).Queryable()
                        .Include( r => r.RegistrationInstance.RegistrationTemplate )
                        .Include( r => r.Registrants.Select( rr => rr.PersonAlias.Person ) )
                        .FirstOrDefault( r => r.Id == registrationId );
                    if ( registration == null )
                    {
                        return;
                    }

                    action( rockContext, registration, new RegistrationSettings( registration.RegistrationInstance.RegistrationTemplate, registration.RegistrationInstance ) );
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( new Exception( $"Registration Cart: {step} failed for registration {registrationId}. The registration and payment stand.", ex ) );
            }
        }

        /// <summary>
        /// Puts the registration's registrants who aren't on the wait list into its group, the way
        /// Rock's RegistrationEntry does: an existing member gets the template's member status,
        /// a new one also the template's role (or the group type's default role).
        /// </summary>
        private static void AddRegistrantsToGroup( RockContext rockContext, RegistrationSettings settings, Registration registration, Dictionary<Guid, object> answers )
        {
            if ( !registration.GroupId.HasValue )
            {
                return;
            }

            var group = new GroupService( rockContext ).Get( registration.GroupId.Value );
            if ( group == null )
            {
                return;
            }

            var groupMemberService = new GroupMemberService( rockContext );
            foreach ( var registrant in registration.Registrants.Where( r => !r.OnWaitList && r.PersonAlias != null ) )
            {
                var personId = registrant.PersonAlias.PersonId;
                var groupMember = group.Members.FirstOrDefault( m => m.PersonId == personId );
                if ( groupMember == null )
                {
                    groupMember = new GroupMember
                    {
                        GroupId = group.Id,
                        PersonId = personId,
                        GroupMemberStatus = settings.GroupMemberStatus,
                        GroupRoleId = settings.GroupTypeId == group.GroupTypeId && settings.GroupMemberRoleId.HasValue
                            ? settings.GroupMemberRoleId.Value
                            : group.GroupType.DefaultGroupRoleId ?? group.GroupType.Roles.Select( r => r.Id ).FirstOrDefault()
                    };
                    groupMemberService.Add( groupMember );
                }
                else
                {
                    groupMember.GroupMemberStatus = settings.GroupMemberStatus;
                }

                rockContext.SaveChanges();
                registrant.GroupMemberId = groupMember.Id;
                rockContext.SaveChanges();

                // The template's group member attribute answers.
                new RegistrationForms( rockContext ).ApplyToGroupMember( registration.RegistrationInstance.RegistrationTemplate, groupMember, answers );
            }
        }

        private static string Possessive( string name ) =>
            name.IsNullOrWhiteSpace() ? "the camper's" : name.EndsWith( "s" ) ? name + "'" : name + "'s";

        private const string PaidButNotRegisteredMessage = "Your payment was received but we could not finish creating the registrations. Please contact the office — do not pay again.";
        private const string CartUnavailableMessage = "Your cart is no longer available. Please reload the page.";

        /// <summary>
        /// Moves the cart Open → Processing in one conditional UPDATE. Only the request whose
        /// UPDATE changes the row gets true; a concurrent one sees zero rows affected.
        /// </summary>
        private bool TryClaim( int cartId )
        {
            // Set Value explicitly: new SqlParameter( name, 0 ) binds the constant 0 to the
            // SqlDbType overload, leaving the parameter typed but without a value.
            var rowsAffected = _rockContext.Database.ExecuteSqlCommand(
                "UPDATE [_org_secc_RegistrationCart_Cart] SET [Status] = @Processing, [ModifiedDateTime] = @Now WHERE [Id] = @CartId AND [Status] = @Open",
                new SqlParameter( "@Processing", SqlDbType.Int ) { Value = ( int ) CartStatus.Processing },
                new SqlParameter( "@Now", SqlDbType.DateTime ) { Value = RockDateTime.Now },
                new SqlParameter( "@CartId", SqlDbType.Int ) { Value = cartId },
                new SqlParameter( "@Open", SqlDbType.Int ) { Value = ( int ) CartStatus.Open } );

            return rowsAffected == 1;
        }

        /// <summary>Another request claimed the cart first: report what became of it.</summary>
        private Result ClaimLost( int cartId )
        {
            var result = new Result { AlreadyProcessed = true };
            var state = new CartService( _rockContext ).Queryable().AsNoTracking()
                .Where( c => c.Id == cartId )
                .Select( c => new
                {
                    c.Status,
                    c.FinancialTransactionId,
                    c.LastCheckoutError,
                    Total = c.FinancialTransaction.TransactionDetails.Sum( d => ( decimal? ) d.Amount )
                } )
                .FirstOrDefault();

            switch ( state?.Status )
            {
                case CartStatus.CheckedOut:
                    result.Success = state.FinancialTransactionId.HasValue || state.LastCheckoutError.IsNullOrWhiteSpace();
                    result.TransactionId = state.FinancialTransactionId;
                    result.Total = state.Total ?? 0m;
                    result.ErrorMessage = result.Success ? null : PaidButNotRegisteredMessage;
                    break;

                case CartStatus.Processing:
                    result.ErrorMessage = "This checkout is already being processed in another window. Please wait a minute before trying again, and contact the office if you're unsure whether it went through.";
                    break;

                case CartStatus.Open:
                    // Claimed and handed back between our UPDATE and this read.
                    result.AlreadyProcessed = false;
                    result.ErrorMessage = "Your cart was busy. Please try again.";
                    break;

                default:
                    result.AlreadyProcessed = false;
                    result.ErrorMessage = CartUnavailableMessage;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Ends a claimed checkout that did not finish: records the error and moves the cart to
        /// <see cref="CheckoutRules.StatusAfterStop"/> for <paramref name="outcome"/>.
        /// </summary>
        /// <param name="message">Shown to the shopper.</param>
        /// <param name="detail">Kept on the cart for support; defaults to <paramref name="message"/>.</param>
        private Result Stop( Cart cart, ChargeOutcome outcome, Result result, string message, string detail = null )
        {
            cart.Status = CheckoutRules.StatusAfterStop( outcome );
            cart.LastCheckoutError = ( detail ?? message ).Truncate( 2000 );
            _rockContext.SaveChanges();

            result.ErrorMessage = message;
            return result;
        }

        /// <summary>
        /// <see cref="Stop"/> on a fresh RockContext, for failures that leave this processor's
        /// context holding half-saved changes.
        /// </summary>
        private static void StopOnFreshContext( int cartId, ChargeOutcome outcome, string detail )
        {
            using ( var fresh = new RockContext() )
            {
                var cart = new CartService( fresh ).Get( cartId );
                if ( cart == null )
                {
                    return;
                }

                cart.Status = CheckoutRules.StatusAfterStop( outcome );
                if ( cart.Status == CartStatus.CheckedOut )
                {
                    cart.CheckedOutDateTime = RockDateTime.Now;
                }

                cart.LastCheckoutError = detail.Truncate( 2000 );
                fresh.SaveChanges();
            }
        }

        private static string Summary( PricedCart priced ) =>
            $"Registration Cart registration — {priced.Lines.Count} item{( priced.Lines.Count == 1 ? "" : "s" )}";

        private static FinancialTransaction Charge( IGatewayComponent gateway, FinancialGateway financialGateway, ReferencePaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = null;

            // Mirrors Rock's own RegistrationEntry block: Obsidian-hosted gateways want a
            // customer vault record before a tokenized charge. CreateCustomerAccount is a
            // Rock-internal API (RK1000) with no public equivalent.
            if ( gateway is IObsidianHostedGatewayComponent obsidianGateway )
            {
                if ( paymentInfo.GatewayPersonIdentifier.IsNullOrWhiteSpace() )
                {
#pragma warning disable RK1000
                    var customerToken = obsidianGateway.CreateCustomerAccount( financialGateway, paymentInfo, out errorMessage );
#pragma warning restore RK1000
                    if ( errorMessage.IsNotNullOrWhiteSpace() )
                    {
                        return null;
                    }

                    paymentInfo.GatewayPersonIdentifier = customerToken;
                }
            }

            var transaction = gateway.Charge( financialGateway, paymentInfo, out errorMessage );
            return errorMessage.IsNotNullOrWhiteSpace() ? null : transaction;
        }

        /// <summary>
        /// Before the charge: each line's signed document, ready to save (by line item id; no
        /// entry when the template has none).
        /// </summary>
        private Dictionary<int, RegistrationSignatures.PreparedDocument> PrepareDocuments( Cart cart, Person payer )
        {
            var signatures = new RegistrationSignatures( _rockContext );
            var personAliasService = new PersonAliasService( _rockContext );
            var documents = new Dictionary<int, RegistrationSignatures.PreparedDocument>();
            foreach ( var line in cart.LineItems )
            {
                var person = personAliasService.GetPerson( line.CartRegistrant.PersonAliasId );
                var prepared = signatures.Prepare( line.RegistrationInstance, payer, person, line.SignatureData, RegistrationForms.FromJson( line.FieldValuesJson ) );
                if ( prepared != null )
                {
                    documents[line.Id] = prepared;
                }
            }

            return documents;
        }

        /// <summary>
        /// Creates one Registration per line item, with its registrant, fees, registrant answers and
        /// signed document. Returns line item id → registration id.
        /// </summary>
        private Dictionary<int, int> CreateRegistrations( Cart cart, Person payer, PricedCart priced, Dictionary<int, RegistrationSignatures.PreparedDocument> documents )
        {
            var registrationService = new RegistrationService( _rockContext );
            var instanceService = new RegistrationInstanceService( _rockContext );
            var feeItemService = new RegistrationTemplateFeeItemService( _rockContext );
            var forms = new RegistrationForms( _rockContext );
            var signatures = new RegistrationSignatures( _rockContext );
            var map = new Dictionary<int, int>();

            foreach ( var line in cart.LineItems )
            {
                var instance = instanceService.Get( line.RegistrationInstanceId );

                var registration = new Registration
                {
                    RegistrationInstanceId = instance.Id,
                    PersonAliasId = payer.PrimaryAliasId,
                    FirstName = payer.NickName.IsNotNullOrWhiteSpace() ? payer.NickName : payer.FirstName,
                    LastName = payer.LastName,
                    ConfirmationEmail = payer.Email,
                    // Only the codes that took something off this line, and only their share of it
                    // (Rock subtracts DiscountAmount from the registrant's cost + fees).
                    DiscountCode = string.Join( ",", priced.CodesFor( line.Id ) ).Truncate( 100 ),
                    DiscountAmount = priced.DiscountFor( line.Id ),
                    DiscountPercentage = 0m,
                    GroupId = new CatalogService( _rockContext ).GetLinkedGroupId( instance.Id ),
                    IsTemporary = false
                };

                // A wait-listed line owes nothing now, but its own template code (financial
                // assistance) stays on the registration for Rock to apply once a seat opens.
                if ( line.IsWaitList && line.TemplateDiscountCode.IsNotNullOrWhiteSpace() )
                {
                    registration.DiscountCode = line.TemplateDiscountCode;
                    registration.DiscountAmount = line.TemplateDiscountAmount;
                    registration.DiscountPercentage = line.TemplateDiscountPercentage;
                }

                registrationService.Add( registration );

                var registrant = new RegistrationRegistrant
                {
                    PersonAliasId = line.CartRegistrant.PersonAliasId,
                    OnWaitList = line.IsWaitList,
                    Cost = line.SnapshotBasePrice
                };
                registration.Registrants.Add( registrant );

                foreach ( var addOn in line.AddOns )
                {
                    var feeItem = feeItemService.Get( addOn.RegistrationTemplateFeeItemId );
                    if ( feeItem == null || !feeItem.RegistrationTemplateFeeId.HasValue )
                    {
                        // Validate refuses a deleted fee item; should one go between that check and
                        // here, keep the charge on the registrant rather than lose it.
                        registrant.Cost += addOn.SnapshotPrice;
                        continue;
                    }

                    registrant.Fees.Add( new RegistrationRegistrantFee
                    {
                        RegistrationTemplateFeeId = feeItem.RegistrationTemplateFeeId.Value,
                        RegistrationTemplateFeeItemId = feeItem.Id,
                        Quantity = 1,
                        Cost = addOn.SnapshotPrice,
                        Option = feeItem.Name
                    } );
                }

                _rockContext.SaveChanges();

                // The registrant's own answers and signed document stand with the registration. The
                // person's answers are written after commit (group member answers once they are in
                // the group).
                forms.ApplyToRegistrant( instance.RegistrationTemplate, registrant, RegistrationForms.FromJson( line.FieldValuesJson ) );
                if ( documents.TryGetValue( line.Id, out var document ) )
                {
                    signatures.Attach( registrant, document );
                }

                line.RegistrationId = registration.Id;
                map[line.Id] = registration.Id;
            }

            _rockContext.SaveChanges();
            return map;
        }

        private void SaveTransaction( FinancialTransaction transaction, Cart cart, Person payer, PricedCart priced,
            Dictionary<int, int> registrationIds, FinancialGateway financialGateway, IGatewayComponent gateway, ReferencePaymentInfo paymentInfo, string batchNamePrefix )
        {
            var instanceService = new RegistrationInstanceService( _rockContext );

            transaction.AuthorizedPersonAliasId = payer.PrimaryAliasId;
            transaction.TransactionDateTime = RockDateTime.Now;
            transaction.FinancialGatewayId = financialGateway.Id;
            transaction.TransactionTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_EVENT_REGISTRATION.AsGuid() ) ?? transaction.TransactionTypeValueId;
            transaction.SourceTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_WEBSITE.AsGuid() );
            transaction.Summary = Summary( priced );
            if ( transaction.FinancialPaymentDetail == null )
            {
                transaction.FinancialPaymentDetail = new FinancialPaymentDetail();
            }

            // As Rock's RegistrationEntry: the gateway knows the payment details best, so this only
            // fills in what it left unset.
            if ( paymentInfo != null )
            {
                transaction.FinancialPaymentDetail.SetFromPaymentInfo( paymentInfo, gateway as GatewayComponent, _rockContext );
            }

            var registrationEntityTypeId = EntityTypeCache.Get<Registration>().Id;
            // Wait-listed campers pay nothing now; Rock bills them when they come off the list.
            foreach ( var line in cart.LineItems.Where( l => !l.IsWaitList ) )
            {
                var pricedLine = priced.Lines.First( l => l.LineItemId == line.Id );
                // Each line posts net of the discounts that applied to it, so an offering-scoped
                // code only reduces that offering's account. The details add up to priced.Total;
                // a line with nothing to pay (a free event may have no account) posts nothing.
                var amount = pricedLine.LineTotal - priced.DiscountFor( line.Id );
                if ( amount <= 0m )
                {
                    continue;
                }

                var accountId = instanceService.Get( line.RegistrationInstanceId ).AccountId;

                transaction.TransactionDetails.Add( new FinancialTransactionDetail
                {
                    Amount = amount,
                    AccountId = accountId.Value,
                    EntityTypeId = registrationEntityTypeId,
                    EntityId = registrationIds[line.Id]
                } );
            }

            var batchService = new FinancialBatchService( _rockContext );
            var batchChanges = new History.HistoryChangeList();
            var batch = batchService.GetForNewTransaction( transaction, batchNamePrefix.IsNullOrWhiteSpace() ? "Registration Cart" : batchNamePrefix );
            FinancialBatchService.EvaluateNewBatchHistory( batch, batchChanges );
            if ( batch.Id == 0 )
            {
                _rockContext.SaveChanges();
            }

            transaction.BatchId = batch.Id;
            new FinancialTransactionService( _rockContext ).Add( transaction );
            _rockContext.SaveChanges();

            batchService.IncrementControlAmount( batch.Id, transaction.TotalAmount, batchChanges );
            _rockContext.SaveChanges();

            HistoryService.SaveChanges( _rockContext, typeof( FinancialBatch ),
                Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(), batch.Id, batchChanges, true );
        }
    }
}
