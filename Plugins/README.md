# SECC Rock Plugins

This folder holds Southeast Christian Church's custom [Rock RMS](https://www.rockrms.com/) plugins —
each `org.secc.*` directory is one plugin (a Visual Studio project or a set of RockWeb-compiled
blocks/themes). Every plugin has its own `README.md` documenting what it does, its components
(blocks, REST endpoints, Lava filters, workflow actions, jobs, field types, models, migrations),
its dependencies, and notes found while documenting.

**Audience:** internal SECC developers and AI coding agents working in this repo.

**Doc tiers:** most plugins use the **standard** tier (overview → components → dependencies →
observations → making changes). High-traffic, complex, or externally-facing plugins use the
**deep** tier, which adds a data-flow diagram, per-component config-attribute tables, edge cases,
and an extending guide. The Tier column below flags which is which.

Each plugin's **Observations** section captures security/improvement notes spotted *while
documenting* — they are starting points, not a completed security audit.

---

## Check-In & Attendance

| Plugin | Tier | What it does |
|--------|------|--------------|
| [FamilyCheckin](./org.secc.FamilyCheckin/README.md) | standard | Southeast's custom check-in stack — kiosk/kiosk-type models, a library of check-in workflow actions, a cached check-in state layer, mobile/touchless check-in, and consent/quick-check-in UI blocks. |
| [CheckinMonitor](./org.secc.CheckinMonitor/README.md) | standard | Staff-facing check-in admin blocks: room-ratio management, a live attendance monitor, mobile check-in printing, a cache inspector, and an advanced "Super" check-in/search tool. |
| [RoomScanner](./org.secc.RoomScanner/README.md) | standard | A REST API (plus two admin blocks) that lets a kids' room-scanner app move children and volunteers in and out of check-in rooms with PIN-authorized overrides and two-volunteer safety rules. |
| [SportsAndFitness](./org.secc.SportsAndFitness/README.md) | standard | Rock blocks and a theme for Southeast's Activities Center — member/guest check-in, group-fitness session tracking, guest self-registration, and a staff "control center". |
| [Microframe](./org.secc.Microframe/README.md) | standard | Drives physical Microframe LED paging signs over TCP — pushing child-pickup codes from Rock so they display (and clear) on the boards. |
| [QRManager](./org.secc.QRManager/README.md) | standard | Generates QR-code images and Lava filters that turn a person into a scannable check-in / Fast Pass QR URL. |

## Connections & Groups

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Connection](./org.secc.Connection/README.md) | standard | Public-facing volunteer signup blocks that turn Rock connection opportunities into configurable, partition-driven signup pages. |
| [ConnectionCards](./org.secc.ConnectionCards/README.md) | standard | A Rock block that ingests a scanned PDF sheet of physical connection cards, slices it into a grid of per-card images, and launches a workflow for each one. |
| [ConnectionsDigest](./org.secc.ConnectionsDigest/README.md) | standard | A scheduled job that emails each connector a periodic digest of their connection requests (new, active, idle, critical) for the selected opportunities. |
| [GroupManager](./org.secc.GroupManager/README.md) | standard | A suite of Rock blocks for self-service group management — leader rosters, attendance, group finder, and a "Publish Group" model that lets ministries advertise and register groups. |
| [GroupTrackerDemo](./org.secc.GroupTrackerDemo/README.md) | standard | REST endpoints that list a leader's groups and let a client view and toggle today's attendance for a group's members. |
| [FamilyOnMission](./org.secc.FamilyOnMission/README.md) | standard | A Rock block that lists the "Family on Mission" classes by time slot and lets a logged-in person sign up for an open one. |

## Communications

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Communication](./org.secc.Communication/README.md) | standard | SMS/keyword administration, communication-list tooling, schedule-reminder jobs, and a Twilio message-history sync for Southeast's messaging. |
| [RecurringCommunications](./org.secc.RecurringCommunications/README.md) | standard | Lets staff define email/SMS/push communications that re-send on a Rock schedule to a Data View audience, sent by a scheduled job. |

