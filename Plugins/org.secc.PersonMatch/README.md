# org.secc.PersonMatch

> A `PersonService.GetByMatch` extension method that resolves loose name/contact fields to existing Rock people (nickname-aware), with optional nameless-person creation.

## Overview

PersonMatch is a thin library plugin: it adds one extension method, `GetByMatch`, to Rock's
`PersonService` that takes loose identity fields (first/last name, DOB, email, phone, address) and
returns the matching `Person` records. Matching is nickname-aware via a "Diminutive Names" defined
type the plugin installs. It has no UI of its own — it's consumed by other plugins (notably the
*Person Attribute From Fields* action in [org.secc.Workflow](../org.secc.Workflow/README.md)) to
de-duplicate people when ingesting form/registration/SMS data.

## Project Info

- **Project file:** `org.secc.PersonMatch.csproj`
- **Root namespace:** `org.secc.PersonMatch`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly only; PostBuildEvent xcopies the DLL)

## Project Layout

```
Extension.cs    PersonService.GetByMatch extension method (the entire matching logic)
/Migrations/    DiminutiveNames_DefinedType — installs the nickname defined type + values
```

## Components

### Extension Method

`org.secc.PersonMatch.Extension.GetByMatch` — extends `PersonService`.

```
IEnumerable<Person> GetByMatch(
    string firstName, string lastName, DateTime? birthDate,
    string email = null, string phone = null,
    string street1 = null, string postalCode = null,
    bool createNameless = false )
```

Matching algorithm:

| Step | Behavior |
|------|----------|
| Required inputs | `firstName` + `lastName` + at least one of (`birthDate`, `email`, `phone`, `street1`). Otherwise returns an empty list — unless `createNameless` and an email/phone are present, in which case it returns a single nameless person (found or created). |
| Fast path | If `birthDate` or `email` is present, tries an exact query on first/nick name + last name (+ DOB + email). Returns immediately on a single exact match. |
| Lenient pass | Queries everyone with the same `LastName` (and matching/absent `BirthDate`), then keeps only those where the phone or email appears anywhere in the person's family, **or** the home address `Street1` matches. Address matching only runs when **both** `street1` and `postalCode` are supplied (the supplied address is geocoded once via `LocationService.Verify` if a literal `Street1` compare fails). |
| Nickname narrowing | Final list is filtered by first name, treating entries in the "Diminutive Names" defined type as equivalent (e.g. "Bob" ≈ "Robert"), comparing against the person's `FirstName`/`NickName`. |

`createNameless` routes to the private `GetOrCreateNamelessPerson`, which looks up an existing
nameless-record-type person by email/phone and **creates and saves a new one** (mobile phone with
messaging enabled) if exactly one isn't found.

## Dependencies & Integrations

- **Rock:** `PersonService`, `RockContext`, `LocationService` (address geocode/verify),
  `AttributeValueService`, `DefinedTypeCache`/`DefinedValueCache`/`AttributeCache`, `PhoneNumber`,
  nameless-person record type and mobile phone-type system Guids.
- **Cross-plugin:** consumed by [org.secc.Workflow](../org.secc.Workflow/README.md)
  (*Person Attribute From Fields* action).
- No third-party APIs.

## Migrations

Ships one Rock plugin migration that installs the matching reference data:

- `DiminutiveNames_DefinedType` (`MigrationNumber 1`, `1.2.0`) — adds the **Diminutive Names**
  defined type (`3E2D2BEE-…`) with a "Goes By" attribute (`C31FDA8A-…`, pipe-delimited alternates)
  and 2,168 name defined values. `Down()` removes the "Goes By" attribute, all 2,168 defined
  values, and the defined type.

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** With `createNameless = true`, `GetByMatch` will **insert and `SaveChanges()`
  a new person** as a side effect of a "lookup" call. Callers driven by public/untrusted input
  (forms, SMS) can therefore create person records — review which workflows pass that flag.
- **Improvement:** The nickname-narrowing loop dereferences `av.Value` and
  `matchingPerson.FirstName`/`NickName` without null guards (`av` from
  `attributeValues.Where(...).FirstOrDefault()` can be null; FirstName/NickName can be null). A
  defined value missing its "Goes By" value, or a person with a null name, risks a
  `NullReferenceException` — worth confirming.
- **Improvement:** The method opens a second `RockContext` internally for its `LocationService`
  and `AttributeValueService`, while the actual person queries still run on the caller's
  `personService` context — so two contexts are live at once for a single call. The address-match
  block also swallows all exceptions silently (`catch (Exception) {}`), which can mask
  geocoding/verify failures.

## Making Changes

- Matching logic lives entirely in `Extension.cs` (`GetByMatch` and `GetOrCreateNamelessPerson`).
  The defined-type/attribute Guids are hardcoded constants at the top of the class.
- To add or remove recognized nicknames, prefer editing the **Diminutive Names** defined type in
  Rock (admin UI) rather than re-running the migration; the migration is a one-time seed.
- The primary caller is the *Person Attribute From Fields* action in
  [org.secc.Workflow](../org.secc.Workflow/README.md) — changes to the signature or matching
  behavior ripple there.
