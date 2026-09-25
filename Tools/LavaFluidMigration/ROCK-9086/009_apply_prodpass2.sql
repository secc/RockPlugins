-- ROCK9086 pass 6: more rows production reported under Fluid verification (2026-09-25, after 007/008 and the share-button fix).
-- Same shape as 005-008: guarded REPLACE per row, @DryRun = 1 rolls back, live run is all-or-nothing, backups to
-- dbo._ROCK9086_LavaBackup. Clear the Rock cache afterwards.
--
-- Row                                   Where                                      Pattern                                   Fluid today
-- AttributeValue 359962155 (block 8820)  /groups/homegroups Redirect block Url      `{% if ((a and b) or (c) ...) %}` and      "Invalid 'if' tag" - parens in if;
--                                                                                   `Replace:' ':''` (colon between args)      "End of tag '%}' was expected"
-- AttributeValue 449779791 (action 76944) RSVP Weekend Follow-up: Detect tapback   RegExMatch:'...\S{1,20} ... [\s\S]*...'    "End of tag" - \s / \S escapes in a
-- AttributeValue 449129643 (action 76890) RSVP Weekend Follow-up: Build Note       RegExReplace:' to [..][\s\S]*$',''         quoted string are rejected by Fluid
-- AttributeValue 77078364, 81148262,     Connection-card intake workflows: Phone   `| Slice: 3,15` on '+1 (xxx) xxx-xxxx'     Fluid reads the 2nd arg as an end
--   113010373, 163028710, 208121354,     (6 rows) and Update Connection Card                                                   index: drops the last two digits
--   374780705, 77083833                  Person: Get Phone Number (1 row)
--
-- Fixes: the redirect condition is precomputed into `hasFilter` with one plain if per clause (Fluid rejects parens); the regex
-- patterns move into a capture (no quoted-string escapes); `Slice: 3,15` -> `Slice: 3,100` (to the end on both engines).
-- Every rewrite verified with LavaProbe (cases P0-P9, Y2-Y6, X7).
--
-- BEHAVIOUR NOTE (redirect row): DotLiquid does not evaluate the parenthesised condition as written - with campus unset (or
-- a real campus absent), ?type= / ?meet= / ?handicap= / ?otc= alone fall through to the campus-only branch (LavaProbe
-- P4-P7 OLD = F). The rewrite does what the template was written to do, so those filters now carry through to /allgroups.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
DECLARE @hit TABLE (Id int);
DECLARE @lq nchar(1) = NCHAR(8220), @rq nchar(1) = NCHAR(8221);   -- curly quotes inside the RSVP regex character classes
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());

INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue)
SELECT 'AttributeValue', av.[Id], 'Value', av.[Value] FROM AttributeValue av
WHERE av.[Id] IN (359962155, 449779791, 449129643, 77078364, 81148262, 113010373, 163028710, 208121354, 374780705, 77083833)
  AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'AttributeValue' AND b.Id = av.[Id]);

-- Home Groups redirect: parens-if -> precomputed hasFilter
DECLARE @oldIf nvarchar(400) = N'{% if ((campus != null and campus != '''' and campus != ''all'') or (ministry != null and ministry != '''') or (meets != null and meets != '''') or (handicap != null and handicap != '''') or (otc != null and otc != '''')) %}';
DECLARE @newIf nvarchar(800) = N'{% assign hasFilter = false %}'
    + N'{% if campus != null and campus != '''' and campus != ''all'' %}{% assign hasFilter = true %}{% endif %}'
    + N'{% if ministry != null and ministry != '''' %}{% assign hasFilter = true %}{% endif %}'
    + N'{% if meets != null and meets != '''' %}{% assign hasFilter = true %}{% endif %}'
    + N'{% if handicap != null and handicap != '''' %}{% assign hasFilter = true %}{% endif %}'
    + N'{% if otc != null and otc != '''' %}{% assign hasFilter = true %}{% endif %}'
    + N'{% if hasFilter %}';
