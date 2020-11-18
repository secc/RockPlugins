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
using Newtonsoft.Json;
using org.secc.Rise.Data;
using org.secc.Rise.Response.Event;
using Rock.Data;

namespace org.secc.Rise.Model
{
    /// <summary></summary>
    /// <seealso cref="org.secc.Rise.Data.RiseService{org.secc.Rise.Model.WebhookEvent}" />
    public class WebhookEventService : RiseService<WebhookEvent>
    {
        public WebhookEventService( RockContext context ) : base( context )
        {
        }

        public static bool TryReserveWebhookEvent( string eventContent )
        {
            RockContext rockContext = new RockContext();
            WebhookEventService webhookEventService = new WebhookEventService( rockContext );

            var eventObject = JsonConvert.DeserializeObject<WebhookEventBase>( eventContent );
            var eventId = eventObject.Id;

            var webhookEvent = new WebhookEvent
            {
                EventId = eventId,
                Content = eventContent,
                Count = 1
            };

            webhookEventService.Add( webhookEvent );

            try
            {
                rockContext.SaveChanges();
                return true;
            }
            catch
            {
                //We are not the first, increment the event count.
                rockContext.Database
                    .ExecuteSqlCommand( "UPDATE [_org_secc_Rise_WebhookEvent] SET [Count] = [Count] + 1 WHERE [EventId] = @eventId",
                                     new System.Data.SqlClient.SqlParameter( "@eventId", eventId ) );
                return false;
            }
        }
    }
}
