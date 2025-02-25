﻿// <copyright>
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
using Rock.Attribute;

namespace org.secc.Imaging.Components
{
    [Export( typeof( SettingsComponent ) )]
    [ExportMetadata( "ComponentName", "Microsoft Face" )]
    [Description( "Settings for Microsoft Face." )]

    [TextField( "Endpoint" )]
    [TextField( "Subscription Key", key: "SubscriptionKey" )]


    public class MicrosoftFaceSettings : SettingsComponent
    {
        public override string Name => "Microsoft Face";
    }
}
