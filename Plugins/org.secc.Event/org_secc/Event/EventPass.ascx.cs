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
            public const string PassNotFoundHeader = "PassNotFoundHeader";
            public const string PassNotFoundMessage = "PassNotFoundMessage";

        }


        private int? registrationId = null;
        private int? registrationRegistrantId = null;
        bool isActiveItemSet = false;

        protected override void OnInit( EventArgs e )
        {
            rPasses.ItemDataBound += rPasses_ItemDataBound;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            registrationId = PageParameter( "Registration" ).AsIntegerOrNull();
            registrationRegistrantId = PageParameter( "Registrant" ).AsIntegerOrNull();
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
            if (!registrationId.HasValue && !registrationRegistrantId.HasValue)
            {
                ShowNoPassesFound();
                return;
            }
            var qrBasePath = GlobalAttributesCache.Value( "PublicApplicationRoot" ) + "GetQRCode.ashx?data=";
            var alternateIdDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() );

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );

            var personSearchKeyQry = new PersonSearchKeyService( rockContext ).Queryable().AsNoTracking()
                .Where( k => k.SearchTypeValueId == alternateIdDV.Id );


            var passes = new List<EventPassData>();


            var registrantQry = new RegistrationRegistrantService( rockContext ).Queryable()
                .AsNoTracking()
                .Include( r => r.Registration.RegistrationInstance )
                .Include( r => r.PersonAlias.Person );

            if (registrationRegistrantId.HasValue)
            {
                registrantQry = registrantQry.Where( r => r.Id.Equals( registrationRegistrantId.Value ) );
            }

            if (registrationId.HasValue)
            {
                registrantQry = registrantQry.Where( r => r.RegistrationId.Equals( registrationId.Value ) );
            }

            if (!GetAttributeValue( AttributeKey.IncludeWaitList ).AsBoolean())
            {
                registrantQry = registrantQry.Where( r => !r.OnWaitList );
            }

            var registrants = registrantQry
                .OrderByDescending( r => r.PersonAlias.PersonId == r.Registration.PersonAlias.PersonId )
                .ThenBy( r => r.PersonAlias.Person.LastName )
                .ThenBy( r => r.PersonAlias.Person.NickName )
                .ToList();

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
                occurrenceDate = occurrence.Schedule.GetFirstStartDateTime();
                location = occurrence.Location;
            }

            var itemOrder = 0;
            foreach (var registrant in registrants)
            {
                var alternateId = personSearchKeyQry.Where( k => k.PersonAlias.PersonId == registrant.PersonAlias.PersonId )
                    .Select( k => k.SearchValue ).FirstOrDefault();

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

            rockContext = null;
            pnlPass.Visible = true;

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