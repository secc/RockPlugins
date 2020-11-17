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

namespace org.secc.Rise.Migrations
{
    using org.secc.DevLib.Extensions.Migration;
    using Rock.Plugin;
    [MigrationNumber( 1, "1.10.2" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Rise_Course",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Url = c.String( maxLength: 200 ),
                    CourseId = c.String( maxLength: 200 ),
                    Name = c.String(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.Url )
                .Index( t => t.CourseId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );

            CreateTable(
                "dbo._org_secc_Rise_CourseEnrolledGroup",
                c => new
                {
                    CourseId = c.Int( nullable: false ),
                    GroupId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.CourseId, t.GroupId } )
                .ForeignKey( "dbo._org_secc_Rise_Course", t => t.CourseId, cascadeDelete: true )
                .ForeignKey( "dbo.Group", t => t.GroupId, cascadeDelete: true )
                .Index( t => t.CourseId )
                .Index( t => t.GroupId )
                .Run( this );

            CreateTable(
                "dbo._org_secc_Rise_CourseCategory",
                c => new
                {
                    CourseId = c.Int( nullable: false ),
                    CategoryId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.CourseId, t.CategoryId } )
                .ForeignKey( "dbo._org_secc_Rise_Course", t => t.CourseId, cascadeDelete: true )
                .ForeignKey( "dbo.Category", t => t.CategoryId, cascadeDelete: true )
                .Index( t => t.CourseId )
                .Index( t => t.CategoryId )
                .Run( this );
        }



        public override void Down()
        {

        }
    }
}
