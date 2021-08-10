using System;
using System.Net;
using org.secc.Communication.Model;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Twilio;
using Twilio.Base;
using Twilio.Rest.Api.V2010.Account;

namespace org.secc.Communication.Twilio
{
    public class TwilioDownloader
    {
        private string accountSid = "";
        private string authToken = "";

        public TwilioDownloader()
        {
            TransportComponent transportComponent = null;
            foreach ( var serviceEntry in TransportContainer.Instance.Components )
            {
                transportComponent = serviceEntry.Value.Value;
                var entityType = EntityTypeCache.Get( transportComponent.GetType() );
                if ( entityType != null && entityType.Guid.Equals( EntityTypeCache.Get( typeof( Rock.Communication.Transport.Twilio ) ).Guid ) )
                {
                    accountSid = transportComponent.GetAttributeValue( "SID" );
                    authToken = transportComponent.GetAttributeValue( "Token" );
                }
            }
        }

        public int SyncItems( DateTime dateTime )
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            TwilioClient.Init( accountSid, authToken );

            ResourceSet<MessageResource> messages = null;

            try
            {
                messages = MessageResource.Read( dateSent: dateTime );
            }
            catch ( Exception e )
            {
                ExceptionLogService.LogException( new Exception( "Could not download Twilio messages see inner exception for details.", e ) );
                return 0;
            }

            var count = 0;

            foreach ( var record in messages )
            {
                count++;
                using ( RockContext rockContext = new RockContext() )
                {
                    TwilioHistoryService twilioHistoryService = new TwilioHistoryService( rockContext );
                    twilioHistoryService.AddOrUpdateResource( record );
                    rockContext.SaveChanges();
                }
            }
            return count;
        }
    }
}
