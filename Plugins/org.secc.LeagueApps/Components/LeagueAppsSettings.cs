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
using org.secc.LeagueApps.Utilities;
using Rock.Attribute;

namespace org.secc.LeagueApps.Components
{
    [Export( typeof( SettingsComponent ) )]
    [ExportMetadata( "ComponentName", "League Apps Settings" )]
    [Description( "Settings for integrating with League Apps." )]

    [EncryptedTextField( "LeagueApps Site Id",
        Description = "Id for LeagueApps site",
        IsRequired = true,
        Category = "LeagueApps",
        Order = 0,
        Key = Constants.LeagueAppsSiteId )]

    [EncryptedTextField( "LeagueApps Client Id",
        Description = "Client Id for authenticating to the LeagueApps API",
        IsRequired = true,
        Category = "LeagueApps API",
        Order = 1,
        Key = Constants.LeagueAppsClientId )]

    [FileField(
        Rock.SystemGuid.BinaryFiletype.DEFAULT,
        Name = "LeagueApps Service Account File",
        Description = "PKCS12 file containing private key for authenticating to the LeagueApps API",
        IsRequired = true,
        Category = "LeagueApps API",
        Order = 2,
        Key = Constants.LeagueAppsServiceAccountFile )]

    [GroupField( "Parent Group",
        Description = "Select the grand parent level group to use.  The structure of the sports leagues will be created under this group with the year of the league as a child, the category of the league as a grandchild, and the league itself as the great-grandchild.",
        IsRequired = true,
        Category = "Group Structure",
        Order = 3,
        Key = Constants.ParentGroup )]

    [GroupTypeField( "Year Group Type",
        Description = "Select the group type to use for the yearly group level.",
        IsRequired = true,
        Category = "Group Structure",
        Order = 4,
        Key = Constants.YearGroupType )]

    [GroupTypeField( "Category Group Type",
        Description = "Select the group type to use for the category level.",
        IsRequired = true,
        Category = "Group Structure",
        Order = 5,
        Key = Constants.CategoryGroupType )]

    [GroupTypeField( "League Group Type",
        Description = "Select the group type to use for the league level.",
        IsRequired = true,
        Category = "Group Structure",
        Order = 6,
        Key = Constants.LeagueGroupType )]

    [AttributeField( Rock.SystemGuid.EntityType.GROUP_MEMBER,
        "League Group Team",
        Description = "Select the group member attribute to be populated with the league group team",
        IsRequired = true,
        AllowMultiple = false,
        Category = "Group Structure",
        Order = 7,
        Key = Constants.LeagueGroupTeam )]

    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS,
        "Default Connection Status",
        Description = "The connection status to use for newly created people.",
        DefaultValue = Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_PROSPECT,
        IsRequired = true,
        Category = "People",
        Order = 8,
        Key = Constants.DefaultConnectionStatus )]



    public class LeagueAppsSettings : SettingsComponent
    {
        public override string Name => "League Apps";
    }
}
