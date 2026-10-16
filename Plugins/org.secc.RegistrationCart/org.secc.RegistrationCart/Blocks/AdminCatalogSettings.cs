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
using org.secc.RegistrationCart.Model;
using org.secc.RegistrationCart.Services;
using org.secc.RegistrationCart.ViewModels;
using Rock;
using Rock.Attribute;
using Rock.Blocks;
using org.secc.RegistrationCart.Domain;

namespace org.secc.RegistrationCart.Blocks
{
    [DisplayName( "Registration Cart Settings" )]
    [Category( "Registration Cart > Administration" )]
    [Description( "Choose which Rock registration templates and instances appear in Registration Cart and who may register for them." )]
    [IconCssClass( "fa fa-shopping-cart" )]
    [SupportedSiteTypes( Rock.Model.SiteType.Web )]

    [LinkedPage( "Catalog Page", Key = AttributeKey.CatalogPage, IsRequired = false, Order = 0 )]

    [Rock.SystemGuid.EntityTypeGuid( SystemGuid.EntityType.BLOCK_ADMIN_CATALOG_SETTINGS )]
    [Rock.SystemGuid.BlockTypeGuid( SystemGuid.BlockType.ADMIN_CATALOG_SETTINGS )]
    public class AdminCatalogSettings : AdminBlockBase
    {
        private static class AttributeKey
        {
            public const string CatalogPage = "CatalogPage";
        }

        public override string ObsidianFileUrl => ObsidianBlockUrl( "adminCatalogSettings" );

        public override object GetObsidianBlockInitialization()
        {
            return new CatalogSettingsInitializationBox
            {
                CanEdit = CanEdit,
                CatalogUrl = this.GetLinkedPageUrl( AttributeKey.CatalogPage ),
                Templates = new CatalogService( RockContext ).GetAdminCatalog().Select( ToBag ).ToList()
            };
        }

        /// <summary>Saves the overlay rows for one template (template, its instances, its fee items).</summary>
        [BlockAction]
        public BlockActionResult SaveTemplate( AdminTemplateBag bag )
        {
            var denied = RequireEdit();
            if ( denied != null ) return denied;
            if ( bag == null ) return ActionBadRequest( "Nothing to save." );

            var templateSettings = new TemplateSettingService( RockContext );
            var ts = templateSettings.Queryable().FirstOrDefault( s => s.RegistrationTemplateId == bag.Id );
            if ( ts == null )
            {
                ts = new TemplateSetting { RegistrationTemplateId = bag.Id };
                templateSettings.Add( ts );
            }

            ts.IsVisible = bag.IsVisible;
            ts.ImageUrl = ( bag.ImageUrl ?? "" ).Trim().Truncate( 500 );
            ts.CatalogDescription = ( bag.CatalogDescription ?? "" ).Trim().Truncate( 2000 );
            ts.Order = bag.Order;

            var instanceSettings = new InstanceSettingService( RockContext );
            foreach ( var ib in bag.Instances ?? new List<AdminInstanceBag>() )
            {
                var s = instanceSettings.Queryable().FirstOrDefault( x => x.RegistrationInstanceId == ib.Id );
                if ( s == null )
                {
                    s = new InstanceSetting { RegistrationInstanceId = ib.Id };
                    instanceSettings.Add( s );
                }

                s.IsVisible = ib.IsVisible;
                s.EligibleGradeBands = ( GradeBand ) ib.EligibleGradeBands;
                s.Order = ib.Order;
                s.EventStartDate = ParseDate( ib.EventStartDate );
                s.EventEndDate = ParseDate( ib.EventEndDate );
            }

            var feeItemSettings = new FeeItemSettingService( RockContext );
            foreach ( var item in ( bag.Fees ?? new List<AdminFeeBag>() ).SelectMany( f => f.Items ?? new List<AdminFeeItemBag>() ) )
            {
                var s = feeItemSettings.Queryable().FirstOrDefault( x => x.RegistrationTemplateFeeItemId == item.Id );
                if ( s == null )
                {
                    s = new FeeItemSetting { RegistrationTemplateFeeItemId = item.Id };
                    feeItemSettings.Add( s );
                }

                s.EligibleGradeBands = ( GradeBand ) item.EligibleGradeBands;
            }

            RockContext.SaveChanges();

            var saved = new CatalogService( RockContext ).GetAdminCatalog().FirstOrDefault( t => t.Template.Id == bag.Id );
            return ActionOk( new SaveTemplateResultBag { Template = saved == null ? null : ToBag( saved ) } );
        }

        private static AdminTemplateBag ToBag( CatalogService.CatalogTemplate t )
        {
            return new AdminTemplateBag
            {
                Id = t.Template.Id,
                Name = t.Template.Name,
                CategoryName = t.Template.Category?.Name,
                RockDescription = t.Template.Description,
                IsVisible = t.IsVisible,
                ImageUrl = t.Setting?.ImageUrl ?? "",
                CatalogDescription = t.Setting?.CatalogDescription ?? "",
                Order = t.Order,
                Instances = t.Instances.Select( i => new AdminInstanceBag
                {
                    Id = i.Instance.Id,
                    Name = i.Instance.Name,
                    WindowText = WindowText( i.Instance ),
                    CampusName = i.CampusName,
                    Price = i.Price,
                    MaxAttendees = i.Instance.MaxAttendees,
                    RegisteredCount = i.RegisteredCount,
                    HasAccount = i.Instance.AccountId.HasValue,
                    IsOpen = i.IsOpen,
                    IsVisible = i.IsVisible,
                    EligibleGradeBands = ( int ) i.Bands,
                    Order = i.Order,
                    EventStartDate = DateString( i.Setting?.EventStartDate ),
                    EventEndDate = DateString( i.Setting?.EventEndDate ),
                    EventDateText = CatalogService.DateRangeText( i.EventStart, i.EventEnd ),
                    EventDateSource = i.EventDateSource
                } ).ToList(),
                Fees = t.Fees.Select( f => new AdminFeeBag
                {
                    Id = f.Fee.Id,
                    Name = f.Fee.Name,
                    RuleText = ( f.IsRequired ? "Required" : "Optional" ) + " · " + ( f.IsLocked ? "always added" : f.Mode == SelectionMode.Single ? "choose one" : "choose any" ),
                    Items = f.Items.Select( i => new AdminFeeItemBag
                    {
                        Id = i.Item.Id,
                        Name = i.Item.Name,
                        Cost = i.Item.Cost,
                        EligibleGradeBands = ( int ) i.Bands
                    } ).ToList()
                } ).ToList()
            };
        }

        private static string WindowText( Rock.Model.RegistrationInstance i )
        {
            if ( i.StartDateTime == null && i.EndDateTime == null ) return "Always open";
            var from = i.StartDateTime?.ToString( "M/d/yyyy" ) ?? "…";
            var to = i.EndDateTime?.ToString( "M/d/yyyy" ) ?? "…";
            return $"{from} – {to}";
        }
    }
}
