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
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Attribute;
using RestSharp;
using Rock;
using System.Linq;
using Rock.Web.Cache;
using System;

namespace org.secc.MinistrySafe
{
    [ActionCategory( "SECC > MinistrySafe" )]
    [Description( "Update results from a user in Ministry Safe." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "MinistrySafe Update" )]
    [WorkflowAttribute( "Person", "The person to add to MinistrySafe.", true, "", "", 0, null, new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [WorkflowAttribute( "Score", "The attribute to save the MinistrySafe score.", true, "", "", 0, null, new string[] { "Rock.Field.Types.IntegerFieldType" } )]
    [WorkflowAttribute( "Completion Date", "The attribute to save the MinistrySafe completion date.", true, "", "", 0, null, new string[] { "Rock.Field.Types.DateFieldType" } )]
    
    class MinistrySafeUpdate : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            
            errorMessages = new List<string>();

            var personAliasGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "Person" ).AsGuid() ).AsGuidOrNull();
            if ( personAliasGuid.HasValue )
            {
                PersonAliasService personAliasService = new PersonAliasService( new RockContext() );
                PersonAlias personAlias = personAliasService.Get( personAliasGuid.Value );
                
                var client = new RestClient( GlobalAttributesCache.Value( "MinistrySafeAPIURL" ) );
                
                var request = new RestRequest( "/users/{ExternalId}", Method.GET );
                request.AddHeader( "Authorization", "Token token=" + GlobalAttributesCache.Value( "MinistrySafeAPIToken" ) );
                request.AddUrlSegment( "ExternalId", personAlias.Id.ToString() );
                var tmp = client.Execute<MinistrySafeUser>( request );
                var user = tmp.Data;

                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "Score" ).AsGuid(), user.score.ToString() );
                if (user.complete_date != null)
                {
                    SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "CompletionDate" ).AsGuid(), user.complete_date.ToString() );
                }

                return true;
            }
            return false;

        }

        private class MinistrySafeUser
        { 
            public string email { get; set; }
            public string employee_id { get; set; }
            public string external_id { get; set; }
            public string first_name { get; set; }
            public string last_name { get; set; }
            public int score { get; set; }
            public DateTime complete_date { get; set; }
            public string direct_login_url { get; set; }
        }
    }
}
