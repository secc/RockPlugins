using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Search.Group;

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

        [LavaInclude]
        public virtual EntityType EntityType { get; set; }

        [DataMember]
        public int? RunIntervalMinutes { get; set; }

        [DataMember]
        public AlarmCondition AlarmCondition { get; set; } = AlarmCondition.Never;

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
                Passed = result.Passed
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

    public partial class SystemTestConfiguration : EntityTypeConfiguration<SystemTest>
    {
        public SystemTestConfiguration()
        {
            this.HasOptional<EntityType>( t => t.EntityType ).WithMany().WillCascadeOnDelete( false );
            this.HasEntitySetName( "SystemTest" );
        }
    }
}
