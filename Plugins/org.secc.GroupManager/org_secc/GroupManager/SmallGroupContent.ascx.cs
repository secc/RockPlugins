// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.Linq.Expressions;
using System.Runtime.Caching;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using DotLiquid;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Field.Types;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    /// <summary>
    /// Block to display content items, html, xml, or transformed xml based on a SQL query or stored procedure.
    /// </summary>
    [DisplayName( "Small Group Content" )]
    [Category( "Groups" )]
    [Description( "Block to display dynamic small group content for a group." )]

    // Block Properties
    [LinkedPage( "Detail Page", "The page to navigate to for details.", false, "", "", 0 )]

    // Custom Settings
    [DefinedTypeField( "ItemType", "Content item types to display on page", true, "", "CustomSetting" )]
    [EnumsField( "Status", "Include items with the following status.", typeof( ContentChannelItemStatus ), false, "2", "CustomSetting" )]
    [IntegerField( "Count", "The maximum number of items to display.", false, 5, "CustomSetting" )]
    [IntegerField( "Cache Duration", "Number of seconds to cache the content.", false, 3600, "CustomSetting" )]
    [BooleanField( "Enable Debug", "Enabling debug will display the fields of the first 5 items to help show you wants available for your liquid.", false, "CustomSetting" )]
    [BooleanField( "Query Parameter Filtering", "Determines if block should evaluate the query string parameters for additional filter criteria.", false, "CustomSetting" )]
    [BooleanField( "ShowSidebar", "Determines if sidebar index should be displayed.", false, "CustomSetting" )]
    [BooleanField( "ShowCalendar", "Show a calendar to select a new date for the lesson?", false, "CustomSetting" )]
    [BooleanField( "FilterByDate", "Filter to show only those content items which are in date.", false, "CustomSetting" )]
    [TextField( "Order", "The specifics of how items should be ordered. This value is set through configuration and should not be modified here.", false, "", "CustomSetting" )]
    [BooleanField( "Merge Content", "Should the content data and attribute values be merged using the liquid template engine.", false, "CustomSetting" )]
    [BooleanField( "Set Page Title", "Determines if the block should set the page title with the channel name or content item.", false, "CustomSetting" )]
    [TextField( "Meta Description Attribute", "Attribute to use for storing the description attribute.", false, "", "CustomSetting" )]
    [TextField( "Meta Image Attribute", "Attribute to use for storing the image attribute.", false, "", "CustomSetting" )]
    [CodeEditorField( "ContentLava", "Lava to display content with.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300,
        true, @"<div class='content'> {% for item in Items %} <div class='row'> <h2> {{ item.Title }} </h2> <div class=''> {{item.Content}} </div> </div> {% endfor %} </div>", "CustomSetting" )]

    public partial class SmallGroupContent : RockBlockCustomSettings
    {
        #region Fields

        private readonly string ITEM_TYPE_NAME = "Rock.Model.ContentChannelItem";
        private readonly string CONTENT_CACHE_KEY = "Content";
        private readonly string TEMPLATE_CACHE_KEY = "Template";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the channel unique identifier.
        /// </summary>
        /// <value>
        /// The channel unique identifier.
        /// </value>
        public Guid? ChannelGuid { get; set; }

        /// <summary>
        /// Gets the settings tool tip.
        /// </summary>
        /// <value>
        /// The settings tool tip.
        /// </value>
        public override string SettingsToolTip
        {
            get
            {
                return "Edit Criteria";
            }
        }

        private Group CurrentGroup;
        private GroupMember CurrentGroupMember;


        #endregion

        #region Base Control Methods

        /// <summary>
        /// Restores the view-state information from a previous user control request that was saved by the <see cref="M:System.Web.UI.UserControl.SaveViewState" /> method.
        /// </summary>
        /// <param name="savedState">An <see cref="T:System.Object" /> that represents the user control state to be restored.</param>
        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            ChannelGuid = ViewState["ChannelGuid"] as Guid?;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // Switch does not automatically initialize again after a partial-postback.  This script 
            // looks for any switch elements that have not been initialized and re-intializes them.
            string script = @"
$(document).ready(function() {
    $('.switch > input').each( function () {
        $(this).parent().switch('init');
    });
});
";
            ScriptManager.RegisterStartupScript( this.Page, this.Page.GetType(), "toggle-switch-init", script, true );

            cblStatus.BindToEnum<ContentChannelItemStatus>();

            this.BlockUpdated += ContentDynamic_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            Button btnTrigger = new Button();
            btnTrigger.ClientIDMode = System.Web.UI.ClientIDMode.Static;
            btnTrigger.ID = "rock-config-cancel-trigger";
            btnTrigger.Click += btnTrigger_Click;
            pnlEditModal.Controls.Add( btnTrigger );

            AsyncPostBackTrigger trigger = new AsyncPostBackTrigger();
            trigger.ControlID = "rock-config-cancel-trigger";
            trigger.EventName = "Click";
            upnlContent.Triggers.Add( trigger );

            GetGroup();

        }

        private void GetGroup()
        {
            RockContext rockContext = new RockContext();
            LoadSession( rockContext );

            if ( CurrentGroup != null )
            {
                Group parentGroup = CurrentGroup.ParentGroup;
                parentGroup.LoadAttributes();
                ChannelGuid = parentGroup.GetAttributeValue( "ContentChannel" ).AsGuid();
            }
        }

        private void LoadSession( RockContext rockContext )
        {
            int groupId = PageParameter( "GroupId" ).AsInteger();
            if ( groupId == 0 && Session["CurrentGroupManagerGroup"] != null )
            {
                groupId = ( int ) Session["CurrentGroupManagerGroup"];
            }

            CurrentGroup = new GroupService( rockContext ).Get( groupId );
            if ( CurrentGroup != null )
            {
                Session["CurrentGroupManagerGroup"] = groupId;
                CurrentGroupMember = CurrentGroup.Members
                    .Where( gm => gm.PersonId == CurrentUser.PersonId )
                    .FirstOrDefault();
            }
            else
            {
                CurrentGroupMember = null;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( CurrentGroup != null && CurrentGroup.IsAuthorized( Authorization.EDIT, CurrentUser.Person ) )
            {
                base.OnLoad( e );

                if ( !Page.IsPostBack )
                {
                    ShowView();
                    if ( GetAttributeValue( "ShowSidebar" ).AsBoolean() )
                    {
                        pnlView.CssClass = "col-md-9";
                        ShowSidebar();
                    }
                }

                if ( GetAttributeValue( "FilterByDate" ).AsBoolean() )
                {
                    var script = string.Format( @"
$('#updateProgress').hide();
document.getElementById('{0}').onchange = function() {{
$('#updateProgress').show();
    __doPostBack('{1}', 'OnClick');
}};
", dpCalendar.ClientID, btnCalendar.UniqueID );
                    ScriptManager.RegisterStartupScript( Page, Page.GetType(), "CalendarScript",
                        script, true );
                }
            }
            else
            {
                pnlSidebar.Visible = false;
                pnlView.Visible = false;
                nbAlert.Visible = true;
                nbAlert.Text = "You are not authorized to view this page.";
            }
        }



        /// <summary>
        /// Saves any user control view-state changes that have occurred since the last page postback.
        /// </summary>
        /// <returns>
        /// Returns the user control's current view state. If there is no view state associated with the control, it returns null.
        /// </returns>
        protected override object SaveViewState()
        {
            ViewState["ChannelGuid"] = ChannelGuid;

            return base.SaveViewState();
        }

        #endregion

        #region Events

        void ContentDynamic_BlockUpdated( object sender, EventArgs e )
        {
            ShowView();
        }

        void btnTrigger_Click( object sender, EventArgs e )
        {
            mdEdit.Hide();
            pnlEditModal.Visible = false;

            ShowView();
        }

        protected void lbSave_Click( object sender, EventArgs e )
        {
            if ( !Page.IsValid )
            {
                return;
            }

            var ddlValue = ddlTypes.SelectedItem.Value;

            SetAttributeValue( "ItemType", ddlValue );
            SetAttributeValue( "EnableDebug", cbDebug.Checked.ToString() );
            SetAttributeValue( "MergeContent", cbMergeContent.Checked.ToString() );
            SetAttributeValue( "CacheDuration", ( nbCacheDuration.Text.AsIntegerOrNull() ?? 5 ).ToString() );
            SetAttributeValue( "QueryParameterFiltering", cbQueryParamFiltering.Checked.ToString() );
            SetAttributeValue( "ShowSidebar", cbShowSidebar.Checked.ToString() );
            SetAttributeValue( "ShowCalendar", cbShowCalendar.Checked.ToString() );
            SetAttributeValue( "FilterByDate", cbFilterByDate.Checked.ToString() );
            SetAttributeValue( "Order", kvlOrder.Value );
            SetAttributeValue( "SetPageTitle", cbSetPageTitle.Checked.ToString() );

            var ppFieldType = new PageReferenceFieldType();
            SetAttributeValue( "DetailPage", ppFieldType.GetEditValue( ppDetailPage, null ) );

            SetAttributeValue( "ContentLava", ceContentLava.Text );

            SaveAttributeValues();

            FlushCacheItem( CONTENT_CACHE_KEY + ChannelGuid );
            FlushCacheItem( TEMPLATE_CACHE_KEY + ChannelGuid );

            mdEdit.Hide();
            pnlEditModal.Visible = false;
            upnlContent.Update();

            ShowView();
        }

        /// <summary>
        /// Handles the AddFilterClick event of the groupControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void groupControl_AddFilterClick( object sender, EventArgs e )
        {
            FilterGroup groupControl = sender as FilterGroup;
            FilterField filterField = new FilterField();
            filterField.DataViewFilterGuid = Guid.NewGuid();
            groupControl.Controls.Add( filterField );
            filterField.ID = string.Format( "ff_{0}", filterField.DataViewFilterGuid.ToString( "N" ) );

            // Remove the 'Other Data View' Filter as it doesn't really make sense to have it available in this scenario
            filterField.ExcludedFilterTypes = new string[] { typeof( Rock.Reporting.DataFilter.OtherDataViewFilter ).FullName };
            filterField.FilteredEntityTypeName = groupControl.FilteredEntityTypeName;
            filterField.Expanded = true;
        }

        /// <summary>
        /// Handles the AddGroupClick event of the groupControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void groupControl_AddGroupClick( object sender, EventArgs e )
        {
            FilterGroup groupControl = sender as FilterGroup;
            FilterGroup childGroupControl = new FilterGroup();
            childGroupControl.DataViewFilterGuid = Guid.NewGuid();
            groupControl.Controls.Add( childGroupControl );
            childGroupControl.ID = string.Format( "fg_{0}", childGroupControl.DataViewFilterGuid.ToString( "N" ) );
            childGroupControl.FilteredEntityTypeName = groupControl.FilteredEntityTypeName;
            childGroupControl.FilterType = FilterExpressionType.GroupAll;
        }

        /// <summary>
        /// Handles the DeleteClick event of the filterControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void filterControl_DeleteClick( object sender, EventArgs e )
        {
            FilterField fieldControl = sender as FilterField;
            fieldControl.Parent.Controls.Remove( fieldControl );
        }

        /// <summary>
        /// Handles the DeleteGroupClick event of the groupControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void groupControl_DeleteGroupClick( object sender, EventArgs e )
        {
            FilterGroup groupControl = sender as FilterGroup;
            groupControl.Parent.Controls.Remove( groupControl );
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Shows the settings.
        /// </summary>
        protected override void ShowSettings()
        {
            pnlEditModal.Visible = true;
            upnlContent.Update();
            mdEdit.Show();

            foreach ( string status in GetAttributeValue( "Status" ).SplitDelimitedValues() )
            {
                var li = cblStatus.Items.FindByValue( status );
                if ( li != null )
                {
                    li.Selected = true;
                }
            }

            DefinedTypeCache defindedTypeCache = DefinedTypeCache.Read( "81717CA0-4403-4F5A-92AB-1F6328F01254".AsGuid() );
            ddlTypes.DataSource = defindedTypeCache.DefinedValues;
            ddlTypes.DataValueField = "Guid";
            ddlTypes.DataTextField = "Value";
            ddlTypes.DataBind();
            var itemTypeValue = GetAttributeValue( "ItemType" );
            try
            {
                ddlTypes.SelectedValue = itemTypeValue;
            }
            catch
            {
            }

            cbDebug.Checked = GetAttributeValue( "EnableDebug" ).AsBoolean();
            cbMergeContent.Checked = GetAttributeValue( "MergeContent" ).AsBoolean();
            cbSetPageTitle.Checked = GetAttributeValue( "SetPageTitle" ).AsBoolean();
            nbCacheDuration.Text = GetAttributeValue( "CacheDuration" );
            cbQueryParamFiltering.Checked = GetAttributeValue( "QueryParameterFiltering" ).AsBoolean();
            cbShowSidebar.Checked = GetAttributeValue( "ShowSidebar" ).AsBoolean();
            cbShowCalendar.Checked = GetAttributeValue( "ShowCalendar" ).AsBoolean();
            cbFilterByDate.Checked = GetAttributeValue( "FilterByDate" ).AsBoolean();

            var ppFieldType = new PageReferenceFieldType();
            ppFieldType.SetEditValue( ppDetailPage, null, GetAttributeValue( "DetailPage" ) );

            var directions = new Dictionary<string, string>();
            directions.Add( "", "" );
            directions.Add( SortDirection.Ascending.ConvertToInt().ToString(), "Ascending" );
            directions.Add( SortDirection.Descending.ConvertToInt().ToString(), "Descending" );
            kvlOrder.CustomValues = directions;
            kvlOrder.Value = GetAttributeValue( "Order" );
            kvlOrder.Required = true;

            ceContentLava.Text = GetAttributeValue( "ContentLava" );

            ShowEdit();

            upnlContent.Update();
        }

        /// <summary>
        /// Binds the grid.
        /// </summary>
        private void ShowView()
        {
            nbContentError.Visible = false;
            upnlContent.Update();

            //Show calendar and set date to now if not yet set.
            pnlCalendar.Visible = GetAttributeValue( "ShowCalendar" ).AsBoolean();
            if ( dpCalendar.SelectedDate == null )
            {
                dpCalendar.SelectedDate = Rock.RockDateTime.Now;
            }

            var pageRef = CurrentPageReference;
            pageRef.Parameters.AddOrReplace( "Page", "PageNum" );

            Dictionary<string, object> linkedPages = new Dictionary<string, object>();
            linkedPages.Add( "DetailPage", LinkedPageUrl( "DetailPage", null ) );

            var errorMessages = new List<string>();
            List<ContentChannelItem> content;
            try
            {
                content = GetContent( errorMessages ) ?? new List<ContentChannelItem>();
            }
            catch ( Exception ex )
            {
                this.LogException( ex );
                Exception exception = ex;
                while ( exception != null )
                {
                    errorMessages.Add( exception.Message );
                    exception = exception.InnerException;
                }

                content = new List<ContentChannelItem>();
            }

            if ( errorMessages.Any() )
            {
                nbContentError.Text = "ERROR: There was a problem getting content...<br/> ";
                nbContentError.NotificationBoxType = NotificationBoxType.Danger;
                nbContentError.Details = errorMessages.AsDelimited( "<br/>" );
                nbContentError.Visible = true;
            }

            var pagination = new Pagination();
            pagination.ItemCount = content.Count();
            pagination.PageSize = GetAttributeValue( "Count" ).AsInteger();
            pagination.CurrentPage = PageParameter( "Page" ).AsIntegerOrNull() ?? 1;
            pagination.UrlTemplate = pageRef.BuildUrl();
            var currentPageContent = pagination.GetCurrentPageItems( content );

            var globalAttributeFields = Rock.Web.Cache.GlobalAttributesCache.GetMergeFields( CurrentPerson );

            // Merge content and attribute fields if block is configured to do so.
            if ( GetAttributeValue( "MergeContent" ).AsBoolean() )
            {
                var itemMergeFields = new Dictionary<string, object>();

                if ( CurrentPerson != null )
                {
                    itemMergeFields.Add( "CurrentPerson", CurrentPerson );
                }
                globalAttributeFields.ToList().ForEach( d => itemMergeFields.Add( d.Key, d.Value ) );

                foreach ( var item in currentPageContent )
                {
                    itemMergeFields.AddOrReplace( "Item", item );
                    item.Content = item.Content.ResolveMergeFields( itemMergeFields );
                    foreach ( var attributeValue in item.AttributeValues )
                    {
                        attributeValue.Value.Value = attributeValue.Value.Value.ResolveMergeFields( itemMergeFields );
                    }
                }
            }

            // add context to merge fields
            var contextEntityTypes = RockPage.GetContextEntityTypes();

            var contextObjects = new Dictionary<string, object>();
            foreach ( var conextEntityType in contextEntityTypes )
            {
                var contextObject = RockPage.GetCurrentContext( conextEntityType );
                contextObjects.Add( conextEntityType.FriendlyName, contextObject );
            }


            var mergeFields = new Dictionary<string, object>();
            mergeFields.Add( "PageParameter", RockPage.PageParameters() );
            mergeFields.Add( "Pagination", pagination );
            mergeFields.Add( "LinkedPages", linkedPages );
            mergeFields.Add( "Items", currentPageContent );
            mergeFields.Add( "Campuses", CampusCache.All() );
            mergeFields.Add( "CurrentPerson", CurrentPerson );
            mergeFields.Add( "Context", contextObjects );

            globalAttributeFields.ToList().ForEach( d => mergeFields.Add( d.Key, d.Value ) );
            mergeFields.Add( "RockVersion", Rock.VersionInfo.VersionInfo.GetRockProductVersionNumber() );

            // enable showing debug info
            if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
            {
                mergeFields["Items"] = currentPageContent.Take( 5 ).ToList();

                lDebug.Visible = true;

                lDebug.Text = mergeFields.lavaDebugInfo();

                mergeFields["Items"] = currentPageContent;
            }
            else
            {
                lDebug.Visible = false;
                lDebug.Text = string.Empty;
            }

            // set page title
            if ( GetAttributeValue( "SetPageTitle" ).AsBoolean() && content.Count > 0 )
            {
                if ( string.IsNullOrWhiteSpace( PageParameter( "Item" ) ) )
                {
                    // set title to channel name
                    string channelName = content.Select( c => c.ContentChannel.Name ).FirstOrDefault();
                    RockPage.BrowserTitle = String.Format( "{0} | {1}", channelName, RockPage.Site.Name );
                    RockPage.PageTitle = channelName;
                    RockPage.Header.Title = String.Format( "{0} | {1}", channelName, RockPage.Site.Name );
                }
                else
                {
                    string itemTitle = content.Select( c => c.Title ).FirstOrDefault();
                    RockPage.PageTitle = itemTitle;
                    RockPage.BrowserTitle = String.Format( "{0} | {1}", itemTitle, RockPage.Site.Name );
                    RockPage.Header.Title = String.Format( "{0} | {1}", itemTitle, RockPage.Site.Name );
                }
            }

            // set description meta tag
            string metaDescriptionAttributeValue = GetAttributeValue( "MetaDescriptionAttribute" );
            if ( !string.IsNullOrWhiteSpace( metaDescriptionAttributeValue ) && content.Count > 0 )
            {
                string attributeValue = GetMetaValueFromAttribute( metaDescriptionAttributeValue, content );

                if ( !string.IsNullOrWhiteSpace( attributeValue ) )
                {
                    // remove default meta description
                    RockPage.Header.Description = attributeValue.SanitizeHtml( true );
                }
            }

            // add meta images
            string metaImageAttributeValue = GetAttributeValue( "MetaImageAttribute" );
            if ( !string.IsNullOrWhiteSpace( metaImageAttributeValue ) && content.Count > 0 )
            {
                string attributeValue = GetMetaValueFromAttribute( metaImageAttributeValue, content );

                if ( !string.IsNullOrWhiteSpace( attributeValue ) )
                {
                    HtmlMeta metaDescription = new HtmlMeta();
                    metaDescription.Name = "og:image";
                    metaDescription.Content = string.Format( "{0}://{1}/GetImage.ashx?guid={2}", Request.Url.Scheme, Request.Url.Authority, attributeValue );
                    RockPage.Header.Controls.Add( metaDescription );

                    HtmlLink imageLink = new HtmlLink();
                    imageLink.Attributes.Add( "rel", "image_src" );
                    imageLink.Attributes.Add( "href", string.Format( "{0}://{1}/GetImage.ashx?guid={2}", Request.Url.Scheme, Request.Url.Authority, attributeValue ) );
                    RockPage.Header.Controls.Add( imageLink );
                }
            }

            var template = GetTemplate();
            var render = template.Render( Hash.FromDictionary( mergeFields ) );

            phContent.Controls.Add( new LiteralControl( render ) );
        }

        private void ShowSidebar()
        {
            pnlSidebar.CssClass = "col-md-3";
            pnlSidebar.Visible = true;
            var errorMessages = new List<string>();
            List<ContentChannelItem> content;
            try
            {
                content = GetContent( errorMessages, false ) ?? new List<ContentChannelItem>();
            }
            catch ( Exception ex )
            {
                this.LogException( ex );
                Exception exception = ex;
                while ( exception != null )
                {
                    errorMessages.Add( exception.Message );
                    exception = exception.InnerException;
                }

                content = new List<ContentChannelItem>();
            }
            var hgcUl = new HtmlGenericContainer( "ul" );
            pnlSidebar.Controls.Add( hgcUl );
            foreach ( var item in content )
            {
                var hgcLi = new HtmlGenericContainer( "li" );
                hgcUl.Controls.Add( hgcLi );
                HyperLink hyp = new HyperLink();
                hyp.NavigateUrl = Request.Url.AbsolutePath + "?GroupId=" + CurrentGroup.Id.ToString() + "&Item=" + item.Id.ToString();
                hyp.Text = item.Title;
                hgcLi.Controls.Add( hyp );
            }
        }

        private string GetMetaValueFromAttribute( string input, List<ContentChannelItem> content )
        {
            string attributeEntityType = input.Split( '^' )[0].ToString() ?? "C";
            string attributeKey = input.Split( '^' )[1].ToString() ?? "";

            string attributeValue = string.Empty;

            if ( attributeEntityType == "C" )
            {
                attributeValue = content.FirstOrDefault().ContentChannel.AttributeValues.Where( a => a.Key == attributeKey ).Select( a => a.Value.Value ).FirstOrDefault();
            }
            else
            {
                attributeValue = content.FirstOrDefault().AttributeValues.Where( a => a.Key == attributeKey ).Select( a => a.Value.Value ).FirstOrDefault();
            }

            return attributeValue;
        }

        private Template GetTemplate()
        {
            var template = GetCacheItem( TEMPLATE_CACHE_KEY + ChannelGuid ) as Template;
            if ( template == null )
            {
                template = Template.Parse( GetAttributeValue( "ContentLava" ) );

                int? cacheDuration = GetAttributeValue( "CacheDuration" ).AsInteger();
                if ( cacheDuration > 0 )
                {
                    var cacheItemPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds( cacheDuration.Value ) };
                    AddCacheItem( TEMPLATE_CACHE_KEY + ChannelGuid, template, cacheItemPolicy );
                }
            }

            return template;
        }

        private List<ContentChannelItem> GetContent( List<string> errorMessages, bool FilterByQRS = true )
        {
            string contentItem = PageParameter( "Item" ) ?? "";
            if ( !FilterByQRS )
            {
                contentItem = "";
            }
            var items = GetCacheItem( CONTENT_CACHE_KEY + ChannelGuid + contentItem ) as List<ContentChannelItem>;
            bool queryParameterFiltering = GetAttributeValue( "QueryParameterFiltering" ).AsBoolean( false );

            if ( items == null || ( queryParameterFiltering && Request.QueryString.Count > 0 ) )
            {
                Guid? channelGuid = ChannelGuid;
                if ( channelGuid.HasValue )
                {
                    var rockContext = new RockContext();
                    var service = new ContentChannelItemService( rockContext );
                    var itemType = typeof( Rock.Model.ContentChannelItem );

                    ParameterExpression paramExpression = service.ParameterExpression;

                    var contentChannel = new ContentChannelService( rockContext ).Get( channelGuid.Value );
                    if ( contentChannel != null )
                    {
                        var entityFields = HackEntityFields( contentChannel, rockContext );

                        if ( items == null && FilterByQRS )
                        {
                            items = new List<ContentChannelItem>();

                            var qry = service.Queryable( "ContentChannel,ContentChannelType" );

                            int? itemId = PageParameter( "Item" ).AsIntegerOrNull();
                            if ( queryParameterFiltering && itemId.HasValue )
                            {
                                qry = qry.Where( i => i.Id == itemId.Value );
                            }
                            else
                            {
                                qry = qry.Where( i => i.ContentChannelId == contentChannel.Id );

                                if ( contentChannel.RequiresApproval )
                                {
                                    // Check for the configured status and limit query to those
                                    var statuses = new List<ContentChannelItemStatus>();

                                    foreach ( string statusVal in ( GetAttributeValue( "Status" ) ?? "2" ).Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ) )
                                    {
                                        var status = statusVal.ConvertToEnumOrNull<ContentChannelItemStatus>();
                                        if ( status != null )
                                        {
                                            statuses.Add( status.Value );
                                        }
                                    }
                                    if ( statuses.Any() )
                                    {
                                        qry = qry.Where( i => statuses.Contains( i.Status ) );
                                    }
                                }
                            }
                            // Now run query and load attributes and filter by itemtype defined type
                            string dataItemType = GetAttributeValue( "ItemType" );

                            foreach ( var item in qry.ToList() )
                            {
                                item.LoadAttributes( rockContext );
                                if ( dataItemType != null )
                                {
                                    //Filter by type such as Lesson or resource
                                    var attValue = item.GetAttributeValue( "ItemType" );
                                    if ( attValue == dataItemType )
                                    {
                                        if ( GetAttributeValue( "FilterByDate" ).AsBoolean() )
                                        {
                                            //Filter out of date content items
                                            var SelectedDate = ( dpCalendar.SelectedDate ?? Rock.RockDateTime.Now ).Date;
                                            if ( SelectedDate >= item.StartDateTime.Date && SelectedDate <= item.ExpireDateTime )
                                            {
                                                items.Add( item );
                                            }

                                        }
                                        else
                                        {
                                            items.Add( item );
                                        }
                                    }
                                }
                                else
                                {
                                    items.Add( item );
                                }
                            }

                            // Order the items
                            SortProperty sortProperty = null;

                            string orderBy = GetAttributeValue( "Order" );
                            if ( !string.IsNullOrWhiteSpace( orderBy ) )
                            {
                                var fieldDirection = new List<string>();
                                foreach ( var itemPair in orderBy.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).Select( a => a.Split( '^' ) ) )
                                {
                                    if ( itemPair.Length == 2 && !string.IsNullOrWhiteSpace( itemPair[0] ) )
                                    {
                                        var sortDirection = SortDirection.Ascending;
                                        if ( !string.IsNullOrWhiteSpace( itemPair[1] ) )
                                        {
                                            sortDirection = itemPair[1].ConvertToEnum<SortDirection>( SortDirection.Ascending );
                                        }
                                        fieldDirection.Add( itemPair[0] + ( sortDirection == SortDirection.Descending ? " desc" : "" ) );
                                    }
                                }

                                sortProperty = new SortProperty();
                                sortProperty.Direction = SortDirection.Ascending;
                                sortProperty.Property = fieldDirection.AsDelimited( "," );

                                string[] columns = sortProperty.Property.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries );

                                var itemQry = items.AsQueryable();
                                IOrderedQueryable<ContentChannelItem> orderedQry = null;

                                for ( int columnIndex = 0; columnIndex < columns.Length; columnIndex++ )
                                {
                                    string column = columns[columnIndex].Trim();

                                    var direction = sortProperty.Direction;
                                    if ( column.ToLower().EndsWith( " desc" ) )
                                    {
                                        column = column.Left( column.Length - 5 );
                                        direction = sortProperty.Direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
                                    }

                                    try
                                    {
                                        if ( column.StartsWith( "Attribute:" ) )
                                        {
                                            string attributeKey = column.Substring( 10 );

                                            if ( direction == SortDirection.Ascending )
                                            {
                                                orderedQry = ( columnIndex == 0 ) ?
                                                    itemQry.OrderBy( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.ValueAsType ) :
                                                    orderedQry.ThenBy( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.ValueAsType );
                                            }
                                            else
                                            {
                                                orderedQry = ( columnIndex == 0 ) ?
                                                    itemQry.OrderByDescending( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.ValueAsType ) :
                                                    orderedQry.ThenByDescending( i => i.AttributeValues.Where( v => v.Key == attributeKey ).FirstOrDefault().Value.ValueAsType );
                                            }
                                        }
                                        else
                                        {
                                            if ( direction == SortDirection.Ascending )
                                            {
                                                orderedQry = ( columnIndex == 0 ) ? itemQry.OrderBy( column ) : orderedQry.ThenBy( column );
                                            }
                                            else
                                            {
                                                orderedQry = ( columnIndex == 0 ) ? itemQry.OrderByDescending( column ) : orderedQry.ThenByDescending( column );
                                            }
                                        }
                                    }
                                    catch { }

                                }

                                try
                                {
                                    if ( orderedQry != null )
                                    {
                                        items = orderedQry.ToList();
                                    }
                                }
                                catch { }

                            }

                            int? cacheDuration = GetAttributeValue( "CacheDuration" ).AsInteger();
                            if ( cacheDuration > 0 )
                            {
                                var cacheItemPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddSeconds( cacheDuration.Value ) };
                                string contentItemCache = PageParameter( "Item" ) ?? "";
                                AddCacheItem( CONTENT_CACHE_KEY + ChannelGuid + contentItemCache, items, cacheItemPolicy );
                            }
                        }


                        // If items could be filtered by querystring values, check for filters
                        if ( queryParameterFiltering )
                        {
                            var pageParameters = PageParameters();
                            if ( pageParameters.Count > 0 )
                            {
                                var propertyFilter = new Rock.Reporting.DataFilter.PropertyFilter();

                                var itemQry = items.AsQueryable();
                                foreach ( string key in PageParameters().Select( p => p.Key ).ToList() )
                                {
                                    var selection = new List<string>();
                                    selection.Add( key );

                                    var entityField = entityFields.FirstOrDefault( f => f.Name.Equals( key, StringComparison.OrdinalIgnoreCase ) );
                                    if ( entityField != null )
                                    {
                                        string value = PageParameter( key );
                                        switch ( entityField.FieldType.Guid.ToString().ToUpper() )
                                        {
                                            case Rock.SystemGuid.FieldType.DAY_OF_WEEK:
                                            case Rock.SystemGuid.FieldType.SINGLE_SELECT:
                                                {
                                                    selection.Add( value );
                                                }
                                                break;
                                            case Rock.SystemGuid.FieldType.MULTI_SELECT:
                                                {
                                                    var values = new List<string>();
                                                    values.Add( value );
                                                    selection.Add( Newtonsoft.Json.JsonConvert.SerializeObject( values ) );
                                                }
                                                break;
                                            default:
                                                {
                                                    selection.Add( ComparisonType.EqualTo.ConvertToInt().ToString() );
                                                    selection.Add( value );
                                                }
                                                break;
                                        }

                                        itemQry = itemQry.Where( paramExpression, propertyFilter.GetExpression( itemType, service, paramExpression, Newtonsoft.Json.JsonConvert.SerializeObject( selection ) ) );
                                    }
                                }

                                items = itemQry.ToList();

                            }
                        }
                    }
                }
            }

            return items;

        }

        /// <summary>
        /// Shows the edit.
        /// </summary>
        public void ShowEdit()
        {
            if ( ChannelGuid.HasValue )
            {
                var rockContext = new RockContext();
                var channel = new ContentChannelService( rockContext ).Queryable( "ContentChannelType" )
                    .FirstOrDefault( c => c.Guid.Equals( ChannelGuid.Value ) );
                if ( channel != null )
                {

                    cblStatus.Visible = channel.RequiresApproval;

                    kvlOrder.CustomKeys = new Dictionary<string, string>();
                    kvlOrder.CustomKeys.Add( "", "" );
                    kvlOrder.CustomKeys.Add( "Title", "Title" );
                    kvlOrder.CustomKeys.Add( "Priority", "Priority" );
                    kvlOrder.CustomKeys.Add( "Status", "Status" );
                    kvlOrder.CustomKeys.Add( "StartDateTime", "Start" );
                    kvlOrder.CustomKeys.Add( "ExpireDateTime", "Expire" );
                }
            }
        }

        /// <summary>
        /// Sets the list value.
        /// </summary>
        /// <param name="listControl">The list control.</param>
        /// <param name="value">The value.</param>
        private void SetListValue( ListControl listControl, string value )
        {
            foreach ( ListItem item in listControl.Items )
            {
                item.Selected = ( item.Value == value );
            }
        }

        /// <summary>
        /// The PropertyFilter checks for it's property/attribute list in a cached items object before recreating 
        /// them using reflection and loading of generic attributes. Because of this, we're going to load them here
        /// and exclude some properties and add additional attributes specific to the channel type, and then save
        /// list to same cached object so that property filter lists our collection of properties/attributes
        /// instead.
        /// </summary>
        private List<Rock.Reporting.EntityField> HackEntityFields( ContentChannel channel, RockContext rockContext )
        {
            if ( channel != null )
            {
                var entityTypeCache = EntityTypeCache.Read( ITEM_TYPE_NAME );
                if ( entityTypeCache != null )
                {
                    var entityType = entityTypeCache.GetEntityType();

                    HttpContext.Current.Items.Remove( string.Format( "EntityHelper:GetEntityFields:{0}", entityType.FullName ) );
                    var entityFields = Rock.Reporting.EntityHelper.GetEntityFields( entityType );
                    foreach ( var entityField in entityFields
                        .Where( f =>
                            f.FieldKind == Rock.Reporting.FieldKind.Attribute &&
                            f.AttributeGuid.HasValue )
                        .ToList() )
                    {
                        var attribute = AttributeCache.Read( entityField.AttributeGuid.Value );
                        if ( attribute != null &&
                            attribute.EntityTypeQualifierColumn == "ContentChannelTypeId" &&
                            attribute.EntityTypeQualifierValue.AsInteger() != channel.ContentChannelTypeId )
                        {
                            entityFields.Remove( entityField );
                        }
                    }

                    if ( entityFields != null )
                    {
                        // Remove the status field
                        var ignoreFields = new List<string>();
                        ignoreFields.Add( "ContentChannelId" );
                        ignoreFields.Add( "Status" );

                        entityFields = entityFields.Where( f => !ignoreFields.Contains( f.Name ) ).ToList();

                        // Add any additional attributes that are specific to channel/type
                        var item = new ContentChannelItem();
                        item.ContentChannel = channel;
                        item.ContentChannelId = channel.Id;
                        item.ContentChannelType = channel.ContentChannelType;
                        item.ContentChannelTypeId = channel.ContentChannelTypeId;
                        item.LoadAttributes( rockContext );
                        foreach ( var attribute in item.Attributes
                            .Where( a =>
                                a.Value.EntityTypeQualifierColumn != "" &&
                                a.Value.EntityTypeQualifierValue != "" )
                            .Select( a => a.Value ) )
                        {
                            if ( !entityFields.Any( f => f.AttributeGuid.Equals( attribute.Guid ) ) )
                            {
                                Rock.Reporting.EntityHelper.AddEntityFieldForAttribute( entityFields, attribute );
                            }
                        }

                        // Re-sort fields
                        int index = 0;
                        var sortedFields = new List<Rock.Reporting.EntityField>();
                        foreach ( var entityProperty in entityFields.OrderBy( p => p.Title ).ThenBy( p => p.Name ) )
                        {
                            entityProperty.Index = index;
                            index++;
                            sortedFields.Add( entityProperty );
                        }

                        // Save new fields to cache ( which report field will use instead of reading them again )
                        HttpContext.Current.Items[string.Format( "EntityHelper:GetEntityFields:{0}", entityType.FullName )] = sortedFields;
                    }

                    return entityFields;
                }
            }

            return null;
        }

        public class Pagination : DotLiquid.Drop
        {

            /// <summary>
            /// Gets or sets the item count.
            /// </summary>
            /// <value>
            /// The item count.
            /// </value>
            public int ItemCount { get; set; }

            /// <summary>
            /// Gets or sets the size of the page.
            /// </summary>
            /// <value>
            /// The size of the page.
            /// </value>
            public int PageSize { get; set; }

            /// <summary>
            /// Gets or sets the current page.
            /// </summary>
            /// <value>
            /// The current page.
            /// </value>
            public int CurrentPage { get; set; }

            /// <summary>
            /// Gets the previous page.
            /// </summary>
            /// <value>
            /// The previous page.
            /// </value>
            public int PreviousPage
            {
                get
                {
                    CurrentPage = CurrentPage > TotalPages ? TotalPages : CurrentPage;
                    return ( CurrentPage > 1 ) ? CurrentPage - 1 : -1;
                }
            }

            /// <summary>
            /// Gets the next page.
            /// </summary>
            /// <value>
            /// The next page.
            /// </value>
            public int NextPage
            {
                get
                {
                    CurrentPage = CurrentPage > TotalPages ? TotalPages : CurrentPage;
                    return ( CurrentPage < TotalPages ) ? CurrentPage + 1 : -1;
                }
            }

            /// <summary>
            /// Gets the total pages.
            /// </summary>
            /// <value>
            /// The total pages.
            /// </value>
            public int TotalPages
            {
                get
                {
                    if ( PageSize == 0 )
                    {
                        return 1;
                    }
                    else
                    {
                        return Convert.ToInt32( Math.Abs( ItemCount / PageSize ) ) +
                            ( ( ItemCount % PageSize ) > 0 ? 1 : 0 );
                    }
                }
            }

            public string UrlTemplate { get; set; }

            /// <summary>
            /// Gets or sets the pages.
            /// </summary>
            /// <value>
            /// The pages.
            /// </value>
            public List<PaginationPage> Pages
            {
                get
                {
                    var pages = new List<PaginationPage>();

                    for ( int i = 1; i <= TotalPages; i++ )
                    {
                        pages.Add( new PaginationPage( UrlTemplate, i ) );
                    }

                    return pages;
                }
            }

            /// <summary>
            /// Gets the current page items.
            /// </summary>
            /// <param name="allItems">All items.</param>
            /// <returns></returns>
            public List<ContentChannelItem> GetCurrentPageItems( List<ContentChannelItem> allItems )
            {
                if ( PageSize > 0 )
                {
                    CurrentPage = CurrentPage > TotalPages ? TotalPages : CurrentPage;
                    return allItems.Skip( ( CurrentPage - 1 ) * PageSize ).Take( PageSize ).ToList();
                }

                return allItems;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        public class PaginationPage : DotLiquid.Drop
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PaginationPage"/> class.
            /// </summary>
            /// <param name="urlTemplate">The URL template.</param>
            /// <param name="pageNumber">The page number.</param>
            public PaginationPage( string urlTemplate, int pageNumber )
            {
                UrlTemplate = urlTemplate;
                PageNumber = pageNumber;
            }

            private string UrlTemplate { get; set; }

            /// <summary>
            /// Gets the page number.
            /// </summary>
            /// <value>
            /// The page number.
            /// </value>
            public int PageNumber { get; private set; }

            /// <summary>
            /// Gets the page URL.
            /// </summary>
            /// <value>
            /// The page URL.
            /// </value>
            public string PageUrl
            {
                get
                {
                    if ( !string.IsNullOrWhiteSpace( UrlTemplate ) && UrlTemplate.Contains( "{0}" ) )
                    {
                        return string.Format( UrlTemplate, PageNumber );
                    }
                    else
                    {
                        return PageNumber.ToString();
                    }
                }
            }

            #endregion

        }

        protected void btnCalendar_Click( object sender, EventArgs e )
        {
            ShowView();
        }
    }

}