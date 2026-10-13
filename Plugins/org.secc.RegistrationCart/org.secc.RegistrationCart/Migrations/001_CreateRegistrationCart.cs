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
    /// Creates every table, entity type, block type, page, route and block the Registration
    /// Cart plugin needs. Runs automatically the first time Rock starts with this
    /// assembly in its bin folder. Down() removes all of it.
    /// </summary>
    [MigrationNumber( 1, "1.16.0" )]
    public class CreateRegistrationCart : Migration
    {
        // Rock core identifiers this migration hangs things off.
        private const string ExternalSiteGuid = "F3F82256-2D66-432B-9D67-3552CD2F4C2B";   // Rock.SystemGuid.Site.EXTERNAL_SITE
        private const string ExternalHomePageGuid = "85F25819-E948-4960-9DDF-00F54D32444E"; // Rock.SystemGuid.Page.EXTERNAL_HOMEPAGE
        private const string InternalInstalledPluginsPageGuid = "5B6DBC42-8B03-4D15-8D92-AAFA28FD8616";
        private const string InternalFullWidthLayoutGuid = "D65F783D-87A9-4CC9-8110-E83466A0EADB";
        private const string PageReferenceFieldTypeGuid = "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108";  // Rock.SystemGuid.FieldType.PAGE_REFERENCE

        private const string BlockAssembly = "org.secc.RegistrationCart, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        public override void Up()
        {
            CreateTables();
            RegisterEntityTypes();
            RegisterBlockTypes();
            CreatePublicPages();
            CreateAdminPages();
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.ADMIN_DISCOUNT_CODES );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.ADMIN_CATALOG_SETTINGS );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.CHECKOUT );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.CART );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.OFFERING_DETAIL );
            RockMigrationHelper.DeleteBlock( SystemGuid.Block.CATALOG );

            RockMigrationHelper.DeletePage( SystemGuid.Page.ADMIN_DISCOUNT_CODES );
            RockMigrationHelper.DeletePage( SystemGuid.Page.ADMIN_CAMP_CART );
            RockMigrationHelper.DeletePage( SystemGuid.Page.CHECKOUT );
            RockMigrationHelper.DeletePage( SystemGuid.Page.CART );
            RockMigrationHelper.DeletePage( SystemGuid.Page.OFFERING );
            RockMigrationHelper.DeletePage( SystemGuid.Page.CAMP_CART );

            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.ADMIN_DISCOUNT_CODES );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.ADMIN_CATALOG_SETTINGS );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.CHECKOUT );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.CART );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.OFFERING_DETAIL );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.CATALOG );

            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.BLOCK_ADMIN_DISCOUNT_CODES );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.BLOCK_ADMIN_CATALOG_SETTINGS );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.BLOCK_CHECKOUT );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.BLOCK_CART );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.BLOCK_OFFERING_DETAIL );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.BLOCK_CATALOG );

            Sql( @"
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_CartDiscount];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_CartLineItemAddOn];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_CartLineItem];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_CartRegistrant];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_Cart];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_DiscountCode];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_FeeItemSetting];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_InstanceSetting];
                DROP TABLE IF EXISTS [_org_secc_RegistrationCart_TemplateSetting];" );

            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CART_DISCOUNT );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CART_LINE_ITEM_ADD_ON );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CART_LINE_ITEM );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CART_REGISTRANT );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CART );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.DISCOUNT_CODE );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.FEE_ITEM_SETTING );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.INSTANCE_SETTING );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.TEMPLATE_SETTING );
        }

        #region Tables

        /// <summary>Standard Rock entity columns every Model&lt;T&gt; table must have.</summary>
        private const string RockEntityColumns = @"
                    [Guid] [uniqueidentifier] NOT NULL,
                    [CreatedDateTime] [datetime] NULL,
                    [ModifiedDateTime] [datetime] NULL,
                    [CreatedByPersonAliasId] [int] NULL,
                    [ModifiedByPersonAliasId] [int] NULL,
                    [ForeignKey] [nvarchar](100) NULL,
                    [ForeignGuid] [uniqueidentifier] NULL,
                    [ForeignId] [int] NULL,";

        private void CreateTables()
        {
            CreateTable( "TemplateSetting", @"
                    [RegistrationTemplateId] [int] NOT NULL,
                    [IsVisible] [bit] NOT NULL DEFAULT 1,
                    [ImageUrl] [nvarchar](500) NULL,
                    [CatalogDescription] [nvarchar](2000) NULL,
                    [Order] [int] NOT NULL DEFAULT 0,",
                Fk( "TemplateSetting", "RegistrationTemplateId", "RegistrationTemplate", cascade: true ),
                "CREATE UNIQUE INDEX [IX__org_secc_RegistrationCart_TemplateSetting_Template] ON [_org_secc_RegistrationCart_TemplateSetting] ([RegistrationTemplateId]);" );

            CreateTable( "InstanceSetting", @"
                    [RegistrationInstanceId] [int] NOT NULL,
                    [IsVisible] [bit] NOT NULL DEFAULT 1,
                    [EligibleGradeBands] [int] NOT NULL DEFAULT 15,
                    [Order] [int] NOT NULL DEFAULT 0,
                    [EventStartDate] [datetime] NULL,
                    [EventEndDate] [datetime] NULL,",
                Fk( "InstanceSetting", "RegistrationInstanceId", "RegistrationInstance", cascade: true ),
                "CREATE UNIQUE INDEX [IX__org_secc_RegistrationCart_InstanceSetting_Instance] ON [_org_secc_RegistrationCart_InstanceSetting] ([RegistrationInstanceId]);" );

            CreateTable( "FeeItemSetting", @"
                    [RegistrationTemplateFeeItemId] [int] NOT NULL,
                    [EligibleGradeBands] [int] NOT NULL DEFAULT 15,",
                Fk( "FeeItemSetting", "RegistrationTemplateFeeItemId", "RegistrationTemplateFeeItem", cascade: true ),
                "CREATE UNIQUE INDEX [IX__org_secc_RegistrationCart_FeeItemSetting_FeeItem] ON [_org_secc_RegistrationCart_FeeItemSetting] ([RegistrationTemplateFeeItemId]);" );

            CreateTable( "DiscountCode", @"
                    [Code] [nvarchar](50) NOT NULL,
                    [DiscountType] [int] NOT NULL DEFAULT 0,
                    [Value] [decimal](18, 2) NOT NULL DEFAULT 0,
                    [Scope] [int] NOT NULL DEFAULT 0,
                    [RegistrationTemplateId] [int] NULL,
                    [ValidFrom] [datetime] NULL,
                    [ValidTo] [datetime] NULL,
                    [IsActive] [bit] NOT NULL DEFAULT 1,",
                Fk( "DiscountCode", "RegistrationTemplateId", "RegistrationTemplate" ),
                "CREATE UNIQUE INDEX [IX__org_secc_RegistrationCart_DiscountCode_Code] ON [_org_secc_RegistrationCart_DiscountCode] ([Code]);" );

            CreateTable( "Cart", @"
                    [OwnerPersonAliasId] [int] NOT NULL,
                    [Status] [int] NOT NULL DEFAULT 0,
                    [CheckedOutDateTime] [datetime] NULL,
                    [FinancialTransactionId] [int] NULL,
                    [LastCheckoutError] [nvarchar](2000) NULL,",
                Fk( "Cart", "OwnerPersonAliasId", "PersonAlias" ),
                Fk( "Cart", "FinancialTransactionId", "FinancialTransaction" ),
                "CREATE INDEX [IX__org_secc_RegistrationCart_Cart_Owner_Status] ON [_org_secc_RegistrationCart_Cart] ([OwnerPersonAliasId], [Status]);" );

            CreateTable( "CartRegistrant", @"
                    [CartId] [int] NOT NULL,
                    [PersonAliasId] [int] NOT NULL,
                    [FirstName] [nvarchar](100) NOT NULL,
                    [LastName] [nvarchar](100) NOT NULL,
                    [GradeBand] [int] NOT NULL DEFAULT 0,",
                Fk( "CartRegistrant", "CartId", "_org_secc_RegistrationCart_Cart", cascade: true ),
                Fk( "CartRegistrant", "PersonAliasId", "PersonAlias" ) );

            CreateTable( "CartLineItem", @"
                    [CartId] [int] NOT NULL,
                    [CartRegistrantId] [int] NOT NULL,
                    [RegistrationTemplateId] [int] NOT NULL,
                    [RegistrationInstanceId] [int] NOT NULL,
                    [SnapshotBasePrice] [decimal](18, 2) NOT NULL DEFAULT 0,
                    [RegistrationId] [int] NULL,",
                Fk( "CartLineItem", "CartId", "_org_secc_RegistrationCart_Cart", cascade: true ),
                Fk( "CartLineItem", "CartRegistrantId", "_org_secc_RegistrationCart_CartRegistrant" ),
                Fk( "CartLineItem", "RegistrationTemplateId", "RegistrationTemplate" ),
                Fk( "CartLineItem", "RegistrationInstanceId", "RegistrationInstance" ),
                Fk( "CartLineItem", "RegistrationId", "Registration" ),
                "CREATE UNIQUE INDEX [IX__org_secc_RegistrationCart_CartLineItem_Registrant_Variant] ON [_org_secc_RegistrationCart_CartLineItem] ([CartRegistrantId], [RegistrationInstanceId]);" );

            CreateTable( "CartLineItemAddOn", @"
                    [CartLineItemId] [int] NOT NULL,
                    [RegistrationTemplateFeeItemId] [int] NOT NULL,
                    [SnapshotName] [nvarchar](200) NULL,
                    [SnapshotPrice] [decimal](18, 2) NOT NULL DEFAULT 0,",
                Fk( "CartLineItemAddOn", "CartLineItemId", "_org_secc_RegistrationCart_CartLineItem", cascade: true ),
                Fk( "CartLineItemAddOn", "RegistrationTemplateFeeItemId", "RegistrationTemplateFeeItem" ) );

            CreateTable( "CartDiscount", @"
                    [CartId] [int] NOT NULL,
                    [DiscountCodeId] [int] NOT NULL,
                    [SnapshotValue] [decimal](18, 2) NOT NULL DEFAULT 0,
                    [SnapshotType] [int] NOT NULL DEFAULT 0,
                    [SnapshotScope] [int] NOT NULL DEFAULT 0,
                    [SnapshotRegistrationTemplateId] [int] NULL,",
                Fk( "CartDiscount", "CartId", "_org_secc_RegistrationCart_Cart", cascade: true ),
                Fk( "CartDiscount", "DiscountCodeId", "_org_secc_RegistrationCart_DiscountCode" ) );
        }

        private void CreateTable( string shortName, string columns, params string[] extraStatements )
        {
            var table = "_org_secc_RegistrationCart_" + shortName;
            Sql( $@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'{table}')
                BEGIN
                    CREATE TABLE [dbo].[{table}](
                        [Id] [int] IDENTITY(1,1) NOT NULL,
                        {columns}
                        {RockEntityColumns}
                        CONSTRAINT [PK_{table}] PRIMARY KEY CLUSTERED ([Id] ASC)
                    );
                    CREATE UNIQUE INDEX [IX_{table}_Guid] ON [dbo].[{table}] ([Guid]);
                    ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [FK_{table}_CreatedByPersonAliasId]
                        FOREIGN KEY ([CreatedByPersonAliasId]) REFERENCES [dbo].[PersonAlias] ([Id]);
                    ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [FK_{table}_ModifiedByPersonAliasId]
                        FOREIGN KEY ([ModifiedByPersonAliasId]) REFERENCES [dbo].[PersonAlias] ([Id]);
                    {string.Join( "\n                    ", extraStatements )}
                END" );
        }

        private static string Fk( string shortName, string column, string referencedTable, bool cascade = false )
        {
            var table = "_org_secc_RegistrationCart_" + shortName;
            var onDelete = cascade ? " ON DELETE CASCADE" : "";
            return $"ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [FK_{table}_{column}] FOREIGN KEY ([{column}]) REFERENCES [dbo].[{referencedTable}] ([Id]){onDelete};";
        }

        #endregion

        #region Entity + block types

        private void RegisterEntityTypes()
        {
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.TemplateSetting", SystemGuid.EntityType.TEMPLATE_SETTING, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.InstanceSetting", SystemGuid.EntityType.INSTANCE_SETTING, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.FeeItemSetting", SystemGuid.EntityType.FEE_ITEM_SETTING, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.DiscountCode", SystemGuid.EntityType.DISCOUNT_CODE, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.Cart", SystemGuid.EntityType.CART, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.CartRegistrant", SystemGuid.EntityType.CART_REGISTRANT, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.CartLineItem", SystemGuid.EntityType.CART_LINE_ITEM, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.CartLineItemAddOn", SystemGuid.EntityType.CART_LINE_ITEM_ADD_ON, true, true );
            RockMigrationHelper.UpdateEntityType( "org.secc.RegistrationCart.Model.CartDiscount", SystemGuid.EntityType.CART_DISCOUNT, true, true );
        }

        private void RegisterBlockTypes()
        {
            BlockType( "Catalog", SystemGuid.EntityType.BLOCK_CATALOG, SystemGuid.BlockType.CATALOG,
                "Registration Cart Catalog", "Lists Rock registration templates that have an open registration instance, as a shoppable catalog." );
            BlockType( "OfferingDetail", SystemGuid.EntityType.BLOCK_OFFERING_DETAIL, SystemGuid.BlockType.OFFERING_DETAIL,
                "Registration Cart Offering", "Choose a family member, a registration instance and fees for one Rock registration template and add them to the cart." );
            BlockType( "Cart", SystemGuid.EntityType.BLOCK_CART, SystemGuid.BlockType.CART,
                "Registration Cart", "Shows the current person's cart with pricing and discount codes." );
            BlockType( "Checkout", SystemGuid.EntityType.BLOCK_CHECKOUT, SystemGuid.BlockType.CHECKOUT,
                "Registration Cart Checkout", "Takes payment for the cart and creates Rock registrations and the financial transaction." );
            BlockType( "AdminCatalogSettings", SystemGuid.EntityType.BLOCK_ADMIN_CATALOG_SETTINGS, SystemGuid.BlockType.ADMIN_CATALOG_SETTINGS,
                "Registration Cart Settings", "Choose which Rock registration templates and instances appear in Registration Cart and who may register for them.", "Registration Cart > Administration" );
            BlockType( "AdminDiscountCodes", SystemGuid.EntityType.BLOCK_ADMIN_DISCOUNT_CODES, SystemGuid.BlockType.ADMIN_DISCOUNT_CODES,
                "Registration Cart Discount Codes", "Administer discount codes.", "Registration Cart > Administration" );

            // Block-type attributes (LinkedPage settings) are pre-created so the page
            // wiring below can set their values before Rock has loaded the blocks.
            BlockTypeAttribute( SystemGuid.BlockType.CATALOG, "Offering Detail Page", "OfferingDetailPage", SystemGuid.BlockTypeAttribute.CATALOG_OFFERING_DETAIL_PAGE );
            BlockTypeAttribute( SystemGuid.BlockType.CATALOG, "Cart Page", "CartPage", SystemGuid.BlockTypeAttribute.CATALOG_CART_PAGE );
            BlockTypeAttribute( SystemGuid.BlockType.OFFERING_DETAIL, "Cart Page", "CartPage", SystemGuid.BlockTypeAttribute.OFFERING_DETAIL_CART_PAGE );
            BlockTypeAttribute( SystemGuid.BlockType.OFFERING_DETAIL, "Catalog Page", "CatalogPage", SystemGuid.BlockTypeAttribute.OFFERING_DETAIL_CATALOG_PAGE );
            BlockTypeAttribute( SystemGuid.BlockType.CART, "Checkout Page", "CheckoutPage", SystemGuid.BlockTypeAttribute.CART_CHECKOUT_PAGE );
            BlockTypeAttribute( SystemGuid.BlockType.CART, "Catalog Page", "CatalogPage", SystemGuid.BlockTypeAttribute.CART_CATALOG_PAGE );
            BlockTypeAttribute( SystemGuid.BlockType.CHECKOUT, "Cart Page", "CartPage", SystemGuid.BlockTypeAttribute.CHECKOUT_CART_PAGE );
            BlockTypeAttribute( SystemGuid.BlockType.CHECKOUT, "Catalog Page", "CatalogPage", SystemGuid.BlockTypeAttribute.CHECKOUT_CATALOG_PAGE );
        }

        private void BlockType( string className, string entityTypeGuid, string blockTypeGuid, string name, string description, string category = "Registration Cart" )
        {
            var typeName = "org.secc.RegistrationCart.Blocks." + className;
            RockMigrationHelper.UpdateEntityType( typeName, name, typeName + ", " + BlockAssembly, false, false, entityTypeGuid );
            RockMigrationHelper.AddOrUpdateEntityBlockType( name, description, typeName, category, blockTypeGuid );
        }

        private void BlockTypeAttribute( string blockTypeGuid, string name, string key, string guid )
        {
            RockMigrationHelper.UpdateBlockTypeAttribute( blockTypeGuid, PageReferenceFieldTypeGuid, name, key, "", "", 0, "", guid );
        }

        #endregion

        #region Pages

        private void CreatePublicPages()
        {
            // The external site's full-width layout; looked up by name so this works on
            // any theme. Falls back to whatever layout the site has.
            var layoutGuid = ( SqlScalar( $@"
                SELECT TOP 1 CAST([Guid] AS nvarchar(36)) FROM [Layout]
                WHERE [SiteId] = (SELECT TOP 1 [Id] FROM [Site] WHERE [Guid] = '{ExternalSiteGuid}')
                ORDER BY CASE WHEN [FileName] = 'FullWidth' THEN 0 ELSE 1 END, [Id]" ) as string )?.ToUpper();

            if ( string.IsNullOrWhiteSpace( layoutGuid ) )
            {
                return; // No external site — pages can be added by hand.
            }

            Page( ExternalHomePageGuid, layoutGuid, "Registration", "Register the whole family for events in one checkout.",
                SystemGuid.Page.CAMP_CART, "fa fa-clipboard-list", "registrations", SystemGuid.PageRoute.CAMP_CART,
                SystemGuid.BlockType.CATALOG, "Catalog", SystemGuid.Block.CATALOG );

            Page( SystemGuid.Page.CAMP_CART, layoutGuid, "Offering", "",
                SystemGuid.Page.OFFERING, "", "registrations/offering/{OfferingKey}", SystemGuid.PageRoute.OFFERING,
                SystemGuid.BlockType.OFFERING_DETAIL, "Offering", SystemGuid.Block.OFFERING_DETAIL );

            Page( SystemGuid.Page.CAMP_CART, layoutGuid, "Cart", "",
                SystemGuid.Page.CART, "", "registrations/cart", SystemGuid.PageRoute.CART,
                SystemGuid.BlockType.CART, "Cart", SystemGuid.Block.CART );

            Page( SystemGuid.Page.CAMP_CART, layoutGuid, "Checkout", "",
                SystemGuid.Page.CHECKOUT, "", "registrations/checkout", SystemGuid.PageRoute.CHECKOUT,
                SystemGuid.BlockType.CHECKOUT, "Checkout", SystemGuid.Block.CHECKOUT );

            // Hide child pages from the external site's navigation, and let the blocks own
            // the page heading (they render their own title band).
            Sql( $"UPDATE [Page] SET [DisplayInNavWhen] = 2 WHERE [Guid] IN ('{SystemGuid.Page.OFFERING}','{SystemGuid.Page.CART}','{SystemGuid.Page.CHECKOUT}')" );
            Sql( $"UPDATE [Page] SET [PageDisplayTitle] = 0, [PageDisplayBreadCrumb] = 0, [PageDisplayDescription] = 0 WHERE [Guid] IN ('{SystemGuid.Page.CAMP_CART}','{SystemGuid.Page.OFFERING}','{SystemGuid.Page.CART}','{SystemGuid.Page.CHECKOUT}')" );

            // Wire the blocks' page links.
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.CATALOG, SystemGuid.BlockTypeAttribute.CATALOG_OFFERING_DETAIL_PAGE, SystemGuid.Page.OFFERING + "," + SystemGuid.PageRoute.OFFERING );
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.CATALOG, SystemGuid.BlockTypeAttribute.CATALOG_CART_PAGE, SystemGuid.Page.CART + "," + SystemGuid.PageRoute.CART );
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.OFFERING_DETAIL, SystemGuid.BlockTypeAttribute.OFFERING_DETAIL_CART_PAGE, SystemGuid.Page.CART + "," + SystemGuid.PageRoute.CART );
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.OFFERING_DETAIL, SystemGuid.BlockTypeAttribute.OFFERING_DETAIL_CATALOG_PAGE, SystemGuid.Page.CAMP_CART + "," + SystemGuid.PageRoute.CAMP_CART );
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.CART, SystemGuid.BlockTypeAttribute.CART_CHECKOUT_PAGE, SystemGuid.Page.CHECKOUT + "," + SystemGuid.PageRoute.CHECKOUT );
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.CART, SystemGuid.BlockTypeAttribute.CART_CATALOG_PAGE, SystemGuid.Page.CAMP_CART + "," + SystemGuid.PageRoute.CAMP_CART );
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.CHECKOUT, SystemGuid.BlockTypeAttribute.CHECKOUT_CART_PAGE, SystemGuid.Page.CART + "," + SystemGuid.PageRoute.CART );
            RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.CHECKOUT, SystemGuid.BlockTypeAttribute.CHECKOUT_CATALOG_PAGE, SystemGuid.Page.CAMP_CART + "," + SystemGuid.PageRoute.CAMP_CART );
        }

        private void CreateAdminPages()
        {
            Page( InternalInstalledPluginsPageGuid, InternalFullWidthLayoutGuid, "Registration Cart", "Choose which Rock registration templates and instances are sold through Registration Cart.",
                SystemGuid.Page.ADMIN_CAMP_CART, "fa fa-shopping-cart", "admin/registration-cart", SystemGuid.PageRoute.ADMIN_CAMP_CART,
                SystemGuid.BlockType.ADMIN_CATALOG_SETTINGS, "Catalog Settings", SystemGuid.Block.ADMIN_CATALOG_SETTINGS );

            Page( SystemGuid.Page.ADMIN_CAMP_CART, InternalFullWidthLayoutGuid, "Discount Codes", "",
                SystemGuid.Page.ADMIN_DISCOUNT_CODES, "fa fa-tags", "admin/registration-cart/discounts", SystemGuid.PageRoute.ADMIN_DISCOUNT_CODES,
                SystemGuid.BlockType.ADMIN_DISCOUNT_CODES, "Discount Codes", SystemGuid.Block.ADMIN_DISCOUNT_CODES );
        }

        private void Page( string parentPageGuid, string layoutGuid, string name, string description, string pageGuid, string icon,
            string route, string routeGuid, string blockTypeGuid, string blockName, string blockGuid )
        {
            RockMigrationHelper.AddPage( true, parentPageGuid, layoutGuid, name, description, pageGuid, icon );
            RockMigrationHelper.AddPageRoute( pageGuid, route, routeGuid );
            // Rock only matches a route for requests on the page's own site unless the route
            // is global. Make ours global so the pages resolve on any host name.
            Sql( $"UPDATE [PageRoute] SET [IsGlobal] = 1 WHERE [Guid] = '{routeGuid}'" );
            RockMigrationHelper.AddBlock( true, pageGuid, "", blockTypeGuid, blockName, "Main", "", "", 0, blockGuid );
        }

        #endregion
    }
}
