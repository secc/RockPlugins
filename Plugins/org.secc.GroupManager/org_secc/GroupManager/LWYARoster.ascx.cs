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
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using Rock;
using Rock.Attribute;
using Rock.Security;
using System.Text;
using org.secc.GroupManager;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "LWYA Roster" )]
    [Category( "SECC > Groups" )]
    [Description( "Presents members of group in roster format." )]
    
    [CustomRadioListField( "Group Member Status", "The group member status to use when adding person to group (default: 'Pending'.)", "2^Pending,1^Active,0^Inactive", true, "2", "", 1 )]
    [DefinedValueField( "2E6540EA-63F0-40FE-BE50-F2A84735E600", "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, "368DD475-242C-49C4-A42C-7278BE690CC2", "", 2 )]
    [DefinedValueField( "8522BADD-2871-45A5-81DD-C76DA07E2E7E", "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, "283999EC-7346-42E3-B807-BCE9B2BABB49", "", 3 )]
    
    //Settings
    [CodeEditorField( "Roster Lava", "Lava to appear in member roster pannels", CodeEditorMode.Lava, CodeEditorTheme.Rock, 600, false )]
    [BooleanField( "Allow Email", "Allow email to be sent from this block.", false )]
    [BooleanField( "Allow SMS", "Allow test messages to be sent from this block.", false )]

    [TextField( "Safe Sender Email", "If the current users email address is not from a safe sender, the email address to use." )]
    public partial class LWYARoster : GroupManagerBlock
    {
        List<GroupMemberData> memberData = new List<GroupMemberData>();
        string smsScript = @"
            var charCount = function(){
                document.getElementById('charCount').innerHTML = $('textarea[id$= \'tbMessage\']').val().length + ' of 160';
            }
            
            $('textarea[id$= \'tbMessage\']').keyup(function(){charCount()});
            ";
        private RockContext _rockContext;
        GroupTypeRole _defaultGroupRole = null;
        DefinedValueCache _dvcConnectionStatus = null;
        DefinedValueCache _dvcRecordStatus = null;


        protected override void OnInit( EventArgs e )
        {

            base.OnInit( e );
            gMembers.ShowActionRow = false;
            gMembers.PersonIdField = "Id";
            gMembers.DataKeyNames = new string[] { "Id" };
            gMembers.GridRebind += gMembers_GridRebind;

            //hide modal footer
            mdMember.Footer.Visible = false;
        }

        private void gMembers_GridRebind( object sender, EventArgs e )
        {
            FilterData();
            BindData();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( CurrentGroup == null )
            {
                NavigateToHomePage();
                return;
            }

            btnEmail.Visible = GetAttributeValue( "AllowEmail" ).AsBoolean();
            btnSMS.Visible = GetAttributeValue( "AllowSMS" ).AsBoolean();

            nbAlert.Visible = false;
            var personAlias = RockPage.CurrentPersonAlias;

            _rockContext = new RockContext();

            //If you are not a leader when one is required hide and quit.
            if ( !CurrentGroup.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                pnlMain.Visible = false;
                nbAlert.Visible = true;
                nbAlert.Text = "You are not authorized to view this page.";
                return;
            }

            Page.Title = CurrentGroup.Name;
            ltTitle.Text = "<h1>" + CurrentGroup.Name + " Roster</h1>";

            GetGroupMembers();

            if ( !Page.IsPostBack )
            {
                GenerateFilters( true );
                BindData();
            }
            else
            {
                //this line is needed or resizing the page
                //afterpost back will stop the grid from being responsive
                gMembers.DataSource = memberData;
                pnlRoster.AddCssClass( "is-showing-items" );
            }
            BindRoster();
        }

        private void GetGroupMembers()
        {
            memberData = new List<GroupMemberData>();
            var groupMembers = CurrentGroupMembers;
            foreach ( var member in groupMembers )
            {
                memberData.Add( new GroupMemberData( member ) );
            }

            if ( !Page.IsPostBack )
            {
                memberData = memberData.Where( m => m.Status != GroupMemberStatus.Inactive ).ToList();
            }

            //Rock allows for people to be in the same group twice as long as their group roles are different
            //We need to filter down to one person with leaders at top
            memberData = memberData
                .OrderByDescending( m => m.IsLeader )
                .ThenBy( m => m.LastName )
                .ThenBy( m => m.FirstName )
                .DistinctBy( m => m.Id ).ToList();
        }

        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            FilterData();
        }

        private void FilterData()
        {
            var genders = cblGender.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => int.Parse( i.Value ) ).ToList();
            var roles = cblRole.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value ).ToList();
            var status = cblStatus.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => int.Parse( i.Value ) ).ToList();

            memberData = memberData.Where( m =>
                 genders.Contains( m.Gender.ConvertToInt() ) &&
                 roles.Contains( m.Role ) &&
                 status.Contains( m.Status.ConvertToInt() )
                ).ToList();
            if ( txtFirstName.Text != string.Empty )
            {
                memberData = memberData.Where( m => m.FirstName.ToLower().Contains( txtFirstName.Text.ToLower() )
                 || m.NickName.ToLower().Contains( txtFirstName.Text.ToLower() ) ).ToList();
            }
            if ( txtLastName.Text != string.Empty )
            {
                memberData = memberData.Where( m => m.LastName.ToLower().Contains( txtLastName.Text.ToLower() ) ).ToList();
            }

            BindData();
        }

        protected void btnSMS_Click( object sender, EventArgs e )
        {
            tbMessage.Text = "";

            CurrentGroup.LoadAttributes();
            cbSMSSendToParents.Visible = CurrentGroup.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
            pnlMain.Visible = false;
            pnlSMS.Visible = true;

            DisplaySMSRecipients();

            ScriptManager.RegisterStartupScript( Page, Page.GetType(), "smsCharCount", smsScript, true );
        }

        protected void btnSMSSend_Click( object sender, EventArgs e )
        {
            bool sendToParents = cbEmailSendToParents.Checked;
            string subject = tbSubject.Text;
            string body = tbBody.Text;

            if ( Page.IsValid )
            {
                _rockContext = new RockContext();
                var communication = UpdateCommunication( _rockContext );

                if ( communication != null )
                {
                    AddRecipients( communication, cbSMSSendToParents.Checked, EntityTypeCache.Get( new Guid( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_SMS ) ).Id );

                    communication.CommunicationType = CommunicationType.SMS;
                    communication.SMSMessage = tbMessage.Text;
                    communication.SMSFromDefinedValueId = DefinedValueCache.Get( CurrentGroup.GetAttributeValue( "TextMessageFrom" ).AsGuid() ).Id;

                    communication.Status = CommunicationStatus.Approved;
                    communication.ReviewedDateTime = RockDateTime.Now;
                    communication.FutureSendDateTime = RockDateTime.Now;
                    communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                    _rockContext.SaveChanges();
                    var transaction = new Rock.Transactions.SendCommunicationTransaction();
                    transaction.CommunicationId = communication.Id;
                    transaction.PersonAlias = CurrentPersonAlias;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
                }
            }
            cbSMSSendToParents.Checked = false;
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
            maSent.Show( "Your message will be been sent shortly.", ModalAlertType.Information );
        }

        protected void btnEmail_Click( object sender, EventArgs e )
        {
            gMembers.SelectedKeys.Clear();
            hfCommunication.Value = null;
            // If we have a CommandArgument, then we should only select one member.
            if ( sender is LinkButton && !string.IsNullOrWhiteSpace( ( ( LinkButton ) sender ).CommandArgument ) )
            {
                hfCommunication.Value = ( ( LinkButton ) sender ).CommandArgument;
                gMembers.SelectedKeys.Add( ( ( LinkButton ) sender ).CommandArgument.AsInteger() );
            }
            tbBody.Text = "";
            tbSubject.Text = "";

            CurrentGroup.LoadAttributes();
            cbEmailSendToParents.Visible = CurrentGroup.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
            pnlMain.Visible = false;
            pnlEmail.Visible = true;

            DisplayEmailRecipients();
        }

        protected void btnEmailSend_Click( object sender, EventArgs e )
        {
            bool sendToParents = cbEmailSendToParents.Checked;

            if ( Page.IsValid )
            {
                _rockContext = new RockContext();
                var communication = UpdateCommunication( _rockContext );

                if ( communication != null )
                {
                    AddRecipients( communication, cbEmailSendToParents.Checked, EntityTypeCache.Get( new Guid(Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_EMAIL) ).Id );

                    communication.CommunicationType = CommunicationType.Email;

                    communication.Subject = tbSubject.Text;
                    communication.FromName = CurrentPerson.FullName;
                    communication.FromEmail = CurrentPerson.Email;
                    communication.ReplyToEmail = CurrentPerson.Email;
                    communication.Message = tbBody.Text.Replace( "\n", "<br />" );

                    communication.Status = CommunicationStatus.Approved;
                    communication.ReviewedDateTime = RockDateTime.Now;
                    communication.FutureSendDateTime = RockDateTime.Now;
                    communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                    _rockContext.SaveChanges();
                    var transaction = new Rock.Transactions.SendCommunicationTransaction();
                    transaction.CommunicationId = communication.Id;
                    transaction.PersonAlias = CurrentPersonAlias;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
                }
            }
            cbEmailSendToParents.Checked = false;
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
            maSent.Show( "Your message will be sent shortly.", ModalAlertType.Information );
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            cbSMSSendToParents.Checked = false;
            cbEmailSendToParents.Checked = false;
            hfCommunication.Value = null;
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
        }
        protected void cbSMSSendToParents_CheckedChanged( object sender, EventArgs e )
        {
            //update our recipient display on switch
            DisplaySMSRecipients();
        }

        protected void cbEmailSendToParents_CheckedChanged( object sender, EventArgs e )
        {
            //update our recipient display on switch
            DisplayEmailRecipients();
        }

        protected void gMembers_RowSelected( object sender, RowEventArgs e )
        {
            var id = e.RowKeyId;
            var member = memberData.Where( md => md.Id == id ).FirstOrDefault();
            if ( member == null )
            {
                return;
            }
            mdMember.Title = member.Name;

            hfMemberId.Value = member.Id.ToString();

            //set membership status and option buttons
            lbStatus.Text = member.Status.ToString();
            switch ( member.Status )
            {
                case GroupMemberStatus.Inactive:
                    lbStatus.LabelType = LabelType.Danger;
                    btnDeactivate.Visible = false;
                    btnActivate.Visible = true;
                    break;
                case GroupMemberStatus.Active:
                    lbStatus.LabelType = LabelType.Success;
                    btnActivate.Visible = false;
                    btnDeactivate.Visible = true;
                    break;
                case GroupMemberStatus.Pending:
                    lbStatus.LabelType = LabelType.Info;
                    btnDeactivate.Visible = false;
                    btnActivate.Visible = true;
                    break;
                default:
                    break;
            }
            //set Role tag
            lbRole.Text = member.Role.ToString();
            if ( member.IsLeader )
            {
                lbRole.LabelType = LabelType.Primary;
            }
            else
            {
                lbRole.LabelType = LabelType.Default;
            }

            //Set literal fields
            ltAddress.Text = member.FormattedAddress;
            ltPhone.Text = member.Phone;
            ltEmail.Text = member.Email;
            ltDateAdded.Text = member.DateAdded;

            //Show Date Inactive if not active
            if ( member.DateInactive != null )
            {
                ltDateInactive.Visible = true;
                ltDateInactive.Text = member.DateInactive.Value.ToShortDateString() + "<br>" + member.DateInactive.ToRelativeDateString();
            }
            else
            {
                ltDateInactive.Visible = false;
            }

            mdMember.Show();
        }

        protected void btnActivate_Click( object sender, EventArgs e )
        {
            int id = hfMemberId.Value.AsInteger();
            if ( id > 0 )
            {
                activateMember( id );
            }
            mdMember.Hide();
            hfMemberId.Value = null;
            GetGroupMembers();
            BindData();
        }

        protected void btnDeactivate_Click( object sender, EventArgs e )
        {
            int id = hfMemberId.Value.AsInteger();
            if ( id > 0 )
            {
                _rockContext = new RockContext();
             
                int? groupTypeId = new GroupService( _rockContext ).Get( id ).GroupTypeId;
                var groupTypeCache = GroupTypeCache.Get( groupTypeId.Value );
                if ( groupTypeCache.EnableGroupHistory == true )
                    
                {
                    archiveMember( id );
                }
                else
                {
                    deactivateMember( id );
                }
            }
            mdMember.Hide();
            hfMemberId.Value = null;
            GetGroupMembers();
            BindData();
        }

        protected void btnCloseModal_Click( object sender, EventArgs e )
        {
            mdMember.Hide();
            hfMemberId.Value = null;
        }

        private void deactivateMember( int id )
        {
            //Do not deactivate the last active leader.
            if ( memberData.Where( m => m.IsLeader && m.Status == GroupMemberStatus.Active ).Count() == 1
                && memberData.Where( m => m.Person.Id == id && m.IsLeader ).Any() )
            {
                return;
            }

            //Select multiple group members because someone can be a member of
            //a group more than once as long as their role is different
            var groupMembers = CurrentGroup.Members.Where( m => m.Person.Id == id );
            if ( groupMembers.Any() )
            {
                foreach ( var groupMember in groupMembers )
                {
                    var gm = new GroupMemberService( _rockContext ).Get( groupMember.Id );
                    if ( gm != null )
                    {
                        gm.GroupMemberStatus = GroupMemberStatus.Inactive;
                    }
                }
                _rockContext.SaveChanges();
            }
        }


        private void archiveMember( int id )
        {

            
            //Do not archive the last active leader.
            if ( memberData.Where( m => m.IsLeader && m.Status == GroupMemberStatus.Active ).Count() == 1
                && memberData.Where( m => m.Person.Id == id && m.IsLeader ).Any() )
            {
                return;
            }

            //Select multiple group members because someone can be a member of
            //a group more than once as long as their role is different
            var groupMembers = CurrentGroup.Members.Where( m => m.Person.Id == id );
            if ( groupMembers.Any() )
            {
                foreach ( var groupMember in groupMembers )
                {
                    var gm = new GroupMemberService( _rockContext ).Get( groupMember.Id );
                    if ( gm != null )
                    {
                        gm.IsArchived = true;
                        gm.ArchivedByPersonAliasId = CurrentPersonAliasId;
                        gm.ArchivedDateTime = RockDateTime.Now;
                    }
                }
                _rockContext.SaveChanges();
            }
        }


        private void activateMember( int id )
        {
            //Select multiple group members because someone can be a member of
            //a group more than once as long as their role is different
            var groupMembers = CurrentGroup.Members.Where( m => m.Person.Id == id );
            if ( groupMembers.Any() )
            {
                foreach ( var groupMember in groupMembers )
                {
                    var gm = new GroupMemberService( _rockContext ).Get( groupMember.Id );
                    if ( gm != null )
                    {
                        gm.GroupMemberStatus = GroupMemberStatus.Active;
                    }
                }
                _rockContext.SaveChanges();
            }
        }

        private void BindData()
        {
            gMembers.DataSource = memberData;
            gMembers.DataBind();

        }

        protected string StyleLeaderLabel( object IsLeader )
        {
            bool _isLeader = ( bool ) IsLeader;
            if ( _isLeader )
            {
                return "label-primary pull-right";
            }
            return "label-default pull-right";
        }

        protected string RosterLava( object Person )
        {
            var lava = GetAttributeValue( "RosterLava" );
            if ( !string.IsNullOrWhiteSpace( lava ) )
            {
                var _person = ( Person ) Person;
                _person.LoadAttributes();
                Dictionary<string, object> mergeObjects = new Dictionary<string, object> {
                    {"Person", _person },
                    {"CurrentPerson", CurrentPerson }
                };
                return lava.ResolveMergeFields( mergeObjects );
            }
            else
            {
                return "";
            }


        }

        protected string StyleStatusLabel( object Status )
        {
            GroupMemberStatus _status = ( GroupMemberStatus ) Status;
            switch ( _status )
            {
                case GroupMemberStatus.Inactive:
                    return "label-danger pull-right";
                case GroupMemberStatus.Active:
                    return "label-success pull-right";
                case GroupMemberStatus.Pending:
                    return "label-info pull-right";
                default:
                    return "label-info pull-right";
            }
        }

        private void BindRoster()
        {
            rRoster.DataSource = memberData.Where( m => m.Status != GroupMemberStatus.Inactive );
            rRoster.DataBind();
        }

        protected void rRoster_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var itemData = ( GroupMemberData ) e.Item.DataItem;
            if ( itemData != null )
            {
                BootstrapButton btnRosterEmail = e.Item.FindControl( "btnRosterEmail" ) as BootstrapButton;
                btnRosterEmail.Text = itemData.Email;
                btnRosterEmail.Click += ( a, b ) => showRosterEmail( itemData.Id );
            }
        }

        private void GenerateFilters( bool FirstRun )
        {
            var roles = CurrentGroup.GroupType.Roles.ToList();
            cblRole.DataSource = roles;
            cblRole.DataBind();

            cblGender.BindToEnum<Gender>();

            cblStatus.BindToEnum<GroupMemberStatus>();

            if ( FirstRun )
            {
                foreach ( ListItem item in cblRole.Items )
                {
                    item.Selected = true;
                }
                foreach ( ListItem item in cblGender.Items )
                {
                    item.Selected = true;
                }
                foreach ( ListItem item in cblStatus.Items )
                {
                    //Select all except for inactive
                    if ( item.Value != "0" )
                    {
                        item.Selected = true;
                    }
                }
            }
        }

        private void DisplaySMSRecipients()
        {
            var recipients = new StringBuilder();
            recipients.Append( "<div class=well><h4>Recipients:</h4>" );

            var sendParents = cbSMSSendToParents.Checked;
            var keys = gMembers.SelectedKeys;

            //List of ids so we don't display the same person twice
            List<int> addedIds = new List<int>();

            List<GroupMemberData> members = new List<GroupMemberData>();

            foreach ( int key in keys )
            {
                members.Add( memberData.Where( md => md.Id == key ).FirstOrDefault() );
            }

            if ( !members.Any() )
            {
                FilterData();
                members = memberData;
            }

            foreach ( var member in members )
            {
                //only load parents if we need them
                if ( sendParents )
                {
                    member.LoadParents();
                }

                if ( member.IsParent || !sendParents )
                {
                    if ( !addedIds.Contains( member.Id ) )
                    {
                        //Add person to recipients
                        if ( member.Phones.Where( pn => pn.IsMessagingEnabled && !pn.IsUnlisted ).Count() > 0 )
                        {
                            recipients.Append( "<span title='Person has textable number'>" + member.Name + "</span> " );
                        }
                        else
                        {
                            recipients.Append( "<span title='Person does not have a textable number and will not recieve this message.' style='color:red'>" + member.Name + "</span> " );
                        }
                        addedIds.Add( member.Id );
                    }
                }
                else
                {
                    //Add person parents to recipients
                    foreach ( Person parent in member.Parents )
                    {
                        if ( !addedIds.Contains( parent.Id ) )
                        {
                            if ( parent.PhoneNumbers.Where( pn => pn.IsMessagingEnabled && !pn.IsUnlisted ).Count() > 0 )
                            {
                                recipients.Append( "<span title='Person has textable number'>" + parent.FullName + "</span> " );
                            }
                            else
                            {
                                recipients.Append( "<span title='Person does not have a textable number and will not recieve this message.' style='color:red'>" + parent.FullName + "</span> " );
                            }
                            addedIds.Add( parent.Id );
                        }
                    }
                }
            }
            recipients.Append( "</div>" );
            ltSMSRecipients.Text = recipients.ToString();
        }

        private void DisplayEmailRecipients()
        {
            var recipients = new StringBuilder();
            recipients.Append( "<div class=well><h4>Recipients:</h4>" );

            var sendParents = cbEmailSendToParents.Checked;

            var keys = gMembers.SelectedKeys;

            //if we are calling this from the roster only choose that one
            if ( !string.IsNullOrWhiteSpace( hfCommunication.Value ) )
            {
                keys = new List<object> { hfCommunication.ValueAsInt() };
            }

            //This list is to keep track of recipients so we don't display them twice
            List<int> addedIds = new List<int>();

            List<GroupMemberData> members = new List<GroupMemberData>();

            foreach ( int key in keys )
            {
                members.Add( memberData.Where( md => md.Id == key ).FirstOrDefault() );
            }

            if ( !members.Any() )
            {
                FilterData();
                members = memberData;
            }

            foreach ( var member in members )
            {
                //only load parents if they are needed
                if ( sendParents )
                {
                    member.LoadParents();
                }

                if ( member.IsParent || !sendParents )
                {
                    if ( !addedIds.Contains( member.Id ) )
                    {
                        //Add person to recipients
                        //Check is for valid email, active email, not set to do not email
                        if ( !string.IsNullOrWhiteSpace( member.Email ) && member.Email.IsValidEmail() &&
                            member.Person.IsEmailActive && member.Person.EmailPreference != EmailPreference.DoNotEmail )
                        {
                            recipients.Append( "<span title='Person has a valid email address'>" + member.Name + "</span> " );
                        }
                        else
                        {
                            recipients.Append( "<span title='Person does not have a valid email address and will not recieve this message.' style='color:red'>" + member.Name + "</span> " );
                        }
                        //Save that we are sending a message to this person
                        addedIds.Add( member.Id );
                    }

                }
                else
                {
                    //Add person parents to recipients
                    foreach ( Person parent in member.Parents )
                    {
                        if ( !addedIds.Contains( parent.Id ) )
                        {
                            if ( !string.IsNullOrWhiteSpace( parent.Email ) && parent.Email.IsValidEmail() &&
                            parent.IsEmailActive && parent.EmailPreference != EmailPreference.DoNotEmail )
                            {
                                recipients.Append( "<span title='Person has a valid email address'>" + parent.FullName + "</span> " );
                            }
                            else
                            {
                                recipients.Append( "<span title='Person does not have a valid email address and will not recieve this message.' style='color:red'>" + parent.FullName + "</span> " );
                            }
                            addedIds.Add( parent.Id );
                        }

                    }
                }
            }
            recipients.Append( "</div>" );
            ltEmailRecipients.Text = recipients.ToString();
        }

        private Rock.Model.Communication UpdateCommunication( RockContext rockContext )
        {
            var communicationService = new CommunicationService( rockContext );
            var recipientService = new CommunicationRecipientService( rockContext );

            Rock.Model.Communication communication = null;
            IQueryable<CommunicationRecipient> qryRecipients = null;

            communication = new Rock.Model.Communication();
            communication.Status = CommunicationStatus.Transient;
            communication.SenderPersonAliasId = CurrentPersonAliasId;
            communicationService.Add( communication );

            qryRecipients = communication.GetRecipientsQry( rockContext );

            communication.IsBulkCommunication = false;

            communication.FutureSendDateTime = null;

            return communication;
        }

        private void AddRecipients( Communication communication, bool sendParents, int mediumEntityTypeId )
        {
            //List to keep from sending multiple messages
            List<int> addedIds = new List<int>();
            if ( !string.IsNullOrWhiteSpace( hfCommunication.Value ) )
            {
                //For adding a single person from the roster
                var member = memberData.Where( m => m.Id == hfCommunication.Value.AsIntegerOrNull() ).FirstOrDefault();
                if ( member != null )
                {
                    if ( member.IsParent || !sendParents )
                    {
                        if ( !addedIds.Contains( member.Id ) )
                        {
                            //Add person to Communication
                            var communicationRecipient = new CommunicationRecipient();
                            communicationRecipient.PersonAliasId = member.PersonAliasId;
                            communicationRecipient.MediumEntityTypeId = mediumEntityTypeId;
                            communication.Recipients.Add( communicationRecipient );
                            addedIds.Add( member.Id );
                        }

                    }
                    else
                    {
                        //Add parents to communication
                        foreach ( Person parent in member.Parents )
                        {
                            if ( !addedIds.Contains( parent.Id ) )
                            {
                                var communicationRecipient = new CommunicationRecipient();
                                communicationRecipient.PersonAliasId = parent.PrimaryAliasId ?? parent.Id;
                                communicationRecipient.MediumEntityTypeId = mediumEntityTypeId;
                                communication.Recipients.Add( communicationRecipient );
                                addedIds.Add( parent.Id );
                            }

                        }
                    }
                }
                //clear the value we are done
                hfCommunication.Value = "";
            }
            else
            {
                var keys = gMembers.SelectedKeys;

                List<GroupMemberData> members = new List<GroupMemberData>();

                foreach ( int key in keys )
                {
                    members.Add( memberData.Where( md => md.Id == key ).FirstOrDefault() );
                }

                if ( !members.Any() )
                {
                    FilterData();
                    members = memberData;
                }
                //For adding the communication recipients from the membership list 
                foreach ( var member in members )
                {
                    member.LoadParents();
                    if ( member.IsParent || !sendParents )
                    {
                        if ( !addedIds.Contains( member.Id ) )
                        {
                            //Add person to Communication
                            var communicationRecipient = new CommunicationRecipient();
                            communicationRecipient.PersonAliasId = member.PersonAliasId;
                            communicationRecipient.MediumEntityTypeId = mediumEntityTypeId;
                            communication.Recipients.Add( communicationRecipient );
                            addedIds.Add( member.Id );
                        }

                    }
                    else
                    {
                        //Add parents to communication
                        foreach ( Person parent in member.Parents )
                        {
                            if ( !addedIds.Contains( parent.Id ) )
                            {
                                var communicationRecipient = new CommunicationRecipient();
                                communicationRecipient.PersonAliasId = parent.PrimaryAliasId ?? parent.Id;
                                communicationRecipient.MediumEntityTypeId = mediumEntityTypeId;
                                communication.Recipients.Add( communicationRecipient );
                                addedIds.Add( parent.Id );
                            }
                        }
                    }
                }
            }
        }

        private void showRosterEmail( int? id )
        {
            hfCommunication.Value = id.ToString();
            CurrentGroup.LoadAttributes();
            cbEmailSendToParents.Visible = CurrentGroup.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
            pnlMain.Visible = false;
            pnlEmail.Visible = true;

            DisplayEmailRecipients();
        }



        protected void gMembers_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.Header )
            {
                //hides check boxes needed for printing
                e.Row.Cells[2].CssClass = "hide";
                e.Row.Cells[3].CssClass = "hide";
                e.Row.Cells[4].CssClass = "hide";
                e.Row.Cells[5].CssClass = "hide";
                e.Row.Cells[6].CssClass = "hide";
            }
        }


        /// <summary>
        /// Handles the Click event of the btnRegister control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnRegister_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid && CheckSettings() )
            {
                if ( _rockContext == null )
                {
                    _rockContext = new RockContext();
                }
                var personService = new PersonService( _rockContext );

                Person person = null;

                var changes = new List<string>();
                var spouseChanges = new List<string>();
                var familyChanges = new List<string>();

                // Try to find person by name/email 
                if ( person == null )
                {
                    var matches = GetByMatch( tbFirstName.Text.Trim(), tbLastName.Text.Trim(), dpBirthday.SelectedDate, pnCell.Text.Trim(), tbEmail.Text.Trim() );

                    //if matches is null it means that information wasn't entered correctly
                    if ( matches == null )
                    {
                        nbInvalid.Visible = true;
                        return;
                    }

                    if ( matches.Count() == 1 )
                    {
                        person = matches.First();
                    }
                }
                // Check to see if this is a new person
                if ( person == null )
                {
                    // If so, create the person and family record for the new person
                    person = new Person();
                    person.FirstName = tbFirstName.Text.Trim();
                    person.LastName = tbLastName.Text.Trim();
                    person.SetBirthDate( dpBirthday.SelectedDate );
                    person.UpdatePhoneNumber( DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id,
                        PhoneNumber.DefaultCountryCode(), pnCell.Text, true, false, _rockContext );
                    person.Email = tbEmail.Text.Trim();
                    person.IsEmailActive = true;
                    person.EmailPreference = EmailPreference.EmailAllowed;
                    person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    person.ConnectionStatusValueId = _dvcConnectionStatus.Id;
                    person.RecordStatusValueId = _dvcRecordStatus.Id;
                    person.Gender = Gender.Unknown;

                    PersonService.SaveNewPerson( person, _rockContext, CurrentGroup.CampusId, false );
                }

                // Save the registration
                AddPersonToGroup( _rockContext, person );
                pnlForm.Visible = false;
                pnlResults.Visible = true;
                ltResults.Text = person.FullName + " has been added to your group.";

                //Mark That We Created a New Person and Clear Form
                //hfUpdated.Value = "true";
                ClearForm();
            }
        }


        private void ClearForm()
        {
            tbFirstName.Text = "";
            tbLastName.Text = "";
            dpBirthday.Text = "";
            pnCell.Text = "";
            tbEmail.Text = "";
        }

        private List<Person> GetByMatch( string firstName, string lastName, DateTime? birthday, string cellPhone, string email )
        {

            cellPhone = PhoneNumber.CleanNumber( cellPhone ) ?? string.Empty;
            email = email ?? string.Empty;

            //Stop if first name or last name is blank or if all three of email, phone and birthday are blank
            if ( string.IsNullOrWhiteSpace( firstName ) || string.IsNullOrWhiteSpace( lastName )
                || !( !string.IsNullOrWhiteSpace( cellPhone ) || !string.IsNullOrWhiteSpace( email ) || birthday != null )
                )
            {
                return null;
            }

            //Search for person who matches first and last name and one of email, phone number, or birthday
            return new PersonService( _rockContext ).Queryable()
                         .Where( p => p.LastName == lastName
                                 && ( p.FirstName == firstName || p.NickName == firstName )
                                 && ( ( p.Email == email && p.Email != string.Empty )
                                 || ( p.PhoneNumbers.Where( pn => pn.Number == cellPhone ).Any() && cellPhone != string.Empty )
                                 || ( birthday != null && p.BirthDate == birthday ) ) )
                                 .ToList();
        }

        /// <summary>
        /// Adds the person to group.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="person">The person.</param>
        /// <param name="workflowType">Type of the workflow.</param>
        /// <param name="groupMembers">The group members.</param>
        private void AddPersonToGroup( RockContext rockContext, Person person )
        {
            if ( person != null )
            {
                if ( !CurrentGroup.Members
                    .Any( m =>
                        m.PersonId == person.Id &&
                        m.GroupRoleId == _defaultGroupRole.Id ) )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    var groupMember = new GroupMember();
                    groupMember.PersonId = person.Id;
                    groupMember.GroupRoleId = _defaultGroupRole.Id;
                    groupMember.GroupMemberStatus = ( GroupMemberStatus ) GetAttributeValue( "GroupMemberStatus" ).AsInteger();
                    groupMember.GroupId = CurrentGroup.Id;
                    groupMemberService.Add( groupMember );
                    rockContext.SaveChanges();
                }
                else
                {
                    foreach(var groupMember in CurrentGroup.Members
                        .Where( m => m.PersonId == person.Id &&
                            m.GroupRoleId == _defaultGroupRole.Id ))
                    {
                        var groupMemberService = new GroupMemberService( rockContext );
                        var efGroupMember = groupMemberService.Get( groupMember.Guid );
                        efGroupMember.GroupMemberStatus = ( GroupMemberStatus ) GetAttributeValue( "GroupMemberStatus" ).AsInteger();
                        debug.Text += groupMember.Person.FullName;
                    }
                    rockContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Checks the settings.
        /// </summary>
        /// <returns></returns>
        private bool CheckSettings()
        {
            _rockContext = _rockContext ?? new RockContext();

            if ( CurrentGroup == null )
            {
                NavigateToHomePage();
                return false;
            }

            //Authorization check. Nothing is visible otherwise
            if ( !CurrentGroup.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                return false;
            }

            _defaultGroupRole = CurrentGroup.GroupType.DefaultGroupRole;

            _dvcConnectionStatus = DefinedValueCache.Get( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
            if ( _dvcConnectionStatus == null )
            {
                return false;
            }

            _dvcRecordStatus = DefinedValueCache.Get( GetAttributeValue( "RecordStatus" ).AsGuid() );
            if ( _dvcRecordStatus == null )
            {
                return false;
            }

                        return true;

        }
        
        protected void btnAddMember_Click( object sender, EventArgs e )
        {

            if ( CheckSettings() )
            {
                mdAddMember.Show();
            }
        }

        protected void btnAddAnother_Click( object sender, EventArgs e )
        {
            pnlForm.Visible = true;
            pnlResults.Visible = false;
        }

        protected void btnClose_Click( object sender, EventArgs e )
        {
            mdAddMember.Hide();
            Response.Redirect( Request.RawUrl );
        }


        protected void btnRemoveMember_Click( object sender, EventArgs e )
        {

            int id = int.Parse( ( ( LinkButton ) sender ).CommandArgument );
            if ( id > 0 )
            {
                _rockContext = new RockContext();

                int? groupTypeId = new GroupService( _rockContext ).Get( CurrentGroup.Id ).GroupTypeId;
                var groupTypeCache = GroupTypeCache.Get( groupTypeId.Value );
                if ( groupTypeCache.EnableGroupHistory == true )

                {
                    archiveMember( id );
                }
                else
                {
                    deactivateMember( id );
                }
            }
            Response.Redirect( Request.RawUrl );
        }
    }

    public class GroupMemberData
    {
        public Person Person { get; private set; }
        
        public int Id
        {
            get
            {
                return Person.Id;
            }
        }
        public int PersonAliasId
        {
            get
            {
                return Person.PrimaryAliasId ?? Person.Id;
            }
        }
        public string Name
        {
            get
            {
                return Person.FullName;
            }
        }
        public string FirstName
        {
            get
            {
                return Person.FirstName;
            }
        }
        public string NickName
        {
            get
            {
                return Person.NickName;
            }
        }

        public string LastName
        {
            get
            {
                return Person.LastName;
            }
        }
        public string Phone
        {
            get
            {
                //gets first non unlisted non empty phone number with a preference to those with SMS enabled
                return Person.PhoneNumbers.Where( pn => !pn.IsUnlisted && !string.IsNullOrWhiteSpace( pn.Number ) )
                        .OrderByDescending( pn => pn.IsMessagingEnabled ).FirstOrDefault() != null
                    ? Person.PhoneNumbers.Where( pn => !pn.IsUnlisted && !string.IsNullOrWhiteSpace( pn.Number ) )
                        .OrderByDescending( pn => pn.IsMessagingEnabled ).FirstOrDefault().NumberFormatted
                    : "";
            }
        }
        public List<PhoneNumber> Phones
        {
            get
            {
                return Person.PhoneNumbers.ToList();
            }
        }
        public string Email
        {
            get
            {
                return Person.Email;
            }
        }
        public string DateAdded { get; private set; }
        public string Address { get; private set; }
        public string City { get; private set; }
        public string State { get; private set; }
        public string Zipcode { get; private set; }
        public string FormattedAddress
        {
            get
            {
                Location _address = Person.GetHomeLocation();
                if ( _address != null )
                {
                    return ( _address.Street1 ?? "" )
                            + ( !string.IsNullOrWhiteSpace( _address.Street2 ) ? "<br />" + _address.Street2 : "" )
                            + ( !( string.IsNullOrWhiteSpace( City ) && string.IsNullOrWhiteSpace( State ) && string.IsNullOrWhiteSpace( Zipcode ) ) ? "<br />" : "" )
                            + ( !string.IsNullOrWhiteSpace( City ) ? City + ", " : "" )
                            + State + Zipcode;
                }
                return "";
            }
        }
        public string Role { get; private set; }
        public bool IsLeader { get; private set; }
        public List<Person> Parents { get; private set; }
        public bool IsParent { get; private set; }
        public Gender Gender
        {
            get
            {
                return Person.Gender;
            }
        }
        public GroupMemberStatus Status { get; private set; }
        public DateTime? LastUpdate { get; private set; }
        public DateTime? DateInactive
        {
            get
            {
                if ( Status == GroupMemberStatus.Inactive )
                {
                    return LastUpdate;
                }
                else
                {
                    return null;
                }
            }
        }
        public string PhotoUrl
        {
            get
            {
                return Person.PhotoUrl;
            }
        }

        public GroupMemberData( GroupMember member )
        {
            this.Person = member.Person;
            if ( member.DateTimeAdded != null )
            {
                DateAdded = member.DateTimeAdded.Value.ToString( "d" );
            }

            Location _address = member.Person.GetHomeLocation();

            if ( _address != null )
            {
                Address = ( _address.Street1 ?? "" ) + " " + ( _address.Street2 ?? "" );
                City = _address.City ?? "";
                State = _address.State ?? "";
                Zipcode = _address.PostalCode ?? "";
            }
            else
            {
                Address = "";
                City = "";
                State = "";
                Zipcode = "";
            }

            Role = member.GroupRole.Name;
            IsLeader = member.GroupRole.IsLeader;
            Status = member.GroupMemberStatus;
            LastUpdate = member.ModifiedDateTime;
        }

        public void LoadParents()
        {
            Parents = new List<Person>();

            var families = this.Person.GetFamilies().ToList();
            var adultGuid = new Guid( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT );
            foreach ( var family in families )
            {
                var familyRoleGuid = family.Members.Where( gm => gm.PersonId == this.Person.Id ).FirstOrDefault().GroupRole.Guid;
                if ( familyRoleGuid == adultGuid )
                {
                    IsParent = true;
                }
                else
                {
                    IsParent = false;
                    Parents.AddRange( family.Members.Where( m => m.GroupRole.Guid == adultGuid ).Select( m => m.Person ).ToList() );
                }
            }
        }


    }
}
