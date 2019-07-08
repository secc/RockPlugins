using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Finance.Utility
{
    public class Statement
    {
        public static void AddMergeFields(Dictionary<string,object> mergeFields, Person targetPerson, DateRange dateRange, List<Guid> excludedCurrencyTypes, List<Guid> accountGuids = null)
        {

            RockContext rockContext = new RockContext();

            FinancialTransactionDetailService financialTransactionDetailService = new FinancialTransactionDetailService(rockContext);

            // fetch all the possible PersonAliasIds that have this GivingID to help optimize the SQL
            var personAliasIds = new PersonAliasService(rockContext).Queryable().Where(a => a.Person.GivingId == targetPerson.GivingId).Select(a => a.Id).ToList();

            // get the transactions for the person or all the members in the person's giving group (Family)
            var qry = financialTransactionDetailService.Queryable().AsNoTracking()
                        .Where(t => t.Transaction.AuthorizedPersonAliasId.HasValue && personAliasIds.Contains(t.Transaction.AuthorizedPersonAliasId.Value));

            qry = qry.Where(t => t.Transaction.TransactionDateTime.Value >= dateRange.Start && t.Transaction.TransactionDateTime.Value <= dateRange.End);

            if (accountGuids == null)
            {
                qry = qry.Where(t => t.Account.IsTaxDeductible);
            }
            else
            {
                qry = qry.Where(t => accountGuids.Contains(t.Account.Guid));
            }
            var excludedQry = qry;
            if (excludedCurrencyTypes.Count > 0)
            {
                qry = qry.Where(t => !excludedCurrencyTypes.Contains(t.Transaction.FinancialPaymentDetail.CurrencyTypeValue.Guid));
                excludedQry = excludedQry.Where(t => excludedCurrencyTypes.Contains(t.Transaction.FinancialPaymentDetail.CurrencyTypeValue.Guid));
                excludedQry = excludedQry.OrderByDescending(t => t.Transaction.TransactionDateTime);
            }

            qry = qry.OrderByDescending(t => t.Transaction.TransactionDateTime);

            mergeFields.Add("StatementStartDate", dateRange.Start?.ToShortDateString());
            mergeFields.Add("StatementEndDate", dateRange.End?.ToShortDateString());

            var familyGroupTypeId = GroupTypeCache.Get(Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY).Id;
            var groupMemberQry = new GroupMemberService(rockContext).Queryable().Where(m => m.Group.GroupTypeId == familyGroupTypeId);

            // get giving group members in order by family role (adult -> child) and then gender (male -> female)
            var givingGroup = new PersonService(rockContext).Queryable().AsNoTracking()
                                    .Where(p => p.GivingId == targetPerson.GivingId)
                                    .GroupJoin(
                                        groupMemberQry,
                                        p => p.Id,
                                        m => m.PersonId,
                                        (p, m) => new { p, m })
                                    .SelectMany(x => x.m.DefaultIfEmpty(), (y, z) => new { Person = y.p, GroupMember = z })
                                    .Select(p => new { FirstName = p.Person.NickName, LastName = p.Person.LastName, FamilyRoleOrder = p.GroupMember.GroupRole.Order, Gender = p.Person.Gender, PersonId = p.Person.Id })
                                    .DistinctBy(p => p.PersonId)
                                    .OrderBy(p => p.FamilyRoleOrder).ThenBy(p => p.Gender)
                                    .ToList();

            string salutation = string.Empty;

            if (givingGroup.GroupBy(g => g.LastName).Count() == 1)
            {
                salutation = string.Join(", ", givingGroup.Select(g => g.FirstName)) + " " + givingGroup.FirstOrDefault().LastName;
                if (salutation.Contains(","))
                {
                    salutation = salutation.ReplaceLastOccurrence(",", " &");
                }
            }
            else
            {
                salutation = string.Join(", ", givingGroup.Select(g => g.FirstName + " " + g.LastName));
                if (salutation.Contains(","))
                {
                    salutation = salutation.ReplaceLastOccurrence(",", " &");
                }
            }
            mergeFields.Add("Salutation", salutation);

            var mailingAddress = targetPerson.GetMailingLocation();
            if (mailingAddress != null)
            {
                mergeFields.Add("StreetAddress1", mailingAddress.Street1);
                mergeFields.Add("StreetAddress2", mailingAddress.Street2);
                mergeFields.Add("City", mailingAddress.City);
                mergeFields.Add("State", mailingAddress.State);
                mergeFields.Add("PostalCode", mailingAddress.PostalCode);
                mergeFields.Add("Country", mailingAddress.Country);
            }
            else
            {
                mergeFields.Add("StreetAddress1", string.Empty);
                mergeFields.Add("StreetAddress2", string.Empty);
                mergeFields.Add("City", string.Empty);
                mergeFields.Add("State", string.Empty);
                mergeFields.Add("PostalCode", string.Empty);
                mergeFields.Add("Country", string.Empty);
            }

            var transactionDetails = qry.GroupJoin(new AttributeValueService(rockContext).Queryable(),
                ft => ft.Id,
                av => av.Id,
                (obj, av) => new { Transaction = obj, AttributeValues = av })
                .ToList()
                .Select(obj =>
                {
                    obj.Transaction.Attributes = obj.AttributeValues.Select(av2 => AttributeCache.Get(av2.Attribute)).ToDictionary(k => k.Key, v => v);
                    obj.Transaction.AttributeValues = obj.AttributeValues.Select(av2 => new AttributeValueCache(av2)).ToDictionary(k => k.AttributeKey, v => v);
                    return obj.Transaction;
                });

            mergeFields.Add("TransactionDetails", transactionDetails);


            if (excludedCurrencyTypes.Count > 0)
            {
                mergeFields.Add("ExcludedTransactionDetails", excludedQry.ToList());
            }
            mergeFields.Add("AccountSummary", qry.GroupBy(t => new { t.Account.Name, t.Account.PublicName, t.Account.Description })
                                                .Select(s => new AccountSummary
                                                {
                                                    AccountName = s.Key.Name,
                                                    PublicName = s.Key.PublicName,
                                                    Description = s.Key.Description,
                                                    Total = s.Sum(a => a.Amount),
                                                    Order = s.Max(a => a.Account.Order)
                                                })
                                                .OrderBy(s => s.Order));
            // pledge information
            var pledges = new FinancialPledgeService(rockContext).Queryable().AsNoTracking()
                                .Where(p => p.PersonAliasId.HasValue && personAliasIds.Contains(p.PersonAliasId.Value)
                                   && p.StartDate <= dateRange.End && p.EndDate >= dateRange.Start)
                                .GroupBy(p => p.Account)
                                .Select(g => new PledgeSummary
                                {
                                    AccountId = g.Key.Id,
                                    AccountName = g.Key.Name,
                                    PublicName = g.Key.PublicName,
                                    AmountPledged = g.Sum(p => p.TotalAmount),
                                    PledgeStartDate = g.Min(p => p.StartDate),
                                    PledgeEndDate = g.Max(p => p.EndDate)
                                })
                                .ToList();

            // add detailed pledge information
            foreach (var pledge in pledges)
            {
                var adjustedPledgeEndDate = pledge.PledgeEndDate.Value.Date;

                if (adjustedPledgeEndDate != DateTime.MaxValue.Date)
                {
                    adjustedPledgeEndDate = adjustedPledgeEndDate.AddDays(1);
                }

                if (adjustedPledgeEndDate > dateRange.End)
                {
                    adjustedPledgeEndDate = dateRange.End.Value;
                }

                if (adjustedPledgeEndDate > RockDateTime.Now)
                {
                    adjustedPledgeEndDate = RockDateTime.Now;
                }

                pledge.AmountGiven = new FinancialTransactionDetailService(rockContext).Queryable()
                                            .Where(t =>
                                                t.AccountId == pledge.AccountId
                                                && t.Transaction.AuthorizedPersonAliasId.HasValue && personAliasIds.Contains(t.Transaction.AuthorizedPersonAliasId.Value)
                                                && t.Transaction.TransactionDateTime >= pledge.PledgeStartDate
                                                && t.Transaction.TransactionDateTime < adjustedPledgeEndDate)
                                            .Sum(t => (decimal?)t.Amount) ?? 0;

                pledge.AmountRemaining = (pledge.AmountGiven > pledge.AmountPledged) ? 0 : (pledge.AmountPledged - pledge.AmountGiven);

                if (pledge.AmountPledged > 0)
                {
                    var test = (double)pledge.AmountGiven / (double)pledge.AmountPledged;
                    pledge.PercentComplete = (int)((pledge.AmountGiven * 100) / pledge.AmountPledged);
                }
            }

            mergeFields.Add("Pledges", pledges);
        }
    }


    /// <summary>
    /// Pledge Summary Class
    /// </summary>
    public class PledgeSummary : DotLiquid.Drop
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
    public class AccountSummary : DotLiquid.Drop
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
}
