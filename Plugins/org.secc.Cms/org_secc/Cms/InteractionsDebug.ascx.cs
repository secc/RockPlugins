// <copyright>
// Copyright Southeast Christian Church

//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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
using System.Data;
using System.Linq;
using org.secc.Warehouse.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Cms
{
    /// <summary>
    /// Block for Interactions Modal
    /// </summary>
    [DisplayName( "InteractionsDebug" )]
    [Category( "SECC > CMS" )]
    [Description( "A block to debug interactions plugin." )]

    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Member Defined Values", "", true )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Attendee Defined Values", "", true )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Prospect Defined Values", "", true )]
    [DataViewField( "Staff DataView", "", true, "Rock.Model.Person" )]

    public partial class Interactions : RockBlock
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties



        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
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
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {

        }

        #endregion

        #region Methods

        protected void btnInteractionsDebug_Click( object sender, EventArgs e )
        {
            var rockContext = new RockContext();

            InteractionService interactionService = new InteractionService( rockContext );

            DateTime? lastRun = new DateTime( 2020, 7, 21, 0, 0, 0 );

            //Get first interaction datetime if the job has never successfully run.
            if ( !lastRun.HasValue )
            {
                lastRun = interactionService.Queryable()
                      .OrderBy( i => i.InteractionDateTime )
                      .Select( i => i.InteractionDateTime )
                      .FirstOrDefault();
            }

            if ( !lastRun.HasValue )
            {
                return;
            }

            while ( lastRun.Value.Date < Rock.RockDateTime.Today.AddDays( 1 ) )
            {
                WarehouseInteractionForDate( lastRun.Value.Date );
                lastRun = lastRun.Value.Date.AddDays( 1 );
            }
        }

        private void WarehouseInteractionForDate( DateTime dayOf )
        {
            var rockContext = new RockContext();
            InteractionService interactionService = new InteractionService( rockContext );
            InteractionComponentService interactionComponentService = new InteractionComponentService( rockContext );

            var pageEntityTypeId = EntityTypeCache.GetId( typeof( Page ) );

            var interactionComponentsQry = interactionComponentService.Queryable()
                .Where( ic => ic.InteractionChannel.ComponentEntityTypeId == pageEntityTypeId && ic.EntityId != null && ic.EntityId != 0 );

            var debug = interactionComponentsQry.Where( c => c.EntityId == 0 || c.EntityId == null ).ToList();

            var nextDay = dayOf.AddDays( 1 );

            var interactionsQry = interactionService.Queryable()
                .Where( i => i.InteractionDateTime >= dayOf && i.InteractionDateTime < nextDay );

            var debug2 = interactionsQry.ToList();

            var dataViewAttributeGuid = GetAttributeValue( "StaffDataView" ).AsGuid();
            var dataViewService = new DataViewService( rockContext );
            var staffIds = new List<int>();
            if ( dataViewAttributeGuid != Guid.Empty )
            {
                var dataView = dataViewService.Get( dataViewAttributeGuid );
                if ( dataView != null )
                {
                    var errors = new List<string>();
                    var qry = dataView.GetQuery( new DataViewGetQueryArgs() );
                    if ( qry != null )
                    {
                        staffIds = qry.Select( e => e.Id ).ToList();
                    }
                }
            }

            var memberDefinedValues = GetAttributeValue( "MemberDefinedValues" ).SplitDelimitedValues();
            var memberDefinedValueGuids = memberDefinedValues.Select( Guid.Parse ).ToList();
            var memberDefinedValueQry = new DefinedValueService( rockContext ).Queryable().Where( a => memberDefinedValueGuids.Contains( a.Guid ) );
            var memberDefinedValueIds = memberDefinedValueQry.Select( s => s.Id ).ToList();

            var attendeeDefinedValues = GetAttributeValue( "AttendeeDefinedValues" ).SplitDelimitedValues();
            var attendeeDefinedValueGuids = memberDefinedValues.Select( Guid.Parse ).ToList();
            var attendeeDefinedValueQry = new DefinedValueService( rockContext ).Queryable().Where( a => attendeeDefinedValueGuids.Contains( a.Guid ) );
            var attendeeDefinedValueIds = attendeeDefinedValueQry.Select( s => s.Id ).ToList();

            var interactionCounts = interactionComponentsQry
                .GroupJoin( interactionsQry,
                    ic => ic.Id,
                    i => i.InteractionComponentId,
                    ( ic, i ) => new
                    {
                        PageId = ic.EntityId,
                        Total = i.Count(),
                        StaffVisitors = i.Where( i2 => i2.PersonAlias != null && staffIds.Contains( i2.PersonAlias.PersonId ) ).Select( i2 => i2.PersonAlias.PersonId ).Distinct().Count(),
                        MemberVisitors = i.Where( i2 => i2.PersonAlias != null && memberDefinedValueIds.Contains( i2.PersonAlias.Person.ConnectionStatusValueId ?? 0 ) && !staffIds.Contains( i2.PersonAlias.PersonId ) ).Select( i2 => i2.PersonAlias.PersonId ).Distinct().Count(),
                        AttendeeVisitors = i.Where( i2 => i2.PersonAlias != null && attendeeDefinedValueIds.Contains( i2.PersonAlias.Person.ConnectionStatusValueId ?? 0 ) && !staffIds.Contains( i2.PersonAlias.PersonId ) ).Select( i2 => i2.PersonAlias.PersonId ).Distinct().Count(),
                        ProspectVisitors = i.Where( i2 => i2.PersonAlias != null && !staffIds.Contains( i2.PersonAlias.PersonId ) && !memberDefinedValueIds.Contains( i2.PersonAlias.PersonId ) && !attendeeDefinedValueIds.Contains( i2.PersonAlias.PersonId ) ).Select( i2 => i2.PersonAlias.PersonId ).Distinct().Count(),
                        AnonymousVisitors = i.Where( i2 => i2.PersonAlias == null ).Select( i2 => i2.InteractionSessionId ).Distinct().Count()
                    }
                )
                .ToList();

            var pageIds = interactionCounts.Select( p => p.PageId ).Distinct();

            foreach ( var pageId in pageIds )
            {
                if ( pageId == null )
                {
                    continue;
                }

                var loopRockContext = new RockContext();
                DailyInteractionService dailyInteractionService = new DailyInteractionService( loopRockContext );

                var dailyInteraction = new DailyInteraction
                {
                    Date = dayOf,
                    Visits = interactionCounts.Where( v => v.PageId == pageId ).Sum( v => v.Total ),
                    PageId = pageId.Value,
                    StaffVisitors = interactionCounts.Where( v => v.PageId == pageId ).Sum( v => v.StaffVisitors ),
                    MemberVisitors = interactionCounts.Where( v => v.PageId == pageId ).Sum( v => v.MemberVisitors ),
                    AttendeeVisitors = interactionCounts.Where( v => v.PageId == pageId ).Sum( v => v.AttendeeVisitors ),
                    ProspectVisitors = interactionCounts.Where( v => v.PageId == pageId ).Sum( v => v.ProspectVisitors ),
                    AnonymousVisitors = interactionCounts.Where( v => v.PageId == pageId ).Sum( v => v.AnonymousVisitors )
                };

                dailyInteractionService.Add( dailyInteraction );
                loopRockContext.SaveChanges();
            }
            #endregion
        }
    }
}
