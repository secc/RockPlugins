# org.secc.RegistrationCart

> A multi-registrant "shopping cart" for Rock event registration — parents add several family
> members to several registration instances (camps, retreats, classes) and check out once.
> Obsidian public/admin blocks, a plugin migration that installs everything, and a checkout that
> charges Rock's configured gateway and writes real Rock Registrations plus one batched
> FinancialTransaction.

## Overview

**The catalog is Rock's own registration setup.** A registration template is an *offering*, its
active instances (inside their registration window) are the *options*, and the template's fees
are the *add-ons*. Staff set up a camp the normal Rock way and it appears in the cart — nothing
is configured twice. The plugin stores only what Rock lacks:

- cart contents (cart, registrants, line items, add-ons, applied discounts),
- cross-registration **discount codes** (separate from Rock's per-template discounts),
- a small **overlay** per template / instance / fee item: show in cart, grade-band or adult
  eligibility, catalog image and description, sort order.

Parents sign in with their Rock login; registrants come from their Rock family (and the
Offering block can add a new child to the family). Eligibility uses coarse **grade bands**
(Elementary, Middle School, High School, Adults) derived from Rock's `GradeOffset`.

Checkout (`Services/CheckoutProcessor.cs`) mirrors Rock's stock RegistrationEntry block:
re-validate the cart → charge the gateway token once (`IGatewayComponent.Charge`, creating a
customer vault record first for Obsidian-hosted gateways) → in one DB transaction create one
`Registration` + `RegistrationRegistrant` (+ `RegistrationRegistrantFee` rows) per line, one
`FinancialTransaction` in a batch named by the block's *Batch Name Prefix* with a detail line per
registration posting to the instance's account, then mark the cart **CheckedOut**. The cart row
is the idempotency record. If registration creation fails *after* a successful charge, the cart
is still closed with `LastCheckoutError` set (and the exception logged with the transaction code)
so support can reconcile; the shopper is told not to pay again.

Originally built (and verified end to end against Rock 16.9 with the Test Gateway) as a
standalone `com.secc.ShoppingCart` project; ported into this repo on 2026-10-13 with the
`org.secc` naming, repo build conventions, and the pricing domain folded into the plugin
assembly. It has **not** been installed on rockalpha or production yet.

## Project Info

- **Projects:** `org.secc.RegistrationCart` (C# plugin), `org.secc.RegistrationCart.Obsidian`
  (Vue/TypeScript frontend), `org.secc.RegistrationCart.Tests` (xunit)
- **Root namespace:** `org.secc.RegistrationCart`
- **Target framework:** .NET Framework 4.7.2 (SDK-style csproj, `LangVersion latest`;
  `RockRMS.Rock` / `RockRMS.Rock.Blocks` NuGet packages `1.16.9-rc.1`)
- **Deploys to:** `RockWeb/Bin/org.secc.RegistrationCart.dll` (custom `CopyPluginToRockWebBin`
  target) and `RockWeb/Plugins/org_secc/RegistrationCart/blocks/*.obs.js` (`npm run deploy`;
  a solution rebuild does **not** copy the JS)
- **Packaging:** `plugin.json` + `BuildPlugin.ps1` (invoked by the pipeline's existing
  BuildPlugin hook; the agent needs Node/npm)

## Project Layout

```
org.secc.RegistrationCart/              C# plugin assembly
  Blocks/                               4 public + 2 admin RockBlockType classes (+ base classes)
  Domain/                               Pure pricing/eligibility engine (no Rock types): Enums,
                                        Entities (pricing model), Pricing/PricingEngine,
                                        Pricing/EligibilityService, Pricing/PricedCart
  Migrations/001_CreateRegistrationCart Tables, entity/block types, pages, routes, blocks
  Model/                                9 entities: 3 overlays, DiscountCode, 5 cart tables
  Services/                             CatalogService, CartManager, CartPricing,
                                        CheckoutProcessor, GradeBands
  ViewModels/                           Bags/boxes shared with the Obsidian blocks
  SystemGuid.cs                         Every GUID the plugin owns — never change
org.secc.RegistrationCart.Obsidian/     Vue 3 / Obsidian frontend
  src/blocks/*.obs                      One entry per block
  src/shared/*                          Partials (cart nav, order summary, tokenizer, ...)
  src/viewModels.d.ts                   Generated from the C# bags (npm run viewmodels)
org.secc.RegistrationCart.Tests/        xunit: PricingEngine, EligibilityService, GradeBands
plugin.json / BuildPlugin.ps1           Rock plugin packaging
```

## Components

### Blocks (Obsidian)

| Block | Page (created by migration) | What it does | Settings |
|-------|-----------------------------|--------------|----------|
| Registration Cart Catalog (`Catalog`) | External › Registration (`/registrations`) | Lists visible templates with an open instance. | Heading, Intro, Offering Detail Page, Cart Page |
| Registration Cart Offering (`OfferingDetail`) | `/registrations/offering/{OfferingKey}` | Pick a family member (or add a child), an instance and fees; add to cart. | Allow Adding Family Members, Cart Page, Catalog Page |
| Registration Cart (`Cart`) | `/registrations/cart` | Cart with pricing; apply/remove discount codes; remove lines. | Checkout Page, Catalog Page |
| Registration Cart Checkout (`Checkout`) | `/registrations/checkout` | Payment + checkout. Uses the gateway's Obsidian hosted control when it has one, else a My Well tokenizer iframe. | Financial Gateway, Batch Name Prefix, Tokenizer Script URL / Gateway URL / Public Key, Cart Page, Catalog Page |
| Registration Cart Settings (`AdminCatalogSettings`) | Admin Tools › Installed Plugins › Registration Cart (`/admin/registration-cart`) | Per template/instance/fee item visibility, eligibility, image, order. Saves require block EDIT. | Catalog Page |
| Registration Cart Discount Codes (`AdminDiscountCodes`) | child of the above (`/admin/registration-cart/discounts`) | CRUD for discount codes (order / per-line / per-offering scope, amount or percent, date window). Saves require block EDIT. | — |

### Data model (`_org_secc_RegistrationCart_*`)

`TemplateSetting`, `InstanceSetting`, `FeeItemSetting` (overlays, cascade-deleted with the Rock
row), `DiscountCode`, and the cart graph `Cart` → `CartRegistrant` / `CartLineItem` →
`CartLineItemAddOn`, plus `CartDiscount`. Line items and discounts **snapshot** prices/values at
add time.

### Migrations

| # | What |
|---|------|
| 001 | Creates the 9 tables, registers entity + block types, creates the External pages under the stock External site's home page (if the site exists) and the admin pages under Installed Plugins, wires the blocks' linked-page settings. Page routes are set **global**. |

## Configuration

1. **Site domains:** Rock resolves the site from the request host name; make sure the public host
   is in the External site's domains and the admin host in the internal site's.
2. **Checkout block:** pick the Financial Gateway. If it implements
   `IObsidianHostedGatewayComponent` its hosted control is used; otherwise fill in the
   Tokenizer Fallback settings (public key only — card data never touches Rock).
3. **Registration instances:** every instance sold through the cart needs an **Account**. Price
   is the instance cost when the template sets cost on instance, else the template cost. Campus
   and event dates come from the instance's event linkage when present.
4. **Catalog settings / discount codes:** Admin Tools › Installed Plugins › Registration Cart.
5. **Security:** public pages work for any signed-in person (anonymous visitors get a sign-in
   prompt); admin blocks need EDIT (inherited from Installed Plugins).

## Dependencies & Integrations

- **Rock:** `RockBlockType` (Obsidian), Registration/RegistrationInstance/Template/Fee services,
  `PersonService` (family members, `AddPersonToFamily`), `IGatewayComponent` /
  `IObsidianHostedGatewayComponent`, `FinancialBatchService`, `HistoryService`,
  `Rock.Plugin` migrations.
- **NuGet:** `RockRMS.Rock` / `RockRMS.Rock.Blocks` `1.16.9-rc.1`, SparkDevNetwork Build Tasks +
  Analyzers; `DotLiquid` referenced from `RockWeb\Bin` with `Private=false`.
- **Frontend:** `@rockrms/obsidian-framework`; the My Well tokenizer script (fallback path only).
- **Cross-plugin:** none. Note that it does **not** go through SECC's `RegistrationEntry` fork in
  [org.secc.Event](../org.secc.Event/README.md).

## Tests

- `org.secc.RegistrationCart.Tests` — xunit over the pure logic: `PricingEngineTests`,
  `EligibilityServiceTests`, `GradeBandsTests`. No RockContext.

## Observations

*From the pre-port assessment (2026-10-13) — fix before production use.*

- **Double charge race:** `CheckoutProcessor.Process` checks `Status == CheckedOut` and then
  charges with no lock; two concurrent checkouts (two tabs, a retry after a timeout) can both
  charge. Needs an atomic Open → Processing transition before the gateway call.
- **Charged total isn't confirmed:** `Checkout.ProcessCheckout` charges whatever the open cart
  prices to *now*; the client should send the total it displayed and the server should refuse
  on mismatch.
- **Discount posting:** `DiscountShares` spreads the whole cart discount proportionally across
  *all* lines, so an offering-scoped code also reduces other instances' account postings.
- **Capacity / window:** capacity and the registration window are checked only when a line is
  added; `Validate()` doesn't re-check them at checkout and carts don't hold spots.
- **Gateway support:** works with Obsidian-hosted gateways (Rock NMI, Test Gateway) or the My
  Well tokenizer. The PayFlow Pro gateway (`org.secc.PayFlowPro`) supports neither.
- **Bypasses registration side effects:** Registration rows are written directly — no registrant
  forms/attributes, digital signatures (SignNow), registration workflows, linked-group
  placement, confirmation emails, or PersonMatch.
- **Add child:** creates a Pending person with no birthdate/gender and a representative grade
  (`GradeBands.GradeOffsetFor` — e.g. Elementary → 3rd grade); duplicate-prone.
- **Migration placement:** pages hang off Rock's *stock* External site/home page, and all routes
  (including `/admin/registration-cart`) are made global.
- **Business rules to confirm:** discount codes stack (by design, see `PricingEngineTests`);
  separate code system from Rock template discounts; sign-in required (no guest checkout).

## Making Changes

- **Backend:** edit the C# project and rebuild — `CopyPluginToRockWebBin` drops the DLL into
  `RockWeb\Bin`. Schema changes go in a **new numbered migration**; never change GUIDs in
  `SystemGuid.cs`. Entities need `[DataContract]` + `IRockEntity`; blocks override
  `ObsidianFileUrl`; bags use ints for enums.
- **Pricing rules** live in `Domain/Pricing` (pure, unit-tested) — `Services/CartPricing`
  projects the Rock cart into it. Keep `Domain/` free of Rock types.
- **Frontend:** edit `src/**` in `org.secc.RegistrationCart.Obsidian`, then `npm run deploy`
  (hard-refresh the browser). After changing a bag/box class, rebuild Debug and run
  `npm run viewmodels`.
- **Packaging:** `BuildPlugin.ps1` + `plugin.json` (Release DLL + `dist/**`).

Last updated: 2026-10-13
