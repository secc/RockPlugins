using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using org.secc.Communication.Data;
using Rock;
using Rock.Data;
using Twilio.Rest.Api.V2010.Account;

namespace org.secc.Communication.Model
{
    public class TwilioHistoryService : CommunicationDataService<TwilioHistory>
    {
        public TwilioHistoryService( RockContext context ) : base( context ) { }

        public TwilioHistory AddOrUpdateResource( MessageResource resource )
        {
            TwilioHistory history = this.GetBySID( resource.Sid );
            if ( history == null )
            {
                history = new TwilioHistory();
                Add( history );
            }

            history.SID = resource.Sid;
            history.Price = resource.Price ?? 0;
            history.Status = GetStatus( resource.Status );
            history.Body = resource.Body.IsNotNullOrWhiteSpace() ? resource.Body : history.Body;
            history.From = resource.From.ToString();
            history.To = resource.To.ToString();
            history.DateCreated = resource.DateCreated;
            history.DateSent = resource.DateSent;
            history.Direction = GetDirection( resource.Direction );
            history.NumberOfSegments = resource.NumSegments.AsInteger();

            return history;
        }

        public TwilioHistory GetBySID( string sid )
        {
            return this.Queryable().Where( th => th.SID == sid ).FirstOrDefault();
        }
        private TwilioDirection GetDirection( MessageResource.DirectionEnum dir )
        {
            var direction = dir.ToString();

            if ( direction == "inbound" )
            {
                return TwilioDirection.Inbound;
            }
            else if ( direction == "outbound-api" )
            {
                return TwilioDirection.OutboundApi;
            }
            else if ( direction == "outbound-call")
            {
                return TwilioDirection.OutboundCall;
            }
            else if ( direction == "outbound-reply" )
            {
                return TwilioDirection.OutboundReply;
            }
            else
            {
                return TwilioDirection.Inbound;
            }
        }

        //Converts from the library enum to our own enum
        //No need to load the Twilio library to use the enum
        //Works in a switch statement
        //Hard coded enum values for consistant DB
        private TwilioStatus GetStatus( MessageResource.StatusEnum stat )
        {
            var status = stat.ToString();
            if ( status == "delivered")
            {
                return TwilioStatus.Delivered;
            }
            else if ( status == "failed" )
            {
                return TwilioStatus.Failed;
            }
            else if ( status == "queued" )
            {
                return TwilioStatus.Queued;
            }
            else if ( status == "received" )
            {
                return TwilioStatus.Received;
            }
            else if ( status == "receiving" )
            {
                return TwilioStatus.Receiving;
            }
            else if ( status == "sending" )
            {
                return TwilioStatus.Sending;
            }
            else if ( status == "sent" )
            {
                return TwilioStatus.Sent;
            }
            else if ( status == "undelivered" )
            {
                return TwilioStatus.Undelivered;
            }
            else
            {
                return TwilioStatus.Queued;
            }
        }
    }
}