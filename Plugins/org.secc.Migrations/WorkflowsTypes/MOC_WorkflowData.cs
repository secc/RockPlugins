using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 2, "1.2.0" )]
    class MOC_WorkflowData : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateEntityType( "Rock.Model.Workflow", "3540E9A7-FE30-43A9-8B0A-A372B63DFC93", true, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActivity", "2CB52ED0-CB06-4D62-9E2C-73B60AFA4C9F", true, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActionType", "23E3273A-B137-48A3-9AFF-C8DC832DDCA6", true, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.ActivateActivity", "38907A90-1634-4A93-8017-619326A4A582", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.AssignActivityToPerson", "FB2981B7-7922-42E1-8ACF-7F63BB7989E6", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CompleteWorkflow", "EEDA4318-F014-4A46-9C76-4C052EF81AA1", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.PersistWorkflow", "F1A39347-6FE0-43D4-89FB-544195088ECF", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.SetAttributeToInitiator", "4EEAC6FA-B838-410B-A8DD-21A364029F5D", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.SetWorkflowName", "36005473-BD5D-470B-B28D-98E6D7ED808D", false, true );

            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.UserEntryForm", "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", false, true );

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "36005473-BD5D-470B-B28D-98E6D7ED808D", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "0A800013-51F7-4902-885A-5BE215D67D3D" ); // Rock.Workflow.Action.SetWorkflowName:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "36005473-BD5D-470B-B28D-98E6D7ED808D", "3B1D93D7-9414-48F9-80E5-6A3FC8F94C20", "Text Value|Attribute Value", "NameValue", "The value to use for the workflow's name. <span class='tip tip-lava'></span>", 1, @"", "93852244-A667-4749-961A-D47F88675BE4" ); // Rock.Workflow.Action.SetWorkflowName:Text Value|Attribute Value

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "36005473-BD5D-470B-B28D-98E6D7ED808D", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "5D95C15A-CCAE-40AD-A9DD-F929DA587115" ); // Rock.Workflow.Action.SetWorkflowName:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "38907A90-1634-4A93-8017-619326A4A582", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "E8ABD802-372C-47BE-82B1-96F50DB5169E" ); // Rock.Workflow.Action.ActivateActivity:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "38907A90-1634-4A93-8017-619326A4A582", "739FD425-5B8C-4605-B775-7E4D9D4C11DB", "Activity", "Activity", "The activity type to activate", 0, @"", "02D5A7A5-8781-46B4-B9FC-AF816829D240" ); // Rock.Workflow.Action.ActivateActivity:Activity

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "38907A90-1634-4A93-8017-619326A4A582", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "3809A78C-B773-440C-8E3F-A8E81D0DAE08" ); // Rock.Workflow.Action.ActivateActivity:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "234910F2-A0DB-4D7D-BAF7-83C880EF30AE" ); // Rock.Workflow.Action.UserEntryForm:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "C178113D-7C86-4229-8424-C6D0CF4A7E23" ); // Rock.Workflow.Action.UserEntryForm:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4EEAC6FA-B838-410B-A8DD-21A364029F5D", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "8E176D08-1ABB-4563-8BDA-0F0A5BA6E92B" ); // Rock.Workflow.Action.SetAttributeToInitiator:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4EEAC6FA-B838-410B-A8DD-21A364029F5D", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Person Attribute", "PersonAttribute", "The attribute to set to the initiator.", 0, @"", "6AF13055-0AF4-4EB9-9722-0CC02DE502FB" ); // Rock.Workflow.Action.SetAttributeToInitiator:Person Attribute

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "4EEAC6FA-B838-410B-A8DD-21A364029F5D", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "6066D937-E36B-4DB1-B752-673B0EACC6F0" ); // Rock.Workflow.Action.SetAttributeToInitiator:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "EEDA4318-F014-4A46-9C76-4C052EF81AA1", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "0CA0DDEF-48EF-4ABC-9822-A05E225DE26C" ); // Rock.Workflow.Action.CompleteWorkflow:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "EEDA4318-F014-4A46-9C76-4C052EF81AA1", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "25CAD4BE-5A00-409D-9BAB-E32518D89956" ); // Rock.Workflow.Action.CompleteWorkflow:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F1A39347-6FE0-43D4-89FB-544195088ECF", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "50B01639-4938-40D2-A791-AA0EB4F86847" ); // Rock.Workflow.Action.PersistWorkflow:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F1A39347-6FE0-43D4-89FB-544195088ECF", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Persist Immediately", "PersistImmediately", "This action will normally cause the workflow to be persisted (saved) once all the current activites/actions have completed processing. Set this flag to true, if the workflow should be persisted immediately. This is only required if a subsequent action needs a persisted workflow with a valid id.", 0, @"False", "290CAD05-F1B7-4D43-AF1B-45CF55147DCA" ); // Rock.Workflow.Action.PersistWorkflow:Persist Immediately

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F1A39347-6FE0-43D4-89FB-544195088ECF", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "86F795B0-0CB6-4DA4-9CE4-B11D0922F361" ); // Rock.Workflow.Action.PersistWorkflow:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "FB2981B7-7922-42E1-8ACF-7F63BB7989E6", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "0B768E17-C64A-4212-BAD5-8A16B9F05A5C" ); // Rock.Workflow.Action.AssignActivityToPerson:Active

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "FB2981B7-7922-42E1-8ACF-7F63BB7989E6", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "5C5F7DB4-51DE-4293-BD73-CABDEB6564AC" ); // Rock.Workflow.Action.AssignActivityToPerson:Order

            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "FB2981B7-7922-42E1-8ACF-7F63BB7989E6", "E4EAB7B2-0B76-429B-AFE4-AD86D7428C70", "Person", "Person", "The person to assign this activity to.", 0, @"", "7ED2571D-B1BF-4DB6-9D04-9B5D064F51D8" ); // Rock.Workflow.Action.AssignActivityToPerson:Person

            RockMigrationHelper.UpdateWorkflowType( false, true, "Minister On Call -Call Intake", "Workflow for managing minister on call requests.", "863AFED2-2876-4099-94DF-63C4C153D539", "Work", "fa fa-phone", 0, false, 2, "0557DFB5-F64E-4225-889A-3B0D3A584CA4" ); // Minister On Call -Call Intake

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Name of Caller", "caller", "Name of caller to MOC", 0, @"", "7060F09F-9D3D-47C8-B3AB-52B56A1C3C62" ); // Minister On Call -Call Intake:Name of Caller

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Home Phone", "HomePhone", "Phone Number of MOC caller.", 1, @"", "A0ECD58F-678B-4F5E-AAB2-FD7F26DABA73" ); // Minister On Call -Call Intake:Home Phone

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Cell Phone", "CellPhone", "Cellphone number of MOC caller.", 2, @"", "D2DA5901-D13F-463C-B7EE-FB70C0F097B5" ); // Minister On Call -Call Intake:Cell Phone

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Relationship to Southeast", "RelationshiptoSoutheast", "", 3, @"", "68E0BBF3-F36C-4BD5-AD8F-98D738AD712E" ); // Minister On Call -Call Intake:Relationship to Southeast

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Request", "Request", "Need or request of caller.", 4, @"", "3EFF7F80-EC1B-4F1D-AECB-BBBF99FBA5CD" ); // Minister On Call -Call Intake:Request

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Action Taken", "ActionTaken", "Action taken by MOC.", 5, @"", "FBEB6552-BEEE-43B0-B3CE-20464D7B6947" ); // Minister On Call -Call Intake:Action Taken

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Comments", "Comments", "", 6, @"", "EF4A708A-AB8E-43CB-99D4-32BF83C8D621" ); // Minister On Call -Call Intake:Comments

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Follow Up Needed", "FollowUpNeeded", "Select if follow up is needed for MOC caller.", 7, @"", "D6E548EC-A3D3-42A3-9EBC-B076AF575F2D" ); // Minister On Call -Call Intake:Follow Up Needed

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "E4EAB7B2-0B76-429B-AFE4-AD86D7428C70", "Minister On Call", "MinisterOnCall", "", 8, @"", "487E17DD-7975-4D89-8ACB-551D995CCA44" ); // Minister On Call -Call Intake:Minister On Call

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Follow Up Notes", "FollowUpNotes", "Notes for follow up pastor.", 9, @"", "A9EE27AE-795C-4679-AEB9-91465A7979B9" ); // Minister On Call -Call Intake:Follow Up Notes

            RockMigrationHelper.AddAttributeQualifier( "7060F09F-9D3D-47C8-B3AB-52B56A1C3C62", "ispassword", @"False", "8C9BCEF5-1E8C-4B51-9857-FADCFCACCD7C" ); // Minister On Call -Call Intake:Name of Caller:ispassword

            RockMigrationHelper.AddAttributeQualifier( "A0ECD58F-678B-4F5E-AAB2-FD7F26DABA73", "ispassword", @"False", "7437B2ED-8010-49D3-9511-9F559BEC8B6D" ); // Minister On Call -Call Intake:Home Phone:ispassword

            RockMigrationHelper.AddAttributeQualifier( "D2DA5901-D13F-463C-B7EE-FB70C0F097B5", "ispassword", @"False", "4589C88A-ED37-4D75-B076-D74AB99C5C82" ); // Minister On Call -Call Intake:Cell Phone:ispassword

            RockMigrationHelper.AddAttributeQualifier( "68E0BBF3-F36C-4BD5-AD8F-98D738AD712E", "fieldtype", @"ddl", "1FA50558-F6ED-4ED7-ACF7-583FBEE9D102" ); // Minister On Call -Call Intake:Relationship to Southeast:fieldtype

            RockMigrationHelper.AddAttributeQualifier( "68E0BBF3-F36C-4BD5-AD8F-98D738AD712E", "values", @"Member, Regular Attendee, Other", "AC5407E7-FF0A-447C-BDD0-4DD7644B1947" ); // Minister On Call -Call Intake:Relationship to Southeast:values

            RockMigrationHelper.AddAttributeQualifier( "3EFF7F80-EC1B-4F1D-AECB-BBBF99FBA5CD", "ispassword", @"False", "EDD45712-C3DA-4537-922E-4D9FA8FCF434" ); // Minister On Call -Call Intake:Request:ispassword

            RockMigrationHelper.AddAttributeQualifier( "FBEB6552-BEEE-43B0-B3CE-20464D7B6947", "ispassword", @"False", "D1BD78A5-0E03-4AAD-9273-BAF4C3DDE2B5" ); // Minister On Call -Call Intake:Action Taken:ispassword

            RockMigrationHelper.AddAttributeQualifier( "EF4A708A-AB8E-43CB-99D4-32BF83C8D621", "ispassword", @"False", "8D5AF7C9-B740-4CF3-AC42-3BA85E945595" ); // Minister On Call -Call Intake:Comments:ispassword

            RockMigrationHelper.AddAttributeQualifier( "D6E548EC-A3D3-42A3-9EBC-B076AF575F2D", "falsetext", @"No", "49C8C8B8-4752-4B76-88E5-19769997BB73" ); // Minister On Call -Call Intake:Follow Up Needed:falsetext

            RockMigrationHelper.AddAttributeQualifier( "D6E548EC-A3D3-42A3-9EBC-B076AF575F2D", "truetext", @"Yes", "B374351C-4713-4D87-9F94-BD1E2D8FD10D" ); // Minister On Call -Call Intake:Follow Up Needed:truetext

            RockMigrationHelper.AddAttributeQualifier( "A9EE27AE-795C-4679-AEB9-91465A7979B9", "ispassword", @"False", "073E8634-393C-4F27-A798-58EAC02651B2" ); // Minister On Call -Call Intake:Follow Up Notes:ispassword

            RockMigrationHelper.UpdateWorkflowActivityType( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", true, "Call Intake", "", true, 0, "EBA2C2C7-4165-4B23-BD6F-4BE51A3195DB" ); // Minister On Call -Call Intake:Call Intake

            RockMigrationHelper.UpdateWorkflowActivityType( "0557DFB5-F64E-4225-889A-3B0D3A584CA4", true, "Follow Up", "", false, 1, "C1B2A281-1061-4793-BDE4-7FAF043F9702" ); // Minister On Call -Call Intake:Follow Up

            RockMigrationHelper.UpdateWorkflowActionForm( @"", @"", "Submit^fdc397cd-8b4a-436e-bea1-bce2e6717c03^^Your information has been submitted successfully.|", "", false, "", "8E70350C-05A5-4E30-98A8-1F432B440CAF" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information

            RockMigrationHelper.UpdateWorkflowActionForm( @"", @"", "Submit^fdc397cd-8b4a-436e-bea1-bce2e6717c03^^Your information has been submitted successfully.|", "88C7D1CC-3478-4562-A301-AE7D4D7FFF6D", true, "", "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05" ); // Minister On Call -Call Intake:Follow Up:Update Notes

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "7060F09F-9D3D-47C8-B3AB-52B56A1C3C62", 0, true, false, true, "65B30BC9-F532-4594-8C26-E0F63B3E0A25" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Name of Caller

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "A0ECD58F-678B-4F5E-AAB2-FD7F26DABA73", 1, true, false, false, "AC7B69E0-88F8-48CF-8A74-FEEEABDEAB8B" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Home Phone

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "D2DA5901-D13F-463C-B7EE-FB70C0F097B5", 2, true, false, false, "DAC4C0DB-345E-43C0-8F98-D1B4C5B1D546" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Cell Phone

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "68E0BBF3-F36C-4BD5-AD8F-98D738AD712E", 3, true, false, true, "459AFC6E-7D90-4FF1-9C48-2FF0F2B6DD04" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Relationship to Southeast

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "3EFF7F80-EC1B-4F1D-AECB-BBBF99FBA5CD", 4, true, false, true, "540E46CE-808F-4C97-9E45-791739D8061E" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Request

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "FBEB6552-BEEE-43B0-B3CE-20464D7B6947", 5, true, false, true, "7F5ABCFA-511C-442B-8BF6-9AA3AE200BBC" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Action Taken

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "EF4A708A-AB8E-43CB-99D4-32BF83C8D621", 6, true, false, false, "D909F78C-2A43-41F2-B03A-6024EBB706F2" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Comments

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "D6E548EC-A3D3-42A3-9EBC-B076AF575F2D", 7, true, false, true, "50B5CECD-8261-4599-88D4-B785C458F98F" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Follow Up Needed

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "487E17DD-7975-4D89-8ACB-551D995CCA44", 8, false, true, false, "DA58D1EF-4D62-4A83-B4C8-021007250AB6" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Minister On Call

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "8E70350C-05A5-4E30-98A8-1F432B440CAF", "A9EE27AE-795C-4679-AEB9-91465A7979B9", 9, false, true, false, "2DB7EFA6-DBA3-489E-A395-B525A1627F6B" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Follow Up Notes

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "7060F09F-9D3D-47C8-B3AB-52B56A1C3C62", 0, true, true, false, "A4AB89C7-9365-43AE-8CA1-6F9F8593C698" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Name of Caller

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "A0ECD58F-678B-4F5E-AAB2-FD7F26DABA73", 1, true, true, false, "BC02018C-CC7D-4F0C-B856-C2D9BA53DD4D" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Home Phone

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "D2DA5901-D13F-463C-B7EE-FB70C0F097B5", 2, true, true, false, "38A82E46-BE1A-48E3-A261-56393A25CB56" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Cell Phone

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "68E0BBF3-F36C-4BD5-AD8F-98D738AD712E", 3, true, true, false, "45856D75-6F37-4D78-B8E5-D230896405F6" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Relationship to Southeast

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "3EFF7F80-EC1B-4F1D-AECB-BBBF99FBA5CD", 4, true, true, false, "AD0FD709-A5A3-4EBC-BE57-BB09F9EAE33D" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Request

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "FBEB6552-BEEE-43B0-B3CE-20464D7B6947", 5, true, true, false, "32A7539D-98BB-4A41-8386-BAC6EB5686AC" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Action Taken

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "EF4A708A-AB8E-43CB-99D4-32BF83C8D621", 6, true, true, false, "46BEED10-9724-4596-9C20-B2315EF75433" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Comments

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "D6E548EC-A3D3-42A3-9EBC-B076AF575F2D", 7, false, true, false, "5F824293-3093-4EDF-8DA9-EE9951AF34B9" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Follow Up Needed

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "487E17DD-7975-4D89-8ACB-551D995CCA44", 8, true, true, false, "8F66480F-38B1-4BE7-8F70-A31EE6CABDE8" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Minister On Call

            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "A9EE27AE-795C-4679-AEB9-91465A7979B9", 9, true, false, true, "F7209EB8-28AB-4DF9-A2E4-F9B9F7176527" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Follow Up Notes

            RockMigrationHelper.UpdateWorkflowActionType( "EBA2C2C7-4165-4B23-BD6F-4BE51A3195DB", "Check for Followup", 4, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "D6E548EC-A3D3-42A3-9EBC-B076AF575F2D", 1, "true", "97DAA2BA-8BCF-44D9-A35A-6C56D5381695" ); // Minister On Call -Call Intake:Call Intake:Check for Followup

            RockMigrationHelper.UpdateWorkflowActionType( "EBA2C2C7-4165-4B23-BD6F-4BE51A3195DB", "Follow Up Not Needed", 5, "EEDA4318-F014-4A46-9C76-4C052EF81AA1", true, true, "", "D6E548EC-A3D3-42A3-9EBC-B076AF575F2D", 2, "true", "9BDA0E9A-9540-41E9-8789-1FF37FC7BEA0" ); // Minister On Call -Call Intake:Call Intake:Follow Up Not Needed

            RockMigrationHelper.UpdateWorkflowActionType( "C1B2A281-1061-4793-BDE4-7FAF043F9702", "Close Workflow", 2, "EEDA4318-F014-4A46-9C76-4C052EF81AA1", true, false, "", "", 1, "", "DD31AD1A-63B5-4531-A084-6C0D41265028" ); // Minister On Call -Call Intake:Follow Up:Close Workflow

            RockMigrationHelper.UpdateWorkflowActionType( "C1B2A281-1061-4793-BDE4-7FAF043F9702", "Assign Activity", 0, "FB2981B7-7922-42E1-8ACF-7F63BB7989E6", true, false, "", "", 1, "", "B071F4D7-3AFC-42DA-9BC2-2407C1CD6255" ); // Minister On Call -Call Intake:Follow Up:Assign Activity

            RockMigrationHelper.UpdateWorkflowActionType( "EBA2C2C7-4165-4B23-BD6F-4BE51A3195DB", "Persist Workflow", 3, "F1A39347-6FE0-43D4-89FB-544195088ECF", true, false, "", "", 1, "", "B941E5B1-963C-4D72-988C-988C45E81529" ); // Minister On Call -Call Intake:Call Intake:Persist Workflow

            RockMigrationHelper.UpdateWorkflowActionType( "EBA2C2C7-4165-4B23-BD6F-4BE51A3195DB", "Set Workflow Name", 2, "36005473-BD5D-470B-B28D-98E6D7ED808D", true, false, "", "", 1, "", "036AD029-2CA3-4424-8C89-32C3D44F639B" ); // Minister On Call -Call Intake:Call Intake:Set Workflow Name

            RockMigrationHelper.UpdateWorkflowActionType( "EBA2C2C7-4165-4B23-BD6F-4BE51A3195DB", "Get Caller Information", 0, "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", true, false, "8E70350C-05A5-4E30-98A8-1F432B440CAF", "", 1, "", "31702C08-D0A5-4FC0-8D43-C2808B8C6536" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information

            RockMigrationHelper.UpdateWorkflowActionType( "C1B2A281-1061-4793-BDE4-7FAF043F9702", "Update Notes", 1, "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", true, false, "849FA5F4-E1DA-4ADD-82DB-DF3243DCAE05", "", 1, "", "95628DAB-ED23-4BE5-85E0-D5EF609DF08E" ); // Minister On Call -Call Intake:Follow Up:Update Notes

            RockMigrationHelper.UpdateWorkflowActionType( "EBA2C2C7-4165-4B23-BD6F-4BE51A3195DB", "Save MOC", 1, "4EEAC6FA-B838-410B-A8DD-21A364029F5D", true, false, "", "", 1, "", "6F1667F5-3DAB-4393-B74E-FA6CEB529CB0" ); // Minister On Call -Call Intake:Call Intake:Save MOC

            RockMigrationHelper.AddActionTypeAttributeValue( "31702C08-D0A5-4FC0-8D43-C2808B8C6536", "234910F2-A0DB-4D7D-BAF7-83C880EF30AE", @"False" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "31702C08-D0A5-4FC0-8D43-C2808B8C6536", "C178113D-7C86-4229-8424-C6D0CF4A7E23", @"" ); // Minister On Call -Call Intake:Call Intake:Get Caller Information:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "6F1667F5-3DAB-4393-B74E-FA6CEB529CB0", "6AF13055-0AF4-4EB9-9722-0CC02DE502FB", @"487e17dd-7975-4d89-8acb-551d995cca44" ); // Minister On Call -Call Intake:Call Intake:Save MOC:Person Attribute

            RockMigrationHelper.AddActionTypeAttributeValue( "6F1667F5-3DAB-4393-B74E-FA6CEB529CB0", "6066D937-E36B-4DB1-B752-673B0EACC6F0", @"" ); // Minister On Call -Call Intake:Call Intake:Save MOC:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "6F1667F5-3DAB-4393-B74E-FA6CEB529CB0", "8E176D08-1ABB-4563-8BDA-0F0A5BA6E92B", @"False" ); // Minister On Call -Call Intake:Call Intake:Save MOC:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "036AD029-2CA3-4424-8C89-32C3D44F639B", "5D95C15A-CCAE-40AD-A9DD-F929DA587115", @"" ); // Minister On Call -Call Intake:Call Intake:Set Workflow Name:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "036AD029-2CA3-4424-8C89-32C3D44F639B", "0A800013-51F7-4902-885A-5BE215D67D3D", @"False" ); // Minister On Call -Call Intake:Call Intake:Set Workflow Name:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "036AD029-2CA3-4424-8C89-32C3D44F639B", "93852244-A667-4749-961A-D47F88675BE4", @"7060f09f-9d3d-47c8-b3ab-52b56a1c3c62" ); // Minister On Call -Call Intake:Call Intake:Set Workflow Name:Text Value|Attribute Value

            RockMigrationHelper.AddActionTypeAttributeValue( "B941E5B1-963C-4D72-988C-988C45E81529", "50B01639-4938-40D2-A791-AA0EB4F86847", @"False" ); // Minister On Call -Call Intake:Call Intake:Persist Workflow:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "B941E5B1-963C-4D72-988C-988C45E81529", "86F795B0-0CB6-4DA4-9CE4-B11D0922F361", @"" ); // Minister On Call -Call Intake:Call Intake:Persist Workflow:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "B941E5B1-963C-4D72-988C-988C45E81529", "290CAD05-F1B7-4D43-AF1B-45CF55147DCA", @"True" ); // Minister On Call -Call Intake:Call Intake:Persist Workflow:Persist Immediately

            RockMigrationHelper.AddActionTypeAttributeValue( "97DAA2BA-8BCF-44D9-A35A-6C56D5381695", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // Minister On Call -Call Intake:Call Intake:Check for Followup:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "97DAA2BA-8BCF-44D9-A35A-6C56D5381695", "3809A78C-B773-440C-8E3F-A8E81D0DAE08", @"" ); // Minister On Call -Call Intake:Call Intake:Check for Followup:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "97DAA2BA-8BCF-44D9-A35A-6C56D5381695", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"C1B2A281-1061-4793-BDE4-7FAF043F9702" ); // Minister On Call -Call Intake:Call Intake:Check for Followup:Activity

            RockMigrationHelper.AddActionTypeAttributeValue( "9BDA0E9A-9540-41E9-8789-1FF37FC7BEA0", "25CAD4BE-5A00-409D-9BAB-E32518D89956", @"" ); // Minister On Call -Call Intake:Call Intake:Follow Up Not Needed:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "9BDA0E9A-9540-41E9-8789-1FF37FC7BEA0", "0CA0DDEF-48EF-4ABC-9822-A05E225DE26C", @"False" ); // Minister On Call -Call Intake:Call Intake:Follow Up Not Needed:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "B071F4D7-3AFC-42DA-9BC2-2407C1CD6255", "0B768E17-C64A-4212-BAD5-8A16B9F05A5C", @"False" ); // Minister On Call -Call Intake:Follow Up:Assign Activity:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "B071F4D7-3AFC-42DA-9BC2-2407C1CD6255", "5C5F7DB4-51DE-4293-BD73-CABDEB6564AC", @"" ); // Minister On Call -Call Intake:Follow Up:Assign Activity:Order

            RockMigrationHelper.AddActionTypePersonAttributeValue( "B071F4D7-3AFC-42DA-9BC2-2407C1CD6255", "7ED2571D-B1BF-4DB6-9D04-9B5D064F51D8", @"bcb45910-276b-4320-86f9-891bce4b6d89" ); // Minister On Call -Call Intake:Follow Up:Assign Activity:Person

            RockMigrationHelper.AddActionTypeAttributeValue( "95628DAB-ED23-4BE5-85E0-D5EF609DF08E", "234910F2-A0DB-4D7D-BAF7-83C880EF30AE", @"False" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Active

            RockMigrationHelper.AddActionTypeAttributeValue( "95628DAB-ED23-4BE5-85E0-D5EF609DF08E", "C178113D-7C86-4229-8424-C6D0CF4A7E23", @"" ); // Minister On Call -Call Intake:Follow Up:Update Notes:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "DD31AD1A-63B5-4531-A084-6C0D41265028", "25CAD4BE-5A00-409D-9BAB-E32518D89956", @"" ); // Minister On Call -Call Intake:Follow Up:Close Workflow:Order

            RockMigrationHelper.AddActionTypeAttributeValue( "DD31AD1A-63B5-4531-A084-6C0D41265028", "0CA0DDEF-48EF-4ABC-9822-A05E225DE26C", @"False" ); // Minister On Call -Call Intake:Follow Up:Close Workflow:Active

        }
        public override void Down()
        {

        }
    }
}

