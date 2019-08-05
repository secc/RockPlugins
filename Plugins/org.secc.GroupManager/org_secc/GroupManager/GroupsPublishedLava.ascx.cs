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
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI.WebControls;
using org.secc.GroupManager.Model;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Attribute;
using Rock.Lava;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// Block for people to find a home group that matches their filters.
    /// </summary>
    [DisplayName( "Publish Groups Lava" )]
    [Category( "SECC > Groups" )]
    [Description( "Block to output published groups in lava." )]
    [CodeEditorField( "Lava Template", "Lava to display all published groups", CodeEditorMode.Lava )]

    public partial class GroupLava : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( SetFilters() )
            {
                BindData();
            }
        }

        protected void BindData()
        {
            List<int> campusIds = cblCampus.Items.OfType<ListItem>().Where( l => l.Selected ).Select( a => a.Value.AsInteger() ).ToList();
            List<int> categories = cblCategory.Items.OfType<ListItem>().Where( l => l.Selected ).Select( a => a.Value.AsInteger() ).ToList();

            // Initialize services
            var rockContext = new RockContext();
            var publishGroupService = new PublishGroupService( rockContext );
            var attributeService = new AttributeService( rockContext );
            var attributeValueService = new AttributeValueService( rockContext );

            //Get the entity types
            var publishGroupEntityTypeId = EntityTypeCache.Get( typeof( PublishGroup ) ).Id;
            var groupEntityTypeId = EntityTypeCache.Get( typeof( Group ) ).Id;

            //Attribute Queryables
            var publishGroupAttributeQry = attributeService.Queryable()
                .Where( a => a.EntityTypeId == publishGroupEntityTypeId )
                .Select( a => a.Id );

            var groupAttributeQry = attributeService.Queryable()
               .Where( a => a.EntityTypeId == groupEntityTypeId )
               .Select( a => a.Id );

            //Attribute Value Queryables
            var publishGroupAttributeValueQry = attributeValueService.Queryable()
                .Where( av => publishGroupAttributeQry.Contains( av.AttributeId ) );

            var groupAttributeValueQry = attributeValueService.Queryable()
               .Where( av => groupAttributeQry.Contains( av.AttributeId ) );


            var qry = publishGroupService
                .Queryable( "Group" )
                .Where( pg => pg.PublishGroupStatus == PublishGroupStatus.Approved ) //Approved
                .Where( pg => pg.StartDateTime <= Rock.RockDateTime.Today && pg.EndDateTime >= Rock.RockDateTime.Today ) //Active
                .Where( pg => pg.Group.GroupType.GroupCapacityRule == GroupCapacityRule.None
                              || pg.Group.GroupCapacity == null || pg.Group.GroupCapacity == 0
                              || pg.Group.Members.Count() < pg.Group.GroupCapacity ); // Not full
            

            // Filter by campus
            if ( campusIds.Any() )
            {
                qry = qry
                    .Where( pg =>
                        !pg.Group.CampusId.HasValue || // All
                        campusIds.Contains( pg.Group.CampusId.Value ) );
            }

            // Filter by Category
            if ( categories.Any() )
            {
                qry = qry
                    .Where( pg => pg.AudienceValues
                        .Any( dv => categories.Contains( dv.Id ) ) );
            }

            //Group join in attributes
            var mixedQry = qry
                .GroupJoin( publishGroupAttributeValueQry,
                    pg => pg.Id,
                    av => av.EntityId,
                    ( pg, av ) => new { pg, av } )
                .GroupJoin( groupAttributeValueQry,
                    pg => pg.pg.GroupId,
                    av => av.EntityId,
                    ( pg, av ) => new { PublishGroup = pg.pg, PublishGroupAttributes = pg.av, GroupAttributes = av } );

            //Add in the attributes in the proper format
            var publishGroups = new List<PublishGroup>();

            foreach ( var item in mixedQry.ToList() )
            {
                var publishGroup = item.PublishGroup;
                publishGroup.AttributeValues = item.PublishGroupAttributes.ToDictionary( av => av.AttributeKey, av => new AttributeValueCache( av ) );
                publishGroup.Attributes = item.PublishGroupAttributes.ToDictionary( av => av.AttributeKey, av => AttributeCache.Get( av.AttributeId ) );
                publishGroup.Group.AttributeValues = item.GroupAttributes.ToDictionary( av => av.AttributeKey, av => new AttributeValueCache( av ) );
                publishGroup.Group.Attributes = item.GroupAttributes.ToDictionary( av => av.AttributeKey, av => AttributeCache.Get( av.AttributeId ) );
                publishGroups.Add( publishGroup );
            }

            // Output mergefields.
            var mergeFields = LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            mergeFields.Add( "PublishGroups", publishGroups );

            lOutput.Text = GetAttributeValue( "LavaTemplate" ).ResolveMergeFields( mergeFields );
        }

        #endregion

        #region Events

        // handlers called by the controls on your block

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindData();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblCategory control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblCategory_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindData();
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the cblCampus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void cblCampus_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindData();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private bool SetFilters()
        {
            //Check for Campus Parameter
            var campusId = PageParameter( GetAttributeValue( "CampusParameterName" ) ).AsIntegerOrNull();
            var campusStr = PageParameter( "Campus" );

            // Setup Campus Filter
            cblCampus.DataSource = CampusCache.All();
            cblCampus.DataBind();

            if ( campusId.HasValue )
            {
                //check if there's a campus with this id.
                var campus = CampusCache.Get( campusId.Value );
                if ( campus != null )
                {
                    cblCampus.SetValue( campusId.Value );
                }
            }
            else if ( !string.IsNullOrEmpty( campusStr ) )
            {
                //check if there's a campus with this name.
                campusStr = campusStr.Replace( " ", "" ).Replace( "-", "" );
                var campusCache = CampusCache.All().Where( c => c.Name.ToLower().Replace( " ", "" ).Replace( "-", "" ) == campusStr.ToLower() ).FirstOrDefault();
                if ( campusCache != null )
                {
                    cblCampus.SetValue( campusCache.Id );
                }
            }


            // Setup Category Filter
            var selectedCategoryGuids = GetAttributeValue( "FilterCategories" ).SplitDelimitedValues( true ).AsGuidList();
            rcwCategory.Visible = selectedCategoryGuids.Any() && GetAttributeValue( "CategoryFilterDisplayMode" ).AsInteger() > 1;
            var definedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.MARKETING_CAMPAIGN_AUDIENCE_TYPE.AsGuid() );
            if ( definedType != null )
            {
                var categoryItems = definedType.DefinedValues.ToList();
                if ( selectedCategoryGuids.Count() > 0 )
                {
                    categoryItems = definedType.DefinedValues.Where( v => selectedCategoryGuids.Contains( v.Guid ) ).ToList();
                }
                cblCategory.DataSource = categoryItems;
                cblCategory.DataBind();
            }

            var categoryId = PageParameter( GetAttributeValue( "CategoryParameterName" ) ).AsIntegerOrNull();
            var ministrySlug = PageParameter( "ministry" ).ToLower();
            if ( categoryId.HasValue )
            {
                if ( definedType.DefinedValues.Where( v => ( selectedCategoryGuids.Contains( v.Guid ) || selectedCategoryGuids.Count() == 0 ) && v.Id == categoryId.Value ).FirstOrDefault() != null )
                {
                    cblCategory.SetValue( categoryId.Value );
                }

            }
            else if ( !string.IsNullOrEmpty( ministrySlug ) )
            {
                var definedValues = definedType.DefinedValues.Where( v => ( selectedCategoryGuids.Contains( v.Guid ) || selectedCategoryGuids.Count() == 0 ) ).ToList();
                DefinedTypeService definedTypeService = new DefinedTypeService( new RockContext() );
                var definedTypeModel = definedTypeService.Get( definedType.Guid );
                foreach ( var dv in definedTypeModel.DefinedValues )
                {
                    dv.LoadAttributes();
                    if ( dv.GetAttributeValue( "URLSlug" ) == ministrySlug )
                    {
                        cblCategory.SetValue( dv.Id );
                        break;
                    }
                }

            }

            // Set filter visibility
            bool showFilter = ( rcwCampus.Visible || rcwCategory.Visible );
            pnlFilters.Visible = false; // hide for now... otherwise it should be managed by the block ^^^

            return true;
        }
        #endregion
    }
}