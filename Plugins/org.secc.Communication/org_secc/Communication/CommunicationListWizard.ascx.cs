using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Communication
{
    [DisplayName( "Communication List Wizard" )]
    [Category( "SECC > Communication" )]
    [Description( "Block for building a communication based on a list." )]
    public partial class CommunicationListWizard : RockBlock, ICustomGridColumns
    {
        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gList.GridRebind += gList_GridRebind;

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            dvDataView.EntityTypeId = EntityTypeCache.GetId( typeof( Person ) );
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            if ( ViewState["GroupId"] != null && ViewState["GroupId"] is int )
            {
                var groupMember = new GroupMember { GroupId = ( int ) ViewState["GroupId"] };
                groupMember.LoadAttributes();
                BindFilter( groupMember );
            }

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
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            BindGrid();
        }

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
            GroupService groupService = new GroupService( rockContext );
            int communicationListGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_COMMUNICATIONLIST.AsGuid() ).Id;

            var groups = groupService.Queryable()
                .Where( g => g.GroupTypeId == communicationListGroupTypeId && g.IsPublic && g.IsActive && !g.IsArchived )
                .ToList()
                .Select( g => new GroupPoco
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    Members = g.ActiveMembers().Count()
                } )
                .ToList();


            groups.OrderBy( g => g.Name ).ToList();

            gList.DataSource = groups;
            gList.DataBind();
        }

        #endregion

        protected void gList_RowSelected( object sender, RowEventArgs e )
        {
            var groupId = e.RowKeyId;
            var groupMember = new GroupMember { GroupId = groupId };

            groupMember.LoadAttributes();

            pnlFilter.Visible = true;
            pnlGrid.Visible = false;
            ViewState["GroupId"] = groupId;

            BindFilter( groupMember );
        }

        private void BindFilter( GroupMember groupMember )
        {
            foreach ( var attribute in groupMember.Attributes.Where( a => a.Value.IsGridColumn ).Select( a => a.Value ).ToList() )
            {
                Control control;

                // Check if this is a Campus or Campuses field type and override with multi-select picker
                if ( attribute.FieldType.Guid == Rock.SystemGuid.FieldType.CAMPUS.AsGuid() ||
                     attribute.FieldType.Guid == Rock.SystemGuid.FieldType.CAMPUSES.AsGuid() )
                {
                    var campusesPicker = new CampusesPicker();
                    campusesPicker.ID = "filter_" + attribute.Id.ToString();
                    campusesPicker.Label = attribute.Name;
                    campusesPicker.Help = attribute.Description;
                    campusesPicker.IncludeInactive = false;
                    phFilter.Controls.Add( campusesPicker );
                    continue;
                }

                control = attribute.FieldType.Field.FilterControl( attribute.QualifierValues, "filter_" + attribute.Id.ToString(), false, Rock.Reporting.FilterMode.SimpleFilter );
                if ( control != null )
                {
                    if ( control is IRockControl )
                    {
                        var rockControl = ( IRockControl ) control;
                        rockControl.Label = attribute.Name;
                        rockControl.Help = attribute.Description;
                        phFilter.Controls.Add( control );
                    }
                    else
                    {
                        var wrapper = new RockControlWrapper();
                        wrapper.ID = control.ID + "_wrapper";
                        wrapper.Label = attribute.Name;
                        wrapper.Controls.Add( control );
                        phFilter.Controls.Add( wrapper );
                    }
                }
            }
        }

        private void BuildCommunication( List<GroupMember> members )
        {
            members = members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Active ).ToList();
            if ( !members.Any() )
            {
                nbError.Text = "No members matched these filters.";
                return;
            }

            RockContext rockContext = new RockContext();
            CommunicationService communicationService = new CommunicationService( rockContext );
            var communication = new Rock.Model.Communication
            {
                IsBulkCommunication = true,
                Status = CommunicationStatus.Transient,
                SenderPersonAliasId = CurrentPersonAliasId
            };

            foreach ( var person in members.Select( m => m.Person ).ToList() )
            {
                communication.Recipients.Add( new CommunicationRecipient() { PersonAliasId = person.PrimaryAliasId.Value } );
            }

            communicationService.Add( communication );
            rockContext.SaveChanges();

            Response.Redirect( "/Communication/" + communication.Id.ToString() );
        }

        protected void btnSelect_Click( object sender, EventArgs e )
        {
            var groupId = ( int ) ViewState["GroupId"];

            //Make a fake group member so we can get the attributes to filter by
            var groupMember = new GroupMember { GroupId = groupId };
            groupMember.LoadAttributes();

            var attributes = groupMember.Attributes.Select( a => a.Value ).Where( a => a.IsGridColumn ).ToList();

            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );

            var qry = groupMemberService.Queryable()
                .Where( gm => gm.GroupId == groupId );

            foreach ( var attribute in attributes )
            {
                var filterControl = phFilter.FindControl( "filter_" + attribute.Id.ToString() );

                // Handle Campus (single) or Campuses (multi) field types with multi-select picker
                bool isCampusFieldType = attribute.FieldType.Guid == Rock.SystemGuid.FieldType.CAMPUS.AsGuid();
                bool isCampusesFieldType = attribute.FieldType.Guid == Rock.SystemGuid.FieldType.CAMPUSES.AsGuid();

                if ( ( isCampusFieldType || isCampusesFieldType ) && filterControl is CampusesPicker )
                {
                    var campusesPicker = ( CampusesPicker ) filterControl;
                    var selectedCampusIds = campusesPicker.SelectedCampusIds;
                    if ( selectedCampusIds.Any() )
                    {
                        // Convert selected campus IDs to GUID strings for attribute value comparison
                        var selectedCampusGuids = selectedCampusIds
                            .Select( id => CampusCache.Get( id )?.Guid.ToString() )
                            .Where( g => g != null )
                            .ToList();

                        // Get the entity type ID for GroupMember
                        int entityTypeId = EntityTypeCache.GetId( typeof( GroupMember ) ) ?? 0;

                        // Build a query for attribute values that contain any of the selected campus GUIDs
                        var attributeValueService = new AttributeValueService( rockContext );

                        // Start with base query for this attribute
                        var avBaseQuery = attributeValueService.Queryable()
                            .Where( av => av.Attribute.EntityTypeId == entityTypeId
                                && av.Attribute.Key == attribute.Key
                                && av.EntityId.HasValue );

                        // Build a single query that matches any of the selected campus GUIDs
                        var matchingEntityIds = avBaseQuery
                            .Where( av => selectedCampusGuids.Any( guid => av.Value.Contains( guid ) ) )
                            .Select( av => av.EntityId )
                            .Distinct();

                        qry = qry.Where( gm => matchingEntityIds.Contains( gm.Id ) );
                    }
                    continue;
                }

                qry = attribute.FieldType.Field.ApplyAttributeQueryFilter( qry, filterControl, attribute, groupMemberService, Rock.Reporting.FilterMode.SimpleFilter );
            }

            if ( dvDataView.SelectedValueAsId().HasValue )
            {
                var dataviewId = dvDataView.SelectedValueAsId().Value;

                DataViewService dataViewService = new DataViewService( rockContext );
                var dataView = dataViewService.Get( dataviewId );

                if ( dataView != null && dataView.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
                {
                    var dvQuery = dataView.GetQuery( new DataViewGetQueryArgs { DbContext = rockContext, DatabaseTimeoutSeconds = 60 } );
                    qry = qry.Where( m => dvQuery.Select( p => p.Id ).Contains( m.PersonId ) );
                }
            }

            BuildCommunication( qry.ToList() );
        }


        protected void btnAnalytics_Click( object sender, RowEventArgs e )
        {
            Response.Redirect( "/EmailAnalytics?CommunicationListId=" + e.RowKeyValue );
        }

        private class GroupPoco
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public string Description { get; set; }
            public int Members { get; set; }
        }
    }
}