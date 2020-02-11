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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.Widgities.Cache;
using org.secc.Widgities.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org.secc.Widgities
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Widgity Container" )]
    [Category( "SECC > Widgity" )]
    [Description( "Block for containing widgities" )]
    public partial class WidgityContainer : Rock.Web.UI.RockBlockCustomSettings
    {
        public override string SettingsToolTip
        {
            get { return "Edit Widgities"; }
        }

        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        protected static class AttributeKey
        {
            internal const string Content = "Content";
        }

        #endregion Attribute Keys

        public List<Widgity> Widgities { get; set; }
        public Dictionary<Guid, List<WidgityItem>> WidgityItems { get; set; }
        public List<Guid> ExpandedPanels { get; set; }


        public List<WidgityItem> CurrentWidgityItems
        {
            get
            {
                if ( ViewState["CurrentWidgityItems"] != null && ViewState["CurrentWidgityItems"] is string )
                {
                    return JsonConvert.DeserializeObject<List<WidgityItem>>( ViewState["CurrentWidgityItems"] as string );
                }
                return null;
            }
            set
            {
                ViewState["CurrentWidgityItems"] = JsonConvert.SerializeObject( value );
            }
        }


        public Guid? CurrentEditWidgity
        {
            get
            {
                if ( ViewState["CurrentEditWidgity"] != null && ViewState["CurrentEditWidgity"] is Guid )
                {
                    return ViewState["CurrentEditWidgity"] as Guid?;
                }
                return null;
            }
            set
            {
                ViewState["CurrentEditWidgity"] = value;
            }
        }

        private void SaveState()
        {
            ViewState["Widgities"] = JsonConvert.SerializeObject( Widgities );
            ViewState["WidgityItems"] = JsonConvert.SerializeObject( WidgityItems );
        }

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/dragula.min.js" );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            if ( ViewState["Widgities"] != null && ViewState["Widgities"] is string )
            {
                Widgities = JsonConvert.DeserializeObject<List<Widgity>>( ViewState["Widgities"] as string );
            }
            else
            {
                Widgities = new List<Widgity>();

            }

            if ( ViewState["WidgityItems"] != null && ViewState["WidgityItems"] is string )
            {
                WidgityItems = JsonConvert.DeserializeObject<Dictionary<Guid, List<WidgityItem>>>( ViewState["WidgityItems"] as string );
            }
            else
            {
                WidgityItems = new Dictionary<Guid, List<WidgityItem>>();

            }


            var currentEditWidgity = Widgities.Where( w => w.Guid == CurrentEditWidgity ).FirstOrDefault();

            if ( currentEditWidgity != null )
            {
                phAttributesEdit.Controls.Clear();
                Rock.Attribute.Helper.AddEditControls( currentEditWidgity, phAttributesEdit, false, BlockValidationGroup );
                BuildWidigityItemAttibutes( currentEditWidgity, false );
            }

            ShowDetails();
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
                ExpandedPanels = new List<Guid>();
                LoadWidgities();
                BindRepeaterMenu();
                ShowDetails();
            }
            else
            {
                if ( Request["__EVENTTARGET"].ToStringSafe() == lbDragCommand.ClientID )
                {
                    ProcessDragEvents();
                }
            }
        }

        private void BindRepeaterMenu()
        {
            RockContext rockContext = new RockContext();
            WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );
            var blockEntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id;

            var widgityTypes = widgityTypeService.Queryable()
                .Where( wt => wt.EntityTypes.Select( e => e.Id ).Contains( blockEntityTypeId ) ).ToList();
            rpWidgityTypes.DataSource = widgityTypes;
            rpWidgityTypes.DataBind();
        }

        private void LoadWidgities()
        {
            var blockEntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id;

            var widgityCache = WidgityCache.GetForEntity( typeof( Block ), BlockCache.Id );

            Widgities = widgityCache.Select( w => w.GetEntity() ).OrderBy( w => w.Order ).ToList();

            WidgityItems = new Dictionary<Guid, List<WidgityItem>>();

            foreach ( var widgity in widgityCache )
            {
                var widgityItems = widgity.WidgityItems;
                WidgityItems[widgity.Guid] = widgityItems.Select( wi => wi.GetEntity() ).ToList();
            }

            SaveState();
        }

        private void ProcessDragEvents()
        {
            string argument = Request["__EVENTARGUMENT"].ToStringSafe();

            var arguments = argument.SplitDelimitedValues();

            switch ( arguments[0] )
            {
                case "add-widgity":
                    AddWidgity( arguments[1].AsInteger(), arguments[2].AsInteger() );
                    break;
                case "move-widgity":
                    MoveWidgity( arguments[1].AsGuid(), arguments[2].AsInteger() );
                    break;
                case "move-widgity-item":
                    MoveWidgityItem( arguments[1].AsGuid(), arguments[2].AsInteger() );
                    break;
                default:
                    break;
            }

        }

        private void MoveWidgityItem( Guid widgityItemGuid, int order )
        {
            var widgity = Widgities.Where( w => w.Guid == CurrentEditWidgity.Value ).FirstOrDefault();
            var widgityItems = WidgityItems[CurrentEditWidgity.Value];

            var widgityItem = widgityItems.Where( w => w.Guid == widgityItemGuid ).FirstOrDefault();
            if ( widgityItems != null )
            {
                widgityItems.Remove( widgityItem );
                widgityItems.Insert( order, widgityItem );
            }

            UpdateWidgityItemAttributes( widgity );
            SaveState();
            GetExpandedPanels();
            ShowDetails();
            BuildWidigityItemAttibutes( widgity, true );
        }

        private void MoveWidgity( Guid widgityGuid, int order )
        {
            var widgity = Widgities.Where( w => w.Guid == widgityGuid ).FirstOrDefault();
            if ( widgity != null )
            {
                Widgities.Remove( widgity );
                Widgities.Insert( order, widgity );
            }

            SaveState();
            ShowDetails();
        }

        private void AddWidgity( int widgityTypeId, int order )
        {
            RockContext rockContext = new RockContext();
            WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );
            var widgityType = widgityTypeService.Get( widgityTypeId );
            if ( widgityType != null )
            {
                var widgity = new Widgity
                {
                    WidgityTypeId = widgityTypeId,
                    Guid = Guid.NewGuid(),
                    EntityId = BlockCache.Id
                };

                WidgityItems[widgity.Guid] = new List<WidgityItem>();

                widgity.LoadAttributes();
                Widgities.Insert( order, widgity );
                SaveState();
                EditWidgity( widgity.Guid );
            }
        }

        #endregion

        #region Events




        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        #endregion

        #region Methods

        private bool IsEditMode()
        {
            var editMode = false;
            var editViewState = ViewState["Mode"];
            if ( editViewState != null && editViewState is string && ( ( string ) editViewState ) == "EDIT" )
            {
                editMode = true;
            }
            return editMode;
        }

        private void ShowDetails()
        {
            if ( IsEditMode() )
            {
                ScriptManager.RegisterStartupScript( upnlContent, upnlContent.GetType(), "InitDrag", "InitDrag();", true );
            }
            phWidgities.Controls.Clear();

            RockContext rockContext = new RockContext();
            WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );
            foreach ( var widgity in Widgities )
            {
                var widgityType = widgityTypeService.Get( widgity.WidgityTypeId );
                var panel = new Panel();
                phWidgities.Controls.Add( panel );
                if ( IsEditMode() )
                {
                    if ( !CurrentEditWidgity.HasValue || ( CurrentEditWidgity.HasValue && CurrentEditWidgity == widgity.Guid ) )
                    {
                        panel.Attributes["data-component-id"] = widgity.Guid.ToString();
                        panel.CssClass = "widgityContent";

                        Panel pencilPanel = new Panel
                        {
                            CssClass = "widgity-edit"
                        };
                        panel.Controls.Add( pencilPanel );

                        LinkButton link = new LinkButton
                        {
                            ID = string.Format( "lbEdit{0}", widgity.Guid ),
                            Text = "<i class='fa fa-pencil'></i>",
                            CssClass = "widgity-edit-pencil"
                        };
                        link.Click += ( s, e ) => { EditWidgity( widgity.Guid ); };
                        pencilPanel.Controls.Add( link );
                        if ( CurrentEditWidgity == widgity.Guid )
                        {
                            panel.AddCssClass( "active-edit" );
                        }
                    }
                }

                Panel displayPanel = new Panel();
                displayPanel.Style.Add( "width", "100%" );
                panel.Controls.Add( displayPanel );

                var mergeObjects = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
                mergeObjects.Add( "Widgity", widgity );
                mergeObjects.Add( "WidgityItems", WidgityItems[widgity.Guid] );
                var output = new Literal
                {
                    Text = widgityType.Markdown.ResolveMergeFields( mergeObjects, CurrentPerson, widgityType.EnabledLavaCommands )
                };
                displayPanel.Controls.Add( output );
            }
        }

        private void EditWidgity( Guid widgityGuid )
        {
            if ( CurrentEditWidgity.HasValue )
            {
                return;
            }

            var widgities = Widgities;
            var widgity = widgities.Where( w => w.Guid == widgityGuid ).FirstOrDefault();

            if ( widgity == null )
            {
                return;
            }

            RockContext rockContext = new RockContext();
            WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );

            var widgityType = widgityTypeService.Get( widgity.WidgityTypeId );

            pnlWidgityTypes.Visible = false;
            pnlWidgityEdit.Visible = true;

            CurrentEditWidgity = widgityGuid;
            CurrentWidgityItems = WidgityItems[widgityGuid]; //Store value for undoing later

            ltWidgityTypeName.Text = widgityType.Name;


            phAttributesEdit.Controls.Clear();
            Rock.Attribute.Helper.AddEditControls( widgity, phAttributesEdit, true, BlockValidationGroup );

            BuildWidigityItemAttibutes( widgity, true );

            ShowDetails();
        }

        private void BuildWidigityItemAttibutes( Widgity widgity, bool setValues )
        {
            RockContext rockContext = new RockContext();
            WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );
            var widgityType = widgityTypeService.Get( widgity.WidgityTypeId );

            phItems.Controls.Clear();
            if ( widgityType.HasItems )
            {
                var widgityItems = WidgityItems[widgity.Guid];
                pnlWidgitItems.Visible = true;
                if ( widgityItems.Any() )
                {
                    foreach ( var widgityItem in widgityItems )
                    {
                        Panel panel = new Panel();
                        phItems.Controls.Add( panel );
                        panel.Attributes["data-component-id"] = widgityItem.Guid.ToString();

                        PanelWidget panelWidget = new PanelWidget
                        {
                            ID = string.Format( "pnlItem_{0}", widgityItem.Guid ),

                        };

                        panel.Controls.Add( panelWidget );

                        if ( ExpandedPanels != null && ExpandedPanels.Contains( widgityItem.Guid ) )
                        {
                            panelWidget.Expanded = true;
                        }

                        if ( widgityItem.AttributeValues.Any() )
                        {
                            panelWidget.Title = "<a class='btn btn-xs btn-link ui-sortable-handle'><i class='fa fa-bars ui-sortable-handle'></i></a> " +
                                widgityItem.AttributeValues.FirstOrDefault().Value.ValueFormatted;
                        }
                        else
                        {
                            panelWidget.Title = "<a class='btn btn-xs btn-link ui-sortable-handle'><i class='fa fa-bars ui-sortable-handle'></i></a> ";
                        }

                        HiddenField hiddenField = new HiddenField
                        {
                            ID = string.Format( "hfItem_{0}", widgityItem.Guid ),
                            Value = widgityItem.Guid.ToString()
                        };
                        panelWidget.Controls.Add( hiddenField );

                        PlaceHolder phItemAttributes = new PlaceHolder
                        {
                            ID = string.Format( "phItem_{0}", widgityItem.Guid )
                        };
                        panelWidget.Controls.Add( phItemAttributes );

                        LinkButton linkButton = new LinkButton()
                        {
                            ID = string.Format( "btnRemove_{0}", widgityItem.Guid ),
                            CssClass = "btn btn-danger btn-xs",
                            Text = "Remove Item"
                        };

                        panelWidget.Controls.Add( linkButton );

                        linkButton.Click += ( s, e ) => { DeleteWidgityItem( widgityItem.Guid ); };

                        Rock.Attribute.Helper.AddEditControls( widgityItem, phItemAttributes, setValues, BlockValidationGroup );
                    }
                }
                else
                {
                    phItems.Controls.Add( new Literal
                    {
                        Text = "<i>No items. Click Add Item to add a new item to this widgity.</i>"
                    } );
                }

            }
            else
            {
                pnlWidgitItems.Visible = false;
            }
        }

        private void DeleteWidgityItem( Guid widgityItemGuid )
        {
            var widgity = Widgities.Where( w => w.Guid == CurrentEditWidgity.Value ).FirstOrDefault();
            var widgityItems = WidgityItems[CurrentEditWidgity.Value];

            var widgityItem = widgityItems.Where( w => w.Guid == widgityItemGuid ).FirstOrDefault();

            widgityItems.Remove( widgityItem );

            UpdateWidgityItemAttributes( widgity );
            SaveState();
            GetExpandedPanels();
            ShowDetails();
            BuildWidigityItemAttibutes( widgity, true );
        }

        #endregion
        protected override void ShowSettings()
        {
            if ( UserCanEdit )
            {
                pnlContent.Style["box-shadow"] = "0 4px 8px 0 rgba(0, 0, 0, 0.2), 0 6px 20px 0 rgba(0, 0, 0, 0.19)";
                ViewState["Mode"] = "EDIT";
                pnlContent.CssClass = "col-md-9";
                pnlMenu.Visible = true;
                ShowDetails();
            }
        }

        protected void HideSettings()
        {
            pnlContent.Style["box-shadow"] = "";
            ViewState["Mode"] = "VIEW";
            pnlContent.CssClass = "";
            pnlMenu.Visible = false;
        }

        protected void btnCancelEdit_Click( object sender, EventArgs e )
        {
            pnlWidgityTypes.Visible = true;
            pnlWidgityEdit.Visible = false;
            WidgityItems[CurrentEditWidgity.Value] = CurrentWidgityItems;
            CurrentEditWidgity = null;
            SaveState();
            ShowDetails();
        }

        protected void btnSaveEdit_Click( object sender, EventArgs e )
        {
            pnlWidgityTypes.Visible = true;
            pnlWidgityEdit.Visible = false;

            var widgityGuid = CurrentEditWidgity.Value;
            var widgity = Widgities.Where( w => w.Guid == widgityGuid ).FirstOrDefault();

            if ( widgity == null )
            {
                return;
            }

            Rock.Attribute.Helper.GetEditValues( phAttributesEdit, widgity );

            UpdateWidgityItemAttributes( widgity );

            SaveState();

            CurrentEditWidgity = null;
            CurrentWidgityItems = null;
            ShowDetails();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            HideSettings();
            LoadWidgities();
            ShowDetails();
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid && UserCanEdit )
            {

                var widgityRefs = Widgities;
                var widgityRefIds = widgityRefs.Select( w => w.Id ).ToList();
                RockContext rockContext = new RockContext();
                rockContext.WrapTransaction( () =>
                {
                    WidgityService widgityService = new WidgityService( rockContext );

                    var blockEntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id;

                    var toRemove = widgityService.Queryable( "WidgityType" )
                        .Where( w => w.EntityId == BlockCache.Id && w.EntityTypeId == blockEntityTypeId )
                        .Where( w => !widgityRefIds.Contains( w.Id ) )
                        .ToList();

                    widgityService.DeleteRange( toRemove );

                    List<Widgity> widgities = new List<Widgity>();

                    foreach ( var widgityRef in widgityRefs )
                    {
                        Widgity widgity = widgityService.Get( widgityRef.Id );

                        if ( widgity == null )
                        {
                            widgity = widgityRef;
                            widgity.EntityTypeId = EntityTypeCache.Get( typeof( Block ) ).Id;
                            widgityService.Add( widgity );
                            rockContext.SaveChanges();
                        }
                        else
                        {
                            widgity.LoadAttributes();
                        }
                        widgities.Add( widgity );
                        widgity.AttributeValues = widgityRef.AttributeValues;
                        widgity.Order = widgities.IndexOf( widgity );
                    }
                    rockContext.SaveChanges();
                    foreach ( var widgity in widgities )
                    {
                        widgity.SaveAttributeValues();
                    }

                    //WidgityItems
                    WidgityItemService widgityItemService = new WidgityItemService( rockContext );

                    foreach ( var widgity in widgities )
                    {
                        var storedWidgityItems = WidgityItems[widgity.Guid];
                        var storedWidgityItemGuids = storedWidgityItems.Select( wi => wi.Guid ).ToList();
                        var databaseWidgityItems = widgityItemService.Queryable().Where( wi => wi.WidgityId == widgity.Id ).ToList();

                        //Remove deleted items
                        var itemsToRemove = databaseWidgityItems.Where( wi => !storedWidgityItemGuids.Contains( wi.Guid ) ).ToList();
                        widgityItemService.DeleteRange( itemsToRemove );

                        foreach ( var item in storedWidgityItems )
                        {
                            if ( item.Id == 0 ) // new
                            {
                                item.Order = storedWidgityItems.IndexOf( item );
                                item.WidgityId = widgity.Id;
                                item.WidgityTypeId = widgity.WidgityTypeId;
                                widgityItemService.Add( item );
                                rockContext.SaveChanges();
                                item.SaveAttributeValues();
                            }
                            else // update
                            {
                                var databaseItem = databaseWidgityItems.Where( wi => wi.Guid == item.Guid ).FirstOrDefault();
                                databaseItem.Order = storedWidgityItems.IndexOf( item );
                                databaseItem.LoadAttributes();
                                databaseItem.AttributeValues = item.AttributeValues;
                                databaseItem.SaveAttributeValues();
                                rockContext.SaveChanges();
                            }
                        }
                    }
                } );
            }
            WidgityCache.Clear();
            WidgityItemCache.Clear();
            HideSettings();
            LoadWidgities();
            ShowDetails();
        }

        protected void btnDeleteWidgity_Click( object sender, EventArgs e )
        {
            var widgity = Widgities.Where( w => w.Guid == CurrentEditWidgity ).FirstOrDefault();
            Widgities.Remove( widgity );
            SaveState();
            pnlWidgityTypes.Visible = true;
            pnlWidgityEdit.Visible = false;
            CurrentEditWidgity = null;
            ShowDetails();
        }

        protected void btnAddItem_Click( object sender, EventArgs e )
        {
            var widgity = Widgities.Where( w => w.Guid == CurrentEditWidgity ).FirstOrDefault();

            var widgityItem = new WidgityItem
            {
                Guid = Guid.NewGuid(),
                WidgityId = widgity.Id,
                WidgityTypeId = widgity.WidgityTypeId
            };

            widgityItem.LoadAttributes();

            var widgityItems = WidgityItems[widgity.Guid];
            widgityItems.Add( widgityItem );
            UpdateWidgityItemAttributes( widgity );
            SaveState();
            GetExpandedPanels();
            ExpandedPanels.Add( widgityItem.Guid );
            BuildWidigityItemAttibutes( widgity, true );
        }

        private void UpdateWidgityItemAttributes( Widgity widgity )
        {
            var widgityItems = WidgityItems[widgity.Guid];
            foreach ( Control container in phItems.Controls )
            {
                var panels = container.Controls.OfType<PanelWidget>().ToList();
                foreach ( var panel in panels )
                {
                    HiddenField hiddenField = panel.Controls[4] as HiddenField;
                    var itemGuid = hiddenField.Value.AsGuid();
                    var widgityItem = widgityItems.Where( wi => wi.Guid == itemGuid ).FirstOrDefault();
                    var placeholder = panel.Controls[5] as PlaceHolder;
                    Rock.Attribute.Helper.GetEditValues( placeholder, widgityItem );
                }
            }
        }

        private void GetExpandedPanels()
        {
            ExpandedPanels = new List<Guid>();
            foreach ( Control container in phItems.Controls )
            {
                var panels = container.Controls.OfType<PanelWidget>().ToList();
                foreach ( var panel in panels )
                {
                    if ( panel.Expanded )
                    {
                        HiddenField hiddenField = panel.Controls[4] as HiddenField;
                        ExpandedPanels.Add( hiddenField.Value.AsGuid() );
                    }
                }
            }
        }
    }
}