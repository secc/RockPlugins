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
    /// ROCK-8501 follow-up: provisions a WorkflowTrigger that fires the
    /// <see cref="org.secc.Workflow.Medication.PropagateMedicationActiveState"/>
    /// action whenever an <c>AttributeValue</c> row for the <c>MedicationActive</c>
    /// attribute is saved. The trigger uses an EntityTypeQualifier on
    /// <c>AttributeId</c>, so every other AttributeValue save in the system is
    /// a single column comparison and skip — no broad fan-out.
    ///
    /// Kill switch (no deploy required):
    ///   <c>UPDATE WorkflowTrigger SET IsActive = 0 WHERE [Guid] = 'CC5A8B9C-1A2B-4C3D-9E5F-6A7B8C9D0E1F'</c>
    /// </summary>
    [MigrationNumber( 1, "1.16.0" )]
    public class MedicationActiveSyncTrigger : Migration
    {
        // Guids for the rows this migration provisions. Held as private
        // constants so the Down() method and the kill-switch comment above
        // reference a single source of truth.
        private const string CategoryGuid             = "8E4F2A1B-6C7D-4E5F-A1B2-C3D4E5F6A7B8";
        private const string ComponentEntityTypeGuid  = "7B5C8D9E-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string WorkflowTypeGuid         = "9C5D8E9F-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string ActivityTypeGuid         = "AC5E8F9A-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string ActionTypeGuid           = "BC5F8A9B-1A2B-4C3D-9E5F-6A7B8C9D0E1F";
        private const string TriggerGuid              = "CC5A8B9C-1A2B-4C3D-9E5F-6A7B8C9D0E1F";

        // Rock SystemGuid.Category.WORKFLOW_TYPE — categories for workflow types are scoped
        // by this EntityType Guid (matches RockMigrationHelper.UpdateCategory's first arg
        // as seen in org.secc.PastoralCare/Migrations/002_Pastoral_WorkflowData.cs).
        private const string WorkflowTypeEntityTypeGuid = "C9F3C4A5-1526-474D-803F-D6C7A45CBBAE";

        public override void Up()
        {
            // 1. Ensure the EntityType row for the new ActionComponent has our known Guid.
            //    Rock's MEF discovery auto-inserts this row on app start with a random Guid;
            //    UpdateEntityType updates by Name so our migration is order-independent.
            RockMigrationHelper.UpdateEntityType(
                "org.secc.Workflow.Medication.PropagateMedicationActiveState",
                ComponentEntityTypeGuid,
                false,  // isEntity (this is a component, not a persisted entity)
                true ); // isSecured

            // 2. Category under which the new workflow type is filed in Rock admin.
            //    Hidden away under a "SECC > System" parent would be cleaner; using a
            //    flat "Medication Sync" category for now since no SECC system category Guid is established.
            RockMigrationHelper.UpdateCategory(
                WorkflowTypeEntityTypeGuid,
                "Medication Sync",
                "fa fa-medkit",
                "System workflows that propagate medication state changes between Person master and per-event snapshot matrices.",
                CategoryGuid );

            // 3. The workflow type itself. IsPersisted=false so each firing is in-memory
            //    only — no rows written to Workflow/WorkflowLog tables.
            //    Args: (isSystem, isActive, name, description, categoryGuid, workTerm,
            //           iconCssClass, processingIntervalSeconds, isPersisted, loggingLevel, guid)
            RockMigrationHelper.UpdateWorkflowType(
                true,                          // isSystem (hides from non-system filter)
                true,                          // isActive
                "Medication Active State Sync",
                "Propagates MedicationActive changes from Person master medication matrices to camp snapshot matrices.",
                CategoryGuid,
                "Sync",
                "fa fa-medkit",
                0,                             // no rate limiting
                false,                         // isPersisted — transient
                0,                             // loggingLevel = None
                WorkflowTypeGuid );

            // 4. Single activity that runs the propagation action.
            //    Args: (workflowTypeGuid, isActive, name, description, isActivatedWithWorkflow, order, guid)
            RockMigrationHelper.UpdateWorkflowActivityType(
                WorkflowTypeGuid,
                true,
                "Propagate",
                "Runs the propagation action.",
                true,                          // activates with workflow
                0,
                ActivityTypeGuid );

            // 5. The action wiring — points the activity at our C# component.
            //    Args: (activityTypeGuid, name, order, entityTypeGuid, isActionCompletedOnSuccess,
            //           isActivityCompletedOnSuccess, criteriaAttributeGuid, criteriaComparisonType,
            //           criteriaValue, guid)
            RockMigrationHelper.UpdateWorkflowActionType(
                ActivityTypeGuid,
                "Propagate Medication Active State",
                0,
                ComponentEntityTypeGuid,
                true,                          // action complete on success
                true,                          // activity complete on success (single-action activity)
                "",
                "",
                1,
                "",
                ActionTypeGuid );

            // 6. The WorkflowTrigger row. Fires only when an AttributeValue with
            //    AttributeId = <MedicationActive attribute id> is saved.
            //
            //    The MedicationActive AttributeId is looked up at runtime because
            //    its numeric id differs between environments. Same pattern as
            //    org.secc.Reporting/Migrations/006_MedicationReportFilterInactive.cs.
            //
            //    WorkflowTriggerType values (from Rock/Model/Workflow/WorkflowTrigger/WorkflowTriggerType.cs):
            //      PreSave=0, PostSave=1, PreDelete=2, PostDelete=3, ImmediatePostSave=4, PostAdd=5
            //    We use PostSave (1): the trigger queues onto WrappedTransactionCompletedTask so
            //    propagation runs after the master AV commits, in a separate context. A propagation
            //    failure cannot roll back the user's edit.
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

            // Component EntityType row is intentionally left in place — Rock's MEF
            // discovery would just re-create it on next app start, and other rows
            // might still reference it.
        }
    }
}
