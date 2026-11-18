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

namespace org.secc.Imaging.Migrations
{
    using Rock.Plugin;

    /// <summary>
    /// ROCK-8759. The Update Person Image block ran whatever SQL was typed into it, and anyone
    /// with Administrate on any page could add it, so its .ascx files were removed. This deletes
    /// what Rock registered for it: the block type (matched by path, because block type Guids
    /// are generated per environment), its blocks with their security rules, and its Execution
    /// Delay attribute (values cascade).
    ///
    /// It also deletes the production page "Binary File To Person Photo" (Admin Tools > Power
    /// Tools), which held only that block, along with the page's security rules. The page is
    /// left alone if anything else has since been added to it or placed under it.
    ///
    /// Every statement is guarded by an existence check, so a re-run is a no-op.
    /// </summary>
    [MigrationNumber( 4, "1.16.0" )]
    public class _004_RemoveUpdatePersonImageBlock : Migration
    {
        public override void Up()
        {
            Sql( @"
                DECLARE @BlockEntityTypeId int = ( SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.Block' )
                DECLARE @PageEntityTypeId int = ( SELECT [Id] FROM [EntityType] WHERE [Name] = 'Rock.Model.Page' )
                DECLARE @BlockTypeId int = ( SELECT [Id] FROM [BlockType] WHERE [Path] = '~/Plugins/org_secc/Imaging/UpdatePersonImage.ascx' )

                IF @BlockTypeId IS NOT NULL
                BEGIN
                    DELETE [Attribute]
                    WHERE [EntityTypeId] = @BlockEntityTypeId
                        AND [EntityTypeQualifierColumn] = 'BlockTypeId'
                        AND [EntityTypeQualifierValue] = CAST( @BlockTypeId AS nvarchar(200) )

                    DELETE [Auth]
                    WHERE [EntityTypeId] = @BlockEntityTypeId
                        AND [EntityId] IN ( SELECT [Id] FROM [Block] WHERE [BlockTypeId] = @BlockTypeId )

                    DELETE [Block] WHERE [BlockTypeId] = @BlockTypeId

                    DELETE [BlockType] WHERE [Id] = @BlockTypeId
                END

                DECLARE @PageId int = ( SELECT [Id] FROM [Page] WHERE [Guid] = '93B62B37-1CF9-470B-AFA3-F53B0267B28C' )

                IF @PageId IS NOT NULL
                    AND NOT EXISTS ( SELECT 1 FROM [Block] WHERE [PageId] = @PageId )
                    AND NOT EXISTS ( SELECT 1 FROM [Page] WHERE [ParentPageId] = @PageId )
                BEGIN
                    DELETE [Auth] WHERE [EntityTypeId] = @PageEntityTypeId AND [EntityId] = @PageId

                    DELETE [Page] WHERE [Id] = @PageId
                END
" );
        }

        public override void Down()
        {
        }
    }
}
