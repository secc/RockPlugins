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
    /// WS12: adds four org-wide attributes to the single LinkList ContentChannel
    /// (EntityType Rock.Model.ContentChannel, qualified by ContentChannelTypeId so
    /// they surface only on our channel — the lone channel of that type):
    ///   GlobalHeaderContent (Html), GlobalHeaderActive (Boolean),
    ///   GlobalFooterContent (Html), GlobalFooterActive (Boolean).
    /// These are NOT per-list — they are an org-wide singleton edited from the
    /// admin List block and shown on every list. All default empty/false.
    /// Idempotent, GUID-guarded, with Down().
    /// </summary>
    [MigrationNumber( 8, "1.16.0" )]
    public class AddGlobalHeaderFooter : Migration
    {
        private const string FieldTypeBoolean = "1EDAFDED-DFE6-4334-B019-6EECBA89E05A";
        private const string FieldTypeHtml = "DD7ED4C0-A9E0-434F-ACFE-BD4F56B043DF";

        public override void Up()
        {
            UpsertChannelAttribute(
                LinkListGuids.ChannelAttributeGlobalHeaderContent, FieldTypeHtml,
                LinkListGuids.ChannelAttributeKey.GlobalHeaderContent, "Global Header Content",
                "Org-wide HTML rendered above every Link List (when active).", 200, string.Empty );
            UpsertChannelAttribute(
                LinkListGuids.ChannelAttributeGlobalHeaderActive, FieldTypeBoolean,
                LinkListGuids.ChannelAttributeKey.GlobalHeaderActive, "Global Header Active",
                "When on, the global header is shown on every Link List.", 201, "False" );
            UpsertChannelAttribute(
                LinkListGuids.ChannelAttributeGlobalFooterContent, FieldTypeHtml,
                LinkListGuids.ChannelAttributeKey.GlobalFooterContent, "Global Footer Content",
                "Org-wide HTML rendered below every Link List (when active).", 202, string.Empty );
            UpsertChannelAttribute(
                LinkListGuids.ChannelAttributeGlobalFooterActive, FieldTypeBoolean,
                LinkListGuids.ChannelAttributeKey.GlobalFooterActive, "Global Footer Active",
                "When on, the global footer is shown on every Link List.", 203, "False" );
        }

        public override void Down()
        {
            DeleteAttributeByGuid( LinkListGuids.ChannelAttributeGlobalHeaderContent );
            DeleteAttributeByGuid( LinkListGuids.ChannelAttributeGlobalHeaderActive );
            DeleteAttributeByGuid( LinkListGuids.ChannelAttributeGlobalFooterContent );
            DeleteAttributeByGuid( LinkListGuids.ChannelAttributeGlobalFooterActive );
        }

        // ----------------------------------------------------------------------
        // Helpers (idempotent, GUID-scoped).
        // ----------------------------------------------------------------------

        private void UpsertChannelAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order, string defaultValue )
        {
            // ContentChannel attributes are qualified by ContentChannelTypeId (the
            // attribute applies to channels of that type). Our type has a single
            // channel, so this scopes the attributes to it.
            Sql( $@"
DECLARE @ContentChannelTypeId INT = (SELECT TOP 1 [Id] FROM [ContentChannelType] WHERE [Guid] = '{LinkListGuids.LinkListChannelType}');
DECLARE @FieldTypeId INT = (SELECT TOP 1 [Id] FROM [FieldType] WHERE [Guid] = '{fieldTypeGuid}');
DECLARE @EntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.ContentChannel');

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
        '{key}', '{Escape( name )}', '{Escape( description )}', {order}, 0,
        '{Escape( defaultValue )}', 0, 0, '{attributeGuid}',
        GETDATE(), GETDATE(), '{Escape( name )}' );
END
ELSE
BEGIN
    UPDATE [Attribute]
       SET [FieldTypeId] = @FieldTypeId, [EntityTypeId] = @EntityTypeId,
           [EntityTypeQualifierColumn] = 'ContentChannelTypeId',
           [EntityTypeQualifierValue] = CAST(@ContentChannelTypeId AS NVARCHAR(50)),
           [Key] = '{key}', [Name] = '{Escape( name )}', [Description] = '{Escape( description )}',
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
