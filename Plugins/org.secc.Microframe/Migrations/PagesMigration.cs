using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Plugin;

namespace org.secc.Microframe.Migrations
{
    [MigrationNumber( 2, "1.7.0" )]
    class PagesMigration : Migration
    {
        public override void Up()
        {
            // Page: Manage                ///parent page                         //Layout                                                  //Page guid
            RockMigrationHelper.AddPage( "5B6DBC42-8B03-4D15-8D92-AAFA28FD8616", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Microframe", "", "43E0DBFF-A8CD-4E59-9B7A-A166C70892D7", "fa fa-wifi" ); // Add Page to Installed Plugins

            RockMigrationHelper.AddPage( "43E0DBFF-A8CD-4E59-9B7A-A166C70892D7", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Sign Categories", "", "C7F67450-B0A8-446B-AD97-0BA84037F7F4", "fa fa-server" ); // Site:Rock RMS

            RockMigrationHelper.AddPage( "C7F67450-B0A8-446B-AD97-0BA84037F7F4", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Sign Category Detail", "", "02F88460-9550-49BB-90AE-C1C8CA78D8F4", "" ); // Site:Rock RMS
                                                                                                                                                                                                   // Page: Sign Categories       
            RockMigrationHelper.AddPage( "43E0DBFF-A8CD-4E59-9B7A-A166C70892D7", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Signs", "", "CD80AF2A-EC8B-4232-8C31-CACFD3516008", "fa fa-wifi" ); // Site:Rock RMS
                                                                                                                                                                                              // Page: Codes              
            RockMigrationHelper.AddPage( "CD80AF2A-EC8B-4232-8C31-CACFD3516008", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Sign Detail", "", "34ECCA9D-FA20-444D-B90B-FBD3CFA96C43", "" ); // Site:Rock RMS
                                                                                                                                                                                          // Page: Sign Detail             
            RockMigrationHelper.AddPage( "43E0DBFF-A8CD-4E59-9B7A-A166C70892D7", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Microframe Pager", "", "3A160BC4-D599-490D-9D8C-9CE079449B38", "fa fa-comment-o" ); // Site:Rock RMS

            RockMigrationHelper.AddBlock( "43E0DBFF-A8CD-4E59-9B7A-A166C70892D7", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Main", "", "", 0, "5BD1106C-AA61-451F-A111-0CEA0A583525" );
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File  
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters  
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% include '~~/Assets/Lava/PageListAsBlocks.lava' %}" ); // Template  
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"1" ); // Number of Levels  
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString  
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block  
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "0CA0DDEF-48EF-4ABC-9822-A05E225DE26C", @"False" ); // Active  
            RockMigrationHelper.AddBlockAttributeValue( "5BD1106C-AA61-451F-A111-0CEA0A583525", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List  

            // Page: Sign Category Detail   
            RockMigrationHelper.UpdateBlockType( "Sign Category Detail", "Displays the details of the given sign category.", "~/Plugins/org_secc/Microframe/SignCategoryDetail.ascx", "SECC > Microframe", "F29D64BF-DC09-46C8-8C98-6F2BA281B686" );
            RockMigrationHelper.UpdateBlockType( "Sign Category List", "Lists all the sign categories.", "~/Plugins/org_secc/Microframe/SignCategoryList.ascx", "SECC > Microframe", "9D3256EF-9D39-4360-9EF8-AEB5372647BB" );
            RockMigrationHelper.UpdateBlockType( "Sign Code Manager", "Manages all of the codes sent out.", "~/Plugins/org_secc/Microframe/SignCodeManager.ascx", "SECC > Microframe", "84A28A57-5F62-4AAB-B219-1227E4EDB820" );
            RockMigrationHelper.UpdateBlockType( "Sign Detail", "Displays the details of the given sign.", "~/Plugins/org_secc/Microframe/SignDetail.ascx", "SECC > Microframe", "8914BBC7-909B-4C67-9B29-DD4DEFA975B6" );
            RockMigrationHelper.UpdateBlockType( "Sign List", "Lists all the Signs.", "~/Plugins/org_secc/Microframe/SignList.ascx", "SECC > Microframe", "611939AE-5017-4D43-8D40-D23C35A826C4" );
            RockMigrationHelper.AddBlock( "CD80AF2A-EC8B-4232-8C31-CACFD3516008", "", "611939AE-5017-4D43-8D40-D23C35A826C4", "Sign List", "Main", "", "", 0, "DF8197C2-FD4E-4929-A651-4D2D0B14EDEB" );
            RockMigrationHelper.AddBlock( "34ECCA9D-FA20-444D-B90B-FBD3CFA96C43", "", "8914BBC7-909B-4C67-9B29-DD4DEFA975B6", "Sign Detail", "Main", "", "", 0, "36BC9DA8-BB61-4ED1-B8F5-D30EC81C7FEB" );
            RockMigrationHelper.AddBlock( "C7F67450-B0A8-446B-AD97-0BA84037F7F4", "", "9D3256EF-9D39-4360-9EF8-AEB5372647BB", "Sign Category List", "Main", "", "", 0, "61B1C21C-CBAA-4908-92DB-A59A2DF88CCA" );
            RockMigrationHelper.AddBlock( "02F88460-9550-49BB-90AE-C1C8CA78D8F4", "", "F29D64BF-DC09-46C8-8C98-6F2BA281B686", "Sign Category Detail", "Main", "", "", 0, "2DC818F6-06FB-49D5-B6E6-5088F271D05B" );
            RockMigrationHelper.AddBlock( "3A160BC4-D599-490D-9D8C-9CE079449B38", "", "84A28A57-5F62-4AAB-B219-1227E4EDB820", "Sign Code Manager", "Main", "", "", 0, "0849CB29-6A5E-4108-94A8-E26CB23ACC8B" );
            RockMigrationHelper.AddBlock( "9DECF4F5-7BC6-42BD-A458-F03DF596958B", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Main", "", "", 0, "EBEC90B9-97D8-4CE7-8890-9688DD05210D" );
            RockMigrationHelper.AddBlockTypeAttribute( "611939AE-5017-4D43-8D40-D23C35A826C4", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "", 0, @"", "048CB669-406C-4234-8BA4-8D6A223BDE88" );
            RockMigrationHelper.AddBlockTypeAttribute( "9D3256EF-9D39-4360-9EF8-AEB5372647BB", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "", 0, @"", "8F0AA221-75C8-4BC6-B6F0-0055D4293B8D" );
            RockMigrationHelper.AddBlockTypeAttribute( "84A28A57-5F62-4AAB-B219-1227E4EDB820", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "", 0, @"", "48A3802F-BA7D-4AA8-B05A-38634B48D725" );
            RockMigrationHelper.AddBlockAttributeValue( "DF8197C2-FD4E-4929-A651-4D2D0B14EDEB", "048CB669-406C-4234-8BA4-8D6A223BDE88", @"34ecca9d-fa20-444d-b90b-fbd3cfa96c43" ); // Detail Page  
            RockMigrationHelper.AddBlockAttributeValue( "61B1C21C-CBAA-4908-92DB-A59A2DF88CCA", "8F0AA221-75C8-4BC6-B6F0-0055D4293B8D", @"02f88460-9550-49bb-90ae-c1c8ca78d8f4" ); // Detail Page  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% include '~~/Assets/Lava/PageListAsBlocks.lava' %}" ); // Template  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"1" ); // Number of Levels  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block  
            RockMigrationHelper.AddBlockAttributeValue( "EBEC90B9-97D8-4CE7-8890-9688DD05210D", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List  
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "48A3802F-BA7D-4AA8-B05A-38634B48D725" );
            RockMigrationHelper.DeleteAttribute( "8F0AA221-75C8-4BC6-B6F0-0055D4293B8D" );
            RockMigrationHelper.DeleteAttribute( "048CB669-406C-4234-8BA4-8D6A223BDE88" );
            RockMigrationHelper.DeleteAttribute( "0A49DABE-42EE-40E5-9E06-0E6530944865" );
            RockMigrationHelper.DeleteAttribute( "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2" );
            RockMigrationHelper.DeleteAttribute( "2EF904CD-976E-4489-8C18-9BA43885ACD9" );
            RockMigrationHelper.DeleteAttribute( "E4CF237D-1D12-4C93-AFD7-78EB296C4B69" );
            RockMigrationHelper.DeleteAttribute( "6C952052-BC79-41BA-8B88-AB8EA3E99648" );
            RockMigrationHelper.DeleteAttribute( "41F1C42E-2395-4063-BD4F-031DF8D5B231" );
            RockMigrationHelper.DeleteAttribute( "1322186A-862A-4CF1-B349-28ECB67229BA" );
            RockMigrationHelper.DeleteAttribute( "EEE71DDE-C6BC-489B-BAA5-1753E322F183" );
            RockMigrationHelper.DeleteAttribute( "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22" );
            RockMigrationHelper.DeleteBlock( "EBEC90B9-97D8-4CE7-8890-9688DD05210D" );
            RockMigrationHelper.DeleteBlock( "0849CB29-6A5E-4108-94A8-E26CB23ACC8B" );
            RockMigrationHelper.DeleteBlock( "61B1C21C-CBAA-4908-92DB-A59A2DF88CCA" );
            RockMigrationHelper.DeleteBlock( "DF8197C2-FD4E-4929-A651-4D2D0B14EDEB" );
            RockMigrationHelper.DeletePage( "7F048367-B80E-41ED-A8EF-CCE3E93F96BF" ); //  Page: Manage
            RockMigrationHelper.DeletePage( "9DECF4F5-7BC6-42BD-A458-F03DF596958B" ); //  Page: Microframe
            RockMigrationHelper.DeletePage( "CD80AF2A-EC8B-4232-8C31-CACFD3516008" ); //  Page: Signs
            RockMigrationHelper.DeletePage( "C7F67450-B0A8-446B-AD97-0BA84037F7F4" ); //  Page: Sign Categories
            RockMigrationHelper.DeletePage( "3A160BC4-D599-490D-9D8C-9CE079449B38" ); //  Page: Codes
        }
    }
}
