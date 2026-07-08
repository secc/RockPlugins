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
    /// WS7 (design): adds an org-wide UseIvyJournalFont Boolean attribute to the
    /// single LinkList ContentChannel (EntityType ContentChannel, qualified by
    /// ContentChannelTypeId). When on, the viewer loads the licensed IvyJournal
    /// serif (Adobe Fonts kit) for headings; off (default) uses the Cormorant
    /// Garamond / Georgia fallback so other-church installs render correctly
    /// without the per-domain Adobe license. Idempotent, GUID-guarded, with Down().
    /// </summary>
    [MigrationNumber( 9, "1.16.0" )]
    public class AddUseIvyJournalFont : Migration
    {
        private const string FieldTypeBoolean = "1EDAFDED-DFE6-4334-B019-6EECBA89E05A";

        public override void Up()
        {
            UpsertChannelAttribute(
                LinkListGuids.ChannelAttributeUseIvyJournalFont, FieldTypeBoolean,
                LinkListGuids.ChannelAttributeKey.UseIvyJournalFont, "Use IvyJournal Font",
                "When on, the viewer loads the IvyJournal serif (Adobe Fonts) for headings. Leave off unless the embed domains are licensed; the fallback is Cormorant Garamond / Georgia.",
                204, "False" );
        }

        public override void Down()
        {
            DeleteAttributeByGuid( LinkListGuids.ChannelAttributeUseIvyJournalFont );
        }

        private void UpsertChannelAttribute( string attributeGuid, string fieldTypeGuid, string key, string name, string description, int order, string defaultValue )
        {
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
