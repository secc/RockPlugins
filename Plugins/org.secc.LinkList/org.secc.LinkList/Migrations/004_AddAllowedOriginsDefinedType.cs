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
using org.secc.LinkList.SystemGuids;

using Rock.Plugin;

namespace org.secc.LinkList.Migrations
{
    /// <summary>
    /// Creates the admin-managed "Link List Allowed Origins" Defined Type and
    /// seeds it from the plugin's hardcoded AllowedOrigins array. The REST
    /// controller unions these (read from cache) with the hardcoded fallback for
    /// its CORS allowlist, so admins can add/remove embed origins from
    /// Admin Tools &gt; General Settings &gt; Defined Types without a deploy.
    ///
    /// Idempotent and GUID-guarded; Down() removes the type and its values.
    /// </summary>
    [MigrationNumber( 4, "1.16.0" )]
    public class AddAllowedOriginsDefinedType : Migration
    {
        public override void Up()
        {
            UpsertDefinedType(
                LinkListGuids.DefinedTypeLinkListAllowedOrigins,
                category: "SECC",
                name: "Link List Allowed Origins",
                description: "Origins (scheme://host[:port]) allowed to embed the Link List web component and call the public REST endpoint via CORS. Unioned with the plugin's hardcoded fallback list." );

            foreach ( var origin in LinkListGuids.AllowedOrigins )
            {
                SeedOriginValue( origin );
            }
        }

        public override void Down()
        {
            Sql( $@"
DECLARE @DefinedTypeId INT = (SELECT TOP 1 [Id] FROM [DefinedType] WHERE [Guid] = '{LinkListGuids.DefinedTypeLinkListAllowedOrigins}');
IF @DefinedTypeId IS NOT NULL
BEGIN
    DELETE FROM [DefinedValue] WHERE [DefinedTypeId] = @DefinedTypeId;
    DELETE FROM [DefinedType] WHERE [Id] = @DefinedTypeId;
END
" );
        }

        // Same shape as Migration 003's defined-type upsert: Rock 1.16 uses
        // [CategoryId] (FK), not a [Category] string. Resolve by name; NULL when
        // the category doesn't exist (uncategorized) rather than fail.
        private void UpsertDefinedType( string typeGuid, string category, string name, string description )
        {
            Sql( $@"
DECLARE @FieldTypeTextId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '9C204CD0-1233-41C5-818A-C5DA439445AA'); -- Text
DECLARE @CategoryId INT = (
    SELECT TOP 1 c.[Id] FROM [Category] c
    JOIN [EntityType] et ON et.[Id] = c.[EntityTypeId] AND et.[Name] = 'Rock.Model.DefinedType'
    WHERE c.[Name] = '{Escape( category )}' );

IF NOT EXISTS ( SELECT 1 FROM [DefinedType] WHERE [Guid] = '{typeGuid}' )
BEGIN
    INSERT INTO [DefinedType] (
        [IsSystem], [FieldTypeId], [Order], [CategoryId], [Name], [Description],
        [Guid], [CreatedDateTime], [ModifiedDateTime], [HelpText] )
    VALUES (
        0, @FieldTypeTextId, 0, @CategoryId, '{Escape( name )}', '{Escape( description )}',
        '{typeGuid}', GETDATE(), GETDATE(), NULL );
END
ELSE
BEGIN
    UPDATE [DefinedType]
       SET [CategoryId]   = @CategoryId,
           [Name]         = '{Escape( name )}',
           [Description]  = '{Escape( description )}',
           [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{typeGuid}';
END
" );
        }

        // Idempotent by (DefinedTypeId, Value). Values are appended after any
        // origins an admin has already added.
        private void SeedOriginValue( string origin )
        {
            Sql( $@"
DECLARE @DefinedTypeId INT = (SELECT TOP 1 [Id] FROM [DefinedType] WHERE [Guid] = '{LinkListGuids.DefinedTypeLinkListAllowedOrigins}');
IF @DefinedTypeId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [DefinedValue] WHERE [DefinedTypeId] = @DefinedTypeId AND [Value] = '{Escape( origin )}' )
BEGIN
    DECLARE @Order INT = (SELECT ISNULL( MAX( [Order] ), -1 ) + 1 FROM [DefinedValue] WHERE [DefinedTypeId] = @DefinedTypeId);
    INSERT INTO [DefinedValue] (
        [IsSystem], [DefinedTypeId], [Order], [Value], [Description],
        [Guid], [CreatedDateTime], [ModifiedDateTime], [IsActive] )
    VALUES (
        0, @DefinedTypeId, @Order, '{Escape( origin )}', '',
        NEWID(), GETDATE(), GETDATE(), 1 );
END
" );
        }

        private static string Escape( string value )
        {
            return ( value ?? string.Empty ).Replace( "'", "''" );
        }
    }
}
