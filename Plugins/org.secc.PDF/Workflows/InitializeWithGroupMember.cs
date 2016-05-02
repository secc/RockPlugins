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
    [Description( "Inititialzes PDF workflow with Group Member. For example when a person joins a group." )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Initialize With Group Member" )]
    class InitializeWithGroupMember : ActionComponent
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

            //Add group member id and person id
            action.Activity.Workflow.SetAttributeValue( "GroupMemberId", groupMember.Id.ToString() );
            action.Activity.Workflow.SetAttributeValue( "PersonId", groupMember.PersonId.ToString() );


            return true;
        }
    }
}
