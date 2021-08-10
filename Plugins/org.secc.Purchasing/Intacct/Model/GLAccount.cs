using RestSharp.Deserializers;

namespace org.secc.Purchasing.Intacct.Model
{
    [DeserializeAs( Name = "glaccount" )]
    public class GLAccount : IntacctModel
    {

        public override int Id
        {
            get
            {
                return AccountNo.HasValue ? AccountNo.Value : 0;
            }
        }

        public override string ApiId
        {
            get
            {
                return AccountNo.ToString();
                ;
            }
        }


        [DeserializeAs( Name = "ACCOUNTNO" )]
        public int? AccountNo { get; set; }

        [DeserializeAs( Name = "TITLE" )]
        public string Title { get; set; }

        [DeserializeAs( Name = "ACCOUNTTYPE" )]
        public string AccountType { get; set; }

        [DeserializeAs( Name = "NORMALBALANCE" )]
        public string NormalBalance { get; set; }

        [DeserializeAs( Name = "CLOSINGTYPE" )]
        public string ClosingType { get; set; }

        [DeserializeAs( Name = "CLOSINGACCOUNTNO" )]
        public string ClosingAccountNo { get; set; }

        [DeserializeAs( Name = "CLOSINGACCOUNTTITLE" )]
        public string ClosingAccountTitle { get; set; }

        [DeserializeAs( Name = "STATUS" )]
        public string Status { get; set; }

        [DeserializeAs( Name = "REQUIREDEPT" )]
        public string RequireDept { get; set; }

        [DeserializeAs( Name = "REQUIRELOC" )]
        public string RequireLoc { get; set; }

        [DeserializeAs( Name = "TAXABLE" )]
        public string Taxable { get; set; }

        [DeserializeAs( Name = "CATEGORYKEY" )]
        public string CategoryKey { get; set; }

        [DeserializeAs( Name = "CATEGORY" )]
        public string Category { get; set; }

        [DeserializeAs( Name = "TAXCODE" )]
        public string TaxCode { get; set; }

        [DeserializeAs( Name = "MRCCODE" )]
        public string MRCCode { get; set; }

        [DeserializeAs( Name = "CLOSETOACCTKEY" )]
        public string CloseToAcctKey { get; set; }

        [DeserializeAs( Name = "ALTERNATIVEACCOUNT" )]
        public string AlternativeAccount { get; set; }

        [DeserializeAs( Name = "WHENCREATED" )]
        public string WhenCreated { get; set; }

        [DeserializeAs( Name = "WHENMODIFIED" )]
        public string WhenModified { get; set; }

        [DeserializeAs( Name = "CREATEDBY" )]
        public string CreatedBy { get; set; }

        [DeserializeAs( Name = "MODIFIEDBY" )]
        public string ModifiedBy { get; set; }

        [DeserializeAs( Name = "SUBLEDGERCONTROLON" )]
        public string SubledgerControlOn { get; set; }

        [DeserializeAs( Name = "MEGAENTITYKEY" )]
        public string MegaEntityKey { get; set; }

        [DeserializeAs( Name = "MEGAENTITYID" )]
        public string MegaEntityId { get; set; }

        [DeserializeAs( Name = "MEGAENTITYNAME" )]
        public string MegaEntityName { get; set; }

        [DeserializeAs( Name = "REQUIREPROJECT" )]
        public string RequireProject { get; set; }

        [DeserializeAs( Name = "REQUIRECUSTOMER" )]
        public string RequireCustomer { get; set; }

        [DeserializeAs( Name = "REQUIREVENDOR" )]
        public string RequireVendor { get; set; }

        [DeserializeAs( Name = "REQUIREEMPLOYEE" )]
        public string RequireEmployee { get; set; }

        [DeserializeAs( Name = "REQUIREITEM" )]
        public string RequireItem { get; set; }

        [DeserializeAs( Name = "REQUIRECLASS" )]
        public string RequireClass { get; set; }

    }
}
