using System;
using System.ComponentModel;
using System.Collections.Generic;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Crm
{
    [DisplayName( "My Account - Generate Apple Pass" )]
    [Category( "SECC > CRM" )]
    [Description( "Generate an Apple Pass" )]

    [IntegerField( "Pass Template Id",
        Description = "The template id for the Apple Pass Template,",
        IsRequired = false,
        Key = AttributeKeys.PassTemplateKey,
        Order = 0 )]
    [WorkflowTypeField( "Apple Pass Workflow",
        Description = "The workflow that generates the Apple Pass.",
        AllowMultiple = false,
        IsRequired = true,
        Order = 1,
        Key = AttributeKeys.ApplePassWorkflowKey )]
    [AttributeField( "Event Pass Person Attribute",
        Description = "Person Attribute that contains the Event Pass.",
        IsRequired = true,
        EntityTypeGuid = Rock.SystemGuid.EntityType.PERSON,
        AllowMultiple = false,
        Order = 2,
        Key = AttributeKeys.PassPersonAttributeKey )]
    public partial class GenerateApplePass : RockBlock
    {
        public static class AttributeKeys
        {
            public const string PassTemplateKey = "PassTemplateId";
            public const string ApplePassWorkflowKey = "ApplePassWorkflow";
            public const string PassPersonAttributeKey = "EventPassAttribute";
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (!IsPostBack)
            {
                VerifyAttributes();
            }
        }

        protected void lbGeneratePass_Click( object sender, EventArgs e )
        {
            Guid fileGuid = Guid.Empty;
            if (hfPassInfo.Value.IsNullOrWhiteSpace())
            {
                return;
            }

            var personAliasGuid = hfPassInfo.Value.AsGuidOrNull();

            if (!personAliasGuid.HasValue)
            {
                return;
            }

            using (var rockContext = new RockContext())
            {
                var personAliasService = new PersonAliasService( rockContext );
                var person = personAliasService.GetPerson( personAliasGuid.Value );
                if (person == null)
                {
                    return;
                }

                var passAttribute = AttributeCache.Get( GetAttributeValue( AttributeKeys.PassPersonAttributeKey ).AsGuid() );
                person.LoadAttributes( rockContext );
                fileGuid = person.GetAttributeValue( passAttribute.Guid ).AsGuid();


                if (fileGuid == Guid.Empty)
                {
                    var passWorkflowType = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.ApplePassWorkflowKey ).AsGuid() );
                    var workflow = Rock.Model.Workflow.Activate( passWorkflowType, $"Apple Pass - {person.FullName}" );
                    workflow.SetAttributeValue( "PassTemplateId", GetAttributeValue( AttributeKeys.PassTemplateKey ).AsInteger() );
                    workflow.SetAttributeValue( "DeliveryPreference", "None" );

                    List<string> workflowErrors;
                    new WorkflowService( rockContext ).Process( workflow, person, out workflowErrors );

                    workflow.LoadAttributes( rockContext );
                    fileGuid = workflow.GetAttributeValue( "PassFile" ).AsGuid();
                }

                if(fileGuid != Guid.Empty)
                {
                    var rockUrl = ResolveRockUrl( $"~/GetFile.ashx?guid={fileGuid}&attachment=true" );
                    Response.Redirect( rockUrl, false );
                }
            }
        }

        private void VerifyAttributes()
        {
            pnlValidation.Visible = false;
            var sbErrors = new System.Text.StringBuilder();
            var passTemplateId = GetAttributeValue( AttributeKeys.PassTemplateKey ).AsIntegerOrNull();
            var applePassWorkflowTypeGuid = GetAttributeValue( AttributeKeys.ApplePassWorkflowKey ).AsGuidOrNull();
            var passPersonAttributeGuid = GetAttributeValue( AttributeKeys.PassPersonAttributeKey ).AsGuidOrNull();

            if (!passTemplateId.HasValue)
            {
                sbErrors.Append( "<li>Pass Template Id is required.</li>" );
            }

            if (!applePassWorkflowTypeGuid.HasValue)
            {
                sbErrors.Append( "<li>Pass Workflow Type is required.</li>" );
            }
            else
            {
                var passWorkflowType = WorkflowTypeCache.Get( applePassWorkflowTypeGuid.Value );
                if (passWorkflowType == null)
                {
                    sbErrors.Append( "<li>Apple Pass Workflow is not found.</li>" );
                }
            }

            if (!passPersonAttributeGuid.HasValue)
            {
                sbErrors.Append( "<li>Event Pass Person Attribute is required.</li>" );
            }

            if (sbErrors.ToString().IsNullOrWhiteSpace() || !UserCanAdministrate)
            {
                return;
            }

            lErrors.Text = sbErrors.ToString();
            pnlValidation.Visible = true;



        }
    }
}