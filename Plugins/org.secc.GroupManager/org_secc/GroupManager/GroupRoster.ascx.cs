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

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "Group Roster" )]
    [Category( "Groups" )]
    [Description( "Presents roster of group in roster format only if current user is leader of group." )]
    public partial class GroupRoster : RockBlock
    {
        Group group = new Group();
        List<MemberData> memberData = new List<MemberData>();
        string smsScript = @"
            var charCount = function(){
                document.getElementById('charCount').innerHTML = $('textarea[id$= \'tbMessage\']').val().length + ' of 160';
            }
            
            $('textarea[id$= \'tbMessage\']').keyup(function(){charCount()});
            ";

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gMembers.ShowActionRow = false;
            gMembers.PersonIdField = "Id";
            gMembers.DataKeyNames = new string[] { "Id" };
            gMembers.GridRebind += gMembers_GridRebind;
        }

        private void gMembers_GridRebind( object sender, EventArgs e )
        {
            BindData();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbAlert.Visible = false;
            var personAlias = RockPage.CurrentPersonAlias;
            int groupId;
            try
            {
                groupId = Int32.Parse( PageParameter( "GroupId" ) );
            }
            catch
            {
                return;
            }
            var rockContext = new RockContext();
            var groupService = new GroupService( rockContext );
            group = groupService.Get( groupId );

            //If you are not a leader when one is required hide and quit.
            if ( !group.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                pnlMain.Visible = false;
                nbAlert.Visible = true;
                nbAlert.Text = "You are not authorized to view this page.";
                return;
            }

            Page.Title = group.Name;
            ltTitle.Text = "<h1>" + group.Name + "</h1>";
            var groupMembers = groupService.Queryable().Where( g => g.Id == groupId ).SelectMany( g => g.Members ).ToList();
            foreach ( var member in groupMembers )
            {
                memberData.Add( new MemberData( member ) );
            }


            if ( !Page.IsPostBack )
            {
                GenerateFilters( true );
                BindData();
            }
            BindRoster();
        }
        protected void btnMembership_Click( object sender, EventArgs e )
        {
            btnMembership.CssClass = "btn btn-primary";
            btnRoster.CssClass = "btn btn-default";
            pnlMembership.Visible = true;
            pnlRoster.Visible = false;
        }

        protected void btnRoster_Click( object sender, EventArgs e )
        {
            btnRoster.CssClass = "btn btn-primary";
            btnMembership.CssClass = "btn btn-default";
            pnlRoster.Visible = true;
            pnlMembership.Visible = false;
        }

        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
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

            group.LoadAttributes();
            cbSMSSendToParents.Visible = group.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
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
                var rockContext = new RockContext();
                var communication = UpdateCommunication( rockContext );

                if ( communication != null )
                {
                    AddRecepients( communication, cbSMSSendToParents.Checked );

                    communication.MediumEntityTypeId = EntityTypeCache.Read( "Rock.Communication.Medium.Sms" ).Id;
                    communication.MediumData.Clear();
                    communication.MediumData.Add( "TextMessage", tbMessage.Text );
                    communication.MediumData.Add( "From", group.GetAttributeValue( "TextMessageFrom" ) );

                    communication.Status = CommunicationStatus.Approved;
                    communication.ReviewedDateTime = RockDateTime.Now;
                    communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                    rockContext.SaveChanges();
                    var transaction = new Rock.Transactions.SendCommunicationTransaction();
                    transaction.CommunicationId = communication.Id;
                    transaction.PersonAlias = CurrentPersonAlias;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
                }
            }
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
            maSent.Show( "Your message has been sent.", ModalAlertType.Information );
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

            group.LoadAttributes();
            cbEmailSendToParents.Visible = group.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
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
                var rockContext = new RockContext();
                var communication = UpdateCommunication( rockContext );

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

                    rockContext.SaveChanges();
                    var transaction = new Rock.Transactions.SendCommunicationTransaction();
                    transaction.CommunicationId = communication.Id;
                    transaction.PersonAlias = CurrentPersonAlias;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
                }
            }
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
            maSent.Show( "Your message has been sent.", ModalAlertType.Information );
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
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

        private void BindData()
        {
            gMembers.DataSource = memberData;
            gMembers.DataBind();

        }

        private void BindRoster()
        {
            rRoster.DataSource = memberData;
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
            var roles = group.GroupType.Roles.ToList();
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
                    item.Selected = true;
                }
            }
        }

        private void DisplaySMSRecipients()
        {
            var recepients = new StringBuilder();
            recepients.Append( "<div class=well><h4>Recepients:</h4>" );

            var sendParents = cbSMSSendToParents.Checked;

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
                    //Add person to recepients
                    if ( member.Phones.Where( pn => pn.IsMessagingEnabled && !pn.IsUnlisted ).Count() > 0 )
                    {
                        recepients.Append( "<span title='Person has textable number'>" + member.Name + "</span> " );
                    }
                    else
                    {
                        recepients.Append( "<span title='Person does not have a textable number and will not recieve this message.' style='color:red'>" + member.Name + "</span> " );
                    }
                }
                else
                {
                    //Add person parents to recepients

                    foreach ( Person parent in member.Parents )
                    {
                        if ( parent.PhoneNumbers.Where( pn => pn.IsMessagingEnabled && !pn.IsUnlisted ).Count() > 0 )
                        {
                            recepients.Append( "<span title='Person has textable number'>" + parent.FullName + "</span> " );
                        }
                        else
                        {
                            recepients.Append( "<span title='Person does not have a textable number and will not recieve this message.' style='color:red'>" + parent.FullName + "</span> " );
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

            var sendParents = cbSMSSendToParents.Checked;

            var keys = gMembers.SelectedKeys;

            //if we are calling this from the roster only choose that one
            if ( !string.IsNullOrWhiteSpace( hfCommunication.Value ) )
            {
                keys = new List<object> { hfCommunication.ValueAsInt() };
            }

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
                    //Add person to recepients
                    //Check is for valid email, active email, not set to do not email
                    if ( !string.IsNullOrWhiteSpace( member.Email ) && member.Email.IsValidEmail() &&
                        member.Person.IsEmailActive && !member.Person.EmailPreference.Equals( 2 ) )
                    {
                        recepients.Append( "<span title='Person has a valid email address'>" + member.Name + "</span> " );
                    }
                    else
                    {
                        recepients.Append( "<span title='Person does not have a valid email address and will not recieve this message.' style='color:red'>" + member.Name + "</span> " );
                    }
                }
                else
                {
                    //Add person parents to recepients
                    foreach ( Person parent in member.Parents )
                    {
                        if ( !string.IsNullOrWhiteSpace( member.Email ) && parent.Email.IsValidEmail() &&
                            parent.IsEmailActive && !parent.EmailPreference.Equals( 2 ) )
                        {
                            recepients.Append( "<span title='Person has a valid email address'>" + parent.FullName + "</span> " );
                        }
                        else
                        {
                            recepients.Append( "<span title='Person does not have a valid email address and will not recieve this message.' style='color:red'>" + parent.FullName + "</span> " );
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
            if ( !string.IsNullOrWhiteSpace( hfCommunication.Value ) )
            {
                //For adding a single person from the roster
                var member = memberData.Where( m => m.Id == hfCommunication.Value.AsIntegerOrNull() ).FirstOrDefault();
                if ( member != null )
                {
                    if ( member.IsParent || !sendParents )
                    {
                        //Add person to Communication
                        var communicationRecipient = new CommunicationRecipient();
                        communicationRecipient.PersonAliasId = member.Id ?? 0;
                        communication.Recipients.Add( communicationRecipient );
                    }
                    else
                    {
                        //Add parents to communication
                        foreach ( Person parent in member.Parents )
                        {
                            var communicationRecipient = new CommunicationRecipient();
                            communicationRecipient.PersonAliasId = parent.PrimaryAliasId ?? parent.Id;
                            communication.Recipients.Add( communicationRecipient );
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
                    if ( member.IsParent || !sendParents )
                    {
                        //Add person to Communication
                        var communicationRecipient = new CommunicationRecipient();
                        communicationRecipient.PersonAliasId = key;
                        communication.Recipients.Add( communicationRecipient );
                    }
                    else
                    {
                        //Add parents to communication
                        foreach ( Person parent in member.Parents )
                        {
                            var communicationRecipient = new CommunicationRecipient();
                            communicationRecipient.PersonAliasId = parent.PrimaryAliasId ?? parent.Id;
                            communication.Recipients.Add( communicationRecipient );
                        }
                    }
                }
            }
        }

        private void showRosterEmail( int? id )
        {
            hfCommunication.Value = id.ToString();
            group.LoadAttributes();
            cbEmailSendToParents.Visible = group.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
            pnlMain.Visible = false;
            pnlEmail.Visible = true;

            DisplayEmailRecipients();
        }
    }

    public class MemberData
    {
        public int? Id { get; set; }
        public Person Person { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public List<PhoneNumber> Phones { get; set; }
        public string Email { get; set; }
        public string DateAdded { get; set; }
        public string Address { get; set; }
        public string FormattedAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zipcode { get; set; }
        public string Role { get; set; }
        public List<Person> Parents { get; set; }
        public bool IsParent { get; set; }
        public Gender Gender { get; set; }
        public GroupMemberStatus Status { get; set; }
        public DateTime? DateInactive { get; set; }
        public string PhotoUrl { get; set; }

        public MemberData( GroupMember member )
        {
            Id = member.Person.PrimaryAliasId;
            this.Person = member.Person;
            Name = member.Person.FullName;
            FirstName = member.Person.FirstName;
            NickName = member.Person.NickName;
            LastName = member.Person.LastName;
            DateAdded = member.DateTimeAdded.Value.ToString( "d" );
            PhotoUrl = member.Person.PhotoUrl;
            Phone = member.Person.PhoneNumbers.Where( pn => !pn.IsUnlisted ).FirstOrDefault() != null ? member.Person.PhoneNumbers.Where( pn => !pn.IsUnlisted ).FirstOrDefault().NumberFormatted : "";
            Phones = member.Person.PhoneNumbers.ToList();
            Email = member.Person.Email;
            Location _address = member.Person.GetHomeLocation();

            if ( _address != null )
            {
                Address = ( _address.Street1 ?? "" ) + " " + ( _address.Street2 ?? "" );
                FormattedAddress = ( _address.Street1 ?? "" ) +
                                   ( !string.IsNullOrWhiteSpace( _address.Street2 ) ? "<br />" + _address.Street2 : "" );
                City = _address.City ?? "";
                State = _address.State ?? "";
                Zipcode = _address.PostalCode ?? "";
            }
            else
            {
                Address = "";
                City = "";
                State = "";
            }

            Role = member.GroupRole.Name;
            Gender = member.Person.Gender;
            Status = member.GroupMemberStatus;
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
