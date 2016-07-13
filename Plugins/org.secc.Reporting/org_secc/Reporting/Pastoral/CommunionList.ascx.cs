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
    [DisplayName( "Communion List" )]
    [Category( "SECC > Reporting > Pastoral" )]
    [Description( "A list of all the pastoral care patients/residents that have been requested communion." )]
    public partial class CommunionList : RockBlock
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
                var personAliasService = new PersonAliasService( rockContext );
                var definedValueService = new DefinedValueService( rockContext );

                var qry = workflowService.Queryable().AsNoTracking()
                    .Where( w => ( w.WorkflowTypeId == 27 || w.WorkflowTypeId == 28 || w.WorkflowTypeId == 29 ) && w.Status == "Active" ).ToList();
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
                    Campus = personAliasService.Get( w.AttributeValues.ContainsKey( "PersonToVisit" ) ? w.AttributeValues["PersonToVisit"].Value.AsGuid() : w.AttributeValues["HomeboundPerson"].Value.AsGuid() ).Person.GetCampus(),
                    Location = w.AttributeValues.ContainsKey( "NursingHome" ) ? w.AttributeValues["NursingHome"].ValueFormatted : "Home",
                    Address = new Func<Location>( () => {
                        if ( w.AttributeValues.ContainsKey( "NursingHome" ) )
                        {
                            DefinedValue dv = definedValueService.Get( w.AttributeValues["NursingHome"].Value.AsGuid() );
                            dv.LoadAttributes();
                            Location location = new Location();
                            location.Street1 = dv.AttributeValues["Qualifier1"].ValueFormatted;
                            location.City = dv.AttributeValues["Qualifier2"].ValueFormatted;
                            location.State = dv.AttributeValues["Qualifier3"].ValueFormatted;
                            location.PostalCode = dv.AttributeValues["Qualifier4"].ValueFormatted;
                            return location;
                        }
                        Person person = personAliasService.Get( w.AttributeValues.ContainsKey( "PersonToVisit" ) ? w.AttributeValues["PersonToVisit"].Value.AsGuid() : w.AttributeValues["HomeboundPerson"].Value.AsGuid() ).Person;
                        return person.GetHomeLocation();
                    } )(),
                    PersonName = w.AttributeValues.ContainsKey( "PersonToVisit" )?w.AttributeValues["PersonToVisit"].ValueFormatted: w.AttributeValues["HomeboundPerson"].ValueFormatted,
                    Age = personAliasService.Get( w.AttributeValues.ContainsKey( "PersonToVisit" )?w.AttributeValues["PersonToVisit"].Value.AsGuid() : w.AttributeValues["HomeboundPerson"].Value.AsGuid() ).Person.Age,
                    Room = w.AttributeValues.ContainsKey( "Room" ) ? w.AttributeValues["Room"].ValueFormatted:"",
                    Date = w.AttributeValues.ContainsKey( "AdmitDate" ) ? w.AttributeValues["AdmitDate"].ValueFormatted : w.AttributeValues["StartDate"].ValueFormatted,
                    Description = w.AttributeValues.ContainsKey( "VisitationRequestDescription" ) ? w.AttributeValues["VisitationRequestDescription"].ValueFormatted: w.AttributeValues["HomeboundResidentDescription"].ValueFormatted,
                    Status = w.Status,
                    Communion = w.AttributeValues["Communion"].ValueFormatted,
                    Actions = ""
                } ).Where(o => o.Communion.AsBoolean()).OrderBy( w => w.Campus.Name ).ThenBy(w=>w.Address.PostalCode).ThenBy( p => p.PersonName ).ToList().AsQueryable();

                //AddGridColumns( newQry.FirstOrDefault() );

                SortProperty sortProperty = gReport.SortProperty;
                if ( sortProperty != null )
                { 
                    gReport.SetLinqDataSource( newQry.Sort( sortProperty ) );
                }
                else
                {
                    gReport.SetLinqDataSource( newQry );
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