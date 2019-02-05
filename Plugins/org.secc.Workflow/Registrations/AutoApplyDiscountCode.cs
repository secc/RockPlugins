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

using Rock.Data;
using Rock.Model;
using Rock.Attribute;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock;
using System.Linq;
using System.Data.Entity;

namespace org.secc.Workflow.Registrations
{
    /// <summary>
    /// Activates all the actions for the current action's activity.
    /// </summary>
    [ActionCategory( "SECC > Registrations" )]
    [Description( "Applies a discount code to a RegistrationState (RegistrationInfo model) that has not yet been persisted." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Apply Discount Code to RegistrationState" )]

    [WorkflowTextOrAttribute( "Discount Code", "Discount Code", "The discount code. <span class='tip tip-lava'></span>", true, key:"DiscountCode" )]
    public class AutoApplyDiscountCode : ActionComponent
    {
        /// <summary>
        /// Applies a discount code to a registration entity
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            if ( entity is Dictionary<string,object> entityDictionary )
            {
                RegistrationInstance registrationInstanceState = ( RegistrationInstance) entityDictionary["RegistrationInstance"];
                RegistrationInfo registrationState = ( RegistrationInfo ) entityDictionary["RegistrationInfo"];

                if ( registrationState != null )
                {
                    registrationState.Registrants.ForEach( r => r.DiscountApplies = true );

                    RegistrationTemplateDiscount discount = null;
                    bool validDiscount = true;

                    string discountCode = GetAttributeValue( action, "DiscountCode", true ).ResolveMergeFields( GetMergeFields( action ) );
                    if ( !string.IsNullOrWhiteSpace( discountCode ) )
                    {
                        // Reload the discounts to make sure we have all the latest ones (workflow can create new codes)
                        RegistrationTemplateService registrationTemplateService = new RegistrationTemplateService( rockContext );
                        registrationInstanceState.RegistrationTemplate.Discounts = registrationTemplateService.Get( registrationInstanceState.RegistrationTemplate.Guid ).Discounts;
                        discount = registrationInstanceState.RegistrationTemplate.Discounts
                            .Where( d => d.Code.Equals( discountCode, StringComparison.OrdinalIgnoreCase ) )
                            .FirstOrDefault();

                        if ( discount == null )
                        {
                            validDiscount = false;
                            errorMessages.Add(string.Format( "'{0}' is not a valid discount code.", discountCode ));
                        }

                        if ( validDiscount && discount.MinRegistrants.HasValue && registrationState.RegistrantCount < discount.MinRegistrants.Value )
                        {
                            validDiscount = false;
                            errorMessages.Add( string.Format( "The '{0}' discount code requires at least {1} registrants.", discountCode,  discount.MinRegistrants.Value ));
                        }

                        if ( validDiscount && discount.StartDate.HasValue && RockDateTime.Today < discount.StartDate.Value )
                        {
                            validDiscount = false;
                            errorMessages.Add( string.Format( "The '{0}' discount code is not available yet.", discountCode ) );
                        }

                        if ( validDiscount && discount.EndDate.HasValue && RockDateTime.Today > discount.EndDate.Value )
                        {
                            validDiscount = false;
                            errorMessages.Add( string.Format( "The '{0}' discount code has expired.", discountCode ) );
                        }

                        if ( validDiscount && discount.MaxUsage.HasValue && registrationInstanceState != null )
                        {
                            var instances = new RegistrationService( rockContext )
                                .Queryable().AsNoTracking()
                                .Where( r =>
                                    r.RegistrationInstanceId == registrationInstanceState.Id &&
                                    ( !registrationState.RegistrationId.HasValue || r.Id != registrationState.RegistrationId.Value ) &&
                                    r.DiscountCode == discountCode )
                                .Count();
                            if ( instances >= discount.MaxUsage.Value )
                            {
                                validDiscount = false;
                                errorMessages.Add( string.Format( "The '{0}' discount code is no longer available.", discountCode ));
                            }
                        }

                        if ( validDiscount && discount.MaxRegistrants.HasValue )
                        {
                            for ( int i = 0; i < registrationState.Registrants.Count; i++ )
                            {
                                registrationState.Registrants[i].DiscountApplies = i < discount.MaxRegistrants.Value;
                            }
                        }
                    }
                    else
                    {
                        validDiscount = false;
                    }

                    registrationState.DiscountCode = validDiscount ? discountCode : string.Empty;
                    registrationState.DiscountPercentage = validDiscount ? discount.DiscountPercentage : 0.0m;
                    registrationState.DiscountAmount = validDiscount ? discount.DiscountAmount : 0.0m;

                    


                }
            }

            return true;
        }
    }
}