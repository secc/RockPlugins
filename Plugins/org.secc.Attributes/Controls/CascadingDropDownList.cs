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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using org.secc.Attributes.Helpers;
using Rock;
using Rock.Web.UI.Controls;

namespace org.secc.Attributes.Controls
{
    public class CascadingDropDownList : CompositeControl, IRockControl
    {
        private Panel panel;
        private List<RockDropDownList> dropDownLists;

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

        public CascadingDropDownList()
            : base()
        {
            HelpBlock = new HelpBlock();
            WarningBlock = new WarningBlock();
            RequiredFieldValidator = new RequiredFieldValidator();
            RequiredFieldValidator.ValidationGroup = this.ValidationGroup;
        }

        public void SetConfiguration( string configuration )
        {
            ViewState["Configuration"] = configuration;
        }

        public void SetConfiguration( List<KeyValueMatrix> configuration )
        {
            ViewState["Configuration"] = JsonConvert.SerializeObject( configuration );
        }

        public string SelectedValue
        {
            get
            {
                if ( ViewState["Value"] != null && ViewState["Value"] is string )
                {
                    return ViewState["Value"] as string;
                }
                return "";
            }
            set
            {
                ViewState["Value"] = value;
                EnsureChildControls();
                UpdateDropDowns( panel );
            }
        }

        public bool DisplayRequiredIndicator { get => ViewState["Required"] as bool? ?? false; set { } }

        protected override void OnLoad( EventArgs e )
        {
            EnsureChildControls();
            base.OnLoad( e );
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            EnsureChildControls();
            UpdateDropDowns( panel );
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Controls.Clear();

            RockControlHelper.CreateChildControls( this, Controls );

            panel = new Panel()
            {
                ID = string.Format( "pnl_{0}", this.ID )
            };
            Controls.Add( panel );

            UpdateDropDowns( panel );

        }

        private void UpdateDropDowns( Panel panel )
        {
            List<string> values = new List<string>();
            dropDownLists = new List<RockDropDownList>();

            if ( ViewState["Value"] != null )
            {
                var value = ViewState["Value"] as string;
                values = value.Split( new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
            }

            panel.Controls.Clear();
            var configurationString = ViewState["Configuration"] as string;

            KeyValueMatrix config = new KeyValueMatrix( configurationString );

            if ( config.Count == 0 )
            {
                var ddl = new DropDownList()
                {
                    ID = string.Format( "ddlNull_{0}", this.ID )
                };
                panel.Controls.Add( ddl );
                this.RequiredFieldValidator.ControlToValidate = ddl.ID;
                return;
            }

            for ( var i = 0; i < config.OrderByDescending( c => c.Count ).First().Count; i++ )
            {
                var ddl = new RockDropDownList
                {
                    ID = string.Format( "ddl_{0}_{1}", this.ID, i ),
                    DataValueField = "Key",
                    DataTextField = "Value",
                    AutoPostBack = true
                };

                if ( i > 0 )
                {
                    ddl.Style.Add( "margin-top", "5px" );
                }

                var source = config
                    .Where( p => p.Count > i )
                    .Select( p => p[i] )
                    .DistinctBy( p => p.Key ).ToList();

                source.Insert( 0, new KeyValuePair<string, string>( "", "" ) );
                ddl.DataSource = source;
                ddl.DataBind();
                ddl.SelectedIndexChanged += Ddl_SelectedIndexChanged;

                ddl.Required = this.Required;


                dropDownLists.Add( ddl );
                panel.Controls.Add( ddl );
                this.RequiredFieldValidator.ControlToValidate = ddl.ID;

                if ( values.Count > i && ddl.Items.FindByValue( values[i] ) != null ) //we have a value for this dropdown
                {
                    ddl.SelectedValue = values[i];
                    //Limit items to only those with this key in its ancestry
                    var rows = config.Where( p => p[i].Key == values[i] );
                    config = new KeyValueMatrix( rows );
                }
                else
                {
                    break; //there is no value for this dropdown, don't continue
                }
            }

        }



        private void Ddl_SelectedIndexChanged( object sender, EventArgs e )
        {
            var values = new List<string>();
            foreach ( var ddl in dropDownLists )
            {
                if ( ddl.SelectedValue.IsNotNullOrWhiteSpace() )
                {
                    values.Add( ddl.SelectedValue );
                }
            }
            var value = string.Join( "|", values );
            ViewState["Value"] = value;
            EnsureChildControls();
            UpdateDropDowns( panel );
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
