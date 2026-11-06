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
using org.secc.LinkList.SystemGuids;
using org.secc.LinkList.Utility;

using Rock.Plugin;

namespace org.secc.LinkList.Migrations
{
    /// <summary>
    /// ROCK-9362: creates the "Link List File" BinaryFileType that editor uploads
    /// (PDF / JPG / PNG link targets) are stored under.
    ///
    /// - Storage: Rock's Database provider, so it works in every environment with no
    ///   provider configuration. An admin can switch it to Azure Blob Storage under
    ///   Admin Tools > General Settings > File Types; existing files keep the provider
    ///   they were written with. The row is INSERT-ONLY so that change survives re-runs.
    /// - Uploads: through the plugin's own api/secc/linklist/upload, which enforces the
    ///   PDF/JPG/PNG allow-list, magic bytes and size cap and records the uploader.
    ///   No EDIT rule is added, so FileUploader.ashx (which needs EDIT on the type and
    ///   checks none of that) refuses everyone but Rock admins for this type.
    /// - Size: PreferredRequired + MaxFileSizeBytes also make FileUploader.ashx reject
    ///   an oversize admin upload (Rock only enforces MaxFileSizeBytes when
    ///   PreferredRequired is set).
    /// - Security: RequiresViewSecurity with VIEW allowed to Rock admins and denied to
    ///   everyone else, so GetFile.ashx refuses these files. They are served only by
    ///   the plugin's REST file endpoint, which applies the owning list's IsPublic /
    ///   edit gate.
    ///
    /// Number 13, not 12: ROCK-9361 (link card display style) claims 012 on a
    /// parallel branch. Rock runs any plugin migration number not yet recorded for the
    /// assembly, so the two can merge in either order.
    /// </summary>
    [MigrationNumber( 13, "1.16.0" )]
    public class AddLinkListFileType : Migration
    {
        public override void Up()
        {
            Sql( $@"
IF NOT EXISTS ( SELECT 1 FROM [BinaryFileType] WHERE [Guid] = '{LinkListGuids.BinaryFileTypeLinkListFile}' )
BEGIN
    DECLARE @StorageEntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType]
        WHERE [Guid] = '0AA42802-04FD-4AEC-B011-FEB127FC85CD'); -- Rock.Storage.Provider.Database

    INSERT INTO [BinaryFileType] (
        [IsSystem], [Name], [Description], [IconCssClass], [StorageEntityTypeId],
        [CacheToServerFileSystem], [RequiresViewSecurity],
        [PreferredFormat], [PreferredResolution], [PreferredColorDepth], [PreferredRequired],
        [MaxFileSizeBytes], [AllowAnonymous], [CacheControlHeaderSettings],
        [Guid], [CreatedDateTime], [ModifiedDateTime] )
    VALUES (
        1, 'Link List File',
        'PDF, JPG and PNG files uploaded as Link List link targets. Uploaded through api/secc/linklist/upload and served only through api/secc/linklist/{{list}}/file/{{file}}, which applies the list''s public / edit gate.',
        'fa fa-file', @StorageEntityTypeId,
        0, 1,
        -1, -1, -1, 1,
        {LinkListFiles.MaxFileSizeBytes}, 0, '{{""RockCacheablityType"":3,""MaxAge"":null,""SharedMaxAge"":null}}',
        '{LinkListGuids.BinaryFileTypeLinkListFile}', GETDATE(), GETDATE() );
END
" );

            // VIEW: Rock admins only (order 0 allow, order 1 deny All Users).
            RockMigrationHelper.AddSecurityAuthForBinaryFileType(
                LinkListGuids.BinaryFileTypeLinkListFile, 0, Rock.Security.Authorization.VIEW, true,
                Rock.SystemGuid.Group.GROUP_ADMINISTRATORS, Rock.Model.SpecialRole.None,
                LinkListGuids.AuthLinkListFileViewAdmins );
            RockMigrationHelper.AddSecurityAuthForBinaryFileType(
                LinkListGuids.BinaryFileTypeLinkListFile, 1, Rock.Security.Authorization.VIEW, false,
                null, Rock.Model.SpecialRole.AllUsers,
                LinkListGuids.AuthLinkListFileViewDenyAll );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileViewDenyAll );
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileViewAdmins );

            // Only drop the type when nothing references it; uploaded files are content,
            // not schema, and must not disappear with a rollback.
            Sql( $@"
DECLARE @TypeId INT = (SELECT TOP 1 [Id] FROM [BinaryFileType] WHERE [Guid] = '{LinkListGuids.BinaryFileTypeLinkListFile}');
IF @TypeId IS NOT NULL AND NOT EXISTS ( SELECT 1 FROM [BinaryFile] WHERE [BinaryFileTypeId] = @TypeId )
    DELETE FROM [BinaryFileType] WHERE [Id] = @TypeId;
" );
        }
    }
}
