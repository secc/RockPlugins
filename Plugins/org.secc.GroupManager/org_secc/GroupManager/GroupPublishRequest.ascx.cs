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
using org.secc.GroupManager.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.GroupManager
{
    [DisplayName( "Group Publish Request" )]
    [Category( "SECC > Groups" )]
    [Description( "Block for camp leaders to see their group." )]

    public partial class GroupPublishRequest : RockBlock
    {
        public PublishGroup PublishGroup { get; set; }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summarysni>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                GetPublishGroup();
                if ( PublishGroup != null )
                {
                    pnlEdit.Visible = true;
                    DisplayForm();
                }
                else
                {
                    pnlSelectGroup.Visible = true;
                }
            }
        }

        private void DisplayForm()
        {
            ddlAudience.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.MARKETING_CAMPAIGN_AUDIENCE_TYPE.AsGuid() ) );
            ltGroupName.Text = PublishGroup.Group.Name;
            tbDescription.Text = PublishGroup.Description.IsNotNullOrWhiteSpace() ? PublishGroup.Description : PublishGroup.Group.Description;

            drPublishDates.UpperValue = PublishGroup.StartDateTime;
            drPublishDates.LowerValue = PublishGroup.EndDateTime;
            if ( PublishGroup.ContactPersonAlias != null )
            {
                pContactPerson.SetValue( PublishGroup.ContactPersonAlias.Person );
            }
            tbContactEmail.Text = PublishGroup.ContactEmail;
            tbContactPhoneNumber.Text = PublishGroup.ContactPhoneNumber;
            tbConfirmationFromName.Text = PublishGroup.ConfirmationFromName;
            tbConfirmationFromEmail.Text = PublishGroup.ConfirmationEmail;
            tbConfirmationSubject.Text = PublishGroup.ConfirmationSubject;
            ceConfirmationBody.Text = PublishGroup.ConfirmationBody;

            ddlAudience.SetValues( PublishGroup.AudienceValues.Select( i => i.ToString() ) );
        }

        private void GetPublishGroup( RockContext rockContext = null )
        {
            rockContext = rockContext ?? new RockContext();
            PublishGroupService publishGroupService = new PublishGroupService( rockContext );
            if ( PageParameter( "PublishGroupId" ).IsNotNullOrWhiteSpace() )
            {
                PublishGroup = publishGroupService.Get( PageParameter( "PublishGroupId" ).AsInteger() );
                if ( PublishGroup != null )
                {
                    return;
                }
            }

            if ( PageParameter( "GroupId" ).IsNotNullOrWhiteSpace() )
            {
                int groupId = PageParameter( "GroupId" ).AsInteger();
                GroupService groupService = new GroupService( rockContext );
                var group = groupService.Get( groupId );
                if ( group != null )
                {
                    PublishGroup = publishGroupService.Queryable()
                    .Where( pg => pg.GroupId == groupId )
                    .ToList()
                    .LastOrDefault();

                    if ( PublishGroup != null )
                    {
                        return;
                    }

                    PublishGroup = new PublishGroup
                    {
                        PublishGroupStatus = PublishGroupStatus.Pending,
                        Group = group,
                        RequestorAlias = CurrentPersonAlias
                    };
                }
            }
        }

        protected void gpGroup_SelectItem( object sender, EventArgs e )
        {
            NavigateToCurrentPage( new Dictionary<string, string> { { "GroupId", gpGroup.SelectedValue } } );
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {

            RockContext rockContext = new RockContext();
            GetPublishGroup( rockContext );
            PublishGroupService publishGroupService = new PublishGroupService( rockContext );

            if ( PublishGroup.PublishGroupStatus == PublishGroupStatus.Approved )
            {
                PublishGroup = ( PublishGroup ) PublishGroup.Clone();
                PublishGroup.Guid = Guid.NewGuid();
                PublishGroup.Id = 0;
                publishGroupService.Add( PublishGroup );
            }

            PublishGroup.PublishGroupStatus = PublishGroupStatus.Pending;
            PublishGroup.Description = tbDescription.Text;
            PublishGroup.StartDateTime = drPublishDates.UpperValue.Value;
            PublishGroup.EndDateTime = drPublishDates.LowerValue.Value;
            PublishGroup.AudienceValues = GetSelectedAudiences( rockContext );
            PublishGroup.ContactPersonAliasId = pContactPerson.PersonAliasId.Value;
            PublishGroup.RequestorAliasId = CurrentPersonAliasId.Value;
            PublishGroup.ContactEmail = tbContactEmail.Text;
            PublishGroup.ContactPhoneNumber = tbContactPhoneNumber.Text;
            PublishGroup.ConfirmationFromName = tbConfirmationFromName.Text;
            PublishGroup.ConfirmationEmail = tbConfirmationFromEmail.Text;
            PublishGroup.ConfirmationSubject = tbConfirmationSubject.Text;
            PublishGroup.ConfirmationBody = ceConfirmationBody.Text;

            rockContext.SaveChanges();
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
                tbContactPhoneNumber.Text = number.NumberFormatted;
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