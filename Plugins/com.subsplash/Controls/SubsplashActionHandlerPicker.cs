// <copyright>
// MIT License
//
// Copyright( c) 2020 Subsplash

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software. 

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Web.UI.Controls;

namespace com.subsplash.Controls
{
    public class ActionHandlerPicker : CompositeControl, IRockControl
    {
        private Panel panel;
        private RockDropDownList handlerType;
        private Panel audioPanel;
        private UrlLinkBox audioUrl;
        private NumberBox audioSelectedIndex;
        private Panel browserPanel;
        private UrlLinkBox url;
        private RockDropDownList style;
        private RockCheckBox showBrowserControls;
        private Panel emailPanel;
        private EmailBox email;
        private RockTextBox subject;
        private RockTextBox body;
        private Panel listPanel;
        private UrlLinkBox listUrl;
        private RockDropDownList listStyle;
        private NumberBox selectedIndex;
        private Panel phonePanel;
        private PhoneNumberBox phone;
        private Panel shareHtmlPanel;
        private UrlLinkBox shareUrl;
        private CodeEditor shareHtml;
        private Panel shareTextPanel;
        private RockTextBox shareBody;
        private Panel customJSONPanel;
        private CodeEditor json;

        private com.subsplash.Model.Action _Action;

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
            set { ViewState["ValidationGroup"] = value; this.RequiredFieldValidator.ValidationGroup = value; }
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

        public ActionHandlerPicker()
            : base()
        {
            HelpBlock = new HelpBlock();
            WarningBlock = new WarningBlock();
            RequiredFieldValidator = new RequiredFieldValidator();
            RequiredFieldValidator.ValidationGroup = this.ValidationGroup;
        }

        public com.subsplash.Model.Action Action
        {
            get
            {
                EnsureChildControls();
                if (_Action == null)
                {
                    _Action = new Model.Action();
                }

                _Action.HandlerType = handlerType.SelectedValue;
                switch ( handlerType.SelectedValue )
                {
                    case "audio":
                        _Action.Url = audioUrl.Text;
                        _Action.SelectedIndex = audioSelectedIndex.Text.AsInteger();
                        break;
                    case "browser":
                        _Action.ContentUrl = url.Text;
                        _Action.Style = style.SelectedValue;
                        _Action.ShowBrowserControls = showBrowserControls.Checked;
                        break;
                    case "email":
                        _Action.Address = email.Text;
                        _Action.Subject = subject.Text;
                        _Action.Body = body.Text;
                        break;
                    case "list":
                        _Action.Url = listUrl.Text;
                        _Action.Style = listStyle.SelectedValue;
                        _Action.SelectedIndex = selectedIndex.Text.AsInteger();
                        break;
                    case "phone":
                        _Action.Number = phone.Text;
                        break;
                    case "htmlShare":
                        _Action.Url = shareUrl.Text;
                        _Action.Body = shareHtml.Text;
                        break;
                    case "defaultShare":
                        _Action.Body = shareBody.Text;
                        break;
                    case "json":
                        _Action.Json = json.Text;
                        break;
                    default:
                        break;
                }
                return _Action;
            }
            set
            {
                _Action = value;
                EnsureChildControls();
                if ( string.IsNullOrWhiteSpace( Action.Json ) )
                {
                    handlerType.SelectedValue = _Action?.HandlerType;
                } else
                {
                    handlerType.SelectedValue = "json";
                }
                switch ( handlerType.SelectedValue )
                {
                    case "audio":
                        audioPanel.Visible = true;
                        audioUrl.Text = _Action?.Url;
                        audioSelectedIndex.Text = _Action?.SelectedIndex.ToString();
                        break;
                    case "browser":
                        browserPanel.Visible = true;
                        url.Text = _Action?.ContentUrl;
                        style.SelectedValue = _Action?.Style;
                        showBrowserControls.Checked = _Action?.ShowBrowserControls == true;
                        break;
                    case "email":
                        emailPanel.Visible = true;
                        email.Text = _Action?.Address;
                        subject.Text = _Action?.Subject;
                        body.Text = _Action?.Body;
                        break;
                    case "list":
                        listPanel.Visible = true;
                        listUrl.Text = _Action?.Url;
                        listStyle.SelectedValue = _Action?.Style;
                        selectedIndex.Text = _Action?.SelectedIndex.ToString();
                        break;
                    case "phone":
                        phonePanel.Visible = true;
                        phone.Text = _Action?.Number;
                        break;
                    case "htmlShare":
                        shareHtmlPanel.Visible = true;
                        shareUrl.Text = _Action?.Url;
                        shareHtml.Text = _Action?.Body;
                        break;
                    case "defaultShare":
                        shareTextPanel.Visible = true;
                        shareBody.Text = _Action?.Body;
                        break;
                    case "json":
                        customJSONPanel.Visible = true;
                        break;
                    default:
                        break;
                }

                json.Text = _Action.Json??JsonConvert.SerializeObject( _Action,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }
                );
            }
        }

