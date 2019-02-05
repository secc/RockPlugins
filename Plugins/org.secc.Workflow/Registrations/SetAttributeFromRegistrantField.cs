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

using Rock.Data;
using Rock.Model;
using Rock.Attribute;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock;
using System.Linq;
using System.Data.Entity;

namespace org.secc.Workflow.Registrations
{
    /// <summary>
    /// Activates all the actions for the current action's activity.
    /// </summary>
    [ActionCategory( "SECC > Registrations" )]
    [Description( "Sets an Attribute Value from a registrant field value using the Attribute Key and a registrant index." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Set Attribute from Registrant Field" )]

    [WorkflowAttribute( "Workflow Attribute", "The Workflow Attribute to set")]
    [WorkflowTextOrAttribute( "Field Key", "Field Key", "The registration template form field attribute key. <span class='tip tip-lava'></span>", true, key: "FieldKey" )]
    [WorkflowTextOrAttribute( "Registrant Index", "Registrant Index", "The Registrant index. <span class='tip tip-lava'></span>", true, "0", key: "RegistrantIndex", fieldTypeClassNames: new string[] { "Rock.Field.Types.IntegerFieldType" } )]
    public class SetAttributeFromRegistrantField : ActionComponent
    {
        /// <summary>
        /// Applies a discount code to a registration entity
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            if ( entity is Dictionary<string,object> entityDictionary )
            {
                RegistrationInstance registrationInstanceState = ( RegistrationInstance) entityDictionary["RegistrationInstance"];
                RegistrationInfo registrationState = ( RegistrationInfo ) entityDictionary["RegistrationInfo"];
                if ( registrationState != null )
                {
                    string fieldKey = GetAttributeValue( action, "FieldKey", true ).ResolveMergeFields( GetMergeFields( action ) );
                    int registrantIndex = GetAttributeValue( action, "RegistrantIndex", true ).ResolveMergeFields( GetMergeFields( action ) ).AsInteger();

                    var formField = registrationInstanceState.RegistrationTemplate.Forms.Select( f => f.Fields.Where( ff => ff.Attribute != null && ff.Attribute.Key == fieldKey ).FirstOrDefault() ).FirstOrDefault();
                    var fieldValue = registrationState.Registrants[registrantIndex].FieldValues.Where( fv => fv.Key == formField.Id ).Select( fv => fv.Value.FieldValue ).FirstOrDefault();

                    // Now store the attribute
                    var targetAttribute = AttributeCache.Get( GetActionAttributeValue( action, "WorkflowAttribute" ).AsGuid(), rockContext );
                    if ( targetAttribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
                    {
                        action.Activity.Workflow.SetAttributeValue( targetAttribute.Key, fieldValue.ToString() );
                    }
                    else if ( targetAttribute.EntityTypeId == new WorkflowActivity().TypeId )
                    {
                        action.Activity.SetAttributeValue( targetAttribute.Key, fieldValue.ToString() );
                    }

                }
            }

            return true;
        }
    }
}