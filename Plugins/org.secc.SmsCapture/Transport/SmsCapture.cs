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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using org.secc.SmsCapture.Model;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Logging;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.SmsCapture.Transport
{
    /// <summary>
    /// DEV/testing SMS transport that captures fully rendered outbound messages to a local
    /// table instead of sending them, then reports success to the communication pipeline so
    /// recipient statuses advance exactly as they would in production. Contains no Twilio SDK
    /// calls and no outbound HTTP whatsoever.
    /// </summary>
    [Description( "Captures SMS to a local table instead of sending. DEV/testing only." )]
    [Export( typeof( TransportComponent ) )]
    [ExportMetadata( "ComponentName", "SMS Capture" )]
    [BooleanField( "Enable Logging",
        Description = "Write a compact line to the Rock log for each captured message.",
        DefaultBooleanValue = true,
        Order = 0,
        Key = AttributeKey.EnableLogging )]
    [IntegerField( "Max Captured Messages",
        Description = "After each send the oldest captured messages beyond this cap are trimmed. Set to 0 to disable trimming.",
        IsRequired = false,
        DefaultIntegerValue = 5000,
        Order = 1,
        Key = AttributeKey.MaxCapturedMessages )]
    public class SmsCapture : TransportComponent
    {
        /// <summary>
        /// Keys to use for Component Attributes
        /// </summary>
        public static class AttributeKey
        {
            public const string EnableLogging = "EnableLogging";
            public const string MaxCapturedMessages = "MaxCapturedMessages";
        }

        /// <summary>
        /// Sends the specified rock message. This is the immediate path used by system
        /// communications, workflow SMS actions and SMS conversation replies.
        /// </summary>
        /// <param name="rockMessage">The rock message.</param>
        /// <param name="mediumEntityTypeId">The medium entity type identifier.</param>
        /// <param name="mediumAttributes">The medium attributes.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Send( RockMessage rockMessage, int mediumEntityTypeId, Dictionary<string, string> mediumAttributes, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // Component attribute values are an in-memory snapshot loaded when the component
            // singleton was constructed; reload so cap/logging changes made in the admin UI
            // take effect without an app restart (including on other web farm nodes).
            this.LoadAttributes();

            var smsMessage = rockMessage as RockSMSMessage;
            if ( smsMessage == null )
            {
                errorMessages.Add( "The message was not a valid RockSMSMessage." );
                return false;
            }

            // Validate From Number
            if ( smsMessage.FromNumber == null )
            {
                errorMessages.Add( "A From Number was not provided." );
                return false;
            }

            // Common Merge Fields
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, rockMessage.CurrentPerson );
            foreach ( var mergeField in rockMessage.AdditionalMergeFields )
            {
                mergeFields.AddOrReplace( mergeField.Key, mergeField.Value );
            }

            var attachmentBinaryFileIds = rockMessage.Attachments.Select( a => a.Id ).ToList().AsDelimited( "," );

            foreach ( var recipient in rockMessage.GetRecipients() )
            {
                try
                {
                    foreach ( var mergeField in mergeFields )
                    {
                        recipient.MergeFields.AddOrIgnore( mergeField.Key, mergeField.Value );
                    }

                    using ( var rockContext = new RockContext() )
                    {
                        CommunicationRecipient communicationRecipient = null;
                        if ( recipient.CommunicationRecipientId.HasValue )
                        {
                            communicationRecipient = new CommunicationRecipientService( rockContext ).Get( recipient.CommunicationRecipientId.Value );
                        }

                        string message = ResolveText( smsMessage.Message, smsMessage.CurrentPerson, communicationRecipient, smsMessage.EnabledLavaCommands, recipient.MergeFields, smsMessage.AppRoot, smsMessage.ThemeRoot );
                        Person recipientPerson = ( Person ) recipient.MergeFields.GetValueOrNull( "Person" );

                        // Create the communication record and capture using that if we have a person since a
                        // communication record requires a valid person (mirrors the Twilio transport structure).
                        if ( smsMessage.CreateCommunicationRecord && recipientPerson != null )
                        {
                            var communicationService = new CommunicationService( rockContext );

                            var createSMSCommunicationArgs = new CommunicationService.CreateSMSCommunicationArgs
                            {
                                FromPerson = smsMessage.CurrentPerson,
                                ToPersonAliasId = recipientPerson?.PrimaryAliasId,
                                Message = message,
                                FromPhone = smsMessage.FromNumber,
                                CommunicationName = smsMessage.CommunicationName,
                                ResponseCode = string.Empty,
                                SystemCommunicationId = smsMessage.SystemCommunicationId
                            };

                            Rock.Model.Communication communication = communicationService.CreateSMSCommunication( createSMSCommunicationArgs );

                            if ( smsMessage.CurrentPerson != null )
                            {
                                communication.CreatedByPersonAliasId = smsMessage.CurrentPerson.PrimaryAliasId;
                                communication.ModifiedByPersonAliasId = smsMessage.CurrentPerson.PrimaryAliasId;
                            }

                            // Since we just created a new communication record, we need to move any attachments from the rockMessage
                            // to the communication's attachments since the Send method below will be handling the capture.
                            if ( smsMessage.Attachments.Any() )
                            {
                                foreach ( var attachment in smsMessage.Attachments )
                                {
                                    communication.AddAttachment( new CommunicationAttachment { BinaryFileId = attachment.Id }, CommunicationType.SMS );
                                }
                            }

                            rockContext.SaveChanges();
                            Send( communication, mediumEntityTypeId, mediumAttributes );

                            communication.SendDateTime = RockDateTime.Now;
                            rockContext.SaveChanges();
                        }
                        else
                        {
                            var capture = new CapturedSms
                            {
                                FromNumber = smsMessage.FromNumber.Value,
                                ToNumber = recipient.To,
                                RecipientPersonAliasId = recipientPerson?.PrimaryAliasId,
                                Body = message,
                                AttachmentBinaryFileIds = attachmentBinaryFileIds,
                                CommunicationId = communicationRecipient?.CommunicationId,
                                CommunicationRecipientId = recipient.CommunicationRecipientId,
                                Source = "RockMessage"
                            };
                            new CapturedSmsService( rockContext ).Add( capture );

                            // ResolveText set communicationRecipient.SentMessage (when there is one); this persists it too.
                            rockContext.SaveChanges();
                            LogCapture( capture );
                        }
                    }
                }
                catch ( Exception ex )
                {
                    errorMessages.Add( ex.Message );
                    ExceptionLogService.LogException( ex );
                }
            }

            TrimCapturedMessages();

            return !errorMessages.Any();
        }

        /// <summary>
        /// Sends the specified communication. This is the bulk/queued path used by the
        /// communication wizard and the Send Communications job.
        /// </summary>
        /// <param name="communication">The communication.</param>
        /// <param name="mediumEntityTypeId">The medium entity type identifier.</param>
        /// <param name="mediumAttributes">The medium attributes.</param>
        public override void Send( Rock.Model.Communication communication, int mediumEntityTypeId, Dictionary<string, string> mediumAttributes )
        {
            // Component attribute values are an in-memory snapshot loaded when the component
            // singleton was constructed; reload so cap/logging changes made in the admin UI
            // take effect without an app restart (including on other web farm nodes).
            this.LoadAttributes();

            string fromPhone;
            var unprocessedRecipientCount = 0;
            var mergeFields = new Dictionary<string, object>();
            Person currentPerson = null;
            var personEntityTypeId = 0;
            var communicationCategoryId = 0;
            var communicationEntityTypeId = 0;
            string attachmentBinaryFileIds = null;

            using ( var rockContext = new RockContext() )
            {
                // Requery the Communication
                communication = new CommunicationService( rockContext ).Get( communication.Id );

                if ( communication != null &&
                    communication.Status == CommunicationStatus.Approved &&
                    ( !communication.FutureSendDateTime.HasValue || communication.FutureSendDateTime.Value.CompareTo( RockDateTime.Now ) <= 0 ) )
                {
                    unprocessedRecipientCount = new CommunicationRecipientService( rockContext ).Queryable()
                        .Where( r =>
                            r.CommunicationId == communication.Id &&
                            r.Status == CommunicationRecipientStatus.Pending &&
                            r.MediumEntityTypeId.HasValue &&
                            r.MediumEntityTypeId.Value == mediumEntityTypeId )
                        .Count();
                }

                if ( unprocessedRecipientCount == 0 )
                {
                    return;
                }

                fromPhone = communication.SMSFromDefinedValue?.Value;
                if ( string.IsNullOrWhiteSpace( fromPhone ) )
                {
                    // just in case we got this far without a From Number, throw an exception
                    throw new Exception( "A From Number was not provided for communication: " + communication.Id.ToString() );
                }

                currentPerson = communication.CreatedByPersonAlias?.Person;
                mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, currentPerson );

                personEntityTypeId = EntityTypeCache.Get<Person>().Id;
                communicationEntityTypeId = EntityTypeCache.Get<Rock.Model.Communication>().Id;
                communicationCategoryId = CategoryCache.Get( Rock.SystemGuid.Category.HISTORY_PERSON_COMMUNICATIONS.AsGuid(), rockContext ).Id;
                attachmentBinaryFileIds = communication.GetAttachmentBinaryFileIds( CommunicationType.SMS ).AsDelimited( "," );
            }

            var publicAppRoot = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" );

            var recipientFound = true;
            while ( recipientFound )
            {
                // make a new rockContext per recipient
                var recipient = GetNextPending( communication.Id, mediumEntityTypeId, communication.IsBulkCommunication );

                // This means we are done, break the loop
                if ( recipient == null )
                {
                    recipientFound = false;
                    continue;
                }

                CaptureCommunicationRecipient( communication, fromPhone, mergeFields, currentPerson, attachmentBinaryFileIds, personEntityTypeId, communicationCategoryId, communicationEntityTypeId, publicAppRoot, recipient );
            }

            TrimCapturedMessages();
        }

        /// <summary>
        /// Captures a single communication recipient's message and advances their status,
        /// mirroring the per-recipient structure of the Twilio transport (new RockContext per
        /// recipient, same status/History side effects) minus the API call.
        /// </summary>
        private void CaptureCommunicationRecipient( Rock.Model.Communication communication, string fromPhone, Dictionary<string, object> mergeFields, Person currentPerson, string attachmentBinaryFileIds, int personEntityTypeId, int communicationCategoryId, int communicationEntityTypeId, string publicAppRoot, CommunicationRecipient recipient )
        {
            using ( var rockContext = new RockContext() )
            {
                try
                {
                    recipient = new CommunicationRecipientService( rockContext ).Get( recipient.Id );
                    var smsNumber = recipient.PersonAlias.Person.PhoneNumbers.GetFirstSmsNumber();
                    if ( !string.IsNullOrWhiteSpace( smsNumber ) )
                    {
                        // Create merge field dictionary
                        var mergeObjects = recipient.CommunicationMergeValues( mergeFields );

                        string message = ResolveText( communication.SMSMessage, currentPerson, recipient, communication.EnabledLavaCommands, mergeObjects, publicAppRoot );

                        var capture = new CapturedSms
                        {
                            FromNumber = fromPhone,
                            ToNumber = smsNumber,
                            RecipientPersonAliasId = recipient.PersonAliasId,
                            Body = message,
                            AttachmentBinaryFileIds = attachmentBinaryFileIds,
                            CommunicationId = communication.Id,
                            CommunicationRecipientId = recipient.Id,
                            Source = "Communication"
                        };
                        new CapturedSmsService( rockContext ).Add( capture );

                        recipient.Status = CommunicationRecipientStatus.Delivered;
                        recipient.StatusNote = "Captured by SMS Capture transport";
                        recipient.SendDateTime = RockDateTime.Now;
                        recipient.TransportEntityTypeName = this.GetType().FullName;
                        recipient.UniqueMessageId = capture.Guid.ToString();

                        try
                        {
                            var historyService = new HistoryService( rockContext );
                            historyService.Add( new History
                            {
                                CreatedByPersonAliasId = communication.SenderPersonAliasId,
                                EntityTypeId = personEntityTypeId,
                                CategoryId = communicationCategoryId,
                                EntityId = recipient.PersonAlias.PersonId,
                                Verb = History.HistoryVerb.Sent.ConvertToString().ToUpper(),
                                ChangeType = History.HistoryChangeType.Record.ToString(),
                                ValueName = "SMS message",
                                Caption = message.Truncate( 200 ),
                                RelatedEntityTypeId = communicationEntityTypeId,
                                RelatedEntityId = communication.Id
                            } );
                        }
                        catch ( Exception ex )
                        {
                            ExceptionLogService.LogException( ex, null );
                        }

                        LogCapture( capture );
                    }
                    else
                    {
                        recipient.Status = CommunicationRecipientStatus.Failed;
                        recipient.StatusNote = "No Phone Number with Messaging Enabled";
                    }
                }
                catch ( Exception ex )
                {
                    recipient.Status = CommunicationRecipientStatus.Failed;
                    recipient.StatusNote = "SMS Capture Exception: " + ex.Message;
                }

                rockContext.SaveChanges();
            }
        }

        private CommunicationRecipient GetNextPending( int communicationId, int mediumEntityId, bool isBulkCommunication )
        {
            using ( var rockContext = new RockContext() )
            {
                var recipient = Rock.Model.Communication.GetNextPending( communicationId, mediumEntityId, rockContext );
                if ( ValidRecipient( recipient, isBulkCommunication ) )
                {
                    return recipient;
                }
                else
                {
                    rockContext.SaveChanges();
                    return GetNextPending( communicationId, mediumEntityId, isBulkCommunication );
                }
            }
        }

        /// <summary>
        /// Opportunistically trims the oldest captured messages beyond the Max Captured Messages cap.
        /// </summary>
        private void TrimCapturedMessages()
        {
            var maxMessages = GetAttributeValue( AttributeKey.MaxCapturedMessages ).AsIntegerOrNull() ?? 5000;
            if ( maxMessages <= 0 )
            {
                return;
            }

            try
            {
                using ( var rockContext = new RockContext() )
                {
                    var capturedSmsService = new CapturedSmsService( rockContext );
                    var excess = capturedSmsService.Queryable()
                        .OrderByDescending( c => c.Id )
                        .Skip( maxMessages )
                        .ToList();

                    if ( excess.Any() )
                    {
                        capturedSmsService.DeleteRange( excess );
                        rockContext.SaveChanges();

                        if ( GetAttributeValue( AttributeKey.EnableLogging ).AsBooleanOrNull() ?? true )
                        {
                            RockLogger.Log.Information( RockLogDomains.Communications,
                                "SMS Capture: trimmed {trimmedCount} captured messages over cap of {maxMessages}",
                                excess.Count, maxMessages );
                        }
                    }
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }
        }

        private void LogCapture( CapturedSms capture )
        {
            if ( GetAttributeValue( AttributeKey.EnableLogging ).AsBooleanOrNull() ?? true )
            {
                RockLogger.Log.Information( RockLogDomains.Communications,
                    "SMS Capture: To={toNumber} From={fromNumber} CommunicationId={communicationId} Source={source} Guid={guid}",
                    capture.ToNumber, capture.FromNumber, capture.CommunicationId, capture.Source, capture.Guid );
            }
        }
    }
}
