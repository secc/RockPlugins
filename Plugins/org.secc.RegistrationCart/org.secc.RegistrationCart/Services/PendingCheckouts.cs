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
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Model;
using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Checkouts that took, or may have taken, a payment without finishing
    /// (<see cref="CheckoutRules.IsPendingCheckout"/>), and the only ways out of that state:
    /// staff reopen one after checking that nothing was charged, staff close one after dealing
    /// with the payment, or the maintenance job reopens one whose charge was never sent. Until
    /// then the cart's lines keep their seats and limited fee items, its campers can't be
    /// registered for the same option again, and the payer is warned not to pay twice.
    /// Every change is one conditional UPDATE, so it can't undo a checkout that finished in the
    /// meantime.
    /// </summary>
    public class PendingCheckouts
    {
        private readonly RockContext _rockContext;

        public PendingCheckouts( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>
        /// The carts that may be pending, as SQL can tell (the status, an error, not closed by
        /// staff). Callers finish with <see cref="IsPending"/>, which also drops a blank error.
        /// </summary>
        public static IQueryable<Cart> Candidates( IQueryable<Cart> carts ) =>
            carts.Where( c => c.Status == CartStatus.Processing
                || ( c.Status == CartStatus.CheckedOut && c.ResolvedDateTime == null && c.LastCheckoutError != null ) );

        public static bool IsPending( Cart cart ) =>
            CheckoutRules.IsPendingCheckout( cart.Status, cart.LastCheckoutError, cart.ResolvedDateTime.HasValue );

        /// <summary>Every pending checkout with its lines, payer and payment, oldest first.</summary>
        public List<Cart> List()
        {
            return Candidates( CartManager.CartGraph( new CartService( _rockContext ).Queryable().AsNoTracking() ) )
                .Include( c => c.OwnerPersonAlias.Person )
                .Include( c => c.FinancialTransaction )
                .OrderBy( c => c.Id )
                .ToList()
                .Where( IsPending )
                .ToList();
        }

        /// <summary>
        /// Staff checked the gateway and nothing was charged: the cart opens again so the family
        /// can pay. Only a claimed (Processing) cart with no payment record that has been quiet for
        /// <see cref="CheckoutRules.MinutesBeforeResolving"/> minutes. Returns why not, or null.
        /// </summary>
        public string Reopen( int cartId, Person staff, string note )
        {
            var detail = $"Reopened by {staff?.FullName ?? "staff"} on {RockDateTime.Now:g}: nothing was charged. {note}".Trim();
            return ReopenProcessing( cartId, detail, onlyUnsent: false, quietMinutes: CheckoutRules.MinutesBeforeResolving )
                ? null
                : "That checkout can't be reopened: it finished, was already dealt with, has a payment recorded, or started less than "
                    + $"{CheckoutRules.MinutesBeforeResolving} minutes ago and may still be running. Reload the page to see where it stands.";
        }

        /// <summary>
        /// Staff have dealt with the payment (refunded it, or registered the campers by hand): the
        /// checkout stops being pending. A claimed cart is abandoned; one that was charged but not
        /// registered stays checked out. Who, when and what they did is kept on the cart. Returns
        /// why not, or null.
        /// </summary>
        public string Close( int cartId, Person staff, string note )
        {
            if ( note.IsNullOrWhiteSpace() )
            {
                return "Please say what you found at the gateway and what was done (refunded, registered by hand, ...).";
            }

            var now = RockDateTime.Now;
            var rowsAffected = _rockContext.Database.ExecuteSqlCommand( @"
                UPDATE [_org_secc_RegistrationCart_Cart]
                SET [Status] = CASE WHEN [Status] = @Processing THEN @Abandoned ELSE [Status] END,
                    [ResolvedDateTime] = @Now, [ResolvedByPersonAliasId] = @StaffAliasId, [ResolutionNote] = @Note
                WHERE [Id] = @CartId AND [ResolvedDateTime] IS NULL
                    AND ( ( [Status] = @Processing AND ISNULL([ModifiedDateTime], [CreatedDateTime]) <= @QuietBefore )
                        OR ( [Status] = @CheckedOut AND ISNULL([LastCheckoutError], '') <> '' ) )",
                new SqlParameter( "@Processing", SqlDbType.Int ) { Value = ( int ) CartStatus.Processing },
                new SqlParameter( "@Abandoned", SqlDbType.Int ) { Value = ( int ) CartStatus.Abandoned },
                new SqlParameter( "@CheckedOut", SqlDbType.Int ) { Value = ( int ) CartStatus.CheckedOut },
                new SqlParameter( "@Now", SqlDbType.DateTime ) { Value = now },
                new SqlParameter( "@StaffAliasId", SqlDbType.Int ) { Value = ( object ) staff?.PrimaryAliasId ?? DBNull.Value },
                new SqlParameter( "@Note", SqlDbType.NVarChar, 1000 ) { Value = note.Trim().Truncate( 1000 ) },
                new SqlParameter( "@CartId", SqlDbType.Int ) { Value = cartId },
                new SqlParameter( "@QuietBefore", SqlDbType.DateTime ) { Value = now.AddMinutes( -CheckoutRules.MinutesBeforeResolving ) } );
            if ( rowsAffected != 1 )
            {
                return "That checkout can't be closed: it finished, was already dealt with, or started less than "
                    + $"{CheckoutRules.MinutesBeforeResolving} minutes ago and may still be running. Reload the page to see where it stands.";
            }

            // An abandoned cart's unused financial assistance codes go with it.
            new TemplateDiscounts( _rockContext ).RemoveUnusedWorkflowCodes( cartId );
            return null;
        }

        /// <summary>
        /// The maintenance job's part: reopens claimed carts, quiet for <paramref name="minutes"/>,
        /// whose charge was never sent to the gateway (the request died between the claim and the
        /// charge, e.g. an app pool recycle), so nothing can have been charged. Returns the cart ids.
        /// </summary>
        public List<int> ReopenUnsent( int minutes )
        {
            var now = RockDateTime.Now;
            var cartIds = new CartService( _rockContext ).Queryable().AsNoTracking()
                .Where( c => c.Status == CartStatus.Processing && c.PaymentSentDateTime == null && c.FinancialTransactionId == null )
                .Select( c => new { c.Id, c.Status, c.PaymentSentDateTime, LastChanged = c.ModifiedDateTime ?? c.CreatedDateTime } )
                .ToList()
                .Where( c => CheckoutRules.CanReopenUnsent( c.Status, c.PaymentSentDateTime, c.LastChanged, now, minutes ) )
                .Select( c => c.Id )
                .ToList();

            return cartIds
                .Where( id => ReopenProcessing( id, $"Checkout stopped before the payment was sent, so nothing was charged. Reopened by the maintenance job on {now:g}.", onlyUnsent: true, quietMinutes: minutes ) )
                .ToList();
        }

        /// <summary>
        /// Stamps the carts as reported by the maintenance job. Plain SQL so the cart's modified
        /// time (when its checkout stopped) stays put.
        /// </summary>
        public void MarkReported( IEnumerable<int> cartIds )
        {
            foreach ( var cartId in cartIds )
            {
                _rockContext.Database.ExecuteSqlCommand(
                    "UPDATE [_org_secc_RegistrationCart_Cart] SET [PendingReportedDateTime] = @Now WHERE [Id] = @CartId",
                    new SqlParameter( "@Now", SqlDbType.DateTime ) { Value = RockDateTime.Now },
                    new SqlParameter( "@CartId", SqlDbType.Int ) { Value = cartId } );
            }
        }

        /// <summary>
        /// Processing → Open in one conditional UPDATE: only while the cart is still claimed, has
        /// no payment record and has been quiet long enough (and, for the job, was never sent to
        /// the gateway).
        /// </summary>
        private bool ReopenProcessing( int cartId, string detail, bool onlyUnsent, int quietMinutes )
        {
            var now = RockDateTime.Now;
            var rowsAffected = _rockContext.Database.ExecuteSqlCommand( @"
                UPDATE [_org_secc_RegistrationCart_Cart]
                SET [Status] = @Open, [PaymentSentDateTime] = NULL, [PendingReportedDateTime] = NULL,
                    [LastCheckoutError] = @Detail, [ModifiedDateTime] = @Now
                WHERE [Id] = @CartId AND [Status] = @Processing AND [FinancialTransactionId] IS NULL
                    AND ISNULL([ModifiedDateTime], [CreatedDateTime]) <= @QuietBefore
                    AND ( @OnlyUnsent = 0 OR [PaymentSentDateTime] IS NULL )",
                new SqlParameter( "@Open", SqlDbType.Int ) { Value = ( int ) CartStatus.Open },
                new SqlParameter( "@Processing", SqlDbType.Int ) { Value = ( int ) CartStatus.Processing },
                new SqlParameter( "@Detail", SqlDbType.NVarChar, 2000 ) { Value = detail.Truncate( 2000 ) },
                new SqlParameter( "@Now", SqlDbType.DateTime ) { Value = now },
                new SqlParameter( "@CartId", SqlDbType.Int ) { Value = cartId },
                new SqlParameter( "@QuietBefore", SqlDbType.DateTime ) { Value = now.AddMinutes( -Math.Max( quietMinutes, CheckoutRules.MinutesBeforeResolving ) ) },
                new SqlParameter( "@OnlyUnsent", SqlDbType.Bit ) { Value = onlyUnsent } );

            if ( rowsAffected != 1 )
            {
                return false;
            }

            KeepOneOpenCart( cartId );
            return true;
        }

        /// <summary>
        /// A family shops from one open cart (their newest). If they started another while this
        /// one was claimed, the one with nothing in it is abandoned; when both hold campers the
        /// reopened one is, so what the family sees doesn't change under them.
        /// </summary>
        private void KeepOneOpenCart( int reopenedCartId )
        {
            var cartService = new CartService( _rockContext );
            var reopened = cartService.Get( reopenedCartId );
            if ( reopened == null )
            {
                return;
            }

            var ownerPersonId = new PersonAliasService( _rockContext ).GetPersonId( reopened.OwnerPersonAliasId );
            var others = cartService.Queryable()
                .Include( c => c.LineItems )
                .Where( c => c.Id != reopenedCartId && c.Status == CartStatus.Open && c.OwnerPersonAlias.PersonId == ownerPersonId )
                .ToList();
            if ( others.Any( c => c.LineItems.Any() ) )
            {
                reopened.Status = CartStatus.Abandoned;
                reopened.LastCheckoutError = ( reopened.LastCheckoutError + " The family already had a newer cart, so this one was closed instead." ).Truncate( 2000 );
            }

            foreach ( var empty in others.Where( c => !c.LineItems.Any() ) )
            {
                empty.Status = CartStatus.Abandoned;
            }

            _rockContext.SaveChanges();
            if ( reopened.Status == CartStatus.Abandoned )
            {
                new TemplateDiscounts( _rockContext ).RemoveUnusedWorkflowCodes( reopenedCartId );
            }
        }
    }
}
