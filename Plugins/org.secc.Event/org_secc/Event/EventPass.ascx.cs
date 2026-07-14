using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;



namespace RockWeb.Plugins.org_secc.Event
{
    [DisplayName( "Event Pass" )]
    [Category( "SECC > Event" )]
    [Description( "Displays a QR pass for registrants of an event." )]


    [BooleanField( "Include Registrants on Waitlist",
        Description = "Indicates if passes should be generated for individuals on the waitlist. Default is No.",
        IsRequired = false,
        DefaultBooleanValue = false,
        Order = 0,
        Category = "Configuration",
        Key = AttributeKey.IncludeWaitList )]

    [BooleanField( "Include Registrar's Registrations",
        Description = "Indicates if the pass, when viewed by a logged-in person, should also include registrants from other registrations in the same registration instance that were made by the viewer or a member of the viewer's family. Anonymous visitors always see only the registration/registrant the link points to. Useful for events where each registrant requires a separate registration (e.g. camps). Default is No.",
        IsRequired = false,
        DefaultBooleanValue = false,
        Order = 1,
        Category = "Configuration",
        Key = AttributeKey.IncludeRegistrarRegistrations )]

    [TextField( "Pass Not Found Header",
        Description = "The header/title of the message box that is displayed if the pass is not found.",
        IsRequired = false,
        DefaultValue = "Pass Not Found",
        Order = 0,
        Category = "Error Responses",
        Key = AttributeKey.PassNotFoundHeader )]
    [CodeEditorField( "Pass Not Found Message",
        Description = "The message to display if the person's event pass is not found.",
        IsRequired = true,
        EditorMode = CodeEditorMode.Html,
        EditorTheme = CodeEditorTheme.Rock,
        Order = 1,
        Category = "Error Responses",
        Key = AttributeKey.PassNotFoundMessage )]
    public partial class EventPass : RockBlock
    {
        public static class AttributeKey
        {
            public const string IncludeWaitList = "IncludeWaitList";
            public const string IncludeRegistrarRegistrations = "IncludeRegistrarRegistrations";
            public const string PassNotFoundHeader = "PassNotFoundHeader";
            public const string PassNotFoundMessage = "PassNotFoundMessage";

        }


        private Guid? registrationGuid = null;
        private Guid? registrantGuid = null;
        bool isActiveItemSet = false;

        protected override void OnInit( EventArgs e )
        {
            rPasses.ItemDataBound += rPasses_ItemDataBound;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            registrationGuid = PageParameter( "Registration" ).AsGuidOrNull();
            registrantGuid = PageParameter( "Registrant" ).AsGuidOrNull();
            if (!IsPostBack)
            {
                LoadPasses();
            }
        }

        private void rPasses_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if (!isActiveItemSet)
            {
                var pnlItem = e.Item.FindControl( "pnlItem" ) as Panel;
                pnlItem.CssClass = pnlItem.CssClass + " active";
                isActiveItemSet = true;
            }
        }

