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
using System.Linq;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Attribute;
using Rock.Workflow;
using System.Web;

namespace RockWeb.Plugins.org_secc.SignNow
{
    /// <summary>
    /// Handles a landing action from SignNow which will update a 
    /// semaphore in the workflow and then redirect back to the workflow
    /// </summary>
    [DisplayName( "SignNow Landing" )]
    [Category( "SECC > SignNow" )]
    [Description( "Handles the SignNow landing activity and updates the associated workflow." )]
    [LinkedPage( "Workflow Entry Page", "The page containing the original workflow entry form.", true )]
    public partial class Landing : Rock.Web.UI.RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            RockContext rockContext = new RockContext();

            Guid? workflowGuid = PageParameter( "Workflow" ).AsGuidOrNull();
            Guid? landingActionGuid = PageParameter( "LandingAction" ).AsGuidOrNull();
            string documentId = PageParameter( "document_id" );

            if (workflowGuid == null)
            {
                nbError.Text = "Error: A valid workflow parameter is required.";
                nbError.Visible = true;
                return;
            }
            if (landingActionGuid == null)
            {
                nbError.Text = "Error: A valid landing action parameter is required.";
                nbError.Visible = true;
                return;
            }
            if (string.IsNullOrWhiteSpace( documentId ))
            {
                nbError.Text = "Error: A valid DocumentId parameter is required.";
                nbError.Visible = true;
                return;
            }

            // Load the Workflow
            WorkflowService workflowService = new WorkflowService( rockContext );
            Workflow workflow = workflowService.Get( workflowGuid.Value );
            if (workflow == null)
            {
                nbError.Text = "Error: A valid workflow parameter is required.";
                nbError.Visible = true;
                return;
            }

            workflow.LoadAttributes();

            WorkflowAction landingAction = workflow.Activities.SelectMany( a => a.Actions.Where( x => x.ActionType.Guid == landingActionGuid ) ).FirstOrDefault();

            if (landingAction == null)
            {
                nbError.Text = "Error: A valid landing action parameter is required.";
                nbError.Visible = true;
                return;
            }
            landingAction.LoadAttributes();

            // Load the attributes from the landing action and then update them
            var documentIdAttribute = AttributeCache.Get( ActionComponent.GetActionAttributeValue( landingAction, "SignNowDocumentId" ).AsGuid(), rockContext );

            if (documentIdAttribute.EntityTypeId == new Workflow().TypeId)
            {
                workflow.SetAttributeValue( documentIdAttribute.Key, documentId );
            }
            else if (documentIdAttribute.EntityTypeId == new WorkflowActivity().TypeId)
            {
                landingAction.Activity.SetAttributeValue( documentIdAttribute.Key, documentId );
            }

            var pdfSignedAttribute = AttributeCache.Get( ActionComponent.GetActionAttributeValue( landingAction, "PDFSigned" ).AsGuid(), rockContext );
            if (pdfSignedAttribute.EntityTypeId == new Workflow().TypeId)
            {
                workflow.SetAttributeValue( pdfSignedAttribute.Key, "True" );
            }
            else if (pdfSignedAttribute.EntityTypeId == new WorkflowActivity().TypeId)
            {
                landingAction.Activity.SetAttributeValue( documentIdAttribute.Key, "True" );
            }

            workflow.SaveAttributeValues();
            landingAction.SaveAttributeValues();

            // Process the workflow
            List<string> errorMessages;
            var output = workflowService.Process( workflow, out errorMessages );

            if (!HttpContext.Current.Response.IsRequestBeingRedirected)
            {
                // Redirect back to the workflow
                NavigateToLinkedPage( "WorkflowEntryPage", new Dictionary<string, string>() { { "WorkflowTypeId", workflow.TypeId.ToString() }, { "WorkflowGuid", workflowGuid.ToString() } } );
            }

        }
    }
}
