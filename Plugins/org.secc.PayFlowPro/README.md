# org.secc.PayFlowPro

> A single custom Rock **financial gateway component** that subclasses Rock's stock PayFlow Pro gateway to add an "Every 4 Weeks" giving frequency, friendlier handling of invalid saved accounts, and a custom recurring-billing reconciliation report.

> **Doc tier: deep.** This plugin moves money — it processes charges and reconciles recurring giving against PayPal's PayFlow Pro reporting API — so it's documented at the deeper technical tier (gateway contract, payment-download flow, config attributes, edge cases). The plugin itself is small (one class + one migration); see the tier note in *Observations*.

## Overview

PayFlowPro is Southeast's customized PayPal/PayFlow Pro payment gateway for Rock giving. It does **not** reimplement a gateway from scratch — it inherits from Rock's built-in `Rock.PayFlowPro.Gateway` and overrides only the few behaviors SECC needs to change: it advertises a reduced set of supported recurring schedules (plus a custom **Every 4 Weeks** / `FRWK` period), on the PayFlow "Original transaction ID not found" charge error, deletes the now-invalid saved account and (when one was found) swaps in a user-readable message, and replaces the payment-download logic with a multi-report query against PayFlow Pro's reporting API to reconcile recurring transactions. It is discovered by Rock via MEF as a `GatewayComponent` and configured through Rock financial-gateway attributes — no code change is needed to wire it to a gateway.

## Project Info

- **Project file:** `org.secc.PayFlowPro.csproj`
- **Root namespace:** `org.secc.PayFlowPro`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly only; `PostBuildEvent` xcopies `org.secc.PayFlowPro.dll`)
- **Key references (project refs):** `Rock`, `Rock.PayFlowPro` (the stock base gateway + `Reporting.Api`), `Rock.Common`, `Rock.Lava.Shared`, `DotLiquid`. **Binary refs:** `Payflow_dotNET.dll` (PayPal SDK, from `libs/PayFlow`) and EntityFramework 6 (6.1.3).

## Project Layout

```
/                       Gateway.cs — the GatewayComponent subclass (the entire plugin)
/Migrations/            001_Create4WeekFrequency.cs — adds the "Every 4 Weeks" financial-frequency defined value
/Properties/            AssemblyInfo.cs
app.config / packages.config   EF6 config + NuGet manifest
Packages.dgml                   NuGet dependency graph (tooling artifact)
```

## How the Gateway Works

`Gateway` is exported via MEF (`[Export(typeof(GatewayComponent))]`, `ComponentName` = "SECC PayFlowPro") and subclasses `Rock.PayFlowPro.Gateway`. Rock discovers it at startup, renders its attributes on the financial-gateway admin screen, and calls the overridden methods through the normal giving pipeline. Only the methods below are overridden; everything else (tokenization, scheduling, profile creation) falls through to the base gateway.

```mermaid
flowchart TD
    A[Rock financial pipeline] --> B{which operation?}
    B -->|one-time / reference charge| C["Charge(gateway, paymentInfo, out err)"]
    C --> C1["base.Charge(...)"]
    C1 --> C2{ReferencePaymentInfo<br/>AND err == '[19] Original<br/>transaction ID not found'?}
    C2 -->|yes, AND a matching<br/>FinancialPersonSavedAccount exists| C3[Delete the saved account<br/>+ replace err with friendly text]
    C2 -->|no| C4[return transaction]
    B -->|recurring schedule setup| D["SetPayPeriod(recurringInfo, freq)"]
    D --> D1[Map frequency Guid -> PayFlow<br/>PayPeriod code WEEK/BIWK/MONT/FRWK/...]
    B -->|download payments job| E["GetPayments(gateway, start, end, out err)"]
    E --> E1[Force TLS 1.0/1.1/1.2]
    E1 --> E2[RecurringBillingReport<br/>via Rock.PayFlowPro.Reporting.Api]
    E2 --> E3[CustomReport for amount/<br/>tender/email by transaction id]
    E3 --> E4{txn found<br/>in custom report?}
    E4 -->|no| E5[TransactionIDSearch fallback<br/>per transaction]
    E4 -->|yes| E6[Build Payment]
    E5 --> E6
    E6 --> E7[Return List&lt;Payment&gt;]
```

