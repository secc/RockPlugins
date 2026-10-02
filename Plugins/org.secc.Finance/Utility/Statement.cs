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
using System.Data.Entity;
using System.Linq;
using Rock.Lava;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Finance.Utility
{
    public class Statement
    {
        public static void AddMergeFields( Dictionary<string, object> mergeFields, Person targetPerson, DateRange dateRange, List<Guid> excludedCurrencyTypes, List<Guid> accountGuids = null )
        {
            excludedCurrencyTypes = excludedCurrencyTypes ?? new List<Guid>();

            RockContext rockContext = new RockContext();

            FinancialTransactionDetailService financialTransactionDetailService = new FinancialTransactionDetailService( rockContext );

            // fetch all the possible PersonAliasIds that have this GivingID to help optimize the SQL
            var personAliasIds = new PersonAliasService( rockContext ).Queryable().Where( a => a.Person.GivingId == targetPerson.GivingId ).Select( a => a.Id ).ToList();

            // get the transactions for the person or all the members in the person's giving group (Family)
            var qry = financialTransactionDetailService.Queryable().AsNoTracking()
                        .Where( t => t.Transaction.AuthorizedPersonAliasId.HasValue && personAliasIds.Contains( t.Transaction.AuthorizedPersonAliasId.Value ) );

            qry = qry.Where( t => t.Transaction.TransactionDateTime.Value >= dateRange.Start && t.Transaction.TransactionDateTime.Value <= dateRange.End );

            if ( accountGuids == null )
            {
                qry = qry.Where( t => t.Account.IsTaxDeductible );
            }
            else
            {
                qry = qry.Where( t => accountGuids.Contains( t.Account.Guid ) );
            }
            qry = qry.OrderByDescending( t => t.Transaction.TransactionDateTime ).ThenByDescending( t => t.Id );

            mergeFields.Add( "StatementStartDate", dateRange.Start?.ToShortDateString() );
            mergeFields.Add( "StatementEndDate", dateRange.End?.ToShortDateString() );

            var familyGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY ).Id;
            var groupMemberQry = new GroupMemberService( rockContext ).Queryable( true ).Where( m => m.Group.GroupTypeId == familyGroupTypeId );

            int recordTypeValueIdBusiness = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_BUSINESS.AsGuid() ).Id;

            // get giving group members in order by family role (adult -> child) and then gender (male -> female)
            var givingGroup = new PersonService( rockContext ).Queryable( true ).AsNoTracking()
                                    .Where( p => p.GivingId == targetPerson.GivingId )
                                    .GroupJoin(
                                        groupMemberQry,
                                        p => p.Id,
                                        m => m.PersonId,
                                        ( p, m ) => new { p, m } )
                                    .SelectMany( x => x.m.DefaultIfEmpty(), ( y, z ) => new { Person = y.p, GroupMember = z } )
                                    .Select( p => new
                                    {
                                        FirstName = p.Person.NickName,
                                        LastName = p.Person.LastName,
                                        FamilyRoleOrder = p.GroupMember.GroupRole.Order,
                                        Gender = p.Person.Gender,
                                        PersonId = p.Person.Id,
                                        IsBusiness = p.Person.RecordTypeValueId == recordTypeValueIdBusiness
                                    } )
                                    .DistinctBy( p => p.PersonId )
                                    .OrderBy( p => p.FamilyRoleOrder ).ThenBy( p => p.Gender )
                                    .ToList();

            string salutation = string.Empty;

            if ( givingGroup.FirstOrDefault() != null && givingGroup.FirstOrDefault().IsBusiness )
            {
                //If it's a business use the last name
                salutation = givingGroup.FirstOrDefault().LastName;
            }
            else
            {
                if ( givingGroup.GroupBy( g => g.LastName ).Count() == 1 )
                {
                    salutation = string.Join( ", ", givingGroup.Select( g => g.FirstName ) ) + " " + givingGroup.FirstOrDefault().LastName;
                    if ( salutation.Contains( "," ) )
                    {
                        salutation = salutation.ReplaceLastOccurrence( ",", " &" );
                    }
                }
                else
                {
                    salutation = string.Join( ", ", givingGroup.Select( g => g.FirstName + " " + g.LastName ) );
                    if ( salutation.Contains( "," ) )
                    {
                        salutation = salutation.ReplaceLastOccurrence( ",", " &" );
                    }
                }
            }

            mergeFields.Add( "Salutation", salutation );

            var mailingAddress = targetPerson.GetMailingLocation();


            //Sometimes we have an address that isn't a mailing address but still need it
            //This is the fallback if there is no mailing address
            if ( mailingAddress == null )
            {
                var homeAddressGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuidOrNull();
                var workAddressGuid = Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_WORK.AsGuidOrNull();
                if ( homeAddressGuid.HasValue && workAddressGuid.HasValue )
                {
                    var homeAddressDv = DefinedValueCache.Get( homeAddressGuid.Value );
                    var workAddressDv = DefinedValueCache.Get( workAddressGuid.Value );
                    var family = targetPerson.GetFamilies();
                    var mailingLocations = family.SelectMany( f => f.GroupLocations )
                         .Where( l => l.GroupLocationTypeValueId == homeAddressDv.Id || l.GroupLocationTypeValueId == workAddressDv.Id )
                         .OrderBy( l => l.IsMappedLocation ? 0 : 1 )
                          .ThenBy( l => l.GroupLocationTypeValueId == homeAddressDv.Id ? 0 : 1 );
                    mailingAddress = mailingLocations.Select( l => l.Location ).FirstOrDefault();
                }
            }

            if ( mailingAddress != null )
            {
                mergeFields.Add( "StreetAddress1", mailingAddress.Street1 );
                mergeFields.Add( "StreetAddress2", mailingAddress.Street2 );
                mergeFields.Add( "City", mailingAddress.City );
                mergeFields.Add( "State", mailingAddress.State );
                mergeFields.Add( "PostalCode", mailingAddress.PostalCode );
                mergeFields.Add( "Country", mailingAddress.Country );
            }
            else
            {
                mergeFields.Add( "StreetAddress1", string.Empty );
                mergeFields.Add( "StreetAddress2", string.Empty );
                mergeFields.Add( "City", string.Empty );
                mergeFields.Add( "State", string.Empty );
                mergeFields.Add( "PostalCode", string.Empty );
                mergeFields.Add( "Country", string.Empty );
            }

            // Eager-load the navigations the lava and the in-memory AccountSummary grouping walk;
            // the query is AsNoTracking, so lazy loading of navigations is not reliable.
            // FinancialPaymentDetail is loaded for the currency split below, and its CurrencyTypeValue
            // because the Giving statement lava reads it on every row; left to lazy loading, that was
            // one extra query per gift (859 for one large household).
            var householdDetails = qry
                .Include( t => t.Transaction.FinancialPaymentDetail.CurrencyTypeValue )
                .Include( t => t.Account )
                .ToList();

            // Split the household's gifts by currency type in memory rather than in SQL. With the
            // currency filter in the query, SQL Server started from the currency types (every
            // check, card and ACH payment in the database) instead of this household, and the QCD
            // statement's 14 excluded types timed out at 30 seconds on every household. One
            // household's gifts for a statement period is a small list.
            List<FinancialTransactionDetail> transactionDetails;
            List<FinancialTransactionDetail> excludedTransactionDetails;

            if ( excludedCurrencyTypes.Count > 0 )
            {
                int noCurrencyTypeCount = SplitByCurrencyType( householdDetails, excludedCurrencyTypes, out transactionDetails, out excludedTransactionDetails );
                if ( noCurrencyTypeCount > 0 )
                {
                    ExceptionLogService.LogException( new MissingCurrencyTypeException( string.Format(
                        "Contribution statement for GivingId {0}: {1} gift line(s) with no currency type were printed with the statement's main gifts. Set the currency type on the gift and regenerate the statement.",
                        targetPerson.GivingId,
                        noCurrencyTypeCount ) ), System.Web.HttpContext.Current );
                }
            }
            else
            {
                transactionDetails = householdDetails;
                excludedTransactionDetails = new List<FinancialTransactionDetail>();
            }

            // The old code joined AttributeValue.Id to FinancialTransactionDetail.Id (wrong column), so
            // attributes never loaded. Bulk-load attributes for the details AND their parent transactions —
            // the statement lava reads values like CheckNumber off the parent FinancialTransaction.
            transactionDetails.LoadAttributes( rockContext );
            if ( excludedTransactionDetails.Any() )
            {
                excludedTransactionDetails.LoadAttributes( rockContext );
            }

            var parentTransactions = transactionDetails
                .Concat( excludedTransactionDetails )
                .Select( d => d.Transaction )
                .Where( t => t != null )
                .DistinctBy( t => t.Id )
                .ToList();
            parentTransactions.LoadAttributes( rockContext );

            mergeFields.Add( "TransactionDetails", transactionDetails );


            if ( excludedCurrencyTypes.Count > 0 )
            {
                mergeFields.Add( "ExcludedTransactionDetails", excludedTransactionDetails );
            }
            mergeFields.Add( "AccountSummary", transactionDetails.GroupBy( t => new { t.Account.Name, t.Account.PublicName, t.Account.Description } )
                                                .Select( s => new AccountSummary
                                                {
                                                    AccountName = s.Key.Name,
                                                    PublicName = s.Key.PublicName,
                                                    Description = s.Key.Description,
                                                    Total = s.Sum( a => a.Amount ),
                                                    Order = s.Max( a => a.Account.Order )
                                                } )
                                                .OrderBy( s => s.Order )
                                                .ToList() );
            // pledge information
            var pledges = new FinancialPledgeService( rockContext ).Queryable().AsNoTracking()
                                .Where( p => p.PersonAliasId.HasValue && personAliasIds.Contains( p.PersonAliasId.Value )
                                    && p.StartDate <= dateRange.End && p.EndDate >= dateRange.Start )
                                .GroupBy( p => p.Account )
                                .Select( g => new PledgeSummary
                                {
                                    AccountId = g.Key.Id,
                                    AccountName = g.Key.Name,
                                    PublicName = g.Key.PublicName,
                                    AmountPledged = g.Sum( p => p.TotalAmount ),
                                    PledgeStartDate = g.Min( p => p.StartDate ),
                                    PledgeEndDate = g.Max( p => p.EndDate )
                                } )
                                .ToList();

            // add detailed pledge information
            foreach ( var pledge in pledges )
            {
                var adjustedPledgeEndDate = pledge.PledgeEndDate.Value.Date;

                if ( adjustedPledgeEndDate != DateTime.MaxValue.Date )
                {
                    adjustedPledgeEndDate = adjustedPledgeEndDate.AddDays( 1 );
                }

                if ( adjustedPledgeEndDate > dateRange.End )
                {
                    adjustedPledgeEndDate = dateRange.End.Value;
                }

                if ( adjustedPledgeEndDate > RockDateTime.Now )
                {
                    adjustedPledgeEndDate = RockDateTime.Now;
                }

                pledge.AmountGiven = new FinancialTransactionDetailService( rockContext ).Queryable()
                                            .Where( t =>
                                                 t.AccountId == pledge.AccountId
                                                 && t.Transaction.AuthorizedPersonAliasId.HasValue && personAliasIds.Contains( t.Transaction.AuthorizedPersonAliasId.Value )
                                                 && t.Transaction.TransactionDateTime >= pledge.PledgeStartDate
                                                 && t.Transaction.TransactionDateTime < adjustedPledgeEndDate )
                                            .Sum( t => ( decimal? ) t.Amount ) ?? 0;

                pledge.AmountRemaining = ( pledge.AmountGiven > pledge.AmountPledged ) ? 0 : ( pledge.AmountPledged - pledge.AmountGiven );

                if ( pledge.AmountPledged > 0 )
                {
                    var test = ( double ) pledge.AmountGiven / ( double ) pledge.AmountPledged;
                    pledge.PercentComplete = ( int ) ( ( pledge.AmountGiven * 100 ) / pledge.AmountPledged );
                }
            }

            mergeFields.Add( "Pledges", pledges );

            var SqlParams = new List<System.Data.SqlClient.SqlParameter>()
            {
                new System.Data.SqlClient.SqlParameter("PersonAliasId", targetPerson.PrimaryAliasId),
                new System.Data.SqlClient.SqlParameter("@EndDate", dateRange.End.Value)
            };

            var moveSummary = rockContext.Database.SqlQuery<MoveCommitmentSummary>
                    ("[dbo].[_org_secc_Commitment_GetTotalsByPersonId] @PersonAliasID, @EndDate", SqlParams.ToArray())
                    .FirstOrDefault();

            mergeFields.Add("MoveSummary", moveSummary);
        }

        /// <summary>
        /// Splits a household's gifts, in statement order, into the statement's main gifts and those in an
        /// excluded currency type, in one pass. A gift with no currency type stays with the main gifts, as
        /// the old SQL filter kept it (so it isn't silently left off every statement).
        /// </summary>
        /// <returns>The number of gifts that had no currency type.</returns>
        private static int SplitByCurrencyType( List<FinancialTransactionDetail> householdDetails, List<Guid> excludedCurrencyTypes,
            out List<FinancialTransactionDetail> transactionDetails, out List<FinancialTransactionDetail> excludedTransactionDetails )
        {
            var excludedCurrencyTypeIds = new HashSet<int>( excludedCurrencyTypes
                .Select( g => DefinedValueCache.Get( g ) )
                .Where( dv => dv != null )
                .Select( dv => dv.Id ) );

            transactionDetails = new List<FinancialTransactionDetail>();
            excludedTransactionDetails = new List<FinancialTransactionDetail>();
            int noCurrencyTypeCount = 0;

            foreach ( var detail in householdDetails )
            {
                var currencyTypeValueId = detail.Transaction?.FinancialPaymentDetail?.CurrencyTypeValueId;
                if ( currencyTypeValueId == null )
                {
                    noCurrencyTypeCount++;
                    transactionDetails.Add( detail );
                }
                else if ( excludedCurrencyTypeIds.Contains( currencyTypeValueId.Value ) )
                {
                    excludedTransactionDetails.Add( detail );
                }
                else
                {
                    transactionDetails.Add( detail );
                }
            }

            return noCurrencyTypeCount;
        }
    }

    /// <summary>
    /// Logged when a contribution statement includes gifts that have no currency type, so these entries
    /// can be filtered in the Exception List.
    /// </summary>
    public class MissingCurrencyTypeException : Exception
    {
        public MissingCurrencyTypeException( string message ) : base( message )
        {
        }
    }


    /// <summary>
    /// Pledge Summary Class
    /// </summary>
    public class PledgeSummary : LavaDataObject
    {
        /// <summary>
        /// Gets or sets the pledge account identifier.
        /// </summary>
        /// <value>
        /// The pledge account identifier.
        /// </value>
        public int AccountId { get; set; }

        /// <summary>
        /// Gets or sets the pledge account.
        /// </summary>
        /// <value>
        /// The pledge account.
        /// </value>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets or sets the Public Name of the pledge account.
        /// </summary>
        /// <value>
        /// The Public Name of the pledge account.
        /// </value>
        public string PublicName { get; set; }

        /// <summary>
        /// Gets or sets the pledge start date.
        /// </summary>
        /// <value>
        /// The pledge start date.
        /// </value>
        public DateTime? PledgeStartDate { get; set; }

        /// <summary>
        /// Gets or sets the pledge end date.
        /// </summary>
        /// <value>
        /// The pledge end date.
        /// </value>
        public DateTime? PledgeEndDate { get; set; }

        /// <summary>
        /// Gets or sets the amount pledged.
        /// </summary>
        /// <value>
        /// The amount pledged.
        /// </value>
        public decimal AmountPledged { get; set; }

        /// <summary>
        /// Gets or sets the amount given.
        /// </summary>
        /// <value>
        /// The amount given.
        /// </value>
        public decimal AmountGiven { get; set; }

        /// <summary>
        /// Gets or sets the amount remaining.
        /// </summary>
        /// <value>
        /// The amount remaining.
        /// </value>
        public decimal AmountRemaining { get; set; }

        /// <summary>
        /// Gets or sets the percent complete.
        /// </summary>
        /// <value>
        /// The percent complete.
        /// </value>
        public int PercentComplete { get; set; }
    }

    /// <summary>
    /// Account Summary Class
    /// </summary>
    public class AccountSummary : LavaDataObject
    {
        /// <summary>
        /// Gets or sets the name of the account.
        /// </summary>
        /// <value>
        /// The name of the account.
        /// </value>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets or sets the public name of the account.
        /// </summary>
        /// <value>
        /// The public name of the account.
        /// </value>
        public string PublicName { get; set; }

        /// <summary>
        /// Gets or sets the description of the account.
        /// </summary>
        /// <value>
        /// The description of the account.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the total.
        /// </summary>
        /// <value>
        /// The total.
        /// </value>
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        /// <value>
        /// The order.
        /// </value>
        public int Order { get; set; }
    }

    public class MoveCommitmentSummary : LavaDataObject
    {
        public int PersonId { get; set; }
        public decimal AmountPledged { get; set; }
        public decimal AmountGiven { get; set; }
        public decimal SecondYearAmountGiven { get; set; }
        public int PledgeDuration { get; set; }
        public DateTime StatusDate { get; set; }
    }
}
