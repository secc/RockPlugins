# org.secc.Communication

> SMS/keyword administration, communication-list tooling, schedule-reminder jobs, and a Twilio message-history sync for Southeast's messaging.

## Overview

This plugin bundles Southeast's messaging extensions: admin blocks for managing Twilio-backed
phone numbers and their keywords (via an external SECC Messaging API), self-service and staff
communication-list tooling, a job that builds reminder communications ahead of a schedule, and a
job/block that pulls Twilio SMS delivery history into a local `TwilioHistory` table for reporting.
It talks to a SECC-hosted Messaging API (REST) for phone/keyword data and directly to Twilio for
message history.

## Project Info

- **Project file:** `org.secc.Communication.csproj`
- **Root namespace:** `org.secc.Communication`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup)
- **Cross-plugin dependency:** `org.secc.DevLib` (`SettingsComponent` infrastructure)

## Project Layout

```
/Components/        MessagingServiceSettings — DevLib SettingsComponent (API url/key, SMS defined type, approver roles)
/Data/              CommunicationDataContext + generic CommunicationDataService<T>
/Messaging/         MessagingClient DTOs (Keyword, MessagingPhoneNumber, MessagingPerson, ...) + reorder item
/Model/             TwilioHistory entity + TwilioHistoryService (maps Twilio MessageResource -> entity)
/Jobs/              SendScheduleReminder, SyncTwilioHistory (SycnTwilioHistory) Quartz jobs
/Twilio/            TwilioDownloader — pulls Twilio MessageResource records via the SDK
/Migrations/        Rock plugin migrations (TwilioHistory table + indexes)
/org_secc/Communication/   UI blocks (.ascx + .ascx.cs)
MessagingClient.cs  REST client for the external SECC Messaging API
SECCMessagingSettings.cs   Plain settings POCO returned by the settings component
```

## Components

### Blocks

Category in Rock: **SECC > Communication**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Communication List Wizard | Build a communication targeted at a public communication-list group. | (none) |
| Manage Communication Lists | Let users manage their communication-list subscriptions; can confirm via SMS keyword. | `AttributeKey` (Type), `KeywordKey` (Keyword), `FromSMSNumber` (confirmation SMS from) |
| Messaging Phone Numbers | List active phone numbers from the SECC Messaging API. | `DetailPage` (linked page) |
| Messaging Phone Number Detail | View/edit a phone number and its keywords via the Messaging API. | (none) |
| Messaging Phone Number Keywords | List/manage keywords for a phone number, with an approval flow. | `ShowFilter` (bool, default true), `EnforceResponseLimit` (bool, default true) |
| Sync Twilio History | Manually trigger a Twilio history sync for a date range. | (none) |

The Messaging blocks call `MessagingClient`, which reads its base URL and function key from the
**SECC Messaging Settings** component.

### Jobs

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `SendScheduleReminder` (DisplayName **SECC \| Send Schedule Reminders**) | A configured number of days before a schedule's occurrence, create Approved Email and/or SMS communications from a `SystemCommunication` template to everyone in a dataview. | `Schedule`, `CommunicationTemplate`, `DistributionDataview` (Person dataview), `DaysBeforeToSend` (default 1), `SendTime` (default `08:00 am`), `CommunicationMethod` (Email/SMS check-list; empty = all) |
| `SycnTwilioHistory` (no `[DisplayName]` — registered under the class name) | Loop back `DaysBack` days (default 2) and upsert each day's Twilio messages into `TwilioHistory`. | `DaysBack` (default 2) |

### Settings Component

`MessagingServiceSettings` (exported as a DevLib `SettingsComponent`, display name **SECC Messaging
Settings**) holds the integration config consumed by `MessagingClient`.

| Setting | Type | Notes |
|---------|------|-------|
| **MessagingApiUrl** | url (required) | Base URL of the SECC Messaging API. |
| **MessagingApiKey** | encrypted text (required) | Function key; decrypted before use. |
| **SMSDefinedType** | defined type | "Twilio SMS Phone Numbers" defined type (default Guid = `COMMUNICATION_SMS_FROM`). |
| **KeywordApproverRoles** | group list | Security roles allowed to approve keyword updates. |

### Migrations-driven data

`TwilioHistory` (`_org_secc_Communication_TwilioHistory`) stores synced Twilio messages: `SID`,
`Body`, `DateCreated`/`DateSent`, `Direction`, `To`/`From`, `Status`, `Price`, `NumberOfSegments`.
`Direction` and `Status` are persisted as a local enum mapping (see `TwilioHistoryService`) so the
Twilio SDK enums aren't needed to read the data.

## Dependencies & Integrations

- **Rock:** `RockContext`, Rock job framework (Quartz `IJob`), `CommunicationService` /
  `SystemCommunicationService`, `ScheduleService`, `DataViewService`, the block/UI framework, and
  the Twilio `TransportComponent` (read for the account SID/token).
- **Third-party:** Twilio .NET SDK (`MessageResource.Read`), RestSharp (Messaging API calls),
  Newtonsoft.Json.
- **External service:** SECC Messaging API (REST) for phone-number and keyword CRUD/reorder.
- **Cross-plugin:** [org.secc.DevLib](../org.secc.DevLib/README.md) (`SettingsComponent`).

## Migrations

- `001_Init` — creates `_org_secc_Communication_TwilioHistory` (PK, person-alias FKs, Guid index).
- `002_Index` — adds non-unique indexes on `SID` and `DateCreated`.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (resolved, ROCK-8675):** `MessagingClient` now sends the decrypted Messaging API
  function key via the `x-functions-key` request header instead of a `?code={MessagingKey}`
  query-string parameter, so the secret no longer travels in request URLs (proxy/server logs,
  Function access logs, App Insights). Still worth confirming the configured `MessagingUrl` is
  HTTPS.
- **Improvement:** The job class is misspelled `SycnTwilioHistory` (typo for "Sync") — renaming the
  class would break the existing scheduled-job registration in the database, so review before
  changing.
- **Improvement:** `SendScheduleReminder.Execute` opens one `RockContext` and then
  `CreateCommunications` opens a second, and `TwilioDownloader.SyncItems` opens a fresh
  `RockContext` per message inside its loop (`MessagingClient` likewise news up a `RestClient` per
  call). Fine for jobs/one-offs but wasteful at scale.

## Making Changes

- To change phone/keyword admin behavior, edit the matching `*.ascx.cs` under
  `org_secc/Communication/`; remote calls go through `MessagingClient.cs`. To change which API it
  hits, update the **SECC Messaging Settings** component, not code.
- Reminder-communication logic lives in `Jobs/SendScheduleReminder.cs`; Twilio history sync lives
  in `Jobs/SyncTwilioHistory.cs` + `Twilio/TwilioDownloader.cs`.
- New columns on `TwilioHistory` need a matching property in `Model/TwilioHistory.cs` and a new
  numbered migration under `/Migrations/` (don't hand-edit migrations that have already run).
- Related: [org.secc.Workflow](../org.secc.Workflow/README.md) (the `SMS Send` workflow action and
  Twilio Lookup action).
