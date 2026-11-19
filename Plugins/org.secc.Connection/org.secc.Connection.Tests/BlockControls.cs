// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Rock.Web.UI.Controls;

namespace org.secc.Connection
{
    /// <summary>
    /// Test build only. RockWeb generates these control fields from VolunteerSignupFormConnections.ascx;
    /// this partial declares them so the code-behind compiles in the test project. It also supplies the
    /// page parameters (a block outside a page has none), its settings, and the URL key list.
    /// </summary>
    public partial class VolunteerSignupFormConnections
    {
        protected UpdatePanel upnlOpportunityDetail;
        protected NotificationBox nbErrorMessage;
        protected Literal lResponseMessage;
        protected Literal lDebug;
        protected Panel pnlSignup;
        protected Literal lIcon;
        protected Literal lTitle;
        protected ValidationSummary vsSummary;
        protected RockTextBox tbFirstName;
        protected RockTextBox tbLastName;
        protected HtmlGenericControl divPhone;
        protected PhoneNumberBox pnPhone;
        protected HtmlGenericControl divPhoneType;
        protected RockDropDownList ddlPhoneType;
        protected HtmlGenericControl divBirthdate;
        protected BirthdayPicker bpBirthdate;
        protected EmailBox tbEmail;
        protected CampusPicker cpCampus;
        protected RockLiteral ltCampus;
        protected Repeater rptGroupRoleAttributes;
        protected RockTextBox tbComments;
        protected Literal lWaiverText;
        protected PlaceHolder phFormAttributes;
        protected Literal lRequirementBlockMessage;
        protected LinkButton btnConnectandAddAnother;
        protected LinkButton btnConnect;

        /// <summary>
        /// The page (route and query string) parameters the test gives the block.
        /// </summary>
        internal Dictionary<string, string> TestPageParameters { get; } = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// Stands in for RockBlock.PageParameter, which reads the hosting RockPage.
        /// </summary>
        public new string PageParameter( string name )
        {
            string value;
            return TestPageParameters.TryGetValue( name, out value ) ? value : string.Empty;
        }

        /// <summary>
        /// The block settings the test gives the block (a block outside a page has no BlockCache).
        /// </summary>
        internal Dictionary<string, string> TestBlockSettings { get; } = new Dictionary<string, string>( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// Stands in for RockBlock.GetAttributeValue, which reads the block's settings from BlockCache.
        /// </summary>
        public new string GetAttributeValue( string key )
        {
            string value;
            return TestBlockSettings.TryGetValue( key, out value ) ? value : string.Empty;
        }

        /// <summary>
        /// The "Group Member Attribute Keys - URL" setting is empty in these tests.
        /// </summary>
        internal void UseNoUrlKeys()
        {
            _urlKeys = new List<string>();
        }

        /// <summary>
        /// Creates the controls the tests read, or the block writes, outside the role repeater.
        /// </summary>
        internal void CreateControls()
        {
            nbErrorMessage = new NotificationBox();
            pnlSignup = new Panel();
            lRequirementBlockMessage = new Literal { Visible = false };
            ddlPhoneType = new RockDropDownList();
            phFormAttributes = new PlaceHolder();
        }
    }
}
