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
using Rock.Utility.EntityCoding;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org.secc.Widgities
{
    /// <summary>
    /// Template block for developers to use to start a new block.
    /// </summary>
    [DisplayName( "Widgity Type Detail" )]
    [Category( "SECC > Widgity" )]
    [Description( "Block for managing a widgity." )]

    public partial class WidgityTypeDetail : Rock.Web.UI.RockBlock
    {

        #region Attribute Keys

        /// <summary>
        /// Keys to use for Block Attributes
        /// </summary>
        protected static class AttributeKey
        {

        }

        #endregion Attribute Keys

        #region PageParameterKeys

        /// <summary>
        /// Keys to use for Page Parameters
        /// </summary>
        protected static class PageParameterKey
        {
            internal const string WidgityTypeId = "WidgityTypeId";
        }

        #endregion PageParameterKeys


        private List<Rock.Model.Attribute> WidgityAttributes { get; set; }
        private List<Rock.Model.Attribute> WidgityItemAttributes { get; set; }

        #region Base Control Methods

        //  overrides of the base RockBlock methods (i.e. OnInit, OnLoad)

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            gWidgityAttributes.Actions.ShowAdd = true;
            gWidgityItemAttributes.Actions.ShowAdd = true;
            gWidgityAttributes.Actions.AddClick += gWidgityAttributes_AddClick;
            gWidgityItemAttributes.Actions.AddClick += gWidgityItemAttributes_AddClick;
            gWidgityAttributes.GridReorder += gWidgityAttributes_GridReorder;
            gWidgityItemAttributes.GridReorder += gWidgityItemAttributes_GridReorder;


            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }


        private void gWidgityAttributes_GridReorder( object sender, Rock.Web.UI.Controls.GridReorderEventArgs e )
        {
            WidgityAttributes = SortAttributes( WidgityAttributes, e.OldIndex, e.NewIndex );
            SaveState();
        }
        private void gWidgityItemAttributes_GridReorder( object sender, Rock.Web.UI.Controls.GridReorderEventArgs e )
        {
            WidgityItemAttributes = SortAttributes( WidgityItemAttributes, e.OldIndex, e.NewIndex );
            SaveState();
        }

        private List<Rock.Model.Attribute> SortAttributes( List<Rock.Model.Attribute> attributeList, int oldIndex, int newIndex )
        {
            var movedItem = attributeList.Where( a => a.Order == oldIndex ).FirstOrDefault();
            if ( movedItem != null )
            {
                if ( newIndex < oldIndex )
                {
                    // Moved up
                    foreach ( var otherItem in attributeList.Where( a => a.Order < oldIndex && a.Order >= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order + 1;
                    }
                }
                else
                {
                    // Moved Down
                    foreach ( var otherItem in attributeList.Where( a => a.Order > oldIndex && a.Order <= newIndex ) )
                    {
                        otherItem.Order = otherItem.Order - 1;
                    }
                }

                movedItem.Order = newIndex;
            }
            return attributeList.OrderBy( a => a.Order ).ToList();
        }

        private void gWidgityAttributes_AddClick( object sender, EventArgs e )
        {
            ShowWidgityAttributeEditor( Guid.NewGuid() );
        }

        private void gWidgityItemAttributes_AddClick( object sender, EventArgs e )
        {
            ShowWidgityItemAttributeEditor( Guid.NewGuid() );
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            if ( ViewState["WidgityAttribues"] != null && ViewState["WidgityAttribues"] is string )
            {
                WidgityAttributes = JsonConvert.DeserializeObject<List<Rock.Model.Attribute>>( ViewState["WidgityAttribues"] as string );
            }
            else
            {
                WidgityAttributes = new List<Rock.Model.Attribute>();
            }

            if ( ViewState["WidgityItemAttributes"] != null && ViewState["WidgityItemAttributes"] is string )
            {
                WidgityItemAttributes = JsonConvert.DeserializeObject<List<Rock.Model.Attribute>>( ViewState["WidgityItemAttributes"] as string );
            }
            else
            {
                WidgityItemAttributes = new List<Rock.Model.Attribute>();
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
                btnExport.Visible = PageParameter( PageParameterKey.WidgityTypeId ).AsInteger() != 0;
                pdAuditDetails.Visible = btnExport.Visible;
                var entityTypes = new EntityTypeService( new RockContext() ).GetEntities()
                   .OrderBy( t => t.FriendlyName )
                   .ToList();
                lbEntityTypes.DataSource = entityTypes;
                lbEntityTypes.DataBind();

                pCategory.EntityTypeId = EntityTypeCache.GetId( typeof( WidgityType ) ).Value;

                ShowDetails();
                BindAttributeGrids();
            }
        }


        private void BindAttributeGrids( bool setValues = true )
        {
            if ( setValues )
            {
                RockContext rockContext = new RockContext();
                AttributeService attributeService = new AttributeService( rockContext );

                var widgityTypeId = PageParameter( PageParameterKey.WidgityTypeId );

                var widgityEntityTypeId = EntityTypeCache.Get( typeof( Widgity ) ).Id;
                WidgityAttributes = attributeService.Queryable()
                    .Where( a => a.EntityTypeId == widgityEntityTypeId && a.EntityTypeQualifierValue == widgityTypeId )
                    .OrderBy( a => a.Order )
                    .ToList();

                var widgityItemEntityTypeId = EntityTypeCache.Get( typeof( WidgityItem ) ).Id;
                WidgityItemAttributes = attributeService.Queryable()
                    .Where( a => a.EntityTypeId == widgityItemEntityTypeId && a.EntityTypeQualifierValue == widgityTypeId )
                    .OrderBy( a => a.Order )
                    .ToList();

                SaveState();
            }

            gWidgityAttributes.DataSource = WidgityAttributes.Select( a => new
            {
                a.Guid,
                a.Name,
                a.Key,
                a.IsRequired,
                FieldType = FieldTypeCache.Get( a.FieldTypeId ).Name
            } ).ToList();
            gWidgityAttributes.DataBind();

            gWidgityItemAttributes.DataSource = WidgityItemAttributes.Select( a => new
            {
                a.Guid,
                a.Name,
                a.Key,
                a.IsRequired,
                FieldType = FieldTypeCache.Get( a.FieldTypeId ).Name
            } ).ToList();
            gWidgityItemAttributes.DataBind();
        }

        private WidgityType GetWidgityType()
        {
            return GetWidgityType( new WidgityTypeService( new RockContext() ) );
        }

        private WidgityType GetWidgityType( WidgityTypeService widgityTypeService )
        {
            var widgityType = widgityTypeService.Get( PageParameter( PageParameterKey.WidgityTypeId ).AsInteger() );
            if ( widgityType == null )
            {
                widgityType = new WidgityType
                {
                    CategoryId = 0,
                    EntityTypes = new List<EntityType>()
                };
            }
            return widgityType;
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

        protected void cbHasItems_CheckedChanged( object sender, EventArgs e )
        {
            pnlWidgityItemAttributes.Visible = cbHasItems.Checked;
        }

        #endregion

        #region Methods
        private void ShowDetails()
        {
            WidgityType widgityType = GetWidgityType();

            tbName.Text = widgityType.Name;
            tbDescription.Text = widgityType.Description;
            lcCommands.SelectedValue = widgityType.EnabledLavaCommands;
            tbIcon.Text = widgityType.Icon;
            ceMarkup.Text = widgityType.Markdown;
            lbEntityTypes.SetValues( widgityType.EntityTypes.Select( w => w.Id.ToString() ).ToList() );
            pCategory.SetValue( widgityType.CategoryId );
            cbHasItems.Checked = widgityType.HasItems;
            pnlWidgityItemAttributes.Visible = cbHasItems.Checked;
        }
        #endregion


        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            rockContext.WrapTransaction( () =>
            {
                WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );
                AttributeService attributeService = new AttributeService( rockContext );
                WidgityType widgityType = GetWidgityType( widgityTypeService );

                if ( widgityType.Id == 0 )
                {
                    widgityTypeService.Add( widgityType );
                }

                widgityType.Name = tbName.Text;
                widgityType.Description = tbDescription.Text;
                widgityType.EnabledLavaCommands = lcCommands.SelectedValue;
                widgityType.Icon = tbIcon.Text;
                widgityType.Markdown = ceMarkup.Text;
                widgityType.HasItems = cbHasItems.Checked;
                var _ = widgityType.EntityTypes.ToList(); //Attach the entity types to context
                widgityType.EntityTypes = new EntityTypeService( rockContext ).GetByIds( lbEntityTypes.SelectedValuesAsInt ).ToList();
                widgityType.CategoryId = pCategory.SelectedValueAsId();
                rockContext.SaveChanges();

                //Widgity Attributes
                var widgityEntityTypeId = EntityTypeCache.Get( typeof( Widgity ) ).Id;

                var actualAttributes = attributeService.Queryable()
                    .Where( a => a.EntityTypeId == widgityEntityTypeId && a.EntityTypeQualifierValue == widgityType.Id.ToString() )
                    .OrderBy( a => a.Order )
                    .ToList();

                //Remove deleted attributes
                foreach ( var attribute in actualAttributes )
                {
                    if ( !WidgityAttributes.Where( a => a.Guid == attribute.Guid ).Any() )
                    {
                        attributeService.Delete( attribute );
                    }
                }

                //Update db from viewstate
                foreach ( var attribute in WidgityAttributes )
                {
                    if ( attribute.Id == 0 )
                    {
                        attribute.Order = WidgityAttributes.IndexOf( attribute );
                        attribute.EntityTypeQualifierValue = widgityType.Id.ToString();
                        attributeService.Add( attribute );
                    }
                    else
                    {
                        var trackedAttribute = actualAttributes.Where( a => a.Guid == attribute.Guid ).FirstOrDefault();
                        trackedAttribute.CopyPropertiesFrom( attribute );
                        foreach ( var qualifier in trackedAttribute.AttributeQualifiers )
                        {
                            var value = attribute.AttributeQualifiers.Where( q => q.Key == qualifier.Key ).FirstOrDefault();
                            if ( value != null )
                            {
                                qualifier.Value = value.Value;
                            }
                        }
                        trackedAttribute.Order = WidgityAttributes.IndexOf( attribute );
                    }
                }
                rockContext.SaveChanges();


                //Widgity Item Attributes
                var widgityItemEntityTypeId = EntityTypeCache.Get( typeof( WidgityItem ) ).Id;

                var actualItemAttributes = attributeService.Queryable()
                    .Where( a => a.EntityTypeId == widgityItemEntityTypeId && a.EntityTypeQualifierValue == widgityType.Id.ToString() )
                    .OrderBy( a => a.Order )
                    .ToList();

                //Remove deleted attributes
                foreach ( var attribute in actualItemAttributes )
                {
                    if ( !WidgityItemAttributes.Where( a => a.Guid == attribute.Guid ).Any() )
                    {
                        attributeService.Delete( attribute );
                    }
                }

                //Update db from viewstate
                foreach ( var attribute in WidgityItemAttributes )
                {
                    if ( attribute.Id == 0 )
                    {
                        attribute.Order = WidgityItemAttributes.IndexOf( attribute );
                        attribute.EntityTypeQualifierValue = widgityType.Id.ToString();
                        attributeService.Add( attribute );
                    }
                    else
                    {
                        var trackedAttribute = actualItemAttributes.Where( a => a.Guid == attribute.Guid ).FirstOrDefault();
                        trackedAttribute.CopyPropertiesFrom( attribute );
                        foreach ( var qualifier in trackedAttribute.AttributeQualifiers )
                        {
                            var value = attribute.AttributeQualifiers.Where( q => q.Key == qualifier.Key ).FirstOrDefault();
                            if ( value != null )
                            {
                                qualifier.Value = value.Value;
                            }
                        }
                        trackedAttribute.Order = WidgityItemAttributes.IndexOf( attribute );
                    }
                }
                rockContext.SaveChanges();
            } );
            WidgityTypeCache.Clear();
            WidgityCache.Clear();
            WidgityItemCache.Clear();
            NavigateToParentPage();
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        protected void gWdigityAttributes_Edit( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            ShowWidgityAttributeEditor( ( Guid ) e.RowKeyValue );
        }

        protected void gWdigityItemAttributes_Edit( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            ShowWidgityItemAttributeEditor( ( Guid ) e.RowKeyValue );
        }

        private void ShowWidgityAttributeEditor( Guid attributeGuid )
        {
            var attribute = WidgityAttributes.Where( a => a.Guid == attributeGuid ).FirstOrDefault();
            if ( attribute == null )
            {
                attribute = new Rock.Model.Attribute
                {
                    Guid = attributeGuid,
                    FieldTypeId = 1,
                    EntityTypeId = EntityTypeCache.Get( typeof( Widgity ) ).Id,
                    EntityTypeQualifierColumn = "WidgityTypeId",
                    EntityTypeQualifierValue = PageParameter( PageParameterKey.WidgityTypeId )
                };
            }
            edtWidgityAttributes.SetAttributeProperties( attribute, typeof( Widgity ) );
            dlgWidgityTypeAttributes.Show();
        }

        private void ShowWidgityItemAttributeEditor( Guid widgityTypeAttributeGuid )
        {
            var attribute = WidgityItemAttributes.Where( a => a.Guid == widgityTypeAttributeGuid ).FirstOrDefault();

            if ( attribute == null )
            {
                attribute = new Rock.Model.Attribute
                {
                    Guid = widgityTypeAttributeGuid,
                    FieldTypeId = 1,
                    EntityTypeId = EntityTypeCache.Get( typeof( WidgityItem ) ).Id,
                    EntityTypeQualifierColumn = "WidgityTypeId",
                    EntityTypeQualifierValue = PageParameter( PageParameterKey.WidgityTypeId )
                };
            }
            edtWidgityItemAttributes.SetAttributeProperties( attribute );
            dlgWidgityItemAttributes.Show();
        }

        protected void gWdigityAttributes_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var toRemove = WidgityAttributes.Where( a => a.Guid == ( Guid ) e.RowKeyValue ).FirstOrDefault();
            if ( toRemove != null )
            {
                WidgityAttributes.Remove( toRemove );
                SaveState();
            }
        }

        protected void gWdigityItemAttributes_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            var toRemove = WidgityItemAttributes.Where( a => a.Guid == ( Guid ) e.RowKeyValue ).FirstOrDefault();
            if ( toRemove != null )
            {
                WidgityItemAttributes.Remove( toRemove );
                SaveState();
            }
        }


        protected void dlgWidgityTypeAttributes_SaveClick( object sender, EventArgs e )
        {
            SaveWidgityAttribute();
            dlgWidgityTypeAttributes.Hide();
        }

        protected void dlgWidgityItemAttributes_SaveClick( object sender, EventArgs e )
        {
            SaveWidgityItemAttribute();
            dlgWidgityItemAttributes.Hide();
        }

        private void SaveWidgityAttribute()
        {
            var attribute = new Rock.Model.Attribute();

            edtWidgityAttributes.GetAttributeProperties( attribute );

            attribute.EntityTypeId = EntityTypeCache.Get( typeof( Widgity ) ).Id;
            attribute.EntityTypeQualifierColumn = "WidgityTypeId";
            attribute.EntityTypeQualifierValue = PageParameter( PageParameterKey.WidgityTypeId );

            var existingAttribute = WidgityAttributes.Where( a => a.Guid == attribute.Guid ).FirstOrDefault();

            if ( existingAttribute == null ) //new attribute
            {
                attribute.Order = WidgityAttributes.Any() ? WidgityAttributes.Max( a => a.Order ) + 1 : 0;
                WidgityAttributes.Add( attribute );
            }
            else //existing attribute
            {
                foreach ( var att in WidgityAttributes )
                {
                    if ( att.Guid == existingAttribute.Guid )
                    {
                        var index = WidgityAttributes.IndexOf( att );
                        WidgityAttributes.Remove( att );
                        WidgityAttributes.Insert( index, attribute );
                        break;
                    }
                }
            }
            SaveState();
        }

        private void SaveWidgityItemAttribute()
        {
            var attribute = new Rock.Model.Attribute();

            edtWidgityItemAttributes.GetAttributeProperties( attribute );

            attribute.EntityTypeId = EntityTypeCache.Get( typeof( WidgityItem ) ).Id;
            attribute.EntityTypeQualifierColumn = "WidgityTypeId";
            attribute.EntityTypeQualifierValue = PageParameter( PageParameterKey.WidgityTypeId );

            var existingAttribute = WidgityItemAttributes.Where( a => a.Guid == attribute.Guid ).FirstOrDefault();

            if ( existingAttribute == null ) //new attribute
            {
                attribute.Order = WidgityItemAttributes.Any() ? WidgityItemAttributes.Max( a => a.Order ) + 1 : 0;
                WidgityItemAttributes.Add( attribute );
            }
            else //existing attribute
            {
                foreach ( var att in WidgityItemAttributes )
                {
                    if ( att.Guid == existingAttribute.Guid )
                    {
                        var index = WidgityItemAttributes.IndexOf( att );
                        WidgityItemAttributes.Remove( att );
                        WidgityItemAttributes.Insert( index, attribute );
                        break;
                    }
                }
            }
            SaveState();
        }

        private void SaveState()
        {
            ViewState["WidgityAttribues"] = JsonConvert.SerializeObject( WidgityAttributes );
            ViewState["WidgityItemAttributes"] = JsonConvert.SerializeObject( WidgityItemAttributes );
            BindAttributeGrids( false );
        }

        protected void dlgWidgityTypeAttributes_SaveThenAddClick( object sender, EventArgs e )
        {
            SaveWidgityAttribute();
            ShowWidgityAttributeEditor( Guid.NewGuid() );
        }

        protected void dlgWidgityItemAttributes_SaveThenAddClick( object sender, EventArgs e )
        {
            SaveWidgityItemAttribute();
            ShowWidgityItemAttributeEditor( Guid.NewGuid() );
        }

        protected void btnExport_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            WidgityTypeService widgityTypeService = new WidgityTypeService( rockContext );
            var widgityType = widgityTypeService.Get( PageParameter( PageParameterKey.WidgityTypeId ).AsInteger() );
            if ( widgityType == null )
            {
                return;
            }

            var coder = new EntityCoder( new RockContext() );
            coder.EnqueueEntity( widgityType, new WidgityTypeExporter() );

            var container = coder.GetExportedEntities();

            Page.EnableViewState = false;
            Page.Response.Clear();
            Page.Response.ContentType = "application/json";
            Page.Response.AppendHeader( "Content-Disposition", string.Format( "attachment; filename=\"{0}_{1}.json\"", widgityType.Name.MakeValidFileName(), RockDateTime.Now.ToString( "yyyyMMddHHmm" ) ) );
            Page.Response.Write( Newtonsoft.Json.JsonConvert.SerializeObject( container, Newtonsoft.Json.Formatting.Indented ) );
            Page.Response.Flush();
            Page.Response.End();

        }
    }
}