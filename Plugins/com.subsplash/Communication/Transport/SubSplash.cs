using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Mail;
using com.subsplash.Model;
using Newtonsoft.Json;
using RestSharp;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;

namespace com.subsplash.Communcation.Transport
{
    /// <summary>
    /// Communication transport for sending push notifications using Subsplash
    /// </summary>
    [Description( "Sends a push notification through Subsplash API" )]
    [Export( typeof( TransportComponent ) )]
    [ExportMetadata( "ComponentName", "Subsplash" )]
    [UrlLinkField( "API Endpoint", "The endpoint for the API services.", true, "https://core.subsplash.com/push/v1" )]
    [TextField( "App Key", "The app key for your Subsplash app.", true )]
    [EncryptedTextField( "Auth Provider Id", "The authentication provider id guid issued to your organization by SubSplash.", true )]
    [EncryptedTextField( "JWT Token", "The WebServices authentication bearer token.", true )]
    class Subsplash : TransportComponent
    {

        /// <summary>
        /// Sends the specified rock message.
        /// </summary>
        /// <param name="rockMessage">The rock message.</param>
        /// <param name="mediumEntityTypeId">The medium entity type identifier.</param>
        /// <param name="mediumAttributes">The medium attributes.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Send( RockMessage rockMessage, int mediumEntityTypeId, Dictionary<string, string> mediumAttributes, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var pushMessage = rockMessage as RockPushMessage;
            if ( pushMessage != null )
            {
                // Get server key
                string serverKey = GetAttributeValue( "ServerKey" );
                //var sender = new Sender( serverKey );

                // Common Merge Field
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, rockMessage.CurrentPerson );
                foreach ( var mergeField in rockMessage.AdditionalMergeFields )
                {
                    mergeFields.AddOrReplace( mergeField.Key, mergeField.Value );
                }

                var recipients = rockMessage.GetRecipients();

                if ( pushMessage.SendSeperatelyToEachRecipient )
                {
                    foreach ( var recipient in recipients )
                    {
                        try
                        {
                            foreach ( var mergeField in mergeFields )
                            {
                                recipient.MergeFields.AddOrIgnore( mergeField.Key, mergeField.Value );
                            }

                            //PushMessage( sender, new List<string> { recipient.To }, pushMessage, recipient.MergeFields );
                        }
                        catch ( Exception ex )
                        {
                            errorMessages.Add( ex.Message );
                            ExceptionLogService.LogException( ex, null );
                        }
                    }
                }
                else
                {
                    try
                    {
                        //PushMessage( sender, recipients.Select( r => r.To ).ToList(), pushMessage, mergeFields );
                    }
                    catch ( Exception ex )
                    {
                        errorMessages.Add( ex.Message );
                        ExceptionLogService.LogException( ex, null );
                    }
                }
            }

