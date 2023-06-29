using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Text;

using org.secc.DevLib.SportsAndFitness;

namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{
    [DisplayName("Search")]
    [Category("Sports and Fitness > Control Center")]
    [Description("Block used to search for Search and Fitness participants")]

    [BooleanField("Search By PIN",
        Description = "Search for Sports and Fitness Participants by their PIN number.",
        DefaultBooleanValue = true,
        Order = 0,
        Key = AttributeKey.SearchByPIN)]
    [BooleanField("Search By Phone Number",
        Description = "Search for Participants by their phone number.",
        DefaultBooleanValue = true,
        Order = 1,
        Key = AttributeKey.SearchByPhone)]
    [LinkedPage("Search Result Page",
        Description = "Search Result Page",
        Order = 2,
        Key = AttributeKey.SearchResultPage)]

    public partial class Search : RockBlock
    {
        #region Attribute Keys
        private static class AttributeKey
        {
            public const string SearchByPhone = "SearchByPhone";
            public const string SearchByPIN = "SearchByPIN";
            public const string SearchResultPage = "SearchResultPage";
        }
        #endregion

        const string SearchKey = "ControlCenterSearch";

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Search_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upMain );
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if(!IsPostBack)
            {
                if (Session[SearchKey] != null)
                {
                    Session.Remove( SearchKey );
                }
                LoadDetails();
            }
            else
            {
                HandleCustomPostBack();
            }
        }
        #endregion Base Control Methods

        #region Events
        private void Search_BlockUpdated( object sender, EventArgs e )
        {
            NavigateToCurrentPage();
        }

        #endregion Events

        #region Internal Methods

        private void HandleCustomPostBack()
        {
            var args = this.Request.Params["__EVENTARGUMENT"];
            if(args == "search" && tbSearch.Text.Length > 2)
            {
                var searchConfiguration = new ControlCenterSearchItem
                {
                    SearchTerm = tbSearch.Text.Trim(),
                    SearchByPhone = GetAttributeValue( AttributeKey.SearchByPhone ).AsBoolean(), 
                    SearchByPIN = GetAttributeValue( AttributeKey.SearchByPIN ).AsBoolean()
                };


                Session[SearchKey] = searchConfiguration.ToString();
                NavigateToLinkedPage( AttributeKey.SearchResultPage );
            }   
        }

        private void LoadDetails()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append( "Search By Name" );
            if(GetAttributeValue(AttributeKey.SearchByPhone).AsBoolean())
            {
                stringBuilder.Append( ", Phone" );
            }
            if(GetAttributeValue(AttributeKey.SearchByPIN).AsBoolean())
            {
                stringBuilder.Append( ", PIN" );
            }
            tbSearch.Placeholder = stringBuilder.ToString();

        }

        #endregion

    }
}