using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
namespace org.secc.Communication.Messaging.Model
{
    public  class TwilioPhoneNumber 
    {
        [JsonProperty("sid")]
        public string Sid { get; set; }
        [JsonProperty("friendly_name")]
        public string FriendlyName { get; set; }
        [JsonProperty("phone_number")]
        public string PhoneNumber { get; set; }
        [JsonProperty("supports_mms")]
        public bool SupportsMMS { get; set; }
        [JsonProperty("supports_voice")]
        public bool SupportsVoice { get; set; }
        [JsonProperty("supports_sms")]
        public bool SupportsSMS { get; set; }
        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

    }
}
