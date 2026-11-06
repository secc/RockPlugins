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
    /// - Storage: Rock core Azure Blob Storage (Rock.Storage.Provider.AzureBlobStorage),
    ///   matching the org's content file types (Media File, Generic Document, ...): no
    ///   container name (the provider's default container) and a folder named after the
    ///   type in PascalCase - "LinkListFile" - written to both AzureBlobContainerFolderPath
    ///   and the legacy pillars ContainerFolderPath key, as the 2026-08 provider
    ///   migration did for every type. Falls back to the Database provider only where
    ///   the Azure provider's EntityType row doesn't exist. The row is INSERT-ONLY so
    ///   an admin's later storage change survives. A local dev box without Azure
    ///   credentials must switch the type to Database under File Types to upload.
    /// - Uploads: through the plugin's own api/secc/linklist/upload, which enforces the
    ///   PDF/JPG/PNG allow-list, magic bytes and size cap, records the uploader, and
    ///   requires EDIT on this type. EDIT is granted to Rock admins, Rock Edit Access
    ///   (core staff) and Staff Like Workers, then denied to everyone else; change who
    ///   may upload on the type's security in Rock. FileUploader.ashx also checks EDIT,
    ///   but a file it creates carries no uploader, so SaveList refuses to attach it.
    /// - Size: PreferredRequired + MaxFileSizeBytes also make FileUploader.ashx reject
    ///   an oversize upload (Rock only enforces MaxFileSizeBytes when PreferredRequired
    ///   is set).
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
        /// <summary>Blob folder for this type (the type name in PascalCase, the org's convention).</summary>
        private const string BlobFolder = "LinkListFile";

        public override void Up()
        {
            Sql( $@"
IF NOT EXISTS ( SELECT 1 FROM [BinaryFileType] WHERE [Guid] = '{LinkListGuids.BinaryFileTypeLinkListFile}' )
BEGIN
    DECLARE @StorageEntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType]
        WHERE [Name] = 'Rock.Storage.Provider.AzureBlobStorage');
    IF @StorageEntityTypeId IS NULL
        SET @StorageEntityTypeId = (SELECT TOP 1 [Id] FROM [EntityType]
            WHERE [Guid] = '0AA42802-04FD-4AEC-B011-FEB127FC85CD'); -- Rock.Storage.Provider.Database

    INSERT INTO [BinaryFileType] (
        [IsSystem], [Name], [Description], [IconCssClass], [StorageEntityTypeId],
        [CacheToServerFileSystem], [RequiresViewSecurity],
        [PreferredFormat], [PreferredResolution], [PreferredColorDepth], [PreferredRequired],
        [MaxFileSizeBytes], [AllowAnonymous], [CacheControlHeaderSettings],
        [Guid], [CreatedDateTime], [ModifiedDateTime] )
    VALUES (
        1, 'Link List File',
        'PDF, JPG and PNG files uploaded as Link List link targets. Uploaded through api/secc/linklist/upload (requires EDIT here) and served only through api/secc/linklist/{{list}}/file/{{file}}, which applies the list''s public / edit gate.',
        'fa fa-file', @StorageEntityTypeId,
        0, 1,
        -1, -1, -1, 1,
        {LinkListFiles.MaxFileSizeBytes}, 0, '{{""RockCacheablityType"":3,""MaxAge"":null,""SharedMaxAge"":null}}',
        '{LinkListGuids.BinaryFileTypeLinkListFile}', GETDATE(), GETDATE() );

    -- Blob folder, written to every folder-path key Rock knows for BinaryFileType
    -- (core + legacy pillars), like the existing content types.
    DECLARE @TypeId INT = SCOPE_IDENTITY();
    INSERT INTO [AttributeValue] ( [IsSystem], [AttributeId], [EntityId], [Value], [Guid], [CreatedDateTime], [ModifiedDateTime] )
    SELECT 0, a.[Id], @TypeId, '{BlobFolder}', NEWID(), GETDATE(), GETDATE()
    FROM [Attribute] a
    WHERE a.[EntityTypeId] = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.BinaryFileType')
      AND a.[EntityTypeQualifierColumn] = 'StorageEntityTypeId'
      AND a.[Key] IN ( 'AzureBlobContainerFolderPath', 'ContainerFolderPath' )
      AND NOT EXISTS ( SELECT 1 FROM [AttributeValue] av WHERE av.[AttributeId] = a.[Id] AND av.[EntityId] = @TypeId );
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

            // EDIT (= may upload): admins, Rock Edit Access (core staff), Staff Like Workers.
            RockMigrationHelper.AddSecurityAuthForBinaryFileType(
                LinkListGuids.BinaryFileTypeLinkListFile, 0, Rock.Security.Authorization.EDIT, true,
                Rock.SystemGuid.Group.GROUP_ADMINISTRATORS, Rock.Model.SpecialRole.None,
                LinkListGuids.AuthLinkListFileEditAdmins );
            RockMigrationHelper.AddSecurityAuthForBinaryFileType(
                LinkListGuids.BinaryFileTypeLinkListFile, 1, Rock.Security.Authorization.EDIT, true,
                Rock.SystemGuid.Group.GROUP_STAFF_MEMBERS, Rock.Model.SpecialRole.None,
                LinkListGuids.AuthLinkListFileEditStaff );
            RockMigrationHelper.AddSecurityAuthForBinaryFileType(
                LinkListGuids.BinaryFileTypeLinkListFile, 2, Rock.Security.Authorization.EDIT, true,
                Rock.SystemGuid.Group.GROUP_STAFF_LIKE_MEMBERS, Rock.Model.SpecialRole.None,
                LinkListGuids.AuthLinkListFileEditStaffLike );
            RockMigrationHelper.AddSecurityAuthForBinaryFileType(
                LinkListGuids.BinaryFileTypeLinkListFile, 3, Rock.Security.Authorization.EDIT, false,
                null, Rock.Model.SpecialRole.AllUsers,
                LinkListGuids.AuthLinkListFileEditDenyAll );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileEditDenyAll );
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileEditStaffLike );
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileEditStaff );
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileEditAdmins );
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileViewDenyAll );
            RockMigrationHelper.DeleteSecurityAuth( LinkListGuids.AuthLinkListFileViewAdmins );

            // Only drop the type when nothing references it; uploaded files are content,
            // not schema, and must not disappear with a rollback.
            Sql( $@"
DECLARE @TypeId INT = (SELECT TOP 1 [Id] FROM [BinaryFileType] WHERE [Guid] = '{LinkListGuids.BinaryFileTypeLinkListFile}');
IF @TypeId IS NOT NULL AND NOT EXISTS ( SELECT 1 FROM [BinaryFile] WHERE [BinaryFileTypeId] = @TypeId )
BEGIN
    DELETE av FROM [AttributeValue] av
        JOIN [Attribute] a ON a.[Id] = av.[AttributeId]
        WHERE av.[EntityId] = @TypeId
          AND a.[EntityTypeId] = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.BinaryFileType');
    DELETE FROM [BinaryFileType] WHERE [Id] = @TypeId;
END
" );
        }
    }
}
