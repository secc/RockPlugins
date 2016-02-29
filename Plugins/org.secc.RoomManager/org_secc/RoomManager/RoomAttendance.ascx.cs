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
        protected override void OnLoad(EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                ViewState["LocationId"] = null;
                ViewState["LocationName"] = null;
            }
            else
            {
                UpdateView();
            }
        }

        protected void btnCheckout_Click(object sender, EventArgs e)
        {
            MakeVisible(pnlCheckout, liCheckout);
        }

        protected void btnCheckin_Click(object sender, EventArgs e)
        {
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
            int? locationId = (int?)ViewState["LocationId"];
            if (locationId!=null)
            {
                RockContext rockContext = new RockContext();
                AttendanceService attendanceService = new AttendanceService(rockContext);
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
        }

        private void CheckInPerson(Person person)
        {
            int? locationId = (int?)ViewState["LocationId"];
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService(rockContext);
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
            liCheckin.RemoveCssClass("active");
            liCheckout.RemoveCssClass("active");

            pnlShow.Visible = true;
            liShow.AddCssClass("active");
        }
    }
}