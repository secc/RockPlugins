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
// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Data;
using System.Linq;
using PayPal.Payments.Common.Utility;
using PayPal.Payments.DataObjects;
using PayPal.Payments.Transactions;
using Rock.Attribute;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Web.Cache;
using Rock;

namespace org.secc.PayFlowPro
{
    /// <summary>
    /// SECC PayFlowPro Payment Gateway
    /// </summary>
    [Description( "SECC PayFlowPro Payment Gateway" )]
    [Export( typeof( GatewayComponent ) )]
    [ExportMetadata( "ComponentName", "SECC PayFlowPro" )]

    [TextField( "PayPal Partner", "", true, "", "", 0, "Partner" )]
    [TextField( "PayPal Merchant Login", "", true, "", "", 1, "Vendor" )]
    [TextField( "PayPal User", "", false, "", "", 2, "User" )]
    [TextField( "PayPal Password", "", true, "", "", 3, "Password", true )]
    [CustomRadioListField( "Mode", "Mode to use for transactions", "Live,Test", true, "Live", "", 4 )]

    public class Gateway : Rock.PayFlowPro.Gateway
    {
        #region Gateway Component Implementation

        /// <summary>
        /// One Time
        /// </summary>
        public const string TRANSACTION_FREQUENCY_FOUR_WEEKS = "B603E480-42C3-41D9-923B-17779C5909A8";

