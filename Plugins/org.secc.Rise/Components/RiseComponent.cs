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
using org.secc.xAPI.Component;
using Rock.Attribute;

namespace org.secc.Rise.Components
{
    [Export( typeof( xAPIComponent ) )]
    [ExportMetadata( "ComponentName", "Rise Component" )]
    [Description( "Rise LMS" )]

    [TextField( "API Key", "The api key for Rise.", order: 0 )]
    public class RiseComponent : xAPIComponent
    {
        public override string Name => "Rise";

        public override string Icon => "fa fa-grin";
    }
}
