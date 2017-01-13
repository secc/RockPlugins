// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Web.UI.HtmlControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Connection
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName( "Connection Schedule Signup" )]
    [Category( "SECC > Connection" )]
    [Description( "Block used to sign up for a connection opportunity." )]

    [BooleanField( "Display Home Phone", "Whether to display home phone", true, "", 0 )]
    [BooleanField( "Display Mobile Phone", "Whether to display mobile phone", true, "", 1 )]
    [CodeEditorField( "Lava Template", "Lava template to use to display the response message.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"{% include '~~/Assets/Lava/OpportunityResponseMessage.lava' %}", "", 2 )]
    [BooleanField( "Enable Debug", "Display a list of merge fields available for lava.", false, "", 3 )]
    [BooleanField( "Enable Campus Context", "If the page has a campus context it's value will be used as a filter", true, "", 4 )]
    [DefinedValueField( "2E6540EA-63F0-40FE-BE50-F2A84735E600", "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, "368DD475-242C-49C4-A42C-7278BE690CC2", "", 5 )]
    [DefinedValueField( "8522BADD-2871-45A5-81DD-C76DA07E2E7E", "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, "283999EC-7346-42E3-B807-BCE9B2BABB49", "", 6 )]
    [TextField( "Group Member Attribute Keys - URL", "The key of any group member attributes that you would like to be set via the URL.  Enter as comma separated values.", false, key: "UrlKeys", order: 7)]
    [TextField( "Group Member Attribute Keys - Form", "The key of the group member attributes to show an edit control for on the opportunity signup.  Enter as comma separated values.", false, key: "FormKeys", order: 8 )]
    public partial class ConnectionOpportunitySignup : RockBlock, IDetailBlock
    {
        #region Fields

        DefinedValueCache _homePhone = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME );
        DefinedValueCache _cellPhone = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlOpportunityDetail );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbErrorMessage.Visible = false;

            if ( !Page.IsPostBack )
            {
                ShowDetail( PageParameter( "OpportunityId" ).AsInteger() );
            }
        }


        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetail( PageParameter( "OpportunityId" ).AsInteger() );
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnConnect_Click( object sender, EventArgs e )
        {
            using ( var rockContext = new RockContext() )
            {
                var opportunityService = new ConnectionOpportunityService( rockContext );
                var connectionRequestService = new ConnectionRequestService( rockContext );
                var personService = new PersonService( rockContext );

                // Get the opportunity and default status
                int opportunityId = PageParameter( "OpportunityId" ).AsInteger();
                var opportunity = opportunityService
                    .Queryable()
                    .Where( o => o.Id == opportunityId )
                    .FirstOrDefault();

                int defaultStatusId = opportunity.ConnectionType.ConnectionStatuses
                    .Where( s => s.IsDefault )
                    .Select( s => s.Id )
                    .FirstOrDefault();

                // If opportunity is valid and has a default status
                if ( opportunity != null && defaultStatusId > 0 )
                {
                    Person person = null;

                    string firstName = tbFirstName.Text.Trim();
                    string lastName = tbLastName.Text.Trim();
                    string email = tbEmail.Text.Trim();
                    int? campusId = cpCampus.SelectedCampusId;

                    // if a person guid was passed in from the query string use that
                    if ( RockPage.PageParameter( "PersonGuid" ) != null && !string.IsNullOrWhiteSpace( RockPage.PageParameter( "PersonGuid" ) ) )
                    {
                        Guid? personGuid = RockPage.PageParameter( "PersonGuid" ).AsGuidOrNull();

                        if ( personGuid.HasValue )
                        {
                            person = personService.Get( personGuid.Value );
                        }
                    }
                    else if ( CurrentPerson != null &&
                      CurrentPerson.LastName.Equals( lastName, StringComparison.OrdinalIgnoreCase ) &&
                      ( CurrentPerson.NickName.Equals( firstName, StringComparison.OrdinalIgnoreCase ) || CurrentPerson.FirstName.Equals( firstName, StringComparison.OrdinalIgnoreCase ) ) &&
                      CurrentPerson.Email.Equals( email, StringComparison.OrdinalIgnoreCase ) )
                    {
                        // If the name and email entered are the same as current person (wasn't changed), use the current person
                        person = personService.Get( CurrentPerson.Id );
                    }

                    else
                    {
                        // Try to find matching person
                        var personMatches = personService.GetByMatch( firstName, lastName, email );
                        if ( personMatches.Count() == 1 )
                        {
                            // If one person with same name and email address exists, use that person
                            person = personMatches.First();
                        }
                    }

                    // If person was not found, create a new one
                    if ( person == null )
                    {
                        // If a match was not found, create a new person
                        var dvcConnectionStatus = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
                        var dvcRecordStatus = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() );

                        person = new Person();
                        person.FirstName = firstName;
                        person.LastName = lastName;
                        person.IsEmailActive = true;
                        person.Email = email;
                        person.EmailPreference = EmailPreference.EmailAllowed;
                        person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                        if ( dvcConnectionStatus != null )
                        {
                            person.ConnectionStatusValueId = dvcConnectionStatus.Id;
                        }
                        if ( dvcRecordStatus != null )
                        {
                            person.RecordStatusValueId = dvcRecordStatus.Id;
                        }

                        PersonService.SaveNewPerson( person, rockContext, campusId, false );
                        person = personService.Get( person.Id );
                    }

                    // If there is a valid person with a primary alias, continue
                    if ( person != null && person.PrimaryAliasId.HasValue )
                    {
                        var changes = new List<string>();

                        if ( pnHome.Visible )
                        {
                            SavePhone( pnHome, person, _homePhone.Guid, changes );
                        }

                        if ( pnMobile.Visible )
                        {
                            SavePhone( pnMobile, person, _cellPhone.Guid, changes );
                        }

                        if ( changes.Any() )
                        {
                            HistoryService.SaveChanges(
                                rockContext,
                                typeof( Person ),
                                Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                                person.Id,
                                changes );
                        }

                        // Now that we have a person, we can create the connection request
                        var connectionRequest = new ConnectionRequest();
                        connectionRequest.PersonAliasId = person.PrimaryAliasId.Value;
                        connectionRequest.Comments = tbComments.Text.Trim();
                        connectionRequest.ConnectionOpportunityId = opportunity.Id;
                        connectionRequest.ConnectionState = ConnectionState.Active;
                        connectionRequest.ConnectionStatusId = defaultStatusId;
                        connectionRequest.CampusId = campusId;
                        connectionRequest.ConnectorPersonAliasId = opportunity.GetDefaultConnectorPersonAliasId( campusId );
                        if ( campusId.HasValue &&
                            opportunity != null &&
                            opportunity.ConnectionOpportunityCampuses != null )
                        {
                            var campus = opportunity.ConnectionOpportunityCampuses
                                .Where( c => c.CampusId == campusId.Value )
                                .FirstOrDefault();
                            if ( campus != null )
                            {
                                connectionRequest.ConnectorPersonAliasId = campus.DefaultConnectorPersonAliasId;
                            }
                        }

                        if ( hdnGroupRoleTypeId.Value.AsInteger() > 0 )
                        {
                            connectionRequest.AssignedGroupMemberRoleId = hdnGroupRoleTypeId.Value.AsInteger();
                            var groupConfig = opportunity.ConnectionOpportunityGroupConfigs.Where( gc => gc.GroupMemberRoleId == hdnGroupRoleTypeId.Value.AsInteger() ).FirstOrDefault();
                            connectionRequest.AssignedGroupMemberStatus = groupConfig.GroupMemberStatus;

                        }

                        if ( hdnGroupId.Value.AsInteger() > 0 )
                        {
                            connectionRequest.AssignedGroupId = hdnGroupId.Value.AsInteger();
                        }

                        var connectionAttributes = GetGroupMemberAttributes( rockContext );

                        if ( connectionAttributes != null || connectionAttributes.Keys.Any() )
                        {
                            var connectionDictionary = new Dictionary<string, string>();
                            foreach(var kvp in connectionAttributes )
                            {
                                connectionDictionary.Add( kvp.Key, kvp.Value.Value );
                            }

                            connectionRequest.AssignedGroupMemberAttributeValues = connectionDictionary.ToJson();
                        }

                        if ( !connectionRequest.IsValid )
                        {
                            // Controls will show warnings
                            return;
                        }

                        connectionRequestService.Add( connectionRequest );
                        rockContext.SaveChanges();

                        var mergeFields = new Dictionary<string, object>();
                        mergeFields.Add( "Opportunity", new ConnectionOpportunityService( rockContext ).Get( PageParameter( "OpportunityId" ).AsInteger() ) );
                        mergeFields.Add( "CurrentPerson", CurrentPerson );
                        mergeFields.Add( "Person", person );

                        lResponseMessage.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );
                        lResponseMessage.Visible = true;

                        pnlSignup.Visible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="opportunityId">The opportunity identifier.</param>
        public void ShowDetail( int opportunityId )
        {
            using ( var rockContext = new RockContext() )
            {
                var opportunity = new ConnectionOpportunityService( rockContext ).Get( opportunityId );
                if ( opportunity == null )
                {
                    pnlSignup.Visible = false;
                    ShowError( "Incorrect Opportunity Type", "The requested opportunity does not exist." );
                    return;
                }

                // load campus dropdown
                var campuses = CampusCache.All().Where( c => ( c.IsActive ?? false ) && opportunity.ConnectionOpportunityCampuses.Any( o => o.CampusId == c.Id ) ).ToList();
                cpCampus.Campuses = campuses;
                cpCampus.Visible = campuses.Any();

                if ( campuses.Any() )
                {
                    cpCampus.SetValue( campuses.First().Id );
                }

                if ( !string.IsNullOrEmpty( PageParameter( "CampusId" ) ) && opportunity.ConnectionOpportunityCampuses.Any( o => o.CampusId == PageParameter( "CampusId" ).AsInteger() ) )
                {
                    cpCampus.SetValue( PageParameter( "CampusId" ).AsInteger() );
                    cpCampus.CssClass = "hidden";
                    cpCampus.Label = null;
                    ltCampus.Text = CampusCache.Read( PageParameter( "CampusId" ).AsInteger() ).Name;
                    ltCampus.Visible = true;
                }

                pnlSignup.Visible = true;

                if ( !string.IsNullOrWhiteSpace( opportunity.IconCssClass ) )
                {
                    lIcon.Text = string.Format( "<i class='{0}' ></i>", opportunity.IconCssClass );
                }

                lTitle.Text = opportunity.Name;

                pnHome.Visible = GetAttributeValue( "DisplayHomePhone" ).AsBoolean();
                pnMobile.Visible = GetAttributeValue( "DisplayMobilePhone" ).AsBoolean();

                Person registrant = null;

                if ( RockPage.PageParameter( "PersonGuid" ) != null )
                {
                    Guid? personGuid = RockPage.PageParameter( "PersonGuid" ).AsGuidOrNull();

                    if ( personGuid.HasValue )
                    {
                        registrant = new PersonService( rockContext ).Get( personGuid.Value );
                    }
                }

                if ( registrant == null && CurrentPerson != null )
                {
                    registrant = CurrentPerson;
                }

                if ( registrant != null )
                {
                    tbFirstName.Text = registrant.FirstName.EncodeHtml();
                    tbLastName.Text = registrant.LastName.EncodeHtml();
                    tbEmail.Text = registrant.Email.EncodeHtml();

                    if ( pnHome.Visible && _homePhone != null )
                    {
                        var homePhoneNumber = registrant.PhoneNumbers.Where( p => p.NumberTypeValueId == _homePhone.Id ).FirstOrDefault();
                        if ( homePhoneNumber != null )
                        {
                            pnHome.Number = homePhoneNumber.NumberFormatted;
                            pnHome.CountryCode = homePhoneNumber.CountryCode;
                        }
                    }

                    if ( pnMobile.Visible && _cellPhone != null )
                    {
                        var cellPhoneNumber = registrant.PhoneNumbers.Where( p => p.NumberTypeValueId == _cellPhone.Id ).FirstOrDefault();
                        if ( cellPhoneNumber != null )
                        {
                            pnMobile.Number = cellPhoneNumber.NumberFormatted;
                            pnMobile.CountryCode = cellPhoneNumber.CountryCode;
                        }
                    }

                    var campus = registrant.GetCampus();
                    if ( campus != null )
                    {
                        cpCampus.SelectedCampusId = campus.Id;
                    }
                }
                else
                {
                    // set the campus to the context
                    if ( GetAttributeValue( "EnableCampusContext" ).AsBoolean() )
                    {
                        var campusEntityType = EntityTypeCache.Read( "Rock.Model.Campus" );
                        var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                        if ( contextCampus != null )
                        {
                            cpCampus.SelectedCampusId = contextCampus.Id;
                        }
                    }
                }


                // Role
                if ( !string.IsNullOrEmpty( PageParameter( "GroupTypeRoleId" ) ) )
                {
                    hdnGroupRoleTypeId.Value = PageParameter( "GroupTypeRoleId" ).AsInteger().ToString();
                    if ( opportunity.ConnectionOpportunityGroupConfigs.Where( gc => gc.GroupMemberRoleId == PageParameter( "GroupTypeRoleId" ).AsInteger() ).Any() )
                    {
                        var groupConfig = opportunity.ConnectionOpportunityGroupConfigs.Where( gc => gc.GroupMemberRoleId == PageParameter( "GroupTypeRoleId" ).AsInteger() ).FirstOrDefault();
                        ltRole.Text = groupConfig.GroupType.Name + " - " + groupConfig.GroupMemberRole.Name;
                        ltRole.Visible = true;
                    }
                }


                // show debug info
                var mergeFields = new Dictionary<string, object>();
                mergeFields.Add( "Opportunity", new ConnectionOpportunityService( rockContext ).Get( PageParameter( "OpportunityId" ).AsInteger() ) );
                mergeFields.Add( "CurrentPerson", CurrentPerson );
                if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
                {
                    lDebug.Visible = true;
                    lDebug.Text = mergeFields.lavaDebugInfo();
                }
            }
        }

        private void SavePhone( PhoneNumberBox phoneNumberBox, Person person, Guid phoneTypeGuid, List<string> changes )
        {
            var numberType = DefinedValueCache.Read( phoneTypeGuid );
            if ( numberType != null )
            {
                var phone = person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberType.Id );
                string oldPhoneNumber = phone != null ? phone.NumberFormattedWithCountryCode : string.Empty;
                string newPhoneNumber = PhoneNumber.CleanNumber( phoneNumberBox.Number );

                if ( newPhoneNumber != string.Empty )
                {
                    if ( phone == null )
                    {
                        phone = new PhoneNumber();
                        person.PhoneNumbers.Add( phone );
                        phone.NumberTypeValueId = numberType.Id;
                    }
                    else
                    {
                        oldPhoneNumber = phone.NumberFormattedWithCountryCode;
                    }
                    phone.CountryCode = PhoneNumber.CleanNumber( phoneNumberBox.CountryCode );
                    phone.Number = newPhoneNumber;

                    History.EvaluateChange(
                        changes,
                        string.Format( "{0} Phone", numberType.Value ),
                        oldPhoneNumber,
                        phone.NumberFormattedWithCountryCode );
                }

            }
        }

        private void ShowError( string title, string message )
        {
            nbErrorMessage.Title = title;
            nbErrorMessage.Text = string.Format( "<p>{0}</p>", message );
            nbErrorMessage.NotificationBoxType = NotificationBoxType.Danger;
            nbErrorMessage.Visible = true;
        }

        protected override void CreateChildControls()
        {
            AddGroupMemberAttributes();
        }

        private void AddGroupMemberAttributes( RockContext rockContext = null )
        {
            // Group
            if ( !string.IsNullOrEmpty( PageParameter( "GroupId" ) ) )
            {
                if ( rockContext == null )
                {
                    rockContext = new RockContext();
                }
                hdnGroupId.Value = PageParameter( "GroupId" ).AsInteger().ToString();

                var group = new GroupService( rockContext ).Get( hdnGroupId.Value.AsInteger() );
                if ( group != null )
                {
                    // Group Attributes
                    var formKeys = GetAttributeValues( "FormKeys" );
                    var urlKeys = GetAttributeValues( "UrlKeys" );

                    AttributeService attributeService = new AttributeService( rockContext );

                    string groupQualifierValue = group.Id.ToString();
                    string groupTypeQualifierValue = group.GroupTypeId.ToString();

                    // Make a fake group member so we can load some attributes.
                    GroupMember groupMember = new GroupMember();
                    groupMember.Group = group;
                    groupMember.GroupId = group.Id;
                    groupMember.LoadAttributes();

                    // Store URL Keys into the ViewState
                    var viewStateAttributes = new Dictionary<string, string>();
                    foreach ( string urlKey in urlKeys )
                    {
                        if ( !string.IsNullOrEmpty( PageParameter( urlKey ) ) && groupMember.Attributes.ContainsKey( urlKey ) )
                        {
                            groupMember.SetAttributeValue( urlKey, PageParameter( urlKey ) );
                            viewStateAttributes.Add( urlKey, PageParameter( urlKey ) );
                        }
                    }
                    ViewState.Add( "SelectedAttributes", viewStateAttributes );
                    SaveViewState();

                    Helper.AddDisplayControls( groupMember, phAttributes, groupMember.Attributes.Where( a => !urlKeys.Contains( a.Key )).Select(a => a.Key).ToList(), true, false );
                    Helper.AddEditControls( "", formKeys, groupMember, phAttributes,  tbLastName.ValidationGroup,  false, new List<String>() );

                }
            }
        }


        private Dictionary<string, AttributeValueCache> GetGroupMemberAttributes( RockContext rockContext = null )
        {
            // Group
            if ( !string.IsNullOrEmpty( PageParameter( "GroupId" ) ) )
            {
                if ( rockContext == null )
                {
                    rockContext = new RockContext();
                }
                hdnGroupId.Value = PageParameter( "GroupId" ).AsInteger().ToString();

                var group = new GroupService( rockContext ).Get( hdnGroupId.Value.AsInteger() );
                if ( group != null )
                {
                    // Make a fake group member so we can load some attributes.
                    GroupMember groupMember = new GroupMember();
                    groupMember.Group = group;
                    groupMember.GroupId = group.Id;
                    groupMember.LoadAttributes();

                    Helper.GetEditValues( phAttributes, groupMember );

                    var readonlyAttributes = (Dictionary<string, string> ) ViewState[ "SelectedAttributes" ];
                    if ( readonlyAttributes != null && readonlyAttributes.Keys.Count > 0)
                    {
                        foreach(var kvp in readonlyAttributes)
                        {
                            if ( groupMember.AttributeValues.ContainsKey(kvp.Key) )
                            {
                                groupMember.AttributeValues[kvp.Key].Value = kvp.Value;
                            }
                        }
                    }
                    return groupMember.AttributeValues;
                }
            }
            return null;
        }
        #endregion
    }
}