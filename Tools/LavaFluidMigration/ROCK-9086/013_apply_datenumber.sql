-- ROCK9086 pass 10: dates stored as 10-14 digit numbers (Date:'yyMMddHHmm' | AsDouble and friends).
-- Rock's Fluid bridge (Rock.Lava.Fluid.FluidExtensions.ToRealObjectValue, line 247) returns (int) for every whole-number
-- decimal, so 2609251846 (today as yyMMddHHmm) throws "Value was either too large or too small for an Int32" as soon as a
-- shortcode collects merge fields or a Rock filter (Plus, etc.) receives it. Dates before 2021-06 fit in Int32, which is why
-- this only started recently. Fix: a decimal point after the day (yyMMdd.HHmm, yyyyMMdd.HHmm[ss]) - ordering and equality are
-- unchanged, the whole part stays at 6-8 digits. Hard-coded yyyyMMddHHmm literals get the same point. LavaProbe U1-U4, V1-V4.
-- HtmlContent 4257 used AsInteger on yyMMddHHmm, which overflows on DotLiquid as well (broken since mid-2021): now AsDouble.
-- HtmlContent 10546 keeps its 12-hour 'hh' (a pre-existing quirk; changing it would change behaviour).
-- Same shape as 005-012: guarded REPLACE per row, @DryRun = 1 rolls back, live run is all-or-nothing, backups to
-- dbo._ROCK9086_LavaBackup, rows absent from the target database log -1. Clear the Rock cache afterwards.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());

INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT 'HtmlContent', x.[Id], 'Content', x.[Content] FROM HtmlContent x WHERE x.[Id] IN (10546, 4109, 4257, 4621, 469, 6434, 6544)
  AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'HtmlContent' AND b.Id = x.[Id] AND b.Col = 'Content');

UPDATE HtmlContent SET [Content] = REPLACE(REPLACE(REPLACE([Content], N'{% assign expire = item.ExpireDateTime | Date:''yyyyMMddhhmmss'' | AsDouble %}', N'{% assign expire = item.ExpireDateTime | Date:''yyyyMMdd.hhmmss'' | AsDouble %}'), N'{% assign now = ''Now'' | Date:''yyyyMMddhhmmss'' | AsDouble %}', N'{% assign now = ''Now'' | Date:''yyyyMMdd.hhmmss'' | AsDouble %}'), N'{% assign start = item.StartDateTime | Date:''yyyyMMddhhmmss'' | AsDouble %}', N'{% assign start = item.StartDateTime | Date:''yyyyMMdd.hhmmss'' | AsDouble %}')
WHERE [Id] = 10546
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign expire = item.ExpireDateTime | Date:''yyyyMMddhhmmss'' | AsDouble %}', N''))) / LEN(N'{% assign expire = item.ExpireDateTime | Date:''yyyyMMddhhmmss'' | AsDouble %}') = 2
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign now = ''Now'' | Date:''yyyyMMddhhmmss'' | AsDouble %}', N''))) / LEN(N'{% assign now = ''Now'' | Date:''yyyyMMddhhmmss'' | AsDouble %}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign start = item.StartDateTime | Date:''yyyyMMddhhmmss'' | AsDouble %}', N''))) / LEN(N'{% assign start = item.StartDateTime | Date:''yyyyMMddhhmmss'' | AsDouble %}') = 2
  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);
INSERT @log VALUES ('HtmlContent', 10546, 'date-number', @@ROWCOUNT);
UPDATE HtmlContent SET [Content] = REPLACE(REPLACE([Content], N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsDouble %}', N'{% assign currentDateTime = ''Now'' | Date:''yyMMdd.HHmm'' | AsDouble %}'), N'{% assign itemStartDateTime = SEKidsItem.StartDateTime | Date:''yyMMddHHmm'' | AsDouble %}', N'{% assign itemStartDateTime = SEKidsItem.StartDateTime | Date:''yyMMdd.HHmm'' | AsDouble %}')
WHERE [Id] = 4109
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsDouble %}', N''))) / LEN(N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsDouble %}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign itemStartDateTime = SEKidsItem.StartDateTime | Date:''yyMMddHHmm'' | AsDouble %}', N''))) / LEN(N'{% assign itemStartDateTime = SEKidsItem.StartDateTime | Date:''yyMMddHHmm'' | AsDouble %}') = 1
  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);
INSERT @log VALUES ('HtmlContent', 4109, 'date-number', @@ROWCOUNT);
UPDATE HtmlContent SET [Content] = REPLACE(REPLACE([Content], N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsInteger %}', N'{% assign currentDateTime = ''Now'' | Date:''yyMMdd.HHmm'' | AsDouble %}'), N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMddHHmm'' | AsInteger %}', N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMdd.HHmm'' | AsDouble %}')
WHERE [Id] = 4257
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsInteger %}', N''))) / LEN(N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsInteger %}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMddHHmm'' | AsInteger %}', N''))) / LEN(N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMddHHmm'' | AsInteger %}') = 1
  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);
