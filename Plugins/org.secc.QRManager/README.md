# org.secc.QRManager

> Generates QR-code images and Lava filters that turn a person into a scannable check-in / Fast Pass QR URL.

## Overview

QRManager exposes a REST endpoint that renders a QR-code PNG for an arbitrary code string,
plus a set of Lava filters that build the QR URL (and an `<img>` tag) for a person. It is used
to produce personal check-in codes and "Personal Fast Pass" (PFP) codes that can be embedded in
emails, statements, and Lava templates and scanned at a kiosk.

## Project Info

- **Project file:** `org.secc.QRManager.csproj`
- **Root namespace:** `org.secc.QRManager`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `QRCoder.dll`)

## Project Layout

```
/Lava/         CustomFilters.cs   — Lava filters (QRUrl, QRImage, PersonalFastPass*)
/Controllers/  QRController        — REST endpoint that renders the QR PNG
/Startup/      LoadCustomFilters   — IRockOwinStartup hook that registers the filters
```

## Components

### REST Endpoints

| Route | Method | Purpose |
|-------|--------|---------|
| `api/qr/{code}` | GET (authenticated) | Returns a PNG QR-code image encoding `{code}`. |

### Lava Filters

Registered at startup via `LoadCustomFilters` (`IRockOwinStartup`).

| Filter | Purpose |
|--------|---------|
| `QRUrl` | Builds the QR URL for a person (accepts `Person`, `PersonAlias`, `GroupMember`, `RegistrationRegistrant`, or a person Id). |
| `QRImage` | Same as `QRUrl` but returns an `<img>` tag (optional `width`, default 300). |
| `PersonalFastPassUrl` | QR URL using the `PFP` (Personal Fast Pass) prefix. |
| `PersonalFastPassImage` | `<img>` tag variant of the Fast Pass URL. |

## Dependencies & Integrations

- **Rock:** `RockContext`, `PersonService`, Rock REST (`ApiControllerBase`).
- **Third-party:** `QRCoder` (QR generation), `System.Drawing` (PNG output), DotLiquid (Lava).

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** The QR "code" is a person's randomly-generated **Alternate Id** search key
  (`GenerateRandomAlternateId`), not a sequential/guessable Id or the person Guid — so codes aren't
  easily enumerable. The `api/qr/{code}` endpoint is `[Authenticate]`-gated and only renders the
  string as an image, so it doesn't leak data on its own. Worth confirming the alternate-id search
  path that *consumes* these codes is itself access-controlled.
- **Improvement:** `GetURL` is a **Lava filter that writes to the database** — when a person has no
  alternate-id search key it creates one and calls `SaveChanges()`. Template rendering should be
  side-effect-free; as written, rendering a Lava template can mutate data and isn't idempotent.
  Consider provisioning the search key up front (job/workflow) and keeping the filter read-only.
- **Improvement:** A new `RockContext` is constructed on every filter call, so rendering a list of N
  people spins up N contexts. Acceptable for one-offs, wasteful in loops.

## Making Changes

- To add or change a filter, edit `Lava/CustomFilters.cs`; filters are auto-registered because
  the whole `CustomFilters` type is passed to `Template.RegisterFilter` in
  `Startup/LoadCustomFilters.Partial.cs`.
- The person-resolution logic (which input types map to a `Person`) lives in the private
  `GetURL` helper in `CustomFilters.cs`.
- See [org.secc.FamilyCheckin](../org.secc.FamilyCheckin/README.md) for where these codes are scanned.
