# org.secc.MetricsDigest

> A single scheduled job that emails a per-campus "metric entry progress" digest, showing how many of the selected metrics have values entered for a date range.

## Overview

MetricsDigest is a one-class Rock plugin: a Quartz scheduled job that audits **metric data entry**.
For each configured metric category it counts, per campus (and per service schedule, when the metric
is partitioned by schedule), how many metrics should have values for the chosen date range versus how
many actually do. It rolls those counts into Lava merge fields and emails them via a System
Communication to a notification group — so staff can see which campuses are behind on entering their
weekly metrics.

> **Scope note:** only metrics whose partitions include a **Campus** partition are counted. A selected
> metric with no campus partition is added to the `Metrics` merge field but contributes nothing to the
> per-campus tallies. Schedule sub-counts are only computed when the metric *also* has a Schedule
> partition.

## Project Info

- **Project file:** `org.secc.MetricsDigest.csproj`
- **Root namespace:** `org.secc.MetricsDigest` (note: the job's actual C# namespace is `org.secc.Jobs`)
- **Target framework:** .NET Framework 4.7.2
- **Deploys to:** `RockWeb/bin/` (assembly + `.pdb`, via the PostBuildEvent `xcopy`)

## Project Layout

```
MetricsDigest.cs        The job (IJob) + the MetricCount Lava Drop
/Properties/            AssemblyInfo
```

## Components

### Jobs

A single Quartz `IJob` (`[DisallowConcurrentExecution]`), wired up as a Rock Job and configured
entirely through job attributes (read from the `JobDataMap`).

| Job (class) | Purpose |
|-------------|---------|
| `MetricsDigest` | Counts entered vs. expected metric values per campus/schedule for a date range and emails the digest to a notification group. |

### Job Settings

Rock attributes declared on the class; values are read from the `JobDataMap` by key. The **key** is the
display name with spaces removed (Rock's default), which is what `dataMap.GetString(...)` uses.

| Attribute (display name) | JobDataMap key | Type | Required | Notes |
|--------------------------|----------------|------|----------|-------|
| **Metrics** | `Metrics` | `MetricCategoriesField` | Yes | Metric categories to include; expanded to metric/category Guid pairs. |
| **Date Range** | `DateRange` | `SlidingDateRangeField` | Yes | Date range over which metric entries are reviewed. |
| **Schedule Categories** | `ScheduleCategories` | `CategoryField` (`Rock.Model.Schedule`) | Yes | Schedule categories used to build the per-service list (needs Campus Attribute to filter by campus). |
| **Campus Attribute** | `CampusAttribute` | `AttributeField` (Schedule entity) | No | Schedule attribute used to filter schedules by campus. |
| **Notification Group** | `NotificationGroup` | `GroupField` | Yes | Group whose members receive the digest email. |
| **Email** | `Email` | `SystemCommunicationField` | Yes | System Communication template sent to each member. The job throws if unset. |

### Models / Lava Drops

| Type | Purpose |
|------|---------|
| `MetricCount` (`DotLiquid.Drop`) | Per-campus tally exposed to Lava: `Campus`, `TotalEntered`, `TotalMetrics`. |

Merge fields passed to the email template: `MetricCounts` (list of `MetricCount`), `Metrics`
(the resolved `Metric` list), `DateRange` (string), and `LastRunDate` (the job's last successful run).

## Dependencies & Integrations

- **Rock:** Quartz job framework (`IJob`, `JobDataMap`), `RockContext` and Rock services
  (`MetricService`, `CategoryService`, `GroupService`, `ScheduleService`, `AttributeValueService`,
  `ServiceJobService`), `CampusCache` / `CategoryCache`, the metric partition model, `RockEmailMessage`
  + System Communications, and Lava (`LavaHelper`, `SlidingDateRangePicker`, `InetCalendarHelper`).
- **Third-party:** DotLiquid (`Drop`), Ical.Net / DDay.iCal / NodaTime (transitive, via schedule
  occurrence calculation), Entity Framework.

## Observations

*Noticed while documenting — not a full audit.*

- **Improvement:** The job class lives in namespace `org.secc.Jobs` while the assembly/root namespace
  is `org.secc.MetricsDigest`. Functionally harmless (the [org.secc.Jobs](../org.secc.Jobs/README.md)
  plugin uses the same namespace but has no `MetricsDigest` type, so the two coexist), but the mismatch
  is surprising and makes the class harder to locate from its assembly name.
- **Improvement:** `AssemblyInfo.cs` is mis-scaffolded — title/product read `org.secc.ConnectionsDigest`
  and the company/copyright are the default `"Microsoft"` / `"Copyright © Microsoft 2017"` rather than
  Southeast Christian Church. Cosmetic, but the title is plainly wrong for this assembly.
- **Improvement:** `metric.MetricValues` and `metric.MetricPartitions` are walked in memory rather than
  queried, and `metricCounts.Where(...).FirstOrDefault()` is re-evaluated repeatedly inside nested
  loops over campuses/services. For a large metric history this loads and scans a lot of rows; a
  filtered query (or a dictionary keyed by campus) would scale better.
- **Improvement:** `int jobId = Convert.ToInt16(context.JobDetail.Description)` parses the job Id out of
  the job *Description* field — fragile if that field is ever edited to hold real description text. The
  Id is normally available more directly from the execution context.
- **Improvement:** The whole compute-and-send block is gated on `notificationGroup != null &&
  metricCategories.Count > 0 && dateRange.Start.HasValue && dateRange.End.HasValue`. If any of those
  fail (no group, no categories, open-ended date range) the job sends **nothing** and silently returns
  `"Sent 0 metric entry digest emails."` with no warning — a misconfiguration looks like a successful
  run. `GroupMember.Person.Email` is also used without a null/blank check, so members with no email
  address can produce empty recipients.

## Making Changes

- All behavior lives in `MetricsDigest.cs`. To change *what* is counted, edit the per-metric/partition
  loop in `Execute`; to change the per-service schedule resolution, edit the private `GetServices`
  helper.
- To add a configuration option, add a Rock `*Field` attribute on the class and read it from
  `dataMap.GetString("<Key>")` — keep the attribute key and the lookup string in sync.
- Email content/layout is driven by the configured System Communication template and the merge fields
  above (`MetricCounts`, `Metrics`, `DateRange`, `LastRunDate`); edit the template in Rock, not here.
- Related scheduled jobs live in [org.secc.Jobs](../org.secc.Jobs/README.md).
