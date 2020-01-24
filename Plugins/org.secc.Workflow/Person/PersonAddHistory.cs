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
using System.ComponentModel.Composition;
using System.Linq;
using System.Data.Entity;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.Person
{
    /// <summary>
    /// Adds history record to the selected person.
    /// </summary>
    [ActionCategory( "SECC > People" )]
    [Description( "Adds history record to the selected person." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Person History Add" )]
    [WorkflowAttribute( "Person", "Workflow attribute that contains the person to add the note to.", true, "", "", 0, null, new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [CategoryField( "Category", "Category for the history entry.", false, "Rock.Model.History" )]
    [TextField( "Caption", "The history entry's caption. <span class='tip tip-lava'></span>", false, "", "", 2 )]
    [TextField( "Verb", "The verb describing the action. <span class='tip tip-lava'></span>", false, "", "", 3 )]
    [MemoField( "Summary", "Summary of the history entry. <span class='tip tip-lava'></span>", true, "", "", 4 )]
    public class PersonAddHistory : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var personGuid = GetAttributeValue( action, "Person", true ).AsGuidOrNull();
            if ( personGuid == null )
            {
                errorMessages.Add( "Person Add History requires a valid person" );
                return false;
            }

            var categoryGuid = GetAttributeValue( action, "Category" ).AsGuid();
            var category = new CategoryService( rockContext ).Get( categoryGuid );
            if ( category == null )
            {
                errorMessages.Add( "Person Add History requires a valid category" );
                return false;
            }

            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            var personAlias = personAliasService.Get( personGuid.Value );

            if ( personAlias != null )
            {
                var person = personAlias.Person;
                var entityTypeId = EntityTypeCache.GetId( typeof( Rock.Model.Person ) );
                var workflowEntityTypeId = EntityTypeCache.GetId( typeof( Rock.Model.Workflow ) );
                var mergeFields = GetMergeFields( action );
                var caption = GetAttributeValue( action, "Caption" ).ResolveMergeFields( mergeFields );
                var summary = GetAttributeValue( action, "Summary" ).ResolveMergeFields( mergeFields );
                var verb = GetAttributeValue( action, "Verb" ).ResolveMergeFields( mergeFields );
                HistoryService historyService = new HistoryService( rockContext );
                History history = new History
                {
                    Caption = caption,
                    Summary = summary,
                    Verb = verb,
                    EntityId = person.Id,
                    EntityTypeId = entityTypeId.Value,
                    CategoryId = category.Id
                };
                if ( action?.Activity?.Workflow != null && action.Activity.WorkflowId != 0 )
                {
                    history.RelatedEntityTypeId = workflowEntityTypeId;
                    history.RelatedEntityId = action.Activity.WorkflowId;
                }
                historyService.Add( history );
                rockContext.SaveChanges();

                return true;
            }
            else
            {
                errorMessages.Add( "Person Add History requires a valid person" );
                return false;
            }
        }
    }
}