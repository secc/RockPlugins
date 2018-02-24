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
    [DisplayName( "Checkin Load Test" )]
    [Category( "SECC > Check-in" )]
    [Description( "Tool for testing check-in under load" )]

    public partial class CheckinLoadTest : CheckInBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                var CurrentKioskId = ( int ) Session["CheckInKioskId"];
                List<int> CheckInGroupTypeIds = ( List<int> ) Session["CheckInGroupTypeIds"];
                CurrentCheckInState = new CheckInState( CurrentKioskId, null, CheckInGroupTypeIds );
                CurrentCheckInState.CheckIn.UserEnteredSearch = true;
                CurrentCheckInState.CheckIn.ConfirmSingleFamily = true;
                CurrentCheckInState.CheckIn.SearchType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CHECKIN_SEARCH_TYPE_PHONE_NUMBER );

                Random rnd = new Random();

                StringBuilder output = new StringBuilder( "Search Results:<hr>" );
                Stopwatch sw = new Stopwatch();

                int count = 0;
                for ( var i = 0; i < 10; i++ )
                {
                    var searchString = LongRandom( 0000, 9999999999, rnd );
                    if ( i % 2 == 0 )
                    {
                        searchString = searchString.Substring( 0, 4 );
                    }
                    CurrentCheckInState.CheckIn.SearchValue = searchString;

                    output.Append( "<br>Searched: " + searchString );
                    sw.Reset();
                    sw.Start();

                    List<string> errors = new List<string>();
                    string workflowActivity = GetAttributeValue( "WorkflowActivity" );
                    if ( string.IsNullOrWhiteSpace( workflowActivity ) )
                    {
                        ltErrors.Text = "Block not configured";
                        return;
                    }
                    try
                    {
                        bool test = ProcessActivity( workflowActivity, out errors );
                        count++;
                    }
                    catch ( Exception ex )
                    {
                        ltErrors.Text = "Error:" + ex.Message;
                        return;
                    }
                    sw.Stop();
                    output.Append( " in " + sw.ElapsedMilliseconds + "ms" );
                }

                ltErrors.Text = output.ToString();
                ScriptManager.RegisterStartupScript( upContent, upContent.GetType(), "reload", "reload();", true );
            }
        }

        string LongRandom( long min, long max, Random rand )
        {
            long result = rand.Next( ( Int32 ) ( min >> 32 ), ( Int32 ) ( max >> 32 ) );
            result = ( result << 32 );
            result = result | ( long ) rand.Next( ( Int32 ) min, ( Int32 ) max );
            return result.ToString();
        }
    }
}