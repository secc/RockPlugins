// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Workflow;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Handle storing the SSN." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Volunteer Application SSN" )]
    class VolunteerApplicationSSN : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // null means the boxes still match the stored SSN. Invalid input isn't stored;
            // Volunteer Application Validation reports it along with the form's other errors.
            string ssn = SsnHelper.ResolveSsn( rockContext, action.Activity.Workflow );

            if ( ssn != null && SsnHelper.IsValidSsnFormat( ssn ) )
            {
                PersonAliasService personAliasService = new PersonAliasService( rockContext );
                // Store the SSN and then set it to ***'s
                PersonAlias personAlias = personAliasService.Get( action.Activity.Workflow.GetAttributeValue( "Person" ).AsGuid() );
                personAlias.Person.LoadAttributes();
                personAlias.Person.SetAttributeValue( "SSN", Encryption.EncryptString( ssn ) );
                personAlias.Person.SaveAttributeValues();
                action.Activity.Workflow.SetAttributeValue( "SSN1", Encryption.EncryptString( "***" ) );
                action.Activity.Workflow.SetAttributeValue( "SSN2", Encryption.EncryptString( "**" ) );
                rockContext.SaveChanges();
            }

            return true;
        }
    }
}
