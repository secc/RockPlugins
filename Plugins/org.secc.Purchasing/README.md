# org.secc.Purchasing

> Southeast's in-house purchasing system — requisitions, purchase orders, receiving, vendors, payment methods, and capital requests — built on a custom LINQ-to-SQL data layer with Sage Intacct GL validation.

## Overview

Purchasing is a full procurement application embedded in Rock as a set of UI blocks. Staff create
**requisitions** (with line items, GL coding, attachments, and an approval chain), which become
**purchase orders** sent to **vendors**; items are then **received** against the PO, and
**payments** / credit-card charges are tracked. A separate **capital request** flow handles larger
purchases with competing **bids** and finance approval. Unlike most SECC plugins, it does not use
the Rock EF model — it has its own LINQ-to-SQL domain model over `_org_secc_Purchasing_*` tables,
and validates GL account/dimension coding against **Sage Intacct** via that vendor's XML API.

## Project Info

- **Project file:** `org.secc.Purchasing.csproj`
- **Root namespace:** `org.secc.Purchasing`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup,
  the PO PDF Lava template, logo), via the PostBuildEvent `xcopy`.

## Project Layout

```
/org_secc/Purchasing/   UI blocks (.ascx + .ascx.cs); PurchaseOrderPDF.lava; SE logo
/App_Code/              Domain model — Requisition, PurchaseOrder, Receipt, Vendor, Payment,
                        CapitalRequest, Approval, Note, Attachment, etc. (all : PurchasingBase)
/App_Code/DataLayer/    Purchasing.dbml LINQ-to-SQL context, ContextHelper, StaffMemberData
/App_Code/Helpers/      Person / Address / Phone helpers
/App_Code/Enums/        HistoryType
/Intacct/               Sage Intacct XML API client (Api, Auth, Functions, Model, ApiClient)
/Properties/            AssemblyInfo, Settings
```

No `/Migrations` folder — the `_org_secc_Purchasing_*` tables are not provisioned by an EF/Rock
plugin migration in this project (see Observations).

## Components

### Blocks

All blocks live under `org_secc/Purchasing/`. The detail/list blocks are `RockBlock`s; the
embedded widgets (`Attachments`, `Notes`, `StaffPicker`, `StaffSearch`, `VendorSelect`) are plain
`UserControl`s reused inside the others.

| Block (class) | Purpose | Key settings |
|---------------|---------|--------------|
| `RequisitionList` | List/filter requisitions. | Requisition Detail Page, Person Detail Page |
| `RequisitionDetail` | Create/edit a requisition: line items, GL coding, approvals, vendor, ship-to. | Validate GL Account (`ValidateAccounts`), Default Requisition Type, Default/footer/scripture text, Expedited Shipping Window, Allow New Vendor Selection, Display Inactive Items, Send-notification toggles, Prompt for Note on Decline, Purchase Order Detail Page |
| `POList` | List/filter purchase orders. | Purchase Order Detail Page, Ministry Area / Position Person Attribute |
| `PODetail` | View/manage a purchase order; receive items; render the PO PDF. | Purchase Order List Page, Requisition Detail Page, Person Detail Page, Default Ship To Name/Attention/Campus, Show Inactive Vendors, Ministry Person Attribute ID, PDF Report Lava, Receiving User Group |
| `VendorList` | List/filter vendors. | Vendor Detail Page, Active Only By Default |
| `VendorDetail` | Create/edit a vendor (preferred-vendor records, addresses, contacts). | — (no block settings) |
| `PaymentMethodList` | Manage payment methods / credit cards. | Show Active By Default, Credit Card Expriation Date Year Options |
| `CapitalRequestList` | List/filter capital requests. | Capital Request Detail Page, Ministry Area Lookup Type, Location Lookup Type, Location Admin Group, Ministry Area / SECC Location Person Attribute |
| `CapitalRequestDetail` | Create/edit a capital request with bids and finance approval. | List Page, Person Detail Page, Allow Ministry/Requester Selection, Ministry Area / Location Lookup Type, Default Title, Minimum Required Bids, Quote Document Type, Finance Approver Tag, ministry/finance/approved/returned notification templates, Requisition Detail Page |
| `Attachments` (UserControl) | Reusable file-attachment list (used by requisition / PO / capital request). | — |
| `Notes` (UserControl) | Reusable note thread. | — |
| `StaffPicker` / `StaffSearch` (UserControl) | Staff lookup widgets (back `StaffMemberData`). | — |
| `VendorSelect` (UserControl) | Vendor picker modal. | — (the "Choose Vendor Instructions" text is a `RequisitionDetail` block setting) |

### Domain Model (LINQ-to-SQL)

