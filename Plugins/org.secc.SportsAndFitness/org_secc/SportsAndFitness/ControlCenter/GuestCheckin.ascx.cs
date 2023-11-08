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


    public partial class GuestCheckin : RockBlock
    {
        #region Block Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            BlockUpdated += guestCheckin_BlockUpdated;
            gPendingCheckins.RowCommand += gPendingCheckins_RowCommand;
            ddlLocation.SelectedIndexChanged += ddlLocation_SelectedIndexChanged;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if (!Page.IsPostBack)
            {
                //LoadPendingCheckins();
                LoadHosts();
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

        private void gPendingCheckins_RowCommand( object sender, GridViewCommandEventArgs e )
        {
            var workflowGuid = e.CommandArgument.ToString().AsGuidOrNull();
            var commandName = e.CommandName.ToLower();

            switch (commandName)
            {
                case "checkin":
                    CheckinGuest( workflowGuid );
                    break;
                case "cancel":
                    CancelGuestCheckin( workflowGuid );
                    break;

                default:
                    break;
            }



        }
        #endregion

        #region Private Methods


        private void CancelGuestCheckin( Guid? workflowGuid )
        {
            throw new NotImplementedException();
        }
        private void CheckinGuest( Guid? workflowGuid )
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

                var activityType = WorkflowTypeCache.Get( checkinWorkflow.WorkflowTypeId )
                    .ActivityTypes.Where( a => a.Name.Equals( activityTypeName, StringComparison.InvariantCultureIgnoreCase ) )
                    .FirstOrDefault();

                if (activityType == null)
                    throw new Exception( $"{activityTypeName} was not found." );

                var errors = new List<string>();
                WorkflowActivity.Activate( activityType, checkinWorkflow, rockContext );
                if (workflowService.Process( checkinWorkflow, out errors ))
                {
                    checkinWorkflow.CompletedDateTime = null;
                }

                rockContext.SaveChanges();
            }
        }

        private void LoadHosts()
        {
            var currentTime = RockDateTime.Now;
            var areaGroupType = GroupTypeCache.Get( GetAttributeValue( "CheckinAreaGroupType" ).AsGuid() );

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

                var attendanceService = new AttendanceService( rockContext );

                var hostsQry = attendanceService.Queryable()
                    .Where( a => occurrenceIds.Contains( a.OccurrenceId ) )
                    .Where( a => a.DidAttend == true )
                    .Where( a => a.EndDateTime == null )
                    .Where( a => a.Note == null || !a.Note.StartsWith( "Guest" ) )
                    .Select( a => new SportsAndFitnessHost
                    {
                        AttendanceId = a.Id,
                        PersonId = a.PersonAlias.PersonId,
                        Host = a.PersonAlias.Person,
                        LocationId = a.Occurrence.LocationId.Value,
                        Location = a.Occurrence.Location.Name,
                        CheckinTime = a.StartDateTime
                    } );

                var selectedLocationId = 0;
                if (ddlLocation.SelectedIndex > 0)
                {
                    selectedLocationId = ddlLocation.SelectedValueAsInt().Value;
                    hostsQry = hostsQry.Where( h => h.LocationId == selectedLocationId );
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
                if(selectedLocationId > 0)
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
                var attributekeys = "Guest,EmergencyContactCount,SignatureDocumentId";
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
                        SignatureDocumentId = w.Attributes.Where( a => a.Attribute.Key == "SignatureDocumentId" ).Select( g => g.ValueAsNumeric ).FirstOrDefault(),
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
                        HasSignedWaiver = item.SignatureDocumentId.HasValue && item.SignatureDocumentId.Value > 0,
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


        }

        #endregion
    }
}