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
    /// Adds the "Link List Design" Defined Type with four Color attributes
    /// (ContentTextColor, BackgroundColor, ButtonColor, ButtonTextColor),
    /// seeds four starter presets, and adds a DesignId attribute on
    /// ContentChannelItem (DefinedValue field type, qualified to that Defined Type).
    ///
    /// Idempotent. Down() removes everything by GUID.
    /// </summary>
    [MigrationNumber( 3, "1.16.0" )]
    public class AddDesignPresets : Migration
    {
        // Rock well-known FieldType GUIDs
        private const string FieldTypeColor = "D747E6AE-C383-4E22-8846-71518E3DD06F";
        private const string FieldTypeDefinedValue = "59D5A94C-94A0-4630-B80A-BB25697D74C7";

        // Defined-value attribute qualifier GUIDs (per defined value × per attribute) - NOT
        // strictly required for AttributeQualifier table; we only need them for AttributeValue rows.

        public override void Up()
        {
            // 1) Defined Type
            UpsertDefinedType(
                LinkListGuids.DefinedTypeLinkListDesign,
                category: "SECC",
                name: "Link List Design",
                description: "Color presets used by the SECC Link List plugin. Each value's attribute values supply the colors applied to lists referencing this preset." );

            // 2) Defined-Type-scoped attributes (one per design field)
            UpsertDefinedTypeAttribute(
                LinkListGuids.DesignAttributeContentTextColor,
                FieldTypeColor,
                LinkListGuids.DesignAttributeKey.ContentTextColor,
                "Content Text Color",
                "Default text color used on the list body.",
                order: 0 );
            UpsertDefinedTypeAttribute(
                LinkListGuids.DesignAttributeBackgroundColor,
                FieldTypeColor,
                LinkListGuids.DesignAttributeKey.BackgroundColor,
                "Background Color",
                "Wrapper background color.",
                order: 1 );
            UpsertDefinedTypeAttribute(
                LinkListGuids.DesignAttributeButtonColor,
                FieldTypeColor,
                LinkListGuids.DesignAttributeKey.ButtonColor,
                "Button Color",
                "Background color for link buttons.",
                order: 2 );
            UpsertDefinedTypeAttribute(
                LinkListGuids.DesignAttributeButtonTextColor,
                FieldTypeColor,
                LinkListGuids.DesignAttributeKey.ButtonTextColor,
                "Button Text Color",
                "Foreground color for link buttons.",
                order: 3 );

            // 3) Seed presets
            SeedDesign( LinkListGuids.DesignSeccDefault, "SECC Default", "Clean dark text on a light background with a deep button color.", 0,
                content: "#1a1a1a", background: "#ffffff", button: "#0c1116", buttonText: "#ffffff" );
            SeedDesign( LinkListGuids.DesignSeccMove, "SECC Move", "Light text on dark background with an orange accent button (MOVE/campaign style).", 1,
                content: "#ffffff", background: "#0c1116", button: "#ff6b35", buttonText: "#ffffff" );
            SeedDesign( LinkListGuids.DesignLightAiry, "Light & Airy", "Soft warm-white background with a blue button.", 2,
                content: "#1f2937", background: "#f7f6f2", button: "#2563eb", buttonText: "#ffffff" );
            SeedDesign( LinkListGuids.DesignHighContrast, "High Contrast", "Maximum contrast for accessibility.", 3,
                content: "#000000", background: "#ffffff", button: "#000000", buttonText: "#ffffff" );

            // 4) DesignId attribute on ContentChannelItem (qualified to LinkList channel
            //    AND to the Link List Design defined type).
            UpsertChannelItemDefinedValueAttribute(
                LinkListGuids.ItemAttributeDesignId,
                LinkListGuids.ItemAttributeKey.DesignId,
                "Design",
                "Optional preset that supplies the list's color values. Manage presets under Admin Tools > General Settings > Defined Types > Link List Design.",
                order: 110 );
        }

        public override void Down()
        {
            // Item attribute first (so AttributeValue rows go away cleanly).
            DeleteAttributeByGuid( LinkListGuids.ItemAttributeDesignId );

            // Defined values (and their attribute values).
            DeleteDefinedValueByGuid( LinkListGuids.DesignSeccDefault );
            DeleteDefinedValueByGuid( LinkListGuids.DesignSeccMove );
            DeleteDefinedValueByGuid( LinkListGuids.DesignLightAiry );
            DeleteDefinedValueByGuid( LinkListGuids.DesignHighContrast );

            // Defined-type attributes.
            DeleteAttributeByGuid( LinkListGuids.DesignAttributeContentTextColor );
            DeleteAttributeByGuid( LinkListGuids.DesignAttributeBackgroundColor );
            DeleteAttributeByGuid( LinkListGuids.DesignAttributeButtonColor );
            DeleteAttributeByGuid( LinkListGuids.DesignAttributeButtonTextColor );

            // Defined type itself.
            Sql( $@"
DELETE FROM [DefinedType] WHERE [Guid] = '{LinkListGuids.DefinedTypeLinkListDesign}';
" );
        }

        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------

        private void UpsertDefinedType( string typeGuid, string category, string name, string description )
        {
            // Rock 1.16 DefinedType is categorized via [CategoryId] (FK to
            // Category, EntityType = Rock.Model.DefinedType), NOT a [Category]
            // string column. Resolve the category by name; leave NULL
            // (uncategorized) if it does not exist rather than fail the install.
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

        private void UpsertDefinedTypeAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order )
        {
            // Attributes on a DefinedValue row are qualified by DefinedTypeId.
            Sql( $@"
DECLARE @DefinedTypeId INT = (SELECT TOP 1 [Id] FROM [DefinedType] WHERE [Guid] = '{LinkListGuids.DefinedTypeLinkListDesign}');
DECLARE @FieldTypeId INT  = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.DefinedValue');

IF @DefinedTypeId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL
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
        'DefinedTypeId', CAST(@DefinedTypeId AS NVARCHAR(50)),
        '{key}', '{Escape( name )}', '{Escape( description )}', {order}, 0,
        '', 0, 0, '{attributeGuid}',
        GETDATE(), GETDATE(), '{Escape( name )}' );
END
ELSE
BEGIN
    UPDATE [Attribute]
       SET [FieldTypeId] = @FieldTypeId,
           [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'DefinedTypeId',
           [EntityTypeQualifierValue]  = CAST(@DefinedTypeId AS NVARCHAR(50)),
           [Key]          = '{key}',
           [Name]         = '{Escape( name )}',
           [Description]  = '{Escape( description )}',
           [Order]        = {order},
           [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{attributeGuid}';
END
" );
        }

        private void SeedDesign( string definedValueGuid, string name, string description, int order,
            string content, string background, string button, string buttonText )
        {
            Sql( $@"
DECLARE @DefinedTypeId INT = (SELECT TOP 1 [Id] FROM [DefinedType] WHERE [Guid] = '{LinkListGuids.DefinedTypeLinkListDesign}');
IF @DefinedTypeId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [DefinedValue] WHERE [Guid] = '{definedValueGuid}' )
BEGIN
    INSERT INTO [DefinedValue] (
        [IsSystem], [DefinedTypeId], [Order], [Value], [Description],
        [Guid], [CreatedDateTime], [ModifiedDateTime], [IsActive] )
    VALUES (
        0, @DefinedTypeId, {order}, '{Escape( name )}', '{Escape( description )}',
        '{definedValueGuid}', GETDATE(), GETDATE(), 1 );
END
ELSE
BEGIN
    UPDATE [DefinedValue]
       SET [Order]              = {order},
           [Value]              = '{Escape( name )}',
           [Description]        = '{Escape( description )}',
           [ModifiedDateTime]   = GETDATE()
     WHERE [Guid] = '{definedValueGuid}';
END
" );

            UpsertDefinedValueAttributeValue( definedValueGuid, LinkListGuids.DesignAttributeContentTextColor, content );
            UpsertDefinedValueAttributeValue( definedValueGuid, LinkListGuids.DesignAttributeBackgroundColor, background );
            UpsertDefinedValueAttributeValue( definedValueGuid, LinkListGuids.DesignAttributeButtonColor, button );
            UpsertDefinedValueAttributeValue( definedValueGuid, LinkListGuids.DesignAttributeButtonTextColor, buttonText );
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

        private void UpsertChannelItemDefinedValueAttribute( string attributeGuid, string key, string name, string description, int order )
        {
            // Same shape as the IsPublic upsert in 001, but adds the
            // 'definedtype' qualifier so Rock's DefinedValue field type knows
            // which Defined Type to surface in pickers.
            Sql( $@"
DECLARE @ChannelId INT     = (SELECT TOP 1 [Id] FROM [ContentChannel] WHERE [Guid] = '{LinkListGuids.LinkListChannel}');
DECLARE @FieldTypeId INT   = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{FieldTypeDefinedValue}');
DECLARE @EntityTypeId INT  = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.ContentChannelItem');
DECLARE @DefinedTypeId INT = (SELECT TOP 1 [Id] FROM [DefinedType] WHERE [Guid] = '{LinkListGuids.DefinedTypeLinkListDesign}');

IF @ChannelId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL OR @DefinedTypeId IS NULL
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
        'ContentChannelId', CAST(@ChannelId AS NVARCHAR(50)),
        '{key}', '{Escape( name )}', '{Escape( description )}', {order}, 0,
        '', 0, 0, '{attributeGuid}',
        GETDATE(), GETDATE(), '{Escape( name )}' );
END
ELSE
BEGIN
    UPDATE [Attribute]
       SET [FieldTypeId] = @FieldTypeId,
           [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'ContentChannelId',
           [EntityTypeQualifierValue]  = CAST(@ChannelId AS NVARCHAR(50)),
           [Key]          = '{key}',
           [Name]         = '{Escape( name )}',
           [Description]  = '{Escape( description )}',
           [Order]        = {order},
           [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{attributeGuid}';
END

DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{attributeGuid}');

-- definedtype qualifier: the Id of our Link List Design defined type
IF NOT EXISTS ( SELECT 1 FROM [AttributeQualifier] WHERE [AttributeId] = @AttributeId AND [Key] = 'definedtype' )
BEGIN
    INSERT INTO [AttributeQualifier] ( [IsSystem], [AttributeId], [Key], [Value], [Guid] )
    VALUES ( 0, @AttributeId, 'definedtype', CAST(@DefinedTypeId AS NVARCHAR(50)), NEWID() );
END
ELSE
BEGIN
    UPDATE [AttributeQualifier]
       SET [Value] = CAST(@DefinedTypeId AS NVARCHAR(50))
     WHERE [AttributeId] = @AttributeId AND [Key] = 'definedtype';
END

-- allowmultiple = False
IF NOT EXISTS ( SELECT 1 FROM [AttributeQualifier] WHERE [AttributeId] = @AttributeId AND [Key] = 'allowmultiple' )
BEGIN
    INSERT INTO [AttributeQualifier] ( [IsSystem], [AttributeId], [Key], [Value], [Guid] )
    VALUES ( 0, @AttributeId, 'allowmultiple', 'False', NEWID() );
END

-- displaydescription = False
IF NOT EXISTS ( SELECT 1 FROM [AttributeQualifier] WHERE [AttributeId] = @AttributeId AND [Key] = 'displaydescription' )
BEGIN
    INSERT INTO [AttributeQualifier] ( [IsSystem], [AttributeId], [Key], [Value], [Guid] )
    VALUES ( 0, @AttributeId, 'displaydescription', 'False', NEWID() );
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

        private void DeleteDefinedValueByGuid( string definedValueGuid )
        {
            Sql( $@"
DECLARE @Id INT = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = '{definedValueGuid}');
IF @Id IS NULL RETURN;

DELETE FROM [AttributeValue] WHERE [EntityId] = @Id
    AND [AttributeId] IN ( SELECT [Id] FROM [Attribute]
        WHERE [EntityTypeQualifierColumn] = 'DefinedTypeId'
        AND [Guid] IN (
            '{LinkListGuids.DesignAttributeContentTextColor}',
            '{LinkListGuids.DesignAttributeBackgroundColor}',
            '{LinkListGuids.DesignAttributeButtonColor}',
            '{LinkListGuids.DesignAttributeButtonTextColor}' ) );

DELETE FROM [DefinedValue] WHERE [Id] = @Id;
" );
        }

        private static string Escape( string value )
        {
            return ( value ?? string.Empty ).Replace( "'", "''" );
        }
    }
}
