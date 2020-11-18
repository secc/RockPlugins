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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;

namespace org.secc.Rise.Model
{
    /// <summary>Locally stored description of webhook events in Rise</summary>
    /// <seealso cref="Rock.Data.Model{org.secc.Rise.Model.WebhookEvent}" />
    /// <seealso cref="Rock.Data.IRockEntity" />
    [Table( "_org_secc_Rise_WebhookEvent" )]
    [DataContract]
    public class WebhookEvent : Model<WebhookEvent>, IRockEntity
    {
        /// <summary>Gets or sets the event identifier.</summary>
        /// <value>The event identifier from Rise.</value>
        [DataMember]
        [MaxLength( 200 )]
        [Index( IsUnique = true )]
        public string EventId { get; set; }

        /// <summary>Gets or sets the content.</summary>
        /// <value>The  json content of the post.</value>
        [DataMember]
        public string Content { get; set; }

        /// <summary>Gets or sets the count.</summary>
        /// <value>The count of number of events sent from Rise.</value>
        [DataMember]
        public int Count { get; set; } = 1;
    }

    public partial class WebhookEventConfiguration : EntityTypeConfiguration<WebhookEvent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public WebhookEventConfiguration()
        {
            this.HasEntitySetName( "WebhookEvents" );
        }
    }
}
