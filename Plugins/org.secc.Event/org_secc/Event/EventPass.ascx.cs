using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.UI;
using Rock.Data;



namespace RockWeb.Plugins.org_secc.Event
{
    [DisplayName("Event Pass")]
    [Category("SECC > Event")]
    [Description("Displays a QR pass for registrants of an event.")]

    public partial class EventPass : RockBlock
    {

        bool isActiveItemSet = false;

        protected override void OnInit( EventArgs e )
        {

            rPasses.ItemDataBound += rPasses_ItemDataBound;
            string script = @"$('.carousel').carousel();";
            ScriptManager.RegisterClientScriptBlock( upMain, upMain.GetType(), "Carousel" + RockDateTime.Now.Ticks, script, true );

        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            LoadPasses();
        }

        private void rPasses_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if(!isActiveItemSet)
            {
                var pnlItem = e.Item.FindControl( "pnlItem" ) as Panel;
                pnlItem.CssClass = pnlItem.CssClass + " active";
                isActiveItemSet = true;
            }
        }

        private void LoadPasses()
        {
            var qrBasePath = GlobalAttributesCache.Value( "PublicApplicationRoot" ) + "GetQRCode.ashx?data=";
            var personIds = new int[] { 172382, 243950, 255085 };
            var alternateIdDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() );

            var rockContext = new RockContext();
            var personService = new PersonService( rockContext );

            var personSearchKeyQry = new PersonSearchKeyService( rockContext ).Queryable().AsNoTracking()
                .Where( k => k.SearchTypeValueId == alternateIdDV.Id );

  
            var passes = new List<EventPassData>();
            foreach (var personId in personIds)
            {
                var person = personService.Get( personId );
                var alternateId = personSearchKeyQry.Where( k => k.PersonAlias.PersonId == person.Id )
                    .Select( k => k.SearchValue ).FirstOrDefault();

                var eventPassData = new EventPassData
                {
                    RegistrationTemplateId = 1,
                    RegistrationInstanceId = 1,
                    RegistrationId = 1,
                    RegistrantId = 1,
                    RegistrantPerson = person,
                    EventName = "All Staff",
                    EventDate = new DateTime( 2024, 2, 13, 9, 30, 0 ),
                    EventLocation = "The Block",
                    QRUrl = $"{qrBasePath}{alternateId}"

                };

                passes.Add( eventPassData );
            }

            isActiveItemSet = false;

            rPasses.DataSource = passes;
            rPasses.DataBind();

            rockContext = null;

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

        }
    }
}