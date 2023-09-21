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


namespace org.secc.Reporting.Migrations
{
    using Rock.Model;
    using Rock.Plugin;
    [MigrationNumber(2, "1.12.0")]
    public partial class WorshipAttendanceHistory : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateCategory( "546d5f43-1184-47c9-8265-2d7bf4e1bca5", "Metric Value Update",
                "fas fa-tally", "", "d68c2db1-fcde-418d-b29b-86d7eebed794" );

            var dropInsertTrigger = @"
IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory]'))
BEGIN
    DROP TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory]
END";

            var insertTrigger= @"
        
CREATE TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory] 
ON [dbo].[MetricValue]
AFTER INSERT AS
BEGIN
    DECLARE @Id INT,
            @MetricId INT,
            @YValue DECIMAL(18,2),
            @ModifiedByPersonAliasId INT,
            @HistoryCategoryId INT,
            @EntityTypeId INT

    -- Set some working variables
    SELECT  @Id = Id, 
            @MetricId = MetricId, 
            @YValue = YValue, 
            @ModifiedByPersonAliasID = ModifiedByPersonAliasId
    FROM Inserted;

    -- If Metric Value is not 2 (Weekend Worship) Return
    IF(@MetricId <> 2) 
    RETURN;

    SELECT TOP 1 @HistoryCategoryId = Id 
    FROM dbo.Category 
    WHERE GUID = 'd68c2db1-fcde-418d-b29b-86d7eebed794'

    SELECT TOP 1 @EntityTypeId = Id
    FROM dbo.EntityType
    WHERE Name = 'Rock.Model.MetricValue'

    INSERT INTO dbo.History
    (  
        IsSystem,
        CategoryId,
        EntityTypeId,
        EntityId,
        Caption,
        Summary,
        Guid,
        CreatedDateTime,
        ModifiedDateTime,
        CreatedByPersonAliasId,
        ModifiedByPersonAliasId,
        Verb,
        ChangeType,
        ValueName,
        NewValue,
        OldValue
    )
    VALUES
    (
        0,
        @HistoryCategoryId,
        @EntityTypeId,
        @Id,
        'Worship Attendance Insert',
        'New Worship Attendance Metric Value Created, History Added by dbo._org_secc_trgMetricValueAttendanceInsertLogToHistory',
        NEWID(),
        GETDATE(),
        GETDATE(),
        @ModifiedByPersonAliasID,
        @ModifiedByPersonAliasID,
        'ADD',
        'RECORD',
        'YValue',
        CAST(@YValue AS NVARCHAR(MAX)),
        NULL
    )
END

ALTER TABLE [dbo].[MetricValue] ENABLE TRIGGER [_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory]

";


            var dropUpdateTrigger = @"
IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory]'))
BEGIN
    DROP TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory] 
END";

            var updateTrigger = @"
CREATE TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory] 
ON [dbo].[MetricValue]
AFTER UPDATE AS
BEGIN

    -- IF YValue Not changing return
    IF NOT UPDATE(YValue)
    RETURN;

    DECLARE @Id INT,
            @MetricId INT,
            @YValue DECIMAL(18,2),
            @OriginalValue DECIMAL(18,2),
            @ModifiedByPersonAliasId INT,
            @HistoryCategoryId INT,
            @EntityTypeId INT

    -- Set some working variables
    SELECT  @Id = Id, 
            @MetricId = MetricId, 
            @YValue = YValue, 
            @ModifiedByPersonAliasID = ModifiedByPersonAliasId
    FROM Inserted;

    SELECT @OriginalValue = YValue 
    FROM Deleted;

    --YValue didn't change
    IF(@OriginalValue = @YValue)
    RETURN;

    -- If Metric Value is not 2 (Weekend Worship) Return
    IF(@MetricId <> 2) 
    RETURN;

    SELECT TOP 1 @HistoryCategoryId = Id 
    FROM dbo.Category 
    WHERE GUID = 'd68c2db1-fcde-418d-b29b-86d7eebed794'

    SELECT TOP 1 @EntityTypeId = Id
    FROM dbo.EntityType
    WHERE Name = 'Rock.Model.MetricValue'

    INSERT INTO dbo.History
    (  
        IsSystem,
        CategoryId,
        EntityTypeId,
        EntityId,
        Caption,
        Summary,
        Guid,
        CreatedDateTime,
        ModifiedDateTime,
        CreatedByPersonAliasId,
        ModifiedByPersonAliasId,
        Verb,
        ChangeType,
        ValueName,
        NewValue,
        OldValue
    )
    VALUES
    (
        0,
        @HistoryCategoryId,
        @EntityTypeId,
        @Id,
        'Worship Attendance Value Updated',
        'New Worship Attendance Metric Value Updated, History Added by dbo._org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory',
        NEWID(),
        GETDATE(),
        GETDATE(),
        @ModifiedByPersonAliasID,
        @ModifiedByPersonAliasID,
        'UPDATE',
        'PROPERTY',
        'YValue',
        CAST(@YValue AS NVARCHAR(MAX)),
        CAST(@OriginalValue AS NVARCHAR(MAX))
    )
END

ALTER TABLE [dbo].[MetricValue] ENABLE TRIGGER [_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory]
";
            Sql( dropInsertTrigger );
            Sql( dropUpdateTrigger );
            Sql( insertTrigger );
            Sql( updateTrigger );
        }

        public override void Down()
        {
            var insertTrigger = @"
IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory]'))
BEGIN
    DROP TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory]
END
";

            var updateTrigger = @"
IF EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[dbo].[_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory]'))
BEGIN
    DROP TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory] 
END
";

            Sql( insertTrigger );
            Sql( updateTrigger );
        }
    }
}