        public bool DisplayRequiredIndicator { get => false; set { } }

        protected override void OnLoad( EventArgs e )
        {
            EnsureChildControls();
            base.OnLoad( e );
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Controls.Clear();

            RockControlHelper.CreateChildControls( this, Controls );

            panel = new Panel();

            /*Panel typePanel = new Panel { CssClass = "col-md-4" };
            Panel numberPanel = new Panel { CssClass = "col-md-8" };*/


            handlerType = new RockDropDownList();
            List<ListItem> items = new List<ListItem>();
            items.Add( new ListItem( "", "" ) );
            items.Add( new ListItem( "Audio", "audio" ) );
            items.Add( new ListItem( "Browser", "browser" ) );
            items.Add( new ListItem( "Email", "email" ) );
            items.Add( new ListItem( "List", "list" ) );
            items.Add( new ListItem( "Phone", "phone" ) );
            items.Add( new ListItem( "Share (Text)", "defaultShare" ) );
            items.Add( new ListItem( "Share (HTML)", "htmlShare" ) );
            items.Add( new ListItem( "Custom JSON", "json" ) );

            handlerType.ID = "handlerType_" + this.ID;
            handlerType.DataTextField = "Text";
            handlerType.DataValueField = "Value";
            handlerType.DataSource = items;
            handlerType.DataBind();
            handlerType.Label = "Handler Type:";
            handlerType.Required = true;
            handlerType.AutoPostBack = true;
            handlerType.SelectedValue = _Action?.HandlerType;

            panel.Controls.Add( handlerType );

            this.RequiredFieldValidator.ControlToValidate = handlerType.ID;
            this.Controls.Add( panel );


            // Handle the audio controls
            audioPanel = new Panel();
            audioPanel.Visible = handlerType.SelectedValue == "Audio";
            audioPanel.ID = "audioPanel_"+ this.ID;
            panel.Controls.Add( audioPanel );

            audioUrl = new UrlLinkBox();
            audioUrl.ID = "audio_url_" + this.ID;
            audioUrl.Label = "URL";
            audioUrl.Help = "Provides the source of content for this Action object.";
            audioUrl.Required = true;
            audioUrl.Text = _Action?.Url;
            audioPanel.Controls.Add( audioUrl );

            audioSelectedIndex = new NumberBox();
            audioSelectedIndex.ID = "audio_selectedindex_" + this.ID;
            audioSelectedIndex.Label = "Selected Index";
            audioSelectedIndex.Help = "This allows you to target a specific item in the playlist.";
            audioSelectedIndex.Text = _Action?.SelectedIndex.ToString();
            audioPanel.Controls.Add( audioSelectedIndex );

            // Handle the browser controls
            browserPanel = new Panel();
            browserPanel.Visible = handlerType.SelectedValue == "Browser";
            browserPanel.ID = "browserPanel_" + this.ID;
            panel.Controls.Add( browserPanel );

            url = new UrlLinkBox();
            url.ID = "browser_url_" + this.ID;
            url.Label = "URL";
            url.Required = true;
            url.Text = _Action?.ContentUrl;
            browserPanel.Controls.Add( url );

            style = new RockDropDownList();
            List<ListItem> styleItems = new List<ListItem>();
            styleItems.Add( new ListItem( "Internal", "internal" ) );
            styleItems.Add( new ListItem( "External", "external" ) );
            style.DataTextField = "Text";
            style.DataValueField = "Value";
            style.DataSource = styleItems;
            style.DataBind();
            style.ID = "browser_style_" + this.ID;
            style.Label = "Style";
            style.Required = true;
            style.SelectedValue = _Action?.Style;
            browserPanel.Controls.Add( style );

            showBrowserControls = new RockCheckBox();
            showBrowserControls.ID = "browser_controls_" + this.ID;
            showBrowserControls.Label = "Show Browser Controls";
            showBrowserControls.Help = "When checked, the app will display a control bar at the bottom of the screen containing Back, Forward, and Refresh buttons for navigating between web pages. This value is only valid when style = internal (because and external browser is outside of the app and will display its own preferred UI).";
            showBrowserControls.Checked = _Action?.ShowBrowserControls == true;
            browserPanel.Controls.Add( showBrowserControls );


            // Handle the email controls
            emailPanel = new Panel();
            emailPanel.Visible = handlerType.SelectedValue == "Email";
            emailPanel.ID = "emailPanel_" + this.ID;
            panel.Controls.Add( emailPanel );

            email = new EmailBox();
            email.Label = "Email Address";
            email.ID = "email_" + this.ID;
            email.Text = _Action?.Address;
            email.Required = true;
            emailPanel.Controls.Add( email );
            
            subject = new RockTextBox();
            subject.Label = "Subject";
            subject.ID = "subject_" + this.ID;
            subject.Text = _Action?.Subject;
            emailPanel.Controls.Add( subject );
            
            body = new RockTextBox();
            body.Label = "Body";
            body.Rows = 8;
            body.TextMode = TextBoxMode.MultiLine;
            body.ID = "body_" + this.ID;
            body.Text = _Action?.Body;
            emailPanel.Controls.Add( body );



            // Handle the list controls
            listPanel = new Panel();
            listPanel.Visible = handlerType.SelectedValue == "List";
            listPanel.ID = "listPanel_" + this.ID;
            panel.Controls.Add( listPanel );
            
            listUrl = new UrlLinkBox();
            listUrl.ID = "list_url_" + this.ID;
            listUrl.Label = "URL";
            listUrl.Required = true;
            listUrl.Text = _Action?.Url;
            listPanel.Controls.Add( listUrl );

            List<ListItem> listStyleItems = new List<ListItem>();
            listStyleItems.Add( new ListItem( "Plain", "plain" ) );
            listStyleItems.Add( new ListItem( "Grid", "grid" ) );
            listStyleItems.Add( new ListItem( "Carousel", "carousel" ) );
            listStyleItems.Add( new ListItem( "Reader", "reader" ) );
            
            listStyle = new RockDropDownList();
            listStyle.DataTextField = "Text";
            listStyle.DataValueField = "Value";
            listStyle.DataSource = listStyleItems;
            listStyle.DataBind();
            listStyle.ID = "list_style_" + this.ID;
            listStyle.Label = "Style";
            listStyle.Help = "Specifies the desired display style for the list at the provided url. Can be carousel, grid, plain, reader";
            listStyle.Required = true;
            listStyle.SelectedValue = _Action?.Style;
            listPanel.Controls.Add( listStyle );

            selectedIndex = new NumberBox();
            selectedIndex.ID = "list_selectedindex_" + this.ID;
            selectedIndex.Label = "Selected Index";
            selectedIndex.Help = "This value allows the user to navigate directly to a particular item in the list specified by the url (Example: linking directly to a particular blog post when style = reader). This value may not be supported by all List styles.";
            selectedIndex.Text = _Action?.SelectedIndex.ToString();
            listPanel.Controls.Add( selectedIndex );



            // Handle the phone controls
            phonePanel = new Panel();
            phonePanel.Visible = handlerType.SelectedValue == "Phone";
            phonePanel.ID = "phonePanel_" + this.ID;
            panel.Controls.Add( phonePanel );
            
            phone = new PhoneNumberBox();
            phone.Label = "Phone Number";
            phone.ID = "phone_" + this.ID;
            phone.Required = true;
            phone.Text = _Action?.Number;
            phonePanel.Controls.Add( phone );



            // Handle the Share (HTML) controls
            shareHtmlPanel = new Panel();
            shareHtmlPanel.Visible = handlerType.SelectedValue == "Share (HTML)";
            shareHtmlPanel.ID = "shareHtmlPanel_" + this.ID;
            panel.Controls.Add( shareHtmlPanel );
            
            shareUrl = new UrlLinkBox();
            shareUrl.ID = "share_url_" + this.ID;
            shareUrl.Label = "URL";
            shareUrl.Required = true;
            shareUrl.Text = _Action?.Url;
            shareHtmlPanel.Controls.Add( shareUrl );

            shareHtml = new CodeEditor();
            shareHtml.EditorMode = CodeEditorMode.Html;
            shareHtml.ID = "share_html_" + this.ID;
            shareHtml.Label = "Text/HTML";
            shareHtml.Required = true;
            shareHtml.Text = _Action?.Body;
            shareHtmlPanel.Controls.Add( shareHtml );



            // Handle the Share (Text) controls
            shareTextPanel = new Panel();
            shareTextPanel.Visible = handlerType.SelectedValue == "Share (Text)";
            shareTextPanel.ID = "shareTextPanel_" + this.ID;
            panel.Controls.Add( shareTextPanel );
            
            shareBody = new RockTextBox();
            shareBody.Label = "Body";
            shareBody.Rows = 8;
            shareBody.TextMode = TextBoxMode.MultiLine;
            shareBody.ID = "share_body_" + this.ID;
            shareBody.Text = _Action?.Body;
            shareTextPanel.Controls.Add( shareBody );



            // Handle the Custom JSON controls
            customJSONPanel = new Panel();
            customJSONPanel.Visible = handlerType.SelectedValue == "Custom JSON";
            customJSONPanel.ID = "customJSONPanel_" + this.ID;
            panel.Controls.Add( customJSONPanel );
            
            json = new CodeEditor();
            json.EditorMode = CodeEditorMode.JavaScript;
            json.ID = "custom_json_" + this.ID;
            json.Label = "JSON";
            json.Required = true;
            json.Text = JsonConvert.SerializeObject( _Action );
            customJSONPanel.Controls.Add( json );

            handlerType.SelectedIndexChanged +=  delegate
            {
                audioPanel.Visible = false;
                browserPanel.Visible = false;
                emailPanel.Visible = false;
                listPanel.Visible = false;
                phonePanel.Visible = false;
                shareHtmlPanel.Visible = false;
                shareTextPanel.Visible = false;
                customJSONPanel.Visible = false;
                switch ( handlerType.SelectedValue )
                {
                    case "audio":
                        audioPanel.Visible = true;
                        break;
                    case "browser":
                        browserPanel.Visible = true;
                        break;
                    case "email":
                        emailPanel.Visible = true;
                        break;
                    case "list":
                        listPanel.Visible = true;
                        break;
                    case "phone":
                        phonePanel.Visible = true;
                        break;
                    case "htmlShare":
                        shareHtmlPanel.Visible = true;
                        break;
                    case "defaultShare":
                        shareTextPanel.Visible = true;
                        break;
                    case "json":
                        customJSONPanel.Visible = true;
                        break;
                    default:
                        break;
                }

            };
        }

        #endregion
        public override void RenderControl( HtmlTextWriter writer )
        {
            if ( this.Visible )
            {
                RockControlHelper.RenderControl( this, writer );
            }
        }

        public void RenderBaseControl( HtmlTextWriter writer )
        {
            panel.RenderControl( writer );
        }
    }
}
