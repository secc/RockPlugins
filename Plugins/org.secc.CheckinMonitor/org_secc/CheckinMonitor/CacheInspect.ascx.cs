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
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using Rock.CheckIn;
using Rock.Attribute;
using org.secc.FamilyCheckin.Utilities;
using Rock.Web.UI;
using System.Data.Entity;
using System.Diagnostics;
using org.secc.FamilyCheckin.Cache;

namespace RockWeb.Plugins.org_secc.CheckinMonitor
{
    [DisplayName( "Cache Inspect" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays custom checkin cache values in a lovely grid." )]

    public partial class CacheInspect : RockBlock
    {
        protected override void OnLoad( EventArgs e )
        {
            gOccurrences.DataSource = OccurrenceCache.All();
            gOccurrences.DataBind();

            gAttendances.DataSource = AttendanceCache.All();
            gAttendances.DataBind();
        }

    }
}