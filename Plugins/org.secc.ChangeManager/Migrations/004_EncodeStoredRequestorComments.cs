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
    /// time, because the field holds system-generated markup composed at write time. Every
    /// row written before that change stored raw, unencoded requestor text, so this
    /// migration encodes the stored values to match the new write-site behavior.
    ///
    /// This must ship in the same deploy as the block changes: it assumes every existing
    /// row is unencoded, which stops being true once the new write sites are live. Rock
    /// runs plugin migrations once, tracked in the PluginMigration table, so it will not
    /// double-encode on subsequent restarts. Do not re-run the SQL by hand.
    ///
    /// Original values are copied to _org_secc_ChangeManager_ChangeRequestCommentBackup
    /// first, so Down() restores exactly and the change stays auditable. That table can be
    /// dropped once the results have been spot-checked.
    /// </summary>
    [MigrationNumber( 4, "1.13.7" )]
    public partial class EncodeStoredRequestorComments : Migration
    {
        public override void Up()
        {
            // Preserve the originals before touching anything.
            Sql( @"
                IF OBJECT_ID( '_org_secc_ChangeManager_ChangeRequestCommentBackup' ) IS NULL
                BEGIN
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
                END
" );

            // Whitespace-only comments become empty, so the detail block's
            // IsNotNullOrWhiteSpace() guard keeps hiding them once line breaks turn into
            // <br> below. Run this before the encode so it sees the raw whitespace.
            Sql( @"
                UPDATE [_org_secc_ChangeManager_ChangeRequest]
                SET [RequestorComment] = ''
                WHERE [RequestorComment] IS NOT NULL
                    AND LTRIM( RTRIM( REPLACE( REPLACE( REPLACE( [RequestorComment], CHAR(13), ' ' ), CHAR(10), ' ' ), CHAR(9), ' ' ) ) ) = '';
" );

            // Mirror EncodeHtml().ConvertCrLfToHtmlBr(). The ampersand must be replaced
            // first or the entities introduced by the later replacements get re-encoded,
            // and the <br> substitution must come last or its angle brackets get encoded.
            // HttpUtility.HtmlEncode also emits numeric entities for some characters above
            // ASCII; those render identically either way, so they are left alone here.
            Sql( @"
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
