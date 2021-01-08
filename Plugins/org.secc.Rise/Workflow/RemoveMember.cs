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
using System.Threading.Tasks;
using org.secc.Rise.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Rise.Workflow
{
    /// <summary>Removes member from Rise group via API</summary>
    /// <seealso cref="Rock.Workflow.ActionComponent"/>
    [ExportMetadata( "ComponentName", "Remove Member" )]
    [ActionCategory( "SECC > Rise" )]
    [Description( "Removes member from Rise group via API" )]
    [Export( typeof( ActionComponent ) )]

    [WorkflowAttribute( "Person", "Attribute of person to remove from group.", fieldTypeClassNames: new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [WorkflowAttribute( "Group", "Attribute of group.", fieldTypeClassNames: new string[] { "Rock.Field.Types.GroupFieldType" } )]

    public class RemoveMember : ActionComponent
    {
        /// <summary>Executes the specified context.</summary>
        /// <param name="context">The context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext context, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            RockContext rockContext = new RockContext();
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            var personAliasGuid = GetAttributeValue( action, "Person", true );
            var personAlias = personAliasService.Get( personAliasGuid.AsGuid() );
            if ( personAlias == null )
            {
                action.AddLogEntry( "Person attribute did not contain a person.", true );
                return false;
            }

            var person = personAlias.Person;
            person.LoadAttributes();
            var personRiseId = person.GetAttributeValue( Constants.PERSON_ATTRIBUTE_KEY_RISEID );
            if ( personRiseId.IsNullOrWhiteSpace() )
            {
                action.AddLogEntry( "Person is not currently in Rise. Person will by synced automatically when their Rise account is created.", true );
                return true; //We didn't fail.
            }


            var groupGuid = GetAttributeValue( action, "Group", true );
            var group = groupService.Get( groupGuid.AsGuid() );
            if ( group == null )
            {
                action.AddLogEntry( "Group attribute did not contain a group.", true );
                return false;
            }

            if ( group.GroupTypeId != GroupTypeCache.Get( Constants.GROUPTYPE_RISE ).Id )
            {
                action.AddLogEntry( "Group was not a Rise group type.", true );
                return false;
            }

            var task = Task.Run( () => //webforms doesn't play nice with async code
            {
                RiseClient riseClient = new RiseClient();
                var riseGroup = riseClient.GetOrCreateGroup( group );

                riseGroup.RemoveMember( personRiseId );
            } );

            task.Wait();

            return true;
        }
    }
}
