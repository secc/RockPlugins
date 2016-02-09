
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

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName("QuickCheckin")]
    [Category("Check-in")]
    [Description("QuickCheckin block for helping parents find their family quickly.")]

    public partial class QuickCheckin : CheckInBlock
    {

        private Schedule currentSchedule;
        private List<QCPerson> qcPeople;

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            //RockPage.AddScriptLink("~/Scripts/iscroll.js");
            //RockPage.AddScriptLink("~/Scripts/CheckinClient/checkin-core.js");

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
                bool test = ProcessActivity(workflowActivity, out errors);
                qcPeople = new List<QCPerson>();
                LoadQCPeople();
                Session.Add("qcPeople", qcPeople);
                currentSchedule = GetCurrentSchedule();
                Session.Add("currentSchedule", currentSchedule);
            }

            qcPeople = (List<QCPerson>)Session["qcPeople"];
            currentSchedule = (Schedule)Session["currentSchedule"];


            if (qcPeople == null || qcPeople.Count == 0)
            {
                NavigateToPreviousPage();
                return;
            }


            DisplaySchedules();
            DisplayPeople();
        }

        private Schedule GetCurrentSchedule()
        {
            var qcPerson = qcPeople.FirstOrDefault();
            if (qcPerson != null && qcPerson.SelectedSchedule != null)
            {
                return qcPerson.SelectedSchedule.Schedule;
            }
            else if (qcPerson.Schedules.Count > 0)
            {
                return qcPerson.Schedules.FirstOrDefault().Schedule;
            }
            return null;
        }

        private void LoadQCPeople()
        {
            List<Schedule> schedules = GetAllSchedules();
            var allPeopleList = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected).FirstOrDefault()
                    .People.OrderBy(p => p.Person.FullNameReversed).ToList();
            foreach (var person in allPeopleList)
            {
                var qcPerson = new QCPerson(person.Person);
                foreach (var schedule in schedules)
                {
                    if (PersonHasSchedule(person, schedule))
                    {
                        QCSchedule qcSchedule = new QCSchedule(schedule);
                        qcPerson.Schedules.Add(qcSchedule);
                        foreach (var groupType in person.GroupTypes)
                        {
                            if (GroupTypeHasSchedule(groupType, schedule))
                            {
                                QCGroupType qcGroupType = new QCGroupType(groupType.GroupType);
                                qcSchedule.GroupTypes.Add(qcGroupType);
                                if (groupType.Selected)
                                {
                                    qcSchedule.SelectedGroupType = qcGroupType;
                                }
                                foreach (var group in groupType.Groups)
                                {
                                    if (GroupHasSchedule(group, schedule))
                                    {
                                        QCGroup qcGroup = new QCGroup(group.Group);
                                        qcGroupType.Groups.Add(qcGroup);
                                        if (group.Selected)
                                        {
                                            qcGroupType.SelectedGroup = qcGroup;
                                        }
                                        foreach (var location in group.Locations)
                                        {
                                            if (LocationHasSchedule(location, schedule))
                                            {
                                                qcGroup.Locations.Add(location.Location);
                                                if (location.Selected)
                                                {
                                                    qcGroup.SelectedLocation = location.Location;
                                                    foreach (var locationSchedule in location.Schedules)
                                                    {
                                                        if (locationSchedule.Selected && locationSchedule.Schedule.Guid == schedule.Guid)
                                                        {
                                                            qcPerson.SelectedSchedule = qcSchedule;
                                                            qcPerson.Enabled = true;
                                                            qcPerson.Selected = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                qcPeople.Add(qcPerson);
            }
        }


        private void DisplayPeople()
        {
            foreach (var qcPerson in qcPeople)
            {
                DisplayPersonButton(qcPerson);
                DisplayPersonClasses(qcPerson);
            }
            UpdateAllClasses();
        }

        private void DisplayPersonButton(QCPerson qcPerson)
        {
            HtmlGenericControl hgcRow = new HtmlGenericControl("div");
            hgcRow.AddCssClass("row");
            phPeople.Controls.Add(hgcRow);
            qcPerson.PlaceHolder = hgcRow;
            //Checkin Button
            var btnPerson = new BootstrapButton();
            btnPerson.DataLoadingText = "<img src = '" + qcPerson.Person.PhotoUrl + "' style = 'height:100px;'><br /> Please Wait...";
            btnPerson.Text = "<img src='" + qcPerson.Person.PhotoUrl + "' style='height:100px;'><br>" + qcPerson.Person.FullName;
            btnPerson.Click += (s, e) => { TogglePerson(qcPerson); };
            hgcRow.Controls.Add(btnPerson);
            qcPerson.UpdatePersonButton();
        }

        private void DisplayPersonClasses(QCPerson qcPerson)
        {
            //Bootstrap well to display if person is not able to check in.
            var wellNoAreas = new HtmlGenericContainer("div", "well col-xs-12 col-md-6");
            qcPerson.PlaceHolder.Controls.Add(wellNoAreas);

            //Area Button
            var btnGroupType = new BootstrapButton();
            btnGroupType.CssClass = "btn btn-default btn-lg col-xs-12 col-md-4";
            qcPerson.PlaceHolder.Controls.Add(btnGroupType);

            //Group Button
            var btnGroup = new BootstrapButton();
            btnGroup.CssClass = "btn btn-default btn-lg col-xs-12 col-md-4";
            qcPerson.PlaceHolder.Controls.Add(btnGroup);

            //Room Button
            var btnLocation = new BootstrapButton();
            btnLocation.CssClass = "btn btn-default btn-lg col-xs-12 col-md-8";
            qcPerson.PlaceHolder.Controls.Add(btnLocation);
        }

        private void UpdateAllClasses()
        {
            foreach (var qcPerson in qcPeople)
            {
                HtmlGenericContainer wellNoAreas = (HtmlGenericContainer)qcPerson.PlaceHolder.Controls[1];
                BootstrapButton btnGroupType = (BootstrapButton)qcPerson.PlaceHolder.Controls[2];
                BootstrapButton btnGroup = (BootstrapButton)qcPerson.PlaceHolder.Controls[3];
                BootstrapButton btnLocation = (BootstrapButton)qcPerson.PlaceHolder.Controls[4];

                if (qcPerson.Enabled == false || qcPerson.SelectedSchedule == null)
                {
                    if (qcPerson.SelectedSchedule == null)
                    {
                        wellNoAreas.InnerText = "(Please select a time to check in.)";
                    }
                    else
                    {
                        wellNoAreas.InnerText = "(No Rooms Available At This Time)";
                    }
                    btnGroupType.Visible = false;
                    btnGroup.Visible = false;
                    btnLocation.Visible = false;
                }
                else
                {
                    wellNoAreas.Visible = false;
                    btnGroupType.Visible = true;
                    if (qcPerson.SelectedSchedule.SelectedGroupType == null)
                    {
                        if (qcPerson.SelectedSchedule.GroupTypes.Count == 0)
                        {
                            //No Areas availble, disable button.
                            btnGroupType.Text = "(No areas available)";
                            btnGroupType.Enabled = false;
                        }
                        else
                        {
                            btnGroupType.Text = "(Please select area)";
                        }
                        btnGroup.Visible = false;
                        btnLocation.Visible = false;
                    }
                    else //Area is not null
                    {
                        //Display Area Name
                        btnGroupType.Text = qcPerson.SelectedSchedule.SelectedGroupType.GroupType.Name;
                        //Make Group Button Visible
                        btnGroup.Visible = true;

                        if (qcPerson.SelectedSchedule.SelectedGroupType.SelectedGroup == null)
                        {
                            //Select Group
                            if (qcPerson.SelectedSchedule.SelectedGroupType.Groups.Count == 0)
                            {
                                //No Group availble, disable button.
                                btnGroup.Text = "(No groups available)";
                                btnGroup.Enabled = false;
                            }
                            else
                            {
                                btnGroup.Text = "(Please select group)";
                            }
                            btnLocation.Visible = false;
                        }
                        else //Group is not null
                        {
                            //Display Group Name
                            btnGroup.Text = qcPerson.SelectedSchedule.SelectedGroupType.SelectedGroup.Group.Name;
                            btnLocation.Visible = true;

                            if (qcPerson.SelectedSchedule.SelectedGroupType.SelectedGroup.SelectedLocation == null)
                            {
                                //Select Room
                                if (qcPerson.SelectedSchedule.SelectedGroupType.Groups.Count == 0)
                                {
                                    //No room availble, disable button.
                                    btnGroup.Text = "(No rooms available)";
                                    btnGroup.Enabled = false;
                                }
                                else
                                {
                                    btnGroup.Text = "(Please select room)";
                                }
                            }
                            else
                            {
                                //Display Room Name
                                btnLocation.Text = qcPerson.SelectedSchedule.SelectedGroupType.SelectedGroup.SelectedLocation.Name;
                            }
                        }
                    }
                }
            }
        }

        private void TogglePerson(QCPerson qcPerson)
        {
            if (qcPerson.Selected == false && qcPerson.Enabled == true)
            {
                qcPerson.Selected = true;
                ((BootstrapButton)qcPerson.PlaceHolder.Controls[0]).CssClass = "btn btn-success btn-lg col-md-4 vcenter";
                ((BootstrapButton)qcPerson.PlaceHolder.Controls[0]).Enabled = true;
            }
            else {
                qcPerson.Selected = false;
                ((BootstrapButton)qcPerson.PlaceHolder.Controls[0]).CssClass = "btn btn-default btn-lg col-md-4 vcenter";
            }
            Session["qcPeople"] = qcPeople;
        }

        private void DisplaySchedules()
        {
            List<Schedule> schedules = GetAllSchedules();

            foreach (var schedule in schedules)
            {

                Button btnSchedule = null;
                // See if we've already created this button
                foreach (Control control in phSchedules.Controls)
                {
                    if (control.GetType() == typeof(Button) && ((Button)control).Text == schedule.Name)
                    {
                        btnSchedule = (Button)control;
                    }
                }
                // If we don't have a button already, go ahead and create a new button
                if (btnSchedule == null)
                {
                    btnSchedule = new Button();
                    btnSchedule.Click += (s, e) => { UpdateSchedule(schedule); };
                    phSchedules.Controls.Add(btnSchedule);
                }

                // Set/Update all the properties on the buttons
                btnSchedule.Text = schedule.Name;

                //btnSchedule.UseSubmitBehavior = false;
                if (currentSchedule != null && schedule.Guid == currentSchedule.Guid)
                {
                    btnSchedule.CssClass = "btn btn-primary";
                    btnSchedule.Enabled = false;
                }
                else
                {
                    btnSchedule.Enabled = true;
                    btnSchedule.CssClass = "btn btn-default";
                }
            }
        }

        private void UpdateSchedule(Schedule schedule)
        {
            //Update class and session variables
            currentSchedule = schedule;
            Session["currentSchedule"] = currentSchedule;

            //Update session information for all people
            foreach (var qcPerson in qcPeople)
            {
                qcPerson.ChangeSchedule(currentSchedule);
                
            }

            DisplaySchedules();
        }


        private bool PersonHasSchedule(CheckInPerson person, Schedule schedule)
        {
            foreach(var groupType in person.GroupTypes)
            {
                if(GroupTypeHasSchedule(groupType, schedule))
                {
                    return true;
                }
            }

            return false;
        }

        private bool GroupTypeHasSchedule(CheckInGroupType groupType, Schedule schedule)
        {
            foreach (var group in groupType.Groups)
            {
                if (GroupHasSchedule(group, schedule))
                {
                    return true;
                }
            }
            return false;
        }

        private bool GroupHasSchedule(CheckInGroup group, Schedule schedule)
        {
            foreach (var location in group.Locations)
            {
                if (LocationHasSchedule(location, schedule))
                {
                    return true;
                }
            }
            return false;
        }

        private bool LocationHasSchedule(CheckInLocation location, Schedule schedule)
        {
            foreach (var locationSchedule in location.Schedules)
            {
                if (locationSchedule.Schedule.ToString() == schedule.ToString())
                {
                    return true;
                }
            }
            return false;
        }

        private List<Schedule> GetAllSchedules()
        {
            List<Schedule> allSchedules = new List<Schedule>();

            var allPeopleList = CurrentCheckInState.CheckIn.Families.Where(f => f.Selected).FirstOrDefault()
            .People.OrderBy(p => p.Person.FullNameReversed).ToList();

            foreach (var person in allPeopleList)
            {
                var allGroupsTypes = person.GroupTypes.ToList();

                foreach (var groupType in allGroupsTypes)
                {
                    var allGroups = groupType.Groups.ToList();

                    foreach (var group in allGroups)
                    {
                        var allLocations = group.Locations.ToList();

                        foreach (var location in allLocations)
                        {
                            foreach(var schedule in location.Schedules)
                            {
                                allSchedules.Add(schedule.Schedule);
                            }

                        }
                    }
                }
            }
            allSchedules = allSchedules.DistinctBy(s =>s.Guid).ToList();

            return allSchedules;
        }
    }

    class QCGroup
    {
        public QCGroup(Rock.Model.Group group)
        {
            this.Group = group;
            this.Locations = new List<Location>();
        }
        public Rock.Model.Group Group { get; set; }
        public List<Location> Locations { get; set; }
        public Location SelectedLocation { get; set; }

        internal void ChangeLocation(Location newLocation, QCPerson qcPerson)
        {
            //Only select new group if it exists in our list of eligible groups 
            SelectedLocation = Locations.FirstOrDefault(l => l.Guid == newLocation.Guid);

            if (SelectedLocation == null)
            {
                qcPerson.DisableButton();
            }
        }
    }


    class QCGroupType
    {
        public QCGroupType(GroupTypeCache groupType)
        {
            this.GroupType = groupType;
            this.Groups = new List<QCGroup>();
        }
        public GroupTypeCache GroupType { get; set; }
        public List<QCGroup> Groups { get; set; }

        public QCGroup SelectedGroup { get; set; }

        internal void ChangeGroup(QCGroup newGroup, QCPerson qcPerson)
        {
            QCGroup prevGroup = SelectedGroup;
            Location prevLocation = null;
            if (prevGroup != null)
            {
                prevLocation = prevGroup.SelectedLocation;
            }
            //Only select new group if it exists in our list of eligible groups 
            SelectedGroup = Groups.FirstOrDefault(p => p.Group.Guid == newGroup.Group.Guid);

            //Try to intelligently pick a new location and make it our selection.
            if (SelectedGroup == null)
            {
                qcPerson.DisableButton();
            }
            else
            {
                //Change selected Group (area) in the following priority: 1. Grou like Group used in previous schedule 2. Group aready in new schedule 3. First or null
                Location newLocation = SelectedGroup.Locations.FirstOrDefault(l => prevGroup.SelectedLocation != null && l.Guid == prevGroup.SelectedLocation.Guid) ??
                                                     SelectedGroup.SelectedLocation ??
                                                     SelectedGroup.Locations.FirstOrDefault();
                //Change group type
                SelectedGroup.ChangeLocation(newLocation, qcPerson);
            }
        }
    }


    class QCSchedule
    {

        public QCSchedule(Schedule schedule)
        {
            this.Schedule = schedule;
            this.GroupTypes = new List<QCGroupType>();
        }

        public Schedule Schedule;
        public List<QCGroupType> GroupTypes { get; set; }
        public QCGroupType SelectedGroupType { get; set; }

        internal void ChangeGroupType(QCGroupType newGroupType, QCPerson qcPerson)
        {
            QCGroupType prevGroupType = SelectedGroupType;
            QCGroup prevGroup = null;
            if (prevGroupType != null)
            {
                prevGroup = prevGroupType.SelectedGroup;
            }
            SelectedGroupType = GroupTypes.FirstOrDefault(p => p.GroupType.Guid == newGroupType.GroupType.Guid);

            if (SelectedGroupType == null)
            {
                qcPerson.DisableButton();
            }
            else
            {
                //Change selected Group (area) in the following priority: 1. Grou like Group used in previous schedule 2. Group aready in new schedule 3. First or null
                QCGroup newGroup = SelectedGroupType.Groups.FirstOrDefault(g => prevGroupType.SelectedGroup != null && g.Group.Guid == prevGroupType.SelectedGroup.Group.Guid) ??
                                                     SelectedGroupType.SelectedGroup ??
                                                     SelectedGroupType.Groups.FirstOrDefault();
                //Change group type
                SelectedGroupType.ChangeGroup(newGroup, qcPerson);
            }
        }
    }


    class QCPerson
    {
        private const string _defaultClass = "btn btn-default btn-lg col-xs-12 col-md-4";
        private const string _checkedClass = "btn btn-success btn-lg col-xs-12 col-md-4";
        public QCPerson()
        {
            this.Enabled = false;
            this.Selected = false;
            this.Schedules = new List<QCSchedule>();
        }
        public QCPerson(Person person)
        {
            this.Person = person;
            this.Enabled = false;
            this.Selected = false;
            this.Schedules = new List<QCSchedule>();
        }
        public HtmlContainerControl PlaceHolder { get; set; }
        public Person Person { get; set; }
        public List<QCSchedule> Schedules { get; set; }
        public QCSchedule SelectedSchedule { get; set; }
        public bool Enabled { get; set; }
        public bool Selected { get; set; }

        internal void DisableButton()
        {
            var btnPerson = (BootstrapButton)this.PlaceHolder.Controls[0];
            btnPerson.Enabled = false;
            btnPerson.CssClass = _defaultClass;
        }

        internal void EnableButton()
        {
            var btnPerson = (BootstrapButton)this.PlaceHolder.Controls[0];
            btnPerson.Enabled = true;
            //if currently selected also make green
            if (this.Selected) btnPerson.CssClass = _checkedClass;
            else btnPerson.CssClass = _defaultClass;
        }

        internal void UpdatePersonButton()
        {
            if (this.Enabled)
            {
                EnableButton();
            }
            else
            {
                DisableButton();
            }
        }

        internal void ChangeSchedule(Schedule newSchedule)
        {
            //save our selections so we can make educated guesses on choosing a new location
            QCSchedule prevSchedule = SelectedSchedule;
            QCGroupType prevGroupType = null;
            if (prevSchedule!=null)
            {
                prevGroupType = prevSchedule.SelectedGroupType;
            }

            //Change selected schedule to new schedule only if in list of available schedules
            SelectedSchedule = Schedules.FirstOrDefault(p => p.Schedule.Guid == newSchedule.Guid);
            if (SelectedSchedule == null)
            {
                DisableButton();
            }
            else
            {
                //Change selected GroupType (area) in the following priority: 1. GT like GT used in previous schedule 2. GT aready in new schedule 3. First or null
                QCGroupType newGroupType = SelectedSchedule.GroupTypes.FirstOrDefault(gt => prevSchedule.SelectedGroupType!=null && gt.GroupType.Guid == prevSchedule.SelectedGroupType.GroupType.Guid) ??
                                                     SelectedSchedule.SelectedGroupType ??
                                                     SelectedSchedule.GroupTypes.FirstOrDefault();
                //Change group type
                SelectedSchedule.ChangeGroupType(newGroupType, this);
            }
        }
    }
}