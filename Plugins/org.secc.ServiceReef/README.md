# org.secc.ServiceReef

> Scheduled jobs that pull mission-trip events, participants, and payments from the ServiceReef API into Rock as groups, people, and financial transactions.

## Overview

ServiceReef integrates Southeast's [ServiceReef](https://www.servicereef.com/) mission-trip
platform with Rock. It ships two Quartz scheduled jobs: `ImportTrips` builds a year/trip group
hierarchy and enrolls participants as group members (matching or creating the Rock person), and
`ImportData` imports trip payments as `FinancialTransaction`s under per-trip financial sub-accounts.
Both call the ServiceReef REST API using a custom HMAC authenticator. There is no UI — the jobs are
configured entirely through Rock job attributes.

## Project Info

- **Project file:** `org.secc.ServiceReef.csproj`
- **Root namespace:** `org.secc.ServiceReef`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `PayPal*` SDK DLLs, via the PostBuildEvent `xcopy`)
- **Cross-plugin dependencies:** [org.secc.PayPalReporting](../org.secc.PayPalReporting/README.md),
  [org.secc.PersonMatch](../org.secc.PersonMatch/README.md)

## Project Layout

```
/ (root)        ImportTrips, ImportData (the two IJobs) + HMACAuthenticator
/Contracts/     POCOs deserialized from the ServiceReef API (Events, Event, Participants,
                Member, Payments, Address, PageInfo)
/Migrations/    Rock plugin migrations (ServiceReef financial source + account-type defined values)
```

## Components

### Jobs

Both are Quartz `IJob`s decorated with `[DisallowConcurrentExecution]`; configuration is per-job via
Rock job attributes (read from the `JobDataMap`). Each pages the ServiceReef API 100 records at a
time and throws (failing the job) on a non-200 response or accumulated warnings.

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `ImportTrips` | Pull ServiceReef events into a Parent Group → "{Year} Mission Trips" → trip group hierarchy, set trip attributes from event categories, and add each participant as a `GroupMember`. | Service Reef API Key / Secret / URL, Parent Group, Year Group Type, Trip Group Type, Default Connection Status, ServiceReef UserId / Profile URL person attributes, Date Range |
| `ImportData` | Pull ServiceReef payments into `FinancialTransaction`s under per-event financial sub-accounts of a parent Account. | PayPal API Username / Password / Signature, Service Reef API Key / Secret / URL, Date Range, Account, Financial Gateway, Transaction Source, Connection Status, ServiceReef Account Type |

#### `ImportTrips` job attributes

| Setting | Type | Notes |
|---------|------|-------|
| **Service Reef API Key** / **Secret** | EncryptedText | HMAC credentials; decrypted at runtime. |
| **Service Reef API URL** | Text | Base API URL (trailing `/` enforced). |
| **Parent Group** | Group | Root group; year groups created as children, trips as grandchildren. |
| **Year Group Type** / **Trip Group Type** | GroupType | Group types for the two created levels. |
| **Default Connection Status** | DefinedValue (Person Connection Status) | Applied to newly created people. |
| **ServiceReef UserId** / **ServiceReef Profile URL** | Attribute (Person) | Person attributes populated with the ServiceReef user id / profile URL; UserId attr is also used as a match key. |
| **Date Range** | SlidingDateRange | Events to import (default `Previous 2 Day`). |

#### `ImportData` job attributes

| Setting | Type | Notes |
|---------|------|-------|
| **PayPal API Username** / **Password** / **Signature** | EncryptedText | PayPal API credentials (declared; transaction tender detail is read from the PayPalReporting table). |
| **Service Reef API Key** / **Secret** / **URL** | EncryptedText / Text | ServiceReef HMAC credentials and base URL. |
| **Account** | Account | Parent financial account; one sub-account is created per ServiceReef event. |
| **Financial Gateway** | FinancialGateway | Gateway stamped on imported transactions. |
| **Transaction Source** | DefinedValue (Financial Source Type) | Defaults to the `Service Reef` source value (migration 001). |
| **Connection Status** | DefinedValue (Person Connection Status) | Applied to newly created people. |
| **ServiceReef Account Type** | DefinedValue (Financial Account Type) | Account type for created sub-accounts; defaults to the `Service Reef` value (migration 002). |
| **Date Range** | SlidingDateRange | Payments to import (default `Previous 2 Day`). |

### Person matching

