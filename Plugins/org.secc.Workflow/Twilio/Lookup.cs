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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Twilio;
using Twilio.Rest.Lookups.V1;

namespace org.secc.Twilio
{
    [ActionCategory( "SECC > Twilio" )]
    [Description( "Make a Twilio Lookup API Call" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Lookup" )]
    [ComponentField( "Rock.Communication.TransportContainer, Rock", "Twilio Transport Container", "The Twilio transport container to use for the API credentials.")]
    [CustomCheckboxListField( "Lookup Services", "Select the lookup services to perform on this request.", "carrier^Carrier,caller-name^Caller Name,whitepages_pro_caller_id^Whitepages Pro Caller Id", true )]
    [WorkflowTextOrAttribute( "Phone Number", "Phone Number", "The phone number to lookup.", true, key:"PhoneNumber", fieldTypeClassNames: new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.PhoneNumberFieldType" } )]
    [WorkflowAttribute( "Results Attribute", "The attribute to store the results (JSON).", true, fieldTypeClassNames: new string[] { "Rock.Field.Types.TextFieldType" } )]
    class TwilioLookup : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {

            errorMessages = new List<string>();

            string twilioGuid = GetAttributeValue( action, "TwilioTransportContainer" );
            string pn = PhoneNumber.CleanNumber( GetAttributeValue( action, "PhoneNumber", true ));
            string[] lookupServices = GetAttributeValue( action, "LookupServices" ).SplitDelimitedValues();

            
            if ( !string.IsNullOrWhiteSpace( twilioGuid ) && !string.IsNullOrWhiteSpace( pn ))
            {
                // Add the country code if applicable
                if (pn.Length == 10)
                {
                    var definedValues = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.COMMUNICATION_PHONE_COUNTRY_CODE.AsGuid() ).DefinedValues.OrderBy( v => v.Order );
                    var countryCode = definedValues.Select( v => v.Value ).FirstOrDefault();
                    pn = countryCode + pn;
                }

                var twilio = Rock.Communication.TransportContainer.GetComponent( twilioGuid );
                if ( twilio != null )
                {
                    twilio.LoadAttributes();
                    string accountSid = twilio.GetAttributeValue( "SID" );
                    string authToken = twilio.GetAttributeValue( "Token" );
                    TwilioClient.Init( accountSid, authToken );

                    // Setup the lookup(s) to perform
                    var type = new List<string>();
                    var addOns = new List<string>();
                    foreach(string lookupService in lookupServices)
                    {
                        if (lookupService.ToLower().StartsWith("whitepages"))
                        {
                            addOns.Add( lookupService );
                        }
                        else
                        {
                            type.Add( lookupService );
                        }
                    }

                    // Now do the lookup
                    var phoneNumber = PhoneNumberResource.Fetch(
                        addOns:addOns,
                        type: type,
                        pathPhoneNumber: new global::Twilio.Types.PhoneNumber( "+" + pn )
                    );
                    
                    // Now store the target attribute
                    var targetAttribute = AttributeCache.Get( GetActionAttributeValue( action, "ResultsAttribute" ).AsGuid(), rockContext );
                    if ( targetAttribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
                    {
                        action.Activity.Workflow.SetAttributeValue( targetAttribute.Key, phoneNumber.ToJson() );
                    }
                    else if ( targetAttribute.EntityTypeId == new WorkflowActivity().TypeId )
                    {
                        action.Activity.SetAttributeValue( targetAttribute.Key, phoneNumber.ToJson() );
                    }
                }
            }
            return true;
        }
    }
}
