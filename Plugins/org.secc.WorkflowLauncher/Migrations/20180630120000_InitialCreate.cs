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
namespace org.secc.WorkflowLauncher.Migrations
{
    using Rock.Plugin;
    [MigrationNumber( 1, "1.7.0" )]
    public partial class InitialCreate : Rock.Plugin.Migration
    {
        public override void Up()
        {

            // Page: Workflow Launcher              
            RockMigrationHelper.AddPage( true, "7F1F4130-CB98-473B-9DE1-7A886D2283ED", "D65F783D-87A9-4CC9-8110-E83466A0EADB","Workflow Launcher","","683D6BE3-A7DD-4833-89EB-735CEF3CA566","fa fa-rocket"); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Workflow Launcher", "Block for launching workflows for common entity types", "~/Plugins/org_secc/Workflow/WorkflowLauncher.ascx", "SECC > Workflow", "C2AB102F-E3B7-41BC-B0D0-6F7AF6E816B3" );
            // Add Block to Page: Workflow Launcher, Site: Rock RMS              
            RockMigrationHelper.AddBlock( true, "683D6BE3-A7DD-4833-89EB-735CEF3CA566","","C2AB102F-E3B7-41BC-B0D0-6F7AF6E816B3","Workflow Launcher","Main","","",0,"2EC7BC6B-312E-4EE4-AD64-42BDE04E448C");   
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( "2EC7BC6B-312E-4EE4-AD64-42BDE04E448C" );
            RockMigrationHelper.DeleteBlockType( "C2AB102F-E3B7-41BC-B0D0-6F7AF6E816B3" );
            RockMigrationHelper.DeletePage( "683D6BE3-A7DD-4833-89EB-735CEF3CA566" ); //  Page: Workflow Launcher
        }
    }
}
