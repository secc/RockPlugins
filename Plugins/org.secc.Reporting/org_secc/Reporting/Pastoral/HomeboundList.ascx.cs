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
    [ContextAware( typeof( Person ) )]
    [DisplayName( "Homebound List" )]
    [Category( "SECC > Reporting > Pastoral" )]
    [Description( "A summary of all the current homebound residents that have been reported to Southeast." )]
    public partial class HomeboundList : RockBlock
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
            gReport.Actions.AddButton.Text = "<i class=\"fa fa-plus\" Title=\"Add Homebound Resident\"></i>";
            gReport.Actions.AddButton.Enabled = true;
            gReport.Actions.AddClick += addHomeboundResident_Click;
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

                var contextEntity = this.ContextEntity();

                var workflowService = new WorkflowService( rockContext );
                var attributeValueService = new WorkflowService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );

                var qry = workflowService.Queryable().AsNoTracking()
                    .Where( w => w.WorkflowTypeId == 38 && w.Status == "Active" ).ToList();
                qry.ForEach(
                     w =>
                     {
                         w.LoadAttributes();
                         w.Activities.ToList().ForEach( a => { a.LoadAttributes(); } );
                     } );

                if ( contextEntity != null )
                {
                    qry = qry.Where( w => w.AttributeValues["HomeboundPerson"].Value == ( ( Person ) contextEntity ).PrimaryAlias.Guid.ToString() ).ToList();
                }

                var newQry = qry.Select( w => new
                {
                    Id = w.Id,
                    Workflow = w,
                    Name = w.Name,
                    Address = new Func<string>( () => {
                        PersonAlias p = personAliasService.Get( w.AttributeValues["HomeboundPerson"].Value.AsGuid() );
                        Location homeLocation = p.Person.GetHomeLocation();
                        if (homeLocation == null)
                        {
                            return "";
                        }
                        return homeLocation.Street1 +
                            homeLocation.City + " " +
                            homeLocation.State + ", " +
                            homeLocation.PostalCode; })(),
                    HomeboundPerson = new Func<Person>( () =>
                    {
                        return personAliasService.Get( w.AttributeValues["HomeboundPerson"].Value.AsGuid() ).Person;
                    } )(),
                    Age = personAliasService.Get( w.AttributeValues["HomeboundPerson"].Value.AsGuid() ).Person.Age,
                    StartDate = w.AttributeValues["StartDate"].ValueFormatted,
                    Description = w.AttributeValues["HomeboundResidentDescription"].ValueFormatted,
                    Visits = w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Count(),
                    LastVisitor = ( w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Any() )?w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).LastOrDefault().AttributeValues["Visitor"].ValueFormatted:"N/A",
                    LastVisitDate = ( w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Any() )?w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).LastOrDefault().AttributeValues["VisitDate"].ValueFormatted:"N/A",
                    LastVisitNotes = ( w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).Any() )?w.Activities.Where( a => a.ActivityType.Name == "Visitation Info" ).LastOrDefault().AttributeValues["VisitNote"].ValueFormatted:"N/A",
                    Status = w.Status,
                    Communion = w.AttributeValues["Communion"].ValueFormatted,
                    Actions = ""
                } ).OrderBy(w=>w.Name).ToList().AsQueryable();

                SortProperty sortProperty = gReport.SortProperty;
                if ( sortProperty != null )
                { 
                    gReport.SetLinqDataSource( newQry.Sort( sortProperty ) );
                }
                else
                {
                    gReport.SetLinqDataSource( newQry.OrderBy( p => p.Name ) );
                }
                gReport.DataBind();



            }
        }
        protected void addHomeboundResident_Click( object sender, EventArgs e )
        {
            string url = "/Pastoral/Homebound/";
            var contextEntity = this.ContextEntity();

            if ( contextEntity != null )
            {
                url += "?PersonId=" + contextEntity.Id;
            }
            Response.Redirect( url );
        }

        protected void gReport_RowSelected( object sender, RowEventArgs e )
        {
            Response.Redirect( "~/Pastoral/Homebound/" + e.RowKeyId );
        }
        #endregion
    }
}