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
    /// WS9: adds two optional per-row matrix attributes to the LinkList
    /// Attribute Matrix Template:
    ///   - Subtitle (Text)  — secondary line under a link's text.
    ///   - Description (Memo) — blurb under a section heading.
    ///
    /// Both default empty, so the ~190 existing lists are unaffected. Mirrors
    /// 002's matrix-attribute upsert shape. Idempotent, GUID-guarded, with Down().
    /// Legacy matrix attributes (URL, LinkText, Target, AdditionalClasses) are
    /// NEVER touched.
    /// </summary>
    [MigrationNumber( 5, "1.16.0" )]
    public class AddSubtitleAndDescription : Migration
    {
        // Rock well-known FieldType GUIDs (verified against the live FieldType table).
        private const string FieldTypeText = "9C204CD0-1233-41C5-818A-C5DA439445AA";
        private const string FieldTypeMemo = "C28C7BF3-A552-4D77-9408-DEDCF760CED0";

        public override void Up()
        {
            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeSubtitle,
                FieldTypeText,
                LinkListGuids.MatrixAttributeKey.Subtitle,
                "Subtitle",
                "Optional secondary line shown under a link's text (link rows only).",
                order: 13,
                defaultValue: string.Empty );

            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeDescription,
                FieldTypeMemo,
                LinkListGuids.MatrixAttributeKey.Description,
                "Description",
                "Optional blurb shown under a section heading (section rows only).",
                order: 14,
                defaultValue: string.Empty );
        }

        public override void Down()
        {
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeSubtitle );
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeDescription );
        }

        // ----------------------------------------------------------------------
        // Helpers - idempotent, scoped by GUID (mirrors 002_AddMatrixAttributes).
        // ----------------------------------------------------------------------

        private void UpsertMatrixAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order, string defaultValue )
        {
            Sql( $@"
DECLARE @TemplateId INT = (SELECT TOP 1 [Id] FROM [AttributeMatrixTemplate] WHERE [Guid] = '{LinkListGuids.LinkListMatrixTemplate}');
DECLARE @FieldTypeId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.AttributeMatrixItem');

IF @TemplateId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL
BEGIN
    RETURN;
END

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
       SET [FieldTypeId] = @FieldTypeId,
           [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'AttributeMatrixTemplateId',
           [EntityTypeQualifierValue]  = CAST(@TemplateId AS NVARCHAR(50)),
           [Key]          = '{key}',
           [Name]         = '{Escape( name )}',
           [Description]  = '{Escape( description )}',
           [Order]        = {order},
           [DefaultValue] = '{Escape( defaultValue )}',
           [ModifiedDateTime] = GETDATE()
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
