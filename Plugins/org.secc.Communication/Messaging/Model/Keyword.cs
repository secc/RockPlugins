using System;
using Newtonsoft.Json;
using Rock.Data;
using Rock.Model;

namespace org.secc.Communication.Messaging.Model
{
    public class Keyword
    {
        [JsonProperty("Id")]
        public Guid Id { get; set; }
        [JsonProperty( "PhoneNumberId" )]
        public Guid PhoneNumberId { get; set; }
        [JsonProperty( "MessageToMatch" )]
        public string MessageToMatch { get; set; } = string.Empty;
        [JsonProperty( "CampaignCode" )]
        public string CampaignCode { get; set; }
        [JsonProperty("ResponseMessage")]
        public string ResponseMessage { get; set; }
        [JsonProperty( "StartDate" )]
        public DateTime? StartDate { get; set; }
        [JsonProperty( "EndDate" )]
        public DateTime? EndDate { get; set; }
        [JsonProperty( "CreatedOn" )]
        public DateTime? CreatedOnDateTime { get; set; }
        [JsonProperty( "CreatedBy" )]
        public MessagingPerson CreatedBy { get; set; }
        [JsonProperty( "ModifiedOn" )]
        public DateTime? ModifiedOnDateTime { get; set; }
        [JsonProperty("ModifiedBy")]
        public MessagingPerson ModifiedBy { get; set; }
        [JsonProperty( "IsActive" )]
        public bool IsActive { get; set; }


    }
}
