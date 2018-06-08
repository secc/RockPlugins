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

        [DataMember]
        public int FinancialGatewayId { get; set; }


        /// <summary>
        /// Gets or sets the <see cref="Rock.Model.FinancialGateway"/> of the transaction.
        /// </summary>
        /// <value>
        /// The <see cref="Rock.Model.FinancialGateway"/> of the file.
        /// </value>
        [DataMember]
        public virtual Rock.Model.FinancialGateway FinancialGateway { get; set; }
    }
}
