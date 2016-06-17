using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 8, "1.2.0" )]
    class SuperCheckin_WorkflowData : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateFieldType( "Address", "", "Rock", "Rock.Field.Types.AddressFieldType", "DA532BC4-0464-4FE4-9FC3-8D80890A1AAB" );

            RockMigrationHelper.UpdateFieldType( "Content Channel Types", "", "Rock", "Rock.Field.Types.ContentChannelTypesFieldType", "BFCFF586-B584-4DF0-9BB7-661FB309142F" );

            RockMigrationHelper.UpdateEntityType( "Rock.Model.Workflow", "3540E9A7-FE30-43A9-8B0A-A372B63DFC93", true, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActivity", "2CB52ED0-CB06-4D62-9E2C-73B60AFA4C9F", true, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActionType", "23E3273A-B137-48A3-9AFF-C8DC832DDCA6", true, true );

            RockMigrationHelper.UpdateEntityType( "org.secc.FamilyCheckin.CreateLabelsAggregate", "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", false, true );

            RockMigrationHelper.UpdateEntityType( "org.secc.FamilyCheckin.HistoricalPreselect", "ACE8A762-A547-4409-BCFD-C631C99746E8", false, true );

            RockMigrationHelper.UpdateEntityType( "org.secc.FamilyCheckin.SelectAllPeople", "4406D2DE-398B-4439-9616-458454239BFF", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.CreateLabels", "8F348E7B-F9FD-4600-852D-477B13B0B4EE", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FilterActiveLocations", "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FilterGroupsByAge", "23F1E3FD-48AE-451F-9911-A5C7523A74B6", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FilterGroupsByGrade", "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CheckIn.FilterGroupsByMembership", "D877272D-FE27-40C3-AA08-7CE04B43A68F", false, true );

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

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "8F348E7B-F9FD-4600-852D-477B13B0B4EE", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "36EB15CE-095C-41ED-9C0F-9EA345599D54" ); // Rock.Workflow.Action.CheckIn.CreateLabels:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "8F348E7B-F9FD-4600-852D-477B13B0B4EE", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "F70112C9-4D93-41B9-A3FB-1E7C866AACCF" ); // Rock.Workflow.Action.CheckIn.CreateLabels:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "ACE8A762-A547-4409-BCFD-C631C99746E8", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "D224733C-510F-42E8-8473-4A3834EF4C1B" ); // org.secc.FamilyCheckin.HistoricalPreselect:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "ACE8A762-A547-4409-BCFD-C631C99746E8", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "0DC0C19A-5D2C-40CC-A7FB-C9DC77308735" ); // org.secc.FamilyCheckin.HistoricalPreselect:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "ACE8A762-A547-4409-BCFD-C631C99746E8", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Week History", "WeekHistory", "Number of weeks history to look back.", 0, @"4", "A67FF180-E74D-4801-BC36-220F74E0723F" ); // org.secc.FamilyCheckin.HistoricalPreselect:Week History

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B8B72812-190E-4802-A63F-E693344754BD", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "EE892293-5B1E-4631-877E-179849F8D0FC" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyPeople:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "B8B72812-190E-4802-A63F-E693344754BD", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "CFDAD883-5FAA-4EC6-B308-30BBB2EFAA94" ); // Rock.Workflow.Action.CheckIn.RemoveEmptyPeople:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "D877272D-FE27-40C3-AA08-7CE04B43A68F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "E05F06D0-955C-49DB-8A4D-2EDBA503FCE3" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByMembership:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "D877272D-FE27-40C3-AA08-7CE04B43A68F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Remove", "Remove", "Select 'Yes' if groups should be be removed.  Select 'No' if they should just be marked as excluded.", 5, @"True", "B2CD4938-C1B7-42AC-8A0A-88E60035821E" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByMembership:Remove

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "D877272D-FE27-40C3-AA08-7CE04B43A68F", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Group Membership Group Attribute", "GroupMembershipGroupAttribute", "Select the attribute used to filter membership group.", 2, @"6f1ff463-e857-4755-b0b7-461e8c183789", "63F02924-2217-4171-B5F7-1A91F13A0ECF" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByMembership:Group Membership Group Attribute

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "D877272D-FE27-40C3-AA08-7CE04B43A68F", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "4468C1FC-E0A2-4006-B347-AEC91F71241C" ); // Rock.Workflow.Action.CheckIn.FilterGroupsByMembership:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "E2F172A8-88E5-4F84-9805-73164516F5FB", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "1C6D8BD4-1A72-41E7-A9B5-AF37613058D8" ); // Rock.Workflow.Action.CheckIn.FindFamilies:Active

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

            RockMigrationHelper.UpdateWorkflowActionType( "E446A867-8144-4E35-9081-CE4BBA82FF20", "Create Labels", 1, "8F348E7B-F9FD-4600-852D-477B13B0B4EE", true, false, "", "", 1, "", "C842058A-5983-4A1B-928A-BDC0FFEAE875" ); // SECC Check-In:Save Attendance:Create Labels

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Filter Active Locations", 14, "7BB371F9-A8DE-49D3-BEEA-C191F6C7D4A0", true, false, "", "", 1, "", "22C0C392-AD53-4234-A8B3-7149A7F24D98" ); // SECC Check-In:Person Search:Filter Active Locations

            RockMigrationHelper.UpdateWorkflowActionType( "62728FE9-42F8-47F9-890D-03C61DDC0AC1", "Find Families", 0, "E2F172A8-88E5-4F84-9805-73164516F5FB", true, false, "", "", 1, "", "ABCD488B-1A92-4BB3-9ED6-F956380B4487" ); // SECC Check-In:Family Search:Find Families

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Find Family Members", 0, "5794B18B-8F43-43B2-8D60-6C047AB096AF", true, false, "", "", 1, "", "116B69F8-639E-4F3B-9167-98DDAD78072D" ); // SECC Check-In:Person Search:Find Family Members

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Find Relationships", 1, "F43099A7-E872-439B-A750-351C741BCFEF", true, false, "", "", 1, "", "C9C9538A-5F5C-4AF4-A19E-30B2DCB25A14" ); // SECC Check-In:Person Search:Find Relationships

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Groups", 4, "008402A8-3A6C-4CB6-A230-6AD532505EDC", true, false, "", "", 1, "", "3F5095C0-F5B9-41F7-B12A-84C97B17DC8D" ); // SECC Check-In:Person Search:Load Groups

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Group Types", 3, "50D5D915-074A-41FB-9EA7-0DBE52141398", true, false, "", "", 1, "", "138C49DC-4BA5-4880-9963-CA79E02D4BD7" ); // SECC Check-In:Person Search:Load Group Types

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Locations", 8, "4492E36A-77C8-4DC7-8128-570FAA161ADB", true, false, "", "", 1, "", "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034" ); // SECC Check-In:Person Search:Load Locations

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Locations", 12, "4492E36A-77C8-4DC7-8128-570FAA161ADB", true, false, "", "", 1, "", "E0239ACD-E446-466B-9D48-F3A1413C6F33" ); // SECC Check-In:Person Search:Load Locations

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Load Schedules", 13, "24A7E196-B50B-4BD6-A347-07CFC5ABEF9E", true, false, "", "", 1, "", "5CD7B411-FA58-41C1-B19F-87BC72D50B7F" ); // SECC Check-In:Person Search:Load Schedules

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Remove Empty Groups", 9, "698115D4-7B5E-48F3-BBB0-C53A20193169", true, false, "", "", 1, "", "141E52BC-4256-44D4-B779-FC495BBD72BA" ); // SECC Check-In:Person Search:Remove Empty Groups

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Remove Empty Group Types", 10, "E998B9A7-31C9-46F6-B91C-4E5C3F06C82F", true, false, "", "", 1, "", "9F290715-00B3-441A-AD3E-9970187898B9" ); // SECC Check-In:Person Search:Remove Empty Group Types

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Remove Empty People", 11, "B8B72812-190E-4802-A63F-E693344754BD", true, false, "", "", 1, "", "37A2ADB0-422A-4080-A967-8F88F90E2D96" ); // SECC Check-In:Person Search:Remove Empty People

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Filter Groups by Age", 5, "23F1E3FD-48AE-451F-9911-A5C7523A74B6", true, false, "", "8B40CAAD-7A30-41E6-973E-276785603566", 1, "False", "BDCE70CD-0F18-4B4A-89BE-CBC393DA598D" ); // SECC Check-In:Person Search:Filter Groups by Age

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Filter Groups by Grade", 6, "542DFBF4-F2F5-4C2F-9C9D-CDB00FDD5F04", true, false, "", "8B40CAAD-7A30-41E6-973E-276785603566", 1, "False", "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E" ); // SECC Check-In:Person Search:Filter Groups by Grade

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Select All People", 2, "4406D2DE-398B-4439-9616-458454239BFF", true, false, "", "", 1, "", "F02C9D65-60BE-4EBB-B4A4-448AFD1AFB78" ); // SECC Check-In:Person Search:Select All People

            RockMigrationHelper.UpdateWorkflowActionType( "E446A867-8144-4E35-9081-CE4BBA82FF20", "Reserve Attendance", 0, "75F22BFE-0868-491C-89DF-AD0A08A03B1A", true, false, "", "", 1, "", "32D37BAE-A61B-4B0B-9925-D68DA4156656" ); // SECC Check-In:Save Attendance:Reserve Attendance

            RockMigrationHelper.UpdateWorkflowActionType( "E446A867-8144-4E35-9081-CE4BBA82FF20", "Create Labels Aggregate", 2, "6D95BBF9-F470-413F-BC3A-ABAC3C245FEB", true, false, "", "", 1, "", "FD2C5F18-BAFC-400E-8A83-D28936C3F367" ); // SECC Check-In:Save Attendance:Create Labels Aggregate

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Filter Groups By Membership", 7, "D877272D-FE27-40C3-AA08-7CE04B43A68F", true, false, "", "", 1, "", "0574E594-A64E-4508-BACC-554566069B1E" ); // SECC Check-In:Person Search:Filter Groups By Membership

            RockMigrationHelper.UpdateWorkflowActionType( "90781BBE-2309-431D-BD90-AA5256C1C8E2", "Historical Preselect", 15, "ACE8A762-A547-4409-BCFD-C631C99746E8", true, false, "", "", 1, "", "4910F614-C699-474F-A438-BEA2723096EC" ); // SECC Check-In:Person Search:Historical Preselect

            RockMigrationHelper.AddActionTypeAttributeValue( "ABCD488B-1A92-4BB3-9ED6-F956380B4487", "3404112D-3A97-4AE8-B699-07F62BD37D81", @"" ); // SECC Check-In:Family Search:Find Families:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "ABCD488B-1A92-4BB3-9ED6-F956380B4487", "1C6D8BD4-1A72-41E7-A9B5-AF37613058D8", @"False" ); // SECC Check-In:Family Search:Find Families:Active

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

            RockMigrationHelper.AddActionTypeAttributeValue( "BDCE70CD-0F18-4B4A-89BE-CBC393DA598D", "688B346E-EAA6-4DD9-9B67-C3AABECADF7D", @"43511b8f-71d9-423a-85bf-d1cd08c1998e" ); // SECC Check-In:Person Search:Filter Groups by Age:Group Age Range Attribute

            RockMigrationHelper.AddActionTypeAttributeValue( "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E", "78A19926-CABA-475D-8AB9-F9EB08BD6FAA", @"" ); // SECC Check-In:Person Search:Filter Groups by Grade:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E", "E69E09E8-C71D-4AA3-86AD-D46DBAE60816", @"False" ); // SECC Check-In:Person Search:Filter Groups by Grade:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "19CA81D6-1D75-4E60-9CD1-522DEC8EE67E", "39F82A06-DD0E-4D51-A364-8D6C0EB32BC4", @"True" ); // SECC Check-In:Person Search:Filter Groups by Grade:Remove

            RockMigrationHelper.AddActionTypeAttributeValue( "0574E594-A64E-4508-BACC-554566069B1E", "4468C1FC-E0A2-4006-B347-AEC91F71241C", @"" ); // SECC Check-In:Person Search:Filter Groups By Membership:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "0574E594-A64E-4508-BACC-554566069B1E", "E05F06D0-955C-49DB-8A4D-2EDBA503FCE3", @"False" ); // SECC Check-In:Person Search:Filter Groups By Membership:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "0574E594-A64E-4508-BACC-554566069B1E", "63F02924-2217-4171-B5F7-1A91F13A0ECF", @"279d7011-ef53-45da-b231-0fde97f49139" ); // SECC Check-In:Person Search:Filter Groups By Membership:Group Membership Group Attribute

            RockMigrationHelper.AddActionTypeAttributeValue( "0574E594-A64E-4508-BACC-554566069B1E", "B2CD4938-C1B7-42AC-8A0A-88E60035821E", @"True" ); // SECC Check-In:Person Search:Filter Groups By Membership:Remove

            RockMigrationHelper.AddActionTypeAttributeValue( "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034", "70203A96-AE70-47AD-A086-FD84792DF2B6", @"True" ); // SECC Check-In:Person Search:Load Locations:Load All

            RockMigrationHelper.AddActionTypeAttributeValue( "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034", "6EE6128C-79BF-4333-85DB-3B0C92B27131", @"" ); // SECC Check-In:Person Search:Load Locations:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "CA61BBCA-8AC7-4580-8EEE-0C4BA8C2D034", "2F3B6B42-A89C-443A-9008-E9E96535E815", @"False" ); // SECC Check-In:Person Search:Load Locations:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "141E52BC-4256-44D4-B779-FC495BBD72BA", "041E4A2B-90C6-4242-A7F1-ED07D9B348F2", @"" ); // SECC Check-In:Person Search:Remove Empty Groups:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "141E52BC-4256-44D4-B779-FC495BBD72BA", "05C329B0-3794-42BD-9467-8F3FF95D7882", @"False" ); // SECC Check-In:Person Search:Remove Empty Groups:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "9F290715-00B3-441A-AD3E-9970187898B9", "4D8C38AC-A58E-49DF-BA53-D28EC00A841A", @"" ); // SECC Check-In:Person Search:Remove Empty Group Types:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "9F290715-00B3-441A-AD3E-9970187898B9", "20941740-7907-42EE-9250-40EEE2C30D75", @"False" ); // SECC Check-In:Person Search:Remove Empty Group Types:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "37A2ADB0-422A-4080-A967-8F88F90E2D96", "CFDAD883-5FAA-4EC6-B308-30BBB2EFAA94", @"" ); // SECC Check-In:Person Search:Remove Empty People:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "37A2ADB0-422A-4080-A967-8F88F90E2D96", "EE892293-5B1E-4631-877E-179849F8D0FC", @"False" ); // SECC Check-In:Person Search:Remove Empty People:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "E0239ACD-E446-466B-9D48-F3A1413C6F33", "70203A96-AE70-47AD-A086-FD84792DF2B6", @"True" ); // SECC Check-In:Person Search:Load Locations:Load All

            RockMigrationHelper.AddActionTypeAttributeValue( "E0239ACD-E446-466B-9D48-F3A1413C6F33", "6EE6128C-79BF-4333-85DB-3B0C92B27131", @"" ); // SECC Check-In:Person Search:Load Locations:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "E0239ACD-E446-466B-9D48-F3A1413C6F33", "2F3B6B42-A89C-443A-9008-E9E96535E815", @"False" ); // SECC Check-In:Person Search:Load Locations:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "5CD7B411-FA58-41C1-B19F-87BC72D50B7F", "B222CAF2-DF12-433C-B5D4-A8DB95B60207", @"True" ); // SECC Check-In:Person Search:Load Schedules:Load All

            RockMigrationHelper.AddActionTypeAttributeValue( "5CD7B411-FA58-41C1-B19F-87BC72D50B7F", "F7B09469-EB3D-44A4-AB8E-C74318BD4669", @"" ); // SECC Check-In:Person Search:Load Schedules:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "5CD7B411-FA58-41C1-B19F-87BC72D50B7F", "4DFA9F8D-F2E6-4040-A23B-2A1F8258C767", @"False" ); // SECC Check-In:Person Search:Load Schedules:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "22C0C392-AD53-4234-A8B3-7149A7F24D98", "C8BE5BB1-9293-4FA0-B4CF-FED19B855465", @"" ); // SECC Check-In:Person Search:Filter Active Locations:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "22C0C392-AD53-4234-A8B3-7149A7F24D98", "D6BCB113-0699-4D58-8002-BC919CB4BA04", @"False" ); // SECC Check-In:Person Search:Filter Active Locations:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "22C0C392-AD53-4234-A8B3-7149A7F24D98", "885D28C5-A395-4A05-AEFB-6131498BDF12", @"True" ); // SECC Check-In:Person Search:Filter Active Locations:Remove

            RockMigrationHelper.AddActionTypeAttributeValue( "4910F614-C699-474F-A438-BEA2723096EC", "A67FF180-E74D-4801-BC36-220F74E0723F", @"4" ); // SECC Check-In:Person Search:Historical Preselect:Week History

            RockMigrationHelper.AddActionTypeAttributeValue( "4910F614-C699-474F-A438-BEA2723096EC", "0DC0C19A-5D2C-40CC-A7FB-C9DC77308735", @"" ); // SECC Check-In:Person Search:Historical Preselect:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "4910F614-C699-474F-A438-BEA2723096EC", "D224733C-510F-42E8-8473-4A3834EF4C1B", @"False" ); // SECC Check-In:Person Search:Historical Preselect:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "D2F66015-7F65-46CA-A82B-E4F743CBEC4E", @"3" ); // SECC Check-In:Save Attendance:Reserve Attendance:Security Code Length

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "69CFCE2B-6F31-49D2-8401-0D99BD266F37", @"False" ); // SECC Check-In:Save Attendance:Reserve Attendance:Reuse Code For Family

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "D92D5332-FB9A-4EB7-988A-9BCBB3EF7897", @"" ); // SECC Check-In:Save Attendance:Reserve Attendance:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "32D37BAE-A61B-4B0B-9925-D68DA4156656", "0F9080F9-4206-4B67-94EF-138D2E211CE9", @"False" ); // SECC Check-In:Save Attendance:Reserve Attendance:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "C842058A-5983-4A1B-928A-BDC0FFEAE875", "F70112C9-4D93-41B9-A3FB-1E7C866AACCF", @"" ); // SECC Check-In:Save Attendance:Create Labels:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "C842058A-5983-4A1B-928A-BDC0FFEAE875", "36EB15CE-095C-41ED-9C0F-9EA345599D54", @"False" ); // SECC Check-In:Save Attendance:Create Labels:Active

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