            return !errorMessages.Any();

        }

        /// <summary>
        /// Sends the specified communication.
        /// </summary>
        /// <param name="communication">The communication.</param>
        /// <param name="mediumEntityTypeId">The medium entity type identifier.</param>
        /// <param name="mediumAttributes">The medium attributes.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Send( Communication communication, int mediumEntityTypeId, Dictionary<string, string> mediumAttributes )
        {
            using ( var communicationRockContext = new RockContext() )
            {
                // Requery the Communication
                communication = new CommunicationService( communicationRockContext )
                    .Queryable( "CreatedByPersonAlias.Person" )
                    .FirstOrDefault( c => c.Id == communication.Id );

                bool hasPendingRecipients;
                if ( communication != null &&
                    communication.Status == CommunicationStatus.Approved &&
                    ( !communication.FutureSendDateTime.HasValue || communication.FutureSendDateTime.Value.CompareTo( RockDateTime.Now ) <= 0 ) )
                {
                    var qryRecipients = new CommunicationRecipientService( communicationRockContext ).Queryable();
                    hasPendingRecipients = qryRecipients
                        .Where( r =>
                            r.CommunicationId == communication.Id &&
                            r.Status == CommunicationRecipientStatus.Pending &&
                            r.MediumEntityTypeId.HasValue &&
                            r.MediumEntityTypeId.Value == mediumEntityTypeId )
                        .Any();
                }
                else
                {
                    hasPendingRecipients = false;
                }

                if ( hasPendingRecipients )
                {
                    var currentPerson = communication.CreatedByPersonAlias?.Person;
                    var globalAttributes = GlobalAttributesCache.Get();
                    string publicAppRoot = globalAttributes.GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, currentPerson );


                    var personEntityTypeId = EntityTypeCache.Get( "Rock.Model.Person" ).Id;
                    var communicationEntityTypeId = EntityTypeCache.Get( "Rock.Model.Communication" ).Id;
                    var communicationCategoryId = CategoryCache.Get( Rock.SystemGuid.Category.HISTORY_PERSON_COMMUNICATIONS.AsGuid(), communicationRockContext ).Id;

                    var client = new RestClient( GetAttributeValue( "APIEndpoint" ) );

                    JsonSerializerSettings serializerSettings = new JsonSerializerSettings
                    {
                        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                    };

                    bool recipientFound = true;
                    while ( recipientFound )
                    {
                        // make a new rockContext per recipient
                        var recipientRockContext = new RockContext();
                        var recipient = Communication.GetNextPending( communication.Id, mediumEntityTypeId, recipientRockContext );
                        if ( recipient != null )
                        {
                            if ( ValidRecipient( recipient, communication.IsBulkCommunication ) )
                            {
                                try
                                {
                                    var personAlias = recipient.PersonAliasId;

                                    // Create merge field dictionary
                                    var mergeObjects = recipient.CommunicationMergeValues( mergeFields );

                                    var message = ResolveText( communication.PushMessage, currentPerson, communication.EnabledLavaCommands, mergeObjects, publicAppRoot );
                                    var title = ResolveText( communication.PushTitle, currentPerson, communication.EnabledLavaCommands, mergeObjects, publicAppRoot );
                                    var sound = ResolveText( communication.PushSound, currentPerson, communication.EnabledLavaCommands, mergeObjects, publicAppRoot );


                                    var notification = new subsplash.Model.Notification();
                                    notification.AppKey = GetAttributeValue( "AppKey" );
                                    notification.Body = title;
                                    notification.Title = title;
                                    notification.AdditionalDescription = message;
                                    notification.PublishedAt = RockDateTime.Now;
                                    notification.Embedded = new NotificationEmbedded();
                                    notification.Embedded.ExternalUser = new ExternalUser();
                                    notification.Embedded.ExternalUser.AuthProviderId = Encryption.DecryptString( GetAttributeValue("AuthProviderId" ) ).AsGuidOrNull();
                                    notification.Embedded.ExternalUser.UserIds = recipient.PersonAlias.Person.Aliases.Select( a => a.AliasPersonId.ToString() ).ToList();

                                    var sendPush = new RestRequest( "notifications", Method.POST );
                                    sendPush.AddHeader( "Content-Type", "application/json" );
                                    sendPush.AddHeader( "Authorization", "Bearer " + Encryption.DecryptString( GetAttributeValue( "JWTToken" ) ) );
                                    sendPush.RequestFormat = DataFormat.Json;
                                    sendPush.AddParameter( "application/json", JsonConvert.SerializeObject( notification, serializerSettings), ParameterType.RequestBody );


                                    var response = client.Execute( sendPush );

                                    ErrorResponse error = ( ErrorResponse ) JsonConvert.DeserializeObject( response.Content, typeof( ErrorResponse ), serializerSettings );

                                    if ( error != null && error.Errors != null )
                                    {
                                        recipient.StatusNote = string.Join( "\n", error.Errors.Select( e => e.Code + ": " + ( e.Message ?? e.Detail ) ).ToList() );
                                        recipient.Status = CommunicationRecipientStatus.Failed;
                                    } 
                                    else
                                    {
                                        recipient.SendDateTime = RockDateTime.Now;
                                        recipient.Status = CommunicationRecipientStatus.Delivered;
                                        subsplash.Model.Notification notificationResponse = ( subsplash.Model.Notification ) JsonConvert.DeserializeObject( response.Content, typeof( subsplash.Model.Notification ), serializerSettings );

                                        recipient.UniqueMessageId = notificationResponse.Id.ToString();

                                    }

                                    recipient.TransportEntityTypeName = this.GetType().FullName;

                                    try
                                    {
                                        var historyService = new HistoryService( recipientRockContext );
                                        historyService.Add( new History
                                        {
                                            CreatedByPersonAliasId = communication.SenderPersonAliasId,
                                            EntityTypeId = personEntityTypeId,
                                            CategoryId = communicationCategoryId,
                                            EntityId = recipient.PersonAlias.PersonId,
                                            Verb = History.HistoryVerb.Sent.ConvertToString().ToUpper(),
                                            ChangeType = History.HistoryChangeType.Record.ToString(),
                                            ValueName = "Push Notification",
                                            Caption = message.Truncate( 200 ),
                                            RelatedEntityTypeId = communicationEntityTypeId,
                                            RelatedEntityId = communication.Id
                                        } );
                                    }
                                    catch ( Exception ex )
                                    {
                                        ExceptionLogService.LogException( ex, null );
                                    }

                                }
                                catch ( Exception ex )
                                {
                                    recipient.Status = CommunicationRecipientStatus.Failed;
                                    recipient.StatusNote = "Subsplash Push Notification Exception: " + ex.Message;
                                }
                            }

                            recipientRockContext.SaveChanges();
                        }
                        else
                        {
                            recipientFound = false;
                        }
                    }
                }
            }
        }

    }
}
