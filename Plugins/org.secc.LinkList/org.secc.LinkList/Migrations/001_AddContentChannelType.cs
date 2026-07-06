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
    /// Creates the Link List Content Channel Type using the production GUID and
    /// adds the legacy item attributes scoped to that type.
    /// </summary>
    [MigrationNumber( 1, "1.16.0" )]
    public class AddContentChannelType : Migration
    {
        private const string FieldTypeBoolean = "1EDAFDED-DFE6-4334-B019-6EECBA89E05A";
        private const string FieldTypeColor = "D747E6AE-C383-4E22-8846-71518E3DD06F";
        private const string FieldTypeHtml = "DD7ED4C0-A9E0-434F-ACFE-BD4F56B043DF";
        private const string FieldTypeImage = "97F8157D-A8C8-4AB3-96A2-9CB2A9049E6D";
        private const string FieldTypeMatrix = "F16FC460-DC1E-4821-9012-5F21F974C677";
        private const string FieldTypeText = "9C204CD0-1233-41C5-818A-C5DA439445AA";

        public override void Up()
        {
            UpsertContentChannelType();
            UpsertMatrixTemplate();
            UpsertContentChannel();

            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeContentTextColor, FieldTypeColor, LinkListGuids.TypeAttributeKey.ContentTextColor, "Content Text Color", 0, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeCustomTitle, FieldTypeText, LinkListGuids.TypeAttributeKey.CustomTitle, "Custom Title", 1, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeHideTitle, FieldTypeBoolean, LinkListGuids.TypeAttributeKey.HideTitle, "Hide Title?", 2, "False" );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeHeaderImage, FieldTypeImage, LinkListGuids.TypeAttributeKey.HeaderImage, "Header Image", 3, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeRoundHeaderImage, FieldTypeBoolean, LinkListGuids.TypeAttributeKey.RoundHeaderImage, "Round Header Image", 4, "True" );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeBackgroundImage, FieldTypeImage, LinkListGuids.TypeAttributeKey.BackgroundImage, "Background Image", 5, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeHeaderVideo, FieldTypeText, LinkListGuids.TypeAttributeKey.HeaderVideo, "Header Video (YouTube Video ID)", 6, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeHeaderVideoVimeoId, FieldTypeText, LinkListGuids.TypeAttributeKey.HeaderVideoVimeoId, "Header Video (Vimeo ID)", 7, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeBackgroundColor, FieldTypeColor, LinkListGuids.TypeAttributeKey.BackgroundColor, "Background Color", 8, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeButtonColor, FieldTypeColor, LinkListGuids.TypeAttributeKey.ButtonColor, "Button Color", 9, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeButtonTextColor, FieldTypeColor, LinkListGuids.TypeAttributeKey.ButtonTextColor, "Button Text Color", 10, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeLinkListMatrix, FieldTypeMatrix, LinkListGuids.TypeAttributeKey.LinkListMatrix, "Link List Matrix", 11, string.Empty );
            UpsertContentChannelTypeItemAttribute( LinkListGuids.TypeAttributeFooterContent, FieldTypeHtml, LinkListGuids.TypeAttributeKey.FooterContent, "Footer Content", 12, string.Empty );

            // Rock core MatrixFieldType reads the "attributematrixtemplate"
            // qualifier as the template *Id* (AsInteger), NOT the Guid. Storing
            // the Guid here breaks Rock's matrix editor and would corrupt the
            // legacy (Id-valued) qualifier on SECC's existing lists if re-run.
            UpsertMatrixTemplateQualifier( LinkListGuids.TypeAttributeLinkListMatrix, LinkListGuids.LinkListMatrixTemplate );
        }

        public override void Down()
        {
            DeleteContentChannelIfEmpty();

            DeleteAttributeByGuid( LinkListGuids.TypeAttributeContentTextColor );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeCustomTitle );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeHideTitle );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeHeaderImage );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeRoundHeaderImage );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeBackgroundImage );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeHeaderVideo );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeHeaderVideoVimeoId );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeBackgroundColor );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeButtonColor );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeButtonTextColor );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeLinkListMatrix );
            DeleteAttributeByGuid( LinkListGuids.TypeAttributeFooterContent );

            Sql( $@"
DECLARE @ContentChannelTypeId INT = (SELECT TOP 1 [Id] FROM [ContentChannelType] WHERE [Guid] = '{LinkListGuids.LinkListChannelType}');
IF @ContentChannelTypeId IS NOT NULL
    AND NOT EXISTS ( SELECT 1 FROM [ContentChannel] WHERE [ContentChannelTypeId] = @ContentChannelTypeId )
BEGIN
    DELETE FROM [ContentChannelType] WHERE [Id] = @ContentChannelTypeId;
END
" );
        }

        private void UpsertContentChannelType()
        {
            Sql( $@"
IF NOT EXISTS ( SELECT 1 FROM [ContentChannelType] WHERE [Guid] = '{LinkListGuids.LinkListChannelType}' )
BEGIN
    INSERT INTO [ContentChannelType] (
        [IsSystem], [Name], [DateRangeType], [IncludeTime],
        [DisablePriority], [DisableContentField], [DisableStatus], [ShowInChannelList],
        [Guid], [CreatedDateTime], [ModifiedDateTime] )
    VALUES (
        0, 'Link List', 0, 0,
        1, 1, 0, 1,
        '{LinkListGuids.LinkListChannelType}', GETDATE(), GETDATE() );
END
ELSE
BEGIN
    UPDATE [ContentChannelType]
       SET [Name] = 'Link List',
           [DateRangeType] = 0,
           [IncludeTime] = 0,
           [DisablePriority] = 1,
           [DisableContentField] = 1,
           [DisableStatus] = 0,
           [ShowInChannelList] = 1,
           [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{LinkListGuids.LinkListChannelType}';
END
" );
        }

        private void UpsertContentChannel()
        {
            Sql( $@"
DECLARE @ContentChannelTypeId INT = (SELECT TOP 1 [Id] FROM [ContentChannelType] WHERE [Guid] = '{LinkListGuids.LinkListChannelType}');
IF @ContentChannelTypeId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [ContentChannel] WHERE [Guid] = '{LinkListGuids.LinkListChannel}' )
BEGIN
    -- NOTE: ContentChannel has no [IsSystem] or [IsContentLibraryEnabled]
    -- column in Rock 1.16 (content-library config moved to
    -- [ContentLibraryConfigurationJson]). Including them fails the install.
    INSERT INTO [ContentChannel] (
        [ContentChannelTypeId], [Name], [Description], [IconCssClass],
        [RequiresApproval], [ItemsManuallyOrdered], [ChildItemsManuallyOrdered], [EnableRss],
        [ChannelUrl], [ItemUrl], [TimeToLive], [ContentControlType], [RootImageDirectory],
        [IsIndexEnabled], [IsTaggingEnabled], [IsStructuredContent], [EnablePersonalization],
        [Guid], [CreatedDateTime], [ModifiedDateTime] )
    VALUES (
        @ContentChannelTypeId, 'Link Lists', '', 'fa fa-list',
        0, 1, 0, 0,
        '', '', 60, 0, '',
        0, 0, 0, 0,
        '{LinkListGuids.LinkListChannel}', GETDATE(), GETDATE() );
END
ELSE
BEGIN
    UPDATE [ContentChannel]
       SET [ContentChannelTypeId] = @ContentChannelTypeId,
           [Name] = 'Link Lists',
           [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{LinkListGuids.LinkListChannel}';
END
" );
        }

        private void DeleteContentChannelIfEmpty()
        {
            Sql( $@"
DECLARE @ContentChannelId INT = (SELECT TOP 1 [Id] FROM [ContentChannel] WHERE [Guid] = '{LinkListGuids.LinkListChannel}');
IF @ContentChannelId IS NOT NULL
    AND NOT EXISTS ( SELECT 1 FROM [ContentChannelItem] WHERE [ContentChannelId] = @ContentChannelId )
BEGIN
    DELETE FROM [ContentChannel] WHERE [Id] = @ContentChannelId;
END
" );
        }

        private void UpsertMatrixTemplate()
        {
            Sql( $@"
IF NOT EXISTS ( SELECT 1 FROM [AttributeMatrixTemplate] WHERE [Guid] = '{LinkListGuids.LinkListMatrixTemplate}' )
BEGIN
    INSERT INTO [AttributeMatrixTemplate] (
        [Name], [Description], [IsActive], [MinimumRows], [MaximumRows], [FormattedLava],
        [Guid], [CreatedDateTime], [ModifiedDateTime] )
    VALUES (
        'Link List Matrix', 'Rows used by Link List content channel items.', 1, 0, NULL, NULL,
        '{LinkListGuids.LinkListMatrixTemplate}', GETDATE(), GETDATE() );
END
ELSE
BEGIN
    UPDATE [AttributeMatrixTemplate]
       SET [Name] = 'Link List Matrix',
           [Description] = 'Rows used by Link List content channel items.',
           [IsActive] = 1,
           [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{LinkListGuids.LinkListMatrixTemplate}';
END
" );
        }

        private void UpsertContentChannelTypeItemAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, int order, string defaultValue )
        {
            Sql( $@"
DECLARE @ContentChannelTypeId INT = (SELECT TOP 1 [Id] FROM [ContentChannelType] WHERE [Guid] = '{LinkListGuids.LinkListChannelType}');
DECLARE @FieldTypeId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.ContentChannelItem');

IF @ContentChannelTypeId IS NULL OR @FieldTypeId IS NULL OR @EntityTypeId IS NULL
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
        'ContentChannelTypeId', CAST(@ContentChannelTypeId AS NVARCHAR(50)),
        '{key}', '{Escape( name )}', '', {order}, 0,
        '{Escape( defaultValue )}', 0, 0, '{attributeGuid}',
        GETDATE(), GETDATE(), '{Escape( name )}' );
END
ELSE
BEGIN
    UPDATE [Attribute]
       SET [FieldTypeId] = @FieldTypeId,
           [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'ContentChannelTypeId',
           [EntityTypeQualifierValue] = CAST(@ContentChannelTypeId AS NVARCHAR(50)),
           [Key] = '{key}',
           [Name] = '{Escape( name )}',
           [Description] = '',
           [Order] = {order},
           [DefaultValue] = '{Escape( defaultValue )}',
           [ModifiedDateTime] = GETDATE()
     WHERE [Guid] = '{attributeGuid}';
END
" );
        }

        /// <summary>
        /// Sets the Matrix field's "attributematrixtemplate" qualifier to the
        /// template's integer Id (resolved from its Guid), which is what Rock
        /// core <c>MatrixFieldType</c> expects.
        /// </summary>
        private void UpsertMatrixTemplateQualifier( string attributeGuid, string templateGuid )
        {
            Sql( $@"
DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{attributeGuid}');
DECLARE @TemplateId INT = (SELECT TOP 1 [Id] FROM [AttributeMatrixTemplate] WHERE [Guid] = '{templateGuid}');
IF @AttributeId IS NULL OR @TemplateId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [AttributeQualifier] WHERE [AttributeId] = @AttributeId AND [Key] = 'attributematrixtemplate' )
BEGIN
    INSERT INTO [AttributeQualifier] ( [IsSystem], [AttributeId], [Key], [Value], [Guid] )
    VALUES ( 0, @AttributeId, 'attributematrixtemplate', CAST(@TemplateId AS NVARCHAR(50)), NEWID() );
END
ELSE
BEGIN
    UPDATE [AttributeQualifier]
       SET [Value] = CAST(@TemplateId AS NVARCHAR(50))
     WHERE [AttributeId] = @AttributeId AND [Key] = 'attributematrixtemplate';
END
" );
        }

        private void UpsertAttributeQualifier( string attributeGuid, string key, string value )
        {
            Sql( $@"
DECLARE @AttributeId INT = (SELECT TOP 1 [Id] FROM [Attribute] WHERE [Guid] = '{attributeGuid}');
IF @AttributeId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [AttributeQualifier] WHERE [AttributeId] = @AttributeId AND [Key] = '{key}' )
BEGIN
    INSERT INTO [AttributeQualifier] ( [IsSystem], [AttributeId], [Key], [Value], [Guid] )
    VALUES ( 0, @AttributeId, '{key}', '{Escape( value )}', NEWID() );
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