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
using DotLiquid;
using org.secc.GroupManager;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "Group Roster" )]
    [Category( "Groups" )]
    [Description( "Presents members of group in roster format." )]

    //Settings
    [CodeEditorField( "Roster Lava", "Lava to appear in member roster pannels", CodeEditorMode.Lava, CodeEditorTheme.Rock, 600, false )]
    [BooleanField( "Allow Email", "Allow email to be sent from this block.", false )]
    [BooleanField( "Allow SMS", "Allow test messages to be sent from this block.", false )]
    public partial class GroupRoster : GroupManagerBlock
    {
        List<MemberData> memberData = new List<MemberData>();
        string smsScript = @"
            var charCount = function(){
                document.getElementById('charCount').innerHTML = $('textarea[id$= \'tbMessage\']').val().length + ' of 160';
            }
            
            $('textarea[id$= \'tbMessage\']').keyup(function(){charCount()});
            ";
        private RockContext _rockContext;


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
            BindData();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (CurrentGroup == null )
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
            ltTitle.Text = "<h1>" + CurrentGroup.Name + "</h1>";

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
            }
            BindRoster();
        }

        private void GetGroupMembers()
        {
            memberData = new List<MemberData>();
            var groupMembers = CurrentGroup.Members;
            foreach ( var member in groupMembers )
            {
                memberData.Add( new MemberData( member ) );
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

        protected void btnMembership_Click( object sender, EventArgs e )
        {
            btnMembership.CssClass = "btn btn-primary";
            btnRoster.CssClass = "btn btn-default";
            btnPrint.Text = "Print Attendance Sheet";
            pnlMembership.Visible = true;
            pnlRoster.Visible = false;
        }

        protected void btnRoster_Click( object sender, EventArgs e )
        {
            btnRoster.CssClass = "btn btn-primary";
            btnMembership.CssClass = "btn btn-default";
            btnPrint.Text = "Print Roster";
            pnlRoster.Visible = true;
            pnlMembership.Visible = false;
        }

        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            FilterData();
        }

        private void FilterData()
        {
            rFilter.Show();

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

            if ( gMembers.SelectedKeys.Count == 0 )
            {
                nbAlert.Text = "Please select members to text.";
                nbAlert.Visible = true;
                return;
            }

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
                    AddRecepients( communication, cbSMSSendToParents.Checked );

                    communication.MediumEntityTypeId = EntityTypeCache.Read( "Rock.Communication.Medium.Sms" ).Id;
                    communication.MediumData.Clear();
                    communication.MediumData.Add( "TextMessage", tbMessage.Text );
                    communication.MediumData.Add( "From", CurrentGroup.GetAttributeValue( "TextMessageFrom" ) );

                    communication.Status = CommunicationStatus.Approved;
                    communication.ReviewedDateTime = RockDateTime.Now;
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
            tbBody.Text = "";
            tbSubject.Text = "";

            if ( gMembers.SelectedKeys.Count == 0 )
            {
                nbAlert.Text = "Please select members to email.";
                nbAlert.Visible = true;
                return;
            }

            CurrentGroup.LoadAttributes();
            cbEmailSendToParents.Visible = CurrentGroup.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
            pnlMain.Visible = false;
            pnlEmail.Visible = true;

            DisplayEmailRecipients();
        }

        protected void btnEmailSend_Click( object sender, EventArgs e )
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
                    AddRecepients( communication, cbEmailSendToParents.Checked );

                    communication.MediumEntityTypeId = EntityTypeCache.Read( "Rock.Communication.Medium.Email" ).Id;
                    communication.MediumData.Clear();
                    communication.Subject = tbSubject.Text;
                    communication.MediumData.Add( "FromName", CurrentPerson.FullName );
                    communication.MediumData.Add( "FromAddress", CurrentPerson.Email );
                    communication.MediumData.Add( "ReplyToAddress", CurrentPerson.Email );
                    communication.MediumData.Add( "TextMessage", tbBody.Text );

                    communication.Status = CommunicationStatus.Approved;
                    communication.ReviewedDateTime = RockDateTime.Now;
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
            //update our recepient display on switch
            DisplaySMSRecipients();
        }

        protected void cbEmailSendToParents_CheckedChanged( object sender, EventArgs e )
        {
            //update our recepient display on switch
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
                deactivateMember( id );
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
                    groupMember.GroupMemberStatus = GroupMemberStatus.Inactive;
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
                    groupMember.GroupMemberStatus = GroupMemberStatus.Active;
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
            rRoster.DataSource = memberData.Where(m => m.Status!=GroupMemberStatus.Inactive);
            rRoster.DataBind();
        }

        protected void rRoster_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var itemData = ( MemberData ) e.Item.DataItem;
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
            var recepients = new StringBuilder();
            recepients.Append( "<div class=well><h4>Recepients:</h4>" );

            var sendParents = cbSMSSendToParents.Checked;

            //List of ids so we don't display the same person twice
            List<int> addedIds = new List<int>();

            foreach ( int key in gMembers.SelectedKeys )
            {
                MemberData member = memberData.Where( md => md.Id == key ).FirstOrDefault();

                //only load parents if we need them
                if ( sendParents )
                {
                    member.LoadParents();
                }

                if ( member.IsParent || !sendParents )
                {
                    if ( !addedIds.Contains( member.Id ) )
                    {
                        //Add person to recepients
                        if ( member.Phones.Where( pn => pn.IsMessagingEnabled && !pn.IsUnlisted ).Count() > 0 )
                        {
                            recepients.Append( "<span title='Person has textable number'>" + member.Name + "</span> " );
                        }
                        else
                        {
                            recepients.Append( "<span title='Person does not have a textable number and will not recieve this message.' style='color:red'>" + member.Name + "</span> " );
                        }
                        addedIds.Add( member.Id );
                    }
                }
                else
                {
                    //Add person parents to recepients
                    foreach ( Person parent in member.Parents )
                    {
                        if ( !addedIds.Contains( parent.Id ) )
                        {
                            if ( parent.PhoneNumbers.Where( pn => pn.IsMessagingEnabled && !pn.IsUnlisted ).Count() > 0 )
                            {
                                recepients.Append( "<span title='Person has textable number'>" + parent.FullName + "</span> " );
                            }
                            else
                            {
                                recepients.Append( "<span title='Person does not have a textable number and will not recieve this message.' style='color:red'>" + parent.FullName + "</span> " );
                            }
                            addedIds.Add( parent.Id );
                        }
                    }
                }
            }
            recepients.Append( "</div>" );
            ltSMSRecipients.Text = recepients.ToString();
        }

        private void DisplayEmailRecipients()
        {
            var recepients = new StringBuilder();
            recepients.Append( "<div class=well><h4>Recepients:</h4>" );

            var sendParents = cbEmailSendToParents.Checked;

            var keys = gMembers.SelectedKeys;

            //if we are calling this from the roster only choose that one
            if ( !string.IsNullOrWhiteSpace( hfCommunication.Value ) )
            {
                keys = new List<object> { hfCommunication.ValueAsInt() };
            }

            //This list is to keep track of recepients so we don't display them twice
            List<int> addedIds = new List<int>();

            foreach ( int key in keys )
            {
                MemberData member = memberData.Where( md => md.Id == key ).FirstOrDefault();


                //only load parents if they are needed
                if ( sendParents )
                {
                    member.LoadParents();
                }

                if ( member.IsParent || !sendParents )
                {
                    if ( !addedIds.Contains( member.Id ) )
                    {
                        //Add person to recepients
                        //Check is for valid email, active email, not set to do not email
                        if ( !string.IsNullOrWhiteSpace( member.Email ) && member.Email.IsValidEmail() &&
                            member.Person.IsEmailActive && member.Person.EmailPreference != EmailPreference.DoNotEmail )
                        {
                            recepients.Append( "<span title='Person has a valid email address'>" + member.Name + "</span> " );
                        }
                        else
                        {
                            recepients.Append( "<span title='Person does not have a valid email address and will not recieve this message.' style='color:red'>" + member.Name + "</span> " );
                        }
                        //Save that we are sending a message to this person
                        addedIds.Add( member.Id );
                    }

                }
                else
                {
                    //Add person parents to recepients
                    foreach ( Person parent in member.Parents )
                    {
                        if ( !addedIds.Contains( parent.Id ) )
                        {
                            if ( !string.IsNullOrWhiteSpace( parent.Email ) && parent.Email.IsValidEmail() &&
                            parent.IsEmailActive && parent.EmailPreference != EmailPreference.DoNotEmail )
                            {
                                recepients.Append( "<span title='Person has a valid email address'>" + parent.FullName + "</span> " );
                            }
                            else
                            {
                                recepients.Append( "<span title='Person does not have a valid email address and will not recieve this message.' style='color:red'>" + parent.FullName + "</span> " );
                            }
                            addedIds.Add( parent.Id );
                        }

                    }
                }
            }
            recepients.Append( "</div>" );
            ltEmailRecipients.Text = recepients.ToString();
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

        private void AddRecepients( Communication communication, bool sendParents )
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
                //For adding the communication recepients from the membership list 
                foreach ( int key in gMembers.SelectedKeys )
                {
                    MemberData member = memberData.Where( md => md.Id == key ).FirstOrDefault();
                    member.LoadParents();
                    if ( member.IsParent || !sendParents )
                    {
                        if ( !addedIds.Contains( member.Id ) )
                        {
                            //Add person to Communication
                            var communicationRecipient = new CommunicationRecipient();
                            communicationRecipient.PersonAliasId = member.PersonAliasId;
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
    }

    public class MemberData
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

        public MemberData( GroupMember member )
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
