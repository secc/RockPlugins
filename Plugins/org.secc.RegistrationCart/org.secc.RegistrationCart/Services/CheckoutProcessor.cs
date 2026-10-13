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
using System.Data.SqlClient;
using System.Linq;
using org.secc.RegistrationCart.Model;
using Rock;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Web.Cache;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Domain.Pricing;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Turns an open cart into money and registrations:
    ///   0. claims the cart (Open → Processing) with one conditional UPDATE,
    ///   1. charges the gateway token once,
    ///   2. creates one Rock Registration (+ registrant + fees) per line item,
    ///   3. records one FinancialTransaction (batched) with a detail line per registration,
    ///   4. closes the cart.
    /// The cart row is the idempotency record: only the request that claims the cart can
    /// charge it, and the cart goes back to Open only when nothing was charged
    /// (<see cref="CheckoutRules.StatusAfterStop"/>).
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
        }

        private readonly RockContext _rockContext;

        public CheckoutProcessor( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>Problems that must be fixed by an admin before this cart can check out. Empty = OK.</summary>
        public List<string> Validate( Cart cart )
        {
            var problems = new List<string>();
            if ( cart == null || cart.LineItems.Count == 0 )
            {
                problems.Add( "The cart is empty." );
                return problems;
            }

            var instanceService = new RegistrationInstanceService( _rockContext );
            foreach ( var line in cart.LineItems )
            {
                var instance = instanceService.Get( line.RegistrationInstanceId );
                if ( instance == null )
                {
                    problems.Add( $"\"{line.RegistrationInstance?.Name}\" no longer exists in Rock." );
                }
                else if ( !instance.IsActive )
                {
                    problems.Add( $"\"{instance.Name}\" is no longer active." );
                }
                else if ( instance.AccountId == null )
                {
                    problems.Add( $"Registration instance \"{instance.Name}\" has no financial account. An administrator must set one in Rock." );
                }
            }

            return problems.Distinct().ToList();
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

            // ---- 0. Claim --------------------------------------------------------
            // Two requests (two tabs, a retry after a timeout) can both get here with the same
            // open cart. Only the one that moves it Open → Processing may go on to charge.
            if ( !TryClaim( cartId ) )
            {
                return ClaimLost( cartId );
            }

            var result = new Result();
            Cart cart;
            PricedCart priced;
            IGatewayComponent gateway = null;
            ReferencePaymentInfo paymentInfo = null;

            // Nothing below can have charged the card until the gateway call, so any failure
            // here hands the cart back.
            try
            {
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

                var problems = Validate( cart );
                if ( problems.Any() )
                {
                    return Stop( cart, ChargeOutcome.NotAttempted, result, string.Join( " ", problems ) );
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

                var dateTexts = new CatalogService( _rockContext ).GetEventDateTexts( cart.LineItems.Select( l => l.RegistrationInstanceId ) );
                var lineInstance = cart.LineItems.ToDictionary( l => l.Id, l => l.RegistrationInstanceId );
                result.ItemDescriptions = priced.Lines.Select( l =>
                {
                    var date = lineInstance.TryGetValue( l.LineItemId, out var iid ) && dateTexts.TryGetValue( iid, out var dt ) && dt.IsNotNullOrWhiteSpace() ? " · " + dt : "";
                    return $"{l.RegistrantName} — {l.OfferingTitle}: {l.VariantName}{date}";
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
                StopOnFreshContext( cartId, ChargeOutcome.NotAttempted, "Checkout failed before payment: " + ex.Message );
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
                    var registrationIds = CreateRegistrations( cart, payer, priced );
                    result.RegistrationIds = registrationIds.Values.ToList();

                    if ( transaction != null )
                    {
                        SaveTransaction( transaction, cart, payer, priced, registrationIds, financialGateway, batchNamePrefix );
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

            return result;
        }

        private const string PaidButNotRegisteredMessage = "Your payment was received but we could not finish creating the registrations. Please contact the office — do not pay again.";
        private const string CartUnavailableMessage = "Your cart is no longer available. Please reload the page.";

        /// <summary>
        /// Moves the cart Open → Processing in one conditional UPDATE. Only the request whose
        /// UPDATE changes the row gets true; a concurrent one sees zero rows affected.
        /// </summary>
        private bool TryClaim( int cartId )
        {
            var rowsAffected = _rockContext.Database.ExecuteSqlCommand(
                "UPDATE [_org_secc_RegistrationCart_Cart] SET [Status] = @Processing, [ModifiedDateTime] = @Now WHERE [Id] = @CartId AND [Status] = @Open",
                new SqlParameter( "@Processing", ( int ) CartStatus.Processing ),
                new SqlParameter( "@Now", RockDateTime.Now ),
                new SqlParameter( "@CartId", cartId ),
                new SqlParameter( "@Open", ( int ) CartStatus.Open ) );

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

        /// <summary>Creates one Registration per line item. Returns line item id → registration id.</summary>
        private Dictionary<int, int> CreateRegistrations( Cart cart, Person payer, PricedCart priced )
        {
            var registrationService = new RegistrationService( _rockContext );
            var instanceService = new RegistrationInstanceService( _rockContext );
            var feeItemService = new RegistrationTemplateFeeItemService( _rockContext );
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
                    IsTemporary = false
                };
                registrationService.Add( registration );

                var registrant = new RegistrationRegistrant
                {
                    PersonAliasId = line.CartRegistrant.PersonAliasId,
                    OnWaitList = false,
                    Cost = line.SnapshotBasePrice
                };
                registration.Registrants.Add( registrant );

                foreach ( var addOn in line.AddOns )
                {
                    var feeItem = feeItemService.Get( addOn.RegistrationTemplateFeeItemId );
                    if ( feeItem == null || !feeItem.RegistrationTemplateFeeId.HasValue )
                    {
                        // Fee item deleted since it was added: keep the charge on the registrant.
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
                line.RegistrationId = registration.Id;
                map[line.Id] = registration.Id;
            }

            _rockContext.SaveChanges();
            return map;
        }

        private void SaveTransaction( FinancialTransaction transaction, Cart cart, Person payer, PricedCart priced,
            Dictionary<int, int> registrationIds, FinancialGateway financialGateway, string batchNamePrefix )
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

            var registrationEntityTypeId = EntityTypeCache.Get<Registration>().Id;
            foreach ( var line in cart.LineItems )
            {
                var pricedLine = priced.Lines.First( l => l.LineItemId == line.Id );
                var instance = instanceService.Get( line.RegistrationInstanceId );
                var accountId = instance.AccountId;
                // Each line posts net of the discounts that applied to it, so an offering-scoped
                // code only reduces that offering's account. The details add up to priced.Total.
                var amount = pricedLine.LineTotal - priced.DiscountFor( line.Id );

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
