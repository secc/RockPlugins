// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.RegistrationCart.Services
{
    /// <summary>
    /// Runs the "Discount Code Workflow" when a camper is added to the cart, the way SECC's
    /// RegistrationEntry fork ran it when a registration was entered: the entity is a dictionary
    /// with "RegistrationInstance", "RegistrationInfo", "ExistingDiscountCode",
    /// "ExistingDiscountPercentage" and "ExistingDiscountAmount", and the workflow applies a code
    /// by setting RegistrationInfo.DiscountCode (SECC's "Auto Apply Discount Code" action). The
    /// financial-assistance workflow uses it to make and apply a single-use FA- code.
    /// </summary>
    public static class DiscountCodeWorkflow
    {
        /// <summary>A code the workflow applied, with its value as the template defines it.</summary>
        public class AppliedCode
        {
            public string Code { get; set; }

            public decimal Amount { get; set; }

            /// <summary>As a fraction (0.25 = 25%), as Rock stores it.</summary>
            public decimal Percentage { get; set; }
        }

        /// <summary>
        /// Runs the workflow for one camper and returns the code it applied, or null when it
        /// applied none (or isn't configured). The camper's form answers are the registrant's
        /// field values. A workflow error is logged rather than stopping the add.
        /// </summary>
        public static AppliedCode Run( Guid? workflowTypeGuid, int registrationInstanceId, Person registrar, Person registrant, Dictionary<Guid, object> answers, decimal cost )
        {
            var workflowType = workflowTypeGuid.HasValue ? WorkflowTypeCache.Get( workflowTypeGuid.Value ) : null;
            if ( workflowType == null || workflowType.IsActive == false )
            {
                return null;
            }

            try
            {
                // Its own context: the workflow saves new discount codes and reloads the
                // template's discounts onto the instance it is given.
                using ( var rockContext = new RockContext() )
                {
                    var instance = new RegistrationInstanceService( rockContext ).Get( registrationInstanceId );
                    if ( instance == null )
                    {
                        return null;
                    }

                    var registrantInfo = new RegistrantInfo( instance, registrant )
                    {
                        PersonAliasGuid = registrant.PrimaryAlias?.Guid ?? Guid.Empty,
                        Cost = cost
                    };
                    foreach ( var fieldValue in RegistrationForms.AttributeFieldValues( instance.RegistrationTemplate, answers ) )
                    {
                        registrantInfo.FieldValues[fieldValue.Key] = fieldValue.Value;
                    }

                    var registrationInfo = new RegistrationInfo( registrar )
                    {
                        DiscountCode = string.Empty,
                        TotalCost = cost
                    };
                    registrationInfo.Registrants.Add( registrantInfo );

                    var entity = new Dictionary<string, object>
                    {
                        { "RegistrationInstance", instance },
                        { "RegistrationInfo", registrationInfo },
                        { "ExistingDiscountCode", string.Empty },
                        { "ExistingDiscountPercentage", 0.0m },
                        { "ExistingDiscountAmount", 0.0m }
                    };

                    var workflow = Rock.Model.Workflow.Activate( workflowType, $"{registrar.FullName} - Discount Code", rockContext );
                    new WorkflowService( rockContext ).Process( workflow, entity, out var errors );
                    if ( errors.Any() )
                    {
                        ExceptionLogService.LogException( new Exception( $"Registration Cart: the discount code workflow for {registrant.FullName} ({instance.Name}) reported: {errors.AsDelimited( "; " )}" ) );
                    }

                    var code = registrationInfo.DiscountCode;
                    if ( code.IsNullOrWhiteSpace() )
                    {
                        return null;
                    }

                    // Read the code back from the template rather than trusting the workflow's copy.
                    var discount = new RegistrationTemplateDiscountService( rockContext ).Queryable()
                        .FirstOrDefault( d => d.RegistrationTemplateId == instance.RegistrationTemplateId && d.Code == code );
                    if ( discount == null )
                    {
                        return null;
                    }

                    return new AppliedCode
                    {
                        Code = discount.Code,
                        Amount = discount.DiscountAmount,
                        Percentage = discount.DiscountPercentage
                    };
                }
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( new Exception( $"Registration Cart: the discount code workflow failed for {registrant.FullName}.", ex ) );
                return null;
            }
        }
    }
}
