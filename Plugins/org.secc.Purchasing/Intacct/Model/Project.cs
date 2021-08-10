using System;
using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{

    [DeserializeAs( Name = "project" )]
    public class Project : IntacctModel
    {
        public override int Id
        {
            get
            {
                int id;
                int.TryParse( ProjectId.Substring( 1 ), out id );
                return id;
            }
        }

        public override string ApiId
        {
            get
            {
                return ProjectId;
            }
        }

        [DeserializeAs( Name = "PROJECTID" )]
        public string ProjectId { get; set; }

        [DeserializeAs( Name = "NAME" )]
        public string Name { get; set; }

        [DeserializeAs( Name = "DESCRIPTION" )]
        public string Description { get; set; }

        [DeserializeAs( Name = "CURRENCY" )]
        public string Currency { get; set; }

        [DeserializeAs( Name = "PROJECTCATEGORY" )]
        public string ProjectCategory { get; set; }

        [DeserializeAs( Name = "PROJECTSTATUSKEY" )]
        public string ProjectStatusKey { get; set; }

        [DeserializeAs( Name = "PROJECTSTATUS" )]
        public string ProjectStatus { get; set; }

        [DeserializeAs( Name = "PREVENTTIMESHEET" )]
        public string PreventTimeSheet { get; set; }

        [DeserializeAs( Name = "PREVENTEXPENSE" )]
        public string PreventExpense { get; set; }

        [DeserializeAs( Name = "PREVENTAPPO" )]
        public string PreventAPPO { get; set; }

        [DeserializeAs( Name = "PREVENTGENINVOICE" )]
        public string PreventGenInvoice { get; set; }

        [DeserializeAs( Name = "STATUS" )]
        public string Status { get; set; }

        [DeserializeAs( Name = "BEGINDATE" )]
        public DateTime? BeginDate { get; set; }

        [DeserializeAs( Name = "ENDDATE" )]
        public DateTime? EndDate { get; set; }

        [DeserializeAs( Name = "BUDGETAMOUNT" )]
        public string BudgetAmount { get; set; }

        [DeserializeAs( Name = "CONTRACTAMOUNT" )]
        public string ContractAmount { get; set; }

        [DeserializeAs( Name = "ACTUALAMOUNT" )]
        public string ActualAmount { get; set; }

        [DeserializeAs( Name = "BUDGETQTY" )]
        public string BudgetQty { get; set; }

        [DeserializeAs( Name = "ESTQTY" )]
        public string EstQty { get; set; }

        [DeserializeAs( Name = "ACTUALQTY" )]
        public string ActualQty { get; set; }

        [DeserializeAs( Name = "APPROVEDQTY" )]
        public string ApprovedQty { get; set; }

        [DeserializeAs( Name = "REMAININGQTY" )]
        public string RemainingQty { get; set; }

        [DeserializeAs( Name = "PERCENTCOMPLETE" )]
        public string PercentComplete { get; set; }

        [DeserializeAs( Name = "OBSPERCENTCOMPLETE" )]
        public string ObsPercentComplete { get; set; }

        [DeserializeAs( Name = "BILLINGTYPE" )]
        public string BillingType { get; set; }

        [DeserializeAs( Name = "SONUMBER" )]
        public string SONumber { get; set; }

        [DeserializeAs( Name = "PONUMBER" )]
        public string PONumber { get; set; }

        [DeserializeAs( Name = "POAMOUNT" )]
        public string POAamount { get; set; }

        [DeserializeAs( Name = "PQNUMBER" )]
        public string PQNumber { get; set; }

        [DeserializeAs( Name = "SFDCKEY" )]
        public string SFDCKey { get; set; }

        [DeserializeAs( Name = "QARROWKEY" )]
        public string QArrowKey { get; set; }

        [DeserializeAs( Name = "OAKEY" )]
        public string OAKey { get; set; }

        [DeserializeAs( Name = "PARENTKEY" )]
        public string ParentKey { get; set; }

        [DeserializeAs( Name = "PARENTID" )]
        public string ParentId { get; set; }

        [DeserializeAs( Name = "PARENTNAME" )]
        public string ParentName { get; set; }

        [DeserializeAs( Name = "INVOICEWITHPARENT" )]
        public string InvoiceWithParent { get; set; }

        [DeserializeAs( Name = "CUSTOMERKEY" )]
        public string CustomerKey { get; set; }

        [DeserializeAs( Name = "CUSTOMERID" )]
        public string CustomerId { get; set; }

        [DeserializeAs( Name = "CUSTOMERNAME" )]
        public string CustomerName { get; set; }

        [DeserializeAs( Name = "SALESCONTACTKEY" )]
        public string SalesContactKey { get; set; }

        [DeserializeAs( Name = "SALESCONTACTID" )]
        public string SalesContactId { get; set; }
        [DeserializeAs( Name = "SALESCONTACTNAME" )]
        public string SalesContactName { get; set; }

        [DeserializeAs( Name = "PROJECTTYPEKEY" )]
        public string ProjectTypeKey { get; set; }

        [DeserializeAs( Name = "PROJECTTYPE" )]
        public string ProjectType { get; set; }

        [DeserializeAs( Name = "MANAGERKEY" )]
        public string ManagerKey { get; set; }

        [DeserializeAs( Name = "MANAGERID" )]
        public string ManagerId { get; set; }

        [DeserializeAs( Name = "MANAGERCONTACTNAME" )]
        public string ManagerContactName { get; set; }

        [DeserializeAs( Name = "PROJECTDEPTKEY" )]
        public string ProjectDeptKey { get; set; }

        [DeserializeAs( Name = "DEPARTMENTID" )]
        public string DepartmentId { get; set; }

        [DeserializeAs( Name = "DEPARTMENTNAME" )]
        public string DepartmentName { get; set; }

        [DeserializeAs( Name = "PROJECTLOCATIONKEY" )]
        public string ProjectLocationId { get; set; }

        [DeserializeAs( Name = "LOCATIONID" )]
        public string LocationId { get; set; }

        [DeserializeAs( Name = "LOCATIONNAME" )]
        public string LocationName { get; set; }

        [DeserializeAs( Name = "CONTACTINFO.CONTACTNAME" )]
        public string ContactName { get; set; }

        [DeserializeAs( Name = "SHIPTO.CONTACTNAME" )]
        public string ShipTo { get; set; }

        [DeserializeAs( Name = "BILLTO.CONTACTNAME" )]
        public string BillTo { get; set; }

        [DeserializeAs( Name = "TERMSKEY" )]
        public string TermsKey { get; set; }

        [DeserializeAs( Name = "TERMNAME" )]
        public string TermName { get; set; }

        [DeserializeAs( Name = "DOCNUMBER" )]
        public string DocNumber { get; set; }

        [DeserializeAs( Name = "CUSTUSERKEY" )]
        public string CustUserKey { get; set; }

        [DeserializeAs( Name = "CUSTUSERID" )]
        public string CustUserId { get; set; }

        [DeserializeAs( Name = "WHENCREATED" )]
        public DateTime? WhenCreated { get; set; }

        [DeserializeAs( Name = "WHENMODIFIED" )]
        public DateTime? WhenModfied { get; set; }

        [DeserializeAs( Name = "CREATEDBY" )]
        public int? CreatedBy { get; set; }

        [DeserializeAs( Name = "MODIFIEDBY" )]
        public int? ModifiedBy { get; set; }

        [DeserializeAs( Name = "BUDGETEDCOST" )]
        public string BudgetedCost { get; set; }

        [DeserializeAs( Name = "CLASSID" )]
        public string ClassId { get; set; }

        [DeserializeAs( Name = "CLASSNAME" )]
        public string ClassName { get; set; }

        [DeserializeAs( Name = "CLASSKEY" )]
        public string ClassKey { get; set; }

        [DeserializeAs( Name = "USERRESTRICTIONS" )]
        public string UserRestrictions { get; set; }

        [DeserializeAs( Name = "BILLABLEEXPDEFAULT" )]
        public string BillableExpDefault { get; set; }

        [DeserializeAs( Name = "BILLABLEAPPODEFAULT" )]
        public string BillableAPPODefault { get; set; }

        [DeserializeAs( Name = "BUDGETID" )]
        public string BudgetId { get; set; }

        [DeserializeAs( Name = "BUDGETKEY" )]
        public string BudgetKey { get; set; }

        [DeserializeAs( Name = "BILLINGRATE" )]
        public string BillingRate { get; set; }

        [DeserializeAs( Name = "BILLINGPRICING" )]
        public string BillingPricing { get; set; }

        [DeserializeAs( Name = "EXPENSERATE" )]
        public string ExpenseRate { get; set; }

        [DeserializeAs( Name = "EXPENSEPRICING" )]
        public string ExpensePricing { get; set; }

        [DeserializeAs( Name = "POAPRATE" )]
        public string POAPRate { get; set; }

        [DeserializeAs( Name = "POAPPRICING" )]
        public string POAPPricing { get; set; }

        [DeserializeAs( Name = "CONTACTKEY" )]
        public string ContactKey { get; set; }

        [DeserializeAs( Name = "SHIPTOKEY" )]
        public string ShipToKey { get; set; }

        [DeserializeAs( Name = "BILLTOKEY" )]
        public string BillToKey { get; set; }

        [DeserializeAs( Name = "INVOICEMESSAGE" )]
        public string InvoiceMessage { get; set; }

        [DeserializeAs( Name = "INVOICECURRENCY" )]
        public string InvoiceCurrency { get; set; }

        [DeserializeAs( Name = "BILLINGOVERMAX" )]
        public string BillingOverMax { get; set; }

        [DeserializeAs( Name = "EXCLUDEEXPENSES" )]
        public string ExcludeExpenses { get; set; }

        [DeserializeAs( Name = "CONTRACTKEY" )]
        public string ContractKey { get; set; }

        [DeserializeAs( Name = "CONTRACTID" )]
        public string ContractId { get; set; }

        [DeserializeAs( Name = "ROOTPARENTKEY" )]
        public string RootParentKey { get; set; }

        [DeserializeAs( Name = "ROOTPARENTID" )]
        public string RootParentId { get; set; }

        [DeserializeAs( Name = "ROOTPARENTNAME" )]
        public string RootParentName { get; set; }

        [DeserializeAs( Name = "MEGAENTITYKEY" )]
        public string MegaEntityKey { get; set; }

        [DeserializeAs( Name = "MEGAENTITYID" )]
        public string MegaEntityId { get; set; }

        [DeserializeAs( Name = "MEGAENTITYNAME" )]
        public string MegaEntityName { get; set; }

    }
}
