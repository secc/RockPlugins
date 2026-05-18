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
using Rock.Plugin;

namespace org.secc.Workflow.Migrations
{
    /// <summary>
    /// ROCK-8501: provisions the WorkflowTrigger that fires
    /// <see cref="org.secc.Workflow.Medication.PropagateMedicationActiveState"/>
    /// on AttributeValue PostSave for the MedicationActive attribute.
    ///
    /// Kill switch:
    ///   <c>UPDATE WorkflowTrigger SET IsActive = 0 WHERE [Guid] = 'CC5A8B9C-1A2B-4C3D-9E5F-6A7B8C9D0E1F'</c>
    /// </summary>
    [MigrationNumber( 1, "1.16.0" )]
    public class MedicationActiveSyncTrigger : Migration
    {
        // Stable Guids for the rows this migration provisions.
        private const string CategoryGuid             = "8E4F2A1B-6C7D-4E5F-A1B2-C3D4E5F6A7B8";
        private const string ComponentEntityTypeGuid  = "7B5C8D9E-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string WorkflowTypeGuid         = "9C5D8E9F-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string ActivityTypeGuid         = "AC5E8F9A-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string ActionTypeGuid           = "BC5F8A9B-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string TriggerGuid              = "CC5A8B9C-1A2B-4C3D-9E5F-6A7B8C9D0E1F";

        // Rock SystemGuid.Category.WORKFLOW_TYPE — scope for the workflow-type category.
        private const string WorkflowTypeEntityTypeGuid = "C9F3C4A5-1526-474D-803F-D6C7A45CBBAE";

        public override void Up()
        {
            // Pin our ActionComponent's EntityType row to a known Guid so the action wiring below can reference it.
            // Rock's MEF discovery would otherwise pick a random Guid on first app start.
            RockMigrationHelper.UpdateEntityType(
                "org.secc.Workflow.Medication.PropagateMedicationActiveState",
                ComponentEntityTypeGuid,
                false,
                true );

            RockMigrationHelper.UpdateCategory(
                WorkflowTypeEntityTypeGuid,
                "Medication Sync",
                "fa fa-medkit",
                "System workflows that propagate medication state changes between Person master and per-event snapshot matrices.",
                CategoryGuid );

            // IsPersisted=false keeps each firing in-memory — no rows written to Workflow / WorkflowLog.
            RockMigrationHelper.UpdateWorkflowType(
                true,                          // isSystem
                true,                          // isActive
                "Medication Active State Sync",
                "Propagates MedicationActive changes from Person master medication matrices to camp snapshot matrices.",
                CategoryGuid,
                "Sync",
                "fa fa-medkit",
                0,
                false,                         // isPersisted
                0,                             // loggingLevel = None
                WorkflowTypeGuid );

            RockMigrationHelper.UpdateWorkflowActivityType(
                WorkflowTypeGuid,
                true,
                "Propagate",
                "Runs the propagation action.",
                true,
                0,
                ActivityTypeGuid );

            RockMigrationHelper.UpdateWorkflowActionType(
                ActivityTypeGuid,
                "Propagate Medication Active State",
                0,
                ComponentEntityTypeGuid,
                true,
                true,
                "",
                "",
                1,
                "",
                ActionTypeGuid );

            // WorkflowTrigger row. EntityTypeQualifier on AttributeId scopes it to one attribute only —
            // every other AttributeValue save is a cheap column-compare and skip.
            // MedicationActive AttributeId is looked up at runtime (varies per environment).
            // WorkflowTriggerType: 1 = PostSave (fires after the master AV commits, in a separate context).
            Sql( $@"
                DECLARE @MedicationActiveAttributeId INT = (
                    SELECT TOP 1 a.Id
                    FROM Attribute a
                    INNER JOIN AttributeMatrixTemplate amt
                        ON CAST(amt.Id AS NVARCHAR(20)) = a.EntityTypeQualifierValue
                    WHERE a.[Key] = 'MedicationActive'
                      AND a.EntityTypeQualifierColumn = 'AttributeMatrixTemplateId'
                      AND amt.[Guid] = 'd2ed9b3d-309a-4a70-bda4-2c090acd1384'
                );

                DECLARE @AttributeValueEntityTypeId INT = (
                    SELECT TOP 1 Id FROM EntityType WHERE [Name] = 'Rock.Model.AttributeValue'
                );

                DECLARE @WorkflowTypeId INT = (
                    SELECT TOP 1 Id FROM WorkflowType WHERE [Guid] = '{WorkflowTypeGuid}'
                );

                IF @MedicationActiveAttributeId IS NOT NULL
                   AND @AttributeValueEntityTypeId IS NOT NULL
                   AND @WorkflowTypeId IS NOT NULL
                BEGIN
                    IF NOT EXISTS ( SELECT 1 FROM WorkflowTrigger WHERE [Guid] = '{TriggerGuid}' )
                    BEGIN
                        INSERT INTO WorkflowTrigger
                            ( IsSystem, EntityTypeId, EntityTypeQualifierColumn,
                              EntityTypeQualifierValue, WorkflowTypeId, WorkflowTriggerType,
                              WorkflowName, IsActive, [Guid] )
                        VALUES
                            ( 1, @AttributeValueEntityTypeId, 'AttributeId',
                              CAST( @MedicationActiveAttributeId AS NVARCHAR(20) ), @WorkflowTypeId, 1,
                              'Medication Active State Sync', 1, '{TriggerGuid}' );
                    END
                    ELSE
                    BEGIN
                        UPDATE WorkflowTrigger
                        SET EntityTypeId             = @AttributeValueEntityTypeId,
                            EntityTypeQualifierColumn = 'AttributeId',
                            EntityTypeQualifierValue  = CAST( @MedicationActiveAttributeId AS NVARCHAR(20) ),
                            WorkflowTypeId            = @WorkflowTypeId,
                            WorkflowTriggerType       = 1,
                            WorkflowName              = 'Medication Active State Sync',
                            IsActive                  = 1
                        WHERE [Guid] = '{TriggerGuid}';
                    END
                END
            " );
        }

        public override void Down()
        {
            Sql( $"DELETE FROM WorkflowTrigger WHERE [Guid] = '{TriggerGuid}';" );

            RockMigrationHelper.DeleteWorkflowActionType( ActionTypeGuid );
            RockMigrationHelper.DeleteWorkflowActivityType( ActivityTypeGuid );
            RockMigrationHelper.DeleteWorkflowType( WorkflowTypeGuid );
            RockMigrationHelper.DeleteCategory( CategoryGuid );

            // Leave the component EntityType row in place — Rock MEF would recreate it anyway.
        }
    }
}
