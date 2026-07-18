# org.secc.FamilyOnMission

> A single Rock block that lists the "Family on Mission" classes by time slot and lets a logged-in person sign up for an open one.

## Overview

FamilyOnMission ships one RockWeb block, **FoM Signup**, used for the Family on Mission
registration page. It reads the child groups of a configured parent group, buckets them by their
schedule's start time, and renders a card per class showing teacher, location, track, and remaining
spots. Inside an optional open/close window it shows a **Sign Up** button (linking to a configured
landing page); outside the window — or once the person is already a member — it shows the class(es)
they're enrolled in instead.

## Project Info

- **Project file:** none — no `.csproj`; RockWeb-compiled (`.ascx` + `.ascx.cs` only).
- **Root namespace:** `RockWeb.Plugins.org_secc.FamilyOnMission`
- **Target framework:** n/a (compiled in-place by RockWeb)
- **Deploys to:** `RockWeb/Plugins/org_secc/FamilyOnMission/` (block markup + code-behind)

## Project Layout

```
/org_secc/FamilyOnMission/   FoMSignup.ascx (+ .ascx.cs) — the sign-up listing block
```

## Components

### Blocks

Category in Rock: **SECC > Family On Misson** (spelling per source).

| Block | Purpose | Key settings |
|-------|---------|--------------|
| FoM Signup | Lists Family on Mission classes grouped by start time; shows a Sign Up button for open classes or the person's enrolled class outside the window. | Parent Group, Next Page, Open, Close |

Block attributes (set in Rock block settings):

| Setting | Type | Notes |
|---------|------|-------|
| **Parent Group** | Group | The parent group whose child groups are the FoM classes. Child groups must have a `Schedule`. |
| **Next Page** | Linked Page | Sign-up target; the `Sign Up` button links here with `?GroupId={id}`. |
| **Open** | DateTime | Optional. Registration is closed before this time. |
| **Close** | DateTime | Optional. Registration is closed after this time. |

Per-group attributes the block reads (must exist on the child groups / their track):

| Attribute | On | Notes |
|-----------|----|-------|
| **Track** | Group | Defined Value Guid; resolves a track whose **Image** / **About** values are shown on the card. |
| **Teacher** | Group | Free-text teacher name shown on the card. |
| **Image** / **About** | Track Defined Value | Track icon and (currently unused in output) description. |

## Dependencies & Integrations

- **Rock:** `RockBlock`, `RockContext`, `GroupService`, `DefinedValueCache`, group schedules /
  capacity / locations, the attribute framework, and `LinkedPageUrl`.
- No third-party APIs.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** Card markup is built by `string.Format` against unencoded group/track/teacher
  values (`Name`, `Description`, `Teacher`, location name) injected into HTML. These are
  staff-entered fields, so exposure is limited, but HTML-encoding the values would be safer and is
  cheap. The first card template also has a mismatched tag (`<h3>…</h2>`).
- **Improvement:** `OnLoad` builds the whole list every postback (no `!IsPostBack` guard) and queries
  group membership/capacity per render; fine for a short class list, worth noting if the parent group
  grows. `trackAbout` is computed but never rendered.
- **Improvement:** Capacity check uses `( GroupCapacity - Members.Count() ) < 1` to decide whether to
  hide a full class. `GroupCapacity` is nullable, so when capacity is unset the subtraction is `null`
  and `null < 1` is `false` — the class is **never** filtered out and shows with no "Spots remaining"
  count. Set a capacity on every FoM class if you want the full-class hide behavior to work.

## Making Changes

- To change the card layout, what fields show, or the open/close behavior, edit
  `org_secc/FamilyOnMission/FoMSignup.ascx.cs` (markup is assembled in `OnLoad`); inline CSS lives at
  the top of `FoMSignup.ascx`.
- Grouping is by each child group's `Schedule.StartTimeOfDay` — classes without a schedule are
  skipped. The Sign Up flow hands off to the configured **Next Page** with `?GroupId=`.