        /// <summary>
        /// Gets the supported payment schedules.
        /// </summary>
        /// <value>
        /// The supported payment schedules.
        /// </value>
        public override List<DefinedValueCache> SupportedPaymentSchedules
        {
            get
            {
                var values = new List<DefinedValueCache>();

                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY ) );
                //values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY ) );
                values.Add( DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY ) );
                return values;
            }
        }

        /// <summary>
        /// Set the recurring frequency pay period
        /// </summary>
        /// <param name="recurringInfo"></param>
        /// <param name="transactionFrequencyValue"></param>
        private void SetPayPeriod( RecurringInfo recurringInfo, DefinedValueCache transactionFrequencyValue )
        {
            recurringInfo.MaxFailPayments = 0;
            recurringInfo.Term = 0;
            var selectedFrequencyGuid = transactionFrequencyValue.Guid.ToString().ToUpper();
            switch ( selectedFrequencyGuid )
            {
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_ONE_TIME:
                    recurringInfo.PayPeriod = "YEAR";
                    recurringInfo.Term = 1;
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_WEEKLY:
                    recurringInfo.PayPeriod = "WEEK";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_BIWEEKLY:
                    recurringInfo.PayPeriod = "BIWK";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEMONTHLY:
                    recurringInfo.PayPeriod = "SMMO";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_MONTHLY:
                    recurringInfo.PayPeriod = "MONT";
                    break;
                case TRANSACTION_FREQUENCY_FOUR_WEEKS:
                    recurringInfo.PayPeriod = "FRWK";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_QUARTERLY:
                    recurringInfo.PayPeriod = "QTER";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_TWICEYEARLY:
                    recurringInfo.PayPeriod = "SMYR";
                    break;
                case Rock.SystemGuid.DefinedValue.TRANSACTION_FREQUENCY_YEARLY:
                    recurringInfo.PayPeriod = "YEAR";
                    break;
            }
        }

        /// <summary>
        /// Gets the payments that have been processed for any scheduled transactions
        /// </summary>
        /// <param name="financialGateway"></param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns></returns>
        public override List<Payment> GetPayments( FinancialGateway financialGateway, DateTime startDate, DateTime endDate, out string errorMessage )
        {
            // set the encryption protocols that are permissible for external SSL connections
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

            var reportingApi = new Rock.PayFlowPro.Reporting.Api(
                GetAttributeValue( financialGateway, "User" ),
                GetAttributeValue( financialGateway, "Vendor" ),
                GetAttributeValue( financialGateway, "Partner" ),
                GetAttributeValue( financialGateway, "Password" ),
                GetAttributeValue( financialGateway, "Mode" ).Equals( "Test", StringComparison.CurrentCultureIgnoreCase ) );

            // Query the PayFlowPro Recurring Billing Report for transactions that were processed during data range
            var recurringBillingParams = new Dictionary<string, string>();
            recurringBillingParams.Add( "start_date", startDate.ToString( "yyyy-MM-dd HH:mm:ss" ) );
            recurringBillingParams.Add( "end_date", endDate.ToString( "yyyy-MM-dd HH:mm:ss" ) );
            recurringBillingParams.Add( "include_declines", "false" );
            DataTable recurringBillingTable = reportingApi.GetReport( "RecurringBillingReport", recurringBillingParams, out errorMessage );
            if ( recurringBillingTable != null )
            {
                // The Recurring Billing Report items does not include the amounts for each transaction, so need 
                // to run a custom report to try and get the amount/tender type for each transaction
                var transactionCodes = new Dictionary<string, int>();
                var customParams = new Dictionary<string, string>();
                customParams.Add( "start_date", startDate.ToString( "yyyy-MM-dd HH:mm:ss" ) );
                customParams.Add( "end_date", endDate.ToString( "yyyy-MM-dd HH:mm:ss" ) );
                customParams.Add( "maximum_amount", "1000000" );
                customParams.Add( "results", "Approvals Only" );
                customParams.Add( "recurring_only", "true" );
                customParams.Add( "show_order_id", "false" );
                customParams.Add( "show_transaction_id", "true" );
                customParams.Add( "show_time", "false" );
                customParams.Add( "show_type", "false" );
                customParams.Add( "show_tender_type", "true" );
                customParams.Add( "show_account_number", "false" );
                customParams.Add( "show_expires", "false" );
                customParams.Add( "show_aba_routing_number", "false" );
                customParams.Add( "show_amount", "true" );
                customParams.Add( "show_result_code", "true" );
                customParams.Add( "show_response_msg", "false" );
                customParams.Add( "show_comment1", "false" );
                customParams.Add( "show_comment2", "false" );
                customParams.Add( "show_tax_amount", "false" );
                customParams.Add( "show_purchase_order", "false" );
                customParams.Add( "show_original_transaction_id", "false" );
                customParams.Add( "show_avs_street_match", "false" );
                customParams.Add( "show_avs_zip_match", "false" );
                customParams.Add( "show_invoice_number", "false" );
                customParams.Add( "show_authcode", "false" );
                customParams.Add( "show_batch_id", "false" );
                customParams.Add( "show_csc_match", "false" );
                customParams.Add( "show_billing_first_name", "false" );
                customParams.Add( "show_billing_last_name", "false" );
                customParams.Add( "show_billing_company_name", "false" );
                customParams.Add( "show_billing_address", "false" );
                customParams.Add( "show_billing_city", "false" );
                customParams.Add( "show_billing_state", "false" );
                customParams.Add( "show_billing_zip", "false" );
                customParams.Add( "show_billing_email", "true" );
                customParams.Add( "show_billing_country", "false" );
                customParams.Add( "show_shipping_first_name", "false" );
                customParams.Add( "show_shipping_last_name", "false" );
                customParams.Add( "show_shipping_address", "false" );
                customParams.Add( "show_shipping_city", "false" );
                customParams.Add( "show_shipping_state", "false" );
                customParams.Add( "show_shipping_zip", "false" );
                customParams.Add( "show_shipping_country", "false" );
                customParams.Add( "show_customer_code", "false" );
                customParams.Add( "show_freight_amount", "false" );
                customParams.Add( "show_duty_amount", "false" );
                DataTable customTable = reportingApi.GetReport( "CustomReport", customParams, out errorMessage );
                if ( customTable != null )
                {
                    for ( int i = 0; i < customTable.Rows.Count; i++ )
                    {
                        transactionCodes.Add( customTable.Rows[i]["Transaction Id"].ToString().Trim(), i );
                    }
                }

                var txns = new List<Payment>();

                var transactionIdParams = new Dictionary<string, string>();
                transactionIdParams.Add( "transaction_id", string.Empty );

                var creditCardTypes = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.FINANCIAL_CREDIT_CARD_TYPE.AsGuid() ).DefinedValues;

                foreach ( DataRow recurringBillingRow in recurringBillingTable.Rows )
                {
                    bool foundTxn = false;
                    string transactionId = recurringBillingRow["Transaction ID"].ToString().Trim();
                    decimal amount = decimal.MinValue;
                    string tenderType = string.Empty;
                    string email = null;

                    if ( transactionCodes.ContainsKey( transactionId ) )
                    {
                        int rowNumber = transactionCodes[transactionId];
                        amount = decimal.TryParse( customTable.Rows[rowNumber]["Amount"].ToString(), out amount ) ? ( amount / 100 ) : 0.0M;
                        tenderType = customTable.Rows[rowNumber]["Tender Type"].ToString();
                        email = customTable.Rows[rowNumber]["PayPal Email ID"].ToString();
                        foundTxn = true;
                    }
                    else
                    {
                        // If the custom report did not include the transaction, run a transactionIDSearch report to get the amount and tender type
                        transactionIdParams["transaction_id"] = transactionId;
                        DataTable transactionIdTable = reportingApi.GetSearch( "TransactionIDSearch", transactionIdParams, out errorMessage );
                        if ( transactionIdTable != null && transactionIdTable.Rows.Count == 1 )
                        {
                            amount = decimal.TryParse( transactionIdTable.Rows[0]["Amount"].ToString(), out amount ) ? ( amount / 100 ) : 0.0M;
                            tenderType = transactionIdTable.Rows[0]["Tender Type"].ToString();
                            foundTxn = true;
                        }
                    }

                    if ( foundTxn )
                    {
                        var payment = new Payment();
                        payment.Amount = amount;
                        payment.TransactionDateTime = recurringBillingRow["Time"].ToString().AsDateTime() ?? DateTime.MinValue;
                        payment.TransactionCode = recurringBillingRow["Transaction ID"].ToString().Trim();
                        payment.GatewayScheduleId = recurringBillingRow["Profile ID"].ToString();
                        payment.ScheduleActive = recurringBillingRow["Status"].ToString() == "Active";
                        payment.CreditCardTypeValue = creditCardTypes.Where( t => t.Value == tenderType ).FirstOrDefault();
                        if ( payment.CreditCardTypeValue != null)
                        {
                            payment.CurrencyTypeValue = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_CREDIT_CARD );
                        }
                        else
                        {
                            payment.CurrencyTypeValue = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.CURRENCY_TYPE_ACH );
                        }
                        if (!string.IsNullOrEmpty(email))
                        {
                            payment.Attributes = new Dictionary<string, string>();
                            payment.Attributes.Add( "CCEmail", email );
                        }
                        txns.Add( payment );
                    }
                    else
                    {
                        errorMessage = "The TransactionIDSearch report did not return a value for transaction: " + recurringBillingRow["Transaction ID"].ToString();
                        return null;
                    }
                }

                return txns;
            }

            errorMessage = "The RecurringBillingReport report did not return any data";
            return null;
        }

        #endregion
    }
}