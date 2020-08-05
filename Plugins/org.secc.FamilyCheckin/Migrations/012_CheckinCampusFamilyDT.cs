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
namespace org.secc.FamilyCheckin.Migrations
{
    using Rock.Plugin;
    using org.secc.DevLib.Extensions.Migration;
    using org.secc.FamilyCheckin.Utilities;
    using System;

    [MigrationNumber( 12, "1.10.2" )]
    public partial class CheckinCampusFamilyDT : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType( "Check-in",
                "Nested Campuses", "Describes campuses that are nested within other campuses so that attendance and mobile check-in can work correctly.",
                Constants.DEFINED_TYPE_NESTED_CAMPUSES );
            RockMigrationHelper.AddDefinedTypeAttribute( Constants.DEFINED_TYPE_NESTED_CAMPUSES, Rock.SystemGuid.FieldType.CAMPUS,
                "Parent Campus", Constants.DEFINED_VALUE_ATTRIBUTE_PARENT_CAMPUS, "The check-in campus must match this for the process to be applied.", 0, true, "",
                 false, true, "CE0BF4A2-2837-4A60-8FC9-417199507546" );
            RockMigrationHelper.AddDefinedTypeAttribute( Constants.DEFINED_TYPE_NESTED_CAMPUSES, Rock.SystemGuid.FieldType.CAMPUS,
            "Child Campus", Constants.DEFINED_VALUE_ATTRIBUTE_CHILD_CAMPUS, "The family campus must match this for the process to be applied.", 0, true, "",
             false, true, "9CDBFA64-63D0-40FE-93DD-09EA78E7BFB0" );
        }

        public override void Down()
        {

        }
    }
}
