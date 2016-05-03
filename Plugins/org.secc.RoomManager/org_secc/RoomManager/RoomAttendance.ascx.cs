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
            rockContext = new RockContext();
            attendanceService = new AttendanceService(rockContext);
            if (!Page.IsPostBack)
            {
                ViewState["LocationId"] = null;
                ViewState["Tab"] = null;
                ViewState["Barcode"] = false;
                BindLocations();
                SaveViewState();
            }
            else
            {
                if (!(bool)ViewState["Barcode"])
                {
                    UpdateView();
                }
                //Hack to recieve custom reports from our barcode reader
                if (Request["__EVENTTARGET"] == upMain.ClientID)
                {
                    BarcodeSearch(Request["__EVENTARGUMENT"]);
                }
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
                    pnlMenue.Visible = true;
                    ltLocation.Text = location.Name;
                    pnlLocation.Visible = false;

                    //Start page refresh script
                    ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "startTimer", "startTimer();", true);

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

        protected void ddlAction_SelectionChanged(object sender, EventArgs e)
        {
            var tab = ddlAction.SelectedValue;
            ViewState["Tab"] = tab;
            SaveViewState();
            UpdateView();
        }

        protected void btnBarcode_Click(object sender, EventArgs e)
        {
            ViewState["Barcode"] = !(bool)ViewState["Barcode"];
            SaveViewState();

            if ((bool)ViewState["Barcode"])
            {
                btnBarcode.CssClass = "btn btn-success btn-block";
                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "enable", "enableBarcode();", true);
                pnlTagSearch.Visible = false;
            }
            else
            {
                btnBarcode.CssClass = "btn btn-default btn-block";
                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "disable", "disableBarcode();", true);
                if ((string)ViewState["Tab"] == "Search")
                {
                    pnlTagSearch.Visible = true;
                }
                UpdateView();
            }
        }

        private void UpdateView()
        {
            phCheckin.Controls.Clear();
            phCheckout.Controls.Clear();
            phSearch.Controls.Clear();

            pnlTagSearch.Visible = false;

            int? locationId = (int?)ViewState["LocationId"];
            if (locationId != null)
            {
                if ((string)ViewState["Tab"]=="Checkin" && (bool)ViewState["Barcode"] == false)
                {
                    //Display checkin tab

                    pnlCheckin.Visible = true;

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
                }
                if ((string)ViewState["Tab"] == "Checkout" && (bool)ViewState["Barcode"] == false)
                {
                    //Display checkout tab

                    pnlCheckout.Visible = true;

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
                }


                if ((string)ViewState["Tab"] == "Search" && (bool)ViewState["Barcode"] == false)
                {

                    pnlTagSearch.Visible = true;

                    //Display search information
                    var code = tbTagSearch.Text;
                    if (code != "")
                    {
                        List<Attendance> attendances = SearchByTag(code);
                        AttendanceCodeService attendanceCodeService = new AttendanceCodeService(rockContext);

                        AttendanceCode attendanceCode = attendanceCodeService.Queryable().Where(ac => ac.Code == code &&
                                                                                DbFunctions.TruncateTime(ac.IssueDateTime) == RockDateTime.Now.Date)
                                                                                .FirstOrDefault();

                        foreach (var attendance in attendances)
                        {
                            BootstrapButton btnPerson = new BootstrapButton();
                            btnPerson.ID = attendance.PersonAlias.Person.Guid.ToString()+"searched";
                            btnPerson.Text = attendance.PersonAlias.Person.FullNameReversed;
                            btnPerson.CssClass = "btn btn-default btn-block";
                            btnPerson.Click += (s, e) => { MovePerson(attendance.PersonAlias.Person, attendanceCode); };
                            phSearch.Controls.Add(btnPerson);
                        }
                    }
                }
            }
        }

        private List<Attendance> SearchByTag(string code)
        {
            code = code.Trim().ToUpper();

            AttendanceCodeService attendanceCodeService = new AttendanceCodeService(rockContext);

            AttendanceCode attendanceCode = attendanceCodeService.Queryable().Where(ac => ac.Code == code &&
                                                                            DbFunctions.TruncateTime(ac.IssueDateTime) == RockDateTime.Now.Date)
                                                                            .FirstOrDefault();
            
            if (attendanceCode != null)
            {
                List<Attendance> attendances = attendanceService.Queryable()
                    .Where(a => a.AttendanceCodeId == attendanceCode.Id)
                    .Where(a => a.PersonAlias.Person != null)
                    .DistinctBy(a => a.PersonAlias.Person.Guid)
                    .ToList();
                return attendances;
            }
            return new List<Attendance>();

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
            ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "toast", "toastr.success('Moved " + person.PrimaryAlias.Person.FullName+"')", true);
            phSearch.Controls.Clear();
        }
        private void CheckOutPerson(Person person)
        {
            int? locationId = (int?)ViewState["LocationId"];
            Attendance attendance = attendanceService.Queryable().Where(a =>
                                                            a.PersonAlias.PersonId == person.Id &&
                                                            DbFunctions.TruncateTime(a.StartDateTime) == RockDateTime.Now.Date &&
                                                            a.EndDateTime == null &&
                                                            a.LocationId == locationId &&
                                                            a.DidAttend==true)
                                                            .FirstOrDefault();
            if (attendance !=null)
            {
                attendance.EndDateTime = RockDateTime.Now;
                rockContext.SaveChanges();
                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "toast", "toastr.success('Checked out " + person.FullName + "')", true);
            }
            else
            {
                ScriptManager.RegisterStartupScript( upMain, upMain.GetType(), "toast", "toastr.error('Could not checkout " + person.FullName + 
                    ". Attendance record not found. Child may not have been checked into room.')", true );
            }
        

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
            if ( attendance != null )
            {
                attendance.StartDateTime = RockDateTime.Now;
                attendance.DidAttend = true;
                rockContext.SaveChanges();

                ScriptManager.RegisterStartupScript(upMain, upMain.GetType(), "toast", "toastr.success('Checked In "
                                                                    + person.FullName +
                                                                    " to " + attendance.Location.Name +"')", true);
            }
            else
            {
                ScriptManager.RegisterStartupScript( upMain, upMain.GetType(), "toast", "toastr.error('Could not check in: "
                                                                    + person.PrimaryAlias.Person.FullName +
                                                                    ". Attendance entry not found. Child may be checked into different room.')", true );
            }


            UpdateView();
        }


        protected void btnTagSearch_Click(object sender, EventArgs e)
        {
           UpdateView();
        }

        private void BarcodeSearch(string code)
        {
            if (code == "")
            {
                return;
            }
            AttendanceCodeService attendanceCodeService = new AttendanceCodeService(rockContext);

            AttendanceCode attendanceCode = attendanceCodeService.Queryable().Where(ac => ac.Code == code &&
                                                                            DbFunctions.TruncateTime(ac.IssueDateTime) == RockDateTime.Now.Date)
                                                                            .FirstOrDefault();
            List<Attendance> attendances = SearchByTag(code);
            foreach (var attendance in attendances)
            {
                if (ViewState["Tab"] != null)
                {
                    switch ((string)ViewState["Tab"])
                    {
                        case "Checkout":
                            CheckOutPerson(attendance.PersonAlias.Person);
                            break;
                        case "Checkin":
                            CheckInPerson(attendance.PersonAlias.Person);
                            break;
                        case "Search":
                            MovePerson(attendance.PersonAlias.Person, attendanceCode);
                            break;
                    }
                }
            }
        }

        
    }
}