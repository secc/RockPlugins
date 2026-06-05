# org.secc.Equip

> A self-contained learning-management system (LMS) for Rock — courses, chapters, multi-format lesson pages, quizzes, and per-person completion tracking with group/data-view requirements.

## Overview

Equip ("the learning tool") lets Southeast author and deliver online courses inside Rock. A
**Course** contains ordered **Chapters**, each containing ordered **Course Pages** (HTML, YouTube,
Vimeo, quiz, or drag-and-drop lessons). As a person works through a course, the plugin records
their progress and pass/fail per page, chapter, and course. **Course Requirements** assign a course
to a group or data view and track who still owes it (and when their completion expires), surfaced
through Lava and report data filters. Staff manage all of this through a set of admin blocks under
**SECC > Equip**; learners consume courses through the Course Entry / Course Lava blocks.

## Project Info

- **Project file:** `org.secc.Equip.csproj`
- **Root namespace:** `org.secc.Equip`
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly) and `RockWeb/Plugins/org_secc/` (block `.ascx` markup
  and `.js`), via the PostBuildEvent `xcopy`
- **Cross-plugin dependency:** [org.secc.Widgities](../org.secc.Widgities/README.md) (the Drag &
  Drop page uses its `WidgityControl`)

## Project Layout

```
/Model/             8 EF entities + their RockContext services (Course, Chapter, CoursePage,
                    CourseRecord, ChapterRecord, CoursePageRecord, CourseRequirement,
                    CourseRequirementStatus)
/Data/              LearningContext — a Rock DbContext over the Equip tables
/CoursePages/       MEF page-type components (HTML, YouTube, Vimeo, Quiz, Drag & Drop)
/CoursePageComponent.cs, CoursePageContainer.cs   base component + MEF container for page types
/Helpers/           CourseRequirementHelper (status sync), PersonCourseInfo (Lava DTO)
/Jobs/              UpdateCourseRequirements — recompute requirement statuses
/Lava/              CustomFilters.cs — the PersonCourseInfo Lava filter
/Startup/           LoadCustomFilters — IRockOwinStartup hook that registers the filter
/Reporting/DataFilter/   3 Person data filters (completion / requirement / expiring)
/Controls/          CoursePicker — a course-selection control used by the data filters and the
                    Course Requirement Detail block
/org_secc/Equip/    12 admin/learner UI blocks (.ascx + .cs) and supporting .js
/Migrations/        Rock plugin migrations (001 stands up all tables; 002–007 add columns)
```

## Components

### Models

EF entities (table prefix `_org_secc_Equip_`), each with a matching `*Service`. `Course` and
`CourseRequirement` implement `ISecured`; `Course` is also `ICategorized` / `IHasActiveFlag`.

| Model | Table | Purpose |
|-------|-------|---------|
| `Course` | `_org_secc_Equip_Course` | Top-level course: name, description, category, image, slug, icon, active flag, `AllowDocumentationMode` flag, optional external URL, and optional `AllowedGroup` / `AllowedDataView` viewer restriction. |
| `Chapter` | `_org_secc_Equip_Chapter` | Ordered section within a course. |
| `CoursePage` | `_org_secc_Equip_CoursePage` | A single lesson page; `EntityTypeId` points at the MEF page-type component, `Configuration` holds that type's serialized settings, `PassingScore` is its threshold. |
| `CourseRecord` | `_org_secc_Equip_CourseRecord` | Per-person attempt/completion of a whole course (`Passed`, `CompletionDateTime`). |
| `ChapterRecord` | `_org_secc_Equip_ChapterRecord` | Per-person completion of a chapter within a course record. |
| `CoursePageRecord` | `_org_secc_Equip_CoursePageRecord` | Per-person result for a page (`Score`, `Passed`, completion details). |
| `CourseRequirement` | `_org_secc_Equip_CourseRequirement` | Assigns a course to a `Group` or `DataView`, with optional `DaysValid` expiration window. |
| `CourseRequirementStatus` | `_org_secc_Equip_CourseRequirementStatus` | Per-person row tracking whether a required course is complete and `ValidUntil`. |

### Course Page Types (MEF components)

Subclasses of `CoursePageComponent`, discovered via MEF (`[Export(typeof(CoursePageComponent))]`)
and resolved through `CoursePageContainer`. Each renders its own author-time configuration and
learner-time display, and scores the page into a `CoursePageRecord`.

| Component (ComponentName) | Icon | Notes |
|---------------------------|------|-------|
| HTML Page | `fa-file-code` | Free-form lesson HTML authored in the Rock HtmlEditor. |
| YouTube Video Page | `fa-youtube` | Embeds a YouTube iframe; "Watch Percent Required" (stored in `PassingScore`) gates completion via `YouTubeHelper.js`. |
| Vimeo Video Page | `fa-vimeo` | Same as YouTube but for Vimeo (`VimeoHelper.js`, Vimeo player API). |
| Quiz Page | `fa-question-circle` | Question/answer builder (`QuizEditor.js` / `QuizDisplay.js`); JSON config + a passing score. |
| Drag & Drop Page | `fa-bars` | Builds the page with [org.secc.Widgities](../org.secc.Widgities/README.md)' `WidgityControl`. |

### Blocks

Category in Rock: **SECC > Equip**.

| Block | Purpose |
|-------|---------|
| Course List | Grid of courses to manage (links to Course Detail). |
| Course Detail | Create/edit a course (category, image, slug, icon, viewer restriction, external URL). |
| Course Information | Read-only course summary with links to edit and to requirements. |
| Course Outline | Shows all chapters and pages in a course. |
| Chapter List | Grid of a course's chapters. |
| Chapter Detail | Create/edit a chapter. |
| Course Page List | Grid of a chapter's pages. |
| Course Page Edit | Create/edit a page (picks the page type, renders its configuration). |
| Course Entry | The learner experience — walks a person through chapters/pages and records results. |
| Course Lava | Renders courses through a configurable Lava template (`CodeEditorField`). |
| Course Requirement List | Grid of course requirements. |
| Course Requirement Detail | Create/edit a requirement (course + group/data view + days-valid). |

