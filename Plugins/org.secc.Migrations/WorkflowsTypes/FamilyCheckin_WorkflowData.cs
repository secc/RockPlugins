using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 1, "1.2.0" )]
    class FamilyCheckin_WorkflowData : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateEntityType( "Rock.Model.Workflow", "3540E9A7-FE30-43A9-8B0A-A372B63DFC93", true, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActivity", "2CB52ED0-CB06-4D62-9E2C-73B60AFA4C9F", true, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActionType", "23E3273A-B137-48A3-9AFF-C8DC832DDCA6", true, true );

            RockMigrationHelper.UpdateEntityType( "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit", "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", false, true );

            RockMigrationHelper.UpdateEntityType( "cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended", "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", false, true );

            RockMigrationHelper.UpdateEntityType( "org.secc.FamilyCheckin.CreateLabelsAggregate", "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", false, true );

            RockMigrationHelper.UpdateEntityType( "org.secc.FamilyCheckin.SelectAllPeople", "4406D2DE-398B-4439-9616-458454239BFF", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FilterActiveLocations", "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FilterGroupsByAge", "23F1E3FD-48AE-451F-9911-A5C7523A74B6", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FilterGroupsByGrade", "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FindFamilies", "E2F172A8-88E5-4F84-9805-73164516F5FB", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FindFamilyMembers", "5794B18B-8F43-43B2-8D60-6C047AB096AF", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FindRelationships", "F43099A7-E872-439B-A750-351C741BCFEF", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.LoadGroups", "008402A8-3A6C-4CB6-A230-6AD532505EDC", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.LoadGroupTypes", "50D5D915-074A-41FB-9EA7-0DBE52141398", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.LoadLocations", "4492E36A-77C8-4DC7-8128-570FAA161ADB", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.LoadSchedules", "24A7E196-B50B-4BD6-A347-07CFC5ABEF9E", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.RemoveEmptyGroups", "698115D4-7B5E-48F3-BBB0-C53A20193169", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.RemoveEmptyGroupTypes", "E998B9A7-31C9-46F6-B91C-4E5C3F06C82F", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.RemoveEmptyPeople", "B8B72812-190E-4802-A63F-E693344754BD", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.ReserveAttendance", "75F22BFE-0868-491C-89DF-AD0A08A03B1A", false, true );

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "008402A8-3A6C-4CB6-A230-6AD532505EDC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "AD7528AD-2A3D-4C26-B452-FA9F4F48953C" ); // Rock.Workflow.Action.CheckIn.LoadGroups:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "008402A8-3A6C-4CB6-A230-6AD532505EDC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Load All", "LoadAll", "By default groups are only loaded for the selected person, group type, and location.  Select this option to load groups for all the loaded people and group types.", 0, @"False", "39762EF0-91D5-4B13-BD34-FC3AC3C24897" ); // Rock.Workflow.Action.CheckIn.LoadGroups:Load All

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "008402A8-3A6C-4CB6-A230-6AD532505EDC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "C26C5959-7144-443B-88ED-28E4A5AE544C" ); // Rock.Workflow.Action.CheckIn.LoadGroups:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "6F98731B-1C17-49F0-8B5C-1C7DBDB08A07" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByAge:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Age Required", "AgeRequired", "Select 'Yes' if groups with an age filter should be removed/excluded when person does not have an age.", 1, @"True", "EEC0F601-BE2C-4C22-8E6B-190CFEA34879" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByAge:Age Required

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove", "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", 0, @"True", "F05781E2-3517-4D20-A3BB-DA56CA025F25" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByAge:Remove

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Group Age Range Attribute", "GroupAgeRangeAttribute", "Select the attribute used to define the age range of the group", 2, @"43511B8F-71D9-423A-85BF-D1CD08C1998E", "688B346E-EAA6-4DD9-9B67-C3AABECADF7D" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByAge:Group Age Range Attribute

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "23F1E3FD-48AE-451F-9911-A5C7523A74B6", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "554108CF-31A1-47C0-A184-18B4A881D7FD" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByAge:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "24A7E196-B50B-4BD6-A347-07CFC5ABEF9E", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "4DFA9F8D-F2E6-4040-A23B-2A1F8258C767" ); // Rock.Workflow.Action.CheckIn.LoadSchedules:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "24A7E196-B50B-4BD6-A347-07CFC5ABEF9E", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Load All", "LoadAll", "By default schedules are only loaded for the selected person, group type, location, and group.  Select this option to load schedules for all the loaded people, group types, locations, and groups.", 0, @"False", "B222CAF2-DF12-433C-B5D4-A8DB95B60207" ); // Rock.Workflow.Action.CheckIn.LoadSchedules:Load All

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "24A7E196-B50B-4BD6-A347-07CFC5ABEF9E", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "F7B09469-EB3D-44A4-AB8E-C74318BD4669" ); // Rock.Workflow.Action.CheckIn.LoadSchedules:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4406D2DE-398B-4439-9616-458454239BFF", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "2DC87EA3-AA59-4C32-A45C-2155255CAD92" ); // org.secc.FamilyCheckin.SelectAllPeople:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4406D2DE-398B-4439-9616-458454239BFF", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "4E1B3B40-0F2D-4DC4-905C-3EDCE8226692" ); // org.secc.FamilyCheckin.SelectAllPeople:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4492E36A-77C8-4DC7-8128-570FAA161ADB", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "2F3B6B42-A89C-443A-9008-E9E96535E815" ); // Rock.Workflow.Action.CheckIn.LoadLocations:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4492E36A-77C8-4DC7-8128-570FAA161ADB", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Load All", "LoadAll", "By default locations are only loaded for the selected person and group type.  Select this option to load locations for all the loaded people and group types.", 0, @"False", "70203A96-AE70-47AD-A086-FD84792DF2B6" ); // Rock.Workflow.Action.CheckIn.LoadLocations:Load All

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4492E36A-77C8-4DC7-8128-570FAA161ADB", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "6EE6128C-79BF-4333-85DB-3B0C92B27131" ); // Rock.Workflow.Action.CheckIn.LoadLocations:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "50D5D915-074A-41FB-9EA7-0DBE52141398", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "1C7CD28E-ACC5-4B88-BC05-E02D72919305" ); // Rock.Workflow.Action.CheckIn.LoadGroupTypes:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "50D5D915-074A-41FB-9EA7-0DBE52141398", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "1F4BD3F6-C528-4160-8478-825C3B8AC85D" ); // Rock.Workflow.Action.CheckIn.LoadGroupTypes:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "E69E09E8-C71D-4AA3-86AD-D46DBAE60816" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByGrade:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove", "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", 0, @"True", "39F82A06-DD0E-4D51-A364-8D6C0EB32BC4" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByGrade:Remove

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "78A19926-CABA-475D-8AB9-F9EB08BD6FAA" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByGrade:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "5794B18B-8F43-43B2-8D60-6C047AB096AF", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "3EF34D41-030B-411F-9D18-D331ABD89F0D" ); // Rock.Workflow.Action.CheckIn.FindFamilyMembers:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "5794B18B-8F43-43B2-8D60-6C047AB096AF", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "857A277E-6824-48FA-8E7A-9988AC4BCB13" ); // Rock.Workflow.Action.CheckIn.FindFamilyMembers:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "698115D4-7B5E-48F3-BBB0-C53A20193169", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "05C329B0-3794-42BD-9467-8F3FF95D7882" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyGroups:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "698115D4-7B5E-48F3-BBB0-C53A20193169", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "041E4A2B-90C6-4242-A7F1-ED07D9B348F2" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyGroups:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "255C5CA3-4DA1-4804-BBEB-DB9A72408E10" ); // org.secc.FamilyCheckin.CreateLabelsAggregate:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Merge Text", "MergeText", "Text to merge label merge field into separated by commas.", 0, @"AAA,BBB,CCC,DDD", "83801F42-6449-438F-9767-84CD72D2726C" ); // org.secc.FamilyCheckin.CreateLabelsAggregate:Merge Text

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "182E86CB-2EDE-4A65-9313-B1A5EBED81F7" ); // org.secc.FamilyCheckin.CreateLabelsAggregate:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", "C403E219-A56B-439E-9D50-9302DFE760CF", "Aggregated Label", "AggregatedLabel", "Label to aggregate", 0, @"", "0702431C-55B8-402E-95AF-B23869C5B4A8" ); // org.secc.FamilyCheckin.CreateLabelsAggregate:Aggregated Label

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "75F22BFE-0868-491C-89DF-AD0A08A03B1A", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "0F9080F9-4206-4B67-94EF-138D2E211CE9" ); // Rock.Workflow.Action.CheckIn.ReserveAttendance:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "75F22BFE-0868-491C-89DF-AD0A08A03B1A", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Reuse Code For Family", "ReuseCodeForFamily", "By default a unique security code is created for each person.  Select this option to use one security code per family.", 0, @"False", "69CFCE2B-6F31-49D2-8401-0D99BD266F37" ); // Rock.Workflow.Action.CheckIn.ReserveAttendance:Reuse Code For Family

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "75F22BFE-0868-491C-89DF-AD0A08A03B1A", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "D92D5332-FB9A-4EB7-988A-9BCBB3EF7897" ); // Rock.Workflow.Action.CheckIn.ReserveAttendance:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "75F22BFE-0868-491C-89DF-AD0A08A03B1A", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Security Code Length", "SecurityCodeLength", "The number of characters to use for the security code.", 0, @"3", "D2F66015-7F65-46CA-A82B-E4F743CBEC4E" ); // Rock.Workflow.Action.CheckIn.ReserveAttendance:Security Code Length

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "D6BCB113-0699-4D58-8002-BC919CB4BA04" ); // Rock.Workflow.Action.CheckIn.FilterActiveLocations:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove", "Remove", "Select 'Yes' if locations should be be removed.  Select 'No' if they should just be marked as excluded.", 0, @"True", "885D28C5-A395-4A05-AEFB-6131498BDF12" ); // Rock.Workflow.Action.CheckIn.FilterActiveLocations:Remove

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "C8BE5BB1-9293-4FA0-B4CF-FED19B855465" ); // Rock.Workflow.Action.CheckIn.FilterActiveLocations:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "83F299E7-F2C9-4F0A-BA51-23D6CD0F9433" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Prioritize Group Membership", "PrioritizeGroupMembership", "Auto-assign the group and location where the person is a group member. The default value is no.", 0, @"False", "C631DE21-88D8-4DA2-BA32-1AD8E9517B6C" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Prioritize Group Membership

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Room Balance", "RoomBalance", "Auto-assign the location with the least number of current people. This only applies when a person fits into multiple groups or locations.", 1, @"False", "0F56F5AB-E6BE-40EA-8FEA-2484A254AE89" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Room Balance

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Excluded Locations", "ExcludedLocations", "Enter a comma-delimited list of location(s) to manually exclude from room balancing (like a catch-all room).", 3, @"Base Camp", "977D60D8-3E4E-4D7A-A58E-A5F4326CA89A" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Excluded Locations

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Balancing Override", "BalancingOverride", "Enter the maximum difference between two locations before room balancing overrides previous attendance.  The default value is 10.", 2, @"10", "E22D1924-0020-4701-A8AD-DC1455694002" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Balancing Override

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "C599F69C-7295-4F82-A9A2-C769DBAF8765" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByBestFit:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B8B72812-190E-4802-A63F-E693344754BD", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "EE892293-5B1E-4631-877E-179849F8D0FC" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyPeople:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B8B72812-190E-4802-A63F-E693344754BD", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "CFDAD883-5FAA-4EC6-B308-30BBB2EFAA94" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyPeople:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "85BB0E2E-A8DE-4991-B92C-D6378F60001D" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Room Balance", "RoomBalance", "Auto-assign the location with the least number of current people. This only applies when a person fits into multiple groups or locations.", 0, @"False", "970E26C3-C561-4654-A8C6-242C465B67DE" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Room Balance

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Excluded Locations", "ExcludedLocations", "Enter a comma-delimited list of location(s) to manually exclude from room balancing (like a catch-all room).", 2, @"Base Camp", "3000190F-0610-4A23-AAF4-3FE01328BAA0" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Excluded Locations

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Balancing Override", "BalancingOverride", "Enter the maximum difference between two locations before room balancing overrides previous attendance.  The default value is 10.", 1, @"10", "80BF0B30-B3EC-4E64-B0B9-AD9A5AAF8E4B" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Balancing Override

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Max Assignments", "MaxAssignments", "Enter the maximum number of auto-assignments based on previous attendance.  The default value is 5.", 4, @"5", "0E8FE214-24B6-4531-A460-2D78E1A7DF17" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Max Assignments

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "7237CFF5-BC59-4C2C-8314-F690038D93F8" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Previous Months Attendance", "PreviousMonthsAttendance", "Enter the number of previous months to look for attendance history.  The default value is 3 months.", 3, @"3", "38A3DD9B-19C2-4A05-8D23-806B4D21BE18" ); // cc.newspring.AttendedCheckIn.Workflow.Action.CheckIn.SelectByMultipleAttended:Previous Months Attendance

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E2F172A8-88E5-4F84-9805-73164516F5FB", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "1C6D8BD4-1A72-41E7-A9B5-AF37613058D8" ); // Rock.Workflow.Action.CheckIn.FindFamilies:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E2F172A8-88E5-4F84-9805-73164516F5FB", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Phone Search Type", "PhoneSearchType", "The type of search to use when finding families with a matching phone number.", 1, @"0", "3F3EB8C9-477C-4399-88E3-A4C27EC55181" ); // Rock.Workflow.Action.CheckIn.FindFamilies:Phone Search Type

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E2F172A8-88E5-4F84-9805-73164516F5FB", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Max Results", "MaxResults", "The maximum number of families to return ( Default is 100, a value of 0 will not limit results ).", 0, @"100", "BC30C3AA-B249-4CC7-A5A3-89A933DE689D" ); // Rock.Workflow.Action.CheckIn.FindFamilies:Max Results

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E2F172A8-88E5-4F84-9805-73164516F5FB", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "3404112D-3A97-4AE8-B699-07F62BD37D81" ); // Rock.Workflow.Action.CheckIn.FindFamilies:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E998B9A7-31C9-46F6-B91C-4E5C3F06C82F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "20941740-7907-42EE-9250-40EEE2C30D75" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyGroupTypes:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E998B9A7-31C9-46F6-B91C-4E5C3F06C82F", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "4D8C38AC-A58E-49DF-BA53-D28EC00A841A" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyGroupTypes:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F43099A7-E872-439B-A750-351C741BCFEF", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "6845038E-A08E-4D0A-BE1C-750034109496" ); // Rock.Workflow.Action.CheckIn.FindRelationships:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F43099A7-E872-439B-A750-351C741BCFEF", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "2C5535C6-80C9-4886-9A93-33A18F46AAA3" ); // Rock.Workflow.Action.CheckIn.FindRelationships:Order

            RockMigrationHelper.UpdateWorkflowType( false, true, "SECC Check-In", "", "8F8B272D-D351-485E-86D6-3EE5B7C84D99", "Check-in", "fa fa-list-ol", 0, false, 3, "80973FF7-E2C4-4A18-9939-216E347BDC27" ); // SECC Check-In

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "80973FF7-E2C4-4A18-9939-216E347BDC27", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Override", "Override", "Used to enable age/grade override.", 0, @"False", "8B40CAAD-7A30-41E6-973E-276785603566" ); // SECC Check-In:Override

            RockMigrationHelper.UpdateWorkflowActivityType( "80973FF7-E2C4-4A18-9939-216E347BDC27", true, "Family Search", "", false, 0, "62728FE9-42F8-47F9-890D-03C61DDC0AC1" ); // SECC Check-In:Family Search

            RockMigrationHelper.UpdateWorkflowActivityType( "80973FF7-E2C4-4A18-9939-216E347BDC27", true, "Person Search", "", false, 1, "90781BBE-2309-431D-BD90-AA5256C1C8E2" ); // SECC Check-In:Person Search

            RockMigrationHelper.UpdateWorkflowActivityType( "80973FF7-E2C4-4A18-9939-216E347BDC27", true, "Save Attendance", "", false, 2, "E446A867-8144-4E35-9081-CE4BBA82FF20" ); // SECC Check-In:Save Attendance

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Filter Active Locations", 14, "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", true, false, "", "", 1, "", "22C0C392-AD53-4234-A8B3-7149A7F24D98" ); // SECC Check-In:Person Search:Filter Active Locations

            RockMigrationHelper.UpdateWorkflowActionType( "62728FE9-42F8-47F9-890D-03C61DDC0AC1", "Find Families", 0, "E2F172A8-88E5-4F84-9805-73164516F5FB", true, false, "", "", 1, "", "ABCD488B-1A92-4BB3-9ED6-F956380B4487" ); // SECC Check-In:Family Search:Find Families

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Find Family Members", 0, "5794B18B-8F43-43B2-8D60-6C047AB096AF", true, false, "", "", 1, "", "116B69F8-639E-4F3B-9167-98DDAD78072D" ); // SECC Check-In:Person Search:Find Family Members

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Find Relationships", 1, "F43099A7-E872-439B-A750-351C741BCFEF", true, false, "", "", 1, "", "C9C9538A-5F5C-4AF4-A19E-30B2DCB25A14" ); // SECC Check-In:Person Search:Find Relationships

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Groups", 4, "008402A8-3A6C-4CB6-A230-6AD532505EDC", true, false, "", "", 1, "", "3F5095C0-F5B9-41F7-B12A-84C97B17DC8D" ); // SECC Check-In:Person Search:Load Groups

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Group Types", 3, "50D5D915-074A-41FB-9EA7-0DBE52141398", true, false, "", "", 1, "", "138C49DC-4BA5-4880-9963-CA79E02D4BD7" ); // SECC Check-In:Person Search:Load Group Types

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Locations", 7, "4492E36A-77C8-4DC7-8128-570FAA161ADB", true, false, "", "", 1, "", "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034" ); // SECC Check-In:Person Search:Load Locations

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Locations", 11, "4492E36A-77C8-4DC7-8128-570FAA161ADB", true, false, "", "", 1, "", "E0239ACD-E446-466B-9D48-F3A1413C6F33" ); // SECC Check-In:Person Search:Load Locations

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Schedules", 12, "24A7E196-B50B-4BD6-A347-07CFC5ABEF9E", true, false, "", "", 1, "", "5CD7B411-FA58-41C1-B19F-87BC72D50B7F" ); // SECC Check-In:Person Search:Load Schedules

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Remove Empty Groups", 8, "698115D4-7B5E-48F3-BBB0-C53A20193169", true, false, "", "", 1, "", "141E52BC-4256-44D4-B779-FC495BBD72BA" ); // SECC Check-In:Person Search:Remove Empty Groups

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Remove Empty Group Types", 9, "E998B9A7-31C9-46F6-B91C-4E5C3F06C82F", true, false, "", "", 1, "", "9F290715-00B3-441A-AD3E-9970187898B9" ); // SECC Check-In:Person Search:Remove Empty Group Types

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Remove Empty People", 10, "B8B72812-190E-4802-A63F-E693344754BD", true, false, "", "", 1, "", "37A2ADB0-422A-4080-A967-8F88F90E2D96" ); // SECC Check-In:Person Search:Remove Empty People

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Filter Groups by Age", 5, "23F1E3FD-48AE-451F-9911-A5C7523A74B6", true, false, "", "8B40CAAD-7A30-41E6-973E-276785603566", 1, "False", "BDCE70CD-0F18-4B4A-89BE-CBC393DA598D" ); // SECC Check-In:Person Search:Filter Groups by Age

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Filter Groups by Grade", 6, "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", true, false, "", "8B40CAAD-7A30-41E6-973E-276785603566", 1, "False", "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E" ); // SECC Check-In:Person Search:Filter Groups by Grade

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Select By Best Fit", 15, "B1A855F8-7ED6-49AE-8EEA-D1DCB6C7E944", true, false, "", "", 1, "", "E9845900-4D0B-4DE4-9B85-D85C53941098" ); // SECC Check-In:Person Search:Select By Best Fit

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Select By Multiple Attended", 13, "DDC2D0CA-28A9-420B-9915-B3831DE75DAC", true, false, "", "", 1, "", "DF425D08-2231-4622-8007-0588212377CF" ); // SECC Check-In:Person Search:Select By Multiple Attended

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Select All People", 2, "4406D2DE-398B-4439-9616-458454239BFF", true, false, "", "", 1, "", "F02C9D65-60BE-4EBB-B4A4-448AFD1AFB78" ); // SECC Check-In:Person Search:Select All People

            RockMigrationHelper.UpdateWorkflowActionType( "E446A867-8144-4E35-9081-CE4BBA82FF20", "Reserve Attendance", 0, "75F22BFE-0868-491C-89DF-AD0A08A03B1A", true, false, "", "", 1, "", "32D37BAE-A61B-4B0B-9925-D68DA4156656" ); // SECC Check-In:Save Attendance:Reserve Attendance

            RockMigrationHelper.UpdateWorkflowActionType( "E446A867-8144-4E35-9081-CE4BBA82FF20", "Create Labels Aggregate", 1, "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", true, false, "", "", 1, "", "FD2C5F18-BAFC-400E-8A83-D28936C3F367" ); // SECC Check-In:Save Attendance:Create Labels Aggregate

            RockMigrationHelper.AddActionTypeAttributeValue( "ABCD488B-1A92-4BB3-9ED6-F956380B4487", "3404112D-3A97-4AE8-B699-07F62BD37D81", @"" ); // SECC Check-In:Family Search:Find Families:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "ABCD488B-1A92-4BB3-9ED6-F956380B4487", "1C6D8BD4-1A72-41E7-A9B5-AF37613058D8", @"False" ); // SECC Check-In:Family Search:Find Families:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "ABCD488B-1A92-4BB3-9ED6-F956380B4487", "BC30C3AA-B249-4CC7-A5A3-89A933DE689D", @"100" ); // SECC Check-In:Family Search:Find Families:Max Results

            RockMigrationHelper.AddActionTypeAttributeValue( "ABCD488B-1A92-4BB3-9ED6-F956380B4487", "3F3EB8C9-477C-4399-88E3-A4C27EC55181", @"0" ); // SECC Check-In:Family Search:Find Families:Phone Search Type

            RockMigrationHelper.AddActionTypeAttributeValue( "116B69F8-639E-4F3B-9167-98DDAD78072D", "857A277E-6824-48FA-8E7A-9988AC4BCB13", @"" ); // SECC Check-In:Person Search:Find Family Members:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "116B69F8-639E-4F3B-9167-98DDAD78072D", "3EF34D41-030B-411F-9D18-D331ABD89F0D", @"False" ); // SECC Check-In:Person Search:Find Family Members:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "C9C9538A-5F5C-4AF4-A19E-30B2DCB25A14", "2C5535C6-80C9-4886-9A93-33A18F46AAA3", @"" ); // SECC Check-In:Person Search:Find Relationships:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "C9C9538A-5F5C-4AF4-A19E-30B2DCB25A14", "6845038E-A08E-4D0A-BE1C-750034109496", @"False" ); // SECC Check-In:Person Search:Find Relationships:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "F02C9D65-60BE-4EBB-B4A4-448AFD1AFB78", "4E1B3B40-0F2D-4DC4-905C-3EDCE8226692", @"" ); // SECC Check-In:Person Search:Select All People:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "F02C9D65-60BE-4EBB-B4A4-448AFD1AFB78", "2DC87EA3-AA59-4C32-A45C-2155255CAD92", @"False" ); // SECC Check-In:Person Search:Select All People:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "138C49DC-4BA5-4880-9963-CA79E02D4BD7", "1F4BD3F6-C528-4160-8478-825C3B8AC85D", @"" ); // SECC Check-In:Person Search:Load Group Types:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "138C49DC-4BA5-4880-9963-CA79E02D4BD7", "1C7CD28E-ACC5-4B88-BC05-E02D72919305", @"False" ); // SECC Check-In:Person Search:Load Group Types:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "3F5095C0-F5B9-41F7-B12A-84C97B17DC8D", "39762EF0-91D5-4B13-BD34-FC3AC3C24897", @"True" ); // SECC Check-In:Person Search:Load Groups:Load All

            RockMigrationHelper.AddActionTypeAttributeValue( "3F5095C0-F5B9-41F7-B12A-84C97B17DC8D", "C26C5959-7144-443B-88ED-28E4A5AE544C", @"" ); // SECC Check-In:Person Search:Load Groups:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "3F5095C0-F5B9-41F7-B12A-84C97B17DC8D", "AD7528AD-2A3D-4C26-B452-FA9F4F48953C", @"False" ); // SECC Check-In:Person Search:Load Groups:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "BDCE70CD-0F18-4B4A-89BE-CBC393DA598D", "554108CF-31A1-47C0-A184-18B4A881D7FD", @"" ); // SECC Check-In:Person Search:Filter Groups by Age:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "BDCE70CD-0F18-4B4A-89BE-CBC393DA598D", "6F98731B-1C17-49F0-8B5C-1C7DBDB08A07", @"False" ); // SECC Check-In:Person Search:Filter Groups by Age:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "BDCE70CD-0F18-4B4A-89BE-CBC393DA598D", "F05781E2-3517-4D20-A3BB-DA56CA025F25", @"True" ); // SECC Check-In:Person Search:Filter Groups by Age:Remove

            RockMigrationHelper.AddActionTypeAttributeValue( "BDCE70CD-0F18-4B4A-89BE-CBC393DA598D", "EEC0F601-BE2C-4C22-8E6B-190CFEA34879", @"True" ); // SECC Check-In:Person Search:Filter Groups by Age:Age Required

            RockMigrationHelper.AddActionTypeAttributeValue( "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E", "78A19926-CABA-475D-8AB9-F9EB08BD6FAA", @"" ); // SECC Check-In:Person Search:Filter Groups by Grade:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E", "E69E09E8-C71D-4AA3-86AD-D46DBAE60816", @"False" ); // SECC Check-In:Person Search:Filter Groups by Grade:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E", "39F82A06-DD0E-4D51-A364-8D6C0EB32BC4", @"True" ); // SECC Check-In:Person Search:Filter Groups by Grade:Remove

            RockMigrationHelper.AddActionTypeAttributeValue( "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034", "2F3B6B42-A89C-443A-9008-E9E96535E815", @"False" ); // SECC Check-In:Person Search:Load Locations:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034", "70203A96-AE70-47AD-A086-FD84792DF2B6", @"True" ); // SECC Check-In:Person Search:Load Locations:Load All

            RockMigrationHelper.AddActionTypeAttributeValue( "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034", "6EE6128C-79BF-4333-85DB-3B0C92B27131", @"" ); // SECC Check-In:Person Search:Load Locations:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "141E52BC-4256-44D4-B779-FC495BBD72BA", "041E4A2B-90C6-4242-A7F1-ED07D9B348F2", @"" ); // SECC Check-In:Person Search:Remove Empty Groups:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "141E52BC-4256-44D4-B779-FC495BBD72BA", "05C329B0-3794-42BD-9467-8F3FF95D7882", @"False" ); // SECC Check-In:Person Search:Remove Empty Groups:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "9F290715-00B3-441A-AD3E-9970187898B9", "4D8C38AC-A58E-49DF-BA53-D28EC00A841A", @"" ); // SECC Check-In:Person Search:Remove Empty Group Types:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "9F290715-00B3-441A-AD3E-9970187898B9", "20941740-7907-42EE-9250-40EEE2C30D75", @"False" ); // SECC Check-In:Person Search:Remove Empty Group Types:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "37A2ADB0-422A-4080-A967-8F88F90E2D96", "CFDAD883-5FAA-4EC6-B308-30BBB2EFAA94", @"" ); // SECC Check-In:Person Search:Remove Empty People:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "37A2ADB0-422A-4080-A967-8F88F90E2D96", "EE892293-5B1E-4631-877E-179849F8D0FC", @"False" ); // SECC Check-In:Person Search:Remove Empty People:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "E0239ACD-E446-466B-9D48-F3A1413C6F33", "2F3B6B42-A89C-443A-9008-E9E96535E815", @"False" ); // SECC Check-In:Person Search:Load Locations:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "E0239ACD-E446-466B-9D48-F3A1413C6F33", "6EE6128C-79BF-4333-85DB-3B0C92B27131", @"" ); // SECC Check-In:Person Search:Load Locations:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "E0239ACD-E446-466B-9D48-F3A1413C6F33", "70203A96-AE70-47AD-A086-FD84792DF2B6", @"True" ); // SECC Check-In:Person Search:Load Locations:Load All

            RockMigrationHelper.AddActionTypeAttributeValue( "5CD7B411-FA58-41C1-B19F-87BC72D50B7F", "B222CAF2-DF12-433C-B5D4-A8DB95B60207", @"True" ); // SECC Check-In:Person Search:Load Schedules:Load All

            RockMigrationHelper.AddActionTypeAttributeValue( "5CD7B411-FA58-41C1-B19F-87BC72D50B7F", "F7B09469-EB3D-44A4-AB8E-C74318BD4669", @"" ); // SECC Check-In:Person Search:Load Schedules:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "5CD7B411-FA58-41C1-B19F-87BC72D50B7F", "4DFA9F8D-F2E6-4040-A23B-2A1F8258C767", @"False" ); // SECC Check-In:Person Search:Load Schedules:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "DF425D08-2231-4622-8007-0588212377CF", "85BB0E2E-A8DE-4991-B92C-D6378F60001D", @"False" ); // SECC Check-In:Person Search:Select By Multiple Attended:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "DF425D08-2231-4622-8007-0588212377CF", "7237CFF5-BC59-4C2C-8314-F690038D93F8", @"" ); // SECC Check-In:Person Search:Select By Multiple Attended:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "DF425D08-2231-4622-8007-0588212377CF", "970E26C3-C561-4654-A8C6-242C465B67DE", @"False" ); // SECC Check-In:Person Search:Select By Multiple Attended:Room Balance

            RockMigrationHelper.AddActionTypeAttributeValue( "DF425D08-2231-4622-8007-0588212377CF", "80BF0B30-B3EC-4E64-B0B9-AD9A5AAF8E4B", @"10" ); // SECC Check-In:Person Search:Select By Multiple Attended:Balancing Override

            RockMigrationHelper.AddActionTypeAttributeValue( "DF425D08-2231-4622-8007-0588212377CF", "3000190F-0610-4A23-AAF4-3FE01328BAA0", @"Base Camp" ); // SECC Check-In:Person Search:Select By Multiple Attended:Excluded Locations

            RockMigrationHelper.AddActionTypeAttributeValue( "DF425D08-2231-4622-8007-0588212377CF", "38A3DD9B-19C2-4A05-8D23-806B4D21BE18", @"3" ); // SECC Check-In:Person Search:Select By Multiple Attended:Previous Months Attendance

            RockMigrationHelper.AddActionTypeAttributeValue( "DF425D08-2231-4622-8007-0588212377CF", "0E8FE214-24B6-4531-A460-2D78E1A7DF17", @"2" ); // SECC Check-In:Person Search:Select By Multiple Attended:Max Assignments

            RockMigrationHelper.AddActionTypeAttributeValue( "22C0C392-AD53-4234-A8B3-7149A7F24D98", "C8BE5BB1-9293-4FA0-B4CF-FED19B855465", @"" ); // SECC Check-In:Person Search:Filter Active Locations:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "22C0C392-AD53-4234-A8B3-7149A7F24D98", "D6BCB113-0699-4D58-8002-BC919CB4BA04", @"False" ); // SECC Check-In:Person Search:Filter Active Locations:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "22C0C392-AD53-4234-A8B3-7149A7F24D98", "885D28C5-A395-4A05-AEFB-6131498BDF12", @"True" ); // SECC Check-In:Person Search:Filter Active Locations:Remove

            RockMigrationHelper.AddActionTypeAttributeValue( "E9845900-4D0B-4DE4-9B85-D85C53941098", "83F299E7-F2C9-4F0A-BA51-23D6CD0F9433", @"False" ); // SECC Check-In:Person Search:Select By Best Fit:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "E9845900-4D0B-4DE4-9B85-D85C53941098", "C599F69C-7295-4F82-A9A2-C769DBAF8765", @"" ); // SECC Check-In:Person Search:Select By Best Fit:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "E9845900-4D0B-4DE4-9B85-D85C53941098", "C631DE21-88D8-4DA2-BA32-1AD8E9517B6C", @"False" ); // SECC Check-In:Person Search:Select By Best Fit:Prioritize Group Membership

            RockMigrationHelper.AddActionTypeAttributeValue( "E9845900-4D0B-4DE4-9B85-D85C53941098", "0F56F5AB-E6BE-40EA-8FEA-2484A254AE89", @"False" ); // SECC Check-In:Person Search:Select By Best Fit:Room Balance

            RockMigrationHelper.AddActionTypeAttributeValue( "E9845900-4D0B-4DE4-9B85-D85C53941098", "E22D1924-0020-4701-A8AD-DC1455694002", @"10" ); // SECC Check-In:Person Search:Select By Best Fit:Balancing Override

            RockMigrationHelper.AddActionTypeAttributeValue( "E9845900-4D0B-4DE4-9B85-D85C53941098", "977D60D8-3E4E-4D7A-A58E-A5F4326CA89A", @"Base Camp" ); // SECC Check-In:Person Search:Select By Best Fit:Excluded Locations

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "D2F66015-7F65-46CA-A82B-E4F743CBEC4E", @"3" ); // SECC Check-In:Save Attendance:Reserve Attendance:Security Code Length

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "69CFCE2B-6F31-49D2-8401-0D99BD266F37", @"False" ); // SECC Check-In:Save Attendance:Reserve Attendance:Reuse Code For Family

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "D92D5332-FB9A-4EB7-988A-9BCBB3EF7897", @"" ); // SECC Check-In:Save Attendance:Reserve Attendance:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "0F9080F9-4206-4B67-94EF-138D2E211CE9", @"False" ); // SECC Check-In:Save Attendance:Reserve Attendance:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "FD2C5F18-BAFC-400E-8A83-D28936C3F367", "0702431C-55B8-402E-95AF-B23869C5B4A8", @"8783be7c-4dcb-4460-a2a3-3a8df19b44e5" ); // SECC Check-In:Save Attendance:Create Labels Aggregate:Aggregated Label

            RockMigrationHelper.AddActionTypeAttributeValue( "FD2C5F18-BAFC-400E-8A83-D28936C3F367", "182E86CB-2EDE-4A65-9313-B1A5EBED81F7", @"" ); // SECC Check-In:Save Attendance:Create Labels Aggregate:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "FD2C5F18-BAFC-400E-8A83-D28936C3F367", "255C5CA3-4DA1-4804-BBEB-DB9A72408E10", @"False" ); // SECC Check-In:Save Attendance:Create Labels Aggregate:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "FD2C5F18-BAFC-400E-8A83-D28936C3F367", "83801F42-6449-438F-9767-84CD72D2726C", @"AAA,BBB,CCC,DDD,EEE,FFF,GGG,HHH,III,JJJ,KKK,LLL" ); // SECC Check-In:Save Attendance:Create Labels Aggregate:Merge Text

        }
        public override void Down()
        {

        }
    }
}

