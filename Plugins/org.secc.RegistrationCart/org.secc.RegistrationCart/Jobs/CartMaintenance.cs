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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Model;
using org.secc.RegistrationCart.Services;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;

namespace org.secc.RegistrationCart.Jobs
{
    /// <summary>
    /// Housekeeping for Registration Cart: closes carts nobody has touched in a while (open carts
    /// hold no seats, so this is only so a family coming back months later starts fresh), and
    /// finishes signed documents whose PDF failed when their registration was paid for.
    /// </summary>
    [DisplayName( "Registration Cart Maintenance" )]
    [Description( "Marks open Registration Cart carts nobody has changed in a while as abandoned, and makes the PDFs of signed documents that failed at checkout." )]

    [IntegerField( "Abandon Open Carts After (days)", Key = AttributeKey.AbandonAfterDays, IsRequired = false, DefaultIntegerValue = 30, Order = 0,
        Description = "An open cart with no changes for this many days is marked abandoned; the family's next visit starts a new cart. 0 turns this off." )]
    [IntegerField( "Retry Signed Document PDFs After (minutes)", Key = AttributeKey.DocumentRetryMinutes, IsRequired = false, DefaultIntegerValue = 10, Order = 1,
        Description = "A signed document from a cart checkout that still has no PDF this long after it was saved gets its PDF made (and its completion email sent) by this job." )]
    [IntegerField( "Report Unconfirmed Checkouts After (minutes)", Key = AttributeKey.ReportPendingMinutes, IsRequired = false, DefaultIntegerValue = 30, Order = 2,
        Description = "A checkout whose payment outcome is unknown (or that was charged but couldn't register) for this long ends the job with a warning listing it, once, so the job's notification email (Notification Status: Error) reaches whoever reconciles payments. 0 turns this off." )]
    public class CartMaintenance : RockJob
    {
        private static class AttributeKey
        {
            public const string AbandonAfterDays = "AbandonAfterDays";
            public const string DocumentRetryMinutes = "DocumentRetryMinutes";
            public const string ReportPendingMinutes = "ReportPendingMinutes";
        }

        public override void Execute()
        {
            var results = new List<string>();
            results.Add( AbandonIdleCarts() );
            results.Add( FinishSignedDocuments() );
            var pending = NewlyUnconfirmedCheckouts();
            if ( pending.Any() )
            {
                results.Add( $"Checkouts needing a payment check: {pending.JoinStrings( " " )}" );
            }

            Result = results.Where( r => r.IsNotNullOrWhiteSpace() ).JoinStrings( " " );
            if ( pending.Any() )
            {
                throw new RockJobWarningException( Result );
            }
        }

        /// <summary>
        /// Checkouts that became unconfirmed (<see cref="CheckoutRules.IsPendingCheckout"/>) more
        /// than the configured minutes ago and haven't been reported yet (they crossed that line
        /// since the job last ran), one line each for whoever checks the gateway.
        /// </summary>
        private List<string> NewlyUnconfirmedCheckouts()
        {
            var minutes = GetAttributeValue( AttributeKey.ReportPendingMinutes ).AsIntegerOrNull() ?? 30;
            if ( minutes <= 0 )
            {
                return new List<string>();
            }

            var cutoff = RockDateTime.Now.AddMinutes( -minutes );
            var since = ServiceJob?.LastRunDateTime?.AddMinutes( -minutes );
            using ( var rockContext = new RockContext() )
            {
                var carts = CartManager.CartGraph( new CartService( rockContext ).Queryable() )
                    .Include( c => c.OwnerPersonAlias.Person )
                    .Where( c => ( c.Status == CartStatus.Processing || ( c.Status == CartStatus.CheckedOut && c.FinancialTransactionId == null ) )
                        && ( c.ModifiedDateTime ?? c.CreatedDateTime ) < cutoff
                        && ( since == null || ( c.ModifiedDateTime ?? c.CreatedDateTime ) >= since ) )
                    .ToList()
                    .Where( c => CheckoutRules.IsPendingCheckout( c.Status, c.FinancialTransactionId.HasValue, c.LastCheckoutError ) )
                    .ToList();

                return carts.Select( c => $"Cart {c.Id} ({c.OwnerPersonAlias?.Person?.FullName}, {CartPricing.Price( c ).Total.FormatAsCurrency()}, {( c.ModifiedDateTime ?? c.CreatedDateTime ):g}): {c.LastCheckoutError ?? "stopped mid-checkout"}." ).ToList();
            }
        }

        /// <summary>
        /// Signed documents saved with a cart's registrations whose PDF failed after the checkout:
        /// makes the PDF and sends the completion email, as checkout would have.
        /// </summary>
        private string FinishSignedDocuments()
        {
            var minutes = GetAttributeValue( AttributeKey.DocumentRetryMinutes ).AsIntegerOrNull() ?? 10;
            var cutoff = RockDateTime.Now.AddMinutes( -minutes );
            List<int> documentIds;
            using ( var rockContext = new RockContext() )
            {
                documentIds = new CartLineItemService( rockContext ).Queryable()
                    .Where( l => l.RegistrationId.HasValue )
                    .SelectMany( l => l.Registration.Registrants )
                    .Where( r => r.SignatureDocumentId.HasValue
                        && r.SignatureDocument.BinaryFileId == null
                        && r.SignatureDocument.CreatedDateTime < cutoff )
                    .Select( r => r.SignatureDocumentId.Value )
                    .Distinct()
                    .ToList();
            }

            var finished = 0;
            foreach ( var documentId in documentIds )
            {
                try
                {
                    using ( var rockContext = new RockContext() )
                    {
                        finished += new RegistrationSignatures( rockContext ).Complete( documentId ) ? 1 : 0;
                    }
                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( new Exception( $"Registration Cart: the PDF for signed document {documentId} still could not be made.", ex ) );
                }
            }

            return documentIds.Any() ? $"{finished} of {documentIds.Count} signed document PDF{( documentIds.Count == 1 ? "" : "s" )} made." : "";
        }

        /// <summary>Open carts with no change to the cart or its lines for the configured days become Abandoned.</summary>
        private string AbandonIdleCarts()
        {
            var days = GetAttributeValue( AttributeKey.AbandonAfterDays ).AsIntegerOrNull() ?? 30;
            if ( days <= 0 )
            {
                return "";
            }

            var cutoff = RockDateTime.Now.AddDays( -days );
            using ( var rockContext = new RockContext() )
            {
                var carts = new CartService( rockContext ).Queryable()
                    .Where( c => c.Status == CartStatus.Open
                        && ( c.ModifiedDateTime ?? c.CreatedDateTime ) < cutoff
                        && !c.LineItems.Any( l => ( l.ModifiedDateTime ?? l.CreatedDateTime ) >= cutoff ) )
                    .ToList();

                foreach ( var cart in carts )
                {
                    cart.Status = CartStatus.Abandoned;
                }

                rockContext.SaveChanges();
                return $"{carts.Count} idle cart{( carts.Count == 1 ? "" : "s" )} abandoned.";
            }
        }
    }
}
