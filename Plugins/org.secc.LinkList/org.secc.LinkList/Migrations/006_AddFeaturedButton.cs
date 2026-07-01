using org.secc.LinkList.SystemGuids;

using Rock.Plugin;

namespace org.secc.LinkList.Migrations
{
    /// <summary>
    /// WS10: Featured button support.
    ///  - Matrix Boolean attribute IsFeatured on link rows (max one per list,
    ///    enforced in the service + UI; hoisted to the top at render time).
    ///  - Per-list ContentChannelType item attributes FeaturedButtonColor /
    ///    FeaturedButtonTextColor (Color), riding the WS3 override-wins model.
    ///  - FeaturedButtonColor / FeaturedButtonTextColor attributes on the
    ///    "Link List Design" Defined Type, seeded for all four presets.
    ///
    /// All new attributes default empty/false, so existing lists are unaffected
    /// (no list has a featured link until an editor sets one). Idempotent,
    /// GUID-guarded, with Down().
    /// </summary>
    [MigrationNumber( 6, "1.16.0" )]
    public class AddFeaturedButton : Migration
    {
        private const string FieldTypeBoolean = "1EDAFDED-DFE6-4334-B019-6EECBA89E05A";
        private const string FieldTypeColor = "D747E6AE-C383-4E22-8846-71518E3DD06F";

        public override void Up()
        {
            // 1) Matrix-row IsFeatured (link rows).
            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeIsFeatured,
                FieldTypeBoolean,
                LinkListGuids.MatrixAttributeKey.IsFeatured,
                "Is Featured",
                "When set on a Link row, it renders as the list's featured button at the top (max one per list).",
                order: 15,
                defaultValue: "False" );

            // 2) Per-list featured-button colors (ContentChannelType item attrs).
            UpsertContentChannelTypeItemAttribute(
                LinkListGuids.TypeAttributeFeaturedButtonColor, FieldTypeColor,
                LinkListGuids.TypeAttributeKey.FeaturedButtonColor, "Featured Button Color", 13, string.Empty );
            UpsertContentChannelTypeItemAttribute(
                LinkListGuids.TypeAttributeFeaturedButtonTextColor, FieldTypeColor,
                LinkListGuids.TypeAttributeKey.FeaturedButtonTextColor, "Featured Button Text Color", 14, string.Empty );

            // 3) Featured-button colors on the Design Defined Type.
            UpsertDefinedTypeAttribute(
                LinkListGuids.DesignAttributeFeaturedButtonColor, FieldTypeColor,
                LinkListGuids.DesignAttributeKey.FeaturedButtonColor, "Featured Button Color",
                "Background color for the featured button.", order: 4 );
            UpsertDefinedTypeAttribute(
                LinkListGuids.DesignAttributeFeaturedButtonTextColor, FieldTypeColor,
                LinkListGuids.DesignAttributeKey.FeaturedButtonTextColor, "Featured Button Text Color",
                "Foreground color for the featured button.", order: 5 );

            // 4) Seed featured colors for every existing preset (distinct accents
            //    so a featured button stands out from normal buttons; Creative can
            //    retune in the WS7 design pass).
            SeedFeatured( LinkListGuids.DesignSeccDefault, featured: "#ff6b35", featuredText: "#ffffff" );
            SeedFeatured( LinkListGuids.DesignSeccMove, featured: "#ffffff", featuredText: "#0c1116" );
            SeedFeatured( LinkListGuids.DesignLightAiry, featured: "#f59e0b", featuredText: "#1f2937" );
            SeedFeatured( LinkListGuids.DesignHighContrast, featured: "#1d4ed8", featuredText: "#ffffff" );
        }

        public override void Down()
        {
            DeleteAttributeValuesForDesignAttribute( LinkListGuids.DesignAttributeFeaturedButtonColor );
            DeleteAttributeValuesForDesignAttribute( LinkListGuids.DesignAttributeFeaturedButtonTextColor );
            DeleteAttributeByGuid( LinkListGuids.DesignAttributeFeaturedButtonColor );
            DeleteAttributeByGuid( LinkListGuids.DesignAttributeFeaturedButtonTextColor );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeFeaturedButtonColor );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeFeaturedButtonTextColor );
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeIsFeatured );
        }

        // ----------------------------------------------------------------------
        // Helpers (idempotent, GUID-scoped; mirror 001/002/003).
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

        private void SeedFeatured( string definedValueGuid, string featured, string featuredText )
        {
            UpsertDefinedValueAttributeValue( definedValueGuid, LinkListGuids.DesignAttributeFeaturedButtonColor, featured );
            UpsertDefinedValueAttributeValue( definedValueGuid, LinkListGuids.DesignAttributeFeaturedButtonTextColor, featuredText );
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

        private void DeleteAttributeValuesForDesignAttribute( string attributeGuid )
        {
            Sql( $@"
DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{attributeGuid}');
IF @AttributeId IS NULL RETURN;
DELETE FROM [AttributeValue] WHERE [AttributeId] = @AttributeId;
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
