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
    /// ROCK-7164: creates the Interactions plumbing for Link List analytics -
    /// a "Link List" medium (DefinedValue under the core Interaction Channel
    /// Medium defined type) and the single "Link Lists" InteractionChannel.
    /// One InteractionComponent per list is auto-created at record time by
    /// <see cref="Services.LinkListInteractionService"/>; interactions are
    /// Operation = "View" (list visits) and "Click" (per-link clicks).
    ///
    /// ChannelEntityId is set to the LinkList ContentChannel so the runtime's
    /// self-healing fallback lookup (medium + channel entity) resolves to THIS
    /// row instead of auto-creating a duplicate channel.
    ///
    /// RetentionDuration (365 days; Rock's cleanup job prunes older rows) is
    /// set on INSERT ONLY so an admin's later change survives re-runs.
    ///
    /// Idempotent and GUID-guarded; Down() removes interactions, components,
    /// the channel, and the medium defined value.
    /// </summary>
    [MigrationNumber( 11, "1.16.0" )]
    public class AddInteractionChannel : Migration
    {
        public override void Up()
        {
            Sql( $@"
DECLARE @MediumDefinedTypeId INT = (SELECT TOP 1 [Id] FROM [DefinedType]
    WHERE [Guid] = '9BF5777A-961F-49A8-A834-45E5C2077967'); -- core: Interaction Channel Medium
IF @MediumDefinedTypeId IS NULL RETURN;

IF NOT EXISTS ( SELECT 1 FROM [DefinedValue] WHERE [Guid] = '{LinkListGuids.InteractionMediumLinkList}' )
BEGIN
    DECLARE @Order INT = (SELECT ISNULL( MAX( [Order] ), -1 ) + 1 FROM [DefinedValue] WHERE [DefinedTypeId] = @MediumDefinedTypeId);
    INSERT INTO [DefinedValue] (
        [IsSystem], [DefinedTypeId], [Order], [Value], [Description],
        [Guid], [CreatedDateTime], [ModifiedDateTime], [IsActive] )
    VALUES (
        0, @MediumDefinedTypeId, @Order, 'Link List',
        'Page views and link clicks on SECC Link Lists.',
        '{LinkListGuids.InteractionMediumLinkList}', GETDATE(), GETDATE(), 1 );
END

DECLARE @MediumValueId INT = (SELECT TOP 1 [Id] FROM [DefinedValue] WHERE [Guid] = '{LinkListGuids.InteractionMediumLinkList}');
DECLARE @ContentChannelId INT = (SELECT TOP 1 [Id] FROM [ContentChannel] WHERE [Guid] = '{LinkListGuids.LinkListChannel}');
DECLARE @CciEntityTypeId INT = (SELECT TOP 1 [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.ContentChannelItem');

IF NOT EXISTS ( SELECT 1 FROM [InteractionChannel] WHERE [Guid] = '{LinkListGuids.InteractionChannelLinkLists}' )
BEGIN
    INSERT INTO [InteractionChannel] (
        [Name], [ChannelTypeMediumValueId], [ChannelEntityId], [ComponentEntityTypeId],
        [RetentionDuration], [UsesSession], [IsActive],
        [Guid], [CreatedDateTime], [ModifiedDateTime] )
    VALUES (
        'Link Lists', @MediumValueId, @ContentChannelId, @CciEntityTypeId,
        365, 0, 1,
        '{LinkListGuids.InteractionChannelLinkLists}', GETDATE(), GETDATE() );
END
" );
        }

        public override void Down()
        {
            Sql( $@"
DECLARE @ChannelId INT = (SELECT TOP 1 [Id] FROM [InteractionChannel] WHERE [Guid] = '{LinkListGuids.InteractionChannelLinkLists}');
IF @ChannelId IS NOT NULL
BEGIN
    DELETE i FROM [Interaction] i
        JOIN [InteractionComponent] ic ON ic.[Id] = i.[InteractionComponentId]
        WHERE ic.[InteractionChannelId] = @ChannelId;
    DELETE FROM [InteractionComponent] WHERE [InteractionChannelId] = @ChannelId;
    DELETE FROM [InteractionChannel] WHERE [Id] = @ChannelId;
END

DELETE FROM [DefinedValue] WHERE [Guid] = '{LinkListGuids.InteractionMediumLinkList}';
" );
        }
    }
}
