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
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace org.secc.Connection
{
    /// <summary>
    /// Block that can load sample data into your Rock database.
    /// Dev note: You can set the XML Document Url setting to your local
    /// file when you're testing new data.  Something like C:\Misc\Rock\Documentation\sampledata.xml
    /// </summary>
    [DisplayName( "Volunteer Signup Block" )]
    [Category( "SECC > Connection" )]
    [Description( "Block for configuring and outputing a configurable page for setting numbers of volunteers needed." )]
    
    [CodeEditorField("Settings", mode:CodeEditorMode.JavaScript, category: "Custom Setting")]
    [KeyValueListField("Counts", category: "Custom Setting" )]
    [CodeEditorField( "Lava", mode: CodeEditorMode.JavaScript, category: "Custom Setting" )]
    [BooleanField( "Enable Debug", "Display a list of merge fields available for lava.", false)]

    [ViewStateModeById]
    public partial class Signup : Rock.Web.UI.RockBlockCustomSettings
    {

        private Dictionary<string, string> attributes = new Dictionary<string, string>();

        private ICollection<ConnectionRequest> connectionRequests = null;


        protected SignupSettings Settings {
            get {
                var settings = GetSetting<SignupSettings>("Settings");
                foreach(var p in settings.Partitions)
                {
                    p.SignupSettings = settings;
                }
                return settings;
                }
            set {
                ViewState["Settings"] = value;
                SaveViewState();
            }
        }
        
        protected Dictionary<string, string> Counts
        {
            get
            {
                return GetSetting<Dictionary<string, string>>( "Counts" );
            }
            set
            {
                ViewState["Counts"] = value;
                SaveViewState();
            }
        }

        private T GetSetting<T>( string key ) where T: new()
        {

            if ( ViewState[key] != null )
            {
                try
                {
                    return ( T ) ViewState[key];
                }
                catch ( Exception )
                {
                    // Just swallow this exception
                }
            }
            if ( !string.IsNullOrWhiteSpace(GetAttributeValue( key )) )
            {
                try
                {
                    if (BlockCache.Attributes[key].FieldType.Guid == Rock.SystemGuid.FieldType.KEY_VALUE_LIST.AsGuid())
                    {
                        ViewState[key] = GetAttributeValue( key ).AsDictionary();
                    }
                    else
                    {
                        ViewState[key] = GetAttributeValue( key ).FromJsonOrNull<T>();
                    }
                }
                catch ( Exception )
                {
                    // Just swallow this exception
                }
            }
            if ( ViewState[key] == null )
            {
                ViewState[key] = new T();
            }
            SaveViewState();
            return (T)ViewState[key];
        }


        #region Base Control Methods

        ////  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            LoadSettings();
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                ceLava.Text = GetAttributeValue( "Lava" );
            }
            
            if ( Settings.Partitions.Count > 0)
            {
                ConnectionOpportunity connection = ( ConnectionOpportunity ) Settings.Entity();
                if ( connection != null && connectionRequests == null)
                {
                    connectionRequests = connection.ConnectionRequests;
                }

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                mergeFields.Add( "Settings", Rock.Lava.RockFilters.FromJSON( GetAttributeValue( "Settings" ) ) );
                mergeFields.Add( "Tree", GetTree( Settings.Partitions.FirstOrDefault(), new GroupTypeRoleService( new RockContext() ), connectionRequests ) );
                mergeFields.Add( "ConnectionRequests", connectionRequests );
                lBody.Text = GetAttributeValue( "Lava" ).ResolveMergeFields(mergeFields);

                if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
                {
                    lDebug.Visible = true;
                    lDebug.Text = mergeFields.lavaDebugInfo();
                }
            }
        }

        protected override void OnPreRender( EventArgs e )
        {
            bddlAddPartition.SelectedValue = "";
        }

        #endregion

        
        /// <summary>
        /// Shows the settings.
        /// </summary>
        protected override void ShowSettings()
        {
            if ( Settings.Partitions.Count > 0 )
            {
                deactivateTabs();
                liCounts.AddCssClass( "active" );
                pnlCounts.Visible = true;
            }

            var connectionOpportunityService = new ConnectionOpportunityService( new RockContext() );
            var connections = connectionOpportunityService.Queryable().Where( co => co.IsActive == true ).OrderBy( co => co.ConnectionType.Name ).ThenBy( co => co.Name ).ToList()
                                                                    .Select( co => new ListItem( co.ConnectionType.Name + ": " + co.Name, co.Guid.ToString() ) ).ToList();
            connections.Insert( 0, new ListItem( "Select One . . ." ) );
            rddlConnection.DataSource = connections;
            rddlConnection.DataTextField = "Text";
            rddlConnection.DataValueField = "Value";
            rddlConnection.DataBind();

            if ( Settings.EntityGuid != Guid.Empty )
            {
                rddlConnection.SelectedValue = Settings.EntityGuid.ToString();
            }

            mdEdit.Show();
        }

        private void UpdateCounts()
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach ( GridViewRow gridRow in gCounts.Rows )
            {
                string rowId = gCounts.DataKeys[gridRow.RowIndex].Value.ToString();

                RockTextBox textBox = ( RockTextBox ) gridRow.FindControl( "tb" + gridRow.ID );
                if (textBox != null)
                {
                    if ( values.ContainsKey( rowId ) )
                    {
                        values[rowId] = textBox.Text;
                    }
                    else
                    {
                        values.Add( rowId, textBox.Text );
                    }
                }
            }
            Counts = values;
            SaveViewState();
        }

        private void LoadSettings()
        {


            using ( var context = new RockContext() )
            {
                int groupMemberEntityId = EntityTypeCache.Read( Rock.SystemGuid.EntityType.GROUP_MEMBER.AsGuid() ).Id;
                if ( Settings.EntityTypeGuid == Rock.SystemGuid.EntityType.CONNECTION_OPPORTUNITY.AsGuid() )
                {
                    AttributeService attributeService = new AttributeService( new RockContext() );
                    ConnectionOpportunity connection = ( ConnectionOpportunity ) Settings.Entity();
                    if ( connection != null )
                    {
                        connectionRequests = connection.ConnectionRequests;

                        var groupTypes = connection.ConnectionOpportunityGroupConfigs.Where( cogc => cogc.GroupType != null && cogc.UseAllGroupsOfType == true ).Select( gt => gt.GroupType ).Distinct().ToList();
                        var groups = connection.ConnectionOpportunityGroups.Select( cog => cog.Group ).Distinct().ToList();
                        foreach ( GroupType groupType in groupTypes )
                        {
                            var gmAttributes = attributeService.Queryable().Where( a => a.EntityTypeQualifierColumn == "GroupTypeId" && a.EntityTypeQualifierValue == groupType.Id.ToString() );
                            foreach ( var attribute in gmAttributes )
                            {
                                if ( attributes.ContainsKey( attribute.Key ) )
                                {
                                    attributes[attribute.Key] = attributes[attribute.Key].ReplaceLastOccurrence( ")", "" ) + ", " + groupType.Name + " Group Type)";
                                }
                                else
                                {
                                    attributes.Add( attribute.Key, attribute.Name + " (" + groupType.Name + " Group Type)" );
                                }
                            }
                        }
                    
                        foreach ( Group group in groups )
                        {
                            var gmAttributes = attributeService.Queryable().Where( a => a.EntityTypeId == groupMemberEntityId && a.EntityTypeQualifierColumn == "GroupId" && a.EntityTypeQualifierValue == group.Id.ToString() );
                            
                            foreach ( var attribute in gmAttributes )
                            {
                                if ( attributes.ContainsKey( attribute.Key ) )
                                {
                                    attributes[attribute.Key] = attributes[attribute.Key].ReplaceLastOccurrence( ")", "" ) + ", " + group.Name + " Group)";
                                }
                                else
                                {
                                    attributes.Add( attribute.Key, attribute.Name + " (" + group.Name + " Group)" );
                                }
                            }
                        }
                    }
                }

                // Load all the partition settings
                if ( Settings.EntityGuid != Guid.Empty )
                {
                    pnlPartitions.Visible = true;
                }

                rptPartions.DataSource = Settings.Partitions;
                rptPartions.DataBind();

                // Remove all existing dynamic columns
                while ( gCounts.Columns.Count  > 1)
                {
                    gCounts.Columns.RemoveAt( 0 );
                }
                DataTable dt = new DataTable();
                dt.Columns.Add( "RowId" );
                foreach ( var partition in Settings.Partitions )
                {
                    DataTable dtTmp = dt.Copy();
                    dt.Clear();
                    String column = partition.PartitionType;
                    if ( partition.PartitionType == "DefinedType" )
                    {
                        var definedType = Rock.Web.Cache.DefinedTypeCache.Read( partition.PartitionValue.AsGuid() );
                        if ( definedType == null)
                        {
                            break;
                        }
                        column = definedType.Name;
                    }
                    var boundField = new BoundField() { HeaderText = column, DataField = column + partition.Guid };
                    gCounts.Columns.Insert( gCounts.Columns.Count-1, boundField );
                    dt.Columns.Add( column + partition.Guid );
                    
                    switch ( partition.PartitionType )
                    {
                        case "DefinedType":

                            var definedType = Rock.Web.Cache.DefinedTypeCache.Read( partition.PartitionValue.AsGuid() );
                            foreach ( var value in definedType.DefinedValues )
                            {
                                AddRowColumnPartition( dtTmp, dt, column + partition.Guid, value.Guid, value.Value );
                            }
                            break;
                        case "Campus":
                            var selectedCampuses = partition.PartitionValue.Split( ',' );
                            foreach ( string campusGuid in selectedCampuses )
                            {
                                var campus = CampusCache.Read( campusGuid.AsGuid() );
                                if (campus!= null)
                                {
                                    AddRowColumnPartition( dtTmp, dt, column + partition.Guid, campus.Guid, campus.Name );
                                }
                            }
                            break;
                        case "Schedule":
                            break;
                        case "Role":
                            GroupTypeRoleService groupTypeRoleService = new GroupTypeRoleService( context );
                            var selectedRoles = partition.PartitionValue.Split( ',' );
                            foreach ( string roleGuid in selectedRoles )
                            {
                                GroupTypeRole role = groupTypeRoleService.Get( roleGuid.AsGuid() );

                                if (role != null)
                                {
                                    AddRowColumnPartition( dtTmp, dt, column + partition.Guid, role.Guid, role.Name );
                                }
                            }
                            break;
                    }
                }
                if ( Settings.Partitions.Count > 0 && dt.Rows.Count > 0 )
                {
                    var dv = dt.AsEnumerable();
                    var dvOrdered = dv.OrderBy( r => r.Field<String>( dt.Columns.Cast<DataColumn>().Select( c => c.ColumnName ).Skip( 1 ).FirstOrDefault() ) );
                    foreach ( var column in dt.Columns.Cast<DataColumn>().Select( c => c.ColumnName ).Skip( 2 ) )
                    {
                        dvOrdered = dvOrdered.ThenBy( r => r.Field<String>( column ) );
                        break;
                    }
                    dt = dvOrdered.CopyToDataTable();

                    gCounts.DataSource = dt;
                    gCounts.DataBind();
                }

                if ( Settings.EntityTypeGuid == Rock.SystemGuid.EntityType.CONNECTION_OPPORTUNITY.AsGuid() )
                {
                    rddlType.SelectedValue = "Connection";
                    rddlConnection.Visible = true;
                }
                else if ( Settings.EntityTypeGuid == Rock.SystemGuid.EntityType.GROUP.AsGuid() )
                {
                    rddlType.SelectedValue = "Group";
                    gpGroup.Visible = true;
                }

            }
        }

        private void AddRowColumnPartition(DataTable source, DataTable target, String columnKey, Guid guid, String value)
        {
            if ( source.Rows.Count == 0 )
            {
                var newRow = target.NewRow();
                newRow["RowId"] = guid.ToString();
                target.Rows.Add( newRow );
                newRow[columnKey] = value;
            }
            foreach ( DataRow rowTmp in source.Rows )
            {
                var newRow = target.NewRow();
                foreach ( DataColumn columnTmp in source.Columns )
                {
                    newRow[columnTmp.ColumnName] = rowTmp[columnTmp.ColumnName];
                }
                newRow["RowId"] = rowTmp["RowId"] + "," + guid.ToString();
                newRow[columnKey] = value;
                target.Rows.Add( newRow );
            }
        }

        
        [Serializable]
        public class PartitionSettings
        {
            private String attributeKey;
            public string PartitionType { get; set; }
            public string PartitionValue { get; set; }
            public Guid Guid { get; set; }

            [JsonIgnore]
            public SignupSettings SignupSettings { get; set; }
            
            public PartitionSettings NextPartition { get
                {
                    if ( SignupSettings != null)
                    {
                        return SignupSettings.Partitions.SkipWhile( p => p != this ).Skip( 1 ).FirstOrDefault();
                    }
                    return null;
                }
            }
        }

        [Serializable]
        public class SignupSettings
        {
            private List<PartitionSettings> _partitions = new List<PartitionSettings>();

            public List<PartitionSettings> Partitions { get { return _partitions; } }

            public Guid EntityTypeGuid { get; set; }

            public Guid EntityGuid { get; set; }

            /// <summary>
            /// Get the entity (group or connection) for this signup
            /// </summary>
            /// <param name="context"></param>
            /// <returns></returns>
            public virtual IModel Entity (RockContext context = null)
            {
                if (context == null)
                {
                    context = new RockContext();
                }
                if ( EntityTypeGuid == Rock.SystemGuid.EntityType.CONNECTION_OPPORTUNITY.AsGuid() )
                {
                    ConnectionOpportunityService connectionOpportunityService = new ConnectionOpportunityService( context );
                    return connectionOpportunityService.Get( EntityGuid );
                }

                if ( EntityTypeGuid == Rock.SystemGuid.EntityType.GROUP.AsGuid() )
                {
                    GroupService groupService = new GroupService( context );
                    return groupService.Get( EntityGuid );
                }
                return null;
            }
            
        }

        #region Events

        protected void lbSettings_Click( object sender, EventArgs e )
        {
            UpdateCounts();
            deactivateTabs();
            liSettings.AddCssClass( "active" );
            pnlSettings.Visible = true;
        }

        protected void lbCounts_Click( object sender, EventArgs e )
        {
            UpdateCounts();
            deactivateTabs();
            liCounts.AddCssClass( "active" );
            pnlCounts.Visible = true;

        }

        protected void lbLava_Click( object sender, EventArgs e )
        {
            UpdateCounts();
            deactivateTabs();
            liLava.AddCssClass( "active" );
            pnlLava.Visible = true;
        }

        private void deactivateTabs()
        {
            liSettings.RemoveCssClass( "active" );
            liCounts.RemoveCssClass( "active" );
            liLava.RemoveCssClass( "active" );
            pnlSettings.Visible = false;
            pnlCounts.Visible = false;
            pnlLava.Visible = false;

        }

        protected void rptPartions_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            if ( e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem )
            {

                var ddlAttribute = ( DropDownList ) e.Item.FindControl( "ddlAttribute" );
                ddlAttribute.DataSource = attributes;
                ddlAttribute.DataTextField = "Value";
                ddlAttribute.DataValueField = "Key";
                ddlAttribute.DataBind();
                

                var partition = ( ( PartitionSettings ) e.Item.DataItem );
                var phPartitionControl = ( PlaceHolder ) e.Item.FindControl( "phPartitionControl" );
                switch ( partition.PartitionType )
                {
                    case "Campus":
                        ddlAttribute.Visible = false;
                        var campuses = new CheckBoxList() { ID = partition.Guid.ToString() };
                        var campusCache = Rock.Web.Cache.CampusCache.All();
                        campuses.DataSource = campusCache;
                        campuses.DataTextField = "Name";
                        campuses.DataValueField = "Guid";
                        campuses.DataBind();
                        if ( !string.IsNullOrWhiteSpace(partition.PartitionValue) )
                        {
                            campuses.SetValues(partition.PartitionValue.Split( ',' ).Select( pv => pv ).ToList());
                        }
                        else
                        {
                            campuses.SetValues( campusCache.Select( c => c.Guid.ToString() ).ToList() );
                            partition.PartitionValue = string.Join( ",", campusCache.Select( c => c.Guid ).ToArray() );
                        }
                        campuses.AutoPostBack = true;
                        campuses.SelectedIndexChanged += Cbl_SelectedIndexChanged;
                        phPartitionControl.Controls.Add( campuses );
                        break;
                    case "Schedule":
                        var schedule = new SchedulePicker() { AllowMultiSelect = true, ID = partition.Guid.ToString() };
                        schedule.SelectItem += Schedule_SelectItem;
                        if ( !string.IsNullOrWhiteSpace(partition.PartitionValue) )
                        {
                            schedule.SetValues(partition.PartitionValue.Split(',').Select( pv => pv.AsInteger()));
                        }
                        phPartitionControl.Controls.Add( schedule );
                        break;
                    case "DefinedType":
                        var definedTypeRddl = new RockDropDownList() { ID = partition.Guid.ToString() };
                        DefinedTypeService definedTypeService = new DefinedTypeService( new RockContext() );
                        var listItems = definedTypeService.Queryable().Select( dt => new { Name = ( dt.Category != null ? dt.Category.Name + ": " : "" ) + dt.Name, Guid = dt.Guid } ).ToList();
                        listItems.Insert( 0, new { Name = "Select One . . .", Guid = Guid.Empty } );
                        definedTypeRddl.DataSource = listItems;
                        definedTypeRddl.DataTextField = "Name";
                        definedTypeRddl.DataValueField = "Guid";
                        definedTypeRddl.DataBind();
                        definedTypeRddl.AutoPostBack = true;
                        definedTypeRddl.SelectedIndexChanged += DefinedTypeRddl_SelectedIndexChanged;
                        if (!string.IsNullOrWhiteSpace(partition.PartitionValue))
                        {
                            definedTypeRddl.SelectedValue = partition.PartitionValue;
                        }
                        phPartitionControl.Controls.Add( definedTypeRddl );
                        break;
                    case "Role":
                        ddlAttribute.Visible = false;
                        var cblRole = new CheckBoxList() { ID = partition.Guid.ToString() };
                        if ( Settings.EntityTypeGuid == Rock.SystemGuid.EntityType.CONNECTION_OPPORTUNITY.AsGuid() )
                        {
                            ConnectionOpportunity connection = ( ConnectionOpportunity ) Settings.Entity();
                            var roles = connection.ConnectionOpportunityGroupConfigs.Where( cogc => cogc.GroupMemberRole != null).Select( r => new {Name = r.GroupType.Name + ": " + r.GroupMemberRole.Name, Guid = r.GroupMemberRole.Guid } ).ToList();
                            cblRole.DataSource = roles;
                            cblRole.DataTextField = "Name";
                            cblRole.DataValueField = "Guid";
                            cblRole.DataBind();
                            if ( !string.IsNullOrWhiteSpace(partition.PartitionValue) )
                            {
                                cblRole.SetValues( partition.PartitionValue.Split( ',' ).ToList() );
                            }
                            else
                            {
                                cblRole.SetValues( roles.Select( c => c.Guid.ToString() ).ToList() );
                                partition.PartitionValue = string.Join( ",", roles.Select( c => c.Guid.ToString() ).ToArray() );
                            }
                            cblRole.SelectedIndexChanged += Cbl_SelectedIndexChanged;
                        }
                        phPartitionControl.Controls.Add( cblRole );
                        break;
                }

                ( ( LinkButton ) e.Item.FindControl( "bbPartitionDelete" ) ).CommandArgument = partition.Guid.ToString();
            }
            
        }

        private void Cbl_SelectedIndexChanged( object sender, EventArgs e )
        {
            var partition = Settings.Partitions.Where( p => p.Guid == ( ( Control ) sender ).ID.AsGuid() ).FirstOrDefault();
            if ( partition != null )
            {
                partition.PartitionValue = String.Join( ",",
                        ( ( CheckBoxList ) sender ).Items.Cast<ListItem>().Where( i => i.Selected ).Select( i => i.Value )
                        );
            }
            SaveViewState();
        }

        protected void BddlAddParition_SelectionChanged( object sender, EventArgs e )
        {
            if ( Counts.Count > 0 )
            {
                hdnPartitionType.Value = ( ( ButtonDropDownList ) sender ).SelectedValue;
                ScriptManager.RegisterStartupScript( upEditControls, upEditControls.GetType(), "PartitionWarning", "Rock.dialogs.confirm('Making changes to partition settings will clear all existing counts!  Are you sure you want to proceed?', function(result) {if(result) {$(\"#" + btnAddPartition.ClientID + "\")[0].click();}});", true );
                return;
            }
            var partition = new PartitionSettings() { PartitionType = ( ( ButtonDropDownList ) sender ).SelectedValue, Guid = Guid.NewGuid(), SignupSettings = Settings };
            Settings.Partitions.Add( partition );
            SaveViewState();
            rptPartions.DataSource = Settings.Partitions;
            rptPartions.DataBind();
        }

        protected void btnAddPartition_Click( object sender, EventArgs e )
        {
            var partition = new PartitionSettings() { PartitionType = hdnPartitionType.Value, Guid = Guid.NewGuid(), SignupSettings = Settings };
            Settings.Partitions.Add( partition );
            SaveViewState();
            rptPartions.DataSource = Settings.Partitions;
            rptPartions.DataBind();
        }

        protected void bbPartitionDelete_Click( object sender, EventArgs e )
        {
            Settings.Partitions.Remove( Settings.Partitions.Where( p => p.Guid == ( ( LinkButton ) sender ).CommandArgument.AsGuid() ).FirstOrDefault() );
            SaveViewState();
            rptPartions.DataSource = Settings.Partitions;
            rptPartions.DataBind();
        }

        protected void GPicker_SelectItem( object sender, EventArgs e )
        {
            int groupId = ( ( GroupPicker ) ( ( HtmlAnchor ) sender ).Parent ).SelectedValue.AsInteger();
            GroupService groupService = new GroupService( new RockContext() );
            Settings.EntityGuid = groupService.Get( groupId ).Guid;
        }

        protected void ConnectionRddl_SelectedIndexChanged( object sender, EventArgs e )
        {
            Settings.EntityGuid = ( ( RockDropDownList ) sender ).SelectedValue.AsGuid();
            SaveViewState();
            pnlPartitions.Visible = true;
        }

        protected void RddlType_SelectedIndexChanged( object sender, EventArgs e )
        {
            gpGroup.Visible = false;
            rddlConnection.Visible = false;
            if ( ( ( RockDropDownList ) sender ).SelectedValue == "Group" )
            {
                gpGroup.Visible = true;
                Settings.EntityTypeGuid = Rock.SystemGuid.EntityType.GROUP.AsGuid();
            }
            else if ( ( ( RockDropDownList ) sender ).SelectedValue == "Connection" )
            {
                rddlConnection.Visible = true;
                Settings.EntityTypeGuid = Rock.SystemGuid.EntityType.CONNECTION_OPPORTUNITY.AsGuid();
            }
            SaveViewState();
        }


        protected void mdEdit_SaveClick( object sender, EventArgs e )
        {
            UpdateCounts();
            SetAttributeValue( "Settings", Settings.ToJson() );
            SetAttributeValue( "Counts", string.Join( "|", Counts.Select( a => a.Key + "^" + a.Value ).ToList() ) );
            SetAttributeValue( "Lava", ceLava.Text );
            SaveAttributeValues();
            mdEdit.Hide();
        }

        private void Schedule_SelectItem( object sender, EventArgs e )
        {
            var partition = Settings.Partitions.Where( p => p.Guid == ( ( Control ) sender ).Parent.ID.AsGuid() ).FirstOrDefault();
            if ( partition != null )
            {
                partition.PartitionValue = String.Join(",", ( ( SchedulePicker ) ( ( Control ) sender ).Parent ).SelectedValues);
            }
            SaveViewState();
        }

        private void DefinedTypeRddl_SelectedIndexChanged( object sender, EventArgs e )
        {
            var partition = Settings.Partitions.Where( p => p.Guid == ( ( Control ) sender ).ID.AsGuid() ).FirstOrDefault();
            if (partition != null)
            {
                partition.PartitionValue = ( ( RockDropDownList ) sender ).SelectedValue;
            }
            SaveViewState();
        }

        protected void gCounts_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                string rowId = gCounts.DataKeys[e.Row.RowIndex].Value.ToString();
                RockTextBox textBox = new RockTextBox();
                textBox.Width = 60;
                textBox.Text = "";
                textBox.ID = "tb" + e.Row.ID;
                if ( Counts.ContainsKey( rowId ) )
                {
                    textBox.Text = Counts[rowId];
                }
                e.Row.Cells[gCounts.Columns.Count-1].Controls.Add( textBox );
            }
        }

        protected List<IDictionary<string, object>> GetTree( PartitionSettings partition, GroupTypeRoleService groupTypeRoleService, ICollection<ConnectionRequest> connectionRequests = null, String concatGuid = null )
        {
            var partitionList = new List<IDictionary<string, object>>();
            if ( partition.PartitionValue == null)
            {
                return null;
            }
            var values =  partition.PartitionValue.Split( ',' );

            if (partition.PartitionType == "DefinedType")
            {
                // Use every Defined Value 
                values = DefinedTypeCache.Read( partition.PartitionValue.AsGuid() ).DefinedValues.Select( dv => dv.Guid.ToString() ).ToArray();
            }

            foreach ( var value in values )
            {
                IDictionary<string, object> inner = new Dictionary<string, object>();
                inner.Add( "Value", value );
                var newConcatGuid = concatGuid == null ? value : concatGuid + "," + value;
                inner.Add( "Limit", Counts.Where( kvp => kvp.Key.Contains( newConcatGuid ) ).Select( kvp => kvp.Value.AsInteger() ).Sum() );
                ICollection<ConnectionRequest> subRequests = null;
                switch ( partition.PartitionType )
                {
                    case "DefinedType":
                        inner["Entity"] = DefinedValueCache.Read( value.AsGuid() );
                        if ( connectionRequests != null)
                        {
                            subRequests = connectionRequests.Where( cr => cr.AssignedGroupMemberAttributeValues != null && cr.AssignedGroupMemberAttributeValues.Contains( value ) ).ToList();

                            inner.Add( "TotalFilled", this.connectionRequests.Where( cr => cr.AssignedGroupMemberAttributeValues != null && cr.AssignedGroupMemberAttributeValues.Contains( value ) && cr.ConnectionState != ConnectionState.Inactive ).Count() );
                        }
                        break;
                    case "Campus":
                        var campus = CampusCache.Read( value.AsGuid() );
                        inner["Entity"] = campus;
                        if ( connectionRequests != null )
                        {
                            subRequests = connectionRequests.Where( cr => cr.CampusId == campus.Id ).ToList();
                            inner.Add( "TotalFilled", this.connectionRequests.Where( cr => cr.CampusId == campus.Id && cr.ConnectionState != ConnectionState.Inactive ).Count() );

                        }
                        break;
                    case "Role":
                        var role = groupTypeRoleService.Get( value.AsGuid() );
                        inner["Entity"] = role;
                        if ( connectionRequests != null )
                        {
                            subRequests = connectionRequests.Where( cr => cr.AssignedGroupMemberRoleId == role.Id ).ToList();
                            inner.Add( "TotalFilled", this.connectionRequests.Where( cr => cr.AssignedGroupMemberRoleId == role.Id && cr.ConnectionState != ConnectionState.Inactive ).Count() );
                        }
                        break;
                }

                inner.Add( "Filled", subRequests != null?subRequests.Where(sr => sr.ConnectionState != ConnectionState.Inactive).Count():0 );
                        
                if ( partition.NextPartition != null) {
                    inner.Add( "Partitions", GetTree( partition.NextPartition, groupTypeRoleService, subRequests, newConcatGuid ) );
                }
                partitionList.Add( inner );
            }
            return partitionList;
        }

        #endregion

        
    }
}