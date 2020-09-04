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
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

namespace org.secc.SystemsMonitor.Component
{
    [Export( typeof( SystemTestComponent ) )]
    [ExportMetadata( "ComponentName", "SQL Expression Match" )]
    [Description( "Uses SQL to monitor state of the system." )]

    [CodeEditorField( "SQL Query", mode: CodeEditorMode.Sql, order: 0 )]
    [TextField( "Matching Expression", "Regular expression to match a success.", order: 1 )]
    public class SQLMatchExpression : SystemTestComponent
    {
        public override string Name => "SQL Expression Match";

        public override string Icon => "fa fa-database";

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
                var query = test.GetAttributeValue( "SQLQuery" );
                object sqlResult = DbService.ExecuteScaler( query, System.Data.CommandType.Text );
                if ( sqlResult != null )
                {
                    string resultValue = sqlResult.ToString();

                    if ( Regex.IsMatch( resultValue, test.GetAttributeValue( "MatchingExpression" ) ) )
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
                            Message = "Query result did not match expression"
                        };

                    }
                }
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( e );
                return new SystemTestResult
                {
                    Passed = false,
                    Message = "An exception occurred while trying to monitor."
                };
            }
            return new SystemTestResult
            {
                Passed = false,
                Message = "SQl returned no results"
            };
        }
    }
}
