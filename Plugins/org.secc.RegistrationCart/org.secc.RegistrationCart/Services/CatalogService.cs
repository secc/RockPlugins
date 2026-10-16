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

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Builds the cart's catalog from Rock's own registration objects:
    ///   offering  = RegistrationTemplate
    ///   option    = RegistrationInstance (active, inside its registration window)
    ///   add-ons   = the template's fees and fee items
    /// The plugin's overlay tables add only what Rock lacks: visibility, grade bands,
    /// image and ordering. Nothing is duplicated — set the camp up the Rock way and it
    /// appears here.
    /// </summary>
    public class CatalogService
    {
        public class CatalogTemplate
        {
            public RegistrationTemplate Template { get; set; }
            public TemplateSetting Setting { get; set; }
            public string ImageUrl { get; set; }
            public List<CatalogInstance> Instances { get; set; } = new List<CatalogInstance>();
            public List<CatalogFee> Fees { get; set; } = new List<CatalogFee>();
            public bool IsVisible => Setting?.IsVisible ?? true;
            public int Order => Setting?.Order ?? 0;
            public string Description => Setting?.CatalogDescription.IsNotNullOrWhiteSpace() == true ? Setting.CatalogDescription : Template.Description;
        }

        public class CatalogInstance
        {
            public RegistrationInstance Instance { get; set; }
            public InstanceSetting Setting { get; set; }
            public decimal Price { get; set; }
            public int? CampusId { get; set; }
            public string CampusName { get; set; }
            public DateTime? EventStart { get; set; }
            public DateTime? EventEnd { get; set; }
            /// <summary>"linked" (calendar event), "setting" (entered by staff) or null.</summary>
            public string EventDateSource { get; set; }
            public DateTime? RegistrationDeadline => Instance.EndDateTime;
            public int? SpotsRemaining { get; set; }
            public bool IsFull => SpotsRemaining.HasValue && SpotsRemaining.Value <= 0;
            /// <summary>Rock's template setting: a full instance takes wait-list registrations.</summary>
            public bool WaitListEnabled => Instance.RegistrationTemplate.WaitListEnabled;
            public int RegisteredCount { get; set; }
            public bool IsOpen { get; set; }
            public bool IsVisible => Setting?.IsVisible ?? true;
            public GradeBand Bands => Setting?.EligibleGradeBands ?? GradeBand.Everyone;
            public int Order => Setting?.Order ?? 0;
        }

        public class CatalogFee
        {
            public RegistrationTemplateFee Fee { get; set; }
            public List<CatalogFeeItem> Items { get; set; } = new List<CatalogFeeItem>();
            public bool IsRequired => Fee.IsRequired;
            /// <summary>Rock "Single" fees are one yes/no item; "Multiple" fees are a list, one-or-many per AllowMultiple.</summary>
            public SelectionMode Mode => Fee.FeeType == RegistrationFeeType.Multiple && !Fee.AllowMultiple ? SelectionMode.Single : SelectionMode.Multi;
            /// <summary>A required single-item fee is mandatory: it is always added.</summary>
            public bool IsLocked => Fee.IsRequired && Fee.FeeType == RegistrationFeeType.Single;
        }

        public class CatalogFeeItem
        {
            public RegistrationTemplateFeeItem Item { get; set; }
            public FeeItemSetting Setting { get; set; }
            public GradeBand Bands => Setting?.EligibleGradeBands ?? GradeBand.Everyone;
        }

        private readonly RockContext _rockContext;

        public CatalogService( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>Templates with at least one open, visible instance — what shoppers see.</summary>
        public List<CatalogTemplate> GetCatalog()
        {
            return Build( includeClosed: false, includeHidden: false, templateId: null )
                .Where( t => t.Instances.Any() )
                .ToList();
        }

        /// <summary>One template with its open, visible instances and fees (null if none).</summary>
        public CatalogTemplate GetTemplate( int templateId )
        {
            return Build( includeClosed: false, includeHidden: false, templateId: templateId )
                .FirstOrDefault( t => t.Instances.Any() );
        }

        /// <summary>Every active template with every active instance, for the admin screen.</summary>
        public List<CatalogTemplate> GetAdminCatalog()
        {
            return Build( includeClosed: true, includeHidden: true, templateId: null );
        }

        /// <summary>A single open, visible instance (with its template + fees) or null.</summary>
        public CatalogInstance GetOpenInstance( int registrationInstanceId, out CatalogTemplate template )
        {
            template = null;
            var templateId = new RegistrationInstanceService( _rockContext ).Queryable()
                .Where( i => i.Id == registrationInstanceId )
                .Select( i => ( int? ) i.RegistrationTemplateId )
                .FirstOrDefault();
            if ( templateId == null )
            {
                return null;
            }

            template = GetTemplate( templateId.Value );
            return template?.Instances.FirstOrDefault( i => i.Instance.Id == registrationInstanceId );
        }

        /// <summary>
        /// The group the instance's event linkage places registrants in. Rock's RegistrationEntry
        /// finds it from the calendar link it was opened from; the cart has no such link, so it
        /// uses the instance's first linkage that names a group (each camp has one).
        /// </summary>
        public int? GetLinkedGroupId( int registrationInstanceId )
        {
            return new EventItemOccurrenceGroupMapService( _rockContext ).Queryable()
                .Where( m => m.RegistrationInstanceId == registrationInstanceId && m.GroupId.HasValue )
                .OrderBy( m => m.Id )
                .Select( m => m.GroupId )
                .FirstOrDefault();
        }

        /// <summary>Event date text per instance id, for cart and checkout lines (empty when unknown).</summary>
        public Dictionary<int, string> GetEventDateTexts( IEnumerable<int> instanceIds )
        {
            var ids = instanceIds.Distinct().ToList();
            var result = ids.ToDictionary( id => id, id => "" );
            if ( !ids.Any() )
            {
                return result;
            }

            var settings = new InstanceSettingService( _rockContext ).Queryable()
                .Where( s => ids.Contains( s.RegistrationInstanceId ) && s.EventStartDate != null )
                .Select( s => new { s.RegistrationInstanceId, s.EventStartDate, s.EventEndDate } )
                .ToList();
            var linkages = new EventItemOccurrenceGroupMapService( _rockContext ).Queryable()
                .Where( m => m.RegistrationInstanceId.HasValue && ids.Contains( m.RegistrationInstanceId.Value ) )
                .Select( m => new
                {
                    InstanceId = m.RegistrationInstanceId.Value,
                    Start = m.EventItemOccurrence.Schedule.EffectiveStartDate ?? m.EventItemOccurrence.NextStartDateTime,
                    End = m.EventItemOccurrence.Schedule.EffectiveEndDate
                } )
                .ToList();

            foreach ( var id in ids )
            {
                var setting = settings.FirstOrDefault( s => s.RegistrationInstanceId == id );
                if ( setting != null )
                {
                    result[id] = DateRangeText( setting.EventStartDate, setting.EventEndDate );
                    continue;
                }

                var link = linkages.FirstOrDefault( l => l.InstanceId == id );
                if ( link?.Start != null )
                {
                    result[id] = DateRangeText( link.Start, link.End );
                }
            }

            return result;
        }

        /// <summary>"Jul 5–9, 2027", "Sep 17 – Oct 2, 2027", "Jul 5, 2027".</summary>
        public static string DateRangeText( DateTime? start, DateTime? end )
        {
            if ( start == null && end == null ) return "";
            if ( start != null && end == null ) return start.Value.ToString( "MMM d, yyyy" );
            if ( start == null ) return "Ends " + end.Value.ToString( "MMM d, yyyy" );
            if ( start.Value.Date == end.Value.Date ) return start.Value.ToString( "MMM d, yyyy" );
            if ( start.Value.Year == end.Value.Year && start.Value.Month == end.Value.Month ) return $"{start.Value:MMM d}–{end.Value:d, yyyy}";
            if ( start.Value.Year == end.Value.Year ) return $"{start.Value:MMM d} – {end.Value:MMM d, yyyy}";
            return $"{start.Value:MMM d, yyyy} – {end.Value:MMM d, yyyy}";
        }

        private List<CatalogTemplate> Build( bool includeClosed, bool includeHidden, int? templateId )
        {
            var now = RockDateTime.Now;
            var instanceQuery = new RegistrationInstanceService( _rockContext ).Queryable()
                .Include( i => i.RegistrationTemplate.Fees.Select( f => f.FeeItems ) )
                .Where( i => i.IsActive && i.RegistrationTemplate.IsActive );

            if ( templateId.HasValue )
            {
                instanceQuery = instanceQuery.Where( i => i.RegistrationTemplateId == templateId.Value );
            }

            if ( !includeClosed )
            {
                instanceQuery = instanceQuery.Where( i =>
                    ( i.StartDateTime == null || i.StartDateTime <= now ) &&
                    ( i.EndDateTime == null || i.EndDateTime >= now ) );
            }

            var instances = instanceQuery.OrderBy( i => i.RegistrationTemplate.Name ).ThenBy( i => i.Name ).ToList();
            if ( !instances.Any() )
            {
                return new List<CatalogTemplate>();
            }

            var instanceIds = instances.Select( i => i.Id ).ToList();
            var templateIds = instances.Select( i => i.RegistrationTemplateId ).Distinct().ToList();

            var templateSettings = new TemplateSettingService( _rockContext ).Queryable()
                .Where( s => templateIds.Contains( s.RegistrationTemplateId ) ).ToList()
                .ToDictionary( s => s.RegistrationTemplateId );
            var instanceSettings = new InstanceSettingService( _rockContext ).Queryable()
                .Where( s => instanceIds.Contains( s.RegistrationInstanceId ) ).ToList()
                .ToDictionary( s => s.RegistrationInstanceId );
            var feeItemSettings = new FeeItemSettingService( _rockContext ).Queryable()
                .Where( s => templateIds.Contains( s.RegistrationTemplateFeeItem.RegistrationTemplateFee.RegistrationTemplateId ) ).ToList()
                .ToDictionary( s => s.RegistrationTemplateFeeItemId );

            // Campus + event date via the instance's event linkage (if any).
            var linkages = new EventItemOccurrenceGroupMapService( _rockContext ).Queryable()
                .Where( m => m.RegistrationInstanceId.HasValue && instanceIds.Contains( m.RegistrationInstanceId.Value ) )
                .Select( m => new
                {
                    InstanceId = m.RegistrationInstanceId.Value,
                    CampusId = m.CampusId ?? m.EventItemOccurrence.CampusId,
                    EventStart = m.EventItemOccurrence.Schedule.EffectiveStartDate ?? m.EventItemOccurrence.NextStartDateTime,
                    EventEnd = m.EventItemOccurrence.Schedule.EffectiveEndDate,
                    PhotoId = m.EventItemOccurrence.EventItem.PhotoId
                } )
                .ToList()
                .GroupBy( l => l.InstanceId )
                .ToDictionary( g => g.Key, g => g.First() );

            // Seats taken: real registrants (not wait-listed) + seated lines in open carts and in
            // carts mid-checkout (Processing), which are about to become registrants.
            var registered = new RegistrationRegistrantService( _rockContext ).Queryable()
                .Where( r => instanceIds.Contains( r.Registration.RegistrationInstanceId ) && !r.OnWaitList )
                .GroupBy( r => r.Registration.RegistrationInstanceId )
                .Select( g => new { InstanceId = g.Key, Count = g.Count() } )
                .ToDictionary( x => x.InstanceId, x => x.Count );
            var reserved = new CartLineItemService( _rockContext ).Queryable()
                .Where( l => instanceIds.Contains( l.RegistrationInstanceId ) && !l.IsWaitList && ( l.Cart.Status == CartStatus.Open || l.Cart.Status == CartStatus.Processing ) )
                .GroupBy( l => l.RegistrationInstanceId )
                .Select( g => new { InstanceId = g.Key, Count = g.Count() } )
                .ToDictionary( x => x.InstanceId, x => x.Count );

            var result = new List<CatalogTemplate>();
            foreach ( var group in instances.GroupBy( i => i.RegistrationTemplateId ) )
            {
                var template = group.First().RegistrationTemplate;
                templateSettings.TryGetValue( template.Id, out var templateSetting );
                var catalogTemplate = new CatalogTemplate { Template = template, Setting = templateSetting };

                if ( !includeHidden && !catalogTemplate.IsVisible )
                {
                    continue;
                }

                int? photoId = null;
                foreach ( var instance in group )
                {
                    instanceSettings.TryGetValue( instance.Id, out var instanceSetting );
                    linkages.TryGetValue( instance.Id, out var linkage );
                    photoId = photoId ?? linkage?.PhotoId;

                    var ci = new CatalogInstance
                    {
                        Instance = instance,
                        Setting = instanceSetting,
                        Price = template.SetCostOnInstance == true ? ( instance.Cost ?? 0m ) : template.Cost,
                        CampusId = linkage?.CampusId,
                        CampusName = linkage?.CampusId != null ? CampusCache.Get( linkage.CampusId.Value )?.Name : null,
                        EventStart = instanceSetting?.EventStartDate ?? linkage?.EventStart,
                        EventEnd = instanceSetting?.EventEndDate ?? ( instanceSetting?.EventStartDate != null ? null : linkage?.EventEnd ),
                        EventDateSource = instanceSetting?.EventStartDate != null ? "setting" : ( linkage?.EventStart != null ? "linked" : null ),
                        RegisteredCount = registered.TryGetValue( instance.Id, out var rc ) ? rc : 0,
                        IsOpen = CheckoutRules.IsWindowOpen( instance.StartDateTime, instance.EndDateTime, now )
                    };

                    if ( instance.MaxAttendees.HasValue && instance.MaxAttendees.Value > 0 )
                    {
                        var taken = ci.RegisteredCount + ( reserved.TryGetValue( instance.Id, out var rv ) ? rv : 0 );
                        ci.SpotsRemaining = Math.Max( 0, instance.MaxAttendees.Value - taken );
                    }

                    if ( !includeHidden && !ci.IsVisible )
                    {
                        continue;
                    }

                    catalogTemplate.Instances.Add( ci );
                }

                catalogTemplate.Instances = catalogTemplate.Instances.OrderBy( i => i.Order ).ThenBy( i => i.Instance.Name ).ToList();

                catalogTemplate.ImageUrl = templateSetting?.ImageUrl.IsNotNullOrWhiteSpace() == true
                    ? templateSetting.ImageUrl
                    : ( photoId.HasValue ? $"/GetImage.ashx?id={photoId.Value}&maxwidth=600" : null );

                foreach ( var fee in template.Fees.Where( f => f.IsActive ).OrderBy( f => f.Order ).ThenBy( f => f.Name ) )
                {
                    var cf = new CatalogFee { Fee = fee };
                    foreach ( var item in fee.FeeItems.Where( i => i.IsActive ).OrderBy( i => i.Order ).ThenBy( i => i.Name ) )
                    {
                        feeItemSettings.TryGetValue( item.Id, out var itemSetting );
                        cf.Items.Add( new CatalogFeeItem { Item = item, Setting = itemSetting } );
                    }

                    if ( cf.Items.Any() )
                    {
                        catalogTemplate.Fees.Add( cf );
                    }
                }

                result.Add( catalogTemplate );
            }

            return result.OrderBy( t => t.Order ).ThenBy( t => t.Template.Name ).ToList();
        }
    }
}