### Lava Filters

Registered at startup via `LoadCustomFilters` (`IRockOwinStartup`).

| Filter | Purpose |
|--------|---------|
| `PersonCourseInfo` | Given a `Person`, returns a list of `PersonCourseInfo` (course, category, complete/expired flags). Optional second arg is a space-delimited list of course Category Ids to scope to. Only returns courses the **current** person is authorized to VIEW. |

### Jobs

| Job (class) | Purpose |
|-------------|---------|
| `UpdateCourseRequirements` | `[DisallowConcurrentExecution]` Quartz `IJob`. Walks every `CourseRequirement` and calls `CourseRequirementHelper.UpdateCourseRequirementStatuses` to add/remove per-person status rows (from the group or data view, intersected with the course's allowed viewers) and recompute complete/`ValidUntil`. |

### Report Data Filters

`DataFilterComponent`s on `Rock.Model.Person`, section **Learning Tool**.

| Filter (ComponentName) | Purpose |
|------------------------|---------|
| Course Completion Status | Filter people by completion of a chosen course — has completed / not completed / started-not-completed / not started. |
| Course Requirement Status | Filter people by their status against a course requirement. |
| Expiring Course Requirement | Filter people whose requirement completion is expiring. |

## Dependencies & Integrations

- **Rock:** EF (`Rock.Data.DbContext`, `Model<T>`, `IRockEntity`), the category/security framework
  (`ICategorized`, `ISecured`, `IHasActiveFlag`), Quartz jobs, reporting data filters, the MEF
  component framework (`Rock.Extension.Container`/`Component`), block/UI framework, DotLiquid (Lava),
  Binary Files (course images), DataViews and Groups (requirement membership + viewer restriction).
- **Cross-plugin:** [org.secc.Widgities](../org.secc.Widgities/README.md) — the Drag & Drop page type.
- **Third-party:** Newtonsoft.Json (quiz/page config serialization). Video pages embed YouTube /
  Vimeo players (client-side; no server-side API calls).

## Migrations

Ships Rock plugin migrations under `/Migrations/`:

- `001_Init` — creates all eight `_org_secc_Equip_*` tables, keys, FKs, and indexes.
- `002_ActiveFlag` — `Course.IsActive`.
- `003_CourseImage` — `Course.ImageId`.
- `004_CourseSlug` — `Course.Slug`.
- `005_AllowedViewers` — `Course.AllowedGroupId` / `Course.AllowedDataViewId`.
- `006_ExternalCourseUrl` — `Course.ExternalCourseUrl`.
- `007_IconCssClass` — `Course.IconCssClass`.

(`Migrations/Configuration.cs` is the EF migrations config; `LearningContext` itself uses a
`NullDatabaseInitializer`, so schema changes flow only through these numbered plugin migrations.)

## Observations

*Noticed while documenting — not a full audit.*

- **Security (low):** Course viewer-restriction logic is split. `Course.PersonCanView` checks
  `AllowedGroup` / `AllowedDataView` membership, while `CustomFilters.PersonCourseInfo` instead
  relies on `IsAuthorized(VIEW, currentPerson)` (Rock category-based security via `ParentAuthority`).
  These are two different gates; confirm the Course Entry / Course Lava blocks consistently enforce
  whichever one is intended so a course can't be viewed through one path but not the other.
- **Improvement:** The `PersonCourseInfo` Lava filter constructs a fresh `RockContext` per call and
  materializes all active courses in the scoped categories, then authorizes them in-memory — fine
  for a single person page, wasteful if looped over many people.
- **Improvement:** Several video/HTML page types disable request validation
  (`ValidateRequestMode.Disabled`) and emit author-supplied HTML/embed markup directly into the page
  (e.g. `YouTubePage`/`VimeoPage` wrap `coursePage.Configuration` in a container literal). That's
  expected for an HTML-authoring tool, but it means page authoring should stay an
  admin/trusted-staff capability — treat the Course Detail/Page Edit blocks' security accordingly.
- **Improvement:** The `Data/LearingContext.cs` filename is misspelled (the class is
  `LearningContext`); harmless but worth tidying. Its doc-comment also still references
  `GroupManagerContext` from a copy/paste.

## Making Changes

- To add a new lesson type, drop a class in `/CoursePages/` that subclasses `CoursePageComponent`
  and is decorated `[Export(typeof(CoursePageComponent))]` + `[ExportMetadata("ComponentName", "…")]`;
  MEF discovers it and `CoursePageContainer` resolves it — no registration step. Follow `HTMLPage`
  (simplest) or `QuizPage` (config + scoring) as templates.
- To change what the `PersonCourseInfo` filter returns, edit `Lava/CustomFilters.cs`; it is
  auto-registered because the whole `CustomFilters` type is passed to `Template.RegisterFilter` in
  `Startup/LoadCustomFilters.cs`.
- New columns/tables belong in a new numbered migration under `/Migrations/` and a matching property
  on the `/Model/` entity — don't hand-edit migrations that have already run.
- Requirement status recomputation lives in `Helpers/CourseRequirementHelper.cs`; the
  `UpdateCourseRequirements` job is just a scheduled wrapper around it.
- The Drag & Drop page type depends on [org.secc.Widgities](../org.secc.Widgities/README.md).
</content>
</invoke>
