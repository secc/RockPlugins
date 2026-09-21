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
// <copyright>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Workflow
{
    /// <summary>
    /// Block for launching workflows for a single entity using querystring parameters.
    /// </summary>
    [DisplayName( "Workflow Entity Launch" )]
    [Category( "SECC > Workflow" )]
    [Description( "Block for launching workflows for a single entity using querystring parameters." )]
    [EntityTypeField( "Entity Type", "The entity type to use when loading the entity by Id." )]
    [WorkflowTypeField( "Workflow Type", "The workflow type to launch for the given entity." )]
    public partial class WorkflowEntityLaunch : RockBlock
    {
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                bbtnLaunch.Enabled = false;
                Guid? entityTypeGuid = GetAttributeValue( "EntityType" ).AsGuidOrNull();
                Guid? workflowGuid = GetAttributeValue( "WorkflowType" ).AsGuidOrNull();
                int? entityId = PageParameter( "EntityId" ).AsIntegerOrNull();

                if ( !workflowGuid.HasValue || !entityTypeGuid.HasValue )
                {
                    ShowMessage( "Please configure the block before using it.", NotificationBoxType.Danger );
                    return;
                }

                if ( !entityId.HasValue || entityId <= 0 )
                {
                    ShowMessage( "Please pass the EntityId as a page parameter.", NotificationBoxType.Danger );
                    return;
                }

                var workflowType = WorkflowTypeCache.Get( workflowGuid.Value );
                if ( workflowType == null )
                {
                    ShowMessage( "The configured workflow type could not be found.", NotificationBoxType.Danger );
                    return;
                }

                if ( !( workflowType.IsActive ?? true ) )
                {
                    ShowMessage( string.Format( "The workflow type <i>{0}</i> is inactive and cannot be launched.", workflowType.Name.EncodeHtml() ), NotificationBoxType.Danger );
                    return;
                }

                var entity = getEntity();
                if ( entity == null )
                {
                    ShowMessage( string.Format( "No entity of the configured type was found with ID {0:d}.", entityId.Value ), NotificationBoxType.Danger );
                    return;
                }

                ShowMessage(
                    string.Format( "Clicking \"Launch\" below will start a new instance of <i>{0}</i> for <i>{1}</i> (ID: {2:d}).", workflowType.Name.EncodeHtml(), entity.ToString().EncodeHtml(), entityId.Value ),
                    NotificationBoxType.Info );
                bbtnLaunch.Enabled = true;
            }
        }

        /// <summary>
        /// Handles the Launch button click. Launches the configured workflow for the entity
        /// synchronously so that success or failure can be reported back to the user.
        /// </summary>
        protected void Launch_Click( object sender, EventArgs e )
        {
            bbtnLaunch.Enabled = false;

            Guid? workflowGuid = GetAttributeValue( "WorkflowType" ).AsGuidOrNull();
            var workflowType = workflowGuid.HasValue ? WorkflowTypeCache.Get( workflowGuid.Value ) : null;
            if ( workflowType == null )
            {
                ShowMessage( "The configured workflow type could not be found.", NotificationBoxType.Danger );
                return;
            }

            if ( !( workflowType.IsActive ?? true ) )
            {
                ShowMessage( string.Format( "The workflow type <i>{0}</i> is inactive and cannot be launched.", workflowType.Name.EncodeHtml() ), NotificationBoxType.Danger );
                return;
            }

            var entity = getEntity();
            if ( entity == null )
            {
                ShowMessage( "The entity could not be found.", NotificationBoxType.Danger );
                return;
            }

            string entityName = entity.ToString();
            string encodedEntityName = entityName.EncodeHtml();

            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var workflowService = new WorkflowService( rockContext );

                    // Guard against a replayed/duplicate postback launching a second copy of the
                    // same workflow for the same entity.
                    var entityTypeId = EntityTypeCache.GetId( entity.GetType() );
                    bool alreadyActive = workflowService.Queryable()
                        .Any( w => w.WorkflowTypeId == workflowType.Id
                            && w.EntityTypeId == entityTypeId
                            && w.EntityId == entity.Id
                            && w.ActivatedDateTime.HasValue
                            && !w.CompletedDateTime.HasValue );
                    if ( alreadyActive )
                    {
                        ShowMessage( string.Format( "An active <i>{0}</i> workflow already exists for <i>{1}</i>. It was not launched again.", workflowType.Name.EncodeHtml(), encodedEntityName ), NotificationBoxType.Warning );
                        return;
                    }

                    var workflow = Rock.Model.Workflow.Activate( workflowType, entityName, rockContext );
                    workflow.InitiatorPersonAliasId = CurrentPersonAliasId;

                    var groupMember = entity as GroupMember;
                    if ( groupMember != null )
                    {
                        if ( groupMember.Group != null )
                        {
                            workflow.SetAttributeValue( "Group", groupMember.Group.Guid.ToString() );
                        }

                        var primaryAlias = groupMember.Person != null ? groupMember.Person.PrimaryAlias : null;
                        if ( primaryAlias != null )
                        {
                            workflow.SetAttributeValue( "Person", primaryAlias.Guid.ToString() );
                        }
                    }

                    List<string> workflowErrors;
                    bool processed = workflowService.Process( workflow, entity, out workflowErrors );

                    if ( processed && ( workflowErrors == null || !workflowErrors.Any() ) )
                    {
                        litOutput.Text = string.Format( "Launched workflow <i>{0}</i> for {1}.", workflowType.Name.EncodeHtml(), encodedEntityName );
                        ShowMessage( string.Format( "Workflow <i>{0}</i> was launched for <i>{1}</i>.", workflowType.Name.EncodeHtml(), encodedEntityName ), NotificationBoxType.Success );
                    }
                    else
                    {
                        var errors = workflowErrors ?? new List<string>();
                        litOutput.Text = string.Format( "Workflow <i>{0}</i> for {1} reported errors:<br />{2}",
                            workflowType.Name.EncodeHtml(),
                            encodedEntityName,
                            string.Join( "<br />", errors.Select( err => err.EncodeHtml() ) ) );
                        ShowMessage( "The workflow was processed but reported errors. See the output below.", NotificationBoxType.Warning );
                    }
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, Context );
                litOutput.Text = string.Format( "Unable to launch workflow for {0}: {1}", encodedEntityName, ex.Message.EncodeHtml() );
                ShowMessage( "An error occurred while launching the workflow. The error has been logged.", NotificationBoxType.Danger );
            }
        }

        /// <summary>
        /// Loads the entity identified by the configured entity type and the EntityId page parameter.
        /// </summary>
        /// <returns>The entity, or null if the block is misconfigured or the entity does not exist.</returns>
        private IEntity getEntity()
        {
            Guid? entityTypeGuid = GetAttributeValue( "EntityType" ).AsGuidOrNull();
            int? entityId = PageParameter( "EntityId" ).AsIntegerOrNull();

            if ( !entityTypeGuid.HasValue || !entityId.HasValue || entityId <= 0 )
            {
                return null;
            }

            var cachedEntityType = EntityTypeCache.Get( entityTypeGuid.Value );
            Type entityType = cachedEntityType != null ? cachedEntityType.GetEntityType() : null;
            if ( entityType == null )
            {
                return null;
            }

            return Reflection.GetIEntityForEntityType( entityType, entityId.Value );
        }

        /// <summary>
        /// Sets the notification box text and type.
        /// </summary>
        private void ShowMessage( string message, NotificationBoxType type )
        {
            nbInformation.Text = message;
            nbInformation.NotificationBoxType = type;
        }
    }
}
