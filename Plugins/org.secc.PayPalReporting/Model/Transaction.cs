namespace org.secc.PayPalReporting.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Runtime.Serialization;
    [Table("_org_secc_PayPalReporting_Transaction")]
    public partial class Transaction : Rock.Data.Model<Transaction>, Rock.Security.ISecured
    {
        [StringLength(255)]
        [DataMember]
        public string GatewayTransactionId { get; set; }

        [StringLength(255)]
        [DataMember]
        public string TenderType { get; set; }

        [DataMember]
        public double? Amount { get; set; }

        [StringLength(255)]
        [DataMember]
        public string Comment1 { get; set; }

        [DataMember]
        public DateTime? TimeCreated { get; set; }

        [StringLength(255)]
        [DataMember]
        public string Type { get; set; }

        [StringLength(255)]
        [DataMember]
        public string Comment2 { get; set; }

        [DataMember]
        public double? Fees { get; set; }

        [StringLength(255)]
        [DataMember]
        public string MerchantTransactionId { get; set; }

        [DataMember]
        public int? BatchId { get; set; }

        [StringLength(50)]
        [DataMember]
        public string BillingFirstName { get; set; }

        [StringLength(50)]
        [DataMember]
        public string BillingLastName { get; set; }

        [DataMember]
        public bool IsZeroFee { get; set; }
    }
}