`App_Code/DataLayer/Purchasing.dbml` maps the tables below; each is wrapped by a hand-written
business class in `App_Code/` deriving from `PurchasingBase`. `ContextHelper.GetDBContext()`
builds the `PurchasingContext` from the **Rock connection string** (so it runs against the Rock
database, not the legacy Shelby/Arena strings still in `app.config`).

| Table | Business class |
|-------|----------------|
| `_org_secc_Purchasing_Requisition` | `Requisition` |
| `_org_secc_Purchasing_RequisitionItem` | `RequisitionItem` |
| `_org_secc_Purchasing_PurchaseOrder` | `PurchaseOrder` |
| `_org_secc_Purchasing_PurchaseOrderItem` | `PurchaseOrderItem` |
| `_org_secc_Purchasing_Receipt` / `_ReceiptItem` | `Receipt` / `ReceiptItem` |
| `_org_secc_Purchasing_Vendor` | `Vendor` (`PreferredVendor` is a code-only class, no own table) |
| `_org_secc_Purchasing_Campus` | (no business class; mapped for joins) |
| `_org_secc_Purchasing_Payment` / `_PaymentCharge` / `_PaymentMethod` / `_CreditCard` | `Payment` / `PaymentCharge` / `PaymentMethod` / `CreditCard` |
| `_org_secc_Purchasing_CapitalRequest` / `_CapitalRequestBid` | `CapitalRequest` / `CapitalRequestBid` |
| `_org_secc_Purchasing_Approval` | `Approval` |
| `_org_secc_Purchasing_Note` / `_Attachment` / `_History` | `Note` / `Attachment` / `History` |
| (Rock) `Person`, `PersonAlias`, `DefinedValue` | mapped read-only for joins |

### Lava

- `org_secc/Purchasing/PurchaseOrderPDF.lava` — the print/PDF template rendered for a purchase
  order (used by `PODetail`).

## Dependencies & Integrations

- **Rock:** `RockContext` (connection string only), `RockBlock`, Rock attribute framework
  (`DefinedTypeField`, `GroupField`, `LinkedPage`, etc.), `GlobalAttributesCache`,
  `Rock.Security.Encryption`, `RockCache` (Intacct response caching), DotLiquid (Lava).
- **Third-party:** **Sage Intacct** XML API (`https://api.intacct.com/ia/xml/xmlgw.phtml`) via
  `RestSharp`; `Newtonsoft.Json`; `System.Data.Linq` (LINQ-to-SQL); EntityFramework 6 (referenced).
- **Cross-plugin:** none at build time.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** `app.config` ships hardcoded legacy connection strings for `ShelbyDBRepl`
  (`ar-sql02`) and an `ArenaDB_ChrisF` dev database. At runtime the active context is built from the
  Rock connection string in `ContextHelper`, so these appear unused, but the stale server/catalog
  names are worth removing to avoid confusion and accidental reconnection.
- **Security (low):** Intacct API credentials are read from the `IntacctAPISettings` global
  attribute and `Rock.Security.Encryption.DecryptString`-decrypted in `RequisitionDetail`. That's
  the right place to store them; confirm the global attribute is flagged encrypted and that
  edit/view rights on it are limited to finance/admin staff.
- **Improvement:** The domain layer is custom LINQ-to-SQL (`Purchasing.dbml`) rather than the Rock
  EF model — a parallel data stack with its own connection handling, history/audit, and no plugin
  migration to create the `_org_secc_Purchasing_*` tables (they must be installed out-of-band).
  New maintainers should expect to manage schema/DDL separately from the code.
- **Improvement:** `AssemblyInfo.cs` carries a blank `AssemblyCompany` and `Copyright © 2012`
  (the file otherwise has the standard SECC license header) — the assembly metadata is stale and
  worth refreshing.

## Making Changes

- Block UI and behavior live in the matching `org_secc/Purchasing/*.ascx(.cs)`; the embedded
  `Attachments`, `Notes`, `StaffPicker`, and `VendorSelect` user controls are shared across the
  detail blocks, so a change there affects requisitions, POs, and capital requests alike.
- Business rules (validation, status transitions, history) live in the `App_Code/*.cs` class for
  the entity (e.g. `Requisition.cs`, `PurchaseOrder.cs`); the table mapping is in
  `App_Code/DataLayer/Purchasing.dbml`. Regenerate the designer if you change the dbml.
- Intacct/GL validation is gated per-block by the **Validate GL Account** (`ValidateAccounts`)
  setting and implemented in `/Intacct/`; credentials come from the `IntacctAPISettings` global
  attribute.
- The PO printout is the `PurchaseOrderPDF.lava` template, not C#.
</content>
</invoke>
