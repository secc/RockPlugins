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
using Rock.Plugin;

namespace org.secc.RegistrationCart.Migrations
{
    /// <summary>
    /// Two things from the PR #336 review.
    /// Unconfirmed checkouts: Cart.PaymentSentDateTime (set just before the gateway is asked to
    /// charge, so a checkout that stopped before then can be reopened without anyone checking
    /// the gateway), Cart.ResolvedDateTime / ResolvedByPersonAliasId / ResolutionNote (staff
    /// closed an unconfirmed checkout on the settings page) and Cart.PendingReportedDateTime
    /// (when the maintenance job last warned about it).
    /// Public pages: migration 001 hung them off Rock's stock External site with global routes.
    /// At SECC that site is the retired 2014 one (old theme, no forced HTTPS), so where the SECC
    /// public site exists the pages move under its home page, on its full-width layout, and their
    /// routes stop being global, like every other route on that site. A database without that
    /// site (a local Rock) keeps them where migration 001 put them. Staff can move the pages
    /// anywhere on the site afterwards; the blocks find each other by page, not by place.
    /// </summary>
    [MigrationNumber( 13, "1.16.0" )]
    public class PendingCheckoutsAndPublicSite : Migration
    {
        /// <summary>The SECC public site ("Southeast Christian Church").</summary>
        private const string SeccPublicSiteGuid = "74DF3DB5-E01C-4B0E-A4D0-51D72B5C607E";

        private const string CartTable = "_org_secc_RegistrationCart_Cart";

        public override void Up()
        {
            AddCartColumn( "PaymentSentDateTime", "[datetime] NULL" );
            AddCartColumn( "ResolvedDateTime", "[datetime] NULL" );
            AddCartColumn( "ResolvedByPersonAliasId", "[int] NULL" );
            AddCartColumn( "ResolutionNote", "[nvarchar](1000) NULL" );
            AddCartColumn( "PendingReportedDateTime", "[datetime] NULL" );

            Sql( $@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_{CartTable}_ResolvedByPersonAliasId')
                BEGIN
                    ALTER TABLE [dbo].[{CartTable}] ADD CONSTRAINT [FK_{CartTable}_ResolvedByPersonAliasId]
                        FOREIGN KEY ([ResolvedByPersonAliasId]) REFERENCES [dbo].[PersonAlias] ([Id]);
                END" );

            // A checkout already closed with an error under the old rule ('has a payment record'
            // meant confirmed) stays confirmed.
            Sql( $@"
                UPDATE [dbo].[{CartTable}]
                SET [ResolvedDateTime] = GETDATE(), [ResolutionNote] = 'Had a payment record before migration 13.'
                WHERE [Status] = 1 AND [FinancialTransactionId] IS NOT NULL AND ISNULL([LastCheckoutError], '') <> '' AND [ResolvedDateTime] IS NULL;" );

            // A checkout left claimed (Processing) before PaymentSentDateTime existed may have
            // reached the gateway: mark it sent, so only staff who have checked the gateway
            // reopen it, never the maintenance job.
            Sql( $@"
                UPDATE [dbo].[{CartTable}]
                SET [PaymentSentDateTime] = COALESCE([ModifiedDateTime], [CreatedDateTime], GETDATE())
                WHERE [Status] = 3 AND [PaymentSentDateTime] IS NULL;" );

            var publicPages =$"'{SystemGuid.Page.CAMP_CART}','{SystemGuid.Page.OFFERING}','{SystemGuid.Page.CART}','{SystemGuid.Page.CHECKOUT}'";
            Sql( $@"
                DECLARE @SiteId int = (SELECT TOP 1 [Id] FROM [Site] WHERE [Guid] = '{SeccPublicSiteGuid}');
                DECLARE @HomePageId int = (SELECT [DefaultPageId] FROM [Site] WHERE [Id] = @SiteId);
                DECLARE @LayoutId int = (SELECT TOP 1 [Id] FROM [Layout] WHERE [SiteId] = @SiteId
                    ORDER BY CASE WHEN [FileName] = 'FullWidth' THEN 0 ELSE 1 END, [Id]);

                IF @HomePageId IS NOT NULL AND @LayoutId IS NOT NULL
                BEGIN
                    UPDATE [Page] SET [LayoutId] = @LayoutId WHERE [Guid] IN ({publicPages});
                    UPDATE [Page] SET [ParentPageId] = @HomePageId WHERE [Guid] = '{SystemGuid.Page.CAMP_CART}';
                    UPDATE [PageRoute] SET [IsGlobal] = 0
                    WHERE [Guid] IN ('{SystemGuid.PageRoute.CAMP_CART}','{SystemGuid.PageRoute.OFFERING}','{SystemGuid.PageRoute.CART}','{SystemGuid.PageRoute.CHECKOUT}');
                END" );
        }

        public override void Down()
        {
            Sql( $@"
                UPDATE [PageRoute] SET [IsGlobal] = 1
                WHERE [Guid] IN ('{SystemGuid.PageRoute.CAMP_CART}','{SystemGuid.PageRoute.OFFERING}','{SystemGuid.PageRoute.CART}','{SystemGuid.PageRoute.CHECKOUT}');" );

            Sql( $@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_{CartTable}_ResolvedByPersonAliasId')
                BEGIN
                    ALTER TABLE [dbo].[{CartTable}] DROP CONSTRAINT [FK_{CartTable}_ResolvedByPersonAliasId];
                END" );

            DropCartColumn( "PendingReportedDateTime" );
            DropCartColumn( "ResolutionNote" );
            DropCartColumn( "ResolvedByPersonAliasId" );
            DropCartColumn( "ResolvedDateTime" );
            DropCartColumn( "PaymentSentDateTime" );
        }

        private void AddCartColumn( string column, string definition )
        {
            Sql( $@"
                IF COL_LENGTH('dbo.{CartTable}', '{column}') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[{CartTable}] ADD [{column}] {definition};
                END" );
        }

        private void DropCartColumn( string column )
        {
            Sql( $@"
                IF COL_LENGTH('dbo.{CartTable}', '{column}') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[{CartTable}] DROP COLUMN [{column}];
                END" );
        }
    }
}
