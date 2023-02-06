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
        [JsonProperty( "description" )]
        public string Description { get; set; }
        [JsonProperty( "keywords" )]
        public SortedList<int, Keyword> Keywords { get; set; } = new SortedList<int, Keyword>();
        [JsonProperty("createdOn")]
        public DateTime? CreatedOnDateTime { get; set; }
        [JsonProperty("modifiedOn")]
        public DateTime? ModifiedOnDateTime { get; set; }
        [JsonProperty("createdBy")]
        public MessagingPerson CreatedBy { get; set; }
        [JsonProperty("modifiedBy")]
        public MessagingPerson ModifiedBy { get; set; }
        [JsonIgnore]
        public bool IsSystem
        {
            get
            {
                return KeywordCount > 0;
            }
        }

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
}
