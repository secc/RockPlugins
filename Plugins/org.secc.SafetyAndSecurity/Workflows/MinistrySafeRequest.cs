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

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Create/update a user in Ministry Safe." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "MinistrySafe Create" )]
    [WorkflowAttribute( "Person", "The person to add to MinistrySafe.", true, "", "", 0, null, new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [WorkflowAttribute( "MinistrySafe URL", "The attribute to save the MinistrySafe direct login URL.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "MinistrySafe URL", "The attribute to save the MinistrySafe direct login URL.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [ValueListField( "MinistrySafe Tags", "The tags to set when the user is uploaded to MinistrySafe.", false)]
    class MinistrySafeRequest : ActionComponent
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

                var post = new RestRequest( "/users", Method.POST );
                post.AddHeader( "Authorization", "Token token=" + GlobalAttributesCache.Value( "MinistrySafeAPIToken" ) );
                post.AddJsonBody( new
                    {
                        user = new MinistrySafeUser() {
                            external_id = personAlias.Id.ToString(),
                            first_name = personAlias.Person.NickName,
                            last_name = personAlias.Person.LastName,
                            email = personAlias.Person.Email
                        },
                        tags = string.Join( ",", GetActionAttributeValue( action, "MinistrySafeTags" ).Trim( '|' ).Split( '|' ) )
                    }
                );
                var execution = client.Execute<bool>( post );
                
                if (execution.Data == true)
                { 
                    var request = new RestRequest( "/users/{ExternalId}", Method.GET );
                    request.AddHeader( "Authorization", "Token token=" + GlobalAttributesCache.Value( "MinistrySafeAPIToken" ) );
                    request.AddUrlSegment( "ExternalId", personAlias.Id.ToString() );
                    var tmp = client.Execute<MinistrySafeUser>( request );
                    var user = tmp.Data;

                    if ( user.direct_login_url != null )
                    {
                        SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "MinistrySafeURL" ).AsGuid(), user.direct_login_url );
                    }

                    return true;
                }
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
            public object complete_date { get; set; }
            public string direct_login_url { get; set; }
        }
    }
}
