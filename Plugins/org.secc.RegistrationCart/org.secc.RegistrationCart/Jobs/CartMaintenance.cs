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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using org.secc.RegistrationCart.Domain;
using org.secc.RegistrationCart.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;

namespace org.secc.RegistrationCart.Jobs
{
    /// <summary>
    /// Housekeeping for Registration Cart: closes carts nobody has touched in a while. Open carts
    /// hold no seats, so this is only so a family coming back months later starts fresh.
    /// </summary>
    [DisplayName( "Registration Cart Maintenance" )]
    [Description( "Marks open Registration Cart carts nobody has changed in a while as abandoned." )]

    [IntegerField( "Abandon Open Carts After (days)", Key = AttributeKey.AbandonAfterDays, IsRequired = false, DefaultIntegerValue = 30, Order = 0,
        Description = "An open cart with no changes for this many days is marked abandoned; the family's next visit starts a new cart. 0 turns this off." )]
    public class CartMaintenance : RockJob
    {
        private static class AttributeKey
        {
            public const string AbandonAfterDays = "AbandonAfterDays";
        }

        public override void Execute()
        {
            var results = new List<string>();
            results.Add( AbandonIdleCarts() );
            Result = results.Where( r => r.IsNotNullOrWhiteSpace() ).JoinStrings( " " );
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
