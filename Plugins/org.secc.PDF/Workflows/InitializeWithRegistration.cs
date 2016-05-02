using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Inititialzes PDF workflow with Group Member via registration. For example when a person is registerd for an event." )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Initialize With Registration" )]
    class InitializeWithRegistration : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            Utility.EnsureAttributes( action, rockContext );

            GroupMember groupMember;
            try
            {
                groupMember = entity as GroupMember;
            }
            catch
            {
                errorMessages.Add( "Could not convert entity to Group Member type." );
                return false;
            }

            //Add the registrant, registrant, and instance merge objects
            action.Activity.Workflow.SetAttributeValue( "GroupMemberId", groupMember.Id.ToString() );
            action.Activity.Workflow.SetAttributeValue( "PersonId", groupMember.PersonId.ToString() );
            var registrationRegistrant = new RegistrationRegistrantService( rockContext ).Queryable().Where(rr => rr.GroupMemberId == groupMember.Id).FirstOrDefault();
            action.Activity.Workflow.SetAttributeValue( "RegistrationRegistrantId", registrationRegistrant.Id.ToString() );
            return true;
        }

        
    }
}
