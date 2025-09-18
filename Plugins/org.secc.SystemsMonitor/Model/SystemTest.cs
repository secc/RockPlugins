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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Lava;

namespace org.secc.SystemsMonitor.Model
{
    [Table( "_org_secc_SystemsMonitor_SystemTest" )]
    [DataContract]
    public class SystemTest : Model<SystemTest>, IRockEntity
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public int? EntityTypeId { get; set; }

        [LavaVisibleAttribute]
        public virtual EntityType EntityType { get; set; }

        [DataMember]
        public int? RunIntervalMinutes { get; set; }

        [DataMember]
        public AlarmCondition AlarmCondition { get; set; } = AlarmCondition.Never;

        [DataMember]
        public AlarmNotification? AlarmNotification { get; set; }

        [DataMember]
        public int? AlarmScore { get; set; }

        public SystemTestResult Run()
        {
            this.LoadAttributes();
            var component = ( SystemTestComponent ) SystemTestContainer.Instance.Dictionary
               .Where( c => c.Value.Value.EntityType.Id == EntityTypeId )
               .Select( c => c.Value.Value )
               .FirstOrDefault();

            var result = component.RunTest( this );

            RockContext rockContext = new RockContext();
            SystemTestHistoryService systemTestHistoryService = new SystemTestHistoryService( rockContext );

            var history = new SystemTestHistory
            {
                SystemTestId = Id,
                Score = result.Score,
                Passed = result.Passed,
                Message = result.Message
            };

            systemTestHistoryService.Add( history );
            rockContext.SaveChanges();

            return result;
        }

        public bool MeetsAlarmCondition( SystemTestResult testResult )
        {
            switch ( AlarmCondition )
            {
                case AlarmCondition.Never:
                    return false;
                case AlarmCondition.Fail:
                    return !testResult.Passed;
                case AlarmCondition.ScoreAbove:
                    return AlarmScore.HasValue && testResult.Score > AlarmScore;
                case AlarmCondition.ScoreBelow:
                    return AlarmScore.HasValue && testResult.Score < AlarmScore;
                default:
                    return false;
            }
        }
    }

    public enum AlarmCondition
    {
        Never = 0,
        Fail = 1,
        ScoreAbove = 2,
        ScoreBelow = 3,
    }

    [Flags]
    public enum AlarmNotification
    {
        Email = 1,
        SMS = 2
    }

    public partial class SystemTestConfiguration : EntityTypeConfiguration<SystemTest>
    {
        public SystemTestConfiguration()
        {
            this.HasOptional<EntityType>( t => t.EntityType ).WithMany().WillCascadeOnDelete( false );
            this.HasEntitySetName( "SystemTest" );
        }
    }
}
