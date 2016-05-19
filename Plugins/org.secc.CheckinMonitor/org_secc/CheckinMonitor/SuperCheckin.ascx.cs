using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Super Checkin" )]
    [Category( "SECC > Check-in" )]
    [Description( "Advanced tool for managing checkin." )]
    public partial class SuperCheckin : CheckInBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if ( !KioskCurrentlyActive )
            {
                NavigateToHomePage();
            }
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                if ( CurrentCheckInState.CheckIn.Families.Where( f => f.Selected ).Any() )
                {
                    pnlManageFamily.Visible = true;
                    ActivateFamily();
                }
                else
                {
                    pnlNewFamily.Visible = true;
                }
            }
        }

        private void ActivateFamily()
        {
            List<string> errorMessages = new List<string>();
            ProcessActivity( GetAttributeValue( "WorkflowActivity" ), out errorMessages );
        }
    }
}