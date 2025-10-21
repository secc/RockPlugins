using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using org.secc.FamilyCheckin.Cache;
using Microsoft.Ajax.Utilities;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter
{
    [Category( "SECC > Sports and Fitness" )]
    [DisplayName( "Guest Check-in" )]
    [Description( "Tool for the control center to check in guests." )]

    [WorkflowTypeField( "Guest Registration Workflow",
        Description = "The workflow type that manages guest registration.",
        IsRequired = true,
        Order = 0,
        Key = "RegistrationWorkflow" )]
    [TextField( "Workflow Status",
        Description = "The workflow status for pending guest registrations.",
        IsRequired = false,
        DefaultValue = "Pending",
        Order = 1,
        Key = "WorkflowStatus" )]
    [TextField( "Cancel Checkin Activity Type Name",
        Description = "Workflow Activity Type for the Cancel Activity.",
        IsRequired = true,
        Order = 2,
        Key = "WorkflowCancelActivity" )]
    [TextField( "Checkin Activity Type Name",
        Description = "Workflow Activity Type to Checkin guest.",
        IsRequired = true,
        Order = 3,
        Key = "WorkflowCheckinActivity" )]
    [GroupTypeField( "Checkin Group Type",
        Description = "The Check-in Area Group Type",
        IsRequired = true,
        Order = 4,
        Key = "CheckinAreaGroupType" )]
    [IntegerField("Maximum Guests",
        Description = "The maximum number of guests that a host can have.",
        IsRequired = false,
        DefaultIntegerValue = 2,
        Order = 5)]


    public partial class GuestCheckin : RockBlock
    {

        int maximumGuests = 0;
        #region Block Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            BlockUpdated += guestCheckin_BlockUpdated;
            gPendingCheckins.RowCommand += gPendingCheckins_RowCommand;
            ddlLocation.SelectedIndexChanged += ddlLocation_SelectedIndexChanged;
            lbReturnToGuest.Click += lbReturnToGuest_Click;
            gHosts.RowCommand += gHosts_RowCommand;
            gHosts.RowDataBound += gHosts_RowDataBound;

            gPendingCheckins.ItemType = "Guests";
            gPendingCheckins.EmptyDataText = "No Pending Guests.";

        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            nbCheckinMessage.Visible = false;
            maximumGuests = GetAttributeValue("MaximumGuests").AsInteger();
            if (!Page.IsPostBack)
            {
                LoadPendingCheckins();
            }
            else
            {

                HandleCustomPostBack();
            }
        }
        #endregion

        #region Events
        private void guestCheckin_BlockUpdated( object sender, EventArgs e )
        {
            LoadPendingCheckins();
        }

        private void ddlLocation_SelectedIndexChanged( object sender, EventArgs e )
        {
            LoadHosts();
        }

        private void gHosts_RowCommand( object sender, GridViewCommandEventArgs e )
        {
            var attendanceId = e.CommandArgument.ToString().AsInteger();

            if (attendanceId <= 0)
            {
                return;
            }

            switch (e.CommandName.ToLower())
            {
                case "selecthost":
                    var attendance = LoadAttendance( attendanceId );
                    CheckinGuest( hfWorkflowGuid.Value.AsGuidOrNull(), attendance );
                    break;
                     
                default:
                    break;
            }
        }

        private void gHosts_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            var dataItem = e.Row.DataItem as SportsAndFitnessHost;

            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var lbSelect = e.Row.FindControl("lbSelect") as LinkButton;
                var hlMax = e.Row.FindControl("hlMaxGuests") as HighlightLabel;
                bool canHaveMoreGuests = dataItem.GuestCount < maximumGuests;

                lbSelect.Visible = canHaveMoreGuests;
                hlMax.Visible = !canHaveMoreGuests;
            }
        }

        private void gPendingCheckins_RowCommand( object sender, GridViewCommandEventArgs e )
        {
            var workflowGuid = e.CommandArgument.ToString().AsGuidOrNull();
            var commandName = e.CommandName.ToLower();

            switch (commandName)
            {
                case "checkin":
                    LoadHostGuestFields( workflowGuid );
                    LoadHosts();
                    break;
                case "cancelcheckin":
                    CancelGuestCheckin( workflowGuid );
                    break;

                default:
                    break;
            }
        }

        private void lbReturnToGuest_Click( object sender, EventArgs e )
        {
            ClearHostGuestFields();
            LoadPendingCheckins();
        }
        #endregion

        #region Private Methods


        private void CancelGuestCheckin( Guid? workflowGuid )
        {
            if(!workflowGuid.HasValue)
            {
                return;
            }

            var activityTypeName = GetAttributeValue( "WorkflowCancelActivity" );
            using (var rockContext = new RockContext())
            {
                var workflowService = new WorkflowService( rockContext );
                var checkinWorkflow = workflowService.Get( workflowGuid.Value );

                var activityType = WorkflowTypeCache.Get( checkinWorkflow.WorkflowTypeId )
                    .ActivityTypes.Where( a => a.Name.Equals( activityTypeName, StringComparison.InvariantCultureIgnoreCase ) )
                    .FirstOrDefault();

                if(activityType == null)
                {
                    throw new Exception( $"{activityTypeName} was not found." );
                }

                var errors = new List<string>();
                WorkflowActivity.Activate( activityType, checkinWorkflow, rockContext );
                workflowService.Process( checkinWorkflow, out errors );

                rockContext.SaveChanges();

            }

            LoadPendingCheckins();
        }
        private void CheckinGuest( Guid? workflowGuid, Attendance attend )
        {

            if (!workflowGuid.HasValue)
            {
                return;
            }
            var activityTypeName = GetAttributeValue( "WorkflowCheckinActivity" );
            using (var rockContext = new RockContext())
            {
                var workflowService = new WorkflowService( rockContext );
                var checkinWorkflow = workflowService.Get( workflowGuid.Value );
                checkinWorkflow.LoadAttributes( rockContext );
                checkinWorkflow.SetAttributeValue( "HostAttendanceId", attend.Id );
                checkinWorkflow.SetAttributeValue( "Host", attend.PersonAlias.Guid );
                checkinWorkflow.SaveAttributeValues( rockContext );

                rockContext.SaveChanges();


                var activityType = WorkflowTypeCache.Get( checkinWorkflow.WorkflowTypeId )
                    .ActivityTypes.Where( a => a.Name.Equals( activityTypeName, StringComparison.InvariantCultureIgnoreCase ) )
                    .FirstOrDefault();

                if (activityType == null)
                    throw new Exception( $"{activityTypeName} was not found." );

                var errors = new List<string>();
                WorkflowActivity.Activate( activityType, checkinWorkflow, rockContext );
                bool checkinSuccess = workflowService.Process( checkinWorkflow, out errors );

                //rockContext.SaveChanges();
                
                checkinWorkflow.LoadAttributes( rockContext );
                var attendanceId = checkinWorkflow.GetAttributeValue( "AttendanceId" ).AsInteger();

                if (attendanceId > 0)
                {
                    var attendance = new AttendanceService( rockContext ).Get( attendanceId );
                    AttendanceCache.AddOrUpdate( attendance );
                }

                LoadPendingCheckins();

                var guest = new PersonAliasService( rockContext ).GetPerson( checkinWorkflow.GetAttributeValue( "Guest" ).AsGuid() );

                if (checkinSuccess)
                {
                    string message = $"{guest.FullName} has successfully been checked in.";
                    nbCheckinMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Success;
                    nbCheckinMessage.Text = message;
                    nbCheckinMessage.Visible = true;
                }
                else
                {
                    string message = $"There was an error checking in {guest.FullName} please try again.";
                    nbCheckinMessage.NotificationBoxType = Rock.Web.UI.Controls.NotificationBoxType.Warning;
                    nbCheckinMessage.Text = message;
                    nbCheckinMessage.Visible = true;
                }
            }
        }

        private void ClearHostGuestFields()
        {
            hfWorkflowGuid.Value = string.Empty;
            lSelectHostHeader.Text = string.Empty;
        }

        private void HandleCustomPostBack()
        {
            var args = this.Request.Params["__EVENTARGUMENT"];
            if(args == "filterByName" && tbNameSearch.Text.Length != 1)
            {
                LoadHosts();
            }
        }

        private Attendance LoadAttendance(int id)
        {
            using (var rockContext = new RockContext())
            {
                var attendanceService = new AttendanceService(rockContext);
                return attendanceService.Queryable()
                    .Include( a => a.Occurrence )
                    .Include( a => a.PersonAlias.Person )
                    .Where( a => a.Id == id )
                    .FirstOrDefault();
            }
        }

        private void LoadHostGuestFields( Guid? workflowGuid)
        {
            if (!workflowGuid.HasValue)
                return;

            using (var context = new RockContext())
            {
                var workflow = new WorkflowService( context ).Get( workflowGuid.Value );
                hfWorkflowGuid.Value = workflowGuid.ToString();
                workflow.LoadAttributes( context );
                var guest = new PersonAliasService( context ).GetPerson( workflow.GetAttributeValue( "Guest" ).AsGuid() );
                lSelectHostHeader.Text = $"Select Host for {guest.FullName}";
            }
        }

        private void LoadHosts()
        {
            var currentTime = RockDateTime.Now;
            var areaGroupType = GroupTypeCache.Get( GetAttributeValue( "CheckinAreaGroupType" ).AsGuid() );
            var workflowType = WorkflowTypeCache.Get(GetAttributeValue("RegistrationWorkflow").AsGuid());
            using (var rockContext = new RockContext())
            {
                var occurrenceService = new AttendanceOccurrenceService( rockContext );
                var groups = new GroupService( rockContext ).Queryable( "GroupLocations.Location" ).AsNoTracking()
                    .Where( g => g.GroupTypeId == areaGroupType.Id )
                    .Where( g => g.IsActive == true )
                    .Where( g => g.IsArchived == false )
                    .SelectMany( g => g.GroupLocations,
                        ( group, groupLocations ) => new
                        {
                            GroupId = group.Id,
                            GroupName = group.Name,
                            GroupLocationId = groupLocations.Id,
                            LocationId = groupLocations.LocationId,
                            LocationName = groupLocations.Location.Name,
                            Schedules = groupLocations.Schedules
                        } )
                    .ToList();

                var occurrenceIds = new List<int>();
                foreach (var item in groups)
                {
                    foreach (var schedule in item.Schedules)
                    {
                        if (schedule.IsCheckInActive)
                        {
                            var occurrence = occurrenceService.Get( currentTime, item.GroupId, item.LocationId, schedule.Id );
                            if (occurrence != null)
                            {
                                occurrenceIds.Add( occurrence.Id );
                            }
                        }
                    }
                }

                var workflowEntityType = EntityTypeCache.GetId(typeof(Workflow));
                var hostAttribute = AttributeCache.GetByEntityTypeQualifier(workflowEntityType.Value, "WorkflowTypeId", workflowType.Id.ToString(), false)
                    .Where(a => a.Key == "Host")
                    .FirstOrDefault();

                var attributeValueQry = new AttributeValueService(rockContext).Queryable().AsNoTracking()
                    .Where(av => av.AttributeId == hostAttribute.Id)
                    .Where(av => av.ValueAsPersonId.HasValue);

                var today = currentTime.Date;
                var hostsGuests = new WorkflowService(rockContext).Queryable().AsNoTracking()
                    .Where(w => w.WorkflowTypeId == workflowType.Id)
                    .Where(w => w.ActivatedDateTime.HasValue && w.ActivatedDateTime.Value > today )
                    .Join(attributeValueQry, w => w.Id, av => av.EntityId, (w, av) => new { WorkflowId = w.Id, PersonId = av.ValueAsPersonId })
                    .GroupBy(h => h.PersonId)
                    .Select(h => new { PersonId = h.Key, GuestCount = h.Count() });
                   

                var attendanceService = new AttendanceService( rockContext );

                var hostsQry = attendanceService.Queryable()
                    .GroupJoin(hostsGuests, a => a.PersonAlias.PersonId, h => h.PersonId,
                        (a, h) => new {Attendance = a, GuestCount = h.Select(h1 => h1.GuestCount).DefaultIfEmpty()})
                    .Where( a => occurrenceIds.Contains( a.Attendance.OccurrenceId ) )
                    .Where( a => a.Attendance.DidAttend == true )
                    .Where( a => a.Attendance.EndDateTime == null )
                    .Where( a => a.Attendance.Note == null || !a.Attendance.Note.StartsWith( "Guest" ) )
                    .Select( a => new SportsAndFitnessHost
                    {
                        AttendanceId = a.Attendance.Id,
                        PersonId = a.Attendance.PersonAlias.PersonId,
                        Host = a.Attendance.PersonAlias.Person,
                        LocationId = a.Attendance.Occurrence.LocationId.Value,
                        Location = a.Attendance.Occurrence.Location.Name,
                        CheckinTime = a.Attendance.StartDateTime,
                        GuestCount = a.GuestCount.FirstOrDefault()
                    } );

                var selectedLocationId = 0;
                if (ddlLocation.SelectedIndex > 0)
                {
                    selectedLocationId = ddlLocation.SelectedValueAsInt().Value;
                    hostsQry = hostsQry.Where( h => h.LocationId == selectedLocationId );
                }

                if(!String.IsNullOrWhiteSpace(tbNameSearch.Text))
                {
                    hostsQry = hostsQry
                        .Where( h => h.Host.LastName.StartsWith( tbNameSearch.Text ) || h.Host.NickName.StartsWith( tbNameSearch.Text ) );
                }


                var locations = groups.Select( h => new
                {
                    LocationId = h.LocationId,
                    Location = h.LocationName
                } )
                .Distinct()
                .OrderBy( h => h.Location )
                .ToList();

                ddlLocation.DataSource = locations;
                ddlLocation.DataValueField = "LocationId";
                ddlLocation.DataTextField = "Location";
                ddlLocation.DataBind();

                ddlLocation.Items.Insert( 0, new ListItem( "", "" ) );
                if (selectedLocationId > 0)
                {
                    var item = ddlLocation.Items.FindByValue( selectedLocationId.ToString() );
                    item.Selected = true;
                }

                gHosts.DataSource = hostsQry
                    .OrderBy( h => h.Host.LastName )
                    .ThenBy( h => h.Host.NickName )
                    .ToList();
                gHosts.DataBind();

                pnlMain.Visible = false;
                pnlSelectHost.Visible = true;

            }
        }

        private void LoadPendingCheckins()
        {
            pnlMain.Visible = false;
            pnlSelectHost.Visible = false;

            var today = RockDateTime.Today;
            var workflowType = WorkflowTypeCache.Get( GetAttributeValue( "RegistrationWorkflow" ).AsGuid() );

            if (workflowType == null)
            {
                return;
            }

            var workflowStatus = GetAttributeValue( "WorkflowStatus" );
            var workflowEntityType = EntityTypeCache.Get( typeof( Workflow ) );
            var workflowTypeIdAsString = workflowType.Id.ToString();
            using (var rockContext = new RockContext())
            {
                var workflowService = new WorkflowService( rockContext );
                var workflowQry = workflowService.Queryable().AsNoTracking()
                    .Where( w => w.WorkflowTypeId == workflowType.Id )
                    .Where( w => w.CreatedDateTime > today )
                    .Where( w => w.Status == workflowStatus );



                var attributeValueService = new AttributeValueService( rockContext );
                var attributekeys = "Guest,EmergencyContactCount,WaiverAcceptedDate";
                var attributeQry = attributeValueService.Queryable().AsNoTracking()
                    .Where( v => v.Attribute.EntityTypeId == workflowEntityType.Id )
                    .Where( v => v.Attribute.EntityTypeQualifierColumn == "WorkflowTypeId" )
                    .Where( v => v.Attribute.EntityTypeQualifierValue == workflowTypeIdAsString )
                    .Where( v => attributekeys.Contains( v.Attribute.Key ) );


                var qry = workflowQry
                    .GroupJoin( attributeQry, w => w.Id, a => a.EntityId,
                        ( w, a ) => new { Workflow = w, Attributes = a.DefaultIfEmpty() } )
                    .Select( w => new
                    {
                        Id = w.Workflow.Id,
                        WorkflowGuid = w.Workflow.Guid,
                        CreatedDateTime = w.Workflow.CreatedDateTime,
                        Status = w.Workflow.Status,
                        GuestPersonId = w.Attributes.Where( a => a.Attribute.Key == "Guest" ).Select( g => g.ValueAsPersonId ).FirstOrDefault(),
                        WaiverAcceptedDate = w.Attributes.Where( a => a.Attribute.Key == "WaiverAcceptedDate" ).Select( g => g.ValueAsDateTime ).FirstOrDefault(),
                        EmergencyContactCount = w.Attributes.Where( a => a.Attribute.Key == "EmergencyContactCount" ).Select( g => g.ValueAsNumeric ).FirstOrDefault()
                    } );

                var guests = new List<SportsAndFitnessGuest>();
                foreach (var item in qry.ToList())
                {
                    guests.Add( new SportsAndFitnessGuest
                    {
                        WorkflowId = item.Id,
                        WorkflowGuid = item.WorkflowGuid,
                        CreatedDateTime = item.CreatedDateTime.Value,
                        Status = item.Status,
                        GuestPersonId = item.GuestPersonId,
                        HasSignedWaiver = item.WaiverAcceptedDate.HasValue,
                        HasEmergencyContacts = item.EmergencyContactCount.HasValue && item.EmergencyContactCount.Value > 0
                    } );
                }

                gPendingCheckins.DataSource = guests;
                gPendingCheckins.DataBind();
                pnlMain.Visible = true;
            }
        }


        public class SportsAndFitnessGuest
        {
            private Person _guest = null;
            private Person _host = null;

            public int WorkflowId { get; set; }
            public Guid WorkflowGuid { get; set; }
            public DateTime CreatedDateTime { get; set; }
            public string Status { get; set; }
            public int? GuestPersonId { get; set; }
            public int? HostPersonId { get; set; }
            public DateTime? CheckinTime { get; set; }
            public int? AttendanceId { get; set; }
            public bool HasEmergencyContacts { get; set; }
            public bool HasSignedWaiver { get; set; }
            public Person Guest
            {
                get
                {
                    if (_guest == null)
                    {
                        _guest = LoadPerson( GuestPersonId );
                    }
                    return _guest;

                }
            }

            public Person Host
            {
                get
                {
                    if (_host == null)
                    {
                        _host = LoadPerson( HostPersonId );
                    }

                    return _host;
                }
            }



            private Person LoadPerson( int? personId )
            {
                if (!personId.HasValue || personId <= 0)
                {
                    return null;
                }

                using (var rockContext = new RockContext())
                {
                    return new PersonService( rockContext ).Get( personId.Value );
                }
            }
        }

        public class SportsAndFitnessHost
        {
            public int AttendanceId { get; set; }
            public int PersonId { get; set; }
            public Person Host { get; set; }
            public int LocationId { get; set; }
            public string Location { get; set; }
            public DateTime? CheckinTime { get; set; }
            public int? GuestCount { get; set; }


        }

        #endregion
    }
}