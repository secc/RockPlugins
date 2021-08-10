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
using org.secc.GroupManager;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "Group Detail Lava" )]
    [Category( "SECC > Groups" )]
    [Description( "Displays information about the group with Lava." )]
    [CodeEditorField( "Lava", "Lava to display", CodeEditorMode.Lava )]

    public partial class GroupDetailLava : GroupManagerBlock
    {

        protected override void OnLoad( EventArgs e )
        {
            if ( CurrentGroup == null )
            {
                NavigateToHomePage();
                return;
            }
            Dictionary<string, object> MergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );
            MergeFields.Add( "Group", CurrentGroup );

            ltLava.Text = GetAttributeValue( "Lava" ).ResolveMergeFields( MergeFields );
        }

    }
}
