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
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using System.Data;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// Block for developers to use to partition attendance by group member attribute.
    /// </summary>
    [DisplayName("Attendance Group Member Attribute")]
    [Category("SECC > Groups")]
    [Description("Block for developers to use to partition attendance by group member attribute.")]
    public partial class AttendanceGroupMemberAttribute : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger(upnlcontent);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int? groupId = 0;

            if (!string.IsNullOrWhiteSpace(PageParameter("GroupId")))
            {
                groupId = PageParameter("GroupId").AsIntegerOrNull();
            }

            if (!Page.IsPostBack)
            {
                if (groupId.HasValue)
                {
                    ShowDetail(groupId.Value);
                }
                else
                {
                    pnldetails.Visible = false;
                }
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated(object sender, EventArgs e)
        {
            string groupId = PageParameter("GroupId");

            if (!string.IsNullOrWhiteSpace(groupId))
            {
                ShowDetail(groupId.AsInteger());
            }
            else
            {
                pnldetails.Visible = false;
            }
        }

        protected void BtnSubmit_Click(object sender, EventArgs e)
        {
            var columns = glist.Columns.Count;

            for (int index = columns - 1; index > 0; index--)
            {
                glist.Columns.RemoveAt(index);
            }
            BindData();
        }

        #endregion

        #region Methods

        private void ShowDetail(int groupId)
        {
            var rockContext = new RockContext();
            var groupservice = new GroupService(rockContext);
            var attributeservice = new AttributeService(rockContext);
            var group = groupservice.Get(groupId);

            if (group == null)
            {
                return;
            }
            var grouptypeId = group.GroupTypeId;
            GroupTypeCache grouptype = GroupTypeCache.Get(grouptypeId);
            var attributes = attributeservice.GetGroupMemberAttributesCombined(groupId, grouptypeId).ToList();
            string groupiconhtml = string.Empty;

            if (grouptype != null)
            {
                groupiconhtml = !string.IsNullOrWhiteSpace(grouptype.IconCssClass) ?
                string.Format("<i class='{0}' ></i>", grouptype.IconCssClass) : string.Empty;
            }
            lgroupiconhtml.Text = groupiconhtml;
            lreadonlytitle.Text = group.Name.FormatAsHtmlTitle();

            foreach (var attribute in attributes)
            {
                cbl.Items.Add(attribute.Name);
            }
        }
        
        private void BindData()
        {
            var rockContext = new RockContext();
            var partitions = cbl.SelectedValues;

            if (partitions.Count == 0)
                return;
            var groupservice = new GroupService(rockContext);
            var groupmemberservice = new GroupMemberService(rockContext);
            var attributeservice = new AttributeService(rockContext);
            var attributevalueservice = new AttributeValueService(rockContext);
            int groupId = PageParameter("GroupId").AsInteger();
            var group = groupservice.Get(groupId);
            var groupmembers = groupmemberservice.GetByGroupId(groupId);
            int attributeId = 0;
            int count = 0;
            List<string> list = new List<string>();           
            List<AttributeValue> attributevalues = null;

            foreach (var partition in partitions)
            {
                attributeId = attributeservice.Queryable().Where(a => a.Name == partition && a.EntityTypeQualifierValue == group.GroupTypeId.ToString()).FirstOrDefault().Id;
                attributevalues = attributevalueservice.GetByAttributeId(attributeId).DistinctBy(a => a.Value).ToList();

                if (count > 0)
                {
                    List<string> list2 = new List<string>();

                    foreach (AttributeValue attributevalue in attributevalues)
                    {
                        for (int position = 0; position < list.Count; position++)
                        {
                            list2.Add(list[position] + "-" + attributevalue.Value);
                        }
                    }
                    list = list2;
                }
                else
                {
                    foreach (AttributeValue attributevalue in attributevalues)
                    {
                        list.Add(attributevalue.Value);
                    }
                }
                count++;
            }
            string daterangepreference = dp.DelimitedValues;

            if (string.IsNullOrWhiteSpace(daterangepreference))
                return;
            var daterange = DateRangePicker.CalculateDateRangeFromDelimitedValues(daterangepreference);

            // if there is no start date, default to three months ago to minimize the chance of loading too much data
            dp.LowerValue = daterange.Start ?? RockDateTime.Today.AddMonths(-3);
            dp.UpperValue = daterange.End;
            DateTime? fromdatetime = dp.LowerValue;
            DateTime? todatetime = dp.UpperValue;
            List<int> locationIds = new List<int>();
            List<int> scheduleIds = new List<int>();
            List<List<int>> attendeelists = new List<List<int>>();
            var occurrences = new AttendanceOccurrenceService(rockContext).GetGroupOccurrences(group, fromdatetime, todatetime, locationIds, scheduleIds);
            int number = 0;

            foreach (var occurrence in occurrences)
            {
                number++;
                RockBoundField rockboundField = new RockBoundField
                {
                    DataField = "Occurrence" + number,
                    HeaderText = occurrence.OccurrenceDate.ToShortDateString()
                };
                glist.Columns.Add(rockboundField);
                attendeelists.Add(occurrence.Attendees.AsQueryable().Where(a=>a.DidAttend == true).Select(a => a.PersonAlias.PersonId).ToList());
            }

            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("MemberAttribute", typeof(string)));

            for (int column = 1; column < number + 1; column++)
            {
                dt.Columns.Add(new DataColumn("Occurrence" + column, typeof(int)));
            }

            foreach (var memberattribute in list)
            {
                int arrayindex = 0;
                var length = occurrences.Count + 1;
                object[] array = new object[length];
                array[0] = memberattribute;
                var membervalues = memberattribute.Split('-');

                foreach (var attendeelist in attendeelists)
                {
                    int total = 0;
                    arrayindex++;

                    foreach (var attendee in attendeelist)
                    {
                        var groupmember = groupmembers.Where(g => g.Person.Id == attendee).FirstOrDefault();
                        groupmember.LoadAttributes();                       
                        var flag = true;
                        int listindex = 0;

                        foreach (var value in membervalues)
                        {                       
                            var membervalue = groupmember.GetAttributeValue(partitions[listindex]);

                            if (value != membervalue)
                                flag = false;
                            listindex++;
                        }

                        if (flag)
                            total++;
                    }
                    array[arrayindex] = total;
                }
                dt.Rows.Add(array);
            }
            glist.DataSource = dt;
            glist.DataBind();
        }

        #endregion
    }
}