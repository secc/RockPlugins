// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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
using System.ComponentModel;
using System.Linq;

using Rock;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using System.Web.UI.HtmlControls;
using Rock.Data;
using org.secc.FamilyCheckin.Utilities;
using Rock.Attribute;
using System.Data.Entity;
using System.Text;
using System.Diagnostics;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Cache Test" )]
    [Category( "SECC > Check-in" )]
    [Description( "Tool for testing check-in cache sanity" )]

    public partial class CacheTest : Rock.Web.UI.RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var today = Rock.RockDateTime.Today;
            var tomorrow = today.AddDays( 1 );

            var attendances = attendanceService.Queryable().Where( a => a.StartDateTime > Rock.RockDateTime.Today && a.StartDateTime < tomorrow && a.EndDateTime == null )
                .GroupBy( a => a.LocationId );
            foreach ( var grouping in attendances )
            {
                var cache = CheckInCountCache.GetByLocation( grouping.Key ?? 0 );
                var cachecount = cache.SelectMany( c => c.PersonIds ).Count();
                var attendanceCount = grouping.Count();
                var text = string.Format( "<br> LocationId: {0} - Cache:{1}, Actual: {2}", grouping.Key, cachecount, attendanceCount );
                if ( cachecount != attendanceCount )
                {
                    text = "<b>" + text + "</b>";
                }
                phOutput.Controls.Add( new Literal()
                {
                    Text = text
                } );

                if ( cachecount != attendanceCount )
                {
                    var cachePersonIds = cache.SelectMany( c => c.PersonIds ).ToList();
                    foreach ( var actualAttendance in grouping )
                    {
                        if ( !cachePersonIds.Contains( actualAttendance.PersonAlias.PersonId ) )
                        {
                            phOutput.Controls.Add( new Literal
                            {
                                Text = string.Format( "<br>--- Actual: Group: {0}, Location: {1}, Person {2    }",
                                actualAttendance.Group != null ? actualAttendance.Group.Name : "None",
                                actualAttendance.Location != null ? actualAttendance.Location.Name : "None",
                                actualAttendance.PersonAlias.PersonId )
                            }
                                );
                        }
                    }
                    foreach ( var cacheAttendance in grouping )
                    {
                        if ( !grouping.Select( a => a.PersonAlias.PersonId ).Contains( cacheAttendance.PersonAlias.PersonId ) )
                        {
                            phOutput.Controls.Add( new Literal
                            {
                                Text = string.Format( "<br>--- Cache: Group: {0}, Location: {1}, Person {2}",
                                cacheAttendance.Group != null ? cacheAttendance.Group.Name : "None",
                                cacheAttendance.Location != null ? cacheAttendance.Location.Name : "None",
                                cacheAttendance.PersonAlias.PersonId )
                            }
                                );
                        }
                    }
                }

            }
        }
    }
}