INSERT @log VALUES ('HtmlContent', 4257, 'date-number', @@ROWCOUNT);
UPDATE HtmlContent SET [Content] = REPLACE(REPLACE(REPLACE([Content], N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsDouble %}', N'{% assign currentDateTime = ''Now'' | Date:''yyMMdd.HHmm'' | AsDouble %}'), N'{% assign itemExpireDateTime = item.ExpireDateTime | Date:''yyMMddHHmm'' | AsDouble %}', N'{% assign itemExpireDateTime = item.ExpireDateTime | Date:''yyMMdd.HHmm'' | AsDouble %}'), N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMddHHmm'' | AsDouble %}', N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMdd.HHmm'' | AsDouble %}')
WHERE [Id] = 4621
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsDouble %}', N''))) / LEN(N'{% assign currentDateTime = ''Now'' | Date:''yyMMddHHmm'' | AsDouble %}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign itemExpireDateTime = item.ExpireDateTime | Date:''yyMMddHHmm'' | AsDouble %}', N''))) / LEN(N'{% assign itemExpireDateTime = item.ExpireDateTime | Date:''yyMMddHHmm'' | AsDouble %}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMddHHmm'' | AsDouble %}', N''))) / LEN(N'{% assign itemStartDateTime = item.StartDateTime | Date:''yyMMddHHmm'' | AsDouble %}') = 1
  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);
INSERT @log VALUES ('HtmlContent', 4621, 'date-number', @@ROWCOUNT);
UPDATE HtmlContent SET [Content] = REPLACE(REPLACE([Content], N'{% assign currentDate = ''Now'' | Date:''yyyyMMddHHmm'' | AsDouble %}', N'{% assign currentDate = ''Now'' | Date:''yyyyMMdd.HHmm'' | AsDouble %}'), N'{% assign financialAidExpiration = ''201805012359'' | AsDouble %}', N'{% assign financialAidExpiration = ''20180501.2359'' | AsDouble %}')
WHERE [Id] = 469
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign currentDate = ''Now'' | Date:''yyyyMMddHHmm'' | AsDouble %}', N''))) / LEN(N'{% assign currentDate = ''Now'' | Date:''yyyyMMddHHmm'' | AsDouble %}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{% assign financialAidExpiration = ''201805012359'' | AsDouble %}', N''))) / LEN(N'{% assign financialAidExpiration = ''201805012359'' | AsDouble %}') = 1
  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);
INSERT @log VALUES ('HtmlContent', 469, 'date-number', @@ROWCOUNT);
UPDATE HtmlContent SET [Content] = REPLACE(REPLACE([Content], N'{%- assign endTime = ''202601041115'' | AsDouble -%}', N'{%- assign endTime = ''20260104.1115'' | AsDouble -%}'), N'{%- assign now = ''Now'' | Date:''yyyyMMddHHmm'' | AsDouble -%}', N'{%- assign now = ''Now'' | Date:''yyyyMMdd.HHmm'' | AsDouble -%}')
WHERE [Id] = 6434
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{%- assign endTime = ''202601041115'' | AsDouble -%}', N''))) / LEN(N'{%- assign endTime = ''202601041115'' | AsDouble -%}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{%- assign now = ''Now'' | Date:''yyyyMMddHHmm'' | AsDouble -%}', N''))) / LEN(N'{%- assign now = ''Now'' | Date:''yyyyMMddHHmm'' | AsDouble -%}') = 1
  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);
INSERT @log VALUES ('HtmlContent', 6434, 'date-number', @@ROWCOUNT);
UPDATE HtmlContent SET [Content] = REPLACE(REPLACE([Content], N'{%- assign nowDateTime = ''Now'' | Date:''yyyyMMddHHmmss'' | AsDouble -%}', N'{%- assign nowDateTime = ''Now'' | Date:''yyyyMMdd.HHmmss'' | AsDouble -%}'), N'{%- assign startDateTime = issue.StartDateTime | Date:''yyyyMMddHHmmss'' | AsDouble -%}', N'{%- assign startDateTime = issue.StartDateTime | Date:''yyyyMMdd.HHmmss'' | AsDouble -%}')
WHERE [Id] = 6544
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{%- assign nowDateTime = ''Now'' | Date:''yyyyMMddHHmmss'' | AsDouble -%}', N''))) / LEN(N'{%- assign nowDateTime = ''Now'' | Date:''yyyyMMddHHmmss'' | AsDouble -%}') = 1
  AND (LEN([Content]) - LEN(REPLACE([Content], N'{%- assign startDateTime = issue.StartDateTime | Date:''yyyyMMddHHmmss'' | AsDouble -%}', N''))) / LEN(N'{%- assign startDateTime = issue.StartDateTime | Date:''yyyyMMddHHmmss'' | AsDouble -%}') = 1
  AND [Version] = (SELECT MAX([Version]) FROM HtmlContent h2 WHERE h2.BlockId = HtmlContent.BlockId);
INSERT @log VALUES ('HtmlContent', 6544, 'date-number', @@ROWCOUNT);

UPDATE l SET [Rows] = -1 FROM @log l WHERE l.[Rows] = 0 AND NOT EXISTS (SELECT 1 FROM HtmlContent x WHERE x.Id = l.Id);
SELECT * FROM @log ORDER BY Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1)) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1))
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 10: a guarded update did not match exactly one row; nothing committed.', 1; END

SELECT h.Id, (LEN(h.Content) - LEN(REPLACE(h.Content, N'MMddHHmm'' |', N''))) / 11 + (LEN(h.Content) - LEN(REPLACE(h.Content, N'MMddhhmmss'' |', N''))) / 13 StillOld,
       (LEN(h.Content) - LEN(REPLACE(h.Content, N'MMdd.', N''))) / 5 NewFormats
FROM HtmlContent h WHERE h.Id IN (10546, 4109, 4257, 4621, 469, 6434, 6544) ORDER BY h.Id;

IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END
