# org.secc.Microframe

> Drives physical Microframe LED paging signs over TCP — pushing child-pickup codes from Rock so they display (and clear) on the boards.

## Overview

Microframe lets check-in staff manage the codes shown on Southeast's physical Microframe LED
"pager" boards. Codes are grouped into **Sign Categories**; each **Sign** (a network-addressable
board with an IP/port/PIN) subscribes to one or more categories. When a code is added or removed —
either through the Sign Code Manager block or the scheduled job — the plugin opens a TCP socket to
each affected sign and synchronizes its displayed tags. Two custom entities (`Sign`, `SignCategory`)
and a set of admin blocks/pages make up the UI.

## Project Info

- **Project file:** `org.secc.Microframe.csproj`
- **Root namespace:** `org.secc.Microframe`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup)

## Project Layout

```
/Model/        Sign, SignCategory entities + their services
/Data/         MicroframeContext / MicroframeService (EF DbContext)
/Utilities/    SignUtilities (sync orchestration) + MicroframeConnection (async TCP client)
/Jobs/         UpdateSigns — Quartz job that re-syncs all signs
/Migrations/   Rock plugin migrations (tables, pages/blocks, indexes)
/org_secc/Microframe/   UI blocks (.ascx + .ascx.cs)
```

## Components

### Models

| Entity | Table | Notes |
|--------|-------|-------|
| `Sign` | `_org_secc_Microframe_Sign` | A physical board: `Name`, `Description`, `IPAddress`, `Port`, 4-char `PIN`. Many-to-many to `SignCategory` via `_org_secc_Microframe_SignSignCategory`. |
| `SignCategory` | `_org_secc_Microframe_SignCategory` | A named group of codes. `Codes` is a single comma-delimited string column. |

Both implement `Rock.Data.Model<T>`, `ISecured`, and `IRockEntity`.

### Blocks

Category in Rock: **SECC > Microframe**.

| Block | Purpose | Key settings |
|-------|---------|--------------|
| Sign List | Lists all signs; links to detail. | **DetailPage** |
| Sign Detail | Edit a sign (name, IP, port, PIN) and its category memberships. | — |
| Sign Category List | Lists all sign categories; links to detail. | **DetailPage** |
| Sign Category Detail | Edit a category (name, description). | — |
| Sign Code Manager | Add/remove codes within a category; pushes the change to subscribed signs immediately. | **Max Length** (`IntegerField`, key `MaxLength`, default 4). A `DetailPage` `LinkedPage` attribute is also registered by the migration but is unused by the block. |

`Sign Code Manager` validates each code against `^\w+$` and the `MaxLength` attribute, writes the
updated comma-delimited `Codes` string, then calls `SignUtilities.UpdateSignCategorySigns`.

### Jobs

| Job | Purpose |
|-----|---------|
| `UpdateSigns` | `[DisallowConcurrentExecution]` Quartz job; calls `SignUtilities.UpdateAllSigns()` to re-push every sign's codes. Returns immediately — the socket work is asynchronous, so completion of the job does not mean every sign has finished updating. |

### Sign sync / device protocol

`SignUtilities` gathers a sign's codes (union of its categories' comma-split `Codes`) and hands them
to `MicroframeConnection`, which speaks the board's TCP protocol via async `Socket` callbacks:

- Connects to `IPAddress:Port` (`SignUtilities` falls back to port **9107** when the `Port` string is
  unparsable) and prefixes each message with the 2-byte PIN. The PIN is derived by splitting the 4-char
  `PIN` into two 2-digit halves and converting each to a byte (`ConvertPIN`); a PIN that isn't exactly
  4 chars silently becomes `{0x00, 0x00}`.
- Sends `GLA` to read the sign's current tags, then diffs: emits `S+ <code>` to add missing codes
  (truncated to 4 chars) and `S- <tag>` to remove tags no longer present.
- A `resets` watchdog retries the connection up to 10 times before giving up.

## Dependencies & Integrations

- **Rock:** `RockContext`, Rock entity/service framework, the block/UI framework, Quartz job engine, plugin migrations.
- **Third-party:** Physical Microframe LED sign hardware (raw TCP socket protocol); Quartz.NET; Entity Framework 6.
- **Cross-plugin:** `org.secc.DevLib` (migration extension helpers used in `003_Indexes`). Codes mirror the child-pickup codes used by check-in — see [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md).

## Migrations

- `001_Init` — creates the `Sign`, `SignCategory`, and `SignSignCategory` join tables.
- `002_PagesMigration` — Microframe admin pages, the five block types, and block instances.
- `003_Indexes` — primary keys, PersonAlias foreign keys, and Guid/Foreign* indexes (each block wrapped in `try/catch`).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** The sign `PIN` is stored as plaintext in `_org_secc_Microframe_Sign.PIN` and
  transmitted over an unencrypted TCP socket. This appears to match the device's native protocol
  (the boards only support a 4-digit numeric PIN), so it's likely a hardware constraint rather than a
  fixable defect — worth confirming these signs sit on a trusted internal VLAN.
- **Improvement:** `MicroframeConnection`'s socket callbacks swallow exceptions into empty `catch {}`
  blocks and `Console.WriteLine`, with no Rock logging. A sign that is offline or misconfigured fails
  silently — there's no surfaced error for an operator. Worth routing failures to `ExceptionLogService`.
- **Improvement:** `UpdateAllSigns` opens a fresh socket per sign with no batching or backoff; fine for
  a handful of boards but worth noting if the sign count grows.

## Making Changes

- To change code validation or the immediate-push behavior, edit
  `org_secc/Microframe/SignCodeManager.ascx.cs` (`btnAdd_Click` / `RemoveCode`).
- The device protocol (connect, `GLA` diff, `S+`/`S-` framing, PIN encoding) lives entirely in
  `Utilities/MicroframeConnection.cs`; the orchestration that gathers codes per sign is in
  `Utilities/SignUtilities.cs`.
- New entity columns or tables belong in a new numbered migration under `/Migrations/` (don't edit
  migrations that have already run).
- To re-sync signs on a schedule, configure the `UpdateSigns` job in Rock; see
  [org.secc.Jobs](../org.secc.Jobs/README.md) for the other SECC scheduled jobs.