**Conventions / contracts:**
- The class implements the Rock `GatewayComponent` contract by inheriting `Rock.PayFlowPro.Gateway`; SECC only overrides `SupportedPaymentSchedules`, `Charge`, and `GetPayments`, plus a private `SetPayPeriod` helper.
- Credentials/mode are read at runtime from the `FinancialGateway` via `GetAttributeValue(financialGateway, key)` (keys `User`, `Vendor`, `Partner`, `Password`, `Mode`).
- `GetPayments` returns `null` (with `errorMessage` set) on a hard failure and a populated `List<Payment>` on success — the Rock download-payments job treats a `null` return as an error.
- The custom **Every 4 Weeks** frequency is identified by the constant `TRANSACTION_FREQUENCY_FOUR_WEEKS` (`B603E480-42C3-41D9-923B-17779C5909A8`), seeded as a defined value by the migration and mapped to PayFlow's `FRWK` pay period.

## Components

### Gateway Component

`SECC PayFlowPro` — appears in the **Component** dropdown when configuring a Financial Gateway in Rock. Configuration attribute keys (in **bold**) are read by the base and overridden methods:

| Setting (label) | Key | Type | Notes |
|-----------------|-----|------|-------|
| PayPal Partner | **Partner** | text (required) | PayFlow partner id. |
| PayPal Merchant Login | **Vendor** | text (required) | PayFlow merchant/vendor login. |
| PayPal User | **User** | text (optional) | PayFlow API user; passed to the reporting API. |
| PayPal Password | **Password** | text (required, password-masked) | PayFlow API password. |
| Mode | *(label "Mode")* | radio `Live,Test` (default `Live`) | `Test` routes the reporting API to the PayFlow test host (case-insensitive compare). |

### Overridden Behavior

| Override | What it changes vs. the base gateway |
|----------|--------------------------------------|
| `SupportedPaymentSchedules` | Advertises only One-Time, Weekly, Bi-Weekly, Monthly (Twice-Monthly is commented out). The base set is replaced. |
| `SetPayPeriod` (private) | Maps each Rock frequency Guid to a PayFlow `PayPeriod` code, including the SECC-specific `FRWK` (Every 4 Weeks); One-Time becomes a 1-term yearly profile. |
| `Charge` | After `base.Charge`, if a `ReferencePaymentInfo` charge fails with `"[19] Original transaction ID not found"` **and** a matching `FinancialPersonSavedAccount` is found (by `TransactionCode` + gateway id), deletes that saved account and replaces the error with a user-readable message. If no matching saved account is found, the original `[19]` error is left untouched. |
| `GetPayments` | Replaces payment download: forces TLS, then queries `RecurringBillingReport` + `CustomReport` (+ a `TransactionIDSearch` fallback) via `Rock.PayFlowPro.Reporting.Api` to build `Payment` rows with amount, tender, schedule, and billing email (as a `CCEmail` attribute). |

## Dependencies & Integrations

- **Rock:** `GatewayComponent` / `Rock.PayFlowPro.Gateway` (base), `RockContext`, `FinancialPersonSavedAccountService`, `FinancialTransaction` / `Payment` / `PaymentInfo` / `ReferencePaymentInfo`, `DefinedValueCache` / `DefinedTypeCache`, financial system Guids, plugin migrations.
- **Third-party:** PayPal **PayFlow Pro** (`Payflow_dotNET.dll` SDK; `PayPal.Payments.DataObjects.RecurringInfo`) and its **reporting API** (via `Rock.PayFlowPro.Reporting.Api`). EntityFramework 6.
- **Other project refs:** `Rock.Common`, `Rock.Lava.Shared`, `DotLiquid` (transitive build dependencies of Rock).
- **Cross-plugin:** none.

## Migrations

Ships one Rock plugin migration under `/Migrations/`:

- `001_Create4WeekFrequency` (`MigrationNumber(1)`) — adds an **"Every 4 Weeks"** defined value to the Financial Frequency defined type, using the `TRANSACTION_FREQUENCY_FOUR_WEEKS` Guid. `Down()` deletes it. (Note: the file's namespace/class are `org.secc.PayFloPro.Migrations.CreateCurrencyType` — name typos that don't affect behavior.)

## Edge Cases & Constraints

