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


namespace org.secc.Migrations
{
    [MigrationNumber( 19, "1.13.0" )]
    class WorshipAttendanceTrigger : Migration
    {

        public override void Up()
        {
            Sql( @"
                IF EXISTS (SELECT * FROM sys.objects where [name] = '_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory' AND TYPE = 'TR')
                BEGIN
                    DROP TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory] 
                END
            " );

            Sql( @"
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
            " );
            Sql( @"ALTER TABLE [dbo].[MetricValue] ENABLE TRIGGER [_org_secc_trgMetricValueWorshipAttendanceInsertLogToHistory]" );


            Sql( @"
                IF EXISTS (SELECT * FROM sys.objects where [name] = '_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory' AND TYPE = 'TR')
                BEGIN
                    DROP TRIGGER [dbo].[_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory] 
                END
            " );
            Sql( @"
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
            " );

            Sql( "ALTER TABLE [dbo].[MetricValue] ENABLE TRIGGER [_org_secc_trgMetricValueWorshipAttendanceUpdateLogToHistory]" );
        }

        public override void Down()
        {
            //Intentionally left blank
        }


    }
}
