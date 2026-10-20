# org.secc.RegistrationCart

> A multi-registrant "shopping cart" for Rock event registration — parents add several family
> members to several registration instances (camps, retreats, classes) and check out once.
> Obsidian public/admin blocks, plugin migrations that install everything, and a checkout that
> charges Rock's configured gateway and writes real Rock Registrations plus one batched
> FinancialTransaction.

## Overview

**The catalog is Rock's own registration setup.** A registration template is an *offering*, its
active instances (inside their registration window) are the *options*, and the template's fees
are the *add-ons*. Staff set up a camp the normal Rock way and turn its template on in the
cart's settings; nothing is configured twice. The plugin stores only what Rock lacks:

- cart contents (cart, registrants, line items, add-ons, applied discounts),
- cross-registration **discount codes** (separate from Rock's per-template discounts; the one
  exception is the financial-assistance code, below),
- a small **overlay** per template / instance / fee item: show in cart, grade-band or adult
  eligibility, catalog image and description, sort order.

Parents sign in with their Rock login; registrants come from their Rock family (and the
Offering block can add a new child to the family). Eligibility uses coarse **grade bands**
(Elementary, Middle School, High School, Adults) derived from Rock's `GradeOffset`.

**Seats are counted the way Rock counts them** (`Services/SeatCounter`, the same count for the
catalog, adding to the cart and checkout): a blank *Max Attendees* is unlimited and 0 means no
seats; every registrant on a non-temporary registration counts, wait list included; Rock
registration sessions count when the instance has Rock's registration timeout on; lines in
other carts mid-checkout count. **Open carts hold no seats**: whoever checks out first gets
them. A cart's campers for one camp take seats in the order they were added
(`Services/SeatPlanner`); the rest go on the **wait list** when the template has *Wait List
Enabled*, else the camp is full for them.

