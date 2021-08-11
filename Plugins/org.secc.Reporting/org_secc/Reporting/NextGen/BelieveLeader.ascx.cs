﻿// <copyright>
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
using System.Text.RegularExpressions;
using System.Collections;
using System.Reflection;
using Rock.Web.Cache;
using System.Diagnostics;
using Newtonsoft.Json;

namespace RockWeb.Blocks.Reporting.NextGen
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName("Believe Leader")]
    [Category("SECC > Reporting > NextGen")]
    [Description("A report for managing the trip attendeeds for NextGen's Bible & Beach Trip.")]
    [GroupField("Group", "The group for managing the members of this trip.", true)]
    [LinkedPage("Registrant Page", "The page for viewing the registrant.", true)]
    [LinkedPage("Group Member Detail Page", "The page for editing the group member.", true)]
    [GroupTypeField("MSM Group Type", "The HSM Group Type to use for the HSM Group Field.", true)]
    [CustomCheckboxListField("Signature Document Templates", "The signature document templates to include.", "select id as value, name as text from SignatureDocumentTemplate")]
    public partial class BelieveLeader : RockBlock
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
            gReport.Actions.CommunicateClick += Actions_CommunicateClick;

                if (string.IsNullOrWhiteSpace(GetAttributeValue("Group")))
            {
                ShowMessage("Block not configured. Please configure to use.", "Configuration Error", "panel panel-danger");
                return;
            }

            gReport.GridRebind += gReport_GridRebind;

            if (!Page.IsPostBack)
            {
                cmpCampus.DataSource = CampusCache.All();
                cmpCampus.DataBind();
                cmpCampus.Items.Insert(0, new ListItem("All Campuses", ""));

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
            var rockContext = new RockContext();

                var groupMemberService = new GroupMemberService(rockContext);
                var groupService = new GroupService(rockContext);
                var groupTypeService = new GroupTypeService(rockContext);
                var attributeService = new AttributeService(rockContext);
                var attributeValueService = new AttributeValueService(rockContext);
                var personService = new PersonService(rockContext);
                var personAliasService = new PersonAliasService(rockContext);
                var entityTypeService = new EntityTypeService(rockContext);
                var registrationRegistrantService = new RegistrationRegistrantService(rockContext);
                var eiogmService = new EventItemOccurrenceGroupMapService(rockContext);
                var groupLocationService = new GroupLocationService(rockContext);
                var locationService = new LocationService(rockContext);
                var signatureDocumentServce = new SignatureDocumentService(rockContext);
                var phoneNumberService = new PhoneNumberService(rockContext);

                int[] signatureDocumentIds = { };
                if (!string.IsNullOrWhiteSpace(GetAttributeValue("SignatureDocumentTemplates"))) {
                    signatureDocumentIds = Array.ConvertAll(GetAttributeValue("SignatureDocumentTemplates").Split(','), int.Parse);
                }
                Guid bbGroup = GetAttributeValue("Group").AsGuid();
                var group = new GroupService(rockContext).Get(bbGroup);
                
                if (group.Name.Contains("Week 2"))
                {
                    cmpCampus.Visible = true;
                }

                Guid hsmGroupTypeGuid = GetAttributeValue("MSMGroupType").AsGuid();
                int? hsmGroupTypeId = groupTypeService.Queryable().Where(gt => gt.Guid == hsmGroupTypeGuid).Select(gt => gt.Id).FirstOrDefault();

                int entityTypeId = entityTypeService.Queryable().Where(et => et.Name == typeof(Rock.Model.Group).FullName).FirstOrDefault().Id;
                
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
                    .GroupJoin(registrationRegistrantService.Queryable(),
                        obj => obj.Id,
                        rr => rr.GroupMemberId,
                        (obj, rr) => new { GroupMember = obj, Person = obj.Person, RegistrationRegistrant = rr })
                    .GroupJoin(attributeValueService.Queryable(),
                        obj => new { PersonId = (int?)obj.Person.Id, AttributeId = 739 },
                        av => new { PersonId = av.EntityId, av.AttributeId },
                        (obj, av) => new { GroupMember = obj.GroupMember, Person = obj.Person, RegistrationRegistrant = obj.RegistrationRegistrant, School = av.Select(s => s.Value).FirstOrDefault() })
                    .GroupJoin(attributeValueService.Queryable(),
                        obj => obj.GroupMember.Id,
                        av => av.EntityId.Value,
                        (obj, avs) => new { GroupMember = obj.GroupMember, Person = obj.Person, RegistrationRegistrant = obj.RegistrationRegistrant, GroupMemberAttributeValues = avs.Where(av => attributeIds.Contains(av.AttributeId)), School=obj.School/*, Location = obj.Location */})
                    .GroupJoin(attributeValueService.Queryable(),
                        obj => obj.RegistrationRegistrant.FirstOrDefault().Id,
                        av => av.EntityId.Value,
                        (obj, avs) => new { GroupMember = obj.GroupMember, Person = obj.Person, RegistrationRegistrant = obj.RegistrationRegistrant, GroupMemberAttributeValues = obj.GroupMemberAttributeValues, RegistrationAttributeValues = avs.Where(av => attributeIds.Contains(av.AttributeId)), School = obj.School/*, Location = obj.Location */ });

                var qry2 = gmTmpqry
                        .GroupJoin(
                            groupMemberService.Queryable()
                            .Join(groupService.Queryable(),
                                gm => new { Id = gm.GroupId, GroupTypeId = 10 },
                                g => new { g.Id, g.GroupTypeId },
                                (gm, g) => new { GroupMember = gm, Group = g })
                            .Join(groupLocationService.Queryable(),
                                obj => new { GroupId = obj.Group.Id, GroupLocationTypeValueId = (int?)19 },
                                gl => new { gl.GroupId, gl.GroupLocationTypeValueId },
                                (g, gl) => new { GroupMember = g.GroupMember, GroupLocation = gl })
                            .Join(locationService.Queryable(),
                                obj => obj.GroupLocation.LocationId,
                                l => l.Id,
                                (obj, l) => new { GroupMember = obj.GroupMember, Location = l }),
                            gm => gm.PersonId,
                            glgm => glgm.GroupMember.PersonId,
                            (obj, l) => new { GroupMember = obj, Location = l.Select(loc => loc.Location).FirstOrDefault() }
                        )
                        .GroupJoin(signatureDocumentServce.Queryable()
                            .Join(personAliasService.Queryable(),
                                sd => sd.AppliesToPersonAliasId,
                                pa => pa.Id,
                                (sd, pa) => new { SignatureDocument = sd, Alias = pa }),
                            obj => obj.GroupMember.PersonId,
                            sd => sd.Alias.PersonId,
                            (obj, sds) => new { GroupMember = obj.GroupMember, Location = obj.Location, SignatureDocuments = sds })
                            .GroupJoin(phoneNumberService.Queryable(),
                    obj => obj.GroupMember.PersonId,
                    p => p.PersonId,
                    (obj, pn) => new { GroupMember = obj.GroupMember, Location = obj.Location, SignatureDocuments = obj.SignatureDocuments, PhoneNumbers = pn });


                if (!String.IsNullOrWhiteSpace(GetUserPreference(string.Format("{0}PersonName", keyPrefix)))) {
                    string personName = GetUserPreference(string.Format("{0}PersonName", keyPrefix)).ToLower();
                    qry = qry.ToList().Where(q => q.GroupMember.Person.FullName.ToLower().Contains(personName)).AsQueryable();
                }
                decimal? lowerVal = GetUserPreference(string.Format("{0}BalanceOwedLower", keyPrefix)).AsDecimalOrNull();
                decimal? upperVal = GetUserPreference(string.Format("{0}BalanceOwedUpper", keyPrefix)).AsDecimalOrNull();

                if (lowerVal != null && upperVal != null)
                {
                    qry = qry.ToList().Where(q => q.RegistrationRegistrant.Select(rr => rr.Registration.BalanceDue).FirstOrDefault() >= lowerVal && q.RegistrationRegistrant.Select( rr => rr.Registration.BalanceDue ).FirstOrDefault() <= upperVal).AsQueryable();
                }
                else if (lowerVal != null)
                {
                    qry = qry.ToList().Where(q => q.RegistrationRegistrant.Select( rr => rr.Registration.BalanceDue ).FirstOrDefault() >= lowerVal).AsQueryable();
                }
                else if (upperVal != null)
                {
                    qry = qry.ToList().Where(q => q.RegistrationRegistrant.Select( rr => rr.Registration.BalanceDue ).FirstOrDefault() <= upperVal).AsQueryable();
                }
                else if (!string.IsNullOrEmpty(cmpCampus.SelectedValue))
                {
                    if (group.Name.Contains("Week 2"))
                    {
                        qry = qry.ToList().Where(q => q.RegistrationAttributeValues.Where(ra => ra.AttributeKey == "Whichcampusdoyouwanttodepartforcampfromandroomwith" && ra.Value == cmpCampus.SelectedValue).Any()).AsQueryable();
                    }
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var tmp = qry.ToList();
                var tmp2 = qry2.ToList();

                lStats.Text = "Query Runtime: " + stopwatch.Elapsed;
                stopwatch.Reset();

                stopwatch.Start();

                var newQry = tmp.Select(g => new
                {
                    Id = g.GroupMember.Id,
                    RegisteredBy = new ModelValue<Person>(g.RegistrationRegistrant.Select( rr => rr.Registration.PersonAlias.Person ).FirstOrDefault()),
                    Registrant = new ModelValue<Person>(g.Person),
                    Age = g.Person.Age,
                    GraduationYear = g.Person.GraduationYear,
                    RegistrationId = g.RegistrationRegistrant.Select( rr => rr.RegistrationId ).FirstOrDefault(),
                    Group = new ModelValue<Rock.Model.Group>((Rock.Model.Group)g.GroupMember.Group),
                    DOB = g.Person.BirthDate.HasValue ? g.Person.BirthDate.Value.ToShortDateString() : "",
                    Address = new ModelValue<Rock.Model.Location>((Rock.Model.Location)tmp2.Where(gm => gm.GroupMember.Id == g.GroupMember.Id).Select(gm => gm.Location).FirstOrDefault()),
                    Email = g.Person.Email,
                    Gender = g.Person.Gender, // (B & B Registration)
                    GraduationYearProfile = g.Person.GraduationYear, // (Person Profile)
                    HomePhone = tmp2.Where(gm => gm.GroupMember.Id == g.GroupMember.Id).SelectMany(gm => gm.PhoneNumbers).Where(pn => pn.NumberTypeValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid()).Select(pn => pn.NumberFormatted).FirstOrDefault(),
                    CellPhone = tmp2.Where(gm => gm.GroupMember.Id == g.GroupMember.Id).SelectMany(gm => gm.PhoneNumbers).Where(pn => pn.NumberTypeValue.Guid == Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid()).Select(pn => pn.NumberFormatted).FirstOrDefault(),
                    GroupMemberData = new Func<GroupMemberAttributes>(() =>
                    {
                        GroupMemberAttributes gma = new GroupMemberAttributes(g.GroupMember, g.Person, g.GroupMemberAttributeValues);
                        return gma;
                    })(),
                    RegistrantData = new Func<RegistrantAttributes>(() =>
                    {
                        RegistrantAttributes row = new RegistrantAttributes(g.RegistrationRegistrant.FirstOrDefault(), g.RegistrationAttributeValues);
                        return row;
                    })(),
                    School = string.IsNullOrEmpty(g.School)?"":DefinedValueCache.Get(g.School.AsGuid()) != null?DefinedValueCache.Get(g.School.AsGuid()).Value:"",
                    LegalRelease = tmp2.Where(gm => gm.GroupMember.Id == g.GroupMember.Id).SelectMany(gm => gm.SignatureDocuments).OrderByDescending(sd => sd.SignatureDocument.CreatedDateTime).Where(sd => signatureDocumentIds.Contains(sd.SignatureDocument.SignatureDocumentTemplateId)).Select(sd => sd.SignatureDocument.SignatureDocumentTemplate.Name + " (" + sd.SignatureDocument.Status.ToString() + ")").FirstOrDefault(),
                    Departure = g.GroupMemberAttributeValues.Where(av => av.AttributeKey == "Departure").Select(av => av.Value).FirstOrDefault(),
                    Campus = group.Campus, // 
                    Role = group.ParentGroup.GroupType.Name.Contains("Serving")|| group.Name.ToLower().Contains("leader")? "Leader":"Student", // 
                    MSMGroup = String.Join(", ", groupMemberService.Queryable().Where(gm => gm.PersonId == g.GroupMember.PersonId && gm.Group.GroupTypeId == hsmGroupTypeId && gm.GroupMemberStatus == GroupMemberStatus.Active).Select(gm => gm.Group.Name).ToList()),
                    Person = g.Person,
                    AddressStreet = tmp2.Where(gm => gm.GroupMember.Id == g.GroupMember.Id).Select(gm => gm.Location!=null?gm.Location.Street1:"").FirstOrDefault(),
                    AddressCityStateZip = tmp2.Where(gm => gm.GroupMember.Id == g.GroupMember.Id).Select(gm => gm.Location != null ? gm.Location.City + ", " + gm.Location.State + " " + gm.Location.PostalCode : "").FirstOrDefault(),

                    RegistrantName = g.Person.FullName,

                }).OrderBy(w => w.Registrant.Model.LastName).ToList().AsQueryable();

                lStats.Text += "<br />Object Build Runtime: " + stopwatch.Elapsed;

                stopwatch.Stop();
                gReport.GetRecipientMergeFields += GReport_GetRecipientMergeFields;
                var mergeFields = new List<String>();
                mergeFields.Add("Id");
                mergeFields.Add("RegisteredBy");
                mergeFields.Add("Group");
                mergeFields.Add("Registrant");
                mergeFields.Add("Age");
                mergeFields.Add("GraduationYear");
                mergeFields.Add("DOB");
                mergeFields.Add("Address");
                mergeFields.Add("Email");
                mergeFields.Add("Gender");
                mergeFields.Add("GraduationYearProfile");
                mergeFields.Add("HomePhone");
                mergeFields.Add("CellPhone");
                mergeFields.Add("GroupMemberData");
                mergeFields.Add("RegistrantData");
                mergeFields.Add("LegalRelease");
                mergeFields.Add("Departure");
                mergeFields.Add("Campus");
                mergeFields.Add("Role");
                mergeFields.Add("MSMGroup");
                gReport.CommunicateMergeFields = mergeFields;

                /*
                if (!String.IsNullOrWhiteSpace(GetUserPreference(string.Format("{0}POA", keyPrefix))))
                {
                    string poa = GetUserPreference(string.Format("{0}POA", keyPrefix));
                    if (poa == "[Blank]") 
                    {
                        poa = "";
                    }
                    newQry = newQry.Where(q => q.GroupMemberData.POA == poa);
                }
                */

                SortProperty sortProperty = gReport.SortProperty;
                if (sortProperty != null)
                {
                    gReport.SetLinqDataSource(newQry.Sort(sortProperty));
                }
                else
                {
                    gReport.SetLinqDataSource(newQry.OrderBy(p => p.Registrant.Model.LastName));
                }
                gReport.DataBind();
            
        }

        private void Actions_CommunicateClick(object sender, EventArgs e)
        {
            var rockPage = Page as RockPage;
            BindGrid();

            var redirectUrl = rockPage.Response.Output.ToString();
            if (redirectUrl != null)
            {
                Regex rgx = new Regex(".*/(\\d*)");
                string result = rgx.Replace(redirectUrl, "$1");

                var recipients = GetDuplicatePersonData();
                if (recipients.Any())
                {
                    // Create communication 
                    var communicationRockContext = new RockContext();
                    var communicationService = new CommunicationService(communicationRockContext);
                    var communicationRecipientService = new CommunicationRecipientService(communicationRockContext);
                    var personAliasService = new PersonAliasService(communicationRockContext);


                    var communication = communicationService.Queryable().Where(c => c.SenderPersonAliasId == rockPage.CurrentPersonAliasId).OrderByDescending(c => c.Id).FirstOrDefault();
                    communication.IsBulkCommunication = false;

                    foreach(var recipient in recipients)
                    {
                        PersonAlias a = personAliasService.Queryable().Where(p => p.PersonId == p.AliasPersonId && p.PersonId == recipient.Key).FirstOrDefault();
                        var communicationRecipient = new CommunicationRecipient
                        {
                            CommunicationId = communication.Id,
                            PersonAliasId = a.Id,
                            AdditionalMergeValues = recipient.Value
                        };
                        communicationRecipientService.Add(communicationRecipient);
                        communicationRockContext.SaveChanges();
                    }
                }
            }
            
        }

        /// <summary>
        /// This method gets any people that are duplicates.  For this report, we want to include them too.
        /// </summary>
        /// <returns></returns>
        private List<KeyValuePair<int, Dictionary<string, object>>> GetDuplicatePersonData()
        {
            var personData = new List<KeyValuePair<int, Dictionary<string, object>>>();

            if (gReport.PersonIdField != null)
            {

                // The ToList() is potentially needed for Linq cases.
                var keysSelected = gReport.SelectedKeys.ToList();
                string dataKeyColumn = gReport.DataKeyNames.FirstOrDefault() ?? "Id";

                // get access to the List<> and its properties
                IList data = gReport.DataSourceAsList;
                if (data != null)
                {
                    Type oType = data.GetType().GetProperty("Item").PropertyType;

                    PropertyInfo idProp = !string.IsNullOrEmpty(dataKeyColumn) ? oType.GetProperty(dataKeyColumn) : null;

                    var personIdProp = new List<PropertyInfo>();
                    var propPath = gReport.PersonIdField.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                    while (propPath.Any())
                    {
                        var property = oType.GetProperty(propPath.First());
                        if (property != null)
                        {
                            personIdProp.Add(property);
                            oType = property.PropertyType;
                        }
                        propPath = propPath.Skip(1).ToList();
                    }

                    foreach (var item in data)
                    {
                        if (!personIdProp.Any())
                        {
                            while (propPath.Any())
                            {
                                var property = item.GetType().GetProperty(propPath.First());
                                if (property != null)
                                {
                                    personIdProp.Add(property);
                                }
                                propPath = propPath.Skip(1).ToList();
                            }
                        }

                        if (idProp == null)
                        {
                            idProp = item.GetType().GetProperty(dataKeyColumn);
                        }

                        if (personIdProp.Any() && idProp != null)
                        {
                            var personIdObjTree = new List<object>();
                            personIdObjTree.Add(item);
                            bool propFound = true;
                            foreach (var prop in personIdProp)
                            {
                                object obj = prop.GetValue(personIdObjTree.Last(), null);
                                if (obj != null)
                                {
                                    personIdObjTree.Add(obj);
                                }
                                else
                                {
                                    propFound = false;
                                    break;
                                }
                            }

                            if (propFound && personIdObjTree.Last() is int)
                            {
                                int personId = (int)personIdObjTree.Last();
                                if (personData.Where(pd => pd.Key == personId).Any())
                                {
                                    int id = (int)idProp.GetValue(item, null);

                                    // Add the personId if none are selected or if it's one of the selected items.
                                    if (!keysSelected.Any() || keysSelected.Contains(id))
                                    {
                                        var mergeValues = new Dictionary<string, object>();
                                        foreach (string mergeField in gReport.CommunicateMergeFields)
                                        {
                                            object obj = item.GetPropertyValue(mergeField);
                                            if (obj != null)
                                            {
                                                mergeValues.Add(mergeField.Replace('.', '_'), obj);
                                            }
                                        }
                                        personData.Add(new KeyValuePair<int, Dictionary<string, object>>(personId, mergeValues));
                                    }
                                }
                                else
                                {
                                    personData.Add(new KeyValuePair<int, Dictionary<string, object>>(personId, null));
                                }
                                
                            }
                        }
                    }
                }
            }
            return personData.Where(pd => pd.Value != null).ToList();
        }

        private void GReport_GetRecipientMergeFields(object sender, GetRecipientMergeFieldsEventArgs e)
        {
            // Nothing here
        }

        /// <summary>
        /// Loads the grid filter values.
        /// </summary>
        private void LoadGridFilters()
        {
            txtPersonName.Text = GetUserPreference(string.Format("{0}PersonName", keyPrefix));
            nreBalanceOwed.LowerValue = GetUserPreference(string.Format("{0}BalanceOwedLower", keyPrefix)).AsDecimalOrNull();
            nreBalanceOwed.UpperValue = GetUserPreference(string.Format("{0}BalanceOwedUpper", keyPrefix)).AsDecimalOrNull();
            ddlPOA.DataSource = new List<string> { "", "[Blank]", "Yes", "N/A" };
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
                    WorkflowActivity workflowActivity = WorkflowActivity.Activate( WorkflowActivityTypeCache.Get( workflowActivityType.Id ), workflow, rockContext);

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
            mdEditRow.Footer.Visible = false;
        }

        class GroupMemberAttributes : IComparable
        {
            public GroupMemberAttributes(GroupMember groupMember, Person person, IEnumerable<AttributeValue> attributeValues)
            {
                GroupMember = groupMember;
                AttributeValues = attributeValues;
                Person = person;
            }

            [Newtonsoft.Json.JsonIgnore]
            public GroupMember GroupMember { get; set; }

            [Newtonsoft.Json.JsonIgnore]
            public Person Person { get; set; }
            private IEnumerable<AttributeValue> AttributeValues { get; set; }

            public int Id { get { return GroupMember.Id; } }
            public String GeneralNotes { get { return GetAttributeValue("GeneralNotes"); } }
            public String RoomingNotes { get { return GetAttributeValue("RoomingNotes"); } }
            public String MedicalNotes { get { return GetAttributeValue("MedicalNotes"); } }
            public String TravelNotes { get { return GetAttributeValue("TravelNotes"); } }
            public String SpecialNotes { get { return GetAttributeValue("SpecialNotes"); } }
            public String TravelExceptions { get { return GetAttributeValue("TravelExceptions"); } }
            public String Group { get { return GetAttributeValue("Group1"); } }
            public String Dorm { get { return GetAttributeValue("Dorm"); } }
            public String Room { get { return GetAttributeValue("Room"); } }
            public String Bus { get { return GetAttributeValue("BusAssignment"); } }
            public String Departure { get { return GetAttributeValue("Departure"); } }
            public String CoLeaders { get { return GetAttributeValue("CoLeaders"); } }
            public String LeadingLocation { get { return GetAttributeValue("LeadingLocation"); } }


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

            [Newtonsoft.Json.JsonIgnore]
            public RegistrationRegistrant Registrant { get; set; }
            private IEnumerable<AttributeValue> AttributeValues { get; set; }

            public int Id { get { return Registrant.Id; } }

            public String ConfirmationEmail
            {
                get { return Registrant != null ? Registrant.Registration.ConfirmationEmail : ""; }
            }
            public Decimal BalanceOwed
            {
                get { return Registrant != null ? Registrant.Registration.BalanceDue : 0; }
            }
            public String BirthDate
            {
                get { return GetAttributeValue("Birthdate").AsDateTime().HasValue ? GetAttributeValue("Birthdate").AsDateTime().Value.ToShortDateString() : null; }
            }
            public String Grade
            {
                get { return GetAttributeValue("grade"); }
            }
            public String ParentName
            {
                get { return GetAttributeValue("ParentGuardianName"); }
            }
            public String ParentCell
            {
                get { return GetAttributeValue("parentcell"); }
            }
            public String EmName
            {
                get { return GetAttributeValue("EmergencyContactName"); }
            }
            public String EmCell
            {
                get { return GetAttributeValue("EmergCellPhone"); }
            }
            public String School
            {
                get { return GetAttributeValue("Schoolifnotlistedabove"); }
            }
            public String OthersInGroup
            {
                get { return GetAttributeValue("inyourgroup"); }
            }
            public String Church
            {
                get { return GetAttributeValue("HomeChurchyouregularlyattend"); }
            }
			public String Blankenbakerhour
			{
				get { return GetAttributeValue("Blankenbakerhour"); }
			}
            public string Campus
            {
                get {
                    if (!String.IsNullOrEmpty(GetAttributeValue("Whichcampusdoyouwanttodepartforcampfromandroomwith")))
                    {
                        return GetAttributeValue("Whichcampusdoyouwanttodepartforcampfromandroomwith");
                    }
                    else if(!String.IsNullOrEmpty(GetAttributeValue("Campus")))
                    {
                        if (GetAttributeValue("Campus").AsGuidOrNull() == null) {
                            return GetAttributeValue("Campus");
                        } else
                        {
                            return CampusCache.Get(GetAttributeValue("Campus").AsGuid()).Name;
                        }
                    }
                    return "";
                }
            }
            public bool RoomMSMGroup
            {
                get { return GetAttributeValue("roomMSMGroup").AsBoolean(); }
            }
            public String Allergies
            {
                get { return GetAttributeValue("foodallergies"); }
            }
            public String AllergySeverity
            {
                get { return GetAttributeValue("howsevere"); }
            }
            public String HowManaged
            {
                get { return GetAttributeValue("howmanage"); }
            }
            public String OtherAllergies
            {
                get { return GetAttributeValue("Listother"); }
            }

            public String MedicalInfo
            {
                get { return GetAttributeValue("medallergies"); }
            }

            public String OTCMeds
            {
                get { return GetAttributeValue("Dispensepain"); }
            }

            public String Insurance
            {
                get { return GetAttributeValue("MedInsCo")+": "+GetAttributeValue("PolicyNumber"); }
            }

			public String Physiciansname
            {
                get { return GetAttributeValue("Physiciansname"); }
            }
			
            public String Photo
            {
                get { return GetAttributeValue("PhotoOptional"); }
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

        [Serializable]
        class ModelValue<T> where T : IModel
        {
            public ModelValue(T model)
            {
                if (model != null)
                {
                    Id = model.Id;
                    Value = model.ToString();
                    Model = model;
                }
            }

            public int Id { get; set;}

            public string Value { get; set;}

            public override string ToString()
            {
                return Value??"";
            }
            
            [Newtonsoft.Json.JsonIgnore]
            public T Model { get; set; }
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