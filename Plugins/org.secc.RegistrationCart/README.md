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

- cart contents (cart, registrants, line items with their discount code, add-ons),
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
acceptable to Rock (an uploaded file must still exist), and each signed document still matching
(signed for this camper, option and answers, on the document as staff have it now, and checked
against Rock's rules here, before the charge). If a camper moved onto or off the wait list, or
the cart no longer prices to the total the page showed, nothing is charged and the page shows
the cart as it is now → create the customer vault record an Obsidian-hosted gateway wants
(nothing is charged by it, so a failure here hands the cart back) → stamp the cart
(`PaymentSentDateTime`) and charge the gateway token once (`IGatewayComponent.Charge`) → save
**the payment by itself**: one `FinancialTransaction` in a batch named by the block's *Batch
Name Prefix*, with a detail line per paying line posting to the instance's account, linked from
the cart → in **one DB transaction**: one `Registration` + `RegistrationRegistrant`
(+ `RegistrationRegistrantFee` rows, registrant attribute answers and the signed document) per
line, each payment detail tied to its registration, and the cart marked **CheckedOut**. Each
detail line (and the registration's `DiscountAmount`) is net of only that line's code, in whole
cents, so the details add up exactly to the charge; the registration's `DiscountCode` is the
line's code. The cart row is the idempotency record: a second request for the same cart is told
the checkout is in progress or already done. The cart goes back to **Open** only when nothing
was charged: the gateway answered with an error, or checkout stopped before the gateway call.
A gateway answer with neither a payment nor an error (how some gateways report a timeout)
counts as unknown, not as a decline. If the registrations fail *after* a successful charge, the
payment is already in Rock (finance can find and refund it), the cart closes with
`LastCheckoutError` set, and the shopper is told not to pay again. If the gateway call fails
without an answer, or the request dies mid-checkout, the cart stays **Processing**. Both are
**unconfirmed checkouts** that staff reopen or close on the settings page (see Observations).

After the registrations are committed, each step runs on its own (a fresh context, its own
error handling), so one failing never skips the others or undoes the registrations or the
payment: the signed documents' PDFs and the completion email that carries them; the person's
form answers (person fields, family campus and address, person attributes, history); registrar
notes and history; the instance's linked group for registrants not on the wait list (the
registration's `GroupId` comes from the instance's first linkage with a group; the member gets
the template's role and status, then the group member answers); Rock's confirmation email and
staff notification; the template's registrant workflow; and each of the template and instance
registration workflows.

**Discount codes (ROCK-9394).** Codes work as they did with SECC's RegistrationEntry fork: they
are the registration template's own discounts (Rock › registration template › Discounts), and a
registration has **one** code. Each cart line is its own registration, so each camper has their
own code; a code never comes off another camper's line. A shared code (e.g. `STAFF50`) can
be typed on several campers, and each use counts toward its *Maximum Usage*. The family types the
code on the Offering page, per camper, under the template's *Discount Code Term* (the box shows
only when the template has discounts). The line's code is chosen as RegistrationEntry chose it
(`Services/TemplateDiscounts`, rules in `Domain/DiscountRules`):

1. the template's first *auto-apply* discount (by order) that can be used;
2. only without one, the Offering block's *Discount Code Workflow* (below), whose code **locks**
   the line's code (as the fork locked the box): the family can't swap it, and a code they typed
   is ignored with a notice;
3. otherwise the typed code, which replaces an auto-applied one.

A code is checked as Rock checks it, with Rock's messages: it must exist on the template (case
doesn't matter), be within its start/end dates, need no more than one registrant (*Minimum
Registrants*), and be under its *Maximum Usage*, counted per instance over Rock's registrations
plus the cart's other lines with the code plus the lines of other carts that are mid-checkout
(as seats and limited extras are counted, so two checkouts can't both take a code's last use).
*Maximum Registrants* 0 leaves the code on the
registration but takes nothing off. Amount and percentage are snapshotted when the code is
applied. Checkout checks each line's code again (registrations since, and the cart's earlier
lines) and asks the family to edit a line whose code can no longer be used. As in
RegistrationEntry, a percentage comes off the base price and the fees whose *Discount Applies*
is set, and a flat amount off the base price then those fees, so a fee that doesn't allow
discounts stays due. Wait-listed lines pay nothing now; their code stays on the registration,
with the template's terms, for Rock to apply when a seat opens.

