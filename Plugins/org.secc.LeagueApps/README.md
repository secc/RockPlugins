# org.secc.LeagueApps

> Imports sports-league programs and members from the LeagueApps API into Rock as a year/category/league group hierarchy with matched people.

## Overview

LeagueApps integrates Southeast's [LeagueApps](https://leagueapps.com/) sports-registration platform
with Rock. Two scheduled jobs call the LeagueApps REST API: one syncs current programs (leagues) into
a three-level Rock group tree and enrolls each registrant as a group member, the other backfills a
LeagueApps user-id person attribute (and family id) onto matched Rock people. People are matched (or
created) through Rock's standard person matching plus suffix and previous-name fallbacks, and the
LeagueApps user id is stored so future imports are idempotent. There are no blocks, REST endpoints, or
Lava filters — it is entirely job-driven.

## Project Info

- **Project file:** `org.secc.LeagueApps.csproj`
- **Root namespace:** `org.secc.LeagueApps`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `jose-jwt.dll`, `Security.Cryptography.dll`, via the PostBuildEvent `xcopy`)
- **Cross-plugin dependencies:** [org.secc.DevLib](../org.secc.DevLib/README.md) (settings component), [org.secc.PersonMatch](../org.secc.PersonMatch/README.md)

## Project Layout

```
/Jobs/         ImportData (programs + registrants), ImportMembers (member/family backfill)
/Utilities/    APIClient (JWT/OAuth REST client), LeagueAppsHelper (person match/create),
               HTMLConvertor (strip HTML from descriptions), Constants (attribute/Guid keys)
/Components/   LeagueAppsSettings — DevLib SettingsComponent holding all config attributes
/Contracts/    POCOs deserialized from the API: Member, Programs, Registrations
/Migrations/   Rock plugin migrations (defined types/values + person & family attributes)
```

## Components

### Jobs

Each is a Quartz `IJob` with `[DisallowConcurrentExecution]`, registered as a Rock Job in the admin UI.
Most configuration lives on the shared `LeagueAppsSettings` component (see below), not on the job itself.

| Job (class) | Purpose | Key settings |
|-------------|---------|--------------|
| `ImportData` | Pull current programs, build/refresh the `Year > Category > League` group tree (league keyed by `ForeignId = programId`), set league group attributes, then enroll each registrant as a `GroupMember` with a mapped role; deactivates leagues no longer returned. | All `LeagueAppsSettings` attributes; no per-job attributes |
| `Jobs.ImportMembers` | Page through all members and backfill the `LeagueAppsUserId` person attribute and `LeagueAppsFamilyId` family attribute. | **CreateNew** (`BooleanField`) — create a new person if no Rock match |

### Settings (`LeagueAppsSettings`)

DevLib `SettingsComponent` (`[Export(typeof(SettingsComponent))]`), configured under Rock's component
settings. Attribute keys in **bold**.

| Setting | Type | Notes |
|---------|------|-------|
| **LeagueAppsSiteId** | EncryptedText | LeagueApps site id (decrypted at call time). |
| **LeagueAppsClientId** | EncryptedText | API key / OAuth client id. |
| **LeagueAppsServiceAccountFile** | File | PKCS#12 (`.p12`) private key used to sign the JWT bearer assertion. |
| **ParentGroup** | Group | Grandparent group the year/category/league tree is built under. |
| **YearGroupType** | GroupType | Group type for the year level. |
| **CategoryGroupType** | GroupType | Group type for the category (mode) level. |
| **LeagueGroupType** | GroupType | Group type for the league itself; its roles map LeagueApps roles. |
| **LeagueGroupTeam** | Attribute (GroupMember) | Group-member attribute populated with the registrant's team. |
| **DefaultConnectionStatus** | DefinedValue (Connection Status) | Connection status for newly created people (default Prospect). |

### Contracts

POCOs deserialized from LeagueApps JSON; epoch-millisecond dates use a custom `MillisecondEpochConverter`.

| Class | Maps to |
|-------|---------|
| `Programs` | A program/league (id, name, sport, season, gender, mode, dates, URLs, logo). |
| `Registrations` | A registrant within a program (`userId`, `team`, `role`). |
| `Member` | A LeagueApps member (id, name, email, birth date, gender, address, phone, `groupId`). |

### Migration-installed data

