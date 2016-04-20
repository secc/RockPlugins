using Rock.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.PayPalReporting.Migrations
{
    [MigrationNumber(1, "1.0.1")]
    class CreateDb : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            Sql(@"
            CREATE TABLE [dbo].[_org_secc_PayPalReporting_Transaction](
	            [Id] [int] IDENTITY(1,1) NOT NULL,
	            [GatewayTransactionId] [nvarchar](255) NOT NULL,
	            [TenderType] [nvarchar](255) NULL,
	            [Amount] [float] NULL,
	            [Comment1] [nvarchar](255) NULL,
	            [TimeCreated] [datetime] NULL,
	            [Type] [nvarchar](255) NULL,
	            [Comment2] [nvarchar](255) NULL,
	            [Fees] [float] NULL,
	            [MerchantTransactionId] [nvarchar](255) NULL,
	            [BatchId] [int] NULL,
	            [BillingFirstName] [nvarchar](50) NULL,
                [BillingLastName] [nvarchar](50) NULL,
	            [IsZeroFee] [bit] NOT NULL,
	            [Guid] [uniqueidentifier] NOT NULL,
	            [CreatedDateTime] [datetime] NULL,
	            [ModifiedDateTime] [datetime] NULL,
	            [CreatedByPersonAliasId] [int] NULL,
	            [ModifiedByPersonAliasId] [int] NULL,
                [ForeignKey] [nvarchar](100) NULL,
	            [ForeignId] [nvarchar](50) NULL,
	            [ForeignGuid] [uniqueidentifier] NULL
             CONSTRAINT [PK__org_secc_PayPalReporting_Transaction] PRIMARY KEY CLUSTERED 
            (
	            [GatewayTransactionId] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 100) ON [PRIMARY]
            ) ON [PRIMARY]

            ALTER TABLE [dbo].[_org_secc_PayPalReporting_Transaction] ADD  CONSTRAINT [DF_org_secc_PayPalReporting_Transaction_IsZeroFee]  DEFAULT ((0)) FOR [IsZeroFee]

            ALTER TABLE[dbo].[_org_secc_PayPalReporting_Transaction]
                    WITH CHECK ADD CONSTRAINT[FK_dbo._org_secc_PayPalReporting_Transaction_dbo.PersonAlias_CreatedByPersonAliasId] FOREIGN KEY([CreatedByPersonAliasId])
                REFERENCES[dbo].[PersonAlias] ([Id])

            ALTER TABLE[dbo].[_org_secc_PayPalReporting_Transaction]
                    CHECK CONSTRAINT[FK_dbo._org_secc_PayPalReporting_Transaction_dbo.PersonAlias_CreatedByPersonAliasId]

            ALTER TABLE[dbo].[_org_secc_PayPalReporting_Transaction]
                    WITH CHECK ADD CONSTRAINT[FK_dbo._org_secc_PayPalReporting_Transaction_dbo.PersonAlias_ModifiedByPersonAliasId] FOREIGN KEY([ModifiedByPersonAliasId])
                REFERENCES[dbo].[PersonAlias] ([Id])

            ALTER TABLE[dbo].[_org_secc_PayPalReporting_Transaction]
                    CHECK CONSTRAINT[FK_dbo._org_secc_PayPalReporting_Transaction_dbo.PersonAlias_ModifiedByPersonAliasId]

            CREATE NONCLUSTERED INDEX [IDX_org_secc_PayPalReporting_Transaction_TimeCreated] ON [dbo].[_org_secc_PayPalReporting_Transaction]
            (
	            [TimeCreated] ASC
            )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

            ");


        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
            Sql(@"
            ALTER TABLE [dbo].[_org_secc_PayPalReporting_Transaction] DROP CONSTRAINT [DF_org_secc_PayPalReporting_Transaction_IsZeroFee]
            ALTER TABLE [dbo].[_org_secc_PayPalReporting_Transaction] DROP CONSTRAINT [FK_dbo._org_secc_PayPalReporting_Transaction_dbo.PersonAlias_CreatedByPersonAliasId]
            ALTER TABLE [dbo].[_org_secc_PayPalReporting_Transaction] DROP CONSTRAINT [FK_dbo._org_secc_PayPalReporting_Transaction_dbo.PersonAlias_ModifiedByPersonAliasId]
            DROP INDEX DROP INDEX [dbo].[_org_secc_PayPalReporting_Transaction].[IDX_org_secc_PayPalReporting_Transaction_TimeCreated]
            DROP TABLE [dbo].[_org_secc_PayPalReporting_Transaction]
            ");
        }
    }
}
