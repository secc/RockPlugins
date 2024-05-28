using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Event
{
    [DisplayName( "Family Registration List Lava" )]
    [Category( "SECC > Event" )]
    [Description( "Registration List Lava block to show children's registrations." )]

    [CodeEditorField( "Lava Template", "Lava template to use to display content", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, true, @"{% include '~~/Assets/Lava/RegistrationListSidebar.lava' %}", "", 2, "LavaTemplate" )]
    [IntegerField( "Max Results", "The maximum number of results to display.", false, 5, order: 3 )]
    [SlidingDateRangeField( "Date Range", "Date range to limit by.", false, "", enabledSlidingDateRangeTypes: "Previous, Last, Current, Next, Upcoming, DateRange", order: 7 )]
    [BooleanField( "Limit to registrations where money is still owed", "", true, "", 8, "LimitToOwed" )]
    public partial class FamilyRegistrationLava : RockBlock
    {

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upContent );
        }


        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if (!Page.IsPostBack)
            {
                LoadContent();
            }
        }
        #endregion

        #region Events
        private void Block_BlockUpdated( object sender, EventArgs e )
        {
            LoadContent();
        }
        #endregion

        private void LoadContent()
        {
            var rockContext = new RockContext();
            var childRole = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ).Roles
                .FirstOrDefault( r => r.Guid == Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() );
            var currentPersonId = CurrentPersonId ?? 0;
            var children = CurrentPerson.GetFamilyMembers( false, rockContext )
                .Where( m => m.GroupRole.Guid == childRole.Guid )
                .Select( m => m.PersonId )
                .ToList();

            var registrationService = new RegistrationService( rockContext );
            var registrationBaseQry = registrationService.Queryable()
                .Where( r => r.RegistrationInstance.IsActive )
                .Where( r => !r.IsTemporary );

            var myRegistrationQry = registrationBaseQry
                .Where( r => r.PersonAlias.PersonId == currentPersonId );

            var registeredByChildQry = registrationBaseQry
                .Where( r => children.Contains( r.PersonAlias.PersonId ) );

            var childRegistrantsQry = registrationBaseQry
                .Where( r => r.Registrants.Where( r1 => children.Contains( r1.PersonAlias.PersonId ) ).Any() );

            var registrationList = myRegistrationQry
                .Union( registeredByChildQry )
                .Union( childRegistrantsQry )
                .ToList();


            List<Registration> hasDates = registrationList.Where( a => a.RegistrationInstance.Linkages.Any( x => x.EventItemOccurrenceId.HasValue && x.EventItemOccurrence.NextStartDateTime.HasValue ) ).ToList();
            List<Registration> noDates = registrationList.Where( a => !hasDates.Any( d => d.Id == a.Id ) ).OrderBy( x => x.RegistrationInstance.Name ).ToList();

            hasDates = hasDates
                .OrderBy( a => a.RegistrationInstance.Linkages
                     .Where( x => x.EventItemOccurrenceId.HasValue && x.EventItemOccurrence.NextStartDateTime.HasValue )
                     .OrderBy( b => b.EventItemOccurrence.NextStartDateTime )
                     .FirstOrDefault()
                     .EventItemOccurrence.NextStartDateTime )
                .ToList();

            // filter by date range
            var requestDateRange = SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( GetAttributeValue( "DateRange" ) ?? "-1||" );
            if (requestDateRange.Start.HasValue)
            {
                hasDates = hasDates
                    .Where( a => a.RegistrationInstance.Linkages
                        .Where( x => x.EventItemOccurrenceId.HasValue && x.EventItemOccurrence.NextStartDateTime.HasValue )
                        .OrderBy( b => b.EventItemOccurrence.NextStartDateTime )
                        .FirstOrDefault()
                        .EventItemOccurrence.NextStartDateTime >= requestDateRange.Start )
                    .ToList();
            }

            if (requestDateRange.End.HasValue)
            {
                hasDates = hasDates
                    .Where( a => a.RegistrationInstance.Linkages
                        .Where( x => x.EventItemOccurrenceId.HasValue && x.EventItemOccurrence.NextStartDateTime.HasValue )
                        .OrderBy( b => b.EventItemOccurrence.NextStartDateTime )
                        .FirstOrDefault()
                        .EventItemOccurrence.NextStartDateTime < requestDateRange.End )
                    .ToList();
            }

            registrationList = hasDates;
            registrationList.AddRange( noDates );

            if (this.GetAttributeValue( "LimitToOwed" ).AsBooleanOrNull() ?? true)
            {
                registrationList = registrationList.Where( a => a.BalanceDue != 0 ).ToList();
            }


            int? maxResults = GetAttributeValue( "MaxResults" ).AsIntegerOrNull();
            if (maxResults.HasValue && maxResults > 0)
            {
                registrationList = registrationList.Take( maxResults.Value ).ToList();
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "Registrations", registrationList );

            string template = GetAttributeValue( "LavaTemplate" );
            lContent.Text = template.ResolveMergeFields( mergeFields );

        }

    }
}