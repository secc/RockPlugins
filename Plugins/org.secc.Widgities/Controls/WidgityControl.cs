using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.Widgities.Cache;
using org.secc.Widgities.Model;
using Rock;
using Rock.Data;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace org.secc.Widgities.Controls
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="System.Web.UI.WebControls.CompositeControl" />
    [ToolboxData( "<{0}:WidgityControl runat=server></{0}:WidgityControl" )]
    public class WidgityControl : CompositeControl
    {
        #region Properties


        [
        Bindable( true ),
        Category( "Behavior" ),
        DefaultValue( "" ),
        Description( "The Entity Type Id for the particlar item" )
        ]
        public int EntityTypeId
        {
            get
            {
                if ( ViewState["EntityTypeId"] != null && ViewState["EntityTypeId"] is int )
                {
                    return ( int ) ViewState["EntityTypeId"];
                }
                return 0;
            }
            set
            {
                ViewState["EntityTypeId"] = value;
            }
        }

        [
       Bindable( true ),
       Category( "Behavior" ),
       DefaultValue( "" ),
       Description( "The Entity Id for the particlar set of widgities" )
       ]
        public int EntityId
        {
            get
            {
                if ( ViewState["EntityId"] != null && ViewState["EntityId"] is int )
                {
                    return ( int ) ViewState["EntityTypeId"];
                }
                return 0;
            }
            set
            {
                ViewState["EntityId"] = value;
            }
        }

        public WidgityMode Mode
        {
            get
            {
                if ( ViewState["WidgityMode"] != null && ViewState["WidgityMode"] is WidgityMode )
                {
                    return ( WidgityMode ) ViewState["WidgityMode"];
                }
                return WidgityMode.View;
            }
            set
            {
                ViewState["WidgityMode"] = value;
            }
        }

        public enum WidgityMode
        {
            View = 0,
            Edit = 1
        }

        private List<Widgity> Widgities { get; set; }
        private Dictionary<Guid, List<WidgityItem>> WidgityItems { get; set; }
        private List<Guid> ExpandedPanels { get; set; }


        private List<WidgityItem> CurrentWidgityItems
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

        private Guid? CurrentEditWidgity
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

        #endregion

        #region Controls

        private LinkButton lbDragCommand;
        private Panel pnlMenu;
        private Panel pnlContent;
        private Panel pnlItems;
        private PlaceHolder phAttributesEdit;

        #endregion

        #region Constructors


        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            ( this.Page as RockPage ).AddScriptLink( "~/Scripts/dragula.min.js" );
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

            ShowWidgities( false );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( !Page.IsPostBack )
            {
                LoadWidgities();
                ShowWidgities( false );
            }
            else
            {
                if ( HttpContext.Current.Request["__EVENTTARGET"].ToStringSafe() == lbDragCommand.ClientID )
                {
                    ProcessDragEvents();
                }
            }
        }

        private void ProcessDragEvents()
        {
            string argument = HttpContext.Current.Request["__EVENTARGUMENT"].ToStringSafe();

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
            ShowWidgities( true );
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
            ShowWidgities( true );
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
                    EntityId = EntityId,
                    EntityTypeId = EntityTypeId
                };

                WidgityItems[widgity.Guid] = new List<WidgityItem>();

                widgity.LoadAttributes();
                Widgities.Insert( order, widgity );
                SaveState();
                EditWidgity( widgity.Guid );
            }
        }

        private void UpdateWidgityItemAttributes( Widgity widgity )
        {
            var widgityItems = WidgityItems[widgity.Guid];
            if ( pnlItems != null )
            {
                foreach ( Control container in pnlItems.Controls )
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
        }

        private void GetExpandedPanels()
        {
            ExpandedPanels = new List<Guid>();
            if ( pnlItems != null )
            {
                foreach ( Control container in pnlItems.Controls )
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

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            lbDragCommand = new LinkButton
            {
                ID = this.ID + "_lbDragCommand",
                CssClass = "hidden"
            };
            Controls.Add( lbDragCommand );

            pnlMenu = new Panel
            {
                ID = this.ID + "_pnlMenu",
                CssClass = "widgityMenu col-md-3"
            };
            Controls.Add( pnlMenu );

            pnlContent = new Panel
            {
                ID = this.ID + "_pnlContent",
                CssClass = "widgityDestination"
            };
            Controls.Add( pnlContent );
        }


        /// <summary>
        /// Called just before rendering begins on the page.
        /// </summary>
        /// <param name="e">The EventArgs that describe this event.</param>
        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );
        }

        /// <summary>
        /// Outputs server control content to a provided <see cref="T:System.Web.UI.HtmlTextWriter" /> object and stores tracing information about the control if tracing is enabled.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {
            foreach ( Control control in Controls )
            {
                control.RenderControl( writer );
            }
        }

        #endregion

        #region Methods

        private void SaveState()
        {
            ViewState["Widgities"] = JsonConvert.SerializeObject( Widgities );
            ViewState["WidgityItems"] = JsonConvert.SerializeObject( WidgityItems );
        }

        private void LoadWidgities()
        {
            var blockEntityTypeId = EntityTypeCache.Get( EntityTypeId ).Id;

            var widgityCache = WidgityCache.GetForEntity( blockEntityTypeId, EntityId );

            Widgities = widgityCache.Select( w => w.GetEntity() ).OrderBy( w => w.Order ).ToList();

            WidgityItems = new Dictionary<Guid, List<WidgityItem>>();

            EntityTypeId = EntityTypeId;
            EntityId = EntityId;
            Mode = Mode;


            foreach ( var widgity in widgityCache )
            {
                var widgityItems = widgity.WidgityItems;
                WidgityItems[widgity.Guid] = widgityItems.Select( wi => wi.GetEntity() ).ToList();
            }

            SaveState();
        }

        private void ShowWidgities( bool setValues )
        {
            var page = this.Page as RockPage;
            page.AddCSSLink( "~/Plugins/org_secc/Widgities/Widgities.css" );

            EnsureChildControls();
            pnlContent.Controls.Clear();

            if ( Mode == WidgityMode.Edit )
            {
                pnlMenu.Visible = true;
                ShowEdit( setValues );
            }
            else
            {
                pnlMenu.Visible = false;
                pnlContent.Style["box-shadow"] = "";
                pnlContent.CssClass = "";
            }

            RockPage rockPage = null;
            if ( HttpContext.Current != null )
            {
                rockPage = HttpContext.Current.Handler as RockPage;
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( rockPage );
            mergeFields.Add( "CurrentPage", PageCache.Get( rockPage.PageId ) );

            foreach ( var widgity in Widgities )
            {
                var panel = new Panel();
                pnlContent.Controls.Add( panel );
                if ( Mode == WidgityMode.Edit )
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

                mergeFields["Widgity"] = widgity;
                mergeFields["WidgityItems"] = WidgityItems[widgity.Guid];
                mergeFields["uniqueid"] = widgity.Guid.ToString().Split( '-' ).First();
                var widgityType = WidgityTypeCache.Get( widgity.WidgityTypeId );
                var output = new Literal
                {
                    Text = widgityType.Markdown.ResolveMergeFields( mergeFields, widgityType.EnabledLavaCommands )
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

            var widgity = Widgities.Where( w => w.Guid == widgityGuid ).FirstOrDefault();

            if ( widgity == null )
            {
                return;
            }

            CurrentEditWidgity = widgityGuid;
            CurrentWidgityItems = WidgityItems[widgityGuid]; //Store value for undoing later

            ShowWidgities( true );


        }

        private void ShowEdit( bool setValues )
        {
            pnlMenu.Controls.Clear();

            pnlContent.Style["box-shadow"] = "0 4px 8px 0 rgba(0, 0, 0, 0.2), 0 6px 20px 0 rgba(0, 0, 0, 0.19)";
            pnlContent.CssClass = "col-md-9 widgityDestination";

            RegisterDragDropScript();

            var currentEditWidgity = Widgities.Where( w => w.Guid == CurrentEditWidgity ).FirstOrDefault();

            if ( currentEditWidgity != null ) //Build Widigty
            {
                ShowWidigtyEdit( setValues );
                //phAttributesEdit.Controls.Clear();
                //Rock.Attribute.Helper.AddEditControls( currentEditWidgity, phAttributesEdit, false, BlockValidationGroup );
                //BuildWidigityItemAttibutes( currentEditWidgity, false );
            }
            else
            {
                ShowWidgityTypes();
            }
        }


        private void ShowWidigtyEdit( bool setValues )
        {
            var widgityGuid = CurrentEditWidgity.Value;
            var widgity = Widgities.Where( w => w.Guid == widgityGuid ).FirstOrDefault();
            var widgityType = WidgityTypeCache.Get( widgity.WidgityTypeId );

            Literal ltName = new Literal
            {
                Text = string.Format( "<h3>{0}</h3>", widgityType.Name )
            };
            pnlMenu.Controls.Add( ltName );

            phAttributesEdit = new PlaceHolder
            {
                ID = this.ID + "_phAttributesEdit"
            };
            pnlMenu.Controls.Add( phAttributesEdit );
            Rock.Attribute.Helper.AddEditControls( widgity, phAttributesEdit, setValues );

            if ( widgityType.HasItems )
            {
                BuildWidigityItemAttibutes( widgity, setValues );
            }

            HtmlGenericContainer hr = new HtmlGenericContainer( "hr" );
            pnlMenu.Controls.Add( hr );

            BootstrapButton btnEditSave = new BootstrapButton
            {
                CssClass = "btn btn-primary",
                Text = "Save",
                ID = this.ID + "_btnEditSave"
            };
            pnlMenu.Controls.Add( btnEditSave );
            btnEditSave.Click += BtnEditSave_Click;

            LinkButton btnEditCancel = new LinkButton
            {
                ID = this.ID + "_btnEditCancel",
                Text = "Cancel",
                CssClass = "btn btn-link",
                CausesValidation = false
            };
            pnlMenu.Controls.Add( btnEditCancel );
            btnEditCancel.Click += BtnEditCancel_Click;

            LinkButton btnDeleteWidgity = new LinkButton
            {
                ID = this.ID + "_btnDeleteWidgity",
                Text = "Delete",
                CssClass = "btn btn-delete pull-right",
                CausesValidation = false
            };
            pnlMenu.Controls.Add( btnDeleteWidgity );
            btnDeleteWidgity.Click += BtnDeleteWidgity_Click;
        }

        private void BtnDeleteWidgity_Click( object sender, EventArgs e )
        {
            var widgity = Widgities.Where( w => w.Guid == CurrentEditWidgity ).FirstOrDefault();
            Widgities.Remove( widgity );
            SaveState();
            CurrentEditWidgity = null;
            ShowWidgities( true );
        }

        private void BtnEditCancel_Click( object sender, EventArgs e )
        {
            WidgityItems[CurrentEditWidgity.Value] = CurrentWidgityItems;
            CurrentEditWidgity = null;
            SaveState();
            ShowWidgities( true );
        }

        private void BtnEditSave_Click( object sender, EventArgs e )
        {
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
            ShowWidgities( true );
        }

        private void BuildWidigityItemAttibutes( Widgity widgity, bool setValues )
        {
            pnlItems = new Panel();
            pnlMenu.Controls.Add( pnlItems );

            var widgityType = WidgityTypeCache.Get( widgity.WidgityTypeId );

            if ( widgityType.HasItems )
            {
                pnlItems.CssClass = "widgityItemControls";
                var widgityItems = WidgityItems[widgity.Guid];
                if ( widgityItems.Any() )
                {
                    foreach ( var widgityItem in widgityItems )
                    {
                        Panel panel = new Panel();
                        pnlItems.Controls.Add( panel );
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

                        Rock.Attribute.Helper.AddEditControls( widgityItem, phItemAttributes, setValues );
                    }
                }
                else
                {
                    pnlItems.Controls.Add( new Literal
                    {
                        Text = "<i>No items. Click Add Item to add a new item to this widgity.</i>"
                    } );
                }

                LinkButton btnAddButton = new LinkButton
                {
                    ID = this.ID + "_AddItem",
                    Text = "Add Item",
                    CssClass = "btn btn-default btn-xs",
                    CausesValidation = false
                };
                pnlMenu.Controls.Add( btnAddButton );
                btnAddButton.Click += BtnAddButton_Click;
            }
        }

        private void BtnAddButton_Click( object sender, EventArgs e )
        {
            var widgity = Widgities.Where( w => w.Guid == CurrentEditWidgity ).FirstOrDefault();

            Rock.Attribute.Helper.GetEditValues( phAttributesEdit, widgity );

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
            ShowWidgities( true );
        }

        private void DeleteWidgityItem( Guid widgityItemGuid )
        {
            var widgity = Widgities.Where( w => w.Guid == CurrentEditWidgity.Value ).FirstOrDefault();
            var widgityItems = WidgityItems[CurrentEditWidgity.Value];

            var widgityItem = widgityItems.Where( w => w.Guid == widgityItemGuid ).FirstOrDefault();

            widgityItems.Remove( widgityItem );

            Rock.Attribute.Helper.GetEditValues( phAttributesEdit, widgity );

            UpdateWidgityItemAttributes( widgity );
            SaveState();
            GetExpandedPanels();
            ShowWidgities( true );
        }

        private void ShowWidgityTypes()
        {
            var widgityTypes = WidgityTypeCache.All()
                .Where( wt => wt.EntityTypes.Select( e => e.Id ).Contains( EntityTypeId ) ).ToList();

            if ( !widgityTypes.Any() )
            {
                NotificationBox notification = new NotificationBox
                {
                    NotificationBoxType = NotificationBoxType.Warning,
                    Text = "There are no widgity types for this entity type: " + EntityTypeCache.Get( EntityTypeId )?.FriendlyName
                };
                pnlMenu.Controls.Add( notification );
                return;
            }

            var categories = widgityTypes
                .Where( wt => wt.Category != null )
                .Select( wt => wt.Category )
                .DistinctBy( c => c.Id )
                .ToList();

            foreach ( var category in categories )
            {
                PanelWidget panelWidget = new PanelWidget
                {
                    ID = this.ID + "_pwCategory_" + category.Id.ToString(),
                    Title = category.Name
                };
                pnlMenu.Controls.Add( panelWidget );

                var dragContainer = new Panel
                {
                    ID = this.ID + "_pnlWidgityContainer_" + category.Id.ToString(),
                    CssClass = "widgitySource"
                };
                panelWidget.Controls.Add( dragContainer );

                var categoryTypes = widgityTypes.Where( wt => wt.CategoryId == category.Id ).ToList();

                foreach ( var widgityType in categoryTypes )
                {
                    HtmlGenericContainer item = new HtmlGenericContainer( "div" );
                    item.InnerHtml = string.Format( "<i class='{0}'></i><br />{1}",
                        widgityType.Icon,
                        widgityType.Name );
                    item.Attributes.Add( "data-component-id", widgityType.Id.ToString() );
                    item.CssClass = "btn btn-default btn-block";
                    dragContainer.Controls.Add( item );
                }
            }

            var noCategoryTypes = widgityTypes.Where( wt => wt.Category == null ).ToList();
            if ( noCategoryTypes.Any() )
            {

                var dragContainerOther = new Panel
                {
                    ID = this.ID + "_pnlWidgityContainer_other",
                    CssClass = "widgitySource"
                };

                if ( categories.Any() )
                {

                    PanelWidget panelWidgetOther = new PanelWidget
                    {
                        ID = this.ID + "_pwCategory_Other",
                        Title = "Other"
                    };
                    pnlMenu.Controls.Add( panelWidgetOther );
                    panelWidgetOther.Controls.Add( dragContainerOther );
                }
                else
                {
                    pnlMenu.Controls.Add( dragContainerOther );
                }

                foreach ( var widgityType in noCategoryTypes )
                {
                    HtmlGenericContainer item = new HtmlGenericContainer( "div" )
                    {
                        InnerHtml = string.Format( "<i class='{0}'></i><br />{1}",
                        widgityType.Icon,
                        widgityType.Name )
                    };
                    item.Attributes.Add( "data-component-id", widgityType.Id.ToString() );
                    dragContainerOther.Controls.Add( item );
                }
            }

            HtmlGenericContainer hr = new HtmlGenericContainer( "hr" );
            pnlMenu.Controls.Add( hr );

            BootstrapButton btnSave = new BootstrapButton
            {
                CssClass = "btn btn-primary",
                Text = "Publish",
                ID = this.ID + "_btnSave"
            };
            pnlMenu.Controls.Add( btnSave );
            btnSave.Click += BtnSave_Click;

            LinkButton btnCancel = new LinkButton
            {
                ID = this.ID + "_btnCancel",
                Text = "Cancel",
                CssClass = "btn btn-link"
            };
            pnlMenu.Controls.Add( btnCancel );
            btnCancel.Click += BtnCancel_Click;
        }

        private void BtnCancel_Click( object sender, EventArgs e )
        {
            HideSettings();
        }

        private void BtnSave_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                var widgityRefs = Widgities;
                var widgityRefIds = widgityRefs.Select( w => w.Id ).ToList();
                RockContext rockContext = new RockContext();
                rockContext.WrapTransaction( () =>
                {
                    WidgityService widgityService = new WidgityService( rockContext );

                    var toRemove = widgityService.Queryable( "WidgityType" )
                        .Where( w => w.EntityId == EntityId && w.EntityTypeId == EntityTypeId )
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
                            widgity.EntityTypeId = EntityTypeId;
                            widgity.EntityId = EntityId;
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
        }

        public void ShowSettings()
        {
            if ( Widgities == null )
            {
                LoadWidgities();
            }
            Mode = WidgityMode.Edit;
            ShowWidgities( false );
        }

        public void HideSettings()
        {
            Mode = WidgityMode.View;
            pnlMenu.Visible = false;
            CurrentEditWidgity = null;
            CurrentWidgityItems = null;
            LoadWidgities();
            ShowWidgities( false );
        }


        private void RegisterDragDropScript()
        {

            string script = string.Format( @"
function InitDrag() {{
                var containers = [];

var items = document.getElementsByClassName('widgitySource');
 $.each(items, function (i) {{
                    containers.push(items[i]);
                }});

             containers.push( document.getElementsByClassName('widgityDestination')[0]);
console.log(containers);
        var widgityDrake = dragula(containers, {{
            revertOnSpill: true,
            copySortSource: true,
            accepts: function (el, target) {{
                console.log(target);
                return true; //target.className === 'widgityDestination';
            }}
        }});

        widgityDrake.on('drop', function (el, target, source, sibling) {{
            var component = $(el).data('component-id');
            var order = $(target).children().index(el);
            if (source.className === 'widgitySource') {{
                var postback = ""javascript:__doPostBack('{0}', 'add-widgity|"" + component + ""|"" + order + ""')"";
            }}
            else {{
                var postback = ""javascript:__doPostBack('{0}', 'move-widgity|"" + component + ""|"" + order + ""')"";
            }}
            window.location = postback;
        }});

        var widgityItemDrake = dragula([document.getElementsByClassName('widgityItemControls')[0]], {{
            revertOnSpill: true,
            copySortSource: true,
            moves: function (el, container, handle) {{
                return handle.classList.contains('ui-sortable-handle');
            }},
            accepts: function (el, target) {{
                return target.className === 'widgityItemControls';
            }}
        }});

        widgityItemDrake.on('drop', function (el, target, source, sibling) {{
            var component = $(el).data('component-id');
            var order = $(target).children().index(el);

            var postback = ""javascript:__doPostBack('{0}', 'move-widgity-item|"" + component + ""|"" + order + ""')"";
            window.location = postback;
        }});

    }}
InitDrag();
", lbDragCommand.ClientID );
            ScriptManager.RegisterStartupScript( this, GetType(), "DragDrop", script, true );
        }
        #endregion
    }
}