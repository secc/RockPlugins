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
    /// WS7 fix 7: adds a dedicated Title color, independent of the body/content
    /// text color.
    ///  - Per-list ContentChannelType item attribute TitleColor (Color).
    ///  - TitleColor attribute on the Link List Design Defined Type, seeded for
    ///    all four presets (= each preset's content color, so nothing changes
    ///    visually until an editor sets a distinct title color).
    /// Empty on legacy lists → the viewer falls back to the content text color
    /// (resolved in BuildBag), so existing lists render identically. Idempotent,
    /// GUID-guarded, with Down().
    /// </summary>
    [MigrationNumber( 10, "1.16.0" )]
    public class AddTitleColor : Migration
    {
        private const string FieldTypeColor = "D747E6AE-C383-4E22-8846-71518E3DD06F";

        public override void Up()
        {
            UpsertContentChannelTypeItemAttribute(
                LinkListGuids.TypeAttributeTitleColor, FieldTypeColor,
                LinkListGuids.TypeAttributeKey.TitleColor, "Title Color", 15, string.Empty );

            UpsertDefinedTypeAttribute(
                LinkListGuids.DesignAttributeTitleColor, FieldTypeColor,
                LinkListGuids.DesignAttributeKey.TitleColor, "Title Color",
                "Color for the list title (falls back to the content text color when unset).", order: 6 );

            // Seed each preset's title color = its content color (no visual change).
            UpsertDefinedValueAttributeValue( LinkListGuids.DesignSeccDefault, LinkListGuids.DesignAttributeTitleColor, "#1a1a1a" );
            UpsertDefinedValueAttributeValue( LinkListGuids.DesignSeccMove, LinkListGuids.DesignAttributeTitleColor, "#ffffff" );
            UpsertDefinedValueAttributeValue( LinkListGuids.DesignLightAiry, LinkListGuids.DesignAttributeTitleColor, "#1f2937" );
            UpsertDefinedValueAttributeValue( LinkListGuids.DesignHighContrast, LinkListGuids.DesignAttributeTitleColor, "#000000" );
        }

        public override void Down()
        {
            Sql( $@"
DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{LinkListGuids.DesignAttributeTitleColor}');
IF @AttributeId IS NOT NULL DELETE FROM [AttributeValue] WHERE [AttributeId] = @AttributeId;
" );
            DeleteAttributeByGuid( LinkListGuids.DesignAttributeTitleColor );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeTitleColor );
        }

        // ----------------------------------------------------------------------
        // Helpers (idempotent, GUID-scoped; mirror 001/003/006).
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

        private void UpsertDefinedTypeAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order )
        {
            Sql( $@"
DECLARE @DefinedTypeId INT = (SELECT TOP 1 [Id] FROM [DefinedType] WHERE [Guid] = '{LinkListGuids.DefinedTypeLinkListDesign}');
DECLARE @FieldTypeId INT  = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.DefinedValue');

IF @DefinedTypeId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL RETURN;

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
        'DefinedTypeId', CAST(@DefinedTypeId AS NVARCHAR(50)),
        '{key}', '{Escape( name )}', '{Escape( description )}', {order}, 0,
        '', 0, 0, '{attributeGuid}',
        GETDATE(), GETDATE(), '{Escape( name )}' );
END
ELSE
BEGIN
    UPDATE [Attribute]
       SET [FieldTypeId] = @FieldTypeId, [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'DefinedTypeId',
           [EntityTypeQualifierValue]  = CAST(@DefinedTypeId AS NVARCHAR(50)),
           [Key] = '{key}', [Name] = '{Escape( name )}', [Description] = '{Escape( description )}',
           [Order] = {order}, [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{attributeGuid}';
END
" );
        }

        private void UpsertDefinedValueAttributeValue( string definedValueGuid, string attributeGuid, string value )
        {
            Sql( $@"
DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{attributeGuid}');
DECLARE @EntityId    INT = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = '{definedValueGuid}');

IF @AttributeId IS NULL OR @EntityId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [AttributeValue] WHERE [AttributeId] = @AttributeId AND [EntityId] = @EntityId )
BEGIN
    INSERT INTO [AttributeValue] ( [IsSystem], [AttributeId], [EntityId], [Value], [Guid], [CreatedDateTime], [ModifiedDateTime] )
    VALUES ( 0, @AttributeId, @EntityId, '{Escape( value )}', NEWID(), GETDATE(), GETDATE() );
END
ELSE
BEGIN
    UPDATE [AttributeValue]
       SET [Value] = '{Escape( value )}', [ModifiedDateTime] = GETDATE()
     WHERE [AttributeId] = @AttributeId AND [EntityId] = @EntityId;
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