## Finance & Payments

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Finance](./org.secc.Finance/README.md) | standard | Generates and serves annual giving (contribution) statements — workflow action + merge-field builder, launch/list/print blocks, a download handler, a generation job, plus a bulk registration-refund block. |
| [PayFlowPro](./org.secc.PayFlowPro/README.md) | deep | A custom Rock financial gateway component subclassing Rock's stock PayFlow Pro gateway to add an "Every 4 Weeks" frequency, friendlier invalid-saved-account handling, and a recurring-billing reconciliation report. |
| [PayPalExpress](./org.secc.PayPalExpress/README.md) | deep | A Rock financial gateway component for PayPal Express Checkout (Classic Merchant API), plus a drop-in Transaction Entry block that adds a PayPal tab to Rock's giving flow. |
| [PayPalReporting](./org.secc.PayPalReporting/README.md) | deep | A scheduled job that pulls PayFlow Pro transaction reports out of PayPal into a custom Rock table, with a block to view/edit individual rows. |
| [Purchasing](./org.secc.Purchasing/README.md) | standard | Southeast's in-house purchasing system — requisitions, purchase orders, receiving, vendors, payment methods, and capital requests — on a custom LINQ-to-SQL data layer with Sage Intacct GL validation. |

## Events, Registration & Learning

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Event](./org.secc.Event/README.md) | standard | Southeast's custom event/registration blocks — calendar Lava, a heavily-customized registration entry/detail pair, QR event passes, SignNow signing, camp-placement import, and the Community Gives Back program. |
| [Equip](./org.secc.Equip/README.md) | standard | A self-contained learning-management system (LMS) for Rock — courses, chapters, multi-format lesson pages, quizzes, and per-person completion tracking with group/data-view requirements. |

## Workflow & Automation

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Workflow](./org.secc.Workflow/README.md) | deep | A library of custom Rock workflow actions (plus two bulk-workflow admin blocks) covering people, communications, registrations, media, and workflow control. |
| [WorkflowLauncher](./org.secc.WorkflowLauncher/README.md) | standard | Tools for launching and bulk-managing Rock workflows over groups, registrations, data views, and arbitrary SQL result sets. |
| [Jobs](./org.secc.Jobs/README.md) | standard | A grab-bag of Southeast's custom Rock scheduled jobs (Quartz `IJob`s) for data maintenance, attendance, communications, and one-off migrations. |
| [PDF](./org.secc.PDF/README.md) | standard | Workflow actions that generate PDFs — render Lava/HTML to PDF, fill a PDF form template, and combine two PDFs — outputting to a binary-file workflow attribute. |
| [SignNowWorkflow](./org.secc.SignNowWorkflow/README.md) | standard | Two Rock workflow actions that push a PDF into SignNow for e-signature and pull the signed copy back when it's done. |

## Security & Identity

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Authentication](./org.secc.Authentication/README.md) | deep | A passwordless SMS one-time-code authentication provider for Rock — texts a 6-digit code to a person's mobile number and logs them in with it. |
| [OAuth](./org.secc.OAuth/README.md) | deep | A self-hosted OAuth 2.0 authorization server for Rock — issues tokens against Rock `UserLogin` credentials, with admin-managed clients/scopes and profile REST endpoints. |
| [Security](./org.secc.Security/README.md) | deep | A grab-bag of SECC security/identity Rock blocks — Wi-Fi captive portals, SAML/SSO redirects (WellRight, Church Online), PIN management, self-service account blocks — plus a reusable SAML 2.0 assertion library. |
| [Search](./org.secc.Search/README.md) | standard | Custom Rock person-search components (address, DOB, envelope) plus admin blocks for person, bulk, and Apple Pass lookups. |

## Reporting & Monitoring

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Reporting](./org.secc.Reporting/README.md) | standard | Southeast's custom reporting surface — admin/ministry report blocks, a "SQL Filter" data-view component, a camp-medication Excel export workflow action, and supporting EF models, migrations, and SQL objects. |
| [MetricsDigest](./org.secc.MetricsDigest/README.md) | standard | A scheduled job that emails a per-campus "metric entry progress" digest, showing how many selected metrics have values entered for a date range. |
| [SystemsMonitor](./org.secc.SystemsMonitor/README.md) | standard | A pluggable system-monitoring framework: define HTTP/SQL/Lava "system tests", run them on a schedule, record pass/fail history, and alert staff by email/SMS on an alarm condition. |

## Pastoral Care & Safety

| Plugin | Tier | What it does |
|--------|------|--------------|
| [PastoralCare](./org.secc.PastoralCare/README.md) | standard | Block-based lists that let Pastoral Care staff track hospital, homebound, nursing-home, and communion visits. |
| [SafetyAndSecurity](./org.secc.SafetyAndSecurity/README.md) | standard | Campus lock-down SMS alerting plus workflow actions for volunteer background-check applications, incident-report PDFs, and MinistrySafe training. |

