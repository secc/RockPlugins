using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 7, "1.2.0" )]
    class Purchasing_PageData :Migration 
    {
        public override void Up()
        {
            // Page: Purchasing
            RockMigrationHelper.AddPage( "20F97A93-7949-4C2A-8A5E-C756FE8585CA", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Purchasing", "", "9532BAA3-333F-42DB-A1E0-1628192ED668", "fa fa-shopping-cart" ); // Site:Rock RMS
            // Page: Functions
            RockMigrationHelper.AddPage( "9532BAA3-333F-42DB-A1E0-1628192ED668", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Functions", "", "51D03BCC-7C6C-4912-9A92-4AB51A94367C", "" ); // Site:Rock RMS
            // Page: CERs
            RockMigrationHelper.AddPage( "51D03BCC-7C6C-4912-9A92-4AB51A94367C", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "CERs", "", "BB8C1AEE-7976-4241-8FF1-2AA16C43BD53", "fa fa-columns" ); // Site:Rock RMS
            // Page: Requisition List
            RockMigrationHelper.AddPage( "51D03BCC-7C6C-4912-9A92-4AB51A94367C", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Requisition List", "", "43145214-DA79-4F4F-8DC1-50E167C4F1C2", "" ); // Site:Rock RMS
            // Page: Purchase Orders
            RockMigrationHelper.AddPage( "51D03BCC-7C6C-4912-9A92-4AB51A94367C", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Purchase Orders", "", "D83FD22D-1007-479E-BDAA-8BA66629DC91", "" ); // Site:Rock RMS
            // Page: Payment Methods
            RockMigrationHelper.AddPage( "51D03BCC-7C6C-4912-9A92-4AB51A94367C", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Payment Methods", "", "39CB9E0A-20BF-4925-BBB2-2193795A1236", "fa fa-credit-card" ); // Site:Rock RMS
            // Page: Vendor List
            RockMigrationHelper.AddPage( "51D03BCC-7C6C-4912-9A92-4AB51A94367C", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Vendor List", "", "923C13D1-2576-494D-A627-60011184F564", "" ); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Capital Request List", "Lists all capital requests.", "~/Plugins/org_secc/Purchasing/CapitalRequestList.ascx", "Purchasing", "841A7C58-7902-4EA3-A2BE-668ACE74B491" );
            RockMigrationHelper.UpdateBlockType( "Requisition List", "Lists all requisitions.", "~/Plugins/org_secc/Purchasing/RequisitionList.ascx", "Purchasing", "FF7FBAAC-3203-4ADB-B084-C312885F4D39" );
            RockMigrationHelper.UpdateBlockType( "Vendor List", "List all vendors in the SECC Purchasing system.", "~/Plugins/org_secc/Purchasing/VendorList.ascx", "Purchasing", "FE8ED74E-F413-49D1-9A53-7CAB4A49A0ED" );
            RockMigrationHelper.UpdateBlockType( "Payment Method List", "", "~/Plugins/org_secc/Purchasing/PaymentMethodList.ascx", "", "74D9FA79-09E8-4E2F-982F-99B6B78481C9" );
            RockMigrationHelper.UpdateBlockType( "Purchase Order List", "Lists/filters all Purchase Orders.", "~/Plugins/org_secc/Purchasing/POList.ascx", "Purchasing", "4A706E99-57E5-40D3-A537-9DB86297B819" );
            RockMigrationHelper.AddBlock( "BB8C1AEE-7976-4241-8FF1-2AA16C43BD53", "", "841A7C58-7902-4EA3-A2BE-668ACE74B491", "Capital Request List", "Main", "", "", 0, "B3FB6642-97DD-4D4B-905A-078AE1B4B211" );

            RockMigrationHelper.AddBlock( "43145214-DA79-4F4F-8DC1-50E167C4F1C2", "", "FF7FBAAC-3203-4ADB-B084-C312885F4D39", "Requisition List", "Main", "", "", 0, "2E6F953A-370B-4DAF-BEAE-79FCEC6D6650" );

            RockMigrationHelper.AddBlock( "D83FD22D-1007-479E-BDAA-8BA66629DC91", "", "4A706E99-57E5-40D3-A537-9DB86297B819", "Purchase Order List", "Main", "", "", 0, "C0EFC36C-1EF3-464F-B68F-1918F944E50A" );

            RockMigrationHelper.AddBlock( "39CB9E0A-20BF-4925-BBB2-2193795A1236", "", "74D9FA79-09E8-4E2F-982F-99B6B78481C9", "Payment Method List", "Main", "", "", 0, "188E331A-4E5F-441B-800D-25C89B9FF46C" );

            RockMigrationHelper.AddBlock( "923C13D1-2576-494D-A627-60011184F564", "", "FE8ED74E-F413-49D1-9A53-7CAB4A49A0ED", "Vendor List", "Main", "", "", 0, "C61C2FC6-E901-4FB6-9AEF-52037DF5AC88" );

            RockMigrationHelper.AddBlockTypeAttribute( "841A7C58-7902-4EA3-A2BE-668ACE74B491", "BC48720C-3610-4BCF-AE66-D255A17F1CDF", "Location Lookup Type", "LocationLookupType", "", "The lookup Type that contains the Location lookup values. If no value is selected, the Location filter will not be available.", 0, @"", "34A4CB96-2244-43BF-B803-FDCAF63443A7" );

            RockMigrationHelper.AddBlockTypeAttribute( "841A7C58-7902-4EA3-A2BE-668ACE74B491", "BC48720C-3610-4BCF-AE66-D255A17F1CDF", "Ministry Area Lookup Type", "MinistryAreaLookupType", "", "The Lookup Type that contains the ministry lookup values.", 0, @"", "BD789BB9-98DA-465F-9297-42886BFD75DB" );

            RockMigrationHelper.AddBlockTypeAttribute( "841A7C58-7902-4EA3-A2BE-668ACE74B491", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Position Attribute", "PositionAttribute", "", "Position Attribute ID. Default is 29.", 0, @"", "CBA9FF6A-AA75-46AA-8964-194A65ABF7F6" );

            RockMigrationHelper.AddBlockTypeAttribute( "841A7C58-7902-4EA3-A2BE-668ACE74B491", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Capital Request Detail Page", "CapitalRequestDetailPage", "", "Page that shows the details of a selected capital request.", 0, @"", "7C946B4E-F86E-4A51-9CBB-2D6F68BC4F7B" );

            RockMigrationHelper.AddBlockTypeAttribute( "FF7FBAAC-3203-4ADB-B084-C312885F4D39", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Position Person Attribute", "PositionPersonAttribute", "", "The person attribute that stores the user's job position.", 0, @"", "111C5F22-E914-4256-AEAB-484836A62335" );

            RockMigrationHelper.AddBlockTypeAttribute( "FF7FBAAC-3203-4ADB-B084-C312885F4D39", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Requisition Detail Page", "RequisitionDetailPage", "", "Requisition Detail Page.", 0, @"", "4C168206-D7BA-4B39-98BA-344B1F1B28B8" );

            RockMigrationHelper.AddBlockTypeAttribute( "FF7FBAAC-3203-4ADB-B084-C312885F4D39", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Ministry Location Person Attribute", "MinistryLocationPersonAttribute", "", "The person attribute that stores the user's Location.", 0, @"", "7EA0B3FA-3718-44AC-B881-CD5F3084009B" );

            RockMigrationHelper.AddBlockTypeAttribute( "FF7FBAAC-3203-4ADB-B084-C312885F4D39", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Ministry Area Person Attribute", "MinistryAreaPersonAttribute", "", "The person attribute that stores the user's Ministry Area.", 0, @"", "6C26C67E-AA62-4F4B-9F1C-D26A9431B302" );

            RockMigrationHelper.AddBlockTypeAttribute( "FE8ED74E-F413-49D1-9A53-7CAB4A49A0ED", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Vendor Detail Page", "VendorDetailPage", "", "Vendor Detail Page", 0, @"", "40934307-CA97-46D7-A33A-C175147EE73C" );

            RockMigrationHelper.AddBlockTypeAttribute( "FE8ED74E-F413-49D1-9A53-7CAB4A49A0ED", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active Only By Default", "ActiveOnlyByDefault", "", "Only show active vendors by default.", 0, @"True", "C69C830F-4878-4FB3-A030-CA912C5A5001" );

            RockMigrationHelper.AddBlockTypeAttribute( "74D9FA79-09E8-4E2F-982F-99B6B78481C9", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Active By Default", "ShowActiveByDefault", "", "Show only active payment methods by default.", 0, @"True", "161A88A1-880E-4AFF-89E4-AFA082977186" );

            RockMigrationHelper.AddBlockTypeAttribute( "74D9FA79-09E8-4E2F-982F-99B6B78481C9", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Credit Card Expriation Date Year Options", "CreditCardExpriationDateYearOptions", "", "The number of years in the future to show for possible credit card expiration dates when adding a credit card payment method. Default is 5", 0, @"5", "2DAE9907-3238-47C5-B0C4-ABBACA7117A2" );

            RockMigrationHelper.AddBlockTypeAttribute( "4A706E99-57E5-40D3-A537-9DB86297B819", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Purchase Order Detail Page", "PurchaseOrderDetailPage", "", "Purchase Order Detail Page", 0, @"", "97EA5B3E-DED0-4C93-9A02-5B3D2638D965" );

            RockMigrationHelper.AddBlockTypeAttribute( "4A706E99-57E5-40D3-A537-9DB86297B819", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Ministry Area Person Attribute", "MinistryAreaPersonAttribute", "", "The person attribute that stores the user's Ministry Area.", 0, @"", "D0750BA0-623D-42BA-9C55-F034FEFA9ACC" );

            RockMigrationHelper.AddBlockTypeAttribute( "4A706E99-57E5-40D3-A537-9DB86297B819", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Position Person Attribute", "PositionPersonAttribute", "", "The person attribute that stores the user's job position.", 0, @"", "72E12555-186F-4CEB-8809-CC52841F4ADE" );

            RockMigrationHelper.AddBlockAttributeValue( "B3FB6642-97DD-4D4B-905A-078AE1B4B211", "34A4CB96-2244-43BF-B803-FDCAF63443A7", @"2E68D37C-FB7B-4AA5-9E09-3785D52156CB" ); // Location Lookup Type

            RockMigrationHelper.AddBlockAttributeValue( "B3FB6642-97DD-4D4B-905A-078AE1B4B211", "BD789BB9-98DA-465F-9297-42886BFD75DB", @"BBA23298-F3E8-4477-8DD9-7CC8DF01AE7B" ); // Ministry Area Lookup Type

            RockMigrationHelper.AddBlockAttributeValue( "B3FB6642-97DD-4D4B-905A-078AE1B4B211", "CBA9FF6A-AA75-46AA-8964-194A65ABF7F6", @"" ); // Position Attribute

            RockMigrationHelper.AddBlockAttributeValue( "B3FB6642-97DD-4D4B-905A-078AE1B4B211", "7C946B4E-F86E-4A51-9CBB-2D6F68BC4F7B", @"b7f23423-9efb-401e-a5f3-01364f2ccffd" ); // Capital Request Detail Page

            RockMigrationHelper.AddBlockAttributeValue( "B3FB6642-97DD-4D4B-905A-078AE1B4B211", "2CC8DA00-AA35-4DF7-AB65-07B726AC7F48", @"False" ); // Active

            RockMigrationHelper.AddBlockAttributeValue( "2E6F953A-370B-4DAF-BEAE-79FCEC6D6650", "111C5F22-E914-4256-AEAB-484836A62335", @"" ); // Position Person Attribute

            RockMigrationHelper.AddBlockAttributeValue( "2E6F953A-370B-4DAF-BEAE-79FCEC6D6650", "4C168206-D7BA-4B39-98BA-344B1F1B28B8", @"235e3d1d-653d-4ee3-bb8c-eb7bf3c01da9" ); // Requisition Detail Page

            RockMigrationHelper.AddBlockAttributeValue( "2E6F953A-370B-4DAF-BEAE-79FCEC6D6650", "7EA0B3FA-3718-44AC-B881-CD5F3084009B", @"" ); // Ministry Location Person Attribute

            RockMigrationHelper.AddBlockAttributeValue( "2E6F953A-370B-4DAF-BEAE-79FCEC6D6650", "6C26C67E-AA62-4F4B-9F1C-D26A9431B302", @"" ); // Ministry Area Person Attribute

            RockMigrationHelper.AddBlockAttributeValue( "C61C2FC6-E901-4FB6-9AEF-52037DF5AC88", "40934307-CA97-46D7-A33A-C175147EE73C", @"2976f862-dd14-4796-875f-b5a69b36bf35" ); // Vendor Detail Page

            RockMigrationHelper.AddBlockAttributeValue( "C61C2FC6-E901-4FB6-9AEF-52037DF5AC88", "C69C830F-4878-4FB3-A030-CA912C5A5001", @"True" ); // Active Only By Default

            RockMigrationHelper.AddBlockAttributeValue( "C0EFC36C-1EF3-464F-B68F-1918F944E50A", "97EA5B3E-DED0-4C93-9A02-5B3D2638D965", @"22c9ccce-aac9-4b75-a2d0-98bec21f4c82" ); // Purchase Order Detail Page

            RockMigrationHelper.AddBlockAttributeValue( "C0EFC36C-1EF3-464F-B68F-1918F944E50A", "D0750BA0-623D-42BA-9C55-F034FEFA9ACC", @"52a74582-5911-4f89-a80f-29a486f228ba" ); // Ministry Area Person Attribute

            RockMigrationHelper.AddBlockAttributeValue( "C0EFC36C-1EF3-464F-B68F-1918F944E50A", "72E12555-186F-4CEB-8809-CC52841F4ADE", @"19f67b7e-d568-4d0d-8953-00adf4aadb49" ); // Position Person Attribute

        }
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "72E12555-186F-4CEB-8809-CC52841F4ADE" );
            RockMigrationHelper.DeleteAttribute( "D0750BA0-623D-42BA-9C55-F034FEFA9ACC" );
            RockMigrationHelper.DeleteAttribute( "97EA5B3E-DED0-4C93-9A02-5B3D2638D965" );
            RockMigrationHelper.DeleteAttribute( "2DAE9907-3238-47C5-B0C4-ABBACA7117A2" );
            RockMigrationHelper.DeleteAttribute( "161A88A1-880E-4AFF-89E4-AFA082977186" );
            RockMigrationHelper.DeleteAttribute( "C69C830F-4878-4FB3-A030-CA912C5A5001" );
            RockMigrationHelper.DeleteAttribute( "40934307-CA97-46D7-A33A-C175147EE73C" );
            RockMigrationHelper.DeleteAttribute( "6C26C67E-AA62-4F4B-9F1C-D26A9431B302" );
            RockMigrationHelper.DeleteAttribute( "7EA0B3FA-3718-44AC-B881-CD5F3084009B" );
            RockMigrationHelper.DeleteAttribute( "4C168206-D7BA-4B39-98BA-344B1F1B28B8" );
            RockMigrationHelper.DeleteAttribute( "111C5F22-E914-4256-AEAB-484836A62335" );
            RockMigrationHelper.DeleteAttribute( "7C946B4E-F86E-4A51-9CBB-2D6F68BC4F7B" );
            RockMigrationHelper.DeleteAttribute( "CBA9FF6A-AA75-46AA-8964-194A65ABF7F6" );
            RockMigrationHelper.DeleteAttribute( "BD789BB9-98DA-465F-9297-42886BFD75DB" );
            RockMigrationHelper.DeleteAttribute( "34A4CB96-2244-43BF-B803-FDCAF63443A7" );
            RockMigrationHelper.DeleteBlock( "C0EFC36C-1EF3-464F-B68F-1918F944E50A" );
            RockMigrationHelper.DeleteBlock( "188E331A-4E5F-441B-800D-25C89B9FF46C" );
            RockMigrationHelper.DeleteBlock( "C61C2FC6-E901-4FB6-9AEF-52037DF5AC88" );
            RockMigrationHelper.DeleteBlock( "2E6F953A-370B-4DAF-BEAE-79FCEC6D6650" );
            RockMigrationHelper.DeleteBlock( "B3FB6642-97DD-4D4B-905A-078AE1B4B211" );
            RockMigrationHelper.DeletePage( "9532BAA3-333F-42DB-A1E0-1628192ED668" ); //  Page: Purchasing
            RockMigrationHelper.DeletePage( "51D03BCC-7C6C-4912-9A92-4AB51A94367C" ); //  Page: Functions
            RockMigrationHelper.DeletePage( "BB8C1AEE-7976-4241-8FF1-2AA16C43BD53" ); //  Page: CERs
            RockMigrationHelper.DeletePage( "43145214-DA79-4F4F-8DC1-50E167C4F1C2" ); //  Page: Requisition List
            RockMigrationHelper.DeletePage( "D83FD22D-1007-479E-BDAA-8BA66629DC91" ); //  Page: Purchase Orders
            RockMigrationHelper.DeletePage( "39CB9E0A-20BF-4925-BBB2-2193795A1236" ); //  Page: Payment Methods
            RockMigrationHelper.DeletePage( "923C13D1-2576-494D-A627-60011184F564" ); //  Page: Vendor List
        }
    }
}
