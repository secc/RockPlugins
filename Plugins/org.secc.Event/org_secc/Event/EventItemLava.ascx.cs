﻿// <copyright>
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
using System.Data.Entity;
using System.Linq;
using System.Web.UI;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Event
{
    /// <summary>
    /// Renders a particular calendar event item using Lava.
    /// </summary>
    [DisplayName( "Calendar Event Item Lava" )]
    [Category( "SECC > Event" )]
    [Description( "Renders a particular calendar event item using Lava." )]
    [LinkedPage( "Registration Page", "Registration page for events", order: 1 )]
    [EventItemField( "EventItem", "", required: false )]
    [CodeEditorField( "Lava Template", "Lava template to use to display the list of events.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"{% include '~~/Assets/Lava/CalendarItem.lava' %}", "", 2 )]
    [BooleanField( "Set Page Title", "Determines if the block should set the page title with the calendar item name.", false, order: 3 )]
    [AttributeField( "E37FB26F-03F6-48DA-8E96-F412616F5EE4", "URL Slug Attribute", "The attribute on the calendar item which contains the URL Slug.", false, order: 4 )]
    [CodeEditorField( "Event Not Found", "Lava template to use to display when no event is found.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"<h1>We are sorry, we couldn't find the event you are looking for.</h2>", "", 6 )]
    public partial class EventItemLava : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                DisplayDetails();
            }
        }

        /// <summary>
        /// Returns breadcrumbs specific to the block that should be added to navigation
        /// based on the current page reference.  This function is called during the page's
        /// oninit to load any initial breadcrumbs.
        /// </summary>
        /// <param name="pageReference">The <see cref="Rock.Web.PageReference" />.</param>
        /// <returns>
        /// A <see cref="System.Collections.Generic.List{BreadCrumb}" /> of block related <see cref="Rock.Web.UI.BreadCrumb">BreadCrumbs</see>.
        /// </returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int? eventCampusId = PageParameter( pageReference, "EventItemId" ).AsIntegerOrNull();
            if ( eventCampusId != null )
            {
                EventItem eventItem = new EventItemService( new RockContext() ).Get( eventCampusId.Value );
                if ( eventItem != null )
                {
                    breadCrumbs.Add( new BreadCrumb( eventItem.Name, pageReference ) );
                }
            }
            else
            {
                // don't show a breadcrumb if we don't have a pageparam to work with
            }

            return breadCrumbs;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            DisplayDetails();
        }

        #endregion

        #region Internal Methods

        private void DisplayDetails()
        {
            RockContext rockContext = new RockContext();

            EventItemService eventItemService = new EventItemService( rockContext );
            var qry = eventItemService
                .Queryable();

            // get the eventItem id if the event item is set via block attribute
            var eventItemAttGuid = GetAttributeValue( "EventItem" ).AsGuid();
            int eventItemId = qry.Where( i => i.Guid == eventItemAttGuid ).Select( i => i.Id ).FirstOrDefault();

            // get the eventItem id if the event item block attribute isn't set
            if ( eventItemId == 0 && !string.IsNullOrWhiteSpace( PageParameter( "EventItemId" ) ) )
            {
                eventItemId = Convert.ToInt32( PageParameter( "EventItemId" ) );
            }
            if ( eventItemId > 0 )
            {
                /*var qry = eventItemService
                    .Queryable()
                    ;*/
                qry = qry.Where( i => i.Id == eventItemId );
            }
            else
            {
                // Get the Slug Attribute
                var slugAttribute = AttributeCache.Get( GetAttributeValue( "URLSlugAttribute" ).AsGuid() );

                // get the slug
                if ( !string.IsNullOrWhiteSpace( PageParameter( "URLSlug" ) ) && slugAttribute != null )
                {
                    int slugAttributeId = slugAttribute.Id;
                    EventCalendarItemService eventCalendarItemService = new EventCalendarItemService( rockContext );
                    AttributeValueService attributeValueService = new AttributeValueService( rockContext );

                    var tmQry = qry.Join( eventCalendarItemService.Queryable(),
                        ei => ei.Id,
                        aci => aci.EventItemId,
                        ( ei, aci ) => new { EventItem = ei, EventCalendarItem = aci }
                    )
                    .Join( attributeValueService.Queryable(),
                        ei => new { Id = ei.EventCalendarItem.Id, AttributeId = slugAttributeId },
                        av => new { Id = av.EntityId ?? 0, AttributeId = av.AttributeId },
                        ( ei, av ) => new { EventItem = ei.EventItem, EventCalendarItem = ei.EventCalendarItem, Slug = av } );

                    string urlSlug = PageParameter( "URLSlug" );

                    tmQry = tmQry.Where( obj => obj.Slug.Value.Contains( urlSlug ) );

                    // The page parameter could contain something like 'camp' while the slug value list contains 'camp-freedom' so we need to double-check
                    // to make sure we have an exact match
                    qry = tmQry.ToList().AsQueryable().Where( obj => obj.Slug.Value.ToLower().Split( '|' ).Contains( urlSlug.ToLower() ) ).Select( obj => obj.EventItem );
                }
                else
                {
                    // If we don't have an eventItemId or slug we shouldn't get the first item in the database.  That would be . . . not good
                    qry = null;
                }
            }

            if ( qry != null )
            {
                var eventItem = qry.FirstOrDefault();

                if ( eventItem != null )
                {
                    // removing any occurrences that don't have a start time in the next twelve months
                    var occurrenceList = eventItem.EventItemOccurrences.ToList();
                    occurrenceList.RemoveAll( o => o.GetStartTimes( RockDateTime.Now, RockDateTime.Now.AddYears( 1 ) ).Count() == 0 );

                    //Check for Campus Id Parameter
                    var campusId = PageParameter( "CampusId" ).AsIntegerOrNull();
                    if ( campusId.HasValue )
                    {
                        //check if there's a campus with this id.
                        var campus = CampusCache.Get( campusId.Value );
                        if ( campus != null )
                        {
                            occurrenceList.RemoveAll( o => o.CampusId != null && o.CampusId != campus.Id );
                        }
                    }

                    //Check for Campus
                    var campusStr = PageParameter( "Campus" );
                    if ( !string.IsNullOrEmpty( campusStr ) )
                    {
                        //check if there's a campus with this name.
                        var campus = CampusCache.All().Where( c => c.Name.ToLower().RemoveSpaces() == campusStr.ToLower().RemoveSpaces() ).FirstOrDefault();
                        if ( campus != null )
                        {
                            occurrenceList.RemoveAll( o => o.CampusId != null && o.CampusId != campus.Id );
                        }
                        else
                        {
                            // check one more time to see if there's a campus slug that matches
                            campus = CampusCache.All().Where( c => c.AttributeValues["Slug"].ToString() == campusStr.ToLower() ).FirstOrDefault();
                            if ( campus != null )
                            {
                                occurrenceList.RemoveAll( o => o.CampusId != null && o.CampusId != campus.Id );
                            }
                        }
                    }

                    eventItem.EventItemOccurrences = occurrenceList.OrderBy( o => o.NextStartDateTime.HasValue ? o.NextStartDateTime : DateTime.Now ).ToList();

                    var mergeFields = new Dictionary<string, object>();
                    mergeFields.Add( "RegistrationPage", LinkedPageRoute( "RegistrationPage" ) );

                    var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
                    var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

                    if ( contextCampus != null )
                    {
                        mergeFields.Add( "CampusContext", contextCampus );
                    }

                    // determine registration status (Register, Full, or Join Wait List) for each unique registration instance
                    Dictionary<int, string> registrationStatusLabels = new Dictionary<int, string>();
                    foreach ( var occurance in eventItem.EventItemOccurrences )
                    {

                        foreach ( var registrationInstance in occurance.Linkages.Select( a => a.RegistrationInstance ).Distinct().ToList() )
                        {
                            if ( registrationStatusLabels.ContainsKey( registrationInstance.Id ) )
                            {
                                continue;
                            }
                            int? maxRegistrantCount = 0;
                            var currentRegistrationCount = 0;

                            if ( registrationInstance != null )
                            {
                                if ( registrationInstance.MaxAttendees != 0 )
                                {
                                    maxRegistrantCount = registrationInstance.MaxAttendees;
                                }
                            }


                            int? registrationSpotsAvailable = null;
                            if ( maxRegistrantCount.HasValue && maxRegistrantCount != 0 )
                            {
                                currentRegistrationCount = new RegistrationRegistrantService( rockContext ).Queryable().AsNoTracking()
                                                                .Where( r =>
                                                                    r.Registration.RegistrationInstanceId == registrationInstance.Id
                                                                    && r.OnWaitList == false )
                                                                .Count();
                                registrationSpotsAvailable = maxRegistrantCount - currentRegistrationCount;
                            }

                            string registrationStatusLabel = "Register";

                            if ( registrationSpotsAvailable.HasValue && registrationSpotsAvailable.Value < 1 )
                            {
                                if ( registrationInstance.RegistrationTemplate.WaitListEnabled )
                                {
                                    registrationStatusLabel = "Join Wait List";
                                }
                                else
                                {
                                    registrationStatusLabel = "Full";
                                }
                            }

                            registrationStatusLabels.Add( registrationInstance.Id, registrationStatusLabel );
                        }
                    }

                    // Status of each registration instance
                    mergeFields.Add( "RegistrationStatusLabels", registrationStatusLabels );

                    mergeFields.Add( "Event", eventItem );
                    mergeFields.Add( "CurrentPerson", CurrentPerson );

                    lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );

                    if ( GetAttributeValue( "SetPageTitle" ).AsBoolean() )
                    {
                        string pageTitle = eventItem != null ? eventItem.Name : "Event";
                        RockPage.PageTitle = pageTitle;
                        RockPage.BrowserTitle = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                        RockPage.Header.Title = String.Format( "{0} | {1}", pageTitle, RockPage.Site.Name );
                    }

                }
                else
                {

                    lOutput.Text = EventNotFoundLava();
                }
            }
            else
            {
                lOutput.Text = EventNotFoundLava();
            }
        }

        private string EventNotFoundLava()
        {
            var mergeFields = new Dictionary<string, object>();
            mergeFields.Add( "RegistrationPage", LinkedPageRoute( "RegistrationPage" ) );

            var campusEntityType = EntityTypeCache.Get( "Rock.Model.Campus" );
            var contextCampus = RockPage.GetCurrentContext( campusEntityType ) as Campus;

            if ( contextCampus != null )
            {
                mergeFields.Add( "CampusContext", contextCampus );
            }
            mergeFields.Add( "CurrentPerson", CurrentPerson );

            return GetAttributeValue( "EventNotFound" ).ResolveMergeFields( mergeFields );
        }
        #endregion

    }

}
