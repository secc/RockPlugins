﻿// <copyright>
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
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace RockWeb.Plugins.org_secc.Communication
{
    /// <summary>
    /// Sends SMS message
    /// </summary>
    [ActionCategory( "SECC > Communication" )]
    [Description( "Sends a SMS message. The recipient can either be a person or a phone number entered in the 'To' field." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SMS Send" )]






    [WorkflowTextOrAttribute( "From", "Attribute Value", "The phone number or an attribute that contains the person or phone number that ther text should be sent from (will default to organization number 733733). <span class='tip tip-lava'></span>", true, "", "", 0, "From",
        new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.PhoneNumberFieldType", "Rock.Field.Types.PersonFieldType", "Rock.Field.Types.DefinedValueFieldType" } )]
    [WorkflowTextOrAttribute( "Recipient", "Attribute Value", "The phone number or an attribute that contains the person or phone number that message should be sent to. <span class='tip tip-lava'></span>", true, "", "", 1, "To",
        new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.PersonFieldType", "Rock.Field.Types.GroupFieldType", "Rock.Field.Types.SecurityRoleFieldType", "Rock.Field.Types.PhoneNumberFieldType" } )]
    [WorkflowTextOrAttribute( "Message", "Attribute Value", "The message or an attribute that contains the message that should be sent. <span class='tip tip-lava'></span>", false, "", "", 2, "Message",
        new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.MemoFieldType" } )]
    [WorkflowAttribute( "Attachment", "Workflow attribute that contains the attachment to be added. Note that when sending attachments with MMS; jpg, gif, and png images are supported for all carriers. Support for other file types is dependent upon each carrier and device. So make sure to test sending this to different carriers and phone types to see if it will work as expected.", false, "", "", 3, null,
        new string[] { "Rock.Field.Types.FileFieldType", "Rock.Field.Types.ImageFieldType" } )]
    [BooleanField( name: "Save Communication History", description: "Should a record of this communication be saved. If a person is provided then it will save to the recipient's profile. If a phone number is provided then the communication record is saved but a communication recipient is not.", defaultValue: false, category: "", order: 4, key: "SaveCommunicationHistory" )]

    public class SendSms : ActionComponent
    {
        /// <summary> 
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var mergeFields = GetMergeFields( action );
            // Get the From value. Can be in the form of Text, Phone Number, Person or Defined type 
            string fromValue = string.Empty;
            int? fromId = null;

            fromValue = GetAttributeValue( action, "From", true );


            if ( !string.IsNullOrWhiteSpace( fromValue ) )
            {


                Guid? fromGuid = fromValue.AsGuidOrNull();

                DefinedTypeCache smsPhoneNumbers = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM );

                // If fromGuid is null but fromValue is not, then this is a phone number (match it up with the value from the DefinedType)
                if ( fromGuid == null )
                {
                    try
                    {
                        fromValue = PhoneNumber.CleanNumber( fromValue );
                        fromGuid = smsPhoneNumbers.DefinedValues.Where( dv => dv.Value.Right( 10 ) == fromValue.Right( 10 ) ).Select( dv => dv.Guid ).FirstOrDefault();
                        if ( fromGuid == Guid.Empty )
                        {
                            action.AddLogEntry( "Invalid sending number: Person or valid SMS phone number not found", true );
                        }
                    }
                    catch ( Exception e )
                    {
                        action.AddLogEntry( "Invalid sending number: Person or valid SMS phone number not found", true );
                        ExceptionLogService.LogException( e );
                    }
                }

                // At this point, fromGuid should either be a Person or a DefinedValue

                if ( fromGuid.HasValue )
                {
                    fromId = smsPhoneNumbers.DefinedValues.Where( dv => dv.Guid == fromGuid || dv.GetAttributeValue( "ResponseRecipient" ) == fromGuid.ToString() ).Select( dv => dv.Id ).FirstOrDefault();

                }
            }

            else
            {
                // The From number is required and was not entered
                action.AddLogEntry( "Invalid sending number: Person or valid SMS phone number not found", true );

            }

            // Get the recipients, Can be in the form of Text, Phone Number, Person, Group or Security role
            var recipients = new List<RockSMSMessageRecipient>();
            string toValue = GetAttributeValue( action, "To" );
            Guid guid = toValue.AsGuid();
            if ( !guid.IsEmpty() )
            {
                var attribute = AttributeCache.Get( guid, rockContext );
                if ( attribute != null )
                {
                    string toAttributeValue = action.GetWorkflowAttributeValue( guid );
                    if ( !string.IsNullOrWhiteSpace( toAttributeValue ) )
                    {
                        switch ( attribute.FieldType.Class )
                        {
                            case "Rock.Field.Types.TextFieldType":
                            case "Rock.Field.Types.PhoneNumberFieldType":
                                {
                                    var smsNumber = toAttributeValue;
                                    smsNumber = PhoneNumber.CleanNumber( smsNumber );
                                    recipients.Add( RockSMSMessageRecipient.CreateAnonymous( smsNumber, mergeFields ) );
                                    break;
                                }
                            case "Rock.Field.Types.PersonFieldType":
                                {
                                    Guid personAliasGuid = toAttributeValue.AsGuid();
                                    if ( !personAliasGuid.IsEmpty() )
                                    {
                                        var phoneNumber = new PersonAliasService( rockContext ).Queryable()
                                            .Where( a => a.Guid.Equals( personAliasGuid ) )
                                            .SelectMany( a => a.Person.PhoneNumbers )
                                            .Where( p => p.IsMessagingEnabled )
                                            .FirstOrDefault();

                                        if ( phoneNumber == null )
                                        {
                                            action.AddLogEntry( "Invalid Recipient: Person or valid SMS phone number not found", true );
                                        }
                                        else
                                        {
                                            var person = new PersonAliasService( rockContext ).GetPerson( personAliasGuid );

                                            var recipient = new RockSMSMessageRecipient( person, phoneNumber.ToSmsNumber(), mergeFields );
                                            recipients.Add( recipient );
                                            recipient.MergeFields.Add( recipient.PersonMergeFieldKey, person );
                                        }
                                    }
                                    break;
                                }

                            case "Rock.Field.Types.GroupFieldType":
                            case "Rock.Field.Types.SecurityRoleFieldType":
                                {
                                    int? groupId = toAttributeValue.AsIntegerOrNull();
                                    Guid? groupGuid = toAttributeValue.AsGuidOrNull();
                                    IQueryable<GroupMember> qry = null;

                                    // Handle situations where the attribute value is the ID
                                    if ( groupId.HasValue )
                                    {
                                        qry = new GroupMemberService( rockContext ).GetByGroupId( groupId.Value );
                                    }

                                    // Handle situations where the attribute value stored is the Guid
                                    else if ( groupGuid.HasValue )
                                    {
                                        qry = new GroupMemberService( rockContext ).GetByGroupGuid( groupGuid.Value );
                                    }
                                    else
                                    {
                                        action.AddLogEntry( "Invalid Recipient: No valid group id or Guid", true );
                                    }

                                    if ( qry != null )
                                    {
                                        foreach ( var person in qry
                                            .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                                            .Select( m => m.Person ) )
                                        {
                                            var phoneNumber = person.PhoneNumbers
                                                .Where( p => p.IsMessagingEnabled )
                                                .FirstOrDefault();
                                            if ( phoneNumber != null )
                                            {
                                                var recipientMergeFields = new Dictionary<string, object>( mergeFields );
                                                var recipient = new RockSMSMessageRecipient( person, phoneNumber.ToSmsNumber(), recipientMergeFields );
                                                recipients.Add( recipient );
                                                recipient.MergeFields.Add( recipient.PersonMergeFieldKey, person );
                                            }
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
            }
            else
            {
                if ( !string.IsNullOrWhiteSpace( toValue ) )
                {
                    recipients.Add( RockSMSMessageRecipient.CreateAnonymous( toValue.ResolveMergeFields( mergeFields ), mergeFields ) );
                }
            }

            // Get the message from the Message attribute.
            // NOTE: Passing 'true' as the checkWorkflowAttributeValue will also check the workflow AttributeValue
            // which allows us to remove the unneeded code.
            string message = GetAttributeValue( action, "Message", checkWorkflowAttributeValue: true );

            // Add the attachment (if one was specified)
            var attachmentBinaryFileGuid = GetAttributeValue( action, "Attachment", true ).AsGuidOrNull();
            BinaryFile binaryFile = null;

            if ( attachmentBinaryFileGuid.HasValue && attachmentBinaryFileGuid != Guid.Empty )
            {
                binaryFile = new BinaryFileService( rockContext ).Get( attachmentBinaryFileGuid.Value );
            }

            // Send the message
            if ( recipients.Any() && ( !string.IsNullOrWhiteSpace( message ) || binaryFile != null ) )
            {
                var smsMessage = new RockSMSMessage();
                smsMessage.SetRecipients( recipients );
                smsMessage.FromNumber = DefinedValueCache.Get( fromId.Value );
                smsMessage.Message = message;
                smsMessage.CreateCommunicationRecord = GetAttributeValue( action, "SaveCommunicationHistory" ).AsBoolean();
                smsMessage.CommunicationName = action.ActionTypeCache.Name;

                if ( binaryFile != null )
                {
                    smsMessage.Attachments.Add( binaryFile );
                }

                smsMessage.Send();
            }
            else
            {
                action.AddLogEntry( "Warning: No text or attachment was supplied so nothing was sent.", true );
            }

            return true;

        }

    }
}