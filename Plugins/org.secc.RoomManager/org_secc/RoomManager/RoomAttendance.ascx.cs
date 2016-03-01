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

namespace RockWeb.Plugins.org_secc.RoomManager
{
    [DisplayName("Room Attendance")]
    [Category("Check-in")]
    [Description("Allows volunteers to manage actual checkin of children.")]
    public partial class RoomAttendance : RockBlock
    {
        private RockContext rockContext;
        private AttendanceService attendanceService;

        protected override void OnLoad(EventArgs e)
        {
            rockContext = new RockContext();
            attendanceService = new AttendanceService(rockContext);
            if (!Page.IsPostBack)
            {
                ViewState["LocationId"] = null;
                ViewState["Tab"] = null;
                SaveViewState();
            }
            else
            {
                UpdateView();
            }
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

        protected void btnChangeLocation_Click(object sender, EventArgs e)
        {
            if (lpLocation.Location != null)
            {
                ViewState["LocationId"] = lpLocation.Location.Id;
                ViewState["LocationName"] = lpLocation.Location.Name;
                SaveViewState();
                UpdateView();
            }
        }

        private void UpdateView()
        {
            phCheckin.Controls.Clear();
            phCheckout.Controls.Clear();
            int? locationId = (int?)ViewState["LocationId"];
            if (locationId!=null)
            {
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

            ReSelectTabs();

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
            ViewState["Tab"] = pnlShow.ID;
            pnlCheckin.Visible = false;
            pnlCheckout.Visible = false;
            liCheckin.RemoveCssClass("active");
            liCheckout.RemoveCssClass("active");

            pnlShow.Visible = true;
            liShow.AddCssClass("active");
        }
    }
}