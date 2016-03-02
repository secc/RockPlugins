using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ComponentModel;

using Rock;
using Rock.Web.UI;
using System.Web.UI.HtmlControls;
using Rock.Web.UI.Controls;
using Rock.Data;
using Rock.Model;
using System.Data.Entity;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.RoomManager
{
    [DisplayName("Room Attendance")]
    [Category("Check-in")]
    [Description("Allows volunteers to manage actual checkin of children.")]
    public partial class RoomAttendance : RockBlock
    {
        private RockContext rockContext;
        private AttendanceService attendanceService;
        private List<Location> locations;

        protected override void OnLoad(EventArgs e)
        {
            nbSearch.Visible = false;
            rockContext = new RockContext();
            attendanceService = new AttendanceService(rockContext);
            if (!Page.IsPostBack)
            {
                ViewState["LocationId"] = null;
                ViewState["Tab"] = null;
                BindLocations();
                SaveViewState();
            }
            else
            {
                UpdateView();
            }
        }

        private void BindLocations()
        {
            Device device = AttemptKioskMatchByIpOrName();
            if (device != null)
            {
                locations = device.Locations.ToList();
                ddlLocation.DataSource = locations;
                ddlLocation.DataTextField = "Name";
                ddlLocation.DataValueField = "Id";
                ddlLocation.DataBind();
            }

        }

        protected void ddlLocation_SelectionChanged(object sender, EventArgs e)
        {
            //Check to make sure this device can access this data
            Device device = AttemptKioskMatchByIpOrName();
            if (device != null)
            {
                locations = device.Locations.ToList();
                var location = locations.Where(l => l.Id.ToString() == ddlLocation.SelectedValue).FirstOrDefault();
                if (location != null)
                {
                    ViewState["LocationId"] = location.Id;
                    UpdateView();
                }
            }
        }

        /// <summary>
        /// Attempts to match a known kiosk based on the IP address of the client.
        /// </summary>
        private Device AttemptKioskMatchByIpOrName()
        {
            // match kiosk by ip/name.
            string hostIp = Request.ServerVariables["REMOTE_ADDR"];
            string forwardedIp = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            string ipAddress = forwardedIp ?? hostIp;
            bool skipDeviceNameLookup = false;

            var rockContext = new RockContext();
            var checkInDeviceTypeId = DefinedValueCache.Read(new Guid("BCCA780E-17C2-4CD5-8480-8C4EB3F6D695") ).Id;
            var device = new DeviceService(rockContext).GetByIPAddress(ipAddress, checkInDeviceTypeId, skipDeviceNameLookup);
            
            return device;


        }
        protected void btnCheckout_Click(object sender, EventArgs e)
        {
            ViewState["Tab"] = "Checkout";
            SaveViewState();
            MakeVisible(pnlCheckout, liCheckout);
        }

        protected void btnCheckin_Click(object sender, EventArgs e)
        {
            ViewState["Tab"] = "Checkin";
            SaveViewState();
            MakeVisible(pnlCheckin, liCheckin);
        }
        protected void btnShowSearch_Click(object sender, EventArgs e)
        {
            ViewState["Tab"] = "Search";
            SaveViewState();
            MakeVisible(pnlTagSearch, liTagSearch);
        }

        private void UpdateView()
        {
            phCheckin.Controls.Clear();
            phCheckout.Controls.Clear();
            phSearch.Controls.Clear();

            int? locationId = (int?)ViewState["LocationId"];
            if (locationId != null)
            {
                //Display checkin tab
                List<Person> reservedPeople = attendanceService.Queryable().Where(a =>
                                                a.LocationId == locationId &&
                                                a.DidAttend == false &&
                                                DbFunctions.TruncateTime(a.StartDateTime) == RockDateTime.Now.Date)
                                                .Select(a => a.PersonAlias)
                                                .Select(pa => pa.Person)
                                                .OrderBy(p => p.FirstName)
                                                .OrderBy(p => p.LastName)
                                                .ToList();

                foreach (var person in reservedPeople)
                {
                    BootstrapButton btnPerson = new BootstrapButton();
                    btnPerson.ID = person.Guid.ToString();
                    btnPerson.Text = person.FullNameReversed;
                    btnPerson.CssClass = "btn btn-default btn-block";
                    btnPerson.Click += (s, e) => { CheckInPerson(person); };
                    phCheckin.Controls.Add(btnPerson);
                }

                //Display checkout tab
                List<Person> checkedInPeople = attendanceService.Queryable().Where(a =>
                                                a.LocationId == locationId &&
                                                a.DidAttend == true &&
                                                DbFunctions.TruncateTime(a.StartDateTime) == RockDateTime.Now.Date &&
                                                a.EndDateTime == null)
                                                .Select(a => a.PersonAlias)
                                                .Select(pa => pa.Person)
                                                .OrderBy(p => p.FirstName)
                                                .OrderBy(p => p.LastName)
                                                .ToList();

                foreach (var person in checkedInPeople)
                {
                    BootstrapButton btnPerson = new BootstrapButton();
                    btnPerson.ID = person.Guid.ToString();
                    btnPerson.Text = person.FullNameReversed;
                    btnPerson.CssClass = "btn btn-default btn-block";
                    btnPerson.Click += (s, e) => { CheckOutPerson(person); };
                    phCheckout.Controls.Add(btnPerson);
                }

                //Display search information
                var code = tbTagSearch.Text.Trim().ToUpper();
                if (code != "")
                {
                    AttendanceCodeService attendanceCodeService = new AttendanceCodeService(rockContext);

                    AttendanceCode attendanceCode = attendanceCodeService.Queryable().Where(ac => ac.Code == code &&
                                                                                    DbFunctions.TruncateTime(ac.IssueDateTime) == RockDateTime.Now.Date)
                                                                                    .FirstOrDefault();
                    if (attendanceCode != null)
                    {
                        List<Person> people = attendanceService.Queryable()
                            .Where(a => a.AttendanceCodeId == attendanceCode.Id)
                            .Select(a => a.PersonAlias.Person)
                            .Where(p => p != null)
                            .DistinctBy(p => p.Guid)
                            .ToList();

                        foreach (var person in people)
                        {
                            BootstrapButton btnPerson = new BootstrapButton();
                            btnPerson.ID = person.Guid.ToString()+"searched";
                            btnPerson.Text = person.FullNameReversed;
                            btnPerson.CssClass = "btn btn-default btn-block";
                            btnPerson.Click += (s, e) => { MovePerson(person, attendanceCode); };
                            phSearch.Controls.Add(btnPerson);
                        }
                    }


                }

                ReSelectTabs();
            }
        }

        private void MovePerson(Person person, AttendanceCode attendanceCode)
        {
            int? locationId = (int?)ViewState["LocationId"];

            Location location = new LocationService(rockContext).Queryable().Where(l => l.Id == locationId).FirstOrDefault();

            if (location == null) return;


            //Close out any open attendance location
            List<Attendance> attendances = attendanceService.Queryable().Where(a =>
                                                            a.PersonAlias.PersonId == person.Id &&
                                                            DbFunctions.TruncateTime(a.StartDateTime) == RockDateTime.Now.Date)
                                                            .ToList();

            foreach (var openAttendance in attendances)
            {
                if (openAttendance.EndDateTime == null)
                {
                    openAttendance.EndDateTime = RockDateTime.Now;
                }
            }

            //Create new attendance record
            Attendance attendance = new Attendance();
            attendance.LocationId = locationId;
            attendance.CampusId = location.CampusId;
            attendance.PersonAliasId = person.PrimaryAliasId;
            attendance.StartDateTime = RockDateTime.Now;
            attendance.AttendanceCodeId = attendanceCode.Id;
            attendance.DidAttend = true;
            attendanceService.Add(attendance);
            rockContext.SaveChanges();

            //Clear text search and notify
            tbTagSearch.Text = "";
            phSearch.Controls.Clear();
            nbSearch.Text = "Child has been moved to this room";
            nbSearch.Visible = true;
        }

        private void ReSelectTabs()
        {
            if (ViewState["Tab"] != null)
            {
                switch ((string)ViewState["Tab"])
                {
                    case "Checkout":
                        MakeVisible(pnlCheckout, liCheckout);
                        break;
                    case "Checkin":
                        MakeVisible(pnlCheckin, liCheckin);
                        break;

                }
            }
        }

        private void CheckOutPerson(Person person)
        {
            int? locationId = (int?)ViewState["LocationId"];
            Attendance attendance = attendanceService.Queryable().Where(a =>
                                                            a.PersonAlias.PersonId == person.Id &&
                                                            DbFunctions.TruncateTime(a.StartDateTime) == RockDateTime.Now.Date &&
                                                            a.LocationId == locationId)
                                                            .FirstOrDefault();
            attendance.EndDateTime = RockDateTime.Now;
            rockContext.SaveChanges();

            UpdateView();
        }

        private void CheckInPerson(Person person)
        {
            int? locationId = (int?)ViewState["LocationId"];
            Attendance attendance = attendanceService.Queryable().Where(a =>
                                                            a.PersonAlias.PersonId == person.Id &&
                                                            DbFunctions.TruncateTime(a.StartDateTime) == RockDateTime.Now.Date &&
                                                            a.LocationId == locationId)
                                                            .FirstOrDefault();
            attendance.StartDateTime = RockDateTime.Now;
            attendance.DidAttend = true;
            rockContext.SaveChanges();

            UpdateView();

        }

        private void MakeVisible(Panel pnlShow, HtmlGenericControl liShow)
        {
            pnlCheckin.Visible = false;
            pnlCheckout.Visible = false;
            pnlTagSearch.Visible = false;
            liCheckin.RemoveCssClass("active");
            liCheckout.RemoveCssClass("active");
            liTagSearch.RemoveCssClass("active");

            pnlShow.Visible = true;
            liShow.AddCssClass("active");
        }

        protected void btnTagSearch_Click(object sender, EventArgs e)
        {
           UpdateView();
        }

        
    }
}