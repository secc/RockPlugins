using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rock;
using Rock.Model;

namespace org.secc.Communication.Messaging.Model
{
    
    public class MessagingPhoneNumber
    {
        [JsonProperty( "id" )]
        public Guid Id { get; set; }
        [JsonProperty( "name" )]
        public string Name { get; set; }
        [JsonProperty( "sid" )]
        public string Sid { get; set; }
        [JsonProperty( "isactive" )]
        public bool IsActive { get; set; }
        [JsonProperty( "number" )]
        public string Number { get; set; }

        [JsonProperty( "keywords" )]
        public SortedList<int, Keyword> Keywords { get; set; } = new SortedList<int, Keyword>();

        [JsonIgnore]
        public int KeywordCount
        {
            get
            {
                return Keywords == null ? 0 : Keywords.Count();
            }
        }

        [JsonIgnore]
        public int ActiveKeywordCount
        {
            get
            {
                if ( Keywords == null )
                {
                    return 0;
                }

                return Keywords
                    .Where( k => k.Value.IsActive )
                    .Where( k => ( !k.Value.StartDate.HasValue || k.Value.StartDate <= RockDateTime.Now ) &&
                        ( !k.Value.EndDate.HasValue || k.Value.EndDate >= RockDateTime.Now ) )
                    .Count();
            }
        }
        [JsonIgnore]
        public string NumberFormatted
        {
            get
            {
                var twilioNumber = Number.Replace( "+1", String.Empty );
                return Rock.Model.PhoneNumber.FormattedNumber("1", twilioNumber, false );
            }
        }
    }

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
        [JsonProperty( "CreatedOnDate" )]
        public DateTime? CreatedOnDate { get; }
        [JsonProperty( "ModifiedOnDateTime" )]
        public DateTime? ModifiedOnDateTime { get; set; }
        [JsonProperty( "IsActive" )]
        public bool IsActive { get; set; }
    }
}
