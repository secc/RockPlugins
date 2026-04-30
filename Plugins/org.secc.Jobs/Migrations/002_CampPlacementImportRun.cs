// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using Rock.Plugin;

namespace org.secc.Jobs.Migrations
{
    [MigrationNumber( 2, "1.12.0" )]
    public partial class CampPlacementImportRun : Migration
    {
        public override void Up()
        {
            Sql( @"
IF OBJECT_ID('[dbo].[_org_secc_CampPlacementImportRun]', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[_org_secc_CampPlacementImportRun] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Guid] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [CreatedDateTime] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreatedByPersonAliasId] INT NULL,
        [Status] INT NOT NULL, -- 0=Queued,1=Running,2=Completed,3=Failed
        [StatusMessage] NVARCHAR(MAX) NULL,
        [PercentComplete] INT NOT NULL DEFAULT 0,
        [ProcessedRows] INT NOT NULL DEFAULT 0,
        [TotalRows] INT NOT NULL DEFAULT 0,
        [SuccessCount] INT NOT NULL DEFAULT 0,
        [SkippedCount] INT NOT NULL DEFAULT 0,
        [ErrorCount] INT NOT NULL DEFAULT 0,
        [RequestJson] NVARCHAR(MAX) NOT NULL,
        [ResultHtml] NVARCHAR(MAX) NULL,
        [CompletedDateTime] DATETIME NULL
    );

    CREATE UNIQUE INDEX [IX__org_secc_CampPlacementImportRun_Guid]
        ON [dbo].[_org_secc_CampPlacementImportRun]([Guid]);

    -- FIX: Included Foreign Key and Index back to PersonAlias for referential integrity
    ALTER TABLE [dbo].[_org_secc_CampPlacementImportRun]  
        ADD CONSTRAINT [FK__org_secc_CampPlacementImportRun_CreatedByPersonAliasId] 
        FOREIGN KEY([CreatedByPersonAliasId]) REFERENCES [dbo].[PersonAlias] ([Id]);

    CREATE INDEX [IX__org_secc_CampPlacementImportRun_CreatedByPersonAliasId] 
        ON [dbo].[_org_secc_CampPlacementImportRun]([CreatedByPersonAliasId]);
END" );
        }

        public override void Down()
        {
            Sql( @"
IF OBJECT_ID('[dbo].[_org_secc_CampPlacementImportRun]', 'U') IS NOT NULL
BEGIN
    -- Implicitly drops constraints and indices associated with the table
    DROP TABLE [dbo].[_org_secc_CampPlacementImportRun];
END" );
        }
    }
}