Both jobs resolve a ServiceReef participant/payer to a Rock `Person` in tiered fashion: (1) look up
the ServiceReef member's `ArenaId` against `PersonAlias.AliasPersonId`; (2) (`ImportTrips` only)
match on the **ServiceReef UserId** person attribute; (3) fall back to `PersonService.GetByMatch`
(via [org.secc.PersonMatch](../org.secc.PersonMatch/README.md)); (4) create a new person + home
location if no match. When multiple matches are found, Member records are preferred over Attendee,
then the lowest `Id`.

## Dependencies & Integrations

- **Rock:** Quartz job framework (`IJob`, `JobDataMap`), `RockContext` and Rock services
  (Financial*, Person*, Group*, Attribute*, DefinedValue/Type, Location), attribute framework,
  `Encryption`, plugin migrations (`Rock.Plugin`).
- **Cross-plugin:** [org.secc.PayPalReporting](../org.secc.PayPalReporting/README.md) —
  `ImportData` reads tender type from its `_org_secc_PaypalReporting_Transaction` table via
  `TransactionService`; [org.secc.PersonMatch](../org.secc.PersonMatch/README.md) — `GetByMatch`.
- **Third-party:** ServiceReef REST API (`v1/events`, `v1/events/{id}`, `v1/events/{id}/participants`,
  `v1/members/{id}`, `v1/payments`) over `RestSharp`, authenticated with a custom HMAC-SHA256
  `IAuthenticator` (`HMACAuthenticator`, `amx` scheme). PayPal Core/Merchant SDKs are referenced;
  Newtonsoft.Json (transitive).

## Migrations

Ships Rock plugin migrations under `/Migrations/`:

- `001_CreateAttributes` — adds the `Service Reef` **Financial Source Type** defined value
  (`MigrationNumber 1`).
- `002_CreateAccountType` — adds the `Service Reef` **Financial Account Type** defined value
  (`MigrationNumber 2`).

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** Both migration files declare `namespace org.secc.PayPalReporting.Migrations`
  rather than `org.secc.ServiceReef.Migrations` — almost certainly a copy/paste artifact from the
  PayPalReporting plugin. Harmless at runtime but misleading; worth correcting.
- **Improvement:** `ImportData` mis-assigns the tender type when the result `Type` is not
  `"CreditCard"`: it sets `tran.TransactionTypeValueId` (transaction *type*) to a currency-type id
  rather than `CurrencyTypeValueId`. The later code re-sets `TransactionTypeValueId` to the
  contribution type, so the earlier write is effectively dead/wrong — worth confirming non-card
  payments record the intended currency type.
- **Improvement:** In `ImportTrips`, the no-email matching branch (`ImportTrips.cs:300`) calls
  `matches.Remove( person )` inside `foreach ( Person match in matches )`, but `person` is still
  `null` there — it almost certainly meant `matches.Remove( match )`. As written, `Remove( null )`
  removes nothing, so non-address-matching candidates are never pruned and disambiguation falls
  through to the member/attendee/lowest-`Id` tiebreak on the unfiltered list. Review that
  address-disambiguation path.
- **Improvement:** New-person creation dereferences `result.Address.*` (e.g. `ImportData` line ~306,
  `ImportTrips` line ~395) without the null guard used a few lines earlier, so a participant/payer
  with a null Address will throw a `NullReferenceException` for the create path.
- **Security (low):** API keys/secrets and PayPal credentials are stored as `EncryptedTextField`
  job attributes and decrypted at runtime — appropriate. Confirm the ServiceReef API base URL and
  these job attributes are only visible to Finance/admin roles in the Jobs admin UI.

## Making Changes

- To change import behavior, edit `ImportTrips.cs` (groups/participants) or `ImportData.cs`
  (payments/transactions); both read all config from `context.JobDetail.JobDataMap`, so keep the
  attribute `Key` and the `dataMap.GetString(...)` lookups in sync.
- The ServiceReef request signing lives in `HMACAuthenticator.cs`; API response shapes are the POCOs
  under `/Contracts/` (add fields there to capture more of the ServiceReef payload).
- New supporting defined values belong in a new numbered migration under `/Migrations/` — don't
  hand-edit migrations that have already run.
- Related: person matching is shared with [org.secc.PersonMatch](../org.secc.PersonMatch/README.md);
  transaction tender lookups come from [org.secc.PayPalReporting](../org.secc.PayPalReporting/README.md).
