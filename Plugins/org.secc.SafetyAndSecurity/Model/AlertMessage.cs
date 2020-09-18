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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Runtime.Serialization;
using Rock;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.SafetyAndSecurity.Model
{
    [Table( "_org_secc_SafetyAndSecurity_AlertMessage" )]
    [DataContract]
    public partial class AlertMessage : Rock.Data.Model<AlertMessage>, Rock.Security.ISecured, Rock.Data.IRockEntity
    {
        [DataMember]
        public int AlertNotificationId { get; set; }

        [LavaInclude]
        public AlertNotification AlertNotification { get; set; }

        [DataMember]
        public string Message { get; set; }

        private IQueryable<Person> GetRecipients()
        {
            RockContext rockContext = new RockContext();

            var audienceId = this.AlertNotification.AudienceValueId;
            var audience = DefinedValueCache.Get( audienceId );

            var dataViewGuid = audience.GetAttributeValue( "DataView" ).AsGuid();

            var dataViewService = new DataViewService( rockContext );
            var dataView = dataViewService.Get( dataViewGuid );

            int timeout = 30;
            List<string> errorMessages;
            var qry = dataView.GetQuery( null, timeout, out errorMessages );
            var recipients = qry.Select( e => ( Person ) e );

            return recipients;
        }
        public void SendCommunication( Guid fromDefinedValue )
        {
            // Get the From value
            var fromValue = DefinedValueCache.Get( fromDefinedValue );
            

            // Get the recipients
            var recipients = this.GetRecipients();

            // Get the message
            string message = this.Message;

            // Send the message
            if ( recipients.Any() && ( !string.IsNullOrWhiteSpace( message ) ) )
            {
                var smsMessage = new RockSMSMessage();
                smsMessage.FromNumber = fromValue;
                smsMessage.Message = $"{(this.AlertNotification.Title != null ? this.AlertNotification.Title + ": " : "")}{message}";
                smsMessage.CreateCommunicationRecord = true;
                smsMessage.communicationName = this.AlertNotification?.Title ?? "Alert Notification Message";

                foreach (var person in recipients.ToList())
                {
                    var mergeObject = new Dictionary<string, object> { { "Person", person } };
                    smsMessage.AddRecipient( new RockEmailMessageRecipient(person, mergeObject) );
                }

                smsMessage.Send();

            }

        }

        public partial class AlertMessageConfiguration : EntityTypeConfiguration<AlertMessage>
        {
            public AlertMessageConfiguration()
            {
                this.HasRequired<AlertNotification>( m => m.AlertNotification ).WithMany().HasForeignKey( m => m.AlertNotificationId ).WillCascadeOnDelete( true );
                this.HasEntitySetName( "AlertMessages" );
            }


        }



    }
}