UPDATE AttributeValue SET [Value] = REPLACE(REPLACE([Value], @oldIf, @newIf), N'| Replace:'' '':''''%}', N'| Replace:'' '',''''%}')
WHERE [Id] = 359962155 AND CHARINDEX(@oldIf, [Value]) > 0 AND CHARINDEX(N'| Replace:'' '':''''%}', [Value]) > 0;
INSERT @log VALUES ('AttributeValue', 359962155, 'parens-if+replace-colon', @@ROWCOUNT);

-- RSVP: Detect tapback reaction
DECLARE @oldG nvarchar(400) = N'{%- assign generic = msg | RegExMatch:''^(Reacted )?\S{1,20} to [' + @lq + N'"][\s\S]*[' + @rq + N'"]\s*$'' -%}';
DECLARE @newG nvarchar(400) = N'{%- capture reGeneric -%}^(Reacted )?\S{1,20} to [' + @lq + N'"][\s\S]*[' + @rq + N'"]\s*${%- endcapture -%}'
    + N'{%- assign generic = msg | RegExMatch:reGeneric -%}';
UPDATE AttributeValue SET [Value] = REPLACE([Value], @oldG, @newG)
WHERE [Id] = 449779791 AND CHARINDEX(@oldG, [Value]) > 0;
INSERT @log VALUES ('AttributeValue', 449779791, 'regex-escape-capture', @@ROWCOUNT);

-- RSVP: Build Note (the capture keeps the leading space of ' to ', so its open tag has no right trim)
DECLARE @oldE nvarchar(400) = N'| RegExReplace:''^Reacted '','''' | RegExReplace:'' to [' + @lq + N'"][\s\S]*$'','''' %}';
DECLARE @newE nvarchar(400) = N'| RegExReplace:''^Reacted '','''' | RegExReplace:reTail,'''' %}';
DECLARE @oldEStart nvarchar(100) = N'{%- assign emoji = Workflow | Attribute:''IncomingMessage''';
DECLARE @newEStart nvarchar(200) = N'{%- capture reTail %} to [' + @lq + N'"][\s\S]*${% endcapture -%}' + @oldEStart;
UPDATE AttributeValue SET [Value] = REPLACE(REPLACE([Value], @oldE, @newE), @oldEStart, @newEStart)
WHERE [Id] = 449129643 AND CHARINDEX(@oldE, [Value]) > 0 AND CHARINDEX(@oldEStart, [Value]) > 0
  AND (LEN([Value]) - LEN(REPLACE([Value], @oldEStart, N''))) / LEN(@oldEStart) = 1;
INSERT @log VALUES ('AttributeValue', 449129643, 'regex-escape-capture', @@ROWCOUNT);

-- Phone Slice: 3,15 -> 3,100
UPDATE AttributeValue SET [Value] = REPLACE(REPLACE([Value], N'| Slice: 3,15}}', N'| Slice: 3,100}}'), N'| Slice: 3,15 }}', N'| Slice: 3,100 }}')
OUTPUT inserted.[Id] INTO @hit
WHERE [Id] IN (77078364, 81148262, 113010373, 163028710, 208121354, 374780705, 77083833)
  AND (CHARINDEX(N'| Slice: 3,15}}', [Value]) > 0 OR CHARINDEX(N'| Slice: 3,15 }}', [Value]) > 0);
INSERT @log SELECT 'AttributeValue', x.Id, 'slice-length', (SELECT COUNT(*) FROM @hit h WHERE h.Id = x.Id)
FROM (VALUES (77078364), (81148262), (113010373), (163028710), (208121354), (374780705), (77083833)) x(Id);

-- The RSVP workflow was built on production after dev was last copied from it: a row that does not exist at all is
-- logged as -1 (absent) and does not block the commit; a row that exists but did not match still does.
UPDATE l SET [Rows] = -1 FROM @log l WHERE l.[Rows] = 0 AND NOT EXISTS (SELECT 1 FROM AttributeValue av WHERE av.[Id] = l.Id);

SELECT * FROM @log ORDER BY Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1)) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] NOT IN (1, -1))
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 6: a guarded update did not match exactly one row; nothing committed.', 1; END

SELECT av.Id, CHARINDEX(N'{% if ((', av.Value) OldParens, CHARINDEX(N'Replace:'' '':''''', av.Value) OldColon, CHARINDEX(N'\S{1,20}', av.Value) - CHARINDEX(N'reGeneric -%}', av.Value) GenericInCapture,
       CHARINDEX(N'RegExReplace:'' to', av.Value) OldTail, CHARINDEX(N'Slice: 3,15', av.Value) OldSlice,
       CHARINDEX(N'{% if hasFilter %}', av.Value) NewIf, CHARINDEX(N'RegExMatch:reGeneric', av.Value) NewGeneric, CHARINDEX(N'RegExReplace:reTail', av.Value) NewTail, CHARINDEX(N'Slice: 3,100', av.Value) NewSlice
FROM AttributeValue av WHERE av.Id IN (359962155, 449779791, 449129643, 77078364, 81148262, 113010373, 163028710, 208121354, 374780705, 77083833) ORDER BY av.Id;

IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END
