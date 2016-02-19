// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using System.Web;

namespace RockWeb.Plugins.org_secc.RoomRatioManager
{
    [DisplayName("Room Ratio Manager")]
    [Category("Check-in")]
    [Description("Helps manage rooms and room ratios.")]


    public partial class RoomRatio : RockBlock
    {
        private CampusCache _currentCampus;

        #region Control Methods


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
                LoadDropdowns();
                BindData();

            }else
            {
                BindData();
            }

        }

        private void BindData()
        {
            List<RoomAttendance> roomAttendances = new List<RoomAttendance>();
            var locations = GetChildLocations();
            var attendances = GetAttendanceData();
            if(locations!=null)
            {
                foreach (var location in locations)
                {
                    RoomAttendance roomAttendance = new RoomAttendance();
                    roomAttendance.Name = location.Name;
                    roomAttendance.Id = location.Id;
                    roomAttendance.Count = attendances.Where(a => a.LocationId == location.Id).Count();
                    var ratio = location.GetAttributeValue("RoomRatio");
                    if (ratio.Length == 0) ratio = "0";
                    roomAttendance.Ratio = ratio+":1";
                    roomAttendance.CalculateRatio(attendances.Where(a => a.LocationId == location.Id).ToList());
                    roomAttendances.Add(roomAttendance);
                    roomAttendance.IsActive = location.IsActive;
                }

            gLocations.DataSource = roomAttendances;
            gLocations.DataBind();
            }
        }

        #endregion

        protected void LoadDropdowns()
        {
            var campusIdString = Request.QueryString["campusId"];
            if (campusIdString != null)
            {
                _currentCampus = CampusCache.Read(Int32.Parse(campusIdString));
                var mergeObjects = new Dictionary<string, object>();
                lCurrentSelection.Text = _currentCampus.Name;
            }
            else
            {
                lCurrentSelection.Text = "Select Campus";
            }

            var campusList = CampusCache.All()
                .Select(a => new CampusItem { Name = a.Name, Id = a.Id })
                .ToList();

            // run lava on each campus
            string dropdownItemTemplate = GetAttributeValue("DropdownItemTemplate");
            if (!string.IsNullOrWhiteSpace(dropdownItemTemplate))
            {
                foreach (var campus in campusList)
                {
                    var mergeObjects = new Dictionary<string, object>();
                    mergeObjects.Add("CampusName", campus.Name);
                    campus.Name = dropdownItemTemplate.ResolveMergeFields(mergeObjects);
                }
            }


            // check if the campus can be unselected
            if (!string.IsNullOrEmpty(GetAttributeValue("ClearSelectionText")))
            {
                var blankCampus = new CampusItem
                {
                    Name = GetAttributeValue("ClearSelectionText"),
                    Id = Rock.Constants.All.Id
                };

                campusList.Insert(0, blankCampus);
            }

            rptCampuses.DataSource = campusList;
            
            rptCampuses.DataBind();
        }


        protected void rptCampuses_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            var campusId = e.CommandArgument.ToString();

            if (campusId != null)
            {
                var queryString = HttpUtility.ParseQueryString(Request.QueryString.ToStringSafe());
                queryString.Set("campusId", campusId.ToString());
                Response.Redirect(string.Format("{0}?{1}", Request.Url.AbsolutePath, queryString), false);
            }
        }

        private List<Location> GetChildLocations()
        {
            if (_currentCampus == null)
            {
                return null;
            }
            var locationService = new LocationService(new RockContext());
            var locations = locationService.Queryable()
                .Where(l =>
                l.ParentLocationId == _currentCampus.LocationId
                && l.LocationTypeValue.Guid.ToString() == Rock.SystemGuid.DefinedValue.LOCATION_TYPE_ROOM)
                .OrderBy(l => l.Name).ToList();
            foreach (var location in locations)
            {
                location.LoadAttributes();
            }
            return locations;
        }

        private List<Attendance> GetAttendanceData()
        {
            return new AttendanceService(new RockContext()).Queryable()
                        .Where(a =>
                           a.ScheduleId.HasValue &&
                           a.GroupId.HasValue &&
                           a.LocationId.HasValue &&
                           a.StartDateTime > RockDateTime.Today &&
                           a.StartDateTime < RockDateTime.Now &&
                           a.DidAttend.HasValue &&
                           a.DidAttend.Value)
                        .ToList();
        }

        protected void gLocations_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (Convert.ToBoolean(DataBinder.Eval(e.Row.DataItem, "IsFull")) == true)
            {
                e.Row.AddCssClass("closed");
            }
        }

        protected void IsFull_SetBadgeType(object sender, BadgeRowEventArgs e)
        {
            string isFull = (string)e.FieldValue;
            if (isFull=="Full")
            {
                e.BadgeType = "Danger";
            }
            else
            {
                e.BadgeType = "Info";
            }
        }

        protected void IsActive_CheckedChanged(object sender, RowEventArgs e)
        {
            RockContext rockContext = new RockContext();
            var locationId = (int)e.RowKeyId;
            LocationService locationService = new LocationService(rockContext);
            var location = locationService.Queryable().Where(l => l.Id == locationId).FirstOrDefault();
            if (location.IsActive)
            {
                location.IsActive = false;
            }
            else
            {
                location.IsActive = true;
            }
            rockContext.SaveChanges();
        }
    }

    public class RoomAttendance
    {
        public bool IsActive {get; set;}
        public string IsFull { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public string Ratio { get; set; }
        public string CurrentRatio { get; set; }
        public int Count { get; set; }
        public int ChildCount { get; set; }
        public int AdultCount { get; set; }

        internal void CalculateRatio(List<Attendance> attendances)
        {
            ChildCount = 0;
            AdultCount = 0;

            foreach (var attendance in attendances)
            {
                int personId = (int)attendance.PersonAliasId;
                PersonAliasService personAliasService = new PersonAliasService(new RockContext());
                var personAlias = personAliasService.GetByAliasId(personId);
                var person = personAliasService.GetPerson(personAlias.Guid);
                //Choose if adult or child by age
                if (person.Age>17)
                {
                    AdultCount++;
                }
                else
                {
                    ChildCount++;
                }
            }
            CurrentRatio = GetRatio(ChildCount, AdultCount);
            if ((AdultCount*(Int32.Parse(Ratio.Split(':')[0]))) <= ChildCount || AdultCount == 0){
                IsFull = "Full";
            }
            else
            {
                IsFull = "Not Full";
            }
            
        }
        private static int Gcd(int a, int b)
        {
            if (a == 0)
                return b;
            else
                return Gcd(b % a, a);
        }

        private static string GetRatio(int a, int b)
        {
            int gcd = Gcd(a, b);
            if (gcd == 0)
            {
                return "0:0";
            }
            else
            { 
            return (a / gcd).ToString() + ":" + (b / gcd).ToString();
            }
        }
    }

    public class CampusItem
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; set; }
    }

}