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
using System.Linq;
using System.Text;

using Quartz;
using Rock.Attribute;
using org.secc.PayPalReporting.Engine;
using System.Data;
using org.secc.PayPalReporting.Model;
using org.secc.PayPalReporting.Data;
using Rock;
using Rock.Security;

namespace org.secc.PayPalReporting
{

    [TextField("Report Name", "Name of PayFlowPro Report/Template to retrieve.", true, "GivingReport")]
    [TextField("PayPal Report URL", "URL for paypal reporting (Update to change to test/prod mode)", true, "https://payments-reports.paypal.com/reportingengine")]
    [BooleanField("Fetch Fees", "Flag for indicating whether this import should fetch fees.", true, "PayPal API")]
    [EncryptedTextField("PayPal API Username", "Username for authenticating to the PayPal API", false, "", "PayPal API")]
    [EncryptedTextField("PayPal API Password", "Password for authenticating to the PayPal API", false, "", "PayPal API")]
    [EncryptedTextField("PayPal API Signature", "Signature for authenticating to the PayPal API", false, "", "PayPal API")]
    [SlidingDateRangeField( "Date Range", "The range of dates to import.", true, "Previous|24|Hour||" )]

    [DisallowConcurrentExecution]
    public class ImportData : IJob
    {
        PayPalReportingContext dbContext;
        TransactionService transactionService;

        /// <summary> 
        /// Empty constructor for job initialization
        /// <para>
        /// Jobs require a public empty constructor so that the
        /// scheduler can instantiate the class whenever it needs.
        /// </para>
        /// </summary>
        public ImportData()
        {
            dbContext = new PayPalReportingContext();
            transactionService = new TransactionService(dbContext);
        }

        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {

            // set the encryption protocols that are permissible for external SSL connections
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12;

            JobDataMap dataMap = context.JobDetail.JobDataMap;

            int totalTransactions = 0;
            int feesSuccessful = 0;
            int feesFailed = 0;

            Rock.Model.FinancialGatewayService fgs = new Rock.Model.FinancialGatewayService( new Rock.Data.RockContext() );
            List<Rock.Model.FinancialGateway> gateways = fgs.Queryable().Where( fg => fg.IsActive && fg.EntityType.Name.Contains( "PayFlowPro" ) ).ToList();
            foreach ( Rock.Model.FinancialGateway gateway in gateways )
            {
                XMLReport report = new XMLReport();
                report.Gateway = gateway;
                report.URL = dataMap.GetString( "PayPalReportURL" );
                report.name = dataMap.GetString( "ReportName" );

                DateRange dateRange = Rock.Web.UI.Controls.SlidingDateRangePicker.CalculateDateRangeFromDelimitedValues( dataMap.GetString( "DateRange" ) ?? "-1||" );
                DataTable data = report.RunReport( dateRange.Start ?? DateTime.Now.AddHours( 0 - 24 ), dateRange.End ?? DateTime.Now.Date.AddSeconds( -1 ) );

                foreach ( DataRow row in data.Rows )
                {
                    ProcessTransaction( row );
                }
                dbContext.SaveChanges();

                totalTransactions += data.Rows.Count;
            }


            // Now load all transaction fees
            if ( dataMap.GetString( "FetchFees" ).AsBoolean() )
            {
                if ( dataMap.GetString( "PayPalAPIUsername" ).IsNullOrWhiteSpace()
                    || dataMap.GetString( "PayPalAPIPassword" ).IsNullOrWhiteSpace()
                    || dataMap.GetString( "PayPalAPISignature" ).IsNullOrWhiteSpace() )
                {
                    throw new Exception( "In order to fetch fees, the PayPal API Username, Password, and Signature are required." );
                }

                String apiUsername = Encryption.DecryptString( dataMap.GetString( "PayPalAPIUsername" ) );
                String apiPassword = Encryption.DecryptString( dataMap.GetString( "PayPalAPIPassword" ) );
                String apiSignature = Encryption.DecryptString( dataMap.GetString( "PayPalAPISignature" ) );
                List<Transaction> zeroFeeTrans = transactionService.Queryable()
                    .Where( tx => tx.Fees == 0 && !tx.IsZeroFee && tx.MerchantTransactionId != "" ).ToList();
                API paypalAPI = new API();
                paypalAPI.Username = apiUsername;
                paypalAPI.Password = apiPassword;
                paypalAPI.Signature = apiSignature;

                foreach ( Transaction zeroFeeTran in zeroFeeTrans )
                {
                    Double fee = Convert.ToDouble( paypalAPI.GetTransactionFee( zeroFeeTran ) );
                    if ( fee != 0 )
                    {
                        zeroFeeTran.Fees = fee;
                        feesSuccessful++;
                    } else
                    {
                        feesFailed++;
                    }
                }
                dbContext.SaveChanges();
            }

            String message = "Payflow Transactions Retrieved: " + totalTransactions;

            if ( dataMap.GetString( "FetchFees" ).AsBoolean() )
            {
                message += " - Non-Zero Fee Transactions:" + feesSuccessful;
                message += " - Zero Fee Transactions:" + feesFailed;
            }

            context.Result = message;
        }


        /// <summary>
        /// Copy a data row to an actual Transaction entity
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        private Transaction ProcessTransaction(DataRow dr)
        {
            Transaction transaction = null;
            if (dr["Transaction ID"] != null)
            {
                transaction = transactionService.Get(dr["Transaction ID"].ToString());
                if (transaction == null)
                {
                    transaction = new Transaction();
                    transaction.GatewayTransactionId = dr["Transaction ID"].ToString();
                    transactionService.Add(transaction);
                }
            }
            else
            {
                throw new Exception("Transaction from PayPal is missing the Gateway Transaction ID.");
            }

            if (dr["Amount"] != null)
            {
                transaction.Amount = Convert.ToDouble(((decimal)dr["Amount"] / 100));
            }

            if (dr["Billing First Name"] != null)
            {
                transaction.BillingFirstName = dr["Billing First Name"].ToString();
            }

            if (dr["Billing Last Name"] != null)
            {
                transaction.BillingLastName = dr["Billing Last Name"].ToString();
            }

            if (dr["Comment1"] != null)
            {
                transaction.Comment1 = dr["Comment1"].ToString();
            }

            if (dr["Comment2"] != null)
            {
                transaction.Comment2 = dr["Comment2"].ToString();
            }

            if (dr["PayPal Fees"] != null)
            {
                transaction.Fees = 0;
            }

            if (dr["PayPal Transaction ID"] != null)
            {
                transaction.MerchantTransactionId = dr["PayPal Transaction ID"].ToString();
            }

            if (dr["Tender Type"] != null)
            {
                transaction.TenderType = dr["Tender Type"].ToString();
            }

            if (dr["Time"] != null)
            {
                transaction.TimeCreated = (DateTime)dr["Time"];
            }

            if (dr["Type"] != null)
            {
                transaction.Type = dr["Type"].ToString();
            }

            if (dr["Batch ID"] != null)
            {
                transaction.BatchId = Convert.ToInt32((decimal)dr["Batch ID"]);
            }
            return transaction;
        }
    }
}
