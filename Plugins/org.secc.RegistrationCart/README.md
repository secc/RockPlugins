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
claim the cart (one conditional `UPDATE … WHERE Status = Open` moves it to **Processing**, so
only one request can charge it) → read and re-validate the cart (each instance and template
still active and shown in the cart, registration window open, an account set) and place its
campers in the seats left (registrants + seated lines in other carts mid-checkout count
against *Max Attendees*; this cart's campers take seats in the order they were added, and the
rest go on the **wait list** when the template has *Wait List Enabled*, else checkout is
refused). If a camper moved onto or off the wait list, or the cart no longer prices to the
total the page showed, refuse with the new total (the page shows it; nothing is charged) and
the shopper pays again → charge the gateway token once
(`IGatewayComponent.Charge`, creating a customer vault record first for Obsidian-hosted
gateways) → in one DB transaction create one `Registration` + `RegistrationRegistrant`
(+ `RegistrationRegistrantFee` rows) per line, one `FinancialTransaction` in a batch named by the
block's *Batch Name Prefix* with a detail line per registration posting to the instance's
account, then mark the cart **CheckedOut**. Each detail line (and the registration's
`DiscountAmount` / `DiscountCode`) is net of only the discounts that applied to that line: the
pricing engine splits each code across its own lines in whole cents (offering codes over the
matching lines, order and percent codes by line total, per-line amounts evenly), so the
details add up exactly to the charge. The cart row is the idempotency record: a second
request for the same cart is told the checkout is in progress or already done. The cart goes
back to **Open** only when nothing was charged (declined, or stopped before the gateway call).
If registration creation fails *after* a successful charge, the cart is still closed with
`LastCheckoutError` set (and the exception logged with the transaction code) so support can
reconcile; the shopper is told not to pay again. If the gateway call fails without an answer,
the cart stays **Processing** (see Observations).

After the registrations are committed, checkout does what Rock's own RegistrationEntry block
does after saving a new registration: registrar notes and history, puts registrants who aren't
wait-listed in the instance's linked group (the registration's `GroupId` comes from the
instance's first linkage with a group; the member gets the template's role and status), queues
Rock's confirmation email and staff notification, and launches the template's registrant
workflow and the template and instance registration workflows. A failure there is logged and
never undoes the registrations or the payment.

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
                                        CheckoutRules, Entities (pricing model),
                                        Pricing/PricingEngine, Pricing/DiscountAllocator,
                                        Pricing/EligibilityService, Pricing/PricedCart
  Migrations/001_CreateRegistrationCart Tables, entity/block types, pages, routes, blocks
  Model/                                9 entities: 3 overlays, DiscountCode, 5 cart tables
  Services/                             CatalogService, CartManager, CartPricing,
                                        CheckoutProcessor, GradeBands, RegistrationForms, SeatLock
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
| Registration Cart Offering (`OfferingDetail`) | `/registrations/offering/{OfferingKey}` | Pick a family member (or add a child), an instance and fees, and fill in the template's registration forms; add to cart. | Allow Adding Family Members, Cart Page, Catalog Page |
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
| 002 | Adds `CartLineItem.IsWaitList`. |
| 003 | Adds `CartLineItem.FieldValuesJson` (form answers) and `CartLineItem.SignatureData`. |

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
  `EligibilityServiceTests`, `GradeBandsTests`, `CheckoutRulesTests` (where a stopped
  checkout leaves the cart; the shown-total check; registration window and seats), `DiscountAllocationTests` (which lines and
  accounts each discount comes off; postings add up to the charge, including 500 random
  carts). No RockContext.

## Observations

*From the pre-port assessment (2026-10-13) — fix before production use.*

- **Carts stuck in Processing:** a cart left with `Status = 3` (Processing) means the gateway
  call failed without an answer (`LastCheckoutError` starts "Payment outcome unknown") or the
  server stopped mid-checkout. The shopper can't retry it (their next visit starts a new cart),
  and its lines keep holding seats. Support: check the gateway for a charge from the payer at
  that time, then set `Status` to 1 (CheckedOut — charged; create the registrations by hand) or
  0 (Open — not charged; the shopper can check out again).
- **Total check is total-only:** checkout compares the amount, not the contents. A cart that
  changed in another tab to different items with the same total is charged and registered as
  it is now.
- **Discount larger than its own lines:** the engine clamps each code against the whole cart's
  remaining value, not just the lines it applies to. So an offering code worth more than its
  lines have left (a flat amount above the line price, or stacked after an order discount)
  still takes the full amount, and the excess posts against other lines. Capping it instead
  would change totals, so it waits on the stacking-rules decision
  (`DiscountAllocationTests` pins the current behavior).
- **Seats go to whoever pays first:** carts don't hold seats at checkout. The catalog's "spots
  remaining" counts seated lines in open carts, but checkout counts only registrants and
  carts mid-checkout, so a camp can show full to one family while another's open cart is
  abandoned. Claim, validate and seat placement run under a per-instance SQL app lock
  (`SeatLock`, held for milliseconds, not through the gateway call). Registrants added outside
  the cart (staff in Rock) don't take that lock.
- **Wait list:** follows the template's *Wait List Enabled*. A full camp can be added as a
  wait-list line ($0, left out of discounts); at checkout lines move onto or off the wait list
  as seats allow (cart order), and the shopper re-confirms any change. Wait-listed campers
  become `OnWaitList` registrants with no transaction detail; staff move them off the list in
  Rock, which bills the balance. The confirmation email is Rock's, so the template's Lava
  decides what a wait-listed registrant is told.
- **Gateway support:** works with Obsidian-hosted gateways (Rock NMI, Test Gateway) or the My
  Well tokenizer. The PayFlow Pro gateway (`org.secc.PayFlowPro`) supports neither.
- **Registration forms:** the offering page shows the template's forms for the chosen camper
  with Rock's own field controls, required-field validation and show/hide rules
  (`Services/RegistrationForms`, a port of Rock's RegistrationEntry form handling; Rock's form
  bags are reused as-is). Answers are stored on the cart line and written at checkout the way
  Rock writes them: person fields (name excepted; address and campus go to the family) and
  person attributes to the person, registrant attributes to the registrant, group member
  attributes once the camper is in the group; "lock if values exist" is honoured. Fields hidden
  from wait-listed registrants in Rock are hidden for wait-list lines. Not yet: the inline
  signature document, editing answers from the cart, and PersonMatch (registrants are always
  existing family members).
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
