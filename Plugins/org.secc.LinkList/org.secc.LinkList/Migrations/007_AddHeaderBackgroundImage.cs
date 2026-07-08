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

using Rock.Plugin;

namespace org.secc.LinkList.Migrations
{
    /// <summary>
    /// WS11: adds a per-list HeaderBackgroundImage attribute (Image / BinaryFile)
    /// to the LinkList ContentChannelType — a full-width banner behind the header
    /// area only (distinct from the logo HeaderImage and the full-viewport
    /// BackgroundImage). Defaults empty, so existing lists are unaffected.
    /// Idempotent, GUID-guarded, with Down().
    /// </summary>
    [MigrationNumber( 7, "1.16.0" )]
    public class AddHeaderBackgroundImage : Migration
    {
        private const string FieldTypeImage = "97F8157D-A8C8-4AB3-96A2-9CB2A9049E6D";

        public override void Up()
        {
            UpsertContentChannelTypeItemAttribute(
                LinkListGuids.TypeAttributeHeaderBackgroundImage, FieldTypeImage,
                LinkListGuids.TypeAttributeKey.HeaderBackgroundImage, "Header Background Image", 16, string.Empty );
        }

        public override void Down()
        {
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeHeaderBackgroundImage );
        }

        // ----------------------------------------------------------------------
        // Helpers (idempotent, GUID-scoped; mirror 001).
        // ----------------------------------------------------------------------

        private void UpsertContentChannelTypeItemAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, int order, string defaultValue )
        {
            Sql( $@"
DECLARE @ContentChannelTypeId INT = (SELECT TOP 1 [Id] FROM [ContentChannelType] WHERE [Guid] = '{LinkListGuids.LinkListChannelType}');
DECLARE @FieldTypeId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.ContentChannelItem');

IF @ContentChannelTypeId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [Attribute] WHERE [Guid] = '{attributeGuid}' )
BEGIN
    INSERT INTO [Attribute] (
        [IsSystem], [FieldTypeId], [EntityTypeId],
        [EntityTypeQualifierColumn], [EntityTypeQualifierValue],
        [Key], [Name], [Description], [Order], [IsGridColumn],
        [DefaultValue], [IsMultiValue], [IsRequired], [Guid],
        [CreatedDateTime], [ModifiedDateTime], [AbbreviatedName] )
    VALUES (
        0, @FieldTypeId, @EntityTypeId,
        'ContentChannelTypeId', CAST(@ContentChannelTypeId AS NVARCHAR(50)),
        '{key}', '{Escape( name )}', '', {order}, 0,
        '{Escape( defaultValue )}', 0, 0, '{attributeGuid}',
        GETDATE(), GETDATE(), '{Escape( name )}' );
END
ELSE
BEGIN
    UPDATE [Attribute]
       SET [FieldTypeId] = @FieldTypeId, [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'ContentChannelTypeId',
           [EntityTypeQualifierValue] = CAST(@ContentChannelTypeId AS NVARCHAR(50)),
           [Key] = '{key}', [Name] = '{Escape( name )}', [Description] = '',
           [Order] = {order}, [DefaultValue] = '{Escape( defaultValue )}', [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{attributeGuid}';
END
" );
        }

        private void DeleteAttributeByGuid( string attributeGuid )
        {
            Sql( $@"
DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{attributeGuid}');
IF @AttributeId IS NULL RETURN;

DELETE FROM [AttributeValue] WHERE [AttributeId] = @AttributeId;
DELETE FROM [AttributeQualifier] WHERE [AttributeId] = @AttributeId;
DELETE FROM [Attribute] WHERE [Id] = @AttributeId;
" );
        }

        private static string Escape( string value )
        {
            return ( value ?? string.Empty ).Replace( "'", "''" );
        }
    }
}
