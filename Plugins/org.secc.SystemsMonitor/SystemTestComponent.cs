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
using System.Collections.Generic;
using org.secc.SystemsMonitor.Model;

namespace org.secc.SystemsMonitor
{
    public abstract class SystemTestComponent : Rock.Extension.Component
    {
        public abstract string Name { get; }
        public abstract string Icon { get; }
        public abstract SystemTestResult RunTest( SystemTest monitorTest );
        public virtual List<AlarmCondition> SupportedAlarmConditions
        {
            get => new List<AlarmCondition> {
                AlarmCondition.Never,
                AlarmCondition.Fail,
                AlarmCondition.ScoreAbove,
                AlarmCondition.ScoreBelow
            };
        }
    }
}