**Financial assistance.** When the Offering block's *Discount Code Workflow* is set (prod: WF 725
"Automatic Financial Assistance"), it runs as each camper is added to the cart, the way SECC's
RegistrationEntry fork ran it: the entity is a dictionary with `RegistrationInstance` and
`RegistrationInfo` (the camper's form answers as registrant field values), and a code the
workflow applies (725 makes a single-use `FA-` template discount: the $50/$100 tier, or the
template cost less $20 for "More") becomes that line's locked code. It runs again only when the
option or answers change: on the edit page, changing them frees the code box, and on save a
workflow that applies no code (the family no longer asks for assistance) removes the FA code and
lets a typed code stand. A workflow that applies a code again (still asking, or asking again
after turning it off) brings back the line's first FA code (`CartLineItem.WorkflowDiscountCode`)
while it can still be used, set to the value the workflow just worked out (so a new "how much"
answer, e.g. $50 → $100, changes that code's amount on the template), and the unused code it
just made is deleted from the template, so each camper keeps one `FA-` code. A code made for a
line is also deleted from the template when the line is removed or its cart is abandoned
(by the job, or closed by staff), unless a registration or another live cart line uses it; only
codes created since the cart began are ever deleted, so a code staff set up is safe whatever
the workflow returns. Checkout records the code as the registration's discount code, so the
template's registration workflow (728) then launches the FA evaluation as before.

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
  Blocks/                               4 public + 1 admin RockBlockType classes (+ base classes)
  Domain/                               Pure pricing/eligibility engine (no Rock types): Enums,
                                        CheckoutRules (seats, fee limits, checkout outcomes),
                                        AnswerRules, DiscountRules (template discount codes),
                                        Entities (pricing model), Pricing/PricingEngine,
                                        Pricing/EligibilityService, Pricing/PricedCart
  Jobs/CartMaintenance                  Abandons idle carts, reopens checkouts that stopped
                                        before payment, retries signed-document PDFs, warns
                                        about unconfirmed checkouts
  Migrations/001–013                    Tables, entity/block types, pages, routes, blocks,
                                        settings, the job (see Migrations)
  Model/                                7 entities: 3 overlays, 4 cart tables
  Services/                             CatalogService, CartManager, CartPricing, CartSettings,
                                        CheckoutProcessor, CheckoutProblem, DiscountCodeWorkflow,
                                        FeeItemUsage, GradeBands, PendingCheckouts,
                                        RegistrationForms, RegistrationSignatures, SeatCounter,
                                        SeatLock, SeatPlanner, TemplateDiscounts
  ViewModels/                           Bags/boxes shared with the Obsidian blocks
  SystemGuid.cs                         Every GUID the plugin owns — never change
org.secc.RegistrationCart.Obsidian/     Vue 3 / Obsidian frontend
  src/blocks/*.obs                      One entry per block
  src/shared/*                          Partials (cart nav, pending-checkout notice, forms,
                                        order summary, tokenizer, ...)
  src/viewModels.d.ts                   Generated from the C# bags (npm run viewmodels)
org.secc.RegistrationCart.Tests/        xunit: pricing, eligibility, grade bands, checkout
                                        rules, answer rules, discount rules
plugin.json / BuildPlugin.ps1           Rock plugin packaging
```

## Components

### Blocks (Obsidian)

| Block | Page (created by migration) | What it does | Settings |
|-------|-----------------------------|--------------|----------|
| Registration Cart Catalog (`Catalog`) | Public site › Registration (`/registrations`), hidden from navigation | Lists templates turned on in the cart's settings that have an open instance. | Heading, Intro, Offering Detail Page, Cart Page |
| Registration Cart Offering (`OfferingDetail`) | `/registrations/offering/{OfferingKey}` | Pick a family member (or add a child, with their birthday and grade), an instance and fees, fill in the template's registration forms, and enter the camper's discount code; add to cart. With `?LineId=` it edits that cart line instead (see *Editing a line*). | Allow Adding Family Members, Maximum Family Members Added Per Day, Discount Code Workflow, Cart Page, Catalog Page |
| Registration Cart (`Cart`) | `/registrations/cart` | Cart with pricing (each line's discount code); edit or remove lines; shows what would stop a line checking out. | Checkout Page, Catalog Page, Offering Detail Page |
| Registration Cart Checkout (`Checkout`) | `/registrations/checkout` | Payment + checkout. Uses the gateway's Obsidian hosted control when it has one, else a My Well tokenizer iframe. Lines that need fixing show with an Edit link. | Financial Gateway, Batch Name Prefix, Tokenizer Script URL / Gateway URL / Public Key, Cart Page, Catalog Page, Offering Detail Page |
| Registration Cart Settings (`AdminCatalogSettings`) | Admin Tools › Installed Plugins › Registration Cart (`/admin/registration-cart`) | Per template/instance/fee item visibility, eligibility, image, order. When there are any, lists the **checkouts needing a payment check** with *reopen* and *close* (see *Unconfirmed checkouts*). Saves and those actions require block EDIT. | Catalog Page |

Every public page shows a warning while the signed-in person has an unconfirmed checkout (see
*Unconfirmed checkouts*).

### Job

**Registration Cart Maintenance** (`Jobs/CartMaintenance`, added by migration 007, every 15
minutes; Admin Tools › System Settings › Jobs Administration):

| Setting | Default | What |
|---------|---------|------|
| Abandon Open Carts After (days) | 30 | An open cart with no changes for this long becomes *Abandoned* (and the unused financial assistance codes made for it are removed); the family's next visit starts a new cart. 0 = off. |
| Retry Signed Document PDFs After (minutes) | 10 | A signed document from a cart checkout still without its PDF gets it made, and its completion email sent. |
| Stop Retrying Signed Document PDFs After (days) | 3 | A document whose PDF still can't be made after this long is left alone; the job's result counts them. 0 = retry forever. |
| Report Unconfirmed Checkouts After (minutes) | 30 | An unconfirmed checkout this old ends the job with a **warning** naming the cart id and the settings page (no names, amounts or error text, since the result is emailed). Set the job's *Notification Emails* (the migration can't know them; *Notification Status* is already Error, which also sends warnings). 0 = off. |
| Repeat The Unconfirmed Checkout Warning Every (hours) | 24 | Until staff reopen or close it, the job warns about a checkout again this often. Every run lists the open ones in its result either way. |
| Reopen Checkouts That Never Reached Payment After (minutes) | 15 | A checkout that stopped before its payment was sent to the gateway (the site restarted mid-checkout) charged nothing; the job reopens the cart. 0 = off. |

Each part of the job runs by itself, so one failing doesn't stop the others (the run then ends
as an exception, with what the other parts did in its message).

### Global attributes

| Attribute | Default | What |
|-----------|---------|------|
| Registration Cart: Show New Templates | False | Whether a template the cart's settings haven't been saved for shows in the catalog. |
| Registration Cart: Default Grade Bands | Elementary, Middle School, High School | Who an instance or extra with no saved setting can be registered for. |
| Registration Cart: Prevent Duplicate Registrations | True | Refuse a camper already registered for the option (wait list included). |

### Data model (`_org_secc_RegistrationCart_*`)

`TemplateSetting`, `InstanceSetting`, `FeeItemSetting` (overlays, cascade-deleted with the Rock
row), and the cart graph `Cart` → `CartRegistrant` / `CartLineItem` → `CartLineItemAddOn`. Line
items **snapshot** prices and their discount code's amount/percentage at add time (an add-on also
snapshots its fee's *Discount Applies*). The cart-wide `DiscountCode` / `CartDiscount` tables are
gone (migration 012). A `Cart` also records its checkout: `Status`, `FinancialTransactionId`
(the payment, saved as soon as the card is charged), `LastCheckoutError`, `PaymentSentDateTime`
(set just before the gateway is asked to charge), `ResolvedDateTime` / `ResolvedByPersonAliasId`
/ `ResolutionNote` (staff closed an unconfirmed checkout) and `PendingReportedDateTime` (the
job's last warning about it).

Rock can delete what carts point at (migration 006): deleting an instance deletes its cart
lines; deleting a registration or a transaction clears the cart's link to it; an add-on doesn't reference its
fee item in the database (it keeps its snapshot name and price, and checkout asks the family to
edit a line whose extra was deleted or turned off).

### Migrations

| # | What |
|---|------|
| 001 | Creates the 9 tables, registers entity + block types, creates the public pages under the stock External site's home page (if the site exists) and the admin pages under Installed Plugins, wires the blocks' linked-page settings. Page routes are set **global** (013 moves the public pages). |
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
| 012 | One template code per registration (ROCK-9394): adds `CartLineItem.IsTemplateDiscountLocked` and `WorkflowDiscountCode` (set on existing lines with a code, which all came from the workflow); removes the Discount Codes admin page, block type and entity types and drops the `DiscountCode` / `CartDiscount` tables. Down only drops the new columns. |
| 013 | Adds the cart's checkout columns `PaymentSentDateTime`, `ResolvedDateTime`, `ResolvedByPersonAliasId`, `ResolutionNote`, `PendingReportedDateTime`. Where the SECC public site exists (site Guid `74DF3DB5-…`, "Southeast Christian Church"), moves the four public pages under its home page on its full-width layout and makes their routes non-global, like that site's other routes; the stock External site at SECC is the retired 2014 one. A database without that site (a local Rock) keeps the pages where 001 put them. |

All thirteen were run in order on a clean copy of a local Rock 1.16.12 database on 2026-11-15
(see *Tests*).

## Configuration

1. **Pages and site:** migration 013 puts the public pages under the SECC public site's home
   page (hidden from navigation), so they get that site's theme and forced HTTPS. Move them
   anywhere on the site in Rock if they belong elsewhere; the blocks find each other by page.
   The routes are not global, so they answer on that site's host names.
2. **Checkout block:** pick the Financial Gateway. If it implements
   `IObsidianHostedGatewayComponent` its hosted control is used; otherwise fill in all three
   Tokenizer Fallback settings (script URL, gateway URL and public key — there are no sandbox
   defaults; card data never touches Rock).
3. **Registration instances:** an instance needs an **Account** when something is due for it
   (free events can do without). Price is the instance cost when the template sets cost on
   instance, else the template cost. Campus and event dates come from the instance's event
   linkage when present.
4. **Catalog settings:** Admin Tools › Installed Plugins › Registration Cart.
   Turn on each template that should be sold through the cart, and set who each instance and
   extra is for. Link the catalog page from the site's navigation when it's ready.
5. **Discount codes:** set them up on each Rock registration template's Discounts, as before.
   **Financial assistance:** set the Offering block's *Discount Code Workflow* to WF 725
   (`fd55a10c-e44a-4a02-b347-37cb9f67be66`), as block 4171 had it for the fork. Leave the
   templates' registration workflow (728) as it is.
6. **Maintenance job:** set its *Notification Emails* to whoever reconciles payments (see *Job*);
   without them a checkout needing a payment check shows only as the job's Warning status and
   on the settings page.
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
  checkout leaves the cart; what a gateway's answer means; unconfirmed checkouts and when one
  may be reopened; the shown-total check; registration window; seats counted as Rock counts
  them; fee item limits), `AnswerRulesTests` (when a stored answer
  is blank), `DiscountRulesTests` (template codes checked as Rock checks them: dates, Maximum
  Usage, Minimum/Maximum Registrants, matching, messages). `PricingEngineTests` covers one code
  per line: it never reaches another line, a shared code applies to each line on its own,
  *Discount Applies*, caps, wait list, and postings adding up to the charge over 500 random
  carts. No RockContext. The test project targets net472; on a Mac without Mono, compile the
  `Domain/` sources and these tests into a throwaway net8 xunit project to run them.
- **End to end (2026-11-15, PR #336 review fixes):** on a throwaway copy of a local Rock 1.16.12
  database with the plugin's objects removed first: migrations 001–013 from clean; then, through
  the real block actions with the Test Gateway: add a child (birthday and grade required, a name
  already in the family refused), add to cart with a signed waiver, edit a line with a locked
  birthday, checkout (payment, details tied to registrations, signed text stored as signed), a
  waiver edited after signing (re-sign required, nothing charged), a registration save forced to
  fail after the charge (payment still in Rock; staff close it), a cart stuck in Processing
  (the job reopens an unsent one, warns about a sent one without names, staff reopen and close),
  a code's last use held by another cart mid-checkout, a wait-listed line (no fees), an uploaded
  file (kept; a missing one stops checkout) and FA code cleanup. Not covered: a real My Well
  charge, and a gateway that answers with neither a payment nor an error (unit-tested rule only).

## Observations

*From the pre-port assessment (2026-10-13) and the PR reviews (2026-10-20, 2026-11-13).*

- **Unconfirmed checkouts:** a checkout that took, or may have taken, a payment without
  finishing. Either the cart is still **Processing** (the gateway call failed without an answer,
  `LastCheckoutError` starting "Payment outcome unknown", or the site stopped mid-checkout), or
  it is **CheckedOut with `LastCheckoutError`** (charged, but the registrations failed; its
  payment is in Rock as `FinancialTransactionId`, with details not yet tied to a registration).
  Until it is dealt with, the payer sees a warning on every page, the cart's lines keep their
  seats and limited extras (while Processing), its campers can't be added for that option
  again, and the maintenance job warns about it (and again every 24 hours).
  - **The job reopens by itself** a Processing cart whose `PaymentSentDateTime` is empty: the
    request died between the claim and the charge, so nothing can have been charged.
  - **Staff deal with the rest** on the settings page (Admin Tools › Installed Plugins ›
    Registration Cart › *Checkouts needing a payment check*), after looking the payment up at
    the gateway. **Reopen** (nothing was charged): the cart is Open again and the family can
    pay; only for a Processing cart with no payment recorded. **Close** (it was charged, and has
    been refunded or the campers registered by hand; the answers are on the cart lines): a
    Processing cart becomes Abandoned, a charged one stays CheckedOut, and who, when and the
    required note are kept on the cart. Neither is allowed in a checkout's first 5 minutes,
    while its request may still be running. No SQL is needed.
  - A reopened cart that the family has since replaced with a new one (with campers in it) is
    abandoned instead, so their cart doesn't change under them.
- **What the gateway's answer means:** an error message is a decline (nothing charged, the cart
  reopens); a payment with no error is a charge; neither is unknown (the cart stays Processing).
  Rock's NMI gateway reports a timeout or unreadable response that last way. My Well's plugin
  source isn't in this repo, so how it reports a timeout hasn't been checked; if it sends an
  error message for one, that checkout is treated as a decline, as in Rock's own registration.
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
  detail and, as in Rock's own registration, **no fees**: extras chosen for a wait-listed line
  aren't written and don't use up a limited extra; the camper chooses and pays for them when
  staff move them off the list in Rock, which bills the balance. The confirmation email
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
  PersonMatch (registrants are always existing family members). **Uploaded files** (File and
  Image questions) are made permanent when the line is saved, because Rock only does that when
  the answer is written at checkout and Rock Cleanup deletes temporary files after a day;
  checkout stops a line whose file no longer exists. A file on a line that is later removed or
  abandoned stays in Rock (it isn't deleted, since it may be the person's own existing file).
  Stored answers use the browser's shape (camelCase), including a locked field's value taken
  from the person, so the edit page can show a locked birthday or address.
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
  signature made for other answers. **The stored document is the HTML the parent signed**, with
  their signed date. Rock's own block renders the Lava again when it saves, moments after the
  signature; a cart line can wait days, and a fresh render would put text the parent never saw
  (today's date, changed person details) over their signature. The signed HTML is safe to keep:
  it was only accepted when it matched the token issued for the server's own render, and it
  stays encrypted on the cart line. A signature is for one version of the document: if staff
  edit the document's Lava after it was signed, the line must be signed again before it can be
  paid for. At checkout the document is checked before the charge, saved with the registration
  (so every paid registration has its document), and its PDF made right after commit; the
  maintenance job retries a PDF that failed (for 3 days) and sends the completion email then.
- **Deleting in Rock:** templates, instances, fees, registrations and transactions can be deleted
  in Rock as usual (see *Data model* for what happens to cart rows).
- **Financial assistance:** 725 runs on every add (it's a persisted workflow type, so each add
  leaves a Workflow row). The `FA-` code made for a line is removed from the template when the
  line is removed or the cart is abandoned, so it can't be typed on a later registration.
  Removing and re-adding a camper makes a new code. A line with an FA code takes no other code
  (one code per registration). 725 has no branch for the 1st–2nd "$25" tier, so it gets the
  cost-less-$20 amount, as it does on prod today.
- **Add child:** asks for first and last name, birthday and the child's actual grade (Rock's
  School Grades, or "not in school yet"), and creates a Pending person with the registration
  template's connection status, else Prospect (as Rock's registration does). A name the family
  already has is refused, so the same child can't be added twice and then registered twice;
  and a family can gain at most *Maximum Family Members Added Per Day* new people (block
  setting, default 5). It does not look for the child outside the family: adding a matched
  person to a family would let any account attach itself to someone else's child, so a child
  who already has a record elsewhere gets a second, Pending one for staff to merge.
- **The same camper twice:** adding refuses a camper already in the cart for the option, and
  checkout refuses a cart that somehow holds one twice (two adds at the same moment).
- **Page placement:** see *Configuration* and migration 013. The admin page stays under
  Installed Plugins with a global route (`/admin/registration-cart`).
- **Discount code rules (decided for ROCK-9394):** codes stay on the registration templates; one
  code per registration (per camper line), never shared across lines; a shared code may be typed
  on several campers; an FA code can't be swapped. Rock counts *Maximum Usage* per instance, so a
  code meant to be used once across a camp's campuses has to be set up that way per template, as
  before. Codes held in open carts aren't reserved: whoever checks out first uses a single-use
  code, and the other cart is asked to edit the line.
- **Business rules to confirm:** sign-in required (no guest checkout).

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

Last updated: 2026-11-15
