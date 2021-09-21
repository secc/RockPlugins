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
using System.ComponentModel.Composition;
using org.secc.DevLib.Components;
using org.secc.Jira.Utilities;
using Rock.Attribute;
using Rock.Security;

namespace org.secc.Jira.Components
{
    [Export( typeof( SettingsComponent ) )]
    [ExportMetadata( "ComponentName", "Jira" )]
    [Description( "Settings for Jira." )]

    [TextField( "User Name",
        Description = "The username to log into Jira",
        Order = 0,
        Key = Constants.UserKey )]

    [EncryptedTextField( "User Token",
        Description = "The generated user token to use as password.",
        Order = 1,
        Key = Constants.UserTokenKey )]

    [TextField( "Base Url",
        Description = "The base url for the Jira instance",
        Order = 2,
        Key = Constants.BaseUrlKey )]

    public class JiraSettingsComponent : SettingsComponent
    {
        public override string Name => "Jira";

        public JiraSettings GetSettings()
        {
            return new JiraSettings
            {
                User = GetAttributeValue( Constants.UserKey ),
                Token = Encryption.DecryptString( GetAttributeValue( Constants.UserTokenKey ) ),
                BaseUrl = GetAttributeValue( Constants.BaseUrlKey ).Trim( '/' ) + Constants.JQLQuery

            };
        }

    }
}
