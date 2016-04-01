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

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName("Group Roster")]
    [Category("Groups")]
    [Description("Presents roster of group in roster format only if current user is leader of group.")]
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

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            gMembers.ShowActionRow = false;
            gMembers.PersonIdField = "Id";
            gMembers.DataKeyNames = new string[] { "Id" };
            gMembers.GridRebind += gMembers_GridRebind;
        }

        private void gMembers_GridRebind(object sender, EventArgs e)
        {
            BindData();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var personAlias = RockPage.CurrentPersonAlias;
            int groupId;
            try
            {
                groupId = Int32.Parse(PageParameter("GroupId"));
            }
            catch
            {
                return;
            }
            var rockContext = new RockContext();
            var groupService = new GroupService(rockContext);
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
            ltTitle.Text = "<h1>"+group.Name+"</h1>";
            var groupMembers = groupService.Queryable().Where(g => g.Id == groupId).SelectMany(g => g.Members).ToList();
            foreach (var member in groupMembers)
            {
                memberData.Add(new MemberData(member));
            }


            if (!Page.IsPostBack)
            {
                GenerateFilters(true);
                BindData();
            }
        }

        private void GenerateFilters(bool FirstRun)
        {
            var roles = group.GroupType.Roles.ToList();
            cblRole.DataSource=roles;
            cblRole.DataBind();

            cblGender.BindToEnum<Gender>();

            cblStatus.BindToEnum<GroupMemberStatus>();

            if (FirstRun)
            {
                foreach (ListItem item in cblRole.Items)
                {
                    item.Selected = true;
                }
                foreach (ListItem item in cblGender.Items)
                {
                    item.Selected = true;
                }
                foreach (ListItem item in cblStatus.Items)
                {
                    item.Selected = true;
                }
            }
        }

        private void BindData()
        {
            
            gMembers.DataSource = memberData;
            gMembers.DataBind();
            
        }

        protected void rFilter_ApplyFilterClick(object sender, EventArgs e)
        {
            rFilter.Show();

            var genders = cblGender.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)).ToList();
            var roles = cblRole.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => i.Value).ToList();
            var status = cblStatus.Items.Cast<ListItem>().Where(i => i.Selected).Select(i => int.Parse(i.Value)).ToList();

            memberData = memberData.Where(m =>
                genders.Contains(m.Gender.ConvertToInt()) &&
                roles.Contains(m.Role) &&
                status.Contains(m.Status.ConvertToInt())
                ).ToList();
            if (txtFirstName.Text != string.Empty)
            {
                memberData = memberData.Where(m => m.FirstName.ToLower().Contains(txtFirstName.Text.ToLower()) 
                || m.NickName.ToLower().Contains(txtFirstName.Text.ToLower())).ToList();
            }
            if (txtLastName.Text != string.Empty)
            {
                memberData = memberData.Where(m => m.LastName.ToLower().Contains(txtLastName.Text.ToLower())).ToList();
            }

            BindData();
        }

        protected void btnSMS_Click(object sender, EventArgs e)
        {
            var key = gMembers.SelectedKeys;
            if (key.Count == 0)
            {
                return;
            }

            group.LoadAttributes();
            cbSMSSendToParents.Visible = group.GetAttributeValue( "AllowEmailParents" ).AsBoolean();
            pnlMain.Visible = false;
            pnlSMS.Visible = true;

            ScriptManager.RegisterStartupScript(Page, Page.GetType(), "smsCharCount", smsScript, true);

        }

        protected void btnEmail_Click(object sender, EventArgs e)
        {
            var key = gMembers.SelectedKeys;
            if (key.Count == 0)
            {
                return;
            }

            group.LoadAttributes();
            cbEmailSendToParents.Visible = group.GetAttributeValue("AllowEmailParents").AsBoolean();
            pnlMain.Visible = false;
            pnlEmail.Visible = true;

        }

        protected void btnSMSSend_Click(object sender, EventArgs e)
        {
            var keys = gMembers.SelectedKeys;
            if (keys.Count == 0)
            {
                return;
            }
            bool sendToParents = cbEmailSendToParents.Checked;
            string subject = tbSubject.Text;
            string body = tbBody.Text;

            if (Page.IsValid)
            {
                var rockContext = new RockContext();
                var communication = UpdateCommunication(rockContext);

                if (communication != null)
                {
                    AddRecepients(communication, cbSMSSendToParents.Checked);

                    communication.MediumEntityTypeId = EntityTypeCache.Read("Rock.Communication.Medium.Sms").Id;
                    communication.MediumData.Clear();
                    communication.MediumData.Add("TextMessage", tbMessage.Text);
                    communication.MediumData.Add("From", group.GetAttributeValue("TextMessageFrom"));

                    communication.Status = CommunicationStatus.Approved;
                    communication.ReviewedDateTime = RockDateTime.Now;
                    communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                    rockContext.SaveChanges();
                    var transaction = new Rock.Transactions.SendCommunicationTransaction();
                    transaction.CommunicationId = communication.Id;
                    transaction.PersonAlias = CurrentPersonAlias;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue(transaction);
                }
            }
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
            maSent.Show("Your message has been sent. (Maybe?)", ModalAlertType.Information);
        }
        protected void btnEmailSend_Click(object sender, EventArgs e)
        {
            var key = gMembers.SelectedKeys;
            if (key.Count == 0)
            {
                return;
            }
            bool sendToParents = cbEmailSendToParents.Checked;
            string subject = tbSubject.Text;
            string body = tbBody.Text;

            if (Page.IsValid)
            {
                var rockContext = new RockContext();
                var communication = UpdateCommunication(rockContext);

                if (communication != null)
                {
                    AddRecepients(communication, cbEmailSendToParents.Checked);

                    communication.MediumEntityTypeId = EntityTypeCache.Read("Rock.Communication.Medium.Email").Id;
                    communication.MediumData.Clear();
                    communication.Subject = tbSubject.Text;
                    communication.MediumData.Add("FromName", CurrentPerson.FullName);
                    communication.MediumData.Add("FromAddress", CurrentPerson.Email);
                    communication.MediumData.Add("ReplyToAddress", CurrentPerson.Email);
                    communication.MediumData.Add("TextMessage", tbBody.Text);

                    communication.Status = CommunicationStatus.Approved;
                    communication.ReviewedDateTime = RockDateTime.Now;
                    communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                    rockContext.SaveChanges();
                    var transaction = new Rock.Transactions.SendCommunicationTransaction();
                    transaction.CommunicationId = communication.Id;
                    transaction.PersonAlias = CurrentPersonAlias;
                    Rock.Transactions.RockQueue.TransactionQueue.Enqueue(transaction);
                }
            }
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
            maSent.Show("Your message has been sent.", ModalAlertType.Information);
        }



        protected void btnCancel_Click(object sender, EventArgs e)
        {
            pnlEmail.Visible = false;
            pnlSMS.Visible = false;
            pnlMain.Visible = true;
        }


        /// <summary>
        /// Updates a communication model with the user-entered values
        /// </summary>
        /// <param name="communicationService">The service.</param>
        /// <returns></returns>
        private Rock.Model.Communication UpdateCommunication(RockContext rockContext)
        {
            var communicationService = new CommunicationService(rockContext);
            var recipientService = new CommunicationRecipientService(rockContext);

            Rock.Model.Communication communication = null;
            IQueryable<CommunicationRecipient> qryRecipients = null;

            communication = new Rock.Model.Communication();
            communication.Status = CommunicationStatus.Transient;
            communication.SenderPersonAliasId = CurrentPersonAliasId;
            communicationService.Add(communication);

            qryRecipients = communication.GetRecipientsQry(rockContext);

            communication.IsBulkCommunication = false;

            communication.FutureSendDateTime = null;

            return communication;
        }
        private void AddRecepients(Communication communication, bool sendParents)
        {
            foreach (int key in gMembers.SelectedKeys)
            {
                MemberData member = memberData.Where(md => md.Id == key).FirstOrDefault();
                if (member.IsParent || !sendParents)
                {
                    var communicationRecipient = new CommunicationRecipient();
                    communicationRecipient.PersonAliasId = key;
                    communication.Recipients.Add(communicationRecipient);
                }
                else
                {
                    foreach (int parent in member.Parents)
                    {
                        var communicationRecipient = new CommunicationRecipient();
                        communicationRecipient.PersonAliasId = parent;
                        communication.Recipients.Add(communicationRecipient);
                    }
                }
            }
        }
    }

    public class MemberData
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public string DateAdded { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Role { get; set; }
        public List<int?> Parents { get; set; }
        public bool IsParent { get; set; }
        public Gender Gender { get; set; }
        public GroupMemberStatus Status { get; set; }
        public DateTime? DateInactive { get; set; }

        public MemberData(GroupMember member)
        {
            Id = member.Person.PrimaryAliasId;
            Name = member.Person.FullName;
            FirstName = member.Person.FirstName;
            NickName = member.Person.NickName;
            LastName = member.Person.LastName;
            DateAdded = member.DateTimeAdded.Value.ToString("d");
            Location _address = member.Person.GetHomeLocation();

            if (_address != null)
            {
                Address = (_address.Street1 ?? "")+ " " + (_address.Street2 ?? "");
                City = _address.City ?? "";
                State = _address.State ?? "";
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

            GetParents(member);
            
        }

        private void GetParents(GroupMember member)
        {
            Parents = new List<int?>();

            var families = member.Person.GetFamilies().ToList();
            var adultGuid = new Guid(Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT);
            foreach (var family in families)
            {
                var familyRoleGuid = family.Members.Where(gm => gm.PersonId == member.PersonId).FirstOrDefault().GroupRole.Guid;
                if (familyRoleGuid == adultGuid)
                {
                    IsParent = true;
                }
                else
                {
                    IsParent = false;
                    Parents.AddRange(family.Members.Where(m => m.GroupRole.Guid == adultGuid).Select(m => m.Person).Select(p => p.PrimaryAliasId).ToList());
                }
            }
        }
    }

}