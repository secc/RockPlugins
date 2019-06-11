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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Attribute;

namespace org.secc.SignNowWorkflow
{
    [ActionCategory( "SECC > Sign Now" )]
    [ExportMetadata( "ComponentName", "SignNow Landing" )]
    [Description( "This workflow action works alongside a SignNow landing block which is connected to this workflow activity." )]
    [Export( typeof( ActionComponent ) )]
    [WorkflowAttribute( "SignNow Document Id", "The attribute which contains the document to check.", true, "", "", 0, null, new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "PDF Signed", "Indicator that we have a signed document from SignNow.", true, "", "", 0, null, new string[] { "Rock.Field.Types.BooleanFieldType" } )]
    class SignNowLanding : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // Check to see if the action's activity does not have the the 'SignNowDocumentId' attribute.
            string signNowDocumentId = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "SignNowDocumentId" ).AsGuid() );
            if ( string.IsNullOrEmpty( signNowDocumentId ) )
            {
                errorMessages.Add( "A sign now document is required for this action" );
                return false;
            }

            // Check to see if the document has been signed
            bool documentSignedFlag = action.GetWorklowAttributeValue(GetActionAttributeValue(action, "PDFSigned").AsGuid()).AsBoolean();
            if (!documentSignedFlag)
            {
                // We can't move on until the document is signed.
                return false;
            }

            // If we get this far, the document is signed and it's time to move on.
            return true;
        }
    }
}