| Defined Type / Attribute | Guid |
|--------------------------|------|
| Sport (Group defined type) | `3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3` |
| Season (Group defined type) | `cb85b2aa-46e6-4655-802c-4fe33379018d` |
| Gender (Group defined type) + values | `4FACC42E-8783-4805-859F-AAC6D8951CE6` |
| `LeagueAppsUserId` person attribute | `FA679B84-53A4-4DE9-84B5-D4EAFCC52E75` |
| `League Apps Family Id` family-group attribute | `D9A8CBFB-4F9B-41B8-837F-24772A00FCAE` |

## Dependencies & Integrations

- **Rock:** Quartz job framework (`IJob`, `JobDataMap`), `RockContext` and Rock services
  (`GroupService`, `PersonService`, `GroupMemberService`, `LocationService`, attribute/defined-value
  services), `RockMigrationHelper` plugin migrations, `Rock.Security.Encryption`, the attribute and
  caching frameworks.
- **Cross-plugin:** [org.secc.DevLib](../org.secc.DevLib/README.md) — `SettingsComponent` base for
  `LeagueAppsSettings`; [org.secc.PersonMatch](../org.secc.PersonMatch/README.md) — types used by the
  match helpers.
- **Third-party APIs:** LeagueApps (`public.leagueapps.io`, `auth.leagueapps.io`,
  `admin.leagueapps.io`) — public calls use an `la-api-key` header; private calls sign an RS256 JWT
  with the PKCS#12 key and exchange it for an OAuth bearer token.
- **Other:** RestSharp (public client), `System.Net.Http.HttpClient` (private client), `jose-jwt`
  (JWT signing), `Security.Cryptography` (RSACng for CNG keys), Newtonsoft.Json.

## Migrations

Ships Rock plugin migrations under `/Migrations/`:

- `001_CreateDefinedTypes` — Sport and Season defined types (`MigrationNumber 1`).
- `002_CreateDefinedValues` — seed Sport/Season defined values (`MigrationNumber 2`).
- `003_CreateGenderType` — Gender defined type (`MigrationNumber 3`).
- `004_CreateGenderDefinedValues` — seed Gender defined values (`MigrationNumber 4`).
- `005_CreateAttribute` — `LeagueAppsUserId` person attribute (`MigrationNumber 5`).
- `006_FamilyAttribute` — `League Apps Family Id` family-group attribute (`MigrationNumber 6`).

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** `ImportData` runs most of its work on a single long-lived `RockContext` (`dbContext`)
  with repeated `SaveChanges()` inside loops, and re-queries groups/defined values per program. The
  inner registrant loop does open a fresh `RockContext` per person, but the outer job can hold a large
  tracked graph for the full run; worth reviewing for large program counts.
- **Improvement:** `006_FamilyAttribute.Down()` is empty, so rolling that migration back leaves the
  family attribute in place. The other migrations implement `Down()` — worth aligning for consistency.
- **Improvement:** The two API base URLs and `"notasecret"` PKCS#12 password are hard-coded in
  `APIClient` (the password matches Google service-account `.p12` convention). The auth flow also runs
  async work via `.GetAwaiter().GetResult()`, which can deadlock under some sync contexts — acceptable
  inside a Quartz job thread, but worth noting if this code is ever reused on a request thread.
- **Improvement:** `LeagueAppsHelper.GetPersonByApiId` matches with a substring `Value.Contains(userId + "|")`,
  while `ImportData` uses a stricter three-pattern match (`== id`, `Contains("|" + id + "|")`,
  `StartsWith(id + "|")`). The substring `Contains` is the looser of the two — a query for `12|` also
  matches a stored value like `512|` — so the two paths can disagree on which person an id maps to;
  worth aligning `GetPersonByApiId` with the stricter `ImportData` patterns.
- **Improvement:** `CreatePersonFromMember` calls `CampusCache.All().FirstOrDefault()` for new
  families' campus, which is order-dependent rather than an explicit/configured campus.

## Making Changes

- To change which groups/group types/attributes the import targets, edit the attributes on the
  **League Apps Settings** component in Rock — no code change needed. Their keys are defined in
  `Utilities/Constants.cs`.
- Person matching, creation, suffix parsing, and family resolution all live in
  `Utilities/LeagueAppsHelper.cs`; the match overloads accept injected lists for unit testing.
- To consume a new LeagueApps API field, add it to the matching `Contracts/*` POCO and reference it in
  the job; API auth and request plumbing is in `Utilities/APIClient.cs`.
- New seed data (defined types/values, attributes) belongs in a new numbered migration under
  `/Migrations/` — don't hand-edit migrations that have already run.
- Related: people created here flow through the same matching concerns as
  [org.secc.PersonMatch](../org.secc.PersonMatch/README.md).
