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
namespace org.secc.Cms.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.13.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            var addColumnSql = @"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.Columns WHERE TABLE_NAME='HtmlContent' and COLUMN_NAME = 'NAME')
                BEGIN
                    ALTER TABLE dbo.HtmlContent     
                    ADD Name NVARCHAR(100) NULL
                END 
            ";
            Sql( addColumnSql );

        }

        public override void Down()
        {
            var dropColumnSql = @"
                IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.Columns WHERE TABLE_NAME='HtmlContent' and COLUMN_NAME = 'NAME')
                BEGIN
                    ALTER TABLE dbo.HtmlContent     
                    DROP COLUMN Name
                END               
            ";

            Sql( dropColumnSql );
        }
    }
}
