// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.ComponentModel;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using Rock.Data;
using System.Linq;
using System.Collections.Generic;
using Rock;
using Rock.Attribute;
using System.Web.UI.WebControls;

namespace RockWeb.Plugins.org_secc.FamilyOnMission
{
    [DisplayName( "FoM Signup" )]
    [Category( "SECC > Family On Misson" )]
    [Description( "Display for output of groups for Family On Mission" )]
    [GroupField( "Parent Group", "The parent group for the Family on Mission classes.", order:0 )]
    [LinkedPage( "Next Page", order:1, required:false )]
    [DateTimeField( "Open", order:2, required: false )]
    [DateTimeField( "Close", order: 3, required: false )]

    public partial class FoMSignup : RockBlock
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summarysni>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            bool registrationOpen = true;

            var open = GetAttributeValue( "Open" ).AsDateTime();
            var close = GetAttributeValue( "Close" ).AsDateTime();
            if ( open != null && Rock.RockDateTime.Now < open )
            {
                registrationOpen = false;
            }
            if ( close != null && Rock.RockDateTime.Now > close )
            {
                registrationOpen = false;
            }

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var url = LinkedPageUrl( "NextPage" );
            Group parentGroup = groupService.Get( GetAttributeValue( "ParentGroup" ).AsGuid() );
            if ( parentGroup == null )
            {
                return;
            }

            var childGroups = parentGroup.Groups.Where( g => g.Schedule != null ).GroupBy( g => g.Schedule.StartTimeOfDay ).OrderBy( k => k.Key ).ToList();
            foreach ( var set in childGroups.Where( g => g.Key != null ) )
            {
                Panel timePanel = new Panel();
                timePanel.CssClass = "col-md-4 time-panel";
                phContent.Controls.Add( timePanel );

                timePanel.Controls.Add( new LiteralControl()
                {
                    Text = string.Format( "<h1>{0}</h1>", set.Key.ToTimeString() )
                } );
                var setPanel = new Panel();
                setPanel.CssClass = "set-panel";
                timePanel.Controls.Add( setPanel );

                var membership = set.SelectMany( g => g.Members ).Where( gm => gm.PersonId == CurrentPersonId ).Select( gm => gm.Group );

                if ( !registrationOpen || membership.Any() )
                {
                    foreach ( var memberGroup in membership )
                    {
                        var panel = new Panel();
                        setPanel.Controls.Add( panel );
                        panel.CssClass = "class-panel";

                        memberGroup.LoadAttributes();

                        panel.Controls.Add( new Literal()
                        {
                            Text = string.Format( "<div class='content-panel'><h2>{0}</h2><b>{3} - {1}</b><br>{2}</div>",
                            memberGroup.Name,
                            memberGroup.GroupLocations.FirstOrDefault() != null ? memberGroup.GroupLocations.FirstOrDefault().Location.Name : "TBD",
                            memberGroup.Description,
                            memberGroup.GetAttributeValue( "Teacher" )
                            )
                        }
                        );
                        panel.Controls.Add( new Literal() );
                    }
                    continue;
                }

                foreach ( var item in set )
                {
                    if ( ( item.GroupCapacity - item.Members.Count() ) < 1 )
                    {
                        continue;
                    }

                    var panel = new Panel();
                    setPanel.Controls.Add( panel );
                    panel.CssClass = "class-panel";

                    item.LoadAttributes();

                    panel.Controls.Add( new Literal()
                    {
                        Text = string.Format( "<div class='pull-right label label-info'>{5} Spot(s) remaining</div>" +
                        "<div class='content-panel'><h2>{0}</h2>" +
                        "<b>{6} - {1}</b><br>{2}<br><br><a href='{3}?GroupId={4}' class='btn btn-primary'>Sign Up</a></div>" +
                        "<div class='fom-spacer'></div>",
                        item.Name,
                        item.GroupLocations.FirstOrDefault() != null ? item.GroupLocations.FirstOrDefault().Location.Name : "TBD",
                        item.Description,
                        url,
                        item.Id,
                        item.GroupCapacity - item.Members.Count(),
                        item.GetAttributeValue( "Teacher" )
                        )
                    }
                    );
                }
            }
        }
        #endregion
    }
}