## Third-party Integrations

| Plugin | Tier | What it does |
|--------|------|--------------|
| [EMS](./org.secc.EMS/README.md) | standard | Thin SOAP client + Rock block that pulls room/event bookings from the EMS (Event Management System) facility-scheduling product and lists them in Rock. |
| [LeagueApps](./org.secc.LeagueApps/README.md) | standard | Imports sports-league programs and members from the LeagueApps API into Rock as a year/category/league group hierarchy with matched people. |
| [ServiceReef](./org.secc.ServiceReef/README.md) | standard | Scheduled jobs that pull mission-trip events, participants, and payments from the ServiceReef API into Rock as groups, people, and financial transactions. |
| [Trak1](./org.secc.Trak1/README.md) | standard | A Rock background-check provider that submits applicant data to the Trak-1 web service and polls back report status/results through a workflow action. |
| [Mapping](./org.secc.Mapping/README.md) | standard | Computes and caches driving distance/time from an address to a set of Rock entities (campuses, groups) via the Azure Maps Route Matrix API, exposed as a REST endpoint and a workflow action. |
| [Imaging](./org.secc.Imaging/README.md) | standard | Renders HTML/Lava into PNG images over REST, and bulk-updates person photos by AI face-cropping existing binary files via Azure Cognitive Services Face. |

## CMS, Theming & UI

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Cms](./org.secc.Cms/README.md) | standard | A grab-bag of Southeast's custom Rock CMS blocks — login/profile UI, section/topper page chrome, content-channel and Twitter Lava, QR-code shortlinks, cache/Font-Awesome admin tools — plus a styled `SectionZone` control. |
| [Widgities](./org.secc.Widgities/README.md) | standard | A drag-and-drop content-builder: admins define reusable "widgity" types (a Lava/Markdown template + an attribute schema), then place and configure them against any Rock entity. |
| [Attributes](./org.secc.Attributes/README.md) | standard | Three custom Rock attribute field types — a cascading (dependent) dropdown, a typed phone-number picker, and a multi-file uploader — with their edit controls and `[FieldAttribute]` decorators. |
| [Themes](./org.secc.Themes/README.md) | standard | Southeast's Rock site themes — the master pages, page layouts, and styling that skin Rock's external and check-in sites. |
| [CustomIcons](./org.secc.CustomIcons/README.md) | standard | Ships a Southeast-branded icon webfont (`se-*` classes) and the LESS that wires it into Rock's Font Awesome stack. |
| [LessInclude](./org.secc.LessInclude/README.md) | standard | A CMS block that lets admins edit LESS variables/theme per page and compiles them to CSS on save, swapping the page's stylesheet for the result. |
| [Sass](./org.secc.Sass/README.md) | standard | Adds SCSS/SASS compilation to Rock themes — Rock natively compiles only LESS, so this compiles each theme's `Styles/*.scss` to CSS on startup and from the CMS theme blocks. |

## Platform, APIs & Developer Tools

| Plugin | Tier | What it does |
|--------|------|--------------|
| [Rest](./org.secc.Rest/README.md) | deep | Custom Rock REST controllers for SECC's apps — account/SMS login, the Groups mobile app, content-channel/sermon feeds, person matching, and financial-statement data. |
| [PersonMatch](./org.secc.PersonMatch/README.md) | standard | A `PersonService.GetByMatch` extension method that resolves loose name/contact fields to existing Rock people (nickname-aware), with optional nameless-person creation. |
| [Administration](./org.secc.Administration/README.md) | standard | A grab-bag of admin-only Rock UI blocks — system diagnostics, one-off data migrations, and group/device housekeeping tools. |
| [ChangeManager](./org.secc.ChangeManager/README.md) | standard | A change-request/approval framework that captures edits to any Rock entity as reviewable `ChangeRecord`s and applies (or reverses) them transactionally on approval. |
| [DevLib](./org.secc.DevLib/README.md) | standard | A small shared library of developer utilities for other SECC plugins — a MEF-backed "settings component" base, EF-migration helper extensions, and a search DTO. |
| [Migrations](./org.secc.Migrations/README.md) | standard | A library of Rock plugin migrations that install SECC's pages, workflows, defined types, attributes, stored procedures, and DB triggers at startup. |
| [AI](./org.secc.AI/README.md) | standard | Empty scaffold project — no functionality yet; reserved as the home for future AI-related Rock code. |
