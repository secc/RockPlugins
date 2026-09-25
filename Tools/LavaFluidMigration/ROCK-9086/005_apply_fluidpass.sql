-- ROCK9086 pass 2: rows found by the Fluid-verification run on dev (sedev, 2026-09-23) that the scanner rules did not cover; applies to prod too.
-- Same shape as 002_apply.sql: every change is a guarded REPLACE of an exact tag; @DryRun = 1 rolls back.
-- Backups go to dbo._ROCK9086_LavaBackup. Rows 1661 and 62012675 were already backed up by 002, so the backup for
-- them is the pre-002 original and 004_rollback.sql reverts both passes for those two rows.
-- Clear the Rock cache afterwards.
--
-- Row                               Pattern                                       Fluid behaviour today
-- WorkflowActionForm 531 Header      filter inside {% if %}                        parse error, header blank
-- AttributeValue 62012675            filter inside {% if %}                        parse error, SE Online events blank
-- HtmlContent 6548 (block 6438)      bool (| AsBoolean) compared to 'false'        bool == 'false' is TRUE in Fluid -> first accordion panel open
-- HtmlContent 1661 (block 3973)      for-loop over an empty-string attribute       Fluid iterates the string once -> stray <br><p>tel:</p> per campus
-- AttributeValue 16588591 (Badge 38) 'blank' is a reserved Liquid keyword          broken on BOTH engines (DotLiquid emits a syntax-error string into CSS)
--
-- Behaviour change on DotLiquid (531 only): the old condition tested the truthiness of `url`, so InternalApplicationRoot
-- was prepended to every URL, absolute ones included. After this change it is prepended only to '/' and '~' paths.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());

INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT 'WorkflowActionForm', [Id], 'Header', [Header] FROM WorkflowActionForm WHERE [Id] = 531 AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'WorkflowActionForm' AND b.Id = 531);
INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT 'AttributeValue', [Id], 'Value', [Value] FROM AttributeValue WHERE [Id] = 62012675 AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'AttributeValue' AND b.Id = 62012675);
INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT 'HtmlContent', [Id], 'Content', [Content] FROM HtmlContent WHERE [Id] = 6548 AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'HtmlContent' AND b.Id = 6548);
INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT 'HtmlContent', [Id], 'Content', [Content] FROM HtmlContent WHERE [Id] = 1661 AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'HtmlContent' AND b.Id = 1661);
INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue) SELECT 'AttributeValue', [Id], 'Value', [Value] FROM AttributeValue WHERE [Id] = 16588591 AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'AttributeValue' AND b.Id = 16588591);

-- 531: hoist the RegExMatch out of the if
UPDATE WorkflowActionForm SET [Header] = REPLACE([Header], N'{% if url | RegExMatch:''^[/~].*$'' %}', N'{% assign urlIsRelative = url | RegExMatch:''^[/~].*$'' %}{% if urlIsRelative %}')
WHERE [Id] = 531 AND CHARINDEX(N'{% if url | RegExMatch:''^[/~].*$'' %}', [Header]) > 0;
INSERT @log VALUES ('WorkflowActionForm', 531, 'filter-in-if', @@ROWCOUNT);

-- 62012675: hoist the Attribute filter out of the if (Slug is never 'Yes', so the offset class stays always-on as before)
UPDATE AttributeValue SET [Value] = REPLACE([Value], N'{% if campus | Attribute: ''Slug'' != ''Yes'' %}', N'{% assign campusSlug = campus | Attribute:''Slug'' %}{% if campusSlug != ''Yes'' %}')
WHERE [Id] = 62012675 AND CHARINDEX(N'{% if campus | Attribute: ''Slug'' != ''Yes'' %}', [Value]) > 0;
INSERT @log VALUES ('AttributeValue', 62012675, 'filter-in-if', @@ROWCOUNT);

