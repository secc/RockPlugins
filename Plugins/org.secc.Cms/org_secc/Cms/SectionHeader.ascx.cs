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
using System.ComponentModel;
using Rock.Model;
using Rock.Security;
using System.Web.UI;
using Rock;
using Rock.Web.Cache;
using Rock.Web.UI;
using System.Web;
using System.Web.UI.WebControls;
using Rock.Attribute;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.CMS
{
    [DisplayName( "Section Header" )]
    [Category( "SECC > CMS" )]
    [Description( "Builds the login/user menu." )]
    [TextField( "Header Text", "The text of the header to show." )]
    [DefinedValueField( "886354FF-253E-4651-A8C3-23ADBA8CC177", "Section Type", "Type of section to style as." )]
    [BooleanField( "Show Header", "Would you like to see the section header?" )]
    public partial class SectionHeader : RockBlock
    {

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                var headerText = GetAttributeValue( "HeaderText" );
                lHeaderText.Text = headerText;
            }
            pnlOptionalHeader.Visible = GetAttributeValue( "ShowHeader" ).AsBoolean();

        }

        #endregion

    }
}