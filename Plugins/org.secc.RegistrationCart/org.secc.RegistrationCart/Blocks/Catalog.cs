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
using System.ComponentModel;
using System.Linq;
using org.secc.RegistrationCart.Services;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Attribute;
using Rock.Blocks;

namespace org.secc.RegistrationCart.Blocks
{
    [DisplayName( "Registration Cart Catalog" )]
    [Category( "Registration Cart" )]
    [Description( "Lists Rock registration templates that have an open registration instance, as a shoppable catalog." )]
    [IconCssClass( "fa fa-shopping-cart" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [TextField( "Heading", Key = AttributeKey.Heading, IsRequired = false, DefaultValue = "Registration", Order = 0 )]
    [TextField( "Intro", Key = AttributeKey.Intro, IsRequired = false, DefaultValue = "Add your whole family to one cart and check out once.", Order = 1 )]
    [LinkedPage( "Offering Detail Page", Key = NavAttributeKey.OfferingDetailPage, IsRequired = false, Order = 2 )]
    [LinkedPage( "Cart Page", Key = NavAttributeKey.CartPage, IsRequired = false, Order = 3 )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.BLOCK_CATALOG )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.CATALOG )]
    public class Catalog : RegistrationCartBlockBase
    {
        private static class AttributeKey
        {
            public const string Heading = "Heading";
            public const string Intro = "Intro";
        }

        public override string ObsidianFileUrl => ObsidianBlockUrl( "catalog" );

        public override object GetObsidianBlockInitialization()
        {
            var cart = GetCartManager()?.GetOpenCart();
            var box = new CatalogInitializationBox
            {
                Navigation = BuildNavigation( cart ),
                Heading = GetAttributeValue( AttributeKey.Heading ),
                Intro = GetAttributeValue( AttributeKey.Intro )
            };

            foreach ( var t in new CatalogService( RockContext ).GetCatalog() )
            {
                var starts = t.Instances.Where( i => i.EventStart.HasValue ).Select( i => i.EventStart.Value ).ToList();
                var ends = t.Instances.Select( i => i.EventEnd ?? i.EventStart ).Where( d => d.HasValue ).Select( d => d.Value ).ToList();
                box.Offerings.Add( new OfferingSummaryBag
                {
                    IdKey = t.Template.IdKey,
                    DetailUrl = this.GetLinkedPageUrl( NavAttributeKey.OfferingDetailPage, OfferingKeyParameter, t.Template.IdKey ),
                    OfferingTypeText = t.Template.Category?.Name,
                    Title = t.Template.Name,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl,
                    DateText = starts.Any() ? DateRangeText( starts.Min(), ends.Any() ? ends.Max() : starts.Max() ) : "",
                    MinPrice = t.Instances.Min( i => i.Price ),
                    OptionCount = t.Instances.Count,
                    CampusNames = t.Instances.Where( i => i.CampusName != null ).Select( i => i.CampusName ).Distinct().OrderBy( n => n ).ToList()
                } );
            }

            return box;
        }
    }
}