-- 6548: compare the boolean to a boolean (4 occurrences, all replaced)
UPDATE HtmlContent SET [Content] = REPLACE([Content], N'{%- if collapsed == ''false'' -%}', N'{%- if collapsed == false -%}')
WHERE [Id] = 6548 AND [Version] = (SELECT MAX([Version]) FROM HtmlContent WHERE BlockId = 6438) AND CHARINDEX(N'{%- if collapsed == ''false'' -%}', [Content]) > 0;
INSERT @log VALUES ('HtmlContent', 6548, 'bool-vs-string', @@ROWCOUNT);

-- 1661: guard the loop body so an empty attribute renders nothing (both anchors must be present exactly once)
UPDATE HtmlContent SET [Content] = REPLACE(REPLACE([Content],
        N'{% for number in campusNumbers %}', N'{% for number in campusNumbers %}{% if number != '''' %}'),
        N'</a></strong></p>', N'</a></strong></p>{% endif %}')
WHERE [Id] = 1661 AND [Version] = (SELECT MAX([Version]) FROM HtmlContent WHERE BlockId = 3973)
  AND CHARINDEX(N'{% for number in campusNumbers %}', [Content]) > 0
  AND CHARINDEX(N'{% for number in campusNumbers %}', [Content], CHARINDEX(N'{% for number in campusNumbers %}', [Content]) + 1) = 0
  AND CHARINDEX(N'</a></strong></p>', [Content]) > 0
  AND CHARINDEX(N'</a></strong></p>', [Content], CHARINDEX(N'</a></strong></p>', [Content]) + 1) = 0
  AND CHARINDEX(N'{% if number != '''' %}', [Content]) = 0;
INSERT @log VALUES ('HtmlContent', 1661, 'for-over-string', @@ROWCOUNT);

-- Badge 38 DisplayText: 'blank' is a Liquid keyword; rename the variable (1 assign + 5 reads)
UPDATE AttributeValue SET [Value] = REPLACE(REPLACE([Value], N'{% assign blank = ', N'{% assign blankColor = '), N'= blank %}', N'= blankColor %}')
WHERE [Id] = 16588591 AND CHARINDEX(N'{% assign blank = ', [Value]) > 0
  AND (LEN([Value]) - LEN(REPLACE([Value], N'blank', N''))) / LEN(N'blank') = 6;
INSERT @log VALUES ('AttributeValue', 16588591, 'reserved-keyword', @@ROWCOUNT);

SELECT * FROM @log ORDER BY Tbl, Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] <> 1) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
-- a live run must be all-or-nothing: a drifted row means the dry run was not read, so nothing is committed
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] <> 1)
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 2: a guarded update did not match exactly one row; nothing committed.', 1; END

-- verify (inside the transaction so the dry run shows the would-be result)
SELECT 'WorkflowActionForm' Tbl, 531 Id, CHARINDEX(N'{% if url | RegExMatch', [Header]) StillHasOld, CHARINDEX(N'{% if urlIsRelative %}', [Header]) HasNew FROM WorkflowActionForm WHERE Id = 531
UNION ALL SELECT 'AttributeValue', 62012675, CHARINDEX(N'{% if campus | Attribute', [Value]), CHARINDEX(N'{% if campusSlug != ''Yes'' %}', [Value]) FROM AttributeValue WHERE Id = 62012675
UNION ALL SELECT 'HtmlContent', 6548, CHARINDEX(N'collapsed == ''false''', [Content]), CHARINDEX(N'collapsed == false', [Content]) FROM HtmlContent WHERE Id = 6548
UNION ALL SELECT 'HtmlContent', 1661, 0, CHARINDEX(N'{% if number != '''' %}', [Content]) FROM HtmlContent WHERE Id = 1661
UNION ALL SELECT 'AttributeValue', 16588591, CHARINDEX(N'= blank %}', [Value]), CHARINDEX(N'blankColor', [Value]) FROM AttributeValue WHERE Id = 16588591;

IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END
