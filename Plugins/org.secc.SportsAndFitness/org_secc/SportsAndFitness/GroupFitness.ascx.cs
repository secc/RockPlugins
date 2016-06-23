using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using System.Reflection;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Rock.Data;
using org.secc.FamilyCheckin.Utilities;

namespace RockWeb.Plugins.org_secc.SportsAndFitness
{
    [DisplayName( "Group Fitness Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Block to check into group fitness." )]
    [TextField( "Checkin Activity", "Activity for completing checkin.", false )]
    [AttributeField( Rock.SystemGuid.EntityType.GROUP_MEMBER, "Sessions Attribute", "Select the attribute used to filter by number of sessions.", true, false, order: 3 )]
    [GroupField( "Group Fitness Group", "Group that group fitness members are in. Needed for reading the number of sessions left.", true )]

    public partial class GroupFitness : CheckInBlock
    {

        private RockContext _rockContext;
        private string _groupSessionsKey;

        private List<GroupTypeCache> parentGroupTypesList;
        private GroupTypeCache currentParentGroupType;
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !KioskCurrentlyActive )
            {
                NavigateToHomePage();
            }

            _rockContext = new RockContext();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var groupSessionsAttributeGuid = GetAttributeValue( "SessionsAttribute" ).AsGuid();
            if ( groupSessionsAttributeGuid != Guid.Empty )
            {
                _groupSessionsKey = AttributeCache.Read( groupSessionsAttributeGuid, _rockContext ).Key;
            }

            if ( !Page.IsPostBack )
            {
                this.Page.Form.DefaultButton = lbSearch.UniqueID;

                if ( CurrentCheckInState == null )
                {
                    NavigateToPreviousPage();
                    return;
                }

                CurrentCheckInState.CheckIn = new CheckInStatus();
                SaveState();
                //people can be preselected coming out of the workflow, we want to unselect them
            }

        }



        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if ( string.IsNullOrWhiteSpace( tbPhone.Text ) )
            { return; }
            CurrentCheckInState.CheckIn.SearchValue = tbPhone.Text;
            CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER ); //this is a lie
            List<string> errors = new List<string>();
            try
            {
                bool test = ProcessActivity( GetAttributeValue( "WorkflowActivity" ), out errors );
            }
            catch
            {
                NavigateToPreviousPage();
                Response.End();
                return;
            }
            if ( CurrentCheckInState.CheckIn.CurrentFamily == null
                || CurrentCheckInState.CheckIn.CurrentPerson == null )
            {
                ShowPersonNotFound();
            }
            else
            {
                ShowPersonCheckin();
            }

        }

        private void ShowPersonCheckin()
        {
            pnlNotFound.Visible = false;
            pnlSearch.Visible = false;
            pnlCheckin.Visible = true;
            ltNickName.Text = CurrentCheckInState.CheckIn.CurrentPerson.Person.NickName;
        }

        private void ShowPersonNotFound()
        {
            pnlCheckin.Visible = false;
            pnlSearch.Visible = false;
            pnlNotFound.Visible = true;
        }

        protected void Timer1_Tick( object sender, EventArgs e )
        {

        }

        private Rock.Model.Group GetMembershipGroup( CheckInPerson person, Rock.Model.Group group = null )
        {
            string membershipGroupGuid = null;

            if ( group != null )
            {
                group.LoadAttributes();
                membershipGroupGuid = group.GetAttributeValue( "Group" );
            }


            if ( string.IsNullOrWhiteSpace( membershipGroupGuid ) )
            {
                membershipGroupGuid = GetAttributeValue( "Group Fitness Group" );
                if ( string.IsNullOrWhiteSpace( membershipGroupGuid ) )
                {
                    return null;
                }
            }
            var membershipGroup = new GroupService( _rockContext ).Get( membershipGroupGuid.AsGuid() );
            return membershipGroup;
        }
    }
}