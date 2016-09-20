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
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData;
using Rock;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.Security;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    ///
    /// </summary>
    public class FinancialTransactionsExtensionsController : Rock.Rest.ApiControllerBase
    {

        public const string NONCASH = "F64662E7-0E12-4604-BBB0-DB774AC3C830" ;

        /// <summary>
        /// Gets the contribution transactions.
        /// </summary>
        /// <param name="groupId">The group unique identifier.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/secc/FinancialTransactions/GetContributionTransactions/{groupId}" )]
        public DataSet GetContributionTransactions( int groupId, [FromBody]ContributionStatementOptions options )
        {
            return GetContributionTransactions( groupId, null, options );
        }

        /// <summary>
        /// Gets the contribution transactions.
        /// </summary>
        /// <param name="personId">The person unique identifier.</param>
        /// <param name="groupId">The group unique identifier.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        [Authenticate, Secured]
        [HttpPost]
        [System.Web.Http.Route( "api/secc/FinancialTransactions/GetContributionTransactions/{groupId}/{personId}" )]
        public DataSet GetContributionTransactions( int groupId, int? personId, [FromBody]ContributionStatementOptions options )
        {
            RockContext rockContext = new RockContext();
            FinancialTransactionService financialTransactionService = new FinancialTransactionService( rockContext );
            var qry = financialTransactionService.Queryable().Where( a => a.TransactionDateTime >= options.StartDate );

            if ( options.EndDate.HasValue )
            {
                qry = qry.Where( a => a.TransactionDateTime < options.EndDate.Value );
            }

            var transactionTypeContribution = Rock.Web.Cache.DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.TRANSACTION_TYPE_CONTRIBUTION.AsGuid() );
            if ( transactionTypeContribution != null )
            {
                int transactionTypeContributionId = transactionTypeContribution.Id;
                qry = qry.Where( a => a.TransactionTypeValueId == transactionTypeContributionId );
            }

            if ( personId.HasValue )
            {
                // get transactions for a specific person
                qry = qry.Where( a => a.AuthorizedPersonAlias.PersonId == personId.Value );
            }
            else
            {
                // get transactions for all the persons in the specified group that have specified that group as their GivingGroup
                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                var personIdList = groupMemberService.GetByGroupId( groupId ).Where( a => a.Person.GivingGroupId == groupId ).Select( s => s.PersonId ).ToList();

                qry = qry.Where( a => personIdList.Contains( a.AuthorizedPersonAlias.PersonId ) );
            }

            if ( options.AccountIds != null )
            {
                qry = qry.Where( a => a.TransactionDetails.Any( x => options.AccountIds.Contains( x.AccountId ) ) );
            }
            

            var selectQry = qry.Where(a => a.FinancialPaymentDetail == null ||
                                            (a.FinancialPaymentDetail != null && a.FinancialPaymentDetail.CurrencyTypeValue == null) || 
                                            ( a.FinancialPaymentDetail != null  && a.FinancialPaymentDetail.CurrencyTypeValue != null  && a.FinancialPaymentDetail.CurrencyTypeValue.Guid != new Guid(NONCASH))
                                     ).Select( a => new
            {
                a.TransactionDateTime,
                a.TransactionCode,
                CurrencyTypeValueName = a.FinancialPaymentDetail != null ? a.FinancialPaymentDetail.CurrencyTypeValue.Value : string.Empty,
                a.Summary,
                Details = a.TransactionDetails.Select( d => new
                {
                    d.AccountId,
                    AccountName = d.Account.Name,
                    ParentAccountId = d.Account != null?d.Account.ParentAccountId:null,
                    ParentAccountName = d.Account != null && d.Account.ParentAccount != null ? d.Account.ParentAccount.Name : null,
                    d.Summary,
                    d.Amount
                } ).OrderBy( x => x.AccountName ),
            } ).OrderBy( a => a.TransactionDateTime );

            DataTable dataTable = new DataTable( "contribution_transactions" );
            dataTable.Columns.Add( "TransactionDateTime", typeof( DateTime ) );
            dataTable.Columns.Add( "TransactionCode" );
            dataTable.Columns.Add( "CurrencyTypeValueName" );
            dataTable.Columns.Add( "Summary" );
            dataTable.Columns.Add( "Amount", typeof( decimal ) );
            dataTable.Columns.Add( "Details", typeof( DataTable ) );

            var list = selectQry.ToList();

            dataTable.BeginLoadData();
            foreach ( var fieldItems in list )
            {
                DataTable detailTable = new DataTable( "transaction_details" );
                detailTable.Columns.Add( "AccountId", typeof( int ) );
                detailTable.Columns.Add( "AccountName" );
                detailTable.Columns.Add( "Summary" );
                detailTable.Columns.Add( "Amount", typeof( decimal ) );

                // remove any Accounts that were not included (in case there was a mix of included and not included accounts in the transaction) and group them
                var transactionDetails = fieldItems.Details.Where( a => options.AccountIds.Contains( a.AccountId ) ).GroupBy(d => d.ParentAccountId ?? d.ParentAccountId).ToList();
                
                foreach ( var detail in transactionDetails )
                {
                    var detailArray = new object[] {
                        detail.Key,
                        detail.Max(d => d.ParentAccountName ?? d.AccountName),
                        detail.Aggregate(string.Empty, (x, d) => x + ", " + d.Summary),
                        detail.Sum(d => d.Amount)
                    };

                    detailTable.Rows.Add( detailArray );
                }
                var itemArray = new object[] {
                    fieldItems.TransactionDateTime,
                    fieldItems.TransactionCode,
                    fieldItems.CurrencyTypeValueName,
                    fieldItems.Summary,
                    transactionDetails.Sum(a => a.Sum(d => d.Amount)),
                    detailTable
                };

                dataTable.Rows.Add( itemArray );
            }

            dataTable.EndLoadData();

            DataSet dataSet = new DataSet();
            dataSet.Tables.Add( dataTable );

            return dataSet;
        }
        
        /// <summary>
        ///
        /// </summary>
        public class ContributionStatementOptions
        {
            /// <summary>
            /// Gets or sets the start date.
            /// </summary>
            /// <value>
            /// The start date.
            /// </value>
            public DateTime StartDate { get; set; }

            /// <summary>
            /// Gets or sets the end date.
            /// </summary>
            /// <value>
            /// The end date.
            /// </value>
            public DateTime? EndDate { get; set; }

            /// <summary>
            /// Gets or sets the account ids.
            /// </summary>
            /// <value>
            /// The account ids.
            /// </value>
            public List<int> AccountIds { get; set; }

            /// <summary>
            /// Gets or sets the person id.
            /// </summary>
            /// <value>
            /// The person id.
            /// </value>
            public int? PersonId { get; set; }

            /// <summary>
            /// Gets or sets the Person DataViewId to filter the statements to
            /// </summary>
            /// <value>
            /// The data view identifier.
            /// </value>
            public int? DataViewId { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether [include individuals with no address].
            /// </summary>
            /// <value>
            /// <c>true</c> if [include individuals with no address]; otherwise, <c>false</c>.
            /// </value>
            public bool IncludeIndividualsWithNoAddress { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether [order by postal code].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [order by postal code]; otherwise, <c>false</c>.
            /// </value>
            public bool OrderByPostalCode { get; set; }
        }
    }
}