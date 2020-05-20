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
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Twilio.Rest.Api.V2010.Account;

namespace org.secc.Communication.Model
{
    [Table( "_org_secc_Communication_TwilioHistory" )]
    [DataContract]
    public partial class TwilioHistory : Rock.Data.Model<TwilioHistory>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [DataMember]
        [Index]
        [MaxLength( 64 )]
        public string SID { get; set; }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public DateTime? DateCreated { get; set; }

        [DataMember]
        public DateTime? DateSent { get; set; }

        [DataMember]
        public TwilioDirection Direction { get; set; }

        [DataMember]
        public string To { get; set; }

        [DataMember]
        public string From { get; set; }

        [DataMember]
        public TwilioStatus Status { get; set; }

        [DataMember]
        public decimal Price { get; set; }

        [DataMember]
        public int NumberOfSegments { get; set; }

    }

    public partial class TwilioHistoryConfiguration : EntityTypeConfiguration<TwilioHistory>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KioskTypeConfiguration"/> class.
        /// </summary>
        public TwilioHistoryConfiguration()
        {
            this.Property( o => o.Price ).HasPrecision( 12, 10 );
            this.HasEntitySetName( "TwilioHistories" );
        }
    }

    public enum TwilioDirection
    {
        Inbound = 0,
        OutboundApi = 1,
        OutboundCall = 2,
        OutboundReply = 3
    }

    public enum TwilioStatus
    {
        Accepted = 0,
        Queued = 1,
        Sending = 2,
        Sent = 3,
        Failed = 4,
        Delivered = 5,
        Undelivered = 6,
        Receiving = 7,
        Received = 8,
        Read = 9,
        PartiallyDelivered = 10,
        Scheduled = 11
    }
}
