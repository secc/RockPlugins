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
using System.ComponentModel;
using System.Web.UI;
using Rock;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Web.UI;

namespace org.secc.Cms
{
    public class SectionZone : Zone
    {
        /// <summary>
        /// Gets or sets the help tip.
        /// </summary>
        /// <value>
        /// The help tip.
        /// </value>
        [
        Bindable( true ),
        Category( "Misc" ),
        DefaultValue( "" ),
        Description( "The friendly name (will default to the ID)." )
        ]
        public new string Name
        {
            get
            {
                string s = ViewState["Name"] as string;
                return s == null ? this.ID.SplitCase() : s;
            }
            set
            {
                ViewState["Name"] = value;
            }
        }

        /// <summary>
        /// Renders a label and <see cref="T:System.Web.UI.WebControls.TextBox"/> control to the specified <see cref="T:System.Web.UI.HtmlTextWriter"/> object.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Web.UI.HtmlTextWriter"/> that receives the rendered output.</param>
        public override void RenderControl( HtmlTextWriter writer )
        {

            if ( this.Visible )
            {
                string cssClass = "";
                foreach ( Control childControl in this.Controls )
                {
                    if ( childControl.GetType().ToString().Contains( "RockBlockWrapper" ) )
                    {
                        foreach ( Control grandChildControl in childControl.Controls )
                        {
                            if ( grandChildControl.GetType().ToString().Contains( "org_secc_cms_sectionheader" ) )
                            {
                                var block = BlockCache.Read( ( ( RockBlock ) grandChildControl ).BlockId );
                                var definedValue = DefinedValueCache.Read( block.GetAttributeValue( "SectionType" ) );
                                if ( definedValue != null && definedValue.AttributeValues.ContainsKey( "ClassName" ) )
                                {
                                    cssClass = definedValue.AttributeValues["ClassName"].ToString();
                                }
                                break;
                            }

                        }
                        if ( cssClass != "" )
                        {
                            break;
                        }
                    }
                }
                writer.AddAttribute( HtmlTextWriterAttribute.Class, cssClass );
                writer.RenderBeginTag( "section" );

                base.RenderControl( writer );
                writer.RenderEndTag();

            }
        }
    }
}
