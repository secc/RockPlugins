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
    [DisplayName( "Publish Group Request" )]
    [Category( "SECC > Groups" )]
    [Description( "Block for camp leaders to see their group." )]

    [MemoField(
        "Registration Details",
        Description = "Default details when someone requests a registration",
        IsRequired = false,
        Key = AttributeKeys.RegistrationDetails,
        Order = 1
        )]

    [MemoField(
        "Childcare Registration Details",
        Description = "Default details when someone requests a childcare registration",
        IsRequired = false,
        Key = AttributeKeys.ChildcareRegistrationDetails,
        Order = 2
        )]

    [WorkflowTypeField(
        "Workflow",
        Description = "Workflow to run after saving the publish group.",
        Key = AttributeKeys.Workflow,
        Order = 3
        )]

    public partial class PublishGroupRequest : RockBlock
    {
        #region Keys
        /// <summary>Attribute keys for the block</summary>
        internal class AttributeKeys
        {
            public const string Workflow = "Workflow";
            public const string RegistrationDetails = "RegistrationDetails";
            public const string ChildcareRegistrationDetails = "ChildcareRegistrationDetails";
        }

        /// <summary>Page Parameter Keys for the Block</summary>
        internal class PageParameterKeys
        {
            public const string GroupId = "GroupId";
            public const string PublishGroupId = "PublishGroupId";
        }

        #endregion

        #region Page Cycle

        /// <summary>Raises the <see cref="E:System.Web.UI.Control.Init"/> event.</summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            PublishGroup publishGroup = GetPublishGroup();
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
                ddlStatus.BindToEnum<PublishGroupStatus>();
                PublishGroup publishGroup = GetPublishGroup();
                if ( publishGroup != null )
                {
                    publishGroup.LoadAttributes();
                    pnlEdit.Visible = true;
                    DisplayForm( publishGroup );
                }
                else
                {
                    pnlEdit.Visible = false;
                    pnlSelectGroup.Visible = true;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>Displays the form.</summary>
        /// <param name="publishGroup">The publish group.</param>
        private void DisplayForm( PublishGroup publishGroup )
        {
            if ( IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                iGroupImage.Required = true;
                btnSave.Text = "Save";
                btnDraft.Visible = false;
                ddlStatus.SelectedValue = publishGroup.PublishGroupStatus.ConvertToInt().ToString();
            }
            else
            {
                ddlStatus.Visible = false;
            }

            ddlAudience.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.MARKETING_CAMPAIGN_AUDIENCE_TYPE.AsGuid() ) );
            ddlDayOfWeek.BindToEnum<DayOfWeek>( true );
            ltGroupName.Text = publishGroup.Group.Name;
            tbDescription.Text = publishGroup.Description.IsNotNullOrWhiteSpace() ? publishGroup.Description : publishGroup.Group.Description;
            iGroupImage.BinaryFileId = publishGroup.ImageId;
            drPublishDates.UpperValue = publishGroup.EndDateTime;
            drPublishDates.LowerValue = publishGroup.StartDateTime;
            ddlDayOfWeek.SelectedValue = publishGroup.WeeklyDayOfWeek != null ? ( ( int ) publishGroup.WeeklyDayOfWeek ).ToString() : "";
            tTimeOfDay.SelectedTime = publishGroup.WeeklyTimeOfDay;
            dpStartDate.SelectedDate = publishGroup.StartDate;
            tbLocationName.Text = publishGroup.MeetingLocation;
            ddlRegistration.SelectedValue = publishGroup.RegistrationRequirement.ConvertToInt().ToString();

            SwitchRegistrationRequirement( publishGroup.RegistrationRequirement );
            tbRegistrationDetails.Text = publishGroup.RegistrationDescription;

            SwitchChildcareOptions( publishGroup.ChildcareOptions );
            tbChildcareRegistrationDetails.Text = publishGroup.ChildcareRegistrationDescription;

            CheckRegistationConflict();

            tbRegistrationLink.Text = publishGroup.RegistrationLink;
            cbAllowSpouseRegistration.Checked = publishGroup.AllowSpouseRegistration;

            ddlChildcareOptions.SelectedValue = publishGroup.ChildcareOptions.ConvertToInt().ToString();
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

        /// <summary>Switches the childcare options.</summary>
        /// <param name="childcareOptions">The childcare options.</param>
        private void SwitchChildcareOptions( ChildcareOptions childcareOptions )
        {
            switch ( childcareOptions )
            {
                case ChildcareOptions.NoChildcare:
                case ChildcareOptions.ChildcareNoRegistration:
                    ddlChildcareNeedRegistration.Visible = false;
                    ddlChildcareNeedRegistration.Required = false;
                    tbChildcareRegistrationLink.Visible = false;
                    tbChildcareRegistrationDetails.Visible = false;
                    tbChildcareRegistrationDetails.Required = false;
                    break;
                case ChildcareOptions.ChildcareRegistrationRequired:
                    ddlChildcareNeedRegistration.Visible = true;
                    ddlChildcareNeedRegistration.Required = true;
                    UpdateChildcareFields();
                    break;
                case ChildcareOptions.ChildareIncludedInCustomRegistration:
                    ddlChildcareNeedRegistration.Visible = false;
                    ddlChildcareNeedRegistration.Required = false;
                    tbChildcareRegistrationLink.Visible = false;
                    tbChildcareRegistrationDetails.Visible = false;
                    tbChildcareRegistrationDetails.Required = false;
                    break;
            }
        }

        /// <summary>Gets the publish group.</summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="publishGroupService">The publish group service.</param>
        /// <returns></returns>
        private PublishGroup GetPublishGroup( RockContext rockContext = null, PublishGroupService publishGroupService = null )
        {
            rockContext = rockContext ?? new RockContext();
            publishGroupService = publishGroupService ?? new PublishGroupService( rockContext );
            if ( PageParameter( PageParameterKeys.PublishGroupId ).IsNotNullOrWhiteSpace() )
            {
                var publishGroup = publishGroupService.Get( PageParameter( PageParameterKeys.PublishGroupId ).AsInteger() );
                if ( publishGroup != null )
                {
                    return publishGroup;
                }
            }

            if ( PageParameter( PageParameterKeys.GroupId ).IsNotNullOrWhiteSpace() )
            {
                int groupId = PageParameter( PageParameterKeys.GroupId ).AsInteger();
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

                    publishGroup = new PublishGroup
                    {
                        PublishGroupStatus = PublishGroupStatus.PendingApproval,
                        RegistrationRequirement = RegistrationRequirement.NoRegistration,
                        ChildcareOptions = ChildcareOptions.NoChildcare,
                        Group = group,
                        RequestorAliasId = CurrentPersonAliasId.Value,
                        ConfirmationBody = "{{ 'Global' | Attribute:'EmailHeader' }}\n<br>\n<br>\n{{ 'Global' | Attribute:'EmailFooter' }}",
                        RegistrationDescription = GetAttributeValue( AttributeKeys.RegistrationDetails ),
                        ChildcareRegistrationDescription = GetAttributeValue( AttributeKeys.ChildcareRegistrationDetails )
                    };

                    if ( group.Schedule != null )
                    {
                        publishGroup.WeeklyDayOfWeek = group.Schedule.WeeklyDayOfWeek;
                        publishGroup.WeeklyTimeOfDay = group.Schedule.WeeklyTimeOfDay;
                    }

                    if ( group.GroupLocations.Any() )
                    {
                        publishGroup.MeetingLocation = group.GroupLocations.FirstOrDefault().Location.Name;
                    }

                    return publishGroup;
                }
            }
            return null;
        }

        /// <summary>Saves the specified publish group status.</summary>
        /// <param name="publishGroupStatus">The publish group status.</param>
        private void Save( PublishGroupStatus publishGroupStatus )
        {
            bool isApprover = false;
            if ( IsUserAuthorized( Rock.Security.Authorization.EDIT ) )
            {
                isApprover = true;
            }

            if ( isApprover )
            {
                publishGroupStatus = ddlStatus.SelectedValueAsEnum<PublishGroupStatus>();
            }
            else if ( publishGroupStatus != PublishGroupStatus.Draft )
            {
                if ( ddlRegistration.SelectedValue == "4" ||
                    ( ddlChildcareOptions.SelectedValue == "2" && ddlChildcareNeedRegistration.SelectedValue == "2" ) )  //Childcare required && Registration needed.
                {
                    publishGroupStatus = PublishGroupStatus.PendingIT;
                }
            }

            RockContext rockContext = new RockContext();
            PublishGroupService publishGroupService = new PublishGroupService( rockContext );
            PublishGroup publishGroup = GetPublishGroup( rockContext, publishGroupService );

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
            publishGroup.PublishGroupStatus = publishGroupStatus;
            publishGroup.Description = tbDescription.Text;
            publishGroup.EndDateTime = drPublishDates.UpperValue.Value;
            publishGroup.StartDateTime = drPublishDates.LowerValue.Value;
            publishGroup.WeeklyDayOfWeek = ddlDayOfWeek.SelectedValueAsEnumOrNull<DayOfWeek>();
            publishGroup.WeeklyTimeOfDay = tTimeOfDay.SelectedTime;
            publishGroup.StartDate = dpStartDate.SelectedDate;
            publishGroup.MeetingLocation = tbLocationName.Text;
            publishGroup.RegistrationRequirement = ( RegistrationRequirement ) ddlRegistration.SelectedValue.AsInteger();
            publishGroup.RequiresRegistration = ddlRegistration.SelectedValue.AsInteger() > 0; //This is obsolete but left in for backward compatability
            publishGroup.RegistrationLink = publishGroup.RegistrationRequirement == RegistrationRequirement.CustomRegistration ? tbRegistrationLink.Text : "";
            publishGroup.ChildcareRegistrationDescription = tbChildcareRegistrationDetails.Text;
            publishGroup.AllowSpouseRegistration = cbAllowSpouseRegistration.Checked;
            publishGroup.ChildcareOptions = ( ChildcareOptions ) ddlChildcareOptions.SelectedValue.AsInteger();
            publishGroup.ChildcareAvailable = ddlChildcareOptions.SelectedValue.AsInteger() > 0;
            publishGroup.ChildcareRegistrationLink = publishGroup.ChildcareAvailable ? tbChildcareRegistrationLink.Text : "";
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

            if ( isApprover && publishGroupStatus == PublishGroupStatus.Approved )
            {
                publishGroup.Group.IsActive = true;
                publishGroup.Group.IsPublic = true;
                //remove all other publish groups for this computer
                publishGroupService.DeleteRange( publishGroupService.Queryable().Where( pg => pg.GroupId == publishGroup.GroupId && pg.Id != publishGroup.Id ) );
            };

            //Set the binary file to not be temporary
            if ( publishGroup.ImageId.HasValue )
            {
                BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                var binaryFile = binaryFileService.Get( publishGroup.ImageId.Value );
                if ( binaryFile != null )
                {
                    binaryFile.IsTemporary = false;
                }
            }

            rockContext.SaveChanges();

            publishGroup.LoadAttributes( rockContext );

            if ( publishGroup.Attributes.Any() )
            {
                Rock.Attribute.Helper.GetEditValues( phAttributeEdits, publishGroup );
                publishGroup.SaveAttributeValues( rockContext );
            }

            if ( publishGroup.PublishGroupStatus != PublishGroupStatus.Draft )
            {
                publishGroup.LaunchWorkflow( GetAttributeValue( AttributeKeys.Workflow ).AsGuidOrNull() );
            }

            NavigateToParentPage();
        }

        /// <summary>Gets the selected audiences.</summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private ICollection<DefinedValue> GetSelectedAudiences( RockContext rockContext )
        {
            DefinedValueService definedValueService = new DefinedValueService( rockContext );
            var audienceId = ddlAudience.SelectedDefinedValuesId;
            return definedValueService.Queryable().Where( dv => audienceId.Contains( dv.Id ) ).ToList();
        }

        /// <summary>Switches the registration requirement.</summary>
        /// <param name="selection">The selection.</param>
        private void SwitchRegistrationRequirement( RegistrationRequirement selection )
        {
            switch ( selection )
            {
                case RegistrationRequirement.NoRegistration:
                    cbAllowSpouseRegistration.Visible = false;
                    tbRegistrationLink.Required = false;
                    tbRegistrationLink.Visible = false;
                    tbRegistrationDetails.Visible = false;
                    tbRegistrationDetails.Required = false;
                    HideConfirmationInformation();
                    break;
                case RegistrationRequirement.RegistrationAvailable:
                    cbAllowSpouseRegistration.Visible = true;
                    tbRegistrationLink.Required = false;
                    tbRegistrationLink.Visible = false;
                    tbRegistrationDetails.Visible = false;
                    tbRegistrationDetails.Required = false;
                    ShowConfirmationInformation();
                    break;

                case RegistrationRequirement.RegistrationRequired:
                    cbAllowSpouseRegistration.Visible = true;
                    tbRegistrationLink.Required = false;
                    tbRegistrationLink.Visible = false;
                    tbRegistrationDetails.Visible = false;
                    tbRegistrationDetails.Required = false;
                    ShowConfirmationInformation();
                    break;
                case RegistrationRequirement.CustomRegistration:
                    cbAllowSpouseRegistration.Visible = false;
                    tbRegistrationLink.Required = true;
                    tbRegistrationLink.Visible = true;
                    tbRegistrationDetails.Visible = false;
                    tbRegistrationDetails.Required = false;
                    HideConfirmationInformation();
                    break;
                case RegistrationRequirement.NeedCustomRegistration:
                    cbAllowSpouseRegistration.Visible = false;
                    tbRegistrationLink.Required = false;
                    tbRegistrationLink.Visible = false;
                    tbRegistrationDetails.Visible = true;
                    tbRegistrationDetails.Required = true;
                    HideConfirmationInformation();
                    break;
                default:
                    break;
            }
        }

        /// <summary>Checks the registation for conflict.</summary>
        private void CheckRegistationConflict()
        {
            if ( ddlChildcareOptions.SelectedValue == "2"
                && ( ddlRegistration.SelectedValue == "0" || ddlRegistration.SelectedValue == "3" ) )
            {
                nbRegistrationError.Visible = true;
            }
            else
            {
                nbRegistrationError.Visible = false;
            }
        }

        /// <summary>Shows the confirmation information.</summary>
        private void ShowConfirmationInformation()
        {
            pnlConfirmation.CssClass = "";
            tbConfirmationFromName.Required = true;
            tbConfirmationFromEmail.Required = true;
            tbConfirmationSubject.Required = true;
            ceConfirmationBody.Required = true;
        }

        /// <summary>Updates the childcare fields.</summary>
        private void UpdateChildcareFields()
        {
            tbChildcareRegistrationLink.Visible = ddlChildcareNeedRegistration.SelectedValue == "1";
            tbChildcareRegistrationLink.Required = tbChildcareRegistrationLink.Visible;
            tbChildcareRegistrationDetails.Visible = ddlChildcareNeedRegistration.SelectedValue == "2";
            tbChildcareRegistrationDetails.Required = tbChildcareRegistrationDetails.Visible;
        }

        /// <summary>Hides the confirmation information.</summary>
        private void HideConfirmationInformation()
        {
            tbConfirmationFromName.Required = false;
            tbConfirmationFromEmail.Required = false;
            tbConfirmationSubject.Required = false;
            ceConfirmationBody.Required = false;
            pnlConfirmation.CssClass = "hidden";
        }

        #endregion

        #region Event Handlers

        /// <summary>Handles the SelectItem event of the gpGroup control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gpGroup_SelectItem( object sender, EventArgs e )
        {
            NavigateToCurrentPage( new Dictionary<string, string> { { PageParameterKeys.GroupId, gpGroup.SelectedValue } } );
        }

        /// <summary>Handles the Click event of the btnSave control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            Save( PublishGroupStatus.PendingApproval );
        }

        protected void btnDraft_Click( object sender, EventArgs e )
        {
            Save( PublishGroupStatus.Draft );
        }

        /// <summary>Handles the SelectPerson event of the pRequestor control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
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
                var number = person.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() );
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

        /// <summary>Handles the SelectedIndexChanged event of the ddlRegistration control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlRegistration_SelectedIndexChanged( object sender, EventArgs e )
        {
            var selection = ( RegistrationRequirement ) ddlRegistration.SelectedValue.AsInteger();

            SwitchRegistrationRequirement( selection );
            CheckRegistationConflict();
        }

        /// <summary>Handles the SelectedIndexChanged event of the ddlChildcareRegistration control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlChildcareRegistration_SelectedIndexChanged( object sender, EventArgs e )
        {
            var childcareOption = ( ChildcareOptions ) ddlChildcareOptions.SelectedValue.AsInteger();
            SwitchChildcareOptions( childcareOption );
            CheckRegistationConflict();
        }

        /// <summary>Handles the SelectedIndexChanged event of the ddlChildcareNeedRegistration control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlChildcareNeedRegistration_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateChildcareFields();
        }
        #endregion
    }
}