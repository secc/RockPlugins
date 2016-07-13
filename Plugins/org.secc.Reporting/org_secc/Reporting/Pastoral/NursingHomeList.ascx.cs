// <copyright>
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
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Reporting
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Nursing Home List" )]
    [Category( "SECC > Reporting > Pastoral" )]
    [Description( "A summary of all the current nursing home residents that have been reported to Southeast." )]
    public partial class NursingHomeList : RockBlock
    {
        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            gReport.GridRebind += gReport_GridRebind;

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
            gReport.Actions.ShowAdd = true;
            gReport.Actions.AddButton.Text = "<i class=\"fa fa-plus\" Title=\"Add Hospitalization\"></i>";
            gReport.Actions.AddButton.Enabled = true;
            gReport.Actions.AddClick += addHospitalization_Click;
            gReport.Actions.ShowMergeTemplate = false;
        }

        /// <summary>
        /// Handles the GridRebind event of the gReport control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gReport_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            using ( var rockContext = new RockContext() )
            {
                var workflowService = new WorkflowService( rockContext );
                var attributeValueService = new WorkflowService( rockContext );
                var definedValueService = new DefinedValueService( rockContext );

                var qry = workflowService.Queryable().AsNoTracking()
                    .Where( w => w.WorkflowTypeId == 28 && w.Status == "Active" ).ToList();
                 qry.ForEach(
                     w =>
                     {
                         w.LoadAttributes();
                         w.Activities.ToList().ForEach( a => { a.LoadAttributes(); } );
                     } );
                var newQry = qry.Select( w => new
                {
                    Workflow = w,
                    Name = w.Name,
                    NursingHome = w.AttributeValues["NursingHome"].ValueFormatted,
                    NursingHomeAddress = new Func<string>( () => {
                        DefinedValue dv = definedValueService.Get( w.AttributeValues["NursingHome"].Value.AsGuid() );
                        dv.LoadAttributes();
                        return dv.AttributeValues["Qualifier1"].ValueFormatted + " " + 
                            dv.AttributeValues["Qualifier2"].ValueFormatted + " " + 
                            dv.AttributeValues["Qualifier3"].ValueFormatted + ", " +
                            dv.AttributeValues["Qualifier4"].ValueFormatted; })(),
                    PersonToVisit = w.AttributeValues["PersonToVisit"].ValueFormatted,
                    Age = w.AttributeValues["Age"].ValueFormatted,
                    Room = w.AttributeValues["Room"].ValueFormatted,
                    AdmitDate = w.AttributeValues["AdmitDate"].ValueFormatted,
                    Description = w.AttributeValues["VisitationRequestDescription"].ValueFormatted,
                    Visits = w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Count(),
                    LastVisitor = ( w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Any() )?w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).LastOrDefault().AttributeValues["Visitor"].ValueFormatted:"N/A",
                    LastVisitDate = ( w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Any() )?w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).LastOrDefault().AttributeValues["VisitDate"].ValueFormatted:"N/A",
                    LastVisitNotes = ( w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Any() )?w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).LastOrDefault().AttributeValues["VisitNote"].ValueFormatted:"N/A",
                    Status = w.Status,
                    Communion = w.AttributeValues["Communion"].ValueFormatted,
                    Actions = ""
                } ).OrderBy(w=>w.NursingHome).ToList().AsQueryable();

                //AddGridColumns( newQry.FirstOrDefault() );

                SortProperty sortProperty = gReport.SortProperty;
                if ( sortProperty != null )
                { 
                    gReport.SetLinqDataSource( newQry.Sort( sortProperty ) );
                }
                else
                {
                    gReport.SetLinqDataSource( newQry.OrderBy( p => p.NursingHome ).ThenBy( p => p.PersonToVisit ) );
                }
                gReport.DataBind();



            }
        }
        protected void addHospitalization_Click( object sender, EventArgs e )
        {
            Response.Redirect( "/Pastoral/NursingHome/" );
        }

        /// <summary>
        /// Adds the grid columns.
        /// </summary>
        /// <param name="dataTable">The data table.</param>
        private void AddGridColumns( object item )
        {
            Type oType = item.GetType();

            gReport.Columns.Clear();

            foreach ( var prop in oType.GetProperties() )
            {
                BoundField bf = new BoundField();

                if ( prop.PropertyType == typeof( bool ) ||
                    prop.PropertyType == typeof( bool? ) )
                {
                    bf = new BoolField();
                }

                if ( prop.PropertyType == typeof( DateTime ) ||
                    prop.PropertyType == typeof( DateTime? ) )
                {
                    bf = new DateTimeField();
                }

                bf.DataField = prop.Name;
                bf.SortExpression = prop.Name;
                bf.HeaderText = prop.Name.SplitCase();
                gReport.Columns.Add( bf );
            }
        }

        #endregion
    }
}