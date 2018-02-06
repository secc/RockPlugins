using System;
using System.Collections.Generic;
using System.ComponentModel;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System.Linq;
using org.secc.OAuth.Model;
using org.secc.OAuth.Data;
using System.Web.UI;

namespace RockWeb.Plugins.org_secc.RoomScanner
{
    /// <summary>
    /// OAuth configuration
    /// </summary>
    [DisplayName( "RoomScanner RoomReport" )]
    [Category( "SECC > Security" )]
    [Description( "Report to show where children were located." )]
    public partial class RoomReport : Rock.Web.UI.RockBlock
    {
        private static int personEntityTypeId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                dpDate.Text = GetUserPreference( "RoomScannerExport" + BlockCache.Id.ToString() );
            }
        }

        protected void btnGo_Click( object sender, EventArgs e )
        {
            var date = dpDate.Text.AsDateTime();
            if ( date == null )
            {
                return;
            }

            var locationId = PageParameter( "LocationId" ).AsInteger();

            var tomorrow = date.Value.AddDays( 1 );

            var rockContext = new RockContext();
            rockContext.Database.CommandTimeout = 300; //5 minutes
            var historyService = new HistoryService( rockContext );
            var qry = historyService.Queryable()
                .Where( h => h.EntityTypeId == personEntityTypeId
                             && h.RelatedEntityId == locationId
                             && h.CategoryId == 4
                             && h.CreatedDateTime > date
                             && h.CreatedDateTime < tomorrow )
                             .Join( new PersonService( rockContext ).Queryable(),
                             h => h.EntityId,
                             p => p.Id,
                             ( h, p ) => new { Person = p, History = h } )
                             .GroupBy( a => a.Person )
                             .OrderBy( a => a.Key.NickName )
                             .ThenBy( a => a.Key.LastName )
                             .ToList();
            var roomEvents = new List<RoomEvent>();
            var failedRoomEvents = new List<RoomEvent>();

            var newDate = new DateTime();
            var personLinks = new List<int>();

            foreach ( var person in qry )
            {
                personLinks.Add( person.Key.Id );
                var roomEvent = new RoomEvent( person.Key );

                foreach ( var item in person )
                {
                    roomEvent.AddEvent( item.History );

                    if ( item.History.Verb == "Entry" && roomEvent.Entry == newDate )
                    {
                        roomEvent.Entry = item.History.CreatedDateTime.Value;
                    }
                    else if ( item.History.Verb == "Exit" )
                    {
                        //if for somee reason there is no entry into the room
                        //we still want the information to appear on the chart
                        if ( roomEvent.Entry == newDate )
                        {
                            roomEvent.Entry = date.Value;
                        }
                        roomEvent.Exit = item.History.CreatedDateTime.Value;
                        roomEvents.Add( roomEvent );
                        roomEvent = new RoomEvent( person.Key );
                    }
                }
                if ( roomEvent.Events.Any() )
                {
                    failedRoomEvents.Add( roomEvent );
                }
            }



            var combined = String.Join( ",", roomEvents.Select( r => r.ToString() ) );
            var peopleString = string.Join( ",", personLinks );

            var script = string.Format( @"
        google.charts.load(""current"", {{ packages: [""timeline""] }});
        google.charts.setOnLoadCallback(drawChart);
        function drawChart() {{

            var container = document.getElementById('report');
            chart = new google.visualization.Timeline(container);
            var dataTable = new google.visualization.DataTable();
            dataTable.addColumn({{ type: 'string', id: 'Room' }});
            dataTable.addColumn({{ type: 'string', id: 'Name' }});
            dataTable.addColumn({{ type: 'string', role: 'tooltip',  'p': {{ 'html': true }} }});
            dataTable.addColumn({{ type: 'date', id: 'Start' }});
            dataTable.addColumn({{ type: 'date', id: 'End' }});
            dataTable.addRows([{0}]);

            var options = {{
                tooltip: {{ isHtml: true }}
            }};
            chart.draw(dataTable, options);

            google.visualization.events.addListener(chart, 'select', alertAction);
}}
            var personLinks = [{1}];
drawChart();", combined, peopleString );


            ScriptManager.RegisterStartupScript( pnlReport, pnlReport.GetType(), "report", script, true );

        }

        protected void dpDate_TextChanged( object sender, EventArgs e )
        {
            var date = dpDate.Text;
            SetUserPreference( "RoomScannerExport" + BlockCache.Id.ToString(), date );
        }
    }
}

class RoomEvent
{
    public Person Person { get; set; }
    public DateTime Entry { get; set; }
    public DateTime Exit { get; set; }
    public List<string> Events { get; set; }

    public RoomEvent( Person person )
    {
        Person = person;
        Events = new List<string>();
    }

    public void AddEvent( History history )
    {
        Events.Add( history.Summary );
    }

    public override string ToString()
    {
        var tooltip = string.Format( "<div class=\"timelineTooltip\"><b>{0}</b><hr>{1}", Person.FullName.Replace( "'", "\\'" ), String.Join( "<br>", Events ) );
        var entry = string.Format( "new Date(0, 0, 0, {0}, {1}, {2})", Entry.Hour, Entry.Minute, Entry.Second );
        var exit = string.Format( "new Date(0, 0, 0, {0}, {1}, {2})", Exit.Hour, Exit.Minute, Exit.Second );
        return string.Format( "['{0}', '{0}', '{1}', {2}, {3}]", Person.FullName.Replace( "'", "\\'" ), tooltip, entry, exit );
    }
}

