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
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Collections.Generic;

namespace RockWeb.Blocks.Reporting.NextGen
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName("Bible & Beach")]
    [Category("SECC > Reporting > NextGen")]
    [Description("A report for managing the trip attendeeds for NextGen's Bible & Beach Trip.")]
    [GroupField("Group", "The group for managing the members of this trip.", true)]
    [LinkedPage("Registrant Page", "The page for viewing the registrant.", true)]
    [LinkedPage("Group Member Detail Page", "The page for editing the group member.", true)]
    public partial class BibleAndBeach : RockBlock
    {


        string keyPrefix = "";

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            keyPrefix = string.Format("{0}-{1}-", GetType().Name, BlockId);
            base.OnLoad(e);

            if (string.IsNullOrWhiteSpace(GetAttributeValue("Group")))
            {
                ShowMessage("Block not configured. Please configure to use.", "Configuration Error", "panel panel-danger");
                return;
            }

            gReport.GridRebind += gReport_GridRebind;

            if (!Page.IsPostBack)
            {
                BindGrid();
                LoadGridFilters();
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gReport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gReport_GridRebind(object sender, EventArgs e)
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            using (var rockContext = new RockContext())
            {

                var groupMemberService = new GroupMemberService(rockContext);
                var groupService = new GroupMemberService(rockContext);
                var attributeService = new AttributeService(rockContext);
                var attributeValueService = new AttributeValueService(rockContext);
                var personAliasService = new PersonAliasService(rockContext);
                var entityTypeService = new EntityTypeService(rockContext);
                var registrationRegistrantService = new RegistrationRegistrantService(rockContext);
                var eiogmService = new EventItemOccurrenceGroupMapService(rockContext);


                Guid bbGroup = GetAttributeValue("Group").AsGuid();


                int entityTypeId = entityTypeService.Queryable().Where(et => et.Name == typeof(Rock.Model.Group).FullName).FirstOrDefault().Id;
                var group = new GroupService(rockContext).Get(bbGroup);
                var registrationTemplateIds = eiogmService.Queryable().Where(r => r.GroupId == group.Id).Select(m => m.RegistrationInstance.RegistrationTemplateId.ToString()).ToList();

                hlGroup.NavigateUrl = "/group/" + group.Id;

                var attributeIds = attributeService.Queryable()
                    .Where(a => (a.EntityTypeQualifierColumn == "GroupId" && a.EntityTypeQualifierValue == group.Id.ToString()) ||
                   (a.EntityTypeQualifierColumn == "GroupTypeId" && a.EntityTypeQualifierValue == group.GroupTypeId.ToString()) ||
                   (a.EntityTypeQualifierColumn == "RegistrationTemplateId" && registrationTemplateIds.Contains(a.EntityTypeQualifierValue)))
                    .Select(a => a.Id).ToList();

                var gmTmpqry = groupMemberService.Queryable()
                     .Where(gm => (gm.GroupId == group.Id));

                var qry = gmTmpqry
                    .Join(registrationRegistrantService.Queryable(),
                        obj => obj.Id,
                        rr => rr.GroupMemberId,
                        (obj, rr) => new { GroupMember = obj, RegistrationRegistrant = rr })
                    .GroupJoin(attributeValueService.Queryable(),
                        obj => obj.GroupMember.Id,
                        av => av.EntityId.Value,
                        (obj, avs) => new { GroupMember = obj.GroupMember, RegistrationRegistrant = obj.RegistrationRegistrant, GroupMemberAttributeValues = avs.Where(av => attributeIds.Contains(av.AttributeId)) })
                    .GroupJoin(attributeValueService.Queryable(),
                        obj => obj.RegistrationRegistrant.Id,
                        av => av.EntityId.Value,
                        (obj, avs) => new { GroupMember = obj.GroupMember, RegistrationRegistrant = obj.RegistrationRegistrant, GroupMemberAttributeValues = obj.GroupMemberAttributeValues, RegistrationAttributeValues = avs.Where(av => attributeIds.Contains(av.AttributeId)) });
                //var qry = qrytest.ToList();

                if (!String.IsNullOrWhiteSpace(GetUserPreference(string.Format("{0}PersonName", keyPrefix)))) {
                    string personName = GetUserPreference(string.Format("{0}PersonName", keyPrefix)).ToLower();
                    qry = qry.ToList().Where(q => q.GroupMember.Person.FullName.ToLower().Contains(personName)).AsQueryable();
                }
                decimal? lowerVal = GetUserPreference(string.Format("{0}BalanceOwedLower", keyPrefix)).AsDecimalOrNull();
                decimal? upperVal = GetUserPreference(string.Format("{0}BalanceOwedUpper", keyPrefix)).AsDecimalOrNull();

                if (lowerVal != null && upperVal != null)
                {
                    qry = qry.ToList().Where(q => q.RegistrationRegistrant.Registration.BalanceDue >= lowerVal && q.RegistrationRegistrant.Registration.BalanceDue <= upperVal).AsQueryable();
                }
                else if (lowerVal != null)
                {
                    qry = qry.ToList().Where(q => q.RegistrationRegistrant.Registration.BalanceDue >= lowerVal).AsQueryable();
                }
                else if (upperVal != null)
                {
                    qry = qry.ToList().Where(q => q.RegistrationRegistrant.Registration.BalanceDue <= upperVal).AsQueryable();
                }


                var newQry = qry.ToList().Select(g => new
                {
                    Id = g.GroupMember.Id,
                    RegistrationId = g.RegistrationRegistrant.RegistrationId,
                    Group = g.GroupMember.Group,
                    Person = g.GroupMember.Person,
                    DOB = g.GroupMember.Person.BirthDate.HasValue ? g.GroupMember.Person.BirthDate.Value.ToShortDateString() : "",
                    Address = g.GroupMember.Person.GetHomeLocation(),
                    Email = g.GroupMember.Person.Email,
                    Gender = g.GroupMember.Person.Gender, // (B & B Registration)
                    GraduationYearProfile = g.GroupMember.Person.GraduationYear, // (Person Profile)
                    HomePhone = g.GroupMember.Person.PhoneNumbers.Where(pn => pn.NumberTypeValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid()).Select(pn => pn.NumberFormatted).FirstOrDefault(),
                    CellPhone = g.GroupMember.Person.PhoneNumbers.Where(pn => pn.NumberTypeValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid()).Select(pn => pn.NumberFormatted).FirstOrDefault(),
                    GroupMember = new Func<GroupMemberAttributes>(() =>
                    {
                        GroupMemberAttributes gma = new GroupMemberAttributes(g.GroupMember, g.GroupMemberAttributeValues);
                        return gma;
                    })(),
                    Registrant = new Func<RegistrantAttributes>(() =>
                    {
                        RegistrantAttributes row = new RegistrantAttributes(g.RegistrationRegistrant, g.RegistrationAttributeValues);
                        return row;
                    })(),
                    LegalRelease = "", // (highest level form on record, pulled from forms page in Rock)
                    Departure = "", // (hopefully based on bus, otherwise a dropdown with 1-4) 
                    FamilyGroup = "", // (open type field)
                    Campus = "920", // 
                    Role = "Student", // 
                    HSMGroup = g.GroupMember.Group.Name
                }).OrderBy(w => w.Person.LastName).ToList().AsQueryable();

                if (!String.IsNullOrWhiteSpace(GetUserPreference(string.Format("{0}POA", keyPrefix))))
                {
                    string poa = GetUserPreference(string.Format("{0}POA", keyPrefix));
                    newQry = newQry.Where(q => q.GroupMember.POA == poa);
                }

                SortProperty sortProperty = gReport.SortProperty;
                if (sortProperty != null)
                {
                    gReport.SetLinqDataSource(newQry.Sort(sortProperty));
                }
                else
                {
                    gReport.SetLinqDataSource(newQry.OrderBy(p => p.Person.LastName));
                }
                gReport.DataBind();


            }
        }

        /// <summary>
        /// Loads the grid filter values.
        /// </summary>
        private void LoadGridFilters()
        {
            txtPersonName.Text = GetUserPreference(string.Format("{0}PersonName", keyPrefix));
            nreBalanceOwed.LowerValue = GetUserPreference(string.Format("{0}BalanceOwedLower", keyPrefix)).AsDecimalOrNull();
            nreBalanceOwed.UpperValue = GetUserPreference(string.Format("{0}BalanceOwedUpper", keyPrefix)).AsDecimalOrNull();
            ddlPOA.DataSource = new List<string> { "", "Yes", "No", "N/A" };
            ddlPOA.DataBind();
            ddlPOA.SelectedValue = GetUserPreference(string.Format("{0}POA", keyPrefix));
        }

        private void ShowMessage(string message, string header = "Information", string cssClass = "panel panel-warning")
        {
            pnlMain.Visible = false;
            pnlInfo.Visible = true;
            ltHeading.Text = header;
            ltBody.Text = message;
            pnlInfo.CssClass = cssClass;
        }

        protected void btnReopen_Command(object sender, CommandEventArgs e)
        {
            using (RockContext rockContext = new RockContext())
            {

                WorkflowService workflowService = new WorkflowService(rockContext);
                Workflow workflow = workflowService.Get(e.CommandArgument.ToString().AsInteger());
                if (workflow != null && !workflow.IsActive)
                {
                    workflow.Status = "Active";
                    workflow.CompletedDateTime = null;

                    // Find the summary activity and activate it.
                    WorkflowActivityType workflowActivityType = workflow.WorkflowType.ActivityTypes.Where(at => at.Name.Contains("Summary")).FirstOrDefault();
                    WorkflowActivity workflowActivity = WorkflowActivity.Activate(workflowActivityType, workflow, rockContext);

                }
                rockContext.SaveChanges();
            }
            BindGrid();
            LoadGridFilters();
        }
        #endregion

        protected void gReport_RowSelected(object sender, RowEventArgs e)
        {
            iGroupMemberIframe.Src = LinkedPageUrl("GroupMemberDetailPage", new Dictionary<string, string>() { { "GroupMemberId", e.RowKeyValues[0].ToString() } });
            mdEditRow.Show();
        }

        class GroupMemberAttributes : IComparable
        {
            public GroupMemberAttributes(GroupMember groupMember, IEnumerable<AttributeValue> attributeValues)
            {
                GroupMember = groupMember;
                AttributeValues = attributeValues;
            }

            public GroupMember GroupMember { get; set; }
            private IEnumerable<AttributeValue> AttributeValues { get; set; }

            public int Id { get { return GroupMember.Id; } }
            public String GeneralNotes { get { return GetAttributeValue("GeneralNotes"); } }
            public String RoomingNotes { get { return GetAttributeValue("RoomingNotes"); } }
            public String MedicalNotes { get { return GetAttributeValue("MedicalNotes"); } }
            public String TravelNotes { get { return GetAttributeValue("TravelNotes"); } }
            public String TravelExceptions { get { return GetAttributeValue("TravelExceptions"); } }
            public String POA { get {
                    if (GroupMember.Person.Age > 18)
                    {
                        return "N/A";
                    }
                    return GetAttributeValue("POA").AsBoolean()?"Yes":"No";
            } }
            public String FamilyGroup { get { return GetAttributeValue("FamilyGroup"); } }
            public String RoomCode { get { return GetAttributeValue("HotelRoomCode"); } }
            public String Bus { get { return GetAttributeValue("Bus"); } }

            public int CompareTo(object obj)
            {
                return Id.CompareTo(obj);
            }

            private String GetAttributeValue(string key)
            {
                return AttributeValues.Where(av => av.AttributeKey == key).Select(av => av.Value).FirstOrDefault();
            }
        }

        class RegistrantAttributes : IComparable
        {
            public RegistrantAttributes(RegistrationRegistrant registrant, IEnumerable<AttributeValue> attributeValues)
            {
                Registrant = registrant;
                AttributeValues = attributeValues;
            }

            public RegistrationRegistrant Registrant { get; set; }
            private IEnumerable<AttributeValue> AttributeValues { get; set; }

            public int Id { get { return Registrant.Id; } }

            public Decimal BalanceOwed
            {
                get { return Registrant.Registration.BalanceDue; }
            }
            public String BirthDate
            {
                get { return GetAttributeValue("Birthdate").AsDateTime().HasValue ? GetAttributeValue("Birthdate").AsDateTime().Value.ToShortDateString() : null; }
            }
            public String GraduationYear
            {
                get { return GetAttributeValue("CurrentGradeProjectedGraduationYear"); }
            }
            public String ParentName
            {
                get { return GetAttributeValue("ParentGuardianName"); }
            }
            public String ParentCell
            {
                get { return GetAttributeValue("BestParentCellPhoneNumber"); }
            }
            public String EmName
            {
                get { return GetAttributeValue("EmergencyContactName"); }
            }
            public String EmCell
            {
                get { return GetAttributeValue("EmergencyContactCellPhoneNumber"); }
            }
            public String School
            {
                get { return Registrant.GetAttributeValue("WheredoyouattendSchool"); }
            }
            public bool FirstBB
            {
                get { return Registrant.GetAttributeValue("IsthisyourfirstBB").AsBoolean(); }
            }
            public String Church
            {
                get { return GetAttributeValue("HomeChurchyouregularlyattend"); }
            }
            public bool GroupRoom
            {
                get { return GetAttributeValue("DoyouprefertoroomwithmembersofyourHSMGroup").AsBoolean(); }
            }
            public String Roommate
            {
                get { return GetAttributeValue("Roommate1FirstandLastName"); }
            }
            public String Roommate2
            {
                get { return GetAttributeValue("Roommate2FirstandLastName"); }
            }
            public String TShirtSize
            {
                get { return GetAttributeValue("T-ShirtSize"); }
            }
            public String DietaryInfo
            {
                get { return GetAttributeValue("Listofdietaryallergies"); }
            }

            public int CompareTo(object obj)
            {
                return Id.CompareTo(obj);
            }
            private String GetAttributeValue(string key)
            {
                return AttributeValues.Where(av => av.AttributeKey == key).Select(av => av.Value).FirstOrDefault();
            }
        }

        protected void btnRegistration_Click(object sender, RowEventArgs e)
        {
            
            var key = e.RowKeyValues[1].ToString();
            NavigateToLinkedPage("RegistrantPage", new Dictionary<string, string>() { { "RegistrationId", key } });

        }

        protected void gfReport_ApplyFilterClick(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtPersonName.Text))
                SetUserPreference(string.Format("{0}PersonName", keyPrefix), txtPersonName.Text);
            else
                DeleteUserPreference(string.Format("{0}PersonName", keyPrefix));

            if (nreBalanceOwed.LowerValue.HasValue)
                SetUserPreference(string.Format("{0}BalanceOwedLower", keyPrefix), nreBalanceOwed.LowerValue.Value.ToString());
            else
                DeleteUserPreference(string.Format("{0}BalanceOwedLower", keyPrefix));

            if (nreBalanceOwed.UpperValue.HasValue)
                SetUserPreference(string.Format("{0}BalanceOwedUpper", keyPrefix), nreBalanceOwed.UpperValue.Value.ToString());
            else
                DeleteUserPreference(string.Format("{0}BalanceOwedUpper", keyPrefix));

            if (!string.IsNullOrWhiteSpace(ddlPOA.SelectedValue))
                SetUserPreference(string.Format("{0}POA", keyPrefix), ddlPOA.SelectedValue);
            else
                DeleteUserPreference(string.Format("{0}POA", keyPrefix));
            BindGrid();
        }

        protected void gfReport_ClearFilterClick(object sender, EventArgs e)
        {
            DeleteUserPreference(string.Format("{0}PersonName", keyPrefix));
            DeleteUserPreference(string.Format("{0}BalanceOwedLower", keyPrefix));
            DeleteUserPreference(string.Format("{0}BalanceOwedUpper", keyPrefix));
            DeleteUserPreference(string.Format("{0}POA", keyPrefix));
            LoadGridFilters();
            BindGrid();
        }
    }
}