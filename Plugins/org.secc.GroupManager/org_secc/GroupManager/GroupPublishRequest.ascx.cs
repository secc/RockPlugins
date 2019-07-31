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
using System.Web.UI;
using org.secc.GroupManager.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.GroupManager
{
    [DisplayName( "Group Publish Request" )]
    [Category( "SECC > Groups" )]
    [Description( "Block for camp leaders to see their group." )]

    [WorkflowTypeField( "Request Workflow", "Workflow to use to use if a new request has been made." )]
    [WorkflowTypeField( "Complete Workflow", "Workflow to use once the request has been approved or denied." )]

    public partial class GroupPublishRequest : RockBlock
    {
        private PublishGroup publishGroup;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            publishGroup = GetPublishGroup();
            if ( publishGroup != null )
            {
                publishGroup.LoadAttributes();
                Rock.Attribute.Helper.AddEditControls( publishGroup, phAttributeEdits, false );
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summarysni>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                if ( publishGroup != null )
                {
                    publishGroup.LoadAttributes();
                    pnlEdit.Visible = true;
                    DisplayForm( publishGroup );
                }
                else
                {
                    pnlSelectGroup.Visible = true;
                }
            }
        }

        private void DisplayForm( PublishGroup publishGroup )
        {
            if ( IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                iGroupImage.Required = true;
            }

            ddlAudience.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.MARKETING_CAMPAIGN_AUDIENCE_TYPE.AsGuid() ) );
            ltGroupName.Text = publishGroup.Group.Name;
            tbDescription.Text = publishGroup.Description.IsNotNullOrWhiteSpace() ? publishGroup.Description : publishGroup.Group.Description;
            iGroupImage.BinaryFileId = publishGroup.ImageId;
            drPublishDates.UpperValue = publishGroup.StartDateTime;
            drPublishDates.LowerValue = publishGroup.EndDateTime;
            cbRequiresRegistration.Checked = publishGroup.RequiresRegistration;
            tbRegistrationLink.Text = publishGroup.RegistrationLink;
            cbChildcareAvailable.Checked = publishGroup.ChildcareAvailable;
            tbChildcareRegistrationLink.Text = publishGroup.ChildcareRegistrationLink;
            if ( publishGroup.ContactPersonAlias != null )
            {
                pContactPerson.SetValue( publishGroup.ContactPersonAlias.Person );
            }
            tbContactEmail.Text = publishGroup.ContactEmail;
            tbContactPhoneNumber.Text = publishGroup.ContactPhoneNumber;
            tbConfirmationFromName.Text = publishGroup.ConfirmationFromName;
            tbConfirmationFromEmail.Text = publishGroup.ConfirmationEmail;
            tbConfirmationSubject.Text = publishGroup.ConfirmationSubject;
            ceConfirmationBody.Text = publishGroup.ConfirmationBody;

            ddlAudience.SetValues( publishGroup.AudienceValues.Select( i => i.Id.ToString() ) );

            if ( publishGroup.Attributes.Any() )
            {
                pnlAttributes.Visible = true;
                phAttributeEdits.Controls.Clear();
                Rock.Attribute.Helper.AddEditControls( publishGroup, phAttributeEdits, true );
            }
        }

        private PublishGroup GetPublishGroup( RockContext rockContext = null, PublishGroupService publishGroupService = null )
        {
            rockContext = rockContext ?? new RockContext();
            publishGroupService = publishGroupService ?? new PublishGroupService( rockContext );
            if ( PageParameter( "PublishGroupId" ).IsNotNullOrWhiteSpace() )
            {
                var publishGroup = publishGroupService.Get( PageParameter( "PublishGroupId" ).AsInteger() );
                if ( publishGroup != null )
                {
                    return publishGroup;
                }
            }

            if ( PageParameter( "GroupId" ).IsNotNullOrWhiteSpace() )
            {
                int groupId = PageParameter( "GroupId" ).AsInteger();
                GroupService groupService = new GroupService( rockContext );
                var group = groupService.Get( groupId );
                if ( group != null )
                {
                    var publishGroup = publishGroupService.Queryable()
                    .Where( pg => pg.GroupId == groupId )
                    .ToList()
                    .LastOrDefault();

                    if ( publishGroup != null )
                    {
                        return publishGroup;
                    }

                    return new PublishGroup
                    {
                        PublishGroupStatus = PublishGroupStatus.Pending,
                        Group = group,
                        RequestorAliasId = CurrentPersonAliasId.Value
                    };
                }
            }
            return null;
        }

        protected void gpGroup_SelectItem( object sender, EventArgs e )
        {
            NavigateToCurrentPage( new Dictionary<string, string> { { "GroupId", gpGroup.SelectedValue } } );
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            PublishGroupService publishGroupService = new PublishGroupService( rockContext );
            publishGroup = GetPublishGroup( rockContext, publishGroupService );

            if ( publishGroup.PublishGroupStatus == PublishGroupStatus.Approved )
            {
                var tempGroup = publishGroup.Group;
                publishGroup = ( PublishGroup ) publishGroup.Clone();
                publishGroup.Guid = Guid.NewGuid();
                publishGroup.Id = 0;
                publishGroup.Group = tempGroup;
                publishGroupService.Add( publishGroup );
            }

            publishGroup.ImageId = iGroupImage.BinaryFileId;
            publishGroup.PublishGroupStatus = PublishGroupStatus.Pending;
            publishGroup.Description = tbDescription.Text;
            publishGroup.StartDateTime = drPublishDates.UpperValue.Value;
            publishGroup.EndDateTime = drPublishDates.LowerValue.Value;
            publishGroup.RequiresRegistration = cbRequiresRegistration.Checked;
            publishGroup.RegistrationLink = cbRequiresRegistration.Checked ? tbRegistrationLink.Text : "";
            publishGroup.ChildcareAvailable = cbChildcareAvailable.Checked;
            publishGroup.ChildcareRegistrationLink = cbChildcareAvailable.Checked ? tbChildcareRegistrationLink.Text : "";
            publishGroup.AudienceValues = GetSelectedAudiences( rockContext );
            publishGroup.ContactPersonAliasId = pContactPerson.PersonAliasId.Value;
            publishGroup.RequestorAliasId = CurrentPersonAliasId.Value;
            publishGroup.ContactEmail = tbContactEmail.Text;
            publishGroup.ContactPhoneNumber = tbContactPhoneNumber.Text;
            publishGroup.ConfirmationFromName = tbConfirmationFromName.Text;
            publishGroup.ConfirmationEmail = tbConfirmationFromEmail.Text;
            publishGroup.ConfirmationSubject = tbConfirmationSubject.Text;
            publishGroup.ConfirmationBody = ceConfirmationBody.Text;

            if ( publishGroup.Id == 0 )
            {
                publishGroupService.Add( publishGroup );
            }

            if ( IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                publishGroup.PublishGroupStatus = PublishGroupStatus.Approved;
                publishGroup.Group.IsActive = true;
                publishGroup.Group.IsPublic = true;
                //remove all other publish groups for this computer
                publishGroupService.DeleteRange( publishGroupService.Queryable().Where( pg => pg.GroupId == publishGroup.GroupId && pg.Id != publishGroup.Id ) );
            };

            rockContext.SaveChanges();

            publishGroup.LoadAttributes( rockContext );

            if ( publishGroup.Attributes.Any() )
            {
                Rock.Attribute.Helper.GetEditValues( phAttributeEdits, publishGroup );
                publishGroup.SaveAttributeValues( rockContext );
            }

            if ( IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                publishGroup.LaunchWorkflow( GetAttributeValue( "CompleteWorkflow" ).AsGuidOrNull() );
            }
            else
            {
                publishGroup.LaunchWorkflow( GetAttributeValue( "RequestWorkflow" ).AsGuidOrNull() );
            }
            NavigateToParentPage();
        }

        private ICollection<DefinedValue> GetSelectedAudiences( RockContext rockContext )
        {
            DefinedValueService definedValueService = new DefinedValueService( rockContext );
            var audienceId = ddlAudience.SelectedDefinedValuesId;
            return definedValueService.Queryable().Where( dv => audienceId.Contains( dv.Id ) ).ToList();
        }

        protected void pRequestor_SelectPerson( object sender, EventArgs e )
        {

            if ( !pContactPerson.PersonId.HasValue )
            {
                return;
            }
            var person = new PersonService( new RockContext() ).Get( pContactPerson.PersonId.Value );
            if ( tbContactEmail.Text.IsNullOrWhiteSpace() )
            {
                tbContactEmail.Text = person.Email;
            }
            if ( tbContactPhoneNumber.Text.IsNullOrWhiteSpace() )
            {
                var number = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
                if ( number != null )
                {
                    tbContactPhoneNumber.Text = number.NumberFormatted;
                }
            }
            if ( tbConfirmationFromEmail.Text.IsNullOrWhiteSpace() )
            {
                tbConfirmationFromEmail.Text = person.Email;
            }
            if ( tbConfirmationFromName.Text.IsNullOrWhiteSpace() )
            {
                tbConfirmationFromName.Text = person.FullName;
            }
        }
    }
}