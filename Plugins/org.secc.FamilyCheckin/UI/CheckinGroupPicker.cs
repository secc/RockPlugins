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
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;


namespace org.secc.FamilyCheckin.UI
{
    /// <summary>
    /// 
    /// </summary>
    public class CheckinGroupPicker : CompositeControl, IRockControl
    {
        #region IRockControl implementation

        /// <summary>
        /// Gets or sets the label text.
        /// </summary>
        /// <value>
        /// The label text.
        /// </value>
        [
        Bindable( true ),
        Category( "Appearance" ),
        DefaultValue( "" ),
        Description( "The text for the label." )
        ]
        public string Label
        {
            get { return ViewState["Label"] as string ?? string.Empty; }
            set { ViewState["Label"] = value; }
        }

        /// <summary>
        /// Gets or sets the form group class.
        /// </summary>
        /// <value>
        /// The form group class.
        /// </value>
        [
        Bindable( true ),
        Category( "Appearance" ),
        Description( "The CSS class to add to the form-group div." )
        ]
        public string FormGroupCssClass
        {
            get { return ViewState["FormGroupCssClass"] as string ?? string.Empty; }
            set { ViewState["FormGroupCssClass"] = value; }
        }

        /// <summary>
        /// Gets or sets the help text.
        /// </summary>
        /// <value>
        /// The help text.
        /// </value>
        [
        Bindable( true ),
        Category( "Appearance" ),
        DefaultValue( "" ),
        Description( "The help block." )
        ]
        public string Help
        {
            get
            {
                return HelpBlock != null ? HelpBlock.Text : string.Empty;
            }

            set
            {
                if ( HelpBlock != null )
                {
                    HelpBlock.Text = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the warning text.
        /// </summary>
        /// <value>
        /// The warning text.
        /// </value>
        [
        Bindable( true ),
        Category( "Appearance" ),
        DefaultValue( "" ),
        Description( "The warning block." )
        ]
        public string Warning
        {
            get
            {
                return WarningBlock != null ? WarningBlock.Text : string.Empty;
            }

            set
            {
                if ( WarningBlock != null )
                {
                    WarningBlock.Text = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RockTextBox"/> is required.
        /// </summary>
        /// <value>
        ///   <c>true</c> if required; otherwise, <c>false</c>.
        /// </value>
        [
        Bindable( true ),
        Category( "Behavior" ),
        DefaultValue( "false" ),
        Description( "Is the value required?" )
        ]
        public bool Required
        {
            get { return ViewState["Required"] as bool? ?? false; }
            set { ViewState["Required"] = value; }
        }

        /// <summary>
        /// Gets or sets the required error message.  If blank, the LabelName name will be used
        /// </summary>
        /// <value>
        /// The required error message.
        /// </value>
        public string RequiredErrorMessage
        {
            get
            {
                return RequiredFieldValidator != null ? RequiredFieldValidator.ErrorMessage : string.Empty;
            }

            set
            {
                if ( RequiredFieldValidator != null )
                {
                    RequiredFieldValidator.ErrorMessage = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets an optional validation group to use.
        /// </summary>
        /// <value>
        /// The validation group.
        /// </value>
        public string ValidationGroup
        {
            get { return ViewState["ValidationGroup"] as string; }
            set { ViewState["ValidationGroup"] = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsValid
        {
            get
            {
                return !Required || RequiredFieldValidator == null || RequiredFieldValidator.IsValid;
            }
        }

        /// <summary>
        /// Gets or sets the help block.
        /// </summary>
        /// <value>
        /// The help block.
        /// </value>
        public HelpBlock HelpBlock { get; set; }

        /// <summary>
        /// Gets or sets the warning block.
        /// </summary>
        /// <value>
        /// The warning block.
        /// </value>
        public WarningBlock WarningBlock { get; set; }

        /// <summary>
        /// Gets or sets the required field validator.
        /// </summary>
        /// <value>
        /// The required field validator.
        /// </value>
        public RequiredFieldValidator RequiredFieldValidator { get; set; }

        #endregion

        private Panel groupsDisplay;
        private RockDropDownList templateDropDownList;
        private RockDropDownList groupTypeDropDownList;
        private RockDropDownList groupDropDownList;
        private BootstrapButton addButton;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckinGroupPicker"/> class.
        /// </summary>
        public CheckinGroupPicker()
            : base()
        {
            HelpBlock = new HelpBlock();
            WarningBlock = new WarningBlock();
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Controls.Clear();
            CreateChildControls( this, Controls );

            groupsDisplay = new Panel();
            groupsDisplay.CssClass = "panel panel-default";

            templateDropDownList = new RockDropDownList();
            templateDropDownList.Label = "Check-in Template";
            templateDropDownList.ID = "templateDropDownList_" + this.ID;
            templateDropDownList.CssClass = "input-sm";
            templateDropDownList.DataTextField = "Name";
            templateDropDownList.DataValueField = "Id";
            templateDropDownList.AutoPostBack = true;
            templateDropDownList.SelectedIndexChanged += templateDropDownList_SelectedIndexChanged;

            groupTypeDropDownList = new RockDropDownList();
            groupTypeDropDownList.Visible = false;
            groupTypeDropDownList.Label = "Check-in Area";
            groupTypeDropDownList.ID = "groupTypeDropDownList_" + this.ID;
            groupTypeDropDownList.CssClass = "input-sm";
            groupTypeDropDownList.DataTextField = "Name";
            groupTypeDropDownList.DataValueField = "Id";
            groupTypeDropDownList.AutoPostBack = true;
            groupTypeDropDownList.SelectedIndexChanged += groupTypeDropDownList_SelectedIndexChanged;

            groupDropDownList = new RockDropDownList();
            groupDropDownList.Visible = false;
            groupDropDownList.Label = "Check-in Group";
            groupDropDownList.ID = "groupDropDownList_" + this.ID;
            groupDropDownList.CssClass = "input-sm";
            groupDropDownList.DataTextField = "Name";
            groupDropDownList.DataValueField = "Id";
            groupDropDownList.AutoPostBack = true;
            groupDropDownList.SelectedIndexChanged += groupDropDownList_SelectedIndexChanged;

            addButton = new BootstrapButton();
            addButton.Text = "Add Group";
            addButton.Visible = false;
            addButton.CssClass = "btn btn-primary btn-block clearfix";
            addButton.Click += addButton_Click;

            Controls.Add( groupsDisplay );
            Controls.Add( templateDropDownList );
            Controls.Add( groupTypeDropDownList );
            Controls.Add( groupDropDownList );
            Controls.Add( addButton );

            BindTemplateDropDown();
            UpdateGroupsDisplay();
        }

        /// <summary>
        /// Updates the picker when the template drop down is updated
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void templateDropDownList_SelectedIndexChanged( object sender, EventArgs e )
        {
            groupTypeDropDownList.Visible = false;
            groupDropDownList.Visible = false;
            addButton.Visible = false;

            var templateId = templateDropDownList.SelectedValue.AsInteger();
            if ( templateId != 0 )
            {
                RockContext rockContext = new RockContext();
                var template = new GroupTypeService( rockContext ).Get( templateId );
                if ( template != null )
                {
                    BindGroupTypeDropDown( template );
                }
                else
                {
                    //If the user was able to select a template that doesn't exist the ddl is probably stale
                    BindTemplateDropDown();
                }
            }
            UpdateGroupsDisplay();
        }

        /// <summary>
        /// Updates the picker when the groupType drop down is updated
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void groupTypeDropDownList_SelectedIndexChanged( object sender, EventArgs e )
        {
            groupDropDownList.Visible = false;
            addButton.Visible = false;

            var groupTypeId = groupTypeDropDownList.SelectedValue.AsInteger();

            if ( groupTypeId != 0 )
            {
                RockContext rockContext = new RockContext();
                var groupType = new GroupTypeService( rockContext ).Get( groupTypeId );
                if ( groupType != null )
                {
                    BindGroupDropDown( groupType );
                }
                else
                {
                    //If the user was able to select a groupType that doesn't exist the ddl is probably stale
                    BindTemplateDropDown();
                }
            }
            UpdateGroupsDisplay();
        }

        /// <summary>
        /// Updates the picker when the group drop down is updated
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void groupDropDownList_SelectedIndexChanged( object sender, EventArgs e )
        {
            var groupId = groupDropDownList.SelectedValue.AsInteger();
            if ( this.AllowMultiSelect )
            {
                addButton.Visible = groupId != 0;
            }
            UpdateGroupsDisplay();
        }

        /// <summary>
        /// Adds group to the seleted groups
        /// </summary>
        /// <param name="group">Group to add</param>
        private void AddGroupToList( Group group )
        {
            if ( AllowMultiSelect && !this.SelectedGroups.Select( g => g.Id ).Contains( group.Id ) )
            {
                var selectedGroups = SelectedGroups.ToList();
                selectedGroups.Add( group );
                this.SelectedGroups = selectedGroups;
            }
        }

        /// <summary>
        /// Adds the selected group to the list of groups
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event arguments</param>
        private void addButton_Click( object sender, EventArgs e )
        {
            var groupId = groupDropDownList.SelectedValue.AsInteger();
            if ( groupId != 0 )
            {
                var group = new GroupService( new RockContext() ).Get( groupId );
                if ( group != null )
                {
                    AddGroupToList( group );
                }
            }
            UpdateGroupsDisplay();
        }

        /// <summary>
        /// Populates the template drop down.
        /// </summary>
        private void BindTemplateDropDown()
        {
            RockContext rockContext = new RockContext();
            GroupTypeService groupTypeService = new GroupTypeService( rockContext );

            Guid templateTypeGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid();

            templateDropDownList.DataSource = groupTypeService
                    .Queryable().AsNoTracking()
                    .Where( t =>
                        t.GroupTypePurposeValue != null &&
                        t.GroupTypePurposeValue.Guid == templateTypeGuid )
                    .OrderBy( t => t.Name ).ToList();
            templateDropDownList.DataBind();
            templateDropDownList.Items.Insert( 0, new ListItem( "", "" ) );

        }

        /// <summary>
        /// Binds the checkin area drop down
        /// </summary>
        /// <param name="template"></param>
        private void BindGroupTypeDropDown( GroupType template )
        {
            groupDropDownList.Visible = false;

            _groupTypeOutput = new List<GroupType>();
            GetChildGroupTypes( template );

            groupTypeDropDownList.DataSource = _groupTypeOutput.Where( gt => gt.Groups.Any() ).ToList();
            groupTypeDropDownList.DataBind();
            groupTypeDropDownList.Items.Insert( 0, new ListItem( "", "" ) );
            groupTypeDropDownList.Visible = true;
        }

        /// <summary>
        /// Binds the checkin group drop down
        /// </summary>
        /// <param name="groupType"></param>
        private void BindGroupDropDown( GroupType groupType )
        {
            EnsureChildControls();
            addButton.Visible = false;

            groupDropDownList.DataSource = GetChildGroups( groupType.Groups ).ToList();
            groupDropDownList.DataBind();
            groupDropDownList.Items.Insert( 0, new ListItem( "", "" ) );
            groupDropDownList.Visible = true;
        }

        private List<GroupType> _groupTypeOutput;

        /// <summary>
        /// Recursively gets the child group types
        /// </summary>
        /// <param name="groupType">Group type you want to search</param>
        /// <returns></returns>
        private void GetChildGroupTypes( GroupType groupType )
        {
            var childGroupTypes = groupType.ChildGroupTypes
                .Where( gt => !_groupTypeOutput.Select( ogt => ogt.Id ).Contains( gt.Id ) );
            _groupTypeOutput.AddRange( childGroupTypes );

            foreach ( var gt in childGroupTypes )
            {
                GetChildGroupTypes( gt );
            }
        }

        /// <summary>
        /// Recursively find returns all child groups including the groups parameter
        /// </summary>
        /// <param name="groups">Groups you want to search</param>
        /// <returns></returns>
        private List<Group> GetChildGroups( ICollection<Group> groups )
        {
            List<Group> output = groups.ToList();
            foreach ( var group in groups )
            {
                output.AddRange( GetChildGroups( group.Groups ) );
            }

            return output;
        }

        /// <summary>
        /// Outputs server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter" /> object and stores tracing information about the control if tracing is enabled.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {
                RockControlHelper.RenderControl( this, writer );
            }
        }

        /// <summary>
        /// Renders the base control.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public void RenderBaseControl( HtmlTextWriter writer )
        {
            writer.RenderBeginTag( HtmlTextWriterTag.Div );
            groupsDisplay.RenderControl( writer );
            templateDropDownList.RenderControl( writer );
            groupTypeDropDownList.RenderControl( writer );
            groupDropDownList.RenderControl( writer );
            addButton.RenderControl( writer );

            writer.RenderEndTag();
        }

        /// <summary>
        /// Property to allow multiple groups to be selected
        /// </summary>
        public bool AllowMultiSelect { get; set; }

        /// <summary>
        /// Gets the selected group.
        /// </summary>
        /// <value>
        /// The selected group.
        /// </value>
        public Group SelectedGroup
        {
            get
            {
                if ( AllowMultiSelect )
                {
                    throw new InvalidOperationException( "Selected group not valid in Multi-Select mode. Use SelectedGroups" );
                }
                EnsureChildControls();
                if ( groupDropDownList.Visible )
                {
                    return new GroupService( new RockContext() ).Get( groupDropDownList.SelectedValue.AsInteger() );
                }

                return null;
            }

            set
            {
                if ( AllowMultiSelect )
                {
                    throw new InvalidOperationException( "Selected group not valid in Multi-Select mode. Use SelectedGroups." );
                }
                EnsureChildControls();
                if ( value != null )
                {
                    Group group = value;
                    GroupType groupType = group.GroupType;
                    GroupType template = GetParentTemplate( groupType );
                    if ( template != null )
                    {
                        templateDropDownList.SelectedValue = template.Id.ToString();
                        BindGroupTypeDropDown( template );
                        groupTypeDropDownList.SelectedValue = groupType.Id.ToString();
                        BindGroupDropDown( groupType );
                        groupDropDownList.SelectedValue = group.Id.ToString();
                    }
                    else
                    {
                        groupTypeDropDownList.Visible = false;
                        groupDropDownList.Visible = false;
                        addButton.Visible = false;
                    }
                }
                else
                {
                    groupTypeDropDownList.Visible = false;
                    groupDropDownList.Visible = false;
                    addButton.Visible = false;
                }
            }
        }

        public IEnumerable<Group> SelectedGroups
        {
            get
            {
                if ( !AllowMultiSelect )
                {
                    throw new InvalidOperationException( "SelectedGroups not valid in Single-Select mode. Use SelectedGroup." );
                }
                EnsureChildControls();

                if ( ViewState["Groups"] != null )
                {
                    List<int> groupIds = ( List<int> ) ViewState["Groups"];
                    return new GroupService( new RockContext() ).GetByIds( groupIds ).ToList();
                }

                return new List<Group>();
            }
            set
            {
                if ( !AllowMultiSelect )
                {

                    throw new InvalidOperationException( "SelectedGroups not valid in Single-Select mode. Use SelectedGroup" );
                }
                ViewState["Groups"] = value.Select( g => g.Id ).ToList();
                EnsureChildControls();
                UpdateGroupsDisplay();
            }
        }

        /// <summary>
        /// Updates the view for multi-select groups
        /// </summary>
        private void UpdateGroupsDisplay()
        {
            if ( AllowMultiSelect && this.SelectedGroups.Any() )
            {
                groupsDisplay.Visible = true;
                groupsDisplay.Controls.Clear();

                Panel header = new Panel();
                header.ID = string.Format( "HEADER{0}", this.ID );
                header.CssClass = "panel-heading";
                groupsDisplay.Controls.Add( header );

                Literal headerText = new Literal();
                headerText.ID = string.Format( "HEADERTEXT{0}", this.ID );
                headerText.Text = "Selected Check-in Groups";
                header.Controls.Add( headerText );

                Panel body = new Panel();
                body.ID = string.Format( "BODY{0}", this.ID );
                body.CssClass = "panel-body";
                groupsDisplay.Controls.Add( body );

                foreach ( var group in this.SelectedGroups )
                {
                    LinkButton lbGroup = new LinkButton();
                    lbGroup.CssClass = "edit";
                    lbGroup.ID = string.Format( "{0}REMOVE{1}", this.ID, group.Id );
                    lbGroup.Text = group.Name + " <i class='fa fa-close'></i><br>";
                    lbGroup.Click += ( s, e ) => { RemoveGroup( group ); };
                    body.Controls.Add( lbGroup );
                }
            }
            else
            {
                groupsDisplay.Visible = false;
            }
        }

        /// <summary>
        /// Removes group from selected groups
        /// </summary>
        /// <param name="group">The group to remove</param>
        private void RemoveGroup( Group group )
        {
            if ( AllowMultiSelect )
            {
                SelectedGroups = SelectedGroups
                    .Where( g => g.Id != group.Id );
            }
            UpdateGroupsDisplay();
        }

        /// <summary>
        /// Gets the parent template of the group
        /// </summary>
        /// <param name="groupType">The checkin area to look under</param>
        /// <returns></returns>
        private GroupType GetParentTemplate( GroupType groupType )
        {
            Guid templateTypeGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid();
            return groupType
                .ParentGroupTypes
                .Where( pgt => pgt.GroupTypePurposeValue.Guid == templateTypeGuid )
                .FirstOrDefault();
        }

        public static void CreateChildControls( IRockControl rockControl, ControlCollection controls )
        {
            if ( rockControl.RequiredFieldValidator != null )
            {
                rockControl.RequiredFieldValidator.ID = rockControl.ID + "_rfv";
                rockControl.RequiredFieldValidator.ControlToValidate = rockControl.ID;
                rockControl.RequiredFieldValidator.Display = ValidatorDisplay.Dynamic;
                rockControl.RequiredFieldValidator.CssClass = "validation-error help-inline";
                rockControl.RequiredFieldValidator.Enabled = rockControl.Required;
                controls.Add( rockControl.RequiredFieldValidator );
            }

            if ( rockControl.HelpBlock != null )
            {
                rockControl.HelpBlock.ID = rockControl.ID + "_hb";
                controls.Add( rockControl.HelpBlock );
            }

            if ( rockControl.WarningBlock != null )
            {
                rockControl.WarningBlock.ID = rockControl.ID + "_wb";
                controls.Add( rockControl.WarningBlock );
            }
        }
    }
}