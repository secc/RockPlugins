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
    ///   1. charges the gateway token once,
    ///   2. creates one Rock Registration (+ registrant + fees) per line item,
    ///   3. records one FinancialTransaction (batched) with a detail line per registration,
    ///   4. closes the cart.
    /// The cart row is the idempotency record — a cart that is already CheckedOut is
    /// never charged again.
    /// </summary>
    public class CheckoutProcessor
    {
        public class Result
        {
            public bool Success { get; set; }
            public bool AlreadyProcessed { get; set; }
            public bool IsDeclined { get; set; }
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

        public Result Process( Cart cart, Person payer, FinancialGateway financialGateway, string gatewayToken, string batchNamePrefix )
        {
            var result = new Result();

            if ( cart.Status == CartStatus.CheckedOut )
            {
                result.AlreadyProcessed = true;
                result.Success = cart.FinancialTransactionId.HasValue || cart.LastCheckoutError.IsNullOrWhiteSpace();
                result.TransactionId = cart.FinancialTransactionId;
                result.ErrorMessage = cart.LastCheckoutError;
                return result;
            }

            var problems = Validate( cart );
            if ( problems.Any() )
            {
                result.ErrorMessage = string.Join( " ", problems );
                return result;
            }

            var priced = CartPricing.Price( cart );
            result.Total = priced.Total;
            var dateTexts = new CatalogService( _rockContext ).GetEventDateTexts( cart.LineItems.Select( l => l.RegistrationInstanceId ) );
            var lineInstance = cart.LineItems.ToDictionary( l => l.Id, l => l.RegistrationInstanceId );
            result.ItemDescriptions = priced.Lines.Select( l =>
            {
                var date = lineInstance.TryGetValue( l.LineItemId, out var iid ) && dateTexts.TryGetValue( iid, out var dt ) && dt.IsNotNullOrWhiteSpace() ? " · " + dt : "";
                return $"{l.RegistrantName} — {l.OfferingTitle}: {l.VariantName}{date}";
            } ).ToList();

            // ---- 1. Charge -------------------------------------------------------
            FinancialTransaction transaction = null;
            if ( priced.Total > 0m )
            {
                if ( financialGateway == null )
                {
                    result.ErrorMessage = "No payment gateway is configured on the checkout block.";
                    return result;
                }

                if ( gatewayToken.IsNullOrWhiteSpace() )
                {
                    result.ErrorMessage = "The payment form did not return a payment token. Please try again.";
                    return result;
                }

                var gateway = financialGateway.GetGatewayComponent() as IGatewayComponent;
                if ( gateway == null )
                {
                    result.ErrorMessage = "The configured payment gateway is not available.";
                    return result;
                }

                var paymentInfo = new ReferencePaymentInfo
                {
                    ReferenceNumber = gatewayToken,
                    Amount = priced.Total,
                    Description = Summary( priced ),
                    FirstName = payer.FirstName,
                    LastName = payer.LastName,
                    Email = payer.Email,
                    TransactionTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_EVENT_REGISTRATION.AsGuid() )
                };

                string errorMessage;
                transaction = Charge( gateway, financialGateway, paymentInfo, out errorMessage );
                if ( transaction == null )
                {
                    result.IsDeclined = true;
                    result.ErrorMessage = errorMessage.IsNullOrWhiteSpace() ? "Payment was declined. Please try a different card." : errorMessage;
                    cart.LastCheckoutError = result.ErrorMessage;
                    _rockContext.SaveChanges();
                    return result;
                }
            }

            // ---- 2 + 3. Registrations, transaction, close cart ---------------------
            // From here on the money has been taken, so whatever happens the cart must
            // close: a retry must never charge twice.
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
                ExceptionLogService.LogException( new Exception( $"Registration Cart: payment {transaction?.TransactionCode} succeeded for cart {cart.Id} but registration creation failed.", ex ) );

                // Close the cart with the error recorded so support can reconcile.
                using ( var fresh = new RockContext() )
                {
                    var closing = new CartService( fresh ).Get( cart.Id );
                    if ( closing != null )
                    {
                        closing.Status = CartStatus.CheckedOut;
                        closing.CheckedOutDateTime = RockDateTime.Now;
                        closing.LastCheckoutError = ( "Payment " + ( transaction?.TransactionCode ?? "(none)" ) + " succeeded but registrations could not be created: " + ex.Message ).Truncate( 2000 );
                        fresh.SaveChanges();
                    }
                }

                result.ErrorMessage = "Your payment was received but we could not finish creating the registrations. Please contact the office — do not pay again.";
                return result;
            }

            return result;
        }

        private static string Summary( PricedCart priced ) =>
            $"Registration Cart registration — {priced.Lines.Count} item{( priced.Lines.Count == 1 ? "" : "s" )}";

        private static FinancialTransaction Charge( IGatewayComponent gateway, FinancialGateway financialGateway, ReferencePaymentInfo paymentInfo, out string errorMessage )
        {
            errorMessage = null;

            // Mirrors Rock's own RegistrationEntry block: Obsidian-hosted gateways want a
            // customer vault record before a tokenized charge.
            if ( gateway is IObsidianHostedGatewayComponent obsidianGateway )
            {
                if ( paymentInfo.GatewayPersonIdentifier.IsNullOrWhiteSpace() )
                {
                    var customerToken = obsidianGateway.CreateCustomerAccount( financialGateway, paymentInfo, out errorMessage );
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
            var discountShares = DiscountShares( priced );
            var map = new Dictionary<int, int>();

            foreach ( var line in cart.LineItems )
            {
                var pricedLine = priced.Lines.First( l => l.LineItemId == line.Id );
                var instance = instanceService.Get( line.RegistrationInstanceId );

                var registration = new Registration
                {
                    RegistrationInstanceId = instance.Id,
                    PersonAliasId = payer.PrimaryAliasId,
                    FirstName = payer.NickName.IsNotNullOrWhiteSpace() ? payer.NickName : payer.FirstName,
                    LastName = payer.LastName,
                    ConfirmationEmail = payer.Email,
                    DiscountCode = string.Join( ",", priced.Discounts.Select( d => d.Code ) ).Truncate( 100 ),
                    DiscountAmount = discountShares.TryGetValue( line.Id, out var share ) ? share : 0m,
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

        /// <summary>Spreads the cart's discount total across lines in proportion to line totals (cents-exact).</summary>
        private static Dictionary<int, decimal> DiscountShares( PricedCart priced )
        {
            var shares = new Dictionary<int, decimal>();
            var subtotal = priced.Subtotal;
            var discount = priced.DiscountTotal;
            if ( discount <= 0m || subtotal <= 0m )
            {
                return shares;
            }

            decimal allocated = 0m;
            var lines = priced.Lines.ToList();
            for ( int i = 0; i < lines.Count; i++ )
            {
                decimal share = i == lines.Count - 1
                    ? discount - allocated
                    : Math.Round( discount * lines[i].LineTotal / subtotal, 2, MidpointRounding.AwayFromZero );
                share = Math.Min( share, lines[i].LineTotal );
                shares[lines[i].LineItemId] = share;
                allocated += share;
            }

            return shares;
        }

        private void SaveTransaction( FinancialTransaction transaction, Cart cart, Person payer, PricedCart priced,
            Dictionary<int, int> registrationIds, FinancialGateway financialGateway, string batchNamePrefix )
        {
            var instanceService = new RegistrationInstanceService( _rockContext );
            var discountShares = DiscountShares( priced );

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
                var amount = pricedLine.LineTotal - ( discountShares.TryGetValue( line.Id, out var share ) ? share : 0m );

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
