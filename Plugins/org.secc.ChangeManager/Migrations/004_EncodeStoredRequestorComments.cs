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

namespace org.secc.ChangeManager.Migrations
{
    using Rock.Plugin;

    /// <summary>
    /// ROCK-8879. ChangeRequestDetail no longer HTML-encodes RequestorComment at render
    /// time, because the field holds system-generated markup composed at write time. Rows
    /// written before that change stored raw, unencoded requestor text — except that some
    /// also contain system-composed markup mixed in (the duplicate-mobile-number warning
    /// from ChangeEntry and the "Comment:" separator from CMPublicProfileEdit). This
    /// migration encodes every stored value to neutralize the user text, then restores
    /// those exact known system fragments so historical warnings keep rendering as markup.
    ///
    /// This must ship in the same deploy as the block changes: it assumes existing rows
    /// are unencoded, which stops being true once the new write sites are live. The whole
    /// Up() runs inside one IF guarded by the backup table's existence, so a re-run
    /// (tracked-migration bookkeeping failing after the data commit, a restored database,
    /// or the SQL being run by hand) is a no-op rather than a double-encode.
    ///
    /// Original values are copied to _org_secc_ChangeManager_ChangeRequestCommentBackup
    /// first, so Down() restores exactly and the change stays auditable. That table can be
    /// dropped once the results have been spot-checked — but note dropping it also removes
    /// the re-run guard, so only drop it once the PluginMigration row is confirmed present.
    /// </summary>
    [MigrationNumber( 4, "1.13.7" )]
    public partial class EncodeStoredRequestorComments : Migration
    {
        public override void Up()
        {
            // Everything runs inside the one IF: the backup table's existence is the
            // sentinel that the data work already happened, which makes the whole
            // migration idempotent (Rock commits the migration transaction before it
            // records the PluginMigration row, so a failure in that gap re-runs Up()).
            Sql( @"
                IF OBJECT_ID( '_org_secc_ChangeManager_ChangeRequestCommentBackup' ) IS NULL
                BEGIN
                    -- Preserve the originals before touching anything.
                    CREATE TABLE [_org_secc_ChangeManager_ChangeRequestCommentBackup] (
                        [ChangeRequestId] INT NOT NULL
                            CONSTRAINT [PK__org_secc_ChangeManager_ChangeRequestCommentBackup] PRIMARY KEY,
                        [RequestorComment] NVARCHAR(MAX) NULL,
                        [BackedUpDateTime] DATETIME NOT NULL
                            CONSTRAINT [DF__org_secc_ChangeManager_ChangeRequestCommentBackup_BackedUpDateTime] DEFAULT ( GETDATE() )
                    );

                    INSERT INTO [_org_secc_ChangeManager_ChangeRequestCommentBackup] ( [ChangeRequestId], [RequestorComment] )
                    SELECT [Id], [RequestorComment]
                    FROM [_org_secc_ChangeManager_ChangeRequest]
                    WHERE [RequestorComment] IS NOT NULL;

                    -- Whitespace-only comments become empty, so the detail block's
                    -- IsNotNullOrWhiteSpace() guard keeps hiding them once line breaks
                    -- turn into <br> below. Runs before the encode so it sees the raw
                    -- whitespace.
                    UPDATE [_org_secc_ChangeManager_ChangeRequest]
                    SET [RequestorComment] = ''
                    WHERE [RequestorComment] IS NOT NULL
                        AND LTRIM( RTRIM( REPLACE( REPLACE( REPLACE( [RequestorComment], CHAR(13), ' ' ), CHAR(10), ' ' ), CHAR(9), ' ' ) ) ) = '';

                    -- Mirror EncodeHtml().ConvertCrLfToHtmlBr(). The ampersand must be
                    -- replaced first or the entities introduced by the later replacements
                    -- get re-encoded, and the <br> substitution must come last or its
                    -- angle brackets get encoded. HttpUtility.HtmlEncode also emits
                    -- numeric entities for some characters above ASCII; those render
                    -- identically either way, so they are left alone here.
                    UPDATE [_org_secc_ChangeManager_ChangeRequest]
                    SET [RequestorComment] =
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE(
                                                REPLACE( [RequestorComment], '&', '&amp;' ),
                                            '<', '&lt;' ),
                                        '>', '&gt;' ),
                                    '""', '&quot;' ),
                                '''', '&#39;' ),
                            CHAR(13) + CHAR(10), '<br>' ),
                        CHAR(10), '<br>' )
                    WHERE [RequestorComment] IS NOT NULL
                        AND [RequestorComment] <> '';

                    -- Rows written before ROCK-8879 by ChangeEntry's duplicate-mobile
                    -- warning mixed system-composed markup into the same column; the
                    -- encode above turned that markup into literal text. Restore the
                    -- exact known system fragments so historical warnings keep rendering
                    -- as markup. The person names inside the warning stay encoded, which
                    -- is the point of the migration. (A user comment containing these
                    -- exact literal fragments would also be restored; accepted as
                    -- vanishingly unlikely for a data repair.)
                    UPDATE [_org_secc_ChangeManager_ChangeRequest]
                    SET [RequestorComment] =
                        REPLACE(
                            REPLACE(
                                REPLACE(
                                    REPLACE(
                                        REPLACE(
                                            REPLACE( [RequestorComment],
                                                '&lt;h4&gt;Dynamically Generated Warnings:&lt;/h4&gt;', '<h4>Dynamically Generated Warnings:</h4>' ),
                                            '&lt;ul&gt;', '<ul>' ),
                                        '&lt;/ul&gt;', '</ul>' ),
                                    '&lt;li&gt;&lt;a href=&#39;/Person/', '<li><a href=''/Person/' ),
                                '&#39; target=&#39;_blank&#39;&gt;', ''' target=''_blank''>' ),
                            '&lt;/a&gt;&lt;/li&gt;', '</a></li>' )
                    WHERE [RequestorComment] LIKE '%&lt;h4&gt;Dynamically Generated Warnings:&lt;/h4&gt;%';

                    -- Same for CMPublicProfileEdit's system-composed separator between
                    -- the fixed 'Added as new person' text and the user's comment.
                    UPDATE [_org_secc_ChangeManager_ChangeRequest]
                    SET [RequestorComment] = REPLACE( [RequestorComment], '&lt;br&gt;&lt;br&gt;Comment: ', '<br><br>Comment: ' )
                    WHERE [RequestorComment] LIKE '%&lt;br&gt;&lt;br&gt;Comment: %';
                END
" );
        }

        public override void Down()
        {
            Sql( @"
                IF OBJECT_ID( '_org_secc_ChangeManager_ChangeRequestCommentBackup' ) IS NOT NULL
                BEGIN
                    UPDATE cr
                    SET cr.[RequestorComment] = b.[RequestorComment]
                    FROM [_org_secc_ChangeManager_ChangeRequest] cr
                    INNER JOIN [_org_secc_ChangeManager_ChangeRequestCommentBackup] b
                        ON b.[ChangeRequestId] = cr.[Id];

                    DROP TABLE [_org_secc_ChangeManager_ChangeRequestCommentBackup];
                END
" );
        }
    }
}
