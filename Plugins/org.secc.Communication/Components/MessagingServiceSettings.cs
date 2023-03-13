using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using org.secc.DevLib.Components;
using Rock;
using Rock.Attribute;
using Rock.Security;
using Rock.Web.Cache;

namespace org.secc.Communication.Components
{
    [Export(typeof(SettingsComponent))]
    [ExportMetadata("ComponentName", "SECC Messaging Settings")]
    [Description("Settings for integration with the SECC Messaging API")]

    [UrlLinkField("Messaging API Url", 
        Description = "URL To the Messaging API.",
        IsRequired = true,
        Category = "Messaging API", 
        Order = 0, 
        Key = ComponentAttributeKeys.MESSAGING_API_URL)]
    [EncryptedTextField("Messaging API Key", 
        Description = "Messaging API Function Key",
        IsRequired = true,
        Category =  "Messaging API", 
        Order = 1,
        Key = ComponentAttributeKeys.MESSAGING_API_KEY)]
    [DefinedTypeField("Twilio SMS Phone Numbers",
        Description = "Rock Defined Value that contains the list of Twilio SMS Numbers",
        IsRequired = false,
        Category = "SMS Configuration",
        Order = 2, 
        DefaultValue = Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM,
        Key = ComponentAttributeKeys.SMS_DEFINED_TYPE)]

    [CustomEnhancedListField(
        "Keyword Approver Security Roles",
        Description = "Security roles that acan approve keyword updates.",
        IsRequired = false,
        ListSource = @"
            SELECT g.[Guid] as [Value], g.[Name] as [Text]
            FROM dbo.[Group] g 
            INNER JOIN dbo.[GroupType] gt on g.GroupTypeId = gt.Id
            WHERE gt.[Guid] = 'aece949f-704c-483e-a4fb-93d5e4720c4c'
            ORDER BY g.[Order], g.[Name]",
        Category = "SMS Configuration",
        Key = ComponentAttributeKeys.KEYWORD_APPROVER_ROLES, 
        Order = 3
    )]

    public  class MessagingServiceSettings : SettingsComponent
    {
        public static class ComponentAttributeKeys
        {
            public const string MESSAGING_API_URL = "MessagingApiUrl";
            public const string MESSAGING_API_KEY = "MessagingApiKey";
            public const string SMS_DEFINED_TYPE = "SMSDefinedType";
            public const string KEYWORD_APPROVER_ROLES = "KeywordApproverRoles";
        }

        public SECCMessagingSettings GetSettings()
        {
            return new SECCMessagingSettings
            {
                MessagingUrl = GetAttributeValue( ComponentAttributeKeys.MESSAGING_API_URL ),
                MessagingKey = Encryption.DecryptString( GetAttributeValue( ComponentAttributeKeys.MESSAGING_API_KEY ) ),
                RockSMSNumbersDefinedType = DefinedTypeCache.Get( GetAttributeValue( ComponentAttributeKeys.SMS_DEFINED_TYPE ).AsGuid() ),
                ApproverGroupGuids = GetAttributeValue( ComponentAttributeKeys.KEYWORD_APPROVER_ROLES ).SplitDelimitedValues()
                    .Select( v => v.AsGuidOrNull() )
                    .Where( v => v.HasValue )
                    .Select( v => v.Value )
                    .ToList()
            };
        }

        public override string Name => "SECC Messaging";
    }
}
