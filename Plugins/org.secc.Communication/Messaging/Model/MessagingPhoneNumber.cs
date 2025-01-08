using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rock;

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
        public List<Keyword> Keywords { get; set; } = new List<Keyword>();
        [JsonProperty( "createdOn" )]
        public DateTime? CreatedOnDateTime { get; set; }
        [JsonProperty( "modifiedOn" )]
        public DateTime? ModifiedOnDateTime { get; set; }
        [JsonProperty( "createdBy" )]
        public MessagingPerson CreatedBy { get; set; }
        [JsonProperty( "modifiedBy" )]
        public MessagingPerson ModifiedBy { get; set; }
        [JsonProperty( "ownedBy" )]
        public MessagingOwner OwnedBy { get; set; }

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
                if (Keywords == null)
                {
                    return 0;
                }

                return Keywords
                    .Where( k => k.IsActive )
                    .Where( k => (!k.StartDate.HasValue || k.StartDate <= RockDateTime.Now) &&
                        (!k.EndDate.HasValue || k.EndDate >= RockDateTime.Now) )
                    .Count();
            }
        }
        [JsonIgnore]
        public string NumberFormatted
        {
            get
            {
                var twilioNumber = Number.Replace( "+1", String.Empty );
                return Rock.Model.PhoneNumber.FormattedNumber( "1", twilioNumber, false );
            }
        }
    }
}