Checkout (`Services/CheckoutProcessor.cs`) mirrors Rock's stock RegistrationEntry block:
claim the cart (one conditional `UPDATE … WHERE Status = Open` moves it to **Processing**, so
only one request can charge it) → place its campers in seats again and re-check everything
that could stop the registrations saving: each instance and template still active and shown in
the cart, registration window open, an account set when something is due, extras still
offered and within Rock's fee item *Maximum Usage*, no camper already registered or on an
unconfirmed checkout, each camper's form answers complete for where they are placed and
acceptable to Rock, and each signed document still matching (it is rendered from the saved
answers and checked against Rock's rules here, before the charge). If a camper moved onto or
off the wait list, or the cart no longer prices to the total the page showed, nothing is
charged and the page shows the cart as it is now → charge the gateway token once
(`IGatewayComponent.Charge`, creating a customer vault record first for Obsidian-hosted
gateways) → in **one DB transaction**: one `Registration` + `RegistrationRegistrant`
(+ `RegistrationRegistrantFee` rows, registrant attribute answers and the signed document) per
line, one `FinancialTransaction` in a batch named by the block's *Batch Name Prefix* with a
detail line per paid registration posting to the instance's account, and the cart marked
**CheckedOut**. Only what must stand with the payment is in that transaction. Each detail line
(and the registration's `DiscountAmount` / `DiscountCode`) is net of only the discounts that
applied to that line, in whole cents, so the details add up exactly to the charge. The cart
row is the idempotency record: a second request for the same cart is told the checkout is in
progress or already done. The cart goes back to **Open** only when nothing was charged
(declined, or stopped before the gateway call). If registration creation fails *after* a
successful charge, the cart is still closed with `LastCheckoutError` set (and the exception
logged with the transaction code) so support can reconcile; the shopper is told not to pay
again. If the gateway call fails without an answer, the cart stays **Processing** (see
Observations).

After the registrations are committed, each step runs on its own (a fresh context, its own
error handling), so one failing never skips the others or undoes the registrations or the
payment: the signed documents' PDFs and the completion email that carries them; the person's
form answers (person fields, family campus and address, person attributes, history); registrar
notes and history; the instance's linked group for registrants not on the wait list (the
registration's `GroupId` comes from the instance's first linkage with a group; the member gets
the template's role and status, then the group member answers); Rock's confirmation email and
staff notification; the template's registrant workflow; and each of the template and instance
registration workflows.

**Financial assistance.** When the Offering block's *Discount Code Workflow* is set (prod: WF 725
"Automatic Financial Assistance"), it runs as each camper is added to the cart, the way SECC's
RegistrationEntry fork ran it: the entity is a dictionary with `RegistrationInstance` and
`RegistrationInfo` (the camper's form answers as registrant field values), and a code the
workflow applies (725 makes a single-use `FA-` template discount: the $50/$100 tier, or the
template cost less $20 for "More") is stored on that line. As in Rock, a template discount
applies to the base price and the fees whose *Discount Applies* is set (a percentage of them, a
flat amount capped at them), so a fee that doesn't allow discounts stays due. It comes off that
line only and no cart code stacks on it. Checkout records it as the registration's discount
code, so the template's registration workflow (728) then launches the FA evaluation as before.
A wait-listed line keeps its code on the registration for when a seat opens.

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
                                        CheckoutRules (seats, fee limits, checkout outcomes),
                                        AnswerRules, Entities (pricing model),
                                        Pricing/PricingEngine, Pricing/DiscountAllocator,
                                        Pricing/EligibilityService, Pricing/PricedCart
  Jobs/CartMaintenance                  Abandons idle carts, retries signed-document PDFs,
                                        reports unconfirmed checkouts
  Migrations/001–011                    Tables, entity/block types, pages, routes, blocks,
                                        settings, the job (see Migrations)
  Model/                                9 entities: 3 overlays, DiscountCode, 5 cart tables
  Services/                             CatalogService, CartManager, CartPricing, CartSettings,
                                        CheckoutProcessor, CheckoutProblem, DiscountCodeWorkflow,
                                        FeeItemUsage, GradeBands, RegistrationForms,
                                        RegistrationSignatures, SeatCounter, SeatLock, SeatPlanner
  ViewModels/                           Bags/boxes shared with the Obsidian blocks
  SystemGuid.cs                         Every GUID the plugin owns — never change
org.secc.RegistrationCart.Obsidian/     Vue 3 / Obsidian frontend
  src/blocks/*.obs                      One entry per block
  src/shared/*                          Partials (cart nav, pending-checkout notice, forms,
                                        order summary, tokenizer, ...)
  src/viewModels.d.ts                   Generated from the C# bags (npm run viewmodels)
org.secc.RegistrationCart.Tests/        xunit: pricing, allocation, eligibility, grade bands,
                                        checkout rules, answer rules
plugin.json / BuildPlugin.ps1           Rock plugin packaging
```

## Components

### Blocks (Obsidian)

| Block | Page (created by migration) | What it does | Settings |
|-------|-----------------------------|--------------|----------|
| Registration Cart Catalog (`Catalog`) | External › Registration (`/registrations`), hidden from navigation | Lists templates turned on in the cart's settings that have an open instance. | Heading, Intro, Offering Detail Page, Cart Page |
| Registration Cart Offering (`OfferingDetail`) | `/registrations/offering/{OfferingKey}` | Pick a family member (or add a child), an instance and fees, and fill in the template's registration forms; add to cart. With `?LineId=` it edits that cart line instead (see *Editing a line*). | Allow Adding Family Members, Discount Code Workflow, Cart Page, Catalog Page |
| Registration Cart (`Cart`) | `/registrations/cart` | Cart with pricing; apply/remove discount codes; edit or remove lines; shows what would stop a line checking out. | Checkout Page, Catalog Page, Offering Detail Page |
| Registration Cart Checkout (`Checkout`) | `/registrations/checkout` | Payment + checkout. Uses the gateway's Obsidian hosted control when it has one, else a My Well tokenizer iframe. Lines that need fixing show with an Edit link. | Financial Gateway, Batch Name Prefix, Tokenizer Script URL / Gateway URL / Public Key, Cart Page, Catalog Page, Offering Detail Page |
| Registration Cart Settings (`AdminCatalogSettings`) | Admin Tools › Installed Plugins › Registration Cart (`/admin/registration-cart`) | Per template/instance/fee item visibility, eligibility, image, order. Saves require block EDIT. | Catalog Page |
| Registration Cart Discount Codes (`AdminDiscountCodes`) | child of the above (`/admin/registration-cart/discounts`) | CRUD for discount codes (order / per-line / per-offering scope, amount or percent, date window). Saves require block EDIT. | — |

Every public page shows a warning while the signed-in person has an unconfirmed checkout (see
*Carts stuck in Processing*).

### Job

**Registration Cart Maintenance** (`Jobs/CartMaintenance`, added by migration 007, every 15
minutes; Admin Tools › System Settings › Jobs Administration):

| Setting | Default | What |
|---------|---------|------|
| Abandon Open Carts After (days) | 30 | An open cart with no changes for this long becomes *Abandoned*; the family's next visit starts a new cart. 0 = off. |
| Retry Signed Document PDFs After (minutes) | 10 | A signed document from a cart checkout still without its PDF gets it made, and its completion email sent. |
| Report Unconfirmed Checkouts After (minutes) | 30 | An unconfirmed checkout this old ends the job with a warning listing it (once per checkout), so the job's notification email reaches whoever reconciles payments. Set the job's *Notification Status* to Error and its *Notification Emails*. 0 = off. |

### Global attributes

| Attribute | Default | What |
|-----------|---------|------|
| Registration Cart: Show New Templates | False | Whether a template the cart's settings haven't been saved for shows in the catalog. |
| Registration Cart: Default Grade Bands | Elementary, Middle School, High School | Who an instance or extra with no saved setting can be registered for. |
| Registration Cart: Prevent Duplicate Registrations | True | Refuse a camper already registered for the option (wait list included). |

### Data model (`_org_secc_RegistrationCart_*`)

`TemplateSetting`, `InstanceSetting`, `FeeItemSetting` (overlays, cascade-deleted with the Rock
row), `DiscountCode`, and the cart graph `Cart` → `CartRegistrant` / `CartLineItem` →
`CartLineItemAddOn`, plus `CartDiscount`. Line items and discounts **snapshot** prices/values at
add time (an add-on also snapshots its fee's *Discount Applies*).

Rock can delete what carts point at (migration 006): deleting an instance deletes its cart
lines; deleting a registration or a transaction clears the cart's link to it; deleting a
template clears an offering-scoped discount code's template; an add-on doesn't reference its
fee item in the database (it keeps its snapshot name and price, and checkout asks the family to
edit a line whose extra was deleted or turned off).

### Migrations

| # | What |
|---|------|
| 001 | Creates the 9 tables, registers entity + block types, creates the External pages under the stock External site's home page (if the site exists) and the admin pages under Installed Plugins, wires the blocks' linked-page settings. Page routes are set **global**. |
| 002 | Adds `CartLineItem.IsWaitList`. |
| 003 | Adds `CartLineItem.FieldValuesJson` (form answers) and `CartLineItem.SignatureData`. |
| 004 | Adds `CartLineItem.TemplateDiscountCode` / `TemplateDiscountAmount` / `TemplateDiscountPercentage` (the financial-assistance code). |
| 005 | Adds the Cart block's *Offering Detail Page* setting (where a line's Edit link goes) and points the cart page's block at the offering page. |
| 006 | Foreign keys to Rock tables stop blocking Rock's deletes (see *Data model*); adds an index on `CartLineItem.RegistrationInstanceId`. |
| 007 | Adds the Registration Cart Maintenance job. |
| 008 | Adds the Checkout block's *Offering Detail Page* setting and points it at the offering page. |
| 009 | Adds the *Registration Cart: Prevent Duplicate Registrations* global attribute. |
| 010 | Adds `CartLineItemAddOn.DiscountApplies`, filled from each add-on's fee. |
| 011 | Adds the *Show New Templates* and *Default Grade Bands* global attributes and hides the catalog page from navigation. |

## Configuration

1. **Site domains:** Rock resolves the site from the request host name; make sure the public host
   is in the External site's domains and the admin host in the internal site's.
2. **Checkout block:** pick the Financial Gateway. If it implements
   `IObsidianHostedGatewayComponent` its hosted control is used; otherwise fill in all three
   Tokenizer Fallback settings (script URL, gateway URL and public key — there are no sandbox
   defaults; card data never touches Rock).
3. **Registration instances:** an instance needs an **Account** when something is due for it
   (free events can do without). Price is the instance cost when the template sets cost on
   instance, else the template cost. Campus and event dates come from the instance's event
   linkage when present.
4. **Catalog settings / discount codes:** Admin Tools › Installed Plugins › Registration Cart.
   Turn on each template that should be sold through the cart, and set who each instance and
   extra is for. Link the catalog page from the site's navigation when it's ready.
5. **Financial assistance:** set the Offering block's *Discount Code Workflow* to WF 725
   (`fd55a10c-e44a-4a02-b347-37cb9f67be66`), as block 4171 had it for the fork. Leave the
   templates' registration workflow (728) as it is.
6. **Maintenance job:** set its notification emails (see *Job*).
7. **Security:** public pages work for any signed-in person (anonymous visitors get a sign-in
   prompt); admin blocks need EDIT (inherited from Installed Plugins).

## Dependencies & Integrations

- **Rock:** `RockBlockType` (Obsidian), Registration/RegistrationInstance/Template/Fee services,
  `PersonService` (family members, `AddPersonToFamily`), `IGatewayComponent` /
  `IObsidianHostedGatewayComponent`, `FinancialBatchService`, `HistoryService`, `RockJob`,
  `Rock.Plugin` migrations.
- **NuGet:** `RockRMS.Rock` / `RockRMS.Rock.Blocks` `1.16.9-rc.1`, SparkDevNetwork Build Tasks +
  Analyzers; `DotLiquid` referenced from `RockWeb\Bin` with `Private=false`.
- **Frontend:** `@rockrms/obsidian-framework`; the My Well tokenizer script (fallback path only).
- **Cross-plugin:** none. Note that it does **not** go through SECC's `RegistrationEntry` fork in
  [org.secc.Event](../org.secc.Event/README.md).

## Tests

- `org.secc.RegistrationCart.Tests` — xunit over the pure logic: `PricingEngineTests`,
  `EligibilityServiceTests`, `GradeBandsTests`, `CheckoutRulesTests` (where a stopped
  checkout leaves the cart; unconfirmed checkouts; the shown-total check; registration window;
  seats counted as Rock counts them; fee item limits), `AnswerRulesTests` (when a stored answer
  is blank), `DiscountAllocationTests` (which lines and accounts each discount comes off; codes
  capped at their own lines; *Discount Applies*; postings add up to the charge, including 500
  random carts, and carts with financial-assistance codes). No RockContext.

## Observations

*From the pre-port assessment (2026-10-13) and the PR review (2026-10-20).*

- **Carts stuck in Processing:** a cart left with `Status = 3` (Processing) means the gateway
  call failed without an answer (`LastCheckoutError` starts "Payment outcome unknown") or the
  server stopped mid-checkout; a cart CheckedOut with `LastCheckoutError` and no
  `FinancialTransactionId` was charged but its registrations failed. Either way the payer sees
  a warning on every page, their campers can't be added for that option again, and the
  maintenance job reports the cart once. Support: check the gateway for a charge from the payer
  at that time, then set `Status` to 1 (CheckedOut — charged; create the registrations by hand,
  the answers are on the cart lines) or 0 (Open — not charged; the shopper can check out again),
  and clear `LastCheckoutError` once reconciled. The cart's lines keep counting as seats while
  it is Processing.
- **Total check is total-only:** checkout compares the amount, not the contents. A cart that
  changed in another tab to different items with the same total is charged and registered as
  it is now.
- **Seats go to whoever pays first:** open carts hold no seats; the catalog's "spots
  remaining" is what's left for the viewer's next camper. Claim, validate and seat placement run
  under a per-instance SQL app lock (`SeatLock`, held for milliseconds, not through the gateway
  call). Rock's own registration and staff adding registrants don't take that lock, so one of
  them can take the last seat while a cart's payment is in flight.
- **Wait list:** follows the template's *Wait List Enabled*. Wait-listed registrants count
  against *Max Attendees* as in Rock, so a seat that frees up stays with the wait list until
  staff move someone off it. A full camp can be added as a wait-list line ($0, left out of
  discounts); placement is worked out again whenever the cart changes and at checkout (cart
  order), and the checkout page says when someone moved. A camper who comes off the wait list
  must answer the questions wait-listed registrants skip before the cart can be paid (the line
  shows an Edit link). Wait-listed campers become `OnWaitList` registrants with no transaction
  detail; staff move them off the list in Rock, which bills the balance. The confirmation email
  is Rock's, so the template's Lava decides what a wait-listed registrant is told.
- **Fee item limits:** Rock's fee item *Maximum Usage* (per instance) is enforced: sold-out items
  show "(none remaining)", can't be added, and a required fee whose items are all sold out stops
  being required, as in Rock.
- **Gateway support:** works with Obsidian-hosted gateways (Rock NMI, Test Gateway) or the My
  Well tokenizer. The PayFlow Pro gateway (`org.secc.PayFlowPro`) supports neither.
- **Registration forms:** the offering page shows the template's forms for the chosen camper
  with Rock's own field controls, required-field validation and show/hide rules
  (`Services/RegistrationForms`, a port of Rock's RegistrationEntry form handling; Rock's form
  bags are reused as-is). The page follows Rock's order: forms, then the fees (headed by the
  template's *Fee Term*, e.g. "Track Times"), then the signature, so text a template puts after
  its last form question (such as a fee disclosure) sits right above the fees, as in Rock.
  The server checks the answers too, when a line is added or changed and before checkout
  charges: required fields for where the camper is placed (fields with visibility rules are left
  to the browser, as in Rock), and email, middle name, phone and address against Rock's rules.
  "Lock if values exist" fields with a value on file are shown disabled, and the value on file
  is what the cart keeps. Answers are stored on the cart line and written at checkout the way
  Rock writes them: registrant attributes with the registration; person fields (name excepted;
  address and campus go to the family) and person attributes to the person right after the
  registrations commit; group member attributes once the camper is in the group. Fields hidden
  from wait-listed registrants in Rock are hidden for wait-list lines. Not supported:
  PersonMatch (registrants are always existing family members).
- **Editing a line:** until checkout completes, each cart line has an Edit link that reopens the
  offering page with its option, extras and answers filled in; *Save changes* updates the same
  line under the same rules as adding it (eligibility, capacity, Rock's fee rules, the form
  questions). The camper can't change (remove and add instead), and the option can only move
  within the same template. Unchanged choices keep their snapshot prices. The signature carries
  over while the camper, option and answers are the ones it was signed for; otherwise the page
  asks for the document to be signed again (rendered with the new answers) and signing saves the
  changes. The discount code workflow runs again only when the option or answers changed (so an
  FA answer change gets a new code).
- **Inline signature:** when the template requires its signature document embedded, the
  offering page renders it from the answers (Rock's Registration/Registrant Lava merge fields)
  and the family signs with Rock's electronic signature control when adding the camper; a
  still-valid earlier signature is reused. The signed data is encrypted and bound by hash to the
  camper, the option, the template's answers and the HTML, and a line can't be saved with a
  signature made for other answers. At checkout the document is rendered again from the saved
  answers and checked before the charge, saved with the registration (so every paid
  registration has its document), and its PDF made right after commit; the maintenance job
  retries a PDF that failed and sends the completion email then.
- **Deleting in Rock:** templates, instances, fees, registrations and transactions can be deleted
  in Rock as usual (see *Data model* for what happens to cart rows).
- **Financial assistance:** 725 runs on every add (it's a persisted workflow type, so each add
  leaves a Workflow row), and a code is generated even if the line is later removed; unused
  `FA-` codes stay on the template unused. Removing and re-adding a camper makes a new code. A
  line with an FA code takes no cart code (one code per camper). 725 has no branch for the
  1st–2nd "$25" tier, so it gets the cost-less-$20 amount, as it does on prod today.
- **Add child:** creates a Pending person with no birthdate/gender and a representative grade
  (`GradeBands.GradeOffsetFor` — e.g. Elementary → 3rd grade); duplicate-prone.
- **Migration placement:** pages hang off Rock's *stock* External site/home page (the catalog
  page hidden from navigation), and all routes (including `/admin/registration-cart`) are made
  global.
- **Business rules to confirm:** cart discount codes stack with each other (by design, see
  `PricingEngineTests`) but never with a financial-assistance code; whether cart codes should
  honour a fee's *Discount Applies* the way template codes do; separate code system from Rock
  template discounts; sign-in required (no guest checkout).

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

Last updated: 2026-10-20
