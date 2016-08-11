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
    [WorkflowTypeField("Hospital Admission Workflow", "", false, true, "", "Workflows")]
    [WorkflowTypeField( "Nursing Home Resident Workflow", "", false, true, "", "Workflows" )]
    [WorkflowTypeField( "Homebound Person Workflow", "", false, true, "", "Workflows" )]
    [DefinedTypeField( "Hospital List") ]
    [DefinedTypeField( "Nursing Home List") ]
    public partial class CommunionList : RockBlock
    {
        #region Control Methods

        public enum COMMUNION_STATES { KY = 1, IN = 2, Other = 3}

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( string.IsNullOrWhiteSpace( GetAttributeValue( "HospitalAdmissionWorkflow" ) ) )
            {
                ShowMessage( "Block not configured. Please configure to use.", "Configuration Error", "panel panel-danger" );
                return;
            }

            gReport.GridRebind += gReport_GridRebind;

            if ( !Page.IsPostBack )
            {
                BindGrid();

                cpCampus.Campuses = Rock.Web.Cache.CampusCache.All();
                cblState.BindToEnum<COMMUNION_STATES>();
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
                var personService = new PersonService( rockContext );
                var definedValueService = new DefinedValueService( rockContext );

                Guid hospitalWorkflow = GetAttributeValue( "HospitalAdmissionWorkflow" ).AsGuid();
                Guid nursingHomeAdmissionWorkflow = GetAttributeValue( "NursingHomeResidentWorkflow" ).AsGuid();
                Guid homeBoundPersonWorkflow = GetAttributeValue( "HomeboundPersonWorkflow" ).AsGuid();
                Guid hospitalList = GetAttributeValue( "HospitalList" ).AsGuid();
                Guid nursingHomeList = GetAttributeValue( "NursingHomeList" ).AsGuid();

                var qry = workflowService.Queryable().AsNoTracking()
                    .Join( attributeService.Queryable(),
                    w => new { EntityTypeId = 113, WorkflowTypeId = w.WorkflowTypeId.ToString() },
                    a => new { EntityTypeId = a.EntityTypeId.Value, WorkflowTypeId = a.EntityTypeQualifierValue },
                    ( w, a ) => new { Workflow = w, Attribute = a } )
                    .Join( attributeValueService.Queryable(),
                    obj => new { AttributeId = obj.Attribute.Id, EntityId = obj.Workflow.Id },
                    av => new { AttributeId = av.AttributeId, EntityId = av.EntityId.Value },
                    ( obj, av ) => new { Workflow = obj.Workflow, Attribute = obj.Attribute, AttributeValue = av } )
                    .GroupBy( obj => obj.Workflow )
                    .Select( obj => new { Workflow = obj.Key, Attributes = obj.Select( a => a.Attribute ).ToList(), AttributeValues = obj.Select( a => a.AttributeValue ) } )
                    .Where( w => ( w.Workflow.WorkflowType.Guid == hospitalWorkflow
                                || w.Workflow.WorkflowType.Guid == nursingHomeAdmissionWorkflow
                                || w.Workflow.WorkflowType.Guid == homeBoundPersonWorkflow
                                ) && w.Workflow.Status == "Active" ).ToList();

                List<DefinedValue> facilities = definedValueService.Queryable().Where( dv => dv.DefinedType.Guid == hospitalList || dv.DefinedType.Guid == nursingHomeList ).ToList();
                facilities.ForEach( h => {
                    h.LoadAttributes();
                } );

                var newQry = qry.Select( w => new
                {
                    Campus = new Func<Campus>( () =>
                    {
                        Campus campus = null;
                        AttributeValue personAliasAV = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "PersonToVisit" || av.AttributeKey == "HomeboundPerson" ).FirstOrDefault();
                        if ( personAliasAV != null )
                        {
                            PersonAlias pa = personAliasService.Get( personAliasAV.Value.AsGuid() );
                            if ( pa != null ) {
                                campus = pa.Person.GetCampus();
                            }
                        }
                        if ( campus == null )
                        {
                            campus = new Campus() { Name = "Unknown" };
                        }
                        return campus;
                    } )(),
                    Person = GetPerson( personAliasService, w.AttributeValues ).FullName,
                    Age = GetPerson( personAliasService, w.AttributeValues ).Age,
                    Description = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "VisitationRequestDescription" || av.AttributeKey == "HomeboundResidentDescription" ).Select( av => av.ValueFormatted ).FirstOrDefault(),
                    Location = new Func<string>( () =>
                    {
                        return w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "NursingHome" || av.AttributeKey == "Hospital" ).Select( av => av.ValueFormatted ).DefaultIfEmpty("Home").FirstOrDefault();
                    } ) (),
                    Address = GetLocation( personService, w.AttributeValues, facilities ).Street1 + " " + GetLocation( personService, w.AttributeValues, facilities ).Street2,
                    City = GetLocation( personService, w.AttributeValues, facilities ).City,
                    State = GetLocation( personService, w.AttributeValues, facilities ).State,
                    PostalCode = GetLocation( personService, w.AttributeValues, facilities ).PostalCode,
                    Room = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "Room" ).Select(av => av.ValueFormatted).FirstOrDefault(),
                    AdmitDate = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "AdmitDate" || av.AttributeKey == "StartDate" ).Select( av => av.ValueFormatted ).FirstOrDefault(),
                    Status = w.Workflow.Status,
                    Communion = w.AttributeValues.AsQueryable().Where( av => av.AttributeKey == "Communion" ).FirstOrDefault().ValueFormatted
                } ).Where( o => o.Communion.AsBoolean() ).OrderBy( w => w.Campus.Name ).ThenBy( w => w.Address == null?"":w.PostalCode ).ThenBy( p => p.Person ).ToList().AsQueryable();


                List<COMMUNION_STATES> states = cblState.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => (COMMUNION_STATES)int.Parse(i.Value)).ToList();

                if ( states.Count > 0 )
                {
                        newQry = newQry.Where( o => ( states.Contains( COMMUNION_STATES.KY ) && o.State == "KY" ) 
                        || ( states.Contains( COMMUNION_STATES.IN ) && o.State == "IN" ) 
                        || (( states.Contains( COMMUNION_STATES.Other ) && o.State != "IN" && o.State != "KY" )));
                }
                List<int> campuses = cpCampus.Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => int.Parse(i.Value) ).ToList();

                if ( campuses.Count > 0 )
                {
                    newQry = newQry.Where( o => campuses.Contains( o.Campus.Id ) );
                }


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

        private Person GetPerson( PersonAliasService personAliasService, IEnumerable<AttributeValue> AttributeValues  )
        {

            AttributeValue personAliasAV = AttributeValues.AsQueryable().Where( av => av.AttributeKey == "PersonToVisit" || av.AttributeKey == "HomeboundPerson" ).FirstOrDefault();
            if ( personAliasAV != null )
            {
                PersonAlias pa = personAliasService.Get( personAliasAV.Value.AsGuid() );
                if ( pa != null )
                {
                    return pa.Person;
                }
            }
            return new Person();
        }

        private Location GetLocation( PersonService personService, IEnumerable<AttributeValue> AttributeValues, List<DefinedValue> facilities )
        {

            string locationGuid = AttributeValues.AsQueryable().Where( av => av.AttributeKey == "NursingHome" || av.AttributeKey == "Hospital" ).Select( av => av.Value ).FirstOrDefault();

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

            int? personId = AttributeValues.AsQueryable().Where( av => av.AttributeKey == "HomeboundPerson" || av.AttributeKey == "PersonToVisit" ).Select( av => av.ValueAsPersonId ).FirstOrDefault();
            if ( personId.HasValue )
            {
                Person p = personService.Get( personId.Value );
                if ( p != null && p.GetHomeLocation() != null)
                {
                    return p.GetHomeLocation();
                }
            }
            return new Location();
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
        
        private void ShowMessage( string message, string header = "Information", string cssClass = "panel panel-warning" )
        {
            pnlMain.Visible = false;
            pnlInfo.Visible = true;
            ltHeading.Text = header;
            ltBody.Text = message;
            pnlInfo.CssClass = cssClass;
        }
        #endregion

        protected void gFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            BindGrid();
        }
    }
}