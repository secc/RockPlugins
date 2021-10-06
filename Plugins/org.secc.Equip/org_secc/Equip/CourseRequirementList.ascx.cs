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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Web.UI;
using org.secc.Equip.Model;
using org.secc.Equip;

namespace RockWeb.Plugins.org.secc.Equip
{
    [DisplayName( "Course Requirement List" )]
    [Category( "SECC > Equip" )]
    [Description( "Block for displaying all of the course requirements." )]

    [LinkedPage(
        "Course Requirement Details Page",
        Description = "Page for editing a course requirement.",
        Key = AttributeKey.CourseRequirementDetails
        )]

    public partial class CourseRequirementList : RockBlock, ISecondaryBlock
    {

        protected static class AttributeKey
        {
            internal const string CourseRequirementDetails = "CourseRequirementDetails";
        }

        protected static class PageParameterKey
        {
            internal const string CourseRequirementId = "CourseRequirementId";
        }

        #region Base Control Methods

        //
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.Actions.ShowAdd = true;
            gList.Actions.AddClick += Actions_AddClick;
            gList.GridRebind += gList_GridRebind;
        }


        private void Actions_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( AttributeKey.CourseRequirementDetails );
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
                BindGrid();
            }
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the GridRebind event of the gPledges control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void gList_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void BindGrid()
        {
            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );

            var allItems = courseRequirementService.Queryable()
                .OrderBy( cr => cr.Id ).ToList();

            var allowedItems = new List<CourseRequirement>();

            foreach ( var item in allItems )
            {
                if ( item.IsAuthorized( Rock.Security.Authorization.VIEW, CurrentPerson ) )
                {
                    allowedItems.Add( item );
                }
            }

            gList.DataSource = allowedItems;
            gList.DataBind();
        }

        #endregion

        protected void gList_RowSelected( object sender, RowEventArgs e )
        {
            var courseRequirementId = e.RowKeyValue.ToString();
            NavigateToLinkedPage(
                AttributeKey.CourseRequirementDetails,
                new Dictionary<string, string> { { PageParameterKey.CourseRequirementId, courseRequirementId } } );
        }

        public void SetVisible( bool visible )
        {
            upnlContent.Visible = visible;
        }

        protected void rComponents_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.DataItem is CoursePageComponent )
            {
                var component = e.Item.DataItem as CoursePageComponent;
                var btnComponent = e.Item.FindControl( "btnComponent" ) as LinkButton;
                btnComponent.Text = string.Format( @"<br><i class=""{0} fa-5x""></i><br><br>", component.Icon );
            }

        }

        protected void btnDelete_Click( object sender, RowEventArgs e )
        {
            var id = ( int ) e.RowKeyValue;
            RockContext rockContext = new RockContext();
            CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );
            var courseRequirement = courseRequirementService.Get( id );
            if ( courseRequirement != null )
            {
                courseRequirementService.Delete( courseRequirement );
                rockContext.SaveChanges();
            }

            BindGrid();
        }
    }
}