using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using org.secc.Finance.Utility;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Rock.Workflow;

namespace org.secc.Finance.Workflow
{
    [ActionCategory( "SECC > Finance" )]
    [Description( "Generate a giving statement." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Generate Giving Statement" )]
    [WorkflowAttribute("Person", "The person for whom to generate a statement.", fieldTypeClassNames: new string[] { "Rock.Field.Types.PersonFieldType" })]
    [WorkflowTextOrAttribute("Start Date", "Start Date Attribute", "The start date to use when fetching the transactions for the statement.", fieldTypeClassNames: new string[] { "Rock.Field.Types.DateFieldType" }, key:"StartDate")]
    [WorkflowTextOrAttribute("End Date", "End Date", "The end date to use when when fetching the transactions for the statement.", fieldTypeClassNames: new string[] { "Rock.Field.Types.DateFieldType" }, key: "EndDate")]
    [CodeEditorField("Lava Template", "The Lava template to use for the contribution statement.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 500, true)]
    [DefinedValueField(Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE, "Excluded Currency Types", "Select the currency types you would like to excluded.", false, true)]
    [AccountsField("Accounts", "A selection of accounts to include on the statement. If none are selected all accounts that are tax-deductible will be uses.", false)]
    [WorkflowAttribute("Statement HTML", "The attribute to store the results (HTML).", true, fieldTypeClassNames: new string[] { "Rock.Field.Types.HtmlFieldType", "Rock.Field.Types.MemoFieldType", "Rock.Field.Types.CodeEditorFieldType", "Rock.Field.Types.TextFieldType" })]

    class GenerateStatement : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            var mergeFields = GetMergeFields( action );
            errorMessages = new List<string>();

            // Get the startdate/enddate
            DateTime? startDate = GetAttributeValue(action, "StartDate", true).ResolveMergeFields(mergeFields).AsDateTime();
            DateTime? endDate = GetAttributeValue(action, "EndDate", true).ResolveMergeFields(mergeFields).AsDateTime();

            // Now set the person
            PersonAliasService personAliasService = new PersonAliasService(rockContext);
            PersonAlias targetPersonAlias = personAliasService.Get(GetAttributeValue(action, "Person", true).AsGuid());


            // get excluded currency types setting
            List<Guid> excludedCurrencyTypes = new List<Guid>();
            if (GetAttributeValue(action, "ExcludedCurrencyTypes").IsNotNullOrWhiteSpace())
            {
                excludedCurrencyTypes = GetAttributeValue(action, "ExcludedCurrencyTypes").Split(',').Select(Guid.Parse).ToList();
            }

            List<Guid> accountGuids = null;
            if (!string.IsNullOrWhiteSpace(GetAttributeValue(action, "Accounts")))
            {
                accountGuids = GetAttributeValue(action, "Accounts").Split(',').Select(Guid.Parse).ToList();
            }

            // Add all the merge fields from the Statement Utility
            Statement.AddMergeFields(mergeFields, targetPersonAlias.Person, new DateRange(startDate, endDate), excludedCurrencyTypes, accountGuids);

            var template = GetAttributeValue(action, "LavaTemplate");

            string output = template.ResolveMergeFields(mergeFields);


            // Now store the target attribute
            var targetAttribute = AttributeCache.Get(GetActionAttributeValue(action, "StatementHTML").AsGuid(), rockContext);
            if (targetAttribute.EntityTypeId == new Rock.Model.Workflow().TypeId)
            {
                action.Activity.Workflow.SetAttributeValue(targetAttribute.Key, output);
            }
            else if (targetAttribute.EntityTypeId == new WorkflowActivity().TypeId)
            {
                action.Activity.SetAttributeValue(targetAttribute.Key, output);
            }
            return true;
        }
    }

    #region Supporting Classes

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
#endregion
}
