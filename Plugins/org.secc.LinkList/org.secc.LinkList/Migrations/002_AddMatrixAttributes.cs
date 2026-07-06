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
    /// Adds matrix-row attributes (ItemType, IndentLevel, IsSectionCollapsed) to the
    /// LinkList Attribute Matrix Template, and adds a channel-scoped IsPublic
    /// attribute to ContentChannelItem.
    ///
    /// Idempotent: safe to run multiple times. Legacy matrix attributes
    /// (URL, LinkText, Target, AdditionalClasses) are NEVER touched.
    /// </summary>
    [MigrationNumber( 2, "1.16.0" )]
    public class AddMatrixAttributes : Migration
    {
        // Rock well-known FieldType GUIDs
        private const string FieldTypeBoolean = "1EDAFDED-DFE6-4334-B019-6EECBA89E05A";
        private const string FieldTypeInteger = "A75DFC58-7A1B-4799-BF31-451B2BBE38FF";
        private const string FieldTypeSingleSelect = "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0";

        public override void Up()
        {
            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeItemType,
                FieldTypeSingleSelect,
                LinkListGuids.MatrixAttributeKey.ItemType,
                "Item Type",
                "Whether this row is a Link, Section header, or Separator.",
                order: 10,
                defaultValue: LinkListGuids.ItemTypeValue.Link );

            // Qualifiers for the SingleSelect field (values + control type).
            UpsertAttributeQualifier(
                LinkListGuids.MatrixAttributeItemType,
                "values",
                "link^Link,section^Section Header,separator^Separator",
                "9F4FA1FC-8D77-4DBE-9D5B-DC03A2C0AAEC" );
            UpsertAttributeQualifier(
                LinkListGuids.MatrixAttributeItemType,
                "fieldtype",
                "ddl",
                "8C26B3DB-1CD7-4D6F-A20E-A52E47AB2DB4" );

            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeIndentLevel,
                FieldTypeInteger,
                LinkListGuids.MatrixAttributeKey.IndentLevel,
                "Indent Level",
                "Visual nesting depth (0 = top level).",
                order: 11,
                defaultValue: "0" );

            UpsertMatrixAttribute(
                LinkListGuids.MatrixAttributeIsSectionCollapsed,
                FieldTypeBoolean,
                LinkListGuids.MatrixAttributeKey.IsSectionCollapsed,
                "Is Section Collapsed",
                "When the row is a Section, controls whether it renders collapsed by default.",
                order: 12,
                defaultValue: "False" );

            // ContentChannelItem-level IsPublic attribute, qualified to the LinkList channel.
            UpsertChannelItemAttribute(
                LinkListGuids.ItemAttributeIsPublic,
                FieldTypeBoolean,
                LinkListGuids.ItemAttributeKey.IsPublic,
                "Is Public",
                "When false, the list is hidden from the public REST endpoint and external embeds.",
                order: 100,
                defaultValue: "True" );
        }

        public override void Down()
        {
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeItemType );
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeIndentLevel );
            DeleteAttributeByGuid( LinkListGuids.MatrixAttributeIsSectionCollapsed );
            DeleteAttributeByGuid( LinkListGuids.ItemAttributeIsPublic );
        }

        // ----------------------------------------------------------------------
        // Helpers - all idempotent, all scoped by GUID.
        // ----------------------------------------------------------------------

        private void UpsertMatrixAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order, string defaultValue )
        {
            // Matrix-row attributes live on Rock.Model.AttributeMatrixItem and are
            // qualified by AttributeMatrixTemplateId = (id of our template GUID).
            Sql( $@"
DECLARE @TemplateId INT = (SELECT TOP 1 [Id] FROM [AttributeMatrixTemplate] WHERE [Guid] = '{LinkListGuids.LinkListMatrixTemplate}');
DECLARE @FieldTypeId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.AttributeMatrixItem');

IF @TemplateId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL
BEGIN
    -- Required dependencies missing; abort silently rather than corrupt state.
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

        private void UpsertChannelItemAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order, string defaultValue )
        {
            Sql( $@"
DECLARE @ChannelId INT = (SELECT TOP 1 [Id] FROM [ContentChannel] WHERE [Guid] = '{LinkListGuids.LinkListChannel}');
DECLARE @FieldTypeId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.ContentChannelItem');

IF @ChannelId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL
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
        '{Escape( defaultValue )}', 0, 0, '{attributeGuid}',
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
           [DefaultValue] = '{Escape( defaultValue )}',
           [ModifiedDateTime] = GETDATE()
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

