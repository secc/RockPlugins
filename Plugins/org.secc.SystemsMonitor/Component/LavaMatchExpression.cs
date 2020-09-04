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
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using org.secc.SystemsMonitor.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace org.secc.SystemsMonitor.Component
{
    [Export( typeof( SystemTestComponent ) )]
    [ExportMetadata( "ComponentName", "Lava Expression Match" )]
    [Description( "Uses Lava to monitor state of the system." )]

    [CodeEditorField( "Lava", mode: CodeEditorMode.Lava, order: 0 )]
    [LavaCommandsField( "Lava Commands", order: 1 )]
    [TextField( "Matching Expression", "Regular expression to match a success.", order: 2 )]
    public class LavaMatchExpression : SystemTestComponent
    {
        public override string Name => "Lava Expression Match";

        public override string Icon => "fa fa-firealt";

        public override List<AlarmCondition> SupportedAlarmConditions
        {
            get => new List<AlarmCondition> {
                AlarmCondition.Never,
                AlarmCondition.Fail,
            };
        }

        public override SystemTestResult RunTest( SystemTest test )
        {
            try
            {
                var lava = test.GetAttributeValue( "Lava" );
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );
                var result = lava.ResolveMergeFields( mergeFields, null, test.GetAttributeValue( "LavaCommands" ) );

                if ( Regex.IsMatch( result, test.GetAttributeValue( "MatchingExpression" ) ) )
                {

                    return new SystemTestResult
                    {
                        Passed = true
                    };
                }
                else
                {
                    return new SystemTestResult
                    {
                        Passed = false,
                        Message = "Result did not match expression"
                    };

                }
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e );
                return new SystemTestResult
                {
                    Passed = false,
                    Message = "An exception occurred while trying to run test."
                };
            }
        }
    }
}
