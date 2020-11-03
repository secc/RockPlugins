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
    using System.Collections.Generic;
    using org.secc.Rise.Utilities;
    using Rock.Plugin;

    [MigrationNumber( 3, "1.10.2" )]
    public partial class PersonAttribute : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddOrUpdatePersonAttributeByGuid( Rock.SystemGuid.FieldType.TEXT, new List<string>(), "Rise Id", "Rise Id",
                Constants.PERSON_ATTRIBUTE_KEY_RISEID, "fa fa-chalkboard", "The user's id in Rise.", 0, "", Constants.PERSON_ATTRIBUTE_RISEID );
        }


        public override void Down()
        {

        }
    }
}
