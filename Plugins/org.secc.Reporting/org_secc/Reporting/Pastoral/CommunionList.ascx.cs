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
                var attributeService = new AttributeService( rockContext );
                var attributeValueService = new AttributeValueService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );
                var definedValueService = new DefinedValueService( rockContext );

                var qry = workflowService.Queryable().AsNoTracking()
                    .Join( attributeService.Queryable() ,
                    w => new { EntityTypeId = 113, WorkflowTypeId = w.WorkflowTypeId.ToString() },
                    a => new { EntityTypeId = a.EntityTypeId.Value, WorkflowTypeId = a.EntityTypeQualifierValue},
                    (w, a) => new { Workflow = w, Attribute = a } )
                    .Join( attributeValueService.Queryable(),
                    obj => new { AttributeId = obj.Attribute.Id, EntityId = obj.Workflow.Id },
                    av => new { AttributeId = av.AttributeId, EntityId = av.EntityId.Value },
                    ( obj, av ) => new { Workflow = obj.Workflow, Attribute = obj.Attribute, AttributeValue = av  } )
                    .GroupBy(obj => obj.Workflow)
                    .Select( obj => new { Workflow = obj.Key, Attributes = obj.Select(a => a.Attribute).ToList(), AttributeValues=obj.Select( a => a.AttributeValue )} )
                    .Where( w => ( w.Workflow.WorkflowTypeId == 27 || w.Workflow.WorkflowTypeId == 28 || w.Workflow.WorkflowTypeId == 29 ) && w.Workflow.Status == "Active" ).ToList();

                List<DefinedValue> facilities = definedValueService.Queryable().Where( dv => dv.DefinedTypeId == 108 || dv.DefinedTypeId == 139 ).ToList();
                facilities.ForEach(h => {
                    h.LoadAttributes();
                });

                var newQry = qry.Select( w => new
                {
                    Workflow = w,
                    Name = w.Workflow.Name,
                    Campus = new Func<Campus>( () =>
                    {
                        Campus campus = null;
                        AttributeValue personAliasAV = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "PersonToVisit" || av.AttributeKey == "HomeboundPerson" ).FirstOrDefault();
                        if ( personAliasAV != null )
                        {
                            campus = personAliasService.Get( personAliasAV.Value.AsGuid() ).Person.GetCampus();
                        }
                        if ( campus == null )
                        {
                            campus = new Campus() { Name = "Unknown" };
                        }
                        return campus;
                    } )(),
                    Person = new Func<Person>( () =>
                    {
                        AttributeValue personAliasAV = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "PersonToVisit" || av.AttributeKey == "HomeboundPerson" ).FirstOrDefault();
                        if ( personAliasAV != null )
                        {
                            return personAliasService.Get( personAliasAV.Value.AsGuid() ).Person;
                        }
                        return null;
                    } )(),
                    Location = new Func<string>( () =>
                    {
                        return w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "NursingHome" || av.AttributeKey == "Hospital" ).Select( av => av.ValueFormatted ).DefaultIfEmpty("Home").FirstOrDefault();
                    } ) (),
                    Address = new Func<Location>( () => {
                        string locationGuid = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "NursingHome" || av.AttributeKey == "Hospital" ).Select( av => av.Value ).FirstOrDefault();
                        
                        if ( locationGuid != null )
                        {
                            DefinedValue dv = facilities.Where( h => h.Guid == locationGuid.AsGuid() ).FirstOrDefault();
                            Location location = new Location();
                            location.Street1 = dv.AttributeValues["Qualifier1"].ValueFormatted;
                            location.City = dv.AttributeValues["Qualifier2"].ValueFormatted;
                            location.State = dv.AttributeValues["Qualifier3"].ValueFormatted;
                            location.PostalCode = dv.AttributeValues["Qualifier4"].ValueFormatted;
                            return location;
                        }

                        int? personId = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "HomeboundPerson" || av.AttributeKey == "PersonToVisit" ).Select( av => av.ValueAsPersonId ).FirstOrDefault();
                        if ( personId.HasValue)
                        {
                            return personAliasService.Get( personId.Value ).Person.GetHomeLocation();
                        }
                        return new Location();
                    } )(),
                    PostalCode = new Func<string>( () => {
                        string postalCode = "";
                        string locationGuid = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "NursingHome" || av.AttributeKey == "Hospital" ).Select( av => av.Value ).FirstOrDefault();
                        
                        if ( locationGuid != null )
                        {
                            DefinedValue dv = facilities.Where( h => h.Guid == locationGuid.AsGuid() ).FirstOrDefault();
                            postalCode = dv.AttributeValues["Qualifier4"].ValueFormatted;
                        }
                        if (String.IsNullOrEmpty(postalCode))
                        {
                            int? personId = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "HomeboundPerson" || av.AttributeKey == "PersonToVisit" ).Select( av => av.ValueAsPersonId ).FirstOrDefault();
                            if ( personId != null && personId.HasValue )
                            {
                                Location location = personAliasService.Get( personId.Value ).Person.GetHomeLocation();
                                if ( location != null)
                                 postalCode = location.PostalCode;
                            }
                        }
                        return postalCode != null?postalCode:"";
                    } )(),
                    Room = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "Room" ).Select(av => av.ValueFormatted).FirstOrDefault(),
                    Date = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "AdmitDate" || av.AttributeKey == "StartDate" ).Select( av => av.ValueFormatted ).FirstOrDefault(),
                    Description = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "VisitationRequestDescription" || av.AttributeKey == "HomeboundResidentDescription" ).Select(av => av.ValueFormatted).FirstOrDefault(),
                    Status = w.Workflow.Status,
                    Communion = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "Communion" ).FirstOrDefault().ValueFormatted,
                    Actions = ""
                } ).Where( o => o.Communion.AsBoolean() ).OrderBy( w => w.Campus.Name ).ThenBy( w => w.Address == null?"":w.Address.PostalCode ).ThenBy( p => p.Person == null ? "" : p.Person.FullName ).ToList().AsQueryable();


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