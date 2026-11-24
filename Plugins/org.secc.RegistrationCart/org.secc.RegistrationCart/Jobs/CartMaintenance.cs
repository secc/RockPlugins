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
using Rock.Web.Cache;

namespace org.secc.RegistrationCart.Jobs
{
    /// <summary>
    /// Housekeeping for Registration Cart: closes carts nobody has touched in a while (open carts
    /// hold no seats, so this is only so a family coming back months later starts fresh), reopens
    /// checkouts that stopped before their payment was sent, finishes signed documents whose PDF
    /// failed when their registration was paid for, lets Rock Cleanup have the uploaded files no
    /// cart line needs any more, and warns about checkouts that need a payment check until staff
    /// deal with them.
    /// </summary>
    [DisplayName( "Registration Cart Maintenance" )]
    [Description( "Marks open Registration Cart carts nobody has changed in a while as abandoned, reopens checkouts that stopped before payment, makes the PDFs of signed documents that failed at checkout, and warns about checkouts whose payment needs checking." )]

    [IntegerField( "Abandon Open Carts After (days)", Key = AttributeKey.AbandonAfterDays, IsRequired = false, DefaultIntegerValue = 30, Order = 0,
        Description = "An open cart with no changes for this many days is marked abandoned; the family's next visit starts a new cart. 0 turns this off." )]
    [IntegerField( "Retry Signed Document PDFs After (minutes)", Key = AttributeKey.DocumentRetryMinutes, IsRequired = false, DefaultIntegerValue = 10, Order = 1,
        Description = "A signed document from a cart checkout that still has no PDF this long after it was saved gets its PDF made (and its completion email sent) by this job." )]
    [IntegerField( "Stop Retrying Signed Document PDFs After (days)", Key = AttributeKey.DocumentRetryDays, IsRequired = false, DefaultIntegerValue = 3, Order = 2,
        Description = "A signed document whose PDF still can't be made this many days after it was saved is no longer retried; the job's result counts them. 0 retries forever." )]
    [IntegerField( "Report Unconfirmed Checkouts After (minutes)", Key = AttributeKey.ReportPendingMinutes, IsRequired = false, DefaultIntegerValue = 30, Order = 3,
        Description = "A checkout whose payment outcome is unknown (or that was charged but couldn't register) for this long ends the job with a warning naming the cart, so the job's notification email (Notification Status: Error, with Notification Emails set) reaches whoever reconciles payments. 0 turns this off." )]
    [IntegerField( "Repeat The Unconfirmed Checkout Warning Every (hours)", Key = AttributeKey.RepeatPendingHours, IsRequired = false, DefaultIntegerValue = 24, Order = 4,
        Description = "Until staff reopen or close an unconfirmed checkout on the Registration Cart settings page, the job warns about it again this often. Every run lists the open ones in its result either way." )]
    [IntegerField( "Reopen Checkouts That Never Reached Payment After (minutes)", Key = AttributeKey.ReopenUnsentMinutes, IsRequired = false, DefaultIntegerValue = 15, Order = 5,
        Description = "A checkout that stopped before its payment was sent to the gateway (e.g. the site restarted) charged nothing; after this long the job reopens the cart so the family can pay. 0 turns this off." )]
    public class CartMaintenance : RockJob
    {
        private static class AttributeKey
        {
            public const string AbandonAfterDays = "AbandonAfterDays";
            public const string DocumentRetryMinutes = "DocumentRetryMinutes";
            public const string DocumentRetryDays = "DocumentRetryDays";
            public const string ReportPendingMinutes = "ReportPendingMinutes";
            public const string RepeatPendingHours = "RepeatPendingHours";
            public const string ReopenUnsentMinutes = "ReopenUnsentMinutes";
        }

        public override void Execute()
        {
            // Each step by itself: one failing must not stop the others, least of all the warning.
            var results = new List<string>();
            var failures = new List<Exception>();
            foreach ( var step in new Func<string>[] { AbandonIdleCarts, ReopenUnsentCheckouts, FinishSignedDocuments, ReleaseUnusedUploads } )
            {
                try
                {
                    results.Add( step() );
                }
                catch ( Exception ex )
                {
                    failures.Add( ex );
                    results.Add( $"{step.Method.Name} failed: {ex.Message}" );
                }
            }

            var warn = false;
            try
            {
                results.Add( UnconfirmedCheckouts( out warn ) );
            }
            catch ( Exception ex )
            {
                failures.Add( ex );
                results.Add( $"{nameof( UnconfirmedCheckouts )} failed: {ex.Message}" );
            }

            Result = results.Where( r => r.IsNotNullOrWhiteSpace() ).JoinStrings( " " );
            if ( failures.Any() )
            {
                throw new AggregateException( Result, failures );
            }

            if ( warn )
            {
                throw new RockJobWarningException( Result );
            }
        }

