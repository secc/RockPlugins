// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using Newtonsoft.Json;
using Humanizer;

namespace org.secc.Rise.Response
{
    [Url( "webhooks" )]

    public class RiseWebhook : RiseBase
    {
        [JsonProperty( "targetUrl" )]
        public string TargetUrl { get; set; }

        [JsonProperty( "events" )]
        public List<string> Events { get; set; }

        [JsonProperty( "sharedSecret" )]
        public string SharedSecret { get; set; }

        [JsonProperty( "apiVersion" )]
        public string ApiVersion { get; set; }

        [JsonProperty( "url" )]
        public string Url { get; set; }

        public string EventsFormatted
        {
            get
            {
                return string.Join( ", ", Events.Select( e => e.Humanize() ) );
            }
        }
    }
}
