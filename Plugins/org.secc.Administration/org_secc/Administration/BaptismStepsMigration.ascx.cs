using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

using Microsoft.AspNet.SignalR;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Administration
{
    [DisplayName( "Baptism Data Migrator" )]
    [Category( "SECC > Administration" )]
    [Description( "Block that will convert baptism data in a person attribute matrix to a Baptism Step Type." )]

    [StepProgramStepTypeField( "Baptism Step Type", "The Baptism Step Type that the data will be migrated to.", true, "", "", 0, "BaptismStepType", "7C50242C-FE2D-4291-B53C-4FE0FE7CE6C1" )]
    [StepProgramStepStatusField( "Completed Status", "The status for a completed step.", true, "", "", 1, "StepStatus", "7C50242C-FE2D-4291-B53C-4FE0FE7CE6C1" )]
    [AttributeField( "72657ED8-D16E-492E-AC12-144C5E7567E7", "Baptism Matrix Person Attribute", "The Person Attribute that references their Baptism Matrix", true, false, "", "", 2, "BaptismPersonAttribute" )]

    public partial class BaptismStepsMigration : RockBlock
    {

        protected class BaptismMatrixReferenceItem
        {
            public int MatrixItemId { get; set; }
            public int PersonId { get; set; }
        }

        #region Fields


        /// <summary>
        /// This holds the reference to the RockMessageHub SignalR Hub context.
        /// </summary>
        private IHubContext _hubContext = GlobalHost.ConnectionManager.GetHubContext<RockMessageHub>();
        Dictionary<string, string> results = new Dictionary<string, string>() { { "Success", "" }, { "Fail", "" } };

        /// <summary>
        /// Gets the signal r notification key.
        /// </summary>
        /// <value>
        /// The signal r notification key.
        /// </value>
        public string SignalRNotificationKey
        {
            get
            {
                return string.Format( "BaptismStepsMigration_BlockId:{0}_SessionId:{1}", this.BlockId, Session.SessionID );
            }
        }

        #endregion Fields

        #region Properties

        /// <summary>
        /// The Baptism Step Type to migrate the baptism matrix items to.
        /// </summary>
        protected StepType BaptismStepType { get; set; }

        /// <summary>
        /// The Step Program's Completed Status.
        /// </summary>
        protected StepStatus StepStatus { get; set; }

        /// <summary>
        /// The Person Attribute that references the person's Baptism Attribute Reocrd.
        /// </summary>
        protected AttributeCache PersonAttribute { get; set; }

        /// <summary>
        /// The Item Ids and the Person ids that need to be migrated.
        /// </summary>
        protected List<BaptismMatrixReferenceItem> BaptismMatrixItems { get; set; }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Processes the block's Initialization methods (events that happen early in the user control's life cycle).
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            RockPage.AddScriptLink( "~/Scripts/jquery.signalR-2.2.0.min.js", fingerprint: false );

        }

        /// <summary>
        /// Processes the Block's load events
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            NotificationBoxClear();
            if ( LoadBlockSettings() )
            {
                LoadPendingMigrations();
            }
        }

        /// <summary>
        /// Loads Viewstate items or this block
        /// </summary>
        /// <param name="savedState"></param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            var baseVieweStateKey = $"{base.BlockId}_";

            var vsMatrixItems = ViewState[$"{baseVieweStateKey}MatrixItems"] as string;

            if ( vsMatrixItems.IsNotNullOrWhiteSpace() )
            {
                BaptismMatrixItems = vsMatrixItems.FromJsonOrNull<List<BaptismMatrixReferenceItem>>();
            }
            else
            {
                BaptismMatrixItems = null;
            }

        }

        /// <summary>
        /// Saves Viewstate Items
        /// </summary>
        /// <returns></returns>
        protected override object SaveViewState()
        {
            var baseViewStateKey = $"{base.BlockId}_";

            ViewState[$"{baseViewStateKey}MatrixItems"] = BaptismMatrixItems.ToJson();

            return base.SaveViewState();

        }
        #endregion

        #region Events

        /// <summary>
        /// Handles the Migrate Record Button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnMigrateRecords_Click( object sender, EventArgs e )
        {
            if ( hfProcessingMigrations.Value.Equals( "0" ) )
            {
                hfProcessingMigrations.Value = "1";
                MigrateBaptismRecords();
            }
            else
            {
                LoadPendingMigrations( true );

                lProgressMessage.Text = String.Empty;
                lProgressResults.Text = String.Empty;

                var js = new StringBuilder();
                js.AppendFormat( "$('#{0}').hide();", pnlProgress.ClientID );
                js.AppendFormat( "$('#{0}').show();", pnlSummary.ClientID );

                ScriptManager.RegisterClientScriptBlock( upMain, upMain.GetType(), "ShowPanels" + DateTime.Now.Ticks, js.ToString(), true );

            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the Block Setting Attributes.  If a requried attribute is not found, it will display an error message
        /// and will return false.
        /// </summary>
        /// <returns>A boolean flag indicating if all block attribute properties have been loaded properly</returns>
        private bool LoadBlockSettings()
        {
            using ( var rockContext = new RockContext() )
            {
                var baptismStepAV = GetAttributeValue( "BaptismStepType" );
                var baptismStepGuids = baptismStepAV.SplitDelimitedValues();

                var stepStatusAV = GetAttributeValue( "StepStatus" );
                var stepstatusGuids = stepStatusAV.SplitDelimitedValues();

                var personAttributeGuid = GetAttributeValue( "BaptismPersonAttribute" ).AsGuid();

                if ( baptismStepAV.IsNotNullOrWhiteSpace() && baptismStepGuids.Length > 1 )
                {
                    BaptismStepType = new StepTypeService( rockContext ).Get( baptismStepGuids[1].AsGuid() );
                }

                if ( stepStatusAV.IsNotNullOrWhiteSpace() && stepstatusGuids.Length > 1 )
                {
                    StepStatus = new StepStatusService( rockContext ).Get( stepstatusGuids[1].AsGuid() );
                }

                PersonAttribute = AttributeCache.Get( personAttributeGuid );
            }

            List<string> errors = new List<string>();

            if ( BaptismStepType == null )
            {
                errors.Add( "Baptism Step Type is not set." );
            }

            if ( StepStatus == null )
            {
                errors.Add( "Step Status is not set." );
            }

            if ( PersonAttribute == null )
            {
                errors.Add( "Baptism Matrix Attribute Not Set." );
            }


            if ( errors.Count > 0 )
            {
                StringBuilder sb = new StringBuilder();
                sb.Append( "<strong><i class='fas fa-exclamation-triangle'></i>&nbsp; Please Verify Block Settings</strong>" );
                sb.Append( "<ul type='disc'>" );
                foreach ( var error in errors )
                {
                    sb.AppendFormat( "<li>{0}</li>", error );
                }
                sb.Append( "</ul>" );

                NotificationBoxSet( sb.ToString(), NotificationBoxType.Validation );
            }

            return errors.Count.Equals( 0 );
        }

        /// <summary>
        /// Loads a class level dictionary of MatrixItemIds and their associated Person Ids that need to be migrated to
        /// the Baptism Step the attribute matrix item.
        /// </summary>
        /// <param name="refreshMatrixItems">Optional boolean paramater that indicates if the Matrix Item dictionary needs to be
        ///    refreshed. Default is false. 
        /// </param>
        private void LoadPendingMigrations( bool refreshMatrixItems = false )
        {
            if ( BaptismMatrixItems == null || refreshMatrixItems )
                using ( var rockContext = new RockContext() )
                {


                    var sql =
                        @"SELECT
                            ami.[Id] as MatrixItemId
                            ,av.[EntityId] as PersonId
                          FROM
                            [AttributeValue] av
                            INNER JOIN [AttributeMatrix] am on TRY_CAST(av.[Value] as UNIQUEIDENTIFIER) = am.[Guid]
                            INNER JOIN [AttributeMatrixItem] ami on am.[Id] = ami.[AttributeMatrixId]
                            LEFT OUTER JOIN [Step] s on ami.[Id] = s.[ForeignId] and s.[StepTypeId] = @BaptismStepType
                          WHERE
                            av.[AttributeId] = @AttributeId
                            AND s.Id IS NULL";

                    BaptismMatrixItems = rockContext.Database.SqlQuery<BaptismMatrixReferenceItem>( sql,
                        new SqlParameter( "@AttributeId", PersonAttribute.Id ),
                        new SqlParameter( "@BaptismStepType", BaptismStepType.Id ) ).ToList();

                    lRecordsToMigrate.Text = BaptismMatrixItems.Count.ToString();
                    nbRecordsToProcess.Text = BaptismMatrixItems.Count.ToString();
                    hfProcessingMigrations.Value = "0";
                }

            pnlSummary.Visible = true;
            pnlActions.Visible = true;

        }


        /// <summary>
        /// Creates and runs the SignalR request that migrates the baptism records.
        /// </summary>
        private void MigrateBaptismRecords()
        {
            int recordsToProcess = nbRecordsToProcess.Text.AsInteger();

            BaptismMatrixItems = BaptismMatrixItems.Take( recordsToProcess ).ToList();
            long totalMilliseconds = 0;

            long baptismsProcessed = 0;
            var importTask = new Task( () =>
             {
                 System.Threading.Thread.Sleep( 1000 );
                 _hubContext.Clients.All.showSummary( this.SignalRNotificationKey, false );
                 _hubContext.Clients.All.showButtons( this.SignalRNotificationKey, false );
                 _hubContext.Clients.All.updateProcessingFlag( this.SignalRNotificationKey, "1" );
                 Stopwatch sw = Stopwatch.StartNew();

                 int i = 1;
                 foreach ( var item in BaptismMatrixItems )
                 {
                     bool baptismMigrated = false;
                     OnProgress( $"Processing Baptism Migration {i} of {BaptismMatrixItems.Count}" );
                     baptismMigrated = MigrateBaptismItem( item.MatrixItemId, item.PersonId );

                     if ( baptismMigrated )
                     {
                         baptismsProcessed++;
                     }
                     i++;

                 }

                 sw.Stop();
                 totalMilliseconds = sw.ElapsedMilliseconds;
                 _hubContext.Clients.All.showButtons( this.SignalRNotificationKey, true );
                 BaptismMatrixItems.Clear();
             } );

            importTask.ContinueWith( ( t ) =>
             {
                 if ( t.IsFaulted )
                 {
                     foreach ( var exception in t.Exception.InnerExceptions )
                     {
                         LogException( exception );
                     }
                     OnProgress( $"ERROR: {t.Exception.Message}" );
                 }
                 else
                 {
                     OnProgress( $"{baptismsProcessed} Baptisms have been Processed. Complete [{totalMilliseconds}]" );
                 }

             } );

            importTask.Start();

        }

        /// <summary>
        /// Migrates a baptism record from a Attribute Matrix Item to a Step
        /// </summary>
        /// <param name="itemId">Matrix Item Id</param>
        /// <param name="personId">The Person Id of the person who was baptized.</param>
        /// <returns></returns>
        private bool MigrateBaptismItem( int itemId, int personId )
        {
            using ( var context = new RockContext() )
            {
                var matrixItem = new AttributeMatrixItemService( context ).Get( itemId );

                var person = new PersonService( context ).Get( personId );

                if ( matrixItem == null )
                {
                    results["Fail"] += String.Format( "Failed Migration {0} - {1}, baptism matrix item not found.",
                        itemId,
                        person != null ? person.FullName : "Unknown" ) + Environment.NewLine;
                    return false;
                }



                matrixItem.LoadAttributes( context );
                var campusGuid = matrixItem.GetAttributeValue( "BaptismLocation" ).AsGuidOrNull();
                var baptismDate = matrixItem.GetAttributeValue( "BaptismDate" ).AsDateTime();
                var baptizedBy = matrixItem.GetAttributeValue( "BaptizedBy" );
                var baptismTypeGuid = matrixItem.GetAttributeValue( "BaptismType" ).AsGuidOrNull();


                var stepService = new StepService( context );



                var step = stepService.Queryable()
                    .Where( s => s.ForeignId == itemId )
                    .Where( s => s.PersonAlias.PersonId == personId )
                    .SingleOrDefault();

                if ( step == null )
                {
                    step = new Step()
                    {
                        StepTypeId = BaptismStepType.Id,
                        PersonAliasId = person.PrimaryAliasId.Value,
                        ForeignId = matrixItem.Id
                    };

                    stepService.Add( step );
                }

                if ( baptismDate >= new DateTime( 1753, 1, 1 ) ) //datetime minimum date
                {
                    step.StartDateTime = baptismDate;
                    step.CompletedDateTime = baptismDate;
                }

                step.StepStatusId = StepStatus.Id;

                if ( campusGuid.HasValue )
                {
                    step.CampusId = CampusCache.Get( campusGuid.Value ).Id;
                }

                context.SaveChanges();
                step.LoadAttributes( context );

                step.SetAttributeValue( "BaptismType", baptismTypeGuid );

                if ( baptizedBy.IsNotNullOrWhiteSpace() )
                {
                    step.SetAttributeValue( "BaptizedBy", baptizedBy );
                }
                else
                {
                    step.SetAttributeValue( "BaptizedBy", null );
                }

                step.SaveAttributeValues( context );

                results["Success"] += String.Format( "Successfully migrated baptism item {0}-{1} for {2}.",
                    matrixItem.Id,
                    baptismTypeGuid.HasValue ? DefinedValueCache.Get( baptismTypeGuid.Value ).Value : "Unknown Type",
                    person != null ? person.FullName : "Unknown" ) + Environment.NewLine;

                return true;

            }
        }

        /// <summary>
        /// Clears the Notification Box at the top of the block
        /// </summary>
        private void NotificationBoxClear()
        {
            nbMain.Text = String.Empty;
            nbMain.Title = String.Empty;
            nbMain.Visible = false;
        }


        /// <summary>
        /// Sets and displays the content of the notification box.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="boxType">The notification/alert box type</param>
        private void NotificationBoxSet( string message, NotificationBoxType boxType )
        {
            nbMain.Text = message;
            nbMain.NotificationBoxType = boxType;
            nbMain.Visible = true;

        }

        /// <summary>
        /// Handles the ProgressChanged event of the BackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ProgressChangedEventArgs"/> instance containing the event data.</param>
        private void OnProgress( object e )
        {

            string progressMessage = string.Empty;
            DescriptionList progressResults = new DescriptionList();
            if ( e is string )
            {
                progressMessage = e.ToString();
            }

            foreach ( var result in results )
            {
                progressResults.Add( result.Key, result.Value );
            }

            WriteProgressMessage( progressMessage, progressResults.Html );
        }

        /// <summary>
        /// Writes the progress message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void WriteProgressMessage( string message, string results )
        {
            _hubContext.Clients.All.receiveNotification( this.SignalRNotificationKey, message, results.ConvertCrLfToHtmlBr() );
        }


        #endregion




    }
}