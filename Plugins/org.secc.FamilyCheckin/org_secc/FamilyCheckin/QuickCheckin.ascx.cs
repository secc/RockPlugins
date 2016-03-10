using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using System.Web.UI.WebControls;
using System.Web.UI;
using Rock.Model;
using System.Reflection;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Net.Sockets;
using System.Net;
using Rock.Data;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName("QuickCheckin")]
    [Category("Check-in")]
    [Description("QuickCheckin block for helping parents check in their family quickly.")]

    public partial class QuickCheckin : CheckInBlock
    {
        private List<GroupTypeCache> parentGroupTypesList;
        private GroupTypeCache currentParentGroupType;
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            RockPage.AddScriptLink("~/Scripts/CheckinClient/cordova-2.4.0.js", false);
            RockPage.AddScriptLink("~/Scripts/CheckinClient/ZebraPrint.js");

            if (!KioskCurrentlyActive)
            {
                NavigateToHomePage();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
                List<string> errors = new List<string>();
                string workflowActivity = GetAttributeValue("WorkflowActivity");
                try
                {
                    //Sometimes this blows up.
                    bool test = ProcessActivity(workflowActivity, out errors);
                }
                catch
                {
                    NavigateToPreviousPage();
                    Response.End();
                    return;
                }

                //Find the parent group types
                parentGroupTypesList = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected).FirstOrDefault()
                    .People.SelectMany(p => p.GroupTypes).SelectMany(gt => gt.GroupType.ParentGroupTypes)
                    .Where(pgt => pgt.ChildGroupTypes.Count > 1).DistinctBy(pgt => pgt.Guid).ToList();
                Session["parentGroupTypesList"] = parentGroupTypesList;


                currentParentGroupType = getCurrentParentGroupType();

                
                Session["currentParentGroupType"] = currentParentGroupType;

                Session["modalActive"] = false;
                Session["modalPerson"] = null;
                Session["modalSchedule"] = null;
            }

            parentGroupTypesList = (List<GroupTypeCache>)Session["parentGroupTypesList"];
            currentParentGroupType = (GroupTypeCache)Session["currentParentGroupType"];

            if (currentParentGroupType == null)
            {
                NavigateToPreviousPage();
            }

            btnParentGroupTypeHeader.Text = currentParentGroupType.Name + " <i class='fa fa-chevron-down'></i>";
            btnParentGroupTypeHeader.DataLoadingText = currentParentGroupType.Name + " <i class='fa fa-refresh fa-spin'>";

            DisplayPeople();

            if ((bool)Session["modalActive"])
            {
                if(Session["modalPerson"]!=null && Session["modalSchedule"] != null)
                {
                ShowRoomChangeModal((Person)Session["modalPerson"], (CheckInSchedule)Session["modalSchedule"]);
                }
                if (Session["modalGroupType"] != null)
                {
                    ShowGroupTypeChangeModal();
                }
            }
        }

        private GroupTypeCache getCurrentParentGroupType()
        {
            var lastCheckin = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                .SelectMany(f => f.People.Where(p => p.Selected)).Select(p => p.LastCheckIn).Where(dt => dt != null ).Max();

            //Find the current parent group type
            foreach (var parentGroupType in parentGroupTypesList)
            {
                if (CurrentCheckInState.CheckIn.Families.Where(f => f.Selected).FirstOrDefault()
                       .People.Where(p => p.LastCheckIn == lastCheckin).SelectMany(p => p.GroupTypes)
                       .Where(gt => gt.Selected == true && gt.GroupType.ParentGroupTypes.Contains(parentGroupType)).Count() > 0)
                {
                    return parentGroupType;
                }
            }
            return null;
        }

        private void ChangeParentGroupType(GroupTypeCache parentGroupType)
        {
            //Save pgt
            currentParentGroupType = parentGroupType;
            Session["currentParentGroupType"] = currentParentGroupType;

            //Clean up our modal mess.
            Session["modalActive"] = false;
            Session["modalGroupType"] = null;
            phModal.Controls.Clear();
            mdChoose.Hide();

            //change name
            btnParentGroupTypeHeader.Text = currentParentGroupType.Name+" <i class='fa fa-chevron-down'></i>";
            btnParentGroupTypeHeader.DataLoadingText = currentParentGroupType.Name + " <i class='fa fa-refresh fa-spin'>";

            //Show updated people info
            phPeople.Controls.Clear();
            DisplayPeople();

            SaveState();
        }

        private void DisplayPeople()
        {
            var people = CurrentCheckInState.CheckIn.Families.SelectMany(f => f.People);

            HtmlGenericControl hgcRow = new HtmlGenericControl("div");
            hgcRow.AddCssClass("row");
            phPeople.Controls.Add(hgcRow);

            foreach (var person in people)
            {
                if (GetCheckinSchedules(person.Person).Count() > 0)
                {
                HtmlGenericControl hgcPadding = new HtmlGenericControl("div");
                hgcPadding.AddCssClass("col-xs-12 col-md-6");
                hgcRow.Controls.Add(hgcPadding);

                HtmlGenericControl hgcCell = new HtmlGenericControl("div");
                hgcCell.AddCssClass("well col-xs-12");
                hgcPadding.Controls.Add(hgcCell);
                DisplayPersonButton(person, hgcCell);
                DisplayPersonCheckinAreas(person.Person, hgcCell);
                }
            }
        }

        private void DisplayPersonCheckinAreas(Person person, HtmlGenericControl hgcRow)
        {
            List<CheckInSchedule> personSchedules = GetCheckinSchedules(person);
            
            foreach (var schedule in personSchedules)
            {
                HtmlGenericControl hgcAreaRow = new HtmlGenericControl("div");
                hgcRow.AddCssClass("row col-xs-12");
                hgcRow.Controls.Add(hgcAreaRow);
                DisplayPersonSchedule(person, schedule, hgcAreaRow);
            }
        }

        private List<CheckInSchedule> GetCheckinSchedules(Person person)
        {
            return CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                .SelectMany(f => f.People.Where(p => p.Person.Guid == person.Guid))
                .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == true))
                .SelectMany(gt => gt.Groups).SelectMany(g => g.Locations).SelectMany(l => l.Schedules)
                .DistinctBy(s => s.Schedule.Guid).ToList();
        }

        private void DisplayPersonSchedule(Person person, CheckInSchedule schedule, HtmlGenericControl hgcAreaRow)
        {
            BootstrapButton btnSchedule = new BootstrapButton();
            btnSchedule.Text = schedule.Schedule.Name + "<br>(Select Room To Checkin)";
            hgcAreaRow.Controls.Add(btnSchedule);
            btnSchedule.CssClass = "btn btn-default col-xs-8";
            btnSchedule.ID = person.Guid.ToString() + currentParentGroupType.Guid.ToString() + schedule.Schedule.Guid.ToString();
            btnSchedule.Click += (s, e) => { ShowRoomChangeModal(person, schedule); };
            btnSchedule.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i><br>Loading Rooms...";

            var groupType = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                .SelectMany(f => f.People.Where(p => p.Person.Guid == person.Guid))
                .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == true))
                .FirstOrDefault(gt => gt.Selected && gt.Groups.SelectMany(g => g.Locations).SelectMany(l => l.Schedules.Where(s => s.Selected)).Select(s => s.Schedule.Guid).Contains(schedule.Schedule.Guid) == true);

            if (groupType != null)
            {
                var group = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                .SelectMany(f => f.People.Where(p => p.Person.Guid == person.Guid))
                .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == true && gt == groupType))
                .SelectMany(gt => gt.Groups).FirstOrDefault(g => g.Selected);

                if (group != null)
                {
                    var room = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                        .SelectMany(f => f.People.Where(p => p.Person.Guid == person.Guid))
                        .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == true && gt == groupType))
                        .SelectMany(gt => gt.Groups.Where(g => g.Selected && g.Group.Guid == group.Group.Guid))
                        .SelectMany(g => g.Locations).Where(l => l.Selected && l.Schedules.FirstOrDefault(s => s.Schedule.Guid == schedule.Schedule.Guid).Selected)
                        .FirstOrDefault();

                    if (room != null)
                    {
                        btnSchedule.CssClass = "btn btn-primary col-xs-8";
                        btnSchedule.Text = "<b>" + schedule.Schedule.Name + "</b><br>" + group + " > " + room;
                    }
                }
            }
        }

        private void ShowRoomChangeModal(Person person, CheckInSchedule schedule)
        {
            List<CheckInGroupType> groupTypes = GetGroupTypes(person, schedule);

            foreach (var groupType in groupTypes)
            {
                List<CheckInGroup> groups = GetGroups(person, schedule, groupType);

                foreach (var group in groups)
                {
                    List<CheckInLocation> rooms = GetLocations(person, schedule, groupType, group);
                    foreach (var room in rooms)
                    {
                        HtmlGenericContainer hgcPadding = new HtmlGenericContainer("div");
                        hgcPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
                        hgcPadding.Style.Add("padding", "5px");
                        phModal.Controls.Add(hgcPadding);


                        //Change room button
                        BootstrapButton btnRoom = new BootstrapButton();
                        btnRoom.ID = "c" + person.Guid.ToString() + schedule.Schedule.Guid.ToString() + room.Location.Guid.ToString();
                        btnRoom.Text = groupType.GroupType.Name + ": " + group.Group.Name + "<br>" +
                            room.Location.Name + " - Count: " + KioskLocationAttendance.Read(room.Location.Id).CurrentCount;
                        btnRoom.CssClass = "btn btn-default btn-block btn-lg";
                        btnRoom.Click += (s, e) =>
                        {
                            ChangeRoomSelection(person, schedule, groupType, group, room);
                            Session["modalActive"] = false;
                            mdChoose.Hide();
                            phPeople.Controls.Clear();
                            DisplayPeople();
                        };
                        btnRoom.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i><br>Changing room to: " + room.Location.Name;
                        hgcPadding.Controls.Add(btnRoom);
                    }
                }
            }
            HtmlGenericContainer hgcCancelPadding = new HtmlGenericContainer("div");
            hgcCancelPadding.CssClass = "col-md-8 col-md-offset-2 col-xs-12";
            hgcCancelPadding.Style.Add("padding", "5px");
            phModal.Controls.Add(hgcCancelPadding);

            BootstrapButton btnCancel = new BootstrapButton();
            btnCancel.ID = "c" + person.Guid.ToString() + schedule.Schedule.Guid.ToString();
            btnCancel.Text = "(Do not check in at "+ schedule.Schedule.Name +")";
            btnCancel.CssClass = "btn btn-danger btn-lg col-md-8 col-xs-12 btn-block";
            btnCancel.Click += (s, e) => {
                ClearRoomSelection(person, schedule);
                Session["modalActive"] = false;
                mdChoose.Hide();
                phPeople.Controls.Clear();
                DisplayPeople();
            };
            btnCancel.DataLoadingText = "<i class='fa fa-refresh fa-spin'></i> Canceling...";
            hgcCancelPadding.Controls.Add(btnCancel);

            mdChoose.Title = "Choose Class";
            mdChoose.CancelLinkVisible = false;
            mdChoose.Show();
            Session["modalActive"] = true;
            Session["modalPerson"] = person;
            Session["modalSchedule"] = schedule;
        }

        protected void CancelModal(object sender, EventArgs e)
        {
            Session["modalActive"] = false;
            mdChoose.Hide();
        }

        private void ChangeRoomSelection(Person person, CheckInSchedule schedule,
            CheckInGroupType groupType, CheckInGroup group, CheckInLocation room)
        {
            ClearRoomSelection(person, schedule);
            room.Selected = true;
            group.Selected = true;
            groupType.Selected = true;
            room.Schedules.Where(s => s.Schedule.Guid == schedule.Schedule.Guid).FirstOrDefault().Selected = true;
            SaveState();
        }

        private void ClearRoomSelection(Person person, CheckInSchedule schedule)
        {
            List<CheckInGroupType> groupTypes = GetGroupTypes(person, schedule);
            
            foreach (var groupType in groupTypes)
            {
                List<CheckInGroup> groups = GetGroups(person, schedule, groupType);

                foreach (var group in groups)
                {
                    List<CheckInLocation> rooms = GetLocations(person, schedule, groupType, group);

                    foreach (var room in rooms)
                    {
                        //Change scheduals in room to not selected
                        foreach (var roomSchedule in room.Schedules)
                        {
                            if (roomSchedule.Schedule.Guid == schedule.Schedule.Guid)
                            {
                                roomSchedule.Selected = false;
                            }
                        }
                        //Set location as not selected if no schedules selected
                        if (room.Schedules.Where(s => s.Selected == true).Count() == 0)
                        {
                            room.Selected = false;
                        }
                    }
                    //Set group as not selected if no locations selected
                    if (group.Locations.Where(l => l.Selected == true).Count() == 0)
                    {
                        group.Selected = false;
                    }
                }
                //Set group type as not selected if no groups selected
                if (groupType.Groups.Where(g => g.Selected == true).Count() == 0)
                {
                    groupType.Selected = false;
                }
            }
            SaveState();
        }

        private List<CheckInLocation> GetLocations(Person person, CheckInSchedule schedule, CheckInGroupType groupType, CheckInGroup group)
        {
            return CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                        .SelectMany(f => f.People.Where(p => p.Person.Guid == person.Guid))
                        .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == true))
                        .Where(gt => gt.GroupType.Guid == groupType.GroupType.Guid)
                        .SelectMany(gt => gt.Groups)
                        .Where(g => g.Group.Guid == group.Group.Guid)
                        .SelectMany(g => g.Locations.Where(
                            l => l.Schedules.Where(
                                s => s.Schedule.Guid == schedule.Schedule.Guid).Count() != 0)).ToList();
        }

        private List<CheckInGroup> GetGroups(Person person, CheckInSchedule schedule, CheckInGroupType groupType)
        {
            return CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                .SelectMany(f => f.People.Where(p => p.Person.Guid == person.Guid))
                .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == true))
                .Where(gt => gt.GroupType.Guid == groupType.GroupType.Guid)
                .SelectMany(gt => gt.Groups)
                .Where(g => g.Locations.Where(
                    l => l.Schedules.Where(
                        s => s.Schedule.Guid == schedule.Schedule.Guid).Count() != 0).Count() != 0).ToList();
        }

        private List<CheckInGroupType> GetGroupTypes(Person person, CheckInSchedule schedule)
        {
            return CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                .SelectMany(f => f.People.Where(p => p.Person.Guid == person.Guid))
                .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == true))
                .Where(gt => gt.Groups.Where(g => g.Locations.Where(
                    l => l.Schedules.Where(
                        s => s.Schedule.Guid == schedule.Schedule.Guid).Count() != 0).Count() != 0).Count() != 0).ToList();
        }

        private void DisplayPersonButton(CheckInPerson person, HtmlGenericControl hgcRow)
        {
            //Padding div to make it look nice.
            HtmlGenericControl hgcPadding = new HtmlGenericControl("div");
            hgcPadding.AddCssClass("col-xs-4");
            hgcRow.Controls.Add(hgcPadding);

            //Checkin Button
            var btnPerson = new BootstrapButton();
            btnPerson.ID = person.Person.Guid.ToString();
            btnPerson.Click += (s, e) => { TogglePerson(person); };
            hgcPadding.Controls.Add(btnPerson);



            if (person.Selected)
            {
                btnPerson.DataLoadingText = "<i class='fa  fa-check-square-o fa-5x'></i><br /> Please Wait...";
                btnPerson.Text = "<i class='fa  fa-check-square-o fa-5x'></i><br/>" + person.Person.NickName;
                btnPerson.CssClass = "btn btn-success btn-lg col-xs-12";
            }
            else
            {
                btnPerson.DataLoadingText = "<i class='fa fa-square-o fa-5x'></i><br /> Please Wait...";
                btnPerson.Text = "<i class='fa fa-square-o fa-5x'></i><br/>" + person.Person.NickName;
                btnPerson.CssClass = "btn btn-default btn-lg col-xs-12";
            }
        }

        private void TogglePerson(CheckInPerson person)
        {
            if (person.Selected)
                person.Selected = false;
            else
                person.Selected = true;

            SaveState();
            phPeople.Controls.Clear();
            DisplayPeople();
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            NavigateToNextPage();
        }

        protected void btnCheckin_Click(object sender, EventArgs e)
        {
            //Unselect all groups not in parent group
            var groupTypes = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected)
                .SelectMany(f => f.People)
                .SelectMany(p => p.GroupTypes.Where(gt => gt.GroupType.ParentGroupTypes.Select(pgt => pgt.Guid).Contains(currentParentGroupType.Guid) == false));
            foreach (var groupType in groupTypes)
            {
                groupType.Selected = false;
            }
            //Check-in and print tags.
            List<string> errors = new List<string>();
            bool test = ProcessActivity("Save Attendance", out errors);
            ProcessLabels();
            pnlMain.Visible = false;
        }

        private void ProcessLabels()
        {
            var printQueue = new Dictionary<string, StringBuilder>();
            foreach (var selectedFamily in CurrentCheckInState.CheckIn.Families.Where(p => p.Selected))
            {
                List<CheckInLabel> labels = new List<CheckInLabel>();
                List<CheckInPerson> selectedPeople = selectedFamily.People.Where(p => p.Selected).ToList();
                List<FamilyLabel> familyLabels = new List<FamilyLabel>();

                foreach (CheckInPerson selectedPerson in selectedPeople)
                {
                    foreach (var groupType in selectedPerson.GroupTypes.Where(gt => gt.Selected))
                    {
                        using (var rockContext = new RockContext())
                        {
                            foreach (var label in groupType.Labels)
                            {
                                var file = new BinaryFileService(rockContext).Get(label.FileGuid);
                                file.LoadAttributes(rockContext);
                                string isFamilyLabel = file.GetAttributeValue("IsFamilyLabel");
                                if (isFamilyLabel != "True")
                                {
                                    labels.Add(label);
                                }
                                else
                                {
                                    List<string> mergeCodes = file.GetAttributeValue("MergeCodes").TrimEnd('|').Split('|').ToList();
                                    FamilyLabel familyLabel = familyLabels.FirstOrDefault(fl => fl.FileGuid == label.FileGuid &&
                                                                                    fl.MergeFields.Count < mergeCodes.Count);
                                    if (familyLabel == null)
                                    {
                                        familyLabel = new FamilyLabel();
                                        familyLabel.FileGuid = label.FileGuid;
                                        familyLabel.LabelObj = label;
                                        foreach (var mergeCode in mergeCodes)
                                        {
                                            familyLabel.MergeKeys.Add(mergeCode.Split('^')[0]);
                                        }
                                        familyLabels.Add(familyLabel);
                                    }
                                    familyLabel.MergeFields.Add((selectedPerson.Person.Age.ToString() ?? "#") + "yr-" + selectedPerson.SecurityCode);
                                }
                            }
                        }
                    }
                }

                //Format all FamilyLabels and add to list of labels to print.
                foreach (FamilyLabel familyLabel in familyLabels)
                {
                    //create padding to clear unused merge fields
                    List<string> padding = Enumerable.Repeat(" ", familyLabel.MergeKeys.Count).ToList();
                    familyLabel.MergeFields.AddRange(padding);
                    for (int i = 0; i < familyLabel.MergeKeys.Count; i++)
                    {
                        familyLabel.LabelObj.MergeFields[familyLabel.MergeKeys[i]] = familyLabel.MergeFields[i];
                    }
                    labels.Add(familyLabel.LabelObj);
                }

                // Print client labels
                if (labels.Any(l => l.PrintFrom == Rock.Model.PrintFrom.Client))
                {
                    var clientLabels = labels.Where(l => l.PrintFrom == PrintFrom.Client).ToList();
                    var urlRoot = string.Format("{0}://{1}", Request.Url.Scheme, Request.Url.Authority);
                    clientLabels.ForEach(l => l.LabelFile = urlRoot + l.LabelFile);
                    AddLabelScript(clientLabels.ToJson());
                }

                // Print server labels
                if (labels.Any(l => l.PrintFrom == Rock.Model.PrintFrom.Server))
                {
                    string delayCut = @"^XB";
                    string endingTag = @"^XZ";
                    var printerIp = string.Empty;
                    var labelContent = new StringBuilder();

                    // make sure labels have a valid ip
                    var lastLabel = labels.Last();
                    foreach (var label in labels.Where(l => l.PrintFrom == PrintFrom.Server && !string.IsNullOrEmpty(l.PrinterAddress)))
                    {
                        var labelCache = KioskLabel.Read(label.FileGuid);
                        if (labelCache != null)
                        {
                            if (printerIp != label.PrinterAddress)
                            {
                                printQueue.AddOrReplace(label.PrinterAddress, labelContent);
                                printerIp = label.PrinterAddress;
                                labelContent = new StringBuilder();
                            }

                            var printContent = labelCache.FileContent;
                            foreach (var mergeField in label.MergeFields)
                            {
                                if (!string.IsNullOrWhiteSpace(mergeField.Value))
                                {
                                    printContent = Regex.Replace(printContent, string.Format(@"(?<=\^FD){0}(?=\^FS)", mergeField.Key), ZebraFormatString(mergeField.Value));
                                }
                                else
                                {
                                    printContent = Regex.Replace(printContent, string.Format(@"\^FO.*\^FS\s*(?=\^FT.*\^FD{0}\^FS)", mergeField.Key), string.Empty);
                                    printContent = Regex.Replace(printContent, string.Format(@"\^FD{0}\^FS", mergeField.Key), "^FD^FS");
                                }
                            }

                            // send a Delay Cut command at the end to prevent cutting intermediary labels
                            if (label != lastLabel)
                            {
                                printContent = Regex.Replace(printContent.Trim(), @"\" + endingTag + @"$", delayCut + endingTag);
                            }

                            labelContent.Append(printContent);
                        }
                    }

                    printQueue.AddOrReplace(printerIp, labelContent);
                }

                if (printQueue.Any())
                {
                    PrintLabels(printQueue);
                    printQueue.Clear();
                }
            }
        }

        /// <summary>
        /// Prints the labels.
        /// </summary>
        /// <param name="families">The families.</param>
        private void PrintLabels(Dictionary<string, StringBuilder> printerContent)
        {
            foreach (var printerIp in printerContent.Keys.Where(k => !string.IsNullOrEmpty(k)))
            {
                StringBuilder labelContent;
                if (printerContent.TryGetValue(printerIp, out labelContent))
                {
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var printerIpEndPoint = new IPEndPoint(IPAddress.Parse(printerIp), 9100);
                    var result = socket.BeginConnect(printerIpEndPoint, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(5000, true);

                    if (socket.Connected)
                    {
                        var ns = new NetworkStream(socket);
                        byte[] toSend = System.Text.Encoding.ASCII.GetBytes(labelContent.ToString());
                        ns.Write(toSend, 0, toSend.Length);
                    }
                    else
                    {
                        //phPrinterStatus.Controls.Add(new LiteralControl(string.Format("Can't connect to printer: {0}", printerIp)));
                    }

                    if (socket != null && socket.Connected)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Adds the label script.
        /// </summary>
        /// <param name="jsonObject">The json object.</param>
        private void AddLabelScript(string jsonObject)
        {
            string script = string.Format(@"
            var labelData = {0};
		    function printLabels() {{
		        ZebraPrintPlugin.printTags(
            	    JSON.stringify(labelData),
            	    function(result) {{
			        }},
			        function(error) {{
				        // error is an array where:
				        // error[0] is the error message
				        // error[1] determines if a re-print is possible (in the case where the JSON is good, but the printer was not connected)
			            console.log('An error occurred: ' + error[0]);
                        navigator.notification.alert(
                            'An error occurred while printing the labels.' + error[0],  // message
                            alertDismissed,         // callback
                            'Error',            // title
                            'Ok'                  // buttonName
                        );
			        }}
                );
	        }}
try{{
            printLabels();
}} catch(e){{}}
            __doPostBack('{1}','OnClick');
            ", jsonObject, btnCancel.UniqueID);
            ScriptManager.RegisterStartupScript(upContent, upContent.GetType(), "addLabelScript", script, true);
        }

        /// <summary>
        /// Formats the Zebra string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="isJson">if set to <c>true</c> [is json].</param>
        /// <returns></returns>
        private static string ZebraFormatString(string input, bool isJson = false)
        {
            if (isJson)
            {
                return input.Replace("é", @"\\82");  // fix acute e
            }
            else
            {
                return input.Replace("é", @"\82");  // fix acute e
            }
        }

        protected void btnParentGroupTypeHeader_Click(object sender, EventArgs e)
        {
            Session["modalActive"] = true;
            Session["modalGroupType"] = true;
            ShowGroupTypeChangeModal();
        }

        private void ShowGroupTypeChangeModal()
        {
            if (parentGroupTypesList.Count > 0)
            {
                foreach (var parentGroupType in parentGroupTypesList)
                {
                    var btnParentGroupType = new BootstrapButton();
                    btnParentGroupType.CssClass = "btn btn-default btn-lg btn-block";
                    btnParentGroupType.Text = "<br>" + parentGroupType.Name + "<br><br>";
                    btnParentGroupType.DataLoadingText= "<br><i class='fa fa-refresh fa-spin'></i> Selecting Program...<br><br>";
                    btnParentGroupType.ID = parentGroupType.Guid.ToString();
                    btnParentGroupType.Click += (s, e1) => { ChangeParentGroupType(parentGroupType); };
                    phModal.Controls.Add(btnParentGroupType);
                }
                mdChoose.Title = "Select Program";
                mdChoose.Show();
                mdChoose.CancelLinkVisible = false;
            }
            else
            {
                mdChoose.Hide();
            }
            
        }
    }
    public class FamilyLabel
    {
        public Guid FileGuid { get; set; }

        public CheckInLabel LabelObj { get; set; }

        private List<string> _mergeFields = new List<string>();
        public List<string> MergeFields
        {
            get
            {
                return _mergeFields;
            }
            set
            {
                _mergeFields = value;
            }
        }
        private List<string> _mergeKeys = new List<string>();

        public List<string> MergeKeys
        {
            get
            {
                return _mergeKeys;
            }
            set
            {
                _mergeKeys = value;
            }
        }
    }
}