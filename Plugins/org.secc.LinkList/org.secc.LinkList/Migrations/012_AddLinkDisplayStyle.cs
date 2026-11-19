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
    /// ROCK-9361: per-link display style.
    ///  - Matrix SingleSelect attribute DisplayStyle (button | card) on link rows.
    ///    Default "button", and a missing/blank value also reads as "button", so
    ///    the existing lists render exactly as before (no data backfill needed).
    ///  - Matrix Image attribute CardImage (BinaryFile guid) shown at the top of
    ///    a card. Ignored for button-style rows.
    ///
    /// A card reuses the row's existing LinkText (title) and Subtitle (shown as
    /// the card description), so no other new fields are needed. Idempotent,
    /// GUID-guarded, with Down(). Mirrors 002/006's matrix-attribute upsert shape.
    /// </summary>
    [MigrationNumber( 12, "1.16.0" )]
    public class AddLinkDisplayStyle : Migration
    {
        // Rock well-known FieldType GUIDs (same values 001/002/007 use).
        private const string FieldTypeSingleSelect = "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0";
        private const string FieldTypeImage = "97F8157D-A8C8-4AB3-96A2-9CB2A9049E6D";

        private const string QualifierDisplayStyleValues = "F5D51862-F374-4ABC-9F82-D7CDB2E06327";
        private const string QualifierDisplayStyleFieldType = "E8E404CC-2CAE-4EAB-8B4F-EFB064BEF7B9";

        public override void Up()
        {
            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeDisplayStyle,
                FieldTypeSingleSelect,
                LinkListGuids.MatrixAttributeKey.DisplayStyle,
                "Display Style",
                "How a Link row renders: a pill Button (default) or a Card (title, description, optional image).",
                order: 16,
                defaultValue: LinkListGuids.DisplayStyleValue.Button );

            UpsertAttributeQualifier(
                LinkListGuids.MatrixAttributeDisplayStyle,
                "values",
                "button^Button,card^Card",
                QualifierDisplayStyleValues );
            UpsertAttributeQualifier(
                LinkListGuids.MatrixAttributeDisplayStyle,
                "fieldtype",
                "ddl",
                QualifierDisplayStyleFieldType );

            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeCardImage,
                FieldTypeImage,
                LinkListGuids.MatrixAttributeKey.CardImage,
                "Card Image",
                "Optional image shown at the top of a Card-style link (ignored for buttons).",
                order: 17,
                defaultValue: string.Empty );
        }

        public override void Down()
        {
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeCardImage );
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeDisplayStyle );
        }

        // ----------------------------------------------------------------------
        // Helpers (idempotent, GUID-scoped; mirror 002/006).
        // ----------------------------------------------------------------------

        private void UpsertMatrixAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order, string defaultValue )
        {
            Sql( $@"
DECLARE @TemplateId INT = (SELECT TOP 1 [Id] FROM [AttributeMatrixTemplate] WHERE [Guid] = '{LinkListGuids.LinkListMatrixTemplate}');
DECLARE @FieldTypeId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.AttributeMatrixItem');

IF @TemplateId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL RETURN;

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
        'AttributeMatrixTemplateId', CAST(@TemplateId AS NVARCHAR(50)),
        '{key}', '{Escape( name )}', '{Escape( description )}', {order}, 0,
        '{Escape( defaultValue )}', 0, 0, '{attributeGuid}',
        GETDATE(), GETDATE(), '{Escape( name )}' );
END
ELSE
BEGIN
    UPDATE [Attribute]
       SET [FieldTypeId] = @FieldTypeId, [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'AttributeMatrixTemplateId',
           [EntityTypeQualifierValue]  = CAST(@TemplateId AS NVARCHAR(50)),
           [Key] = '{key}', [Name] = '{Escape( name )}', [Description] = '{Escape( description )}',
           [Order] = {order}, [DefaultValue] = '{Escape( defaultValue )}', [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{attributeGuid}';
END
" );
        }

        private void UpsertAttributeQualifier( string attributeGuid, string key, string value, string qualifierGuid )
        {
            Sql( $@"
DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{attributeGuid}');
IF @AttributeId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [AttributeQualifier] WHERE [AttributeId] = @AttributeId AND [Key] = '{key}' )
BEGIN
    INSERT INTO [AttributeQualifier] ( [IsSystem], [AttributeId], [Key], [Value], [Guid] )
    VALUES ( 0, @AttributeId, '{key}', '{Escape( value )}', '{qualifierGuid}' );
END
ELSE
BEGIN
    UPDATE [AttributeQualifier]
       SET [Value] = '{Escape( value )}'
     WHERE [AttributeId] = @AttributeId AND [Key] = '{key}';
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
