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
using org.secc.Rise.Utilities;
using org.secc.xAPI.Component;
using Rock.Attribute;

namespace org.secc.Rise.Components
{
    /// <summary>Rise implementation of the xAPI Component</summary>
    /// <seealso cref="org.secc.xAPI.Component.xAPIComponent" />
    [Export( typeof( xAPIComponent ) )]
    [ExportMetadata( "ComponentName", "Rise Component" )]
    [Description( "Rise LMS" )]

    [TextField( "API Key",
        Description = "The api key for Rise.",
        Order = 1,
        Key = Constants.COMPONENT_ATTRIBUTE_KEY_APIKEY )]
    [TextField( "Shared Secret",
        Description = "The shared secret for validating webhook events",
        Order = 2,
        Key = Constants.COMPONENT_ATTRIBUTE_KEY_SHAREDSECRET )]

    
    public class RiseComponent : xAPIComponent
    {
        /// <summary>Gets the name.</summary>
        /// <value>The name.</value>
        public override string Name => "Rise";

        /// <summary>Gets the icon.</summary>
        /// <value>The icon.</value>
        public override string Icon => "fa fa-chalkboard";
    }
}