        private void LoadPasses()
        {
            if (!registrationGuid.HasValue && !registrantGuid.HasValue)
            {
                ShowNoPassesFound();
                return;
            }
            var qrBasePath = GlobalAttributesCache.Value( "PublicApplicationRoot" ) + "GetQRCode.ashx?data=";
            var alternateIdDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() );

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );
            var registrantService = new RegistrationRegistrantService( rockContext );

            var includeWaitList = GetAttributeValue( AttributeKey.IncludeWaitList ).AsBoolean();
            var includeRegistrarRegistrations = GetAttributeValue( AttributeKey.IncludeRegistrarRegistrations ).AsBoolean();

            // Used to order a deep-linked registrant first. The OrderBy below runs unconditionally,
            // so .Value would throw when the Registrant parameter is absent — Guid.Empty matches no row.
            var registrantGuidValue = registrantGuid ?? Guid.Empty;

            var passes = new List<EventPassData>();

            // Expansion is keyed to the authenticated viewer, never to the link: anonymous visitors
            // see only what the page-parameter guids name. For a logged-in viewer, anchor on the
            // registrants the guids unlock (after the waitlist filter, so a waitlist-only link stays
            // "not found") and add registrations in the same instance made by the viewer's family.
            var anchorRegistrantIds = new List<int>();
            var registrarPersonIds = new List<int>();
            var anchorRegistrationInstanceId = 0;
            var expand = false;
            if (includeRegistrarRegistrations && CurrentPersonId.HasValue)
            {
                // Expansion may only trigger from rows the guid-only path would show, so this query
                // must apply the same eligibility filters (waitlist, person alias) as the main query.
                var anchorQry = ApplyPageParameterFilters( registrantService.Queryable().AsNoTracking() );

                if (!includeWaitList)
                {
                    anchorQry = anchorQry.Where( r => !r.OnWaitList );
                }

                var anchors = anchorQry
                    .Where( r => r.PersonAliasId.HasValue )
                    .Select( r => new
                    {
                        r.Id,
                        r.Registration.RegistrationInstanceId
                    } )
                    .ToList();

                if (anchors.Any())
                {
                    expand = true;
                    anchorRegistrantIds = anchors.Select( a => a.Id ).ToList();

                    // The page parameters pin a single registrant or registration, so every anchor
                    // shares one registration instance.
                    anchorRegistrationInstanceId = anchors.First().RegistrationInstanceId;

                    registrarPersonIds = personService.GetFamilyMembers( CurrentPersonId.Value, true )
                        .Select( m => m.PersonId )
                        .ToList();

                    // GetFamilyMembers is built from family GroupMember rows, so a viewer with no
                    // family group record gets an empty list — include the viewer themselves so
                    // "registrar = me" always works.
                    if (!registrarPersonIds.Contains( CurrentPersonId.Value ))
                    {
                        registrarPersonIds.Add( CurrentPersonId.Value );
                    }
                }
            }

            // Registrants with no person alias are excluded everywhere: a pass is person-keyed
            // (QR = person search key) and the markup binds RegistrantPerson.*, so an alias-less
            // row can never render.
            var registrantQry = registrantService.Queryable()
                .AsNoTracking()
                .Include( r => r.Registration.RegistrationInstance )
                .Include( r => r.PersonAlias.Person )
                .Where( r => r.PersonAliasId.HasValue );

            if (expand)
            {
                // Keep every registrant the page parameters already unlocked (by id, so both sides
                // of the OR stay index-seekable), and add registrants in the same registration
                // instance whose registration was made by the viewer or the viewer's family
                // (a null registrar never matches the IN list).
                registrantQry = registrantQry.Where( r =>
                    anchorRegistrantIds.Contains( r.Id )
                    || ( r.Registration.RegistrationInstanceId == anchorRegistrationInstanceId
                        && registrarPersonIds.Contains( r.Registration.PersonAlias.PersonId ) ) );
            }
            else
            {
                registrantQry = ApplyPageParameterFilters( registrantQry );
            }

            if (!includeWaitList)
            {
                // Must stay even though the anchor query filters the waitlist too — this is what
                // hides waitlisted rows pulled in by the expansion branch.
                registrantQry = registrantQry.Where( r => !r.OnWaitList );
            }

            var registrants = registrantQry
                .OrderByDescending( r => r.Guid == registrantGuidValue )
                .ThenByDescending( r => r.Registration.PersonAliasId.HasValue
                    && r.PersonAlias.PersonId == r.Registration.PersonAlias.PersonId )
                .ThenBy( r => r.PersonAlias.Person.LastName )
                .ThenBy( r => r.PersonAlias.Person.NickName )
                .ToList();

            if (expand)
            {
                // A person could appear on multiple registrations (e.g. registered by both parents).
                // Only generate one pass per person. DistinctBy keeps the FIRST row under the sort
                // above, so the sort must keep the deep-linked registrant in front of duplicates.
                registrants = registrants
                    .DistinctBy( r => r.PersonAlias.PersonId )
                    .ToList();
            }

            if(!registrants.Any())
            {
                ShowNoPassesFound();
                return;
            }

            var registrationInstanceId = registrants.Select( r => r.Registration.RegistrationInstanceId ).FirstOrDefault();
            var occurrence = new EventItemOccurrenceGroupMapService( rockContext ).Queryable()
                .Include( o => o.EventItemOccurrence.Schedule )
                .Where( o => o.RegistrationInstanceId == registrationInstanceId )
                .Select( o => o.EventItemOccurrence )
                .FirstOrDefault();

            DateTime? occurrenceDate = null;
            string location = null;

            if(occurrence != null)
            {
                occurrenceDate = occurrence.Schedule?.GetFirstStartDateTime();
                location = occurrence.Location;
            }

            var personIds = registrants
                .Select( r => r.PersonAlias.PersonId )
                .Distinct()
                .ToList();

            var alternateIdLookup = new PersonSearchKeyService( rockContext ).Queryable().AsNoTracking()
                .Where( k => k.SearchTypeValueId == alternateIdDV.Id
                    && personIds.Contains( k.PersonAlias.PersonId ) )
                .Select( k => new { k.Id, k.PersonAlias.PersonId, k.SearchValue } )
                .ToList()
                .OrderBy( k => k.Id )
                .GroupBy( k => k.PersonId )
                .ToDictionary( g => g.Key, g => g.First().SearchValue );

            var itemOrder = 0;
            foreach (var registrant in registrants)
            {
                alternateIdLookup.TryGetValue( registrant.PersonAlias.PersonId, out string alternateId );

                var eventPassData = new EventPassData
                {
                    RegistrationTemplateId = registrant.Registration.RegistrationInstance.RegistrationTemplateId,
                    RegistrationInstanceId = registrant.Registration.RegistrationInstanceId,
                    RegistrationId = registrant.RegistrationId,
                    RegistrantId = registrant.Id,
                    RegistrantPerson = registrant.Person,
                    EventName = registrant.Registration.RegistrationInstance.Name,
                    QRUrl = $"{qrBasePath}{alternateId}",
                    ItemOrder = itemOrder,
                    EventDate = occurrenceDate,
                    EventLocation = location
                };

                passes.Add( eventPassData );
                itemOrder++;
            }

            isActiveItemSet = false;

            rPasses.DataSource = passes.OrderBy(p => p.ItemOrder);
            rPasses.DataBind();

            rPassIndicator.DataSource = passes.OrderBy( p => p.ItemOrder );
            rPassIndicator.DataBind();

            phCarouselControls.Visible = passes.Count > 1;

            rockContext = null;
            pnlPass.Visible = true;

        }

        /// <summary>
        /// Applies the Registration/Registrant page-parameter filters — the single definition of
        /// which registrant rows a link's guids unlock.
        /// </summary>
        private IQueryable<RegistrationRegistrant> ApplyPageParameterFilters( IQueryable<RegistrationRegistrant> qry )
        {
            if (registrantGuid.HasValue)
            {
                var registrantGuidValue = registrantGuid.Value;
                qry = qry.Where( r => r.Guid.Equals( registrantGuidValue ) );
            }

            if (registrationGuid.HasValue)
            {
                var registrationGuidValue = registrationGuid.Value;
                qry = qry.Where( r => r.Registration.Guid.Equals( registrationGuidValue ) );
            }

            return qry;
        }

        private void ShowNoPassesFound()
        {
            lPassNotFoundTitle.Text = GetAttributeValue( AttributeKey.PassNotFoundHeader );
            lPassNotFoundMessasge.Text = GetAttributeValue( AttributeKey.PassNotFoundMessage );
            pnlAlert.Visible = true;
        }

        public class EventPassData
        {
            public int RegistrationTemplateId { get; set; }
            public int RegistrationInstanceId { get; set; }
            public int RegistrationId { get; set; }
            public int RegistrantId { get; set; }
            public Person RegistrantPerson { get; set; }
            public string EventName { get; set; }
            public DateTime? EventDate { get; set; }
            public string EventLocation { get; set; }
            public string QRUrl { get; set; }
            public int ItemOrder { get; set; }

        }
    }
}