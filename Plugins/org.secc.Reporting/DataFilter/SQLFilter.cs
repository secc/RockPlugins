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
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Reporting.Data;
using org.secc.Reporting.Model;
using org.secc.Reporting.Transactions;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Transactions;
using Rock.Web.UI.Controls;

namespace org.secc.Reporting.DataFilter
{
    /// <summary>
    /// 
    /// </summary>
    [Description( "Filter entities from a SQL query." )]
    [Export( typeof( DataFilterComponent ) )]
    [ExportMetadata( "ComponentName", "SQL Filter" )]
    public class SQLFilter : DataFilterComponent
    {
        #region Properties

        /// <summary>
        /// Gets the entity type that filter applies to.
        /// </summary>
        /// <value>
        /// The entity that filter applies to.
        /// </value>
        public override string AppliesToEntityType
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <value>
        /// The section.
        /// </value>
        public override string Section
        {
            get { return "Additional Filters"; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <value>
        /// The title.
        /// </value>
        public override string GetTitle( Type entityType )
        {
            return "SQL Filter";
        }

        /// <summary>
        /// Formats the selection on the client-side.  When the filter is collapsed by the user, the Filterfield control
        /// will set the description of the filter to whatever is returned by this property.  If including script, the
        /// controls parent container can be referenced through a '$content' variable that is set by the control before 
        /// referencing this property.
        /// </summary>
        /// <value>
        /// The client format script.
        /// </value>
        public override string GetClientFormatSelection( Type entityType )
        {
            return @" 
function() {
  var result = ""SQL Filter"";
  return result;
}
";
        }

        /// <summary>
        /// Formats the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="selection">The selection.</param>
        /// <returns></returns>
        public override string FormatSelection( Type entityType, string selection )
        {
            string result = "SQL Filter";
            return result;
        }

        /// <summary>
        /// Creates the child controls.
        /// </summary>
        /// <returns></returns>
        public override Control[] CreateChildControls( Type entityType, FilterField filterControl )
        {
            var notificationBox = new NotificationBox
            {
                ID = filterControl.ID + "_nbNotice",
                Text = "Running SQL commands directly against the database while powerful, can be extremely dangerous. The difference is all in your hands.<br>If you are unsure of the SQL you are about to run <b>DO NOT</b> proceed.",
                Title = "Warning!",
                NotificationBoxType = NotificationBoxType.Warning
            };
            filterControl.Controls.Add( notificationBox );

            var tbSQL = new CodeEditor
            {
                ID = filterControl.ID + "_tbSQL",
                EditorMode = CodeEditorMode.Sql,
                Label = "SQL Query",
                Help = "Return a list of the entity Ids. Example: SELECT [Id] FROM [Person] WHERE [LastName] = 'Decker'"
            };
            filterControl.Controls.Add( tbSQL );

            return new System.Web.UI.Control[2] { notificationBox, tbSQL };
        }

        /// <summary>
        /// Renders the controls.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="filterControl">The filter control.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="controls">The controls.</param>
        public override void RenderControls( Type entityType, FilterField filterControl, HtmlTextWriter writer, Control[] controls )
        {
            base.RenderControls( entityType, filterControl, writer, controls );
        }

        /// <summary>
        /// Gets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <returns></returns>
        public override string GetSelection( Type entityType, Control[] controls )
        {
            if ( controls.Count() >= 2 )
            {
                SqlFilterCleanupTransaction sqlFilterCleanupTransaction = new SqlFilterCleanupTransaction();
                RockQueue.TransactionQueue.Enqueue( sqlFilterCleanupTransaction );

                CodeEditor tbSQL = controls[1] as CodeEditor;
                return tbSQL.Text;
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets the selection.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        /// <param name="controls">The controls.</param>
        /// <param name="selection">The selection.</param>
        public override void SetSelection( Type entityType, Control[] controls, string selection )
        {
            if ( controls.Count() >= 2 )
            {
                CodeEditor tbSQL = controls[1] as CodeEditor;
                tbSQL.Text = selection;
            }
        }

        public override Expression GetExpression( Type entityType, IService serviceInstance, ParameterExpression parameterExpression, string selection )
        {
            if ( selection.IsNotNullOrWhiteSpace() )
            {
                var hash = Helpers.Md5Hash( selection );

                var rockContext = ( RockContext ) serviceInstance.Context;
                MethodInfo queryableMethodInfo = serviceInstance.GetType().GetMethod( "Queryable", new Type[] { } );
                IQueryable<IEntity> entityQuery = queryableMethodInfo.Invoke( serviceInstance, null ) as IQueryable<IEntity>;

                var entityIds = serviceInstance.Context.Database.SqlQuery<int>( selection ).ToList();

                //Small data sets don't need to be put in the database for subselect
                if ( entityIds.Count <= 1000 )
                {
                    var qry = entityQuery.Where( p => entityIds.Contains( p.Id ) );
                    return FilterExpressionExtractor.Extract<IEntity>( qry, parameterExpression, "p" );
                }

                /*
                 * SqlQuery() returns what is essentially an IEnumerable. This means when we build our expression
                 * we won't be telling EF how to get the values we want, but the actual values themselves.
                 * This is okay for smallish sets of data, but if it gets over a few thousand it overwhelms SQL Server
                 * and throws an error. In testing with SQL Server 2017 this happened at around 32,000 items.
                 * The code below saves our data set in a skinny table so that we can query against it. Before querying
                 * it updates the table using direct requests to keep the data not stale.
                 * https://docs.microsoft.com/en-us/sql/t-sql/language-elements/in-transact-sql?view=sql-server-2017#remarks
                 */

                using ( ReportingContext updateContext = new ReportingContext() )
                {
                    var sqlStores = updateContext.DataViewSQLFilterStores;

                    //This is slower, but we need to stay out of the database
                    var storedEntityIds = sqlStores.Where( s => s.Hash == hash ).Select( s => s.EntityId ).ToList();

                    var toRemove = storedEntityIds.Except( entityIds ).ToList();
                    var toAdd = entityIds.Except( storedEntityIds ).ToList();

                    //If we need to remove more than 5,000 entries it's faster/safer to just remove all the entries
                    if ( toRemove.Count >= 5000 )
                    {
                        updateContext.Database.ExecuteSqlCommand( string.Format( "DELETE FROM _org_secc_Reporting_DataViewSQLFilterStore WHERE Hash = '{0}'", hash ) );
                        toAdd = entityIds;
                    }
                    else if ( toRemove.Any() )
                    {
                        updateContext.Database.ExecuteSqlCommand( string.Format( "DELETE FROM _org_secc_Reporting_DataViewSQLFilterStore WHERE Hash = '{0}' AND EntityId IN ({1})", hash, string.Join( ",", toRemove ) ) );
                    }

                    if ( toAdd.Any() )
                    {
                        List<DataViewSQLFilterStore> storesToAdd = toAdd.Select( s =>
                         new DataViewSQLFilterStore()
                         {
                             Hash = hash,
                             EntityId = s
                         } ).ToList();

                        updateContext.BulkInsert( storesToAdd );
                    }
                }

                DataViewSqlFilterStoreService dataViewSqlFilterSoreService = new DataViewSqlFilterStoreService( rockContext );

                var selectedEntityIds = dataViewSqlFilterSoreService.Queryable().Where( s => s.Hash == hash ).Select( s => s.EntityId );
                var subselectQry = entityQuery.Where( p => selectedEntityIds.Contains( p.Id ) );
                return FilterExpressionExtractor.Extract<IEntity>( subselectQry, parameterExpression, "p" );
            }
            return null;
        }
        #endregion
    }
}