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
using org.secc.Communication.Twilio;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;

namespace org.secc.Communication.Jobs
{
    [IntegerField( "Days Back", "The number of days back (including today) to sync.", defaultValue: 2 )]

    [DisallowConcurrentExecution]
    public class SycnTwilioHistory : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            var daysBack = dataMap.GetString( "DaysBack" ).AsInteger();
            if ( daysBack == 0 )
            {
                daysBack = 2;
            }

            int syncCount = 0;

            for ( var i = 0; i < daysBack; i++ )
            {
                TwilioDownloader twilioDownloader = new TwilioDownloader();
                syncCount += twilioDownloader.SyncItems( RockDateTime.Today.AddDays( i * -1 ) );

                rockContext.SaveChanges();

                context.Result = string.Format( $"Synced {syncCount} text messages between {RockDateTime.Today.AddDays( 1 - daysBack ).ToString( "M/d/yyy" )} and {RockDateTime.Today.ToString( "M/d/yyy" )}" );
            }

        }
    }
}
