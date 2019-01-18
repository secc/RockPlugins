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
using System.Linq;
using System.Data;
using System.Data.Entity;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System.Collections.Generic;
using System;
using System.Reflection;
using Rock.Web.Cache;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace org.secc.Jobs
{
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Transaction Source", "The transaction source for PushPay transactions.", true)]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE, "Currency Type", "The currency type source for PushPay check transactions.", true )]
    [AttributeField( Rock.SystemGuid.EntityType.FINANCIAL_TRANSACTION, "Check Number Attribute", "The check number finacial transaction attribute.")]
    [SlidingDateRangeField( "Date Range", "The date range of transactions to include", true, "Previous|24|Hour||" )]
    [DisallowConcurrentExecution]
    public class PushPayDownloadCheckNumbers : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            // Load all of the attributes
            var transactionSource = DefinedValueCache.Get( dataMap.GetString( "TransactionSource" ).AsGuid() );
            var currencyType = DefinedValueCache.Get( dataMap.GetString( "CurrencyType" ).AsGuid() );
            var checkNumberAttribute = AttributeCache.Get( dataMap.GetString( "CheckNumberAttribute" ).AsGuid() );
            DateRange dateRange = Rock.Web.UI.Controls.SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.GetString( "DateRange" ) ?? "-1||" );

            FinancialTransactionService financialTransactionService = new FinancialTransactionService( rockContext );
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );

            // Fetch any transactions that don't have check numbers
            var checkTransactions = financialTransactionService.Queryable( "FinancialPaymentDetail" )
                                        .Where( ft => ft.SourceTypeValueId == transactionSource.Id 
                                                      && ft.FinancialPaymentDetail.CurrencyTypeValueId == currencyType.Id
                                                      && ft.CreatedDateTime >= dateRange.Start
                                                      && ft.CreatedDateTime <= dateRange.End )
                                        .GroupJoin( attributeValueService.Queryable(),
                                            ft => new { EntityId = ( int? ) ft.Id, AttributeId = checkNumberAttribute.Id },
                                            av => new { av.EntityId, AttributeId = av.AttributeId },
                                            ( ft, av ) => new { Transaction = ft, CheckNumberAttributes = av } )
                                        .Where( ft => ft.CheckNumberAttributes.Count() == 0 );
                                                   

            int updates = 0;
            int errors = 0;
            string errorMessages = "";

            if ( checkTransactions.Count() > 0 )
            {
                // First setup our PushPay Merchant data and get OAuth Tokens
                DataSet merchants = DbService.GetDataSet( "select AccountId, MerchantKey from _com_pushPay_RockRMS_Merchant", CommandType.Text, null );

                List<MerchantData> merchantDataList = new List<MerchantData>();

                foreach ( DataRow merchantRow in merchants.Tables[0].Rows )
                {
                    int accountId = merchantRow["AccountId"].ToString().AsInteger();
                    string merchantKey = merchantRow["MerchantKey"].ToString();

                    // Get an OAuth token for the account
                    Assembly assembly = Assembly.LoadFrom( System.Web.Hosting.HostingEnvironment.MapPath( "~/bin/com.pushpay.RockRMS.dll" ) );
                    Type pushpayApiType = assembly.GetType( "com.pushpay.RockRMS.PushpayApi" );
                    MethodInfo accessTokenMethodInfo = pushpayApiType.GetMethod( "GetAccessToken" );
                    string oAuthToken = Convert.ToString( accessTokenMethodInfo.Invoke( null, new object[] { accountId } ) );

                    MerchantData data = new MerchantData();
                    data.AccountId = accountId;
                    data.MerchantKey = merchantKey;
                    data.OAuthToken = oAuthToken;

                    merchantDataList.Add( data );

                }
                
                foreach ( var transaction in checkTransactions )
                {
                    string currentError = "";
                    Boolean setCheckNumber = false;
                    foreach( var merchantData in merchantDataList )
                    {
                        // Fetch the payment information from PushPay
                        var task = GetPayment( merchantData.OAuthToken, merchantData.MerchantKey, transaction.Transaction.ForeignKey );
                        task.Wait();
                        if ( !string.IsNullOrWhiteSpace( task.Result?.DepositedCheck?.CheckNumber ) )
                        {
                            transaction.Transaction.LoadAttributes( rockContext );
                            transaction.Transaction.SetAttributeValue( checkNumberAttribute.Key, task.Result?.DepositedCheck?.CheckNumber );
                            transaction.Transaction.SaveAttributeValues();
                            updates++;
                            setCheckNumber = true;
                            continue;
                        }
                        else
                        {
                            if ( task.Result.Error != null)
                            {
                                currentError = " (" + task.Result.Error + ")";
                            } else if ( string.IsNullOrWhiteSpace( task.Result?.DepositedCheck?.CheckNumber ) )
                            {
                                currentError = " (No Check Number)";
                            }
                        }
                    }
                    // If we get here without a check number we have a problem
                    if ( setCheckNumber == false) {
                        errorMessages += string.Format( " - Unable to fetch check number for PushPay transaction {0}.\n", transaction.Transaction.ForeignKey + currentError ); 
                        errors++;
                    }

                }
            }


            context.Result = string.Format( "Updated {0} Financial Transaction Check Numbers with {1} Error(s).\n\n{2}", updates, errors, errorMessages );
        }


        /// <summary>
        ///	Get Payment
        /// </summary>
        /// <remarks>
        ///	Get a payment details from a payment token
        /// </remarks>
        public async Task<Response> GetPayment(string oAuthToken, string merchantKey, string paymentToken)
        {
            using ( var client = new HttpClient() )
            {
                var requestUrl = string.Format( "/v1/merchant/{0}/payment/{1}", merchantKey, paymentToken);

                client.BaseAddress = new Uri( "https://api.pushpay.com" );
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add( new MediaTypeWithQualityHeaderValue( "application/json" ) );
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", oAuthToken );


                var httpResponse = await client.GetAsync( requestUrl );
                if ( httpResponse.IsSuccessStatusCode )
                {
                    var content = await httpResponse.Content.ReadAsStringAsync();

                    var response = JsonConvert.DeserializeObject<Response>(
                        content, new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        } );
                    return response;
                }
                else
                {
                    return new Response { Error = httpResponse.StatusCode + " Error" };
                }
            }
        }

    }

    class MerchantData
    {
        public int AccountId { get; set; }
        public string MerchantKey { get; set; }
        public string OAuthToken { get; set; }
    }
    
    public class Response
    {
        public string PaymentMethodType { get; set; }
        public string Source { get; set; }
        public DepositedCheck DepositedCheck { get; set; }
        [JsonIgnore]
        public string Error { get; set; }
    }

    public class DepositedCheck
    {
        public string RoutingNumber { get; set; }
        public string Reference { get; set; }
        public string BankName { get; set; }
        public string CheckNumber { get; set; }
    }
}