        /// <summary>
        /// The checkouts that have been unconfirmed (<see cref="CheckoutRules.IsPendingCheckout"/>)
        /// for the configured minutes, named by cart id only, with where staff deal with them: no
        /// names, amounts or error text, because the job's result is emailed. Every run lists
        /// them; <paramref name="warn"/> (which sends the email) is set when one hasn't been
        /// warned about yet, or not for the repeat hours, so a cart is never reported just once
        /// and then forgotten.
        /// </summary>
        private string UnconfirmedCheckouts( out bool warn )
        {
            warn = false;
            var minutes = GetAttributeValue( AttributeKey.ReportPendingMinutes ).AsIntegerOrNull() ?? 30;
            if ( minutes <= 0 )
            {
                return "";
            }

            var now = RockDateTime.Now;
            var repeatHours = Math.Max( 1, GetAttributeValue( AttributeKey.RepeatPendingHours ).AsIntegerOrNull() ?? 24 );
            using ( var rockContext = new RockContext() )
            {
                var pending = PendingCheckouts.Candidates( new CartService( rockContext ).Queryable().AsNoTracking() )
                    .OrderBy( c => c.Id )
                    .ToList()
                    .Where( c => PendingCheckouts.IsPending( c ) && CheckoutRules.IsQuiet( c.ModifiedDateTime ?? c.CreatedDateTime, now, minutes ) )
                    .ToList();
                if ( !pending.Any() )
                {
                    return "";
                }

                var due = pending.Where( c => c.PendingReportedDateTime == null || c.PendingReportedDateTime <= now.AddHours( -repeatHours ) ).ToList();
                if ( due.Any() )
                {
                    warn = true;
                    new PendingCheckouts( rockContext ).MarkReported( due.Select( c => c.Id ) );
                }

                var settingsUrl = GlobalAttributesCache.Get().GetValue( "InternalApplicationRoot" ).EnsureTrailingForwardslash() + "admin/registration-cart";
                return $"{pending.Count} {( pending.Count == 1 ? "checkout needs" : "checkouts need" )} a payment check (cart {pending.Select( c => c.Id.ToString() ).JoinStrings( ", " )}). "
                    + $"Check the gateway, then reopen or close each one under \"Checkouts needing a payment check\" at {settingsUrl}";
            }
        }

        /// <summary>Checkouts that stopped before their payment was sent to the gateway open again (<see cref="PendingCheckouts.ReopenUnsent"/>).</summary>
        private string ReopenUnsentCheckouts()
        {
            var minutes = GetAttributeValue( AttributeKey.ReopenUnsentMinutes ).AsIntegerOrNull() ?? 15;
            if ( minutes <= 0 )
            {
                return "";
            }

            using ( var rockContext = new RockContext() )
            {
                var reopened = new PendingCheckouts( rockContext ).ReopenUnsent( minutes );
                return reopened.Any() ? $"{reopened.Count} checkout{( reopened.Count == 1 ? "" : "s" )} that stopped before payment reopened (cart {reopened.Select( id => id.ToString() ).JoinStrings( ", " )})." : "";
            }
        }

        /// <summary>
        /// Signed documents saved with a cart's registrations whose PDF failed after the checkout:
        /// makes the PDF and sends the completion email, as checkout would have. A document is
        /// retried for the configured days, then left for staff (and counted in the result).
        /// </summary>
        private string FinishSignedDocuments()
        {
            var minutes = GetAttributeValue( AttributeKey.DocumentRetryMinutes ).AsIntegerOrNull() ?? 10;
            var days = GetAttributeValue( AttributeKey.DocumentRetryDays ).AsIntegerOrNull() ?? 3;
            var cutoff = RockDateTime.Now.AddMinutes( -minutes );
            var giveUpBefore = days > 0 ? RockDateTime.Now.AddDays( -days ) : DateTime.MinValue;
            List<int> documentIds;
            int givenUp;
            using ( var rockContext = new RockContext() )
            {
                var missing = new CartLineItemService( rockContext ).Queryable()
                    .Where( l => l.RegistrationId.HasValue )
                    .SelectMany( l => l.Registration.Registrants )
                    .Where( r => r.SignatureDocumentId.HasValue
                        && r.SignatureDocument.BinaryFileId == null
                        && r.SignatureDocument.CreatedDateTime < cutoff )
                    .Select( r => new { Id = r.SignatureDocumentId.Value, r.SignatureDocument.CreatedDateTime } )
                    .Distinct()
                    .ToList();
                documentIds = missing.Where( d => d.CreatedDateTime >= giveUpBefore ).Select( d => d.Id ).ToList();
                givenUp = missing.Count - documentIds.Count;
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

            var results = new List<string>();
            if ( documentIds.Any() )
            {
                results.Add( $"{finished} of {documentIds.Count} signed document PDF{( documentIds.Count == 1 ? "" : "s" )} made." );
            }

            if ( givenUp > 0 )
            {
                results.Add( $"{givenUp} signed document{( givenUp == 1 ? "" : "s" )} from cart checkouts still {( givenUp == 1 ? "has" : "have" )} no PDF after {days} day{( days == 1 ? "" : "s" )} and {( givenUp == 1 ? "is" : "are" )} no longer retried." );
            }

            return results.JoinStrings( " " );
        }

        /// <summary>
        /// Files a family uploaded for a cart line that no line needs any more (removed, replaced,
        /// or its cart abandoned) go back to temporary, for Rock Cleanup to delete. After
        /// <see cref="AbandonIdleCarts"/>, so the carts it just closed are included.
        /// </summary>
        private string ReleaseUnusedUploads()
        {
            using ( var rockContext = new RockContext() )
            {
                var released = new RegistrationForms( rockContext ).ReleaseUnusedUploads();
                return released > 0 ? $"{released} uploaded file{( released == 1 ? "" : "s" )} no cart needs any more left for Rock Cleanup." : "";
            }
        }

        /// <summary>
        /// Open carts with no change to the cart or its lines for the configured days become
        /// Abandoned, and the financial assistance codes made for their lines that nothing else
        /// uses are removed from the template.
        /// </summary>
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

                var discounts = new TemplateDiscounts( rockContext );
                foreach ( var cart in carts )
                {
                    discounts.RemoveUnusedWorkflowCodes( cart.Id );
                }

                return $"{carts.Count} idle cart{( carts.Count == 1 ? "" : "s" )} abandoned.";
            }
        }
    }
}
