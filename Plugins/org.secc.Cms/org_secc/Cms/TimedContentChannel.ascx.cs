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
using Rock;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using System.Web.UI.WebControls;
using Rock.Attribute;
using Rock.Web.UI.Controls;
using Rock.Data;
using System.Linq;
using System.Collections.Generic;

namespace RockWeb.Plugins.org_secc.CMS
{
    [DisplayName( "Timed Content Channel" )]
    [Category( "SECC > CMS" )]
    [Description( "Displays the items in a content channel with timer" )]
    [TextField( "Timer Attribute Key", "The key of the content channel with timer." )]
    [ContentChannelField( "Content Channel", "The content channel to display" )]
    [TextField( "Maximum Cache", "Maximum lenght to set cache in seconds." )]
    [CodeEditorField( "Lava", "The lava template to use.", CodeEditorMode.Lava )]
    [LavaCommandsField( "Enabled Lava Commands", "Additional lava commands to enable.", false )]
    public partial class TimedContentChannel : RockBlock
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                var output = RockCache.Get( "TimedContentChannel_" + this.BlockId.ToString() );
                if ( output != null )
                {
                    ltOutput.Text = output as string;
                    return;
                }

                RockContext rockContext = new RockContext();
                ContentChannelService contentChannelService = new ContentChannelService( rockContext );
                ScheduleService scheduleService = new ScheduleService( rockContext );

                var cacheLength = GetAttributeValue( "MaximumCache" ).AsDouble();

                var contentChannelGuid = GetAttributeValue( "ContentChannel" ).AsGuid();
                var contentChannel = contentChannelService.Get( contentChannelGuid );
                var contentChannelItems = contentChannel.Items.OrderBy( i => i.Priority ).ToList();
                List<ContentChannelItem> mergeItems = new List<ContentChannelItem>();
                var scheduleKey = GetAttributeValue( "TimerAttributeKey" );
                foreach ( var item in contentChannelItems )
                {
                    item.LoadAttributes();
                    var scheduleValue = item.GetAttributeValue( scheduleKey );
                    if ( scheduleValue.IsNotNullOrWhiteSpace() )
                    {
                        var schedule = scheduleService.Get( scheduleValue.AsGuid() );
                        if ( schedule == null )
                        {
                            continue;
                        }

                        if ( schedule.WasScheduleActive( Rock.RockDateTime.Now ) )
                        {
                            mergeItems.Add( item );
                            var end = schedule.GetCalendarEvent().DTEnd.TimeOfDay;
                            var endMinutes = ( end - Rock.RockDateTime.Now.TimeOfDay ).TotalSeconds;
                            cacheLength = Math.Min( cacheLength, endMinutes );
                        }
                        else
                        {
                            var nextTime = schedule.GetNextStartDateTime( Rock.RockDateTime.Now );
                            if ( nextTime.HasValue )
                            {
                                var time = nextTime.Value - Rock.RockDateTime.Now;
                                var minutes = time.TotalSeconds;
                                cacheLength = Math.Min( cacheLength, minutes );
                            }
                        }
                    }
                    else
                    {
                        mergeItems.Add( item );
                    }
                }
                var lava = GetAttributeValue( "Lava" );
                var mergefields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
                mergefields.Add( "ContentChannelItems", mergeItems );
                lava = lava.ResolveMergeFields( mergefields, CurrentPerson, GetAttributeValue( "EnabledLavaCommands" ) );
                ltOutput.Text = lava;
                RockCache.AddOrUpdate( "TimedContentChannel_" + this.BlockId.ToString(), "", lava, Rock.RockDateTime.Now.AddSeconds( cacheLength ) );
            }
        }

        #endregion

    }
}