- **`Mode` only switches the *reporting* host.** In `GetPayments`, `Mode == "Test"` selects the PayFlow test reporting endpoint. The base gateway is responsible for honoring `Mode` on the charge path — confirm both sides agree before testing against production credentials.
- **TLS is forced inside `GetPayments`.** It sets `ServicePointManager.SecurityProtocol` to `Tls | Tls11 | Tls12` for the *whole process*, which re-enables TLS 1.0/1.1 globally (see Observations).
- **`Charge` deletes saved accounts on a specific error string.** The cleanup only triggers on the exact message `"[19] Original transaction ID not found"`; if PayFlow changes that string the saved account won't be removed and the cryptic error surfaces to the donor. Also note the friendly message is only substituted **inside** the "saved account found" branch — if the `[19]` error fires but no matching saved account row exists, the donor still sees the raw `[19]` text.
- **`GetPayments` short-circuits on the first un-resolvable transaction.** If even one recurring-billing row can't be matched by `CustomReport` or `TransactionIDSearch`, the method sets `errorMessage` and returns `null` — discarding any payments already collected for that run.
- **Amounts are divided by 100.** Report amounts are treated as cents (`amount / 100`); a report that returns dollars would be off by 100×.
- **`SetPayPeriod` maps more frequencies than `SupportedPaymentSchedules` advertises.** The switch also handles Twice-Monthly (`SMMO`, commented out of the schedule list), Quarterly (`QTER`), Twice-Yearly (`SMYR`), and Yearly (`YEAR`) — so a schedule created through another path with one of those frequencies would still map to a valid PayFlow pay period even though donors can't select it in this gateway's UI. One-Time maps to a 1-term `YEAR` profile.

## Observations

*Noticed while documenting — not a full audit; this plugin handles payments, so flagged for confirmation.*

- **Tier mismatch (process):** This is effectively a **standard-tier** plugin by size — one gateway class and one defined-value migration, no blocks/REST/jobs/Lava. It was assigned the deep tier (and written as such) on the reasonable grounds that it processes money and brokers PayPal credentials, but a human may prefer to downgrade it to standard to match siblings like [org.secc.QRManager](../org.secc.QRManager/README.md).
- **Security (review):** `GetPayments` sets `ServicePointManager.SecurityProtocol = Tls | Tls11 | Tls12`, which **re-enables TLS 1.0 and 1.1** process-wide (not just for this call) and clobbers any stricter policy Rock or the host set. Worth confirming PayPal still requires anything below TLS 1.2; if not, narrow this to `Tls12` (or `Tls12 | Tls13`).
- **Security (low):** The PayFlow `Password` attribute is declared password-masked (`isPassword: true`), but `User`/`Vendor`/`Partner` are plain text fields and the billing email is copied into a `CCEmail` payment attribute. Standard for a gateway, but confirm gateway-admin access is restricted to trusted finance staff.
- **Improvement:** `GetPayments` is a long single method that builds a ~50-key `customParams` dictionary inline and does per-transaction fallback API calls in a loop (N extra round-trips when `CustomReport` misses rows). For large date ranges this is chatty; the parameter block could be extracted and the fallback batched.
- **Improvement:** The migration's namespace (`org.secc.PayFloPro`) and class name (`CreateCurrencyType`) don't match what it does or where it lives. Harmless, but confusing for future maintainers grepping for the frequency seed.

## Extending

There is no plugin-specific extension surface beyond the gateway itself — to change giving behavior, override more of the base `Rock.PayFlowPro.Gateway` contract in `Gateway.cs`. To add another custom frequency, follow the existing pattern: define the Guid constant, seed it in a new numbered migration, and map it in `SetPayPeriod`.

```csharp
// Gateway.cs
public const string TRANSACTION_FREQUENCY_MY_PERIOD = "<new-guid>";

private void SetPayPeriod( RecurringInfo recurringInfo, DefinedValueCache transactionFrequencyValue )
{
    // ... existing cases ...
    case TRANSACTION_FREQUENCY_MY_PERIOD:
        recurringInfo.PayPeriod = "MYCODE"; // a valid PayFlow PayPeriod code
        break;
}
```

```csharp
// Migrations/002_CreateMyFrequency.cs
[MigrationNumber( 2, "1.0.0" )]
class CreateMyFrequency : Rock.Plugin.Migration
{
    public override void Up() =>
        RockMigrationHelper.AddDefinedValue(
            Rock.SystemGuid.DefinedType.FINANCIAL_FREQUENCY,
            "My Period", "My Period", Gateway.TRANSACTION_FREQUENCY_MY_PERIOD );

    public override void Down() =>
        RockMigrationHelper.DeleteDefinedValue( Gateway.TRANSACTION_FREQUENCY_MY_PERIOD );
}
```

MEF discovers the gateway automatically; new migrations run in number order on app start.

## Making Changes

- All gateway behavior lives in `Gateway.cs`. Edit `SupportedPaymentSchedules` to change which schedules donors can pick, `SetPayPeriod` to change frequency→PayFlow mappings, and `Charge`/`GetPayments` for charge error handling and payment reconciliation.
- New giving frequencies require both a `SetPayPeriod` case and a numbered migration under `/Migrations/` (don't edit a migration that has already run).
- Reporting-API behavior (report names, host selection) comes from the shared `Rock.PayFlowPro` project's `Reporting.Api` — changes there affect this gateway too.
- Credentials and Live/Test mode are admin data on the Financial Gateway record, not code.
