-- ROCK9086 pass 4: rows surfaced by production itself once its engine was set to FluidVerification (2026-09-25).
-- Same shape as 005/006: guarded REPLACE per row, @DryRun = 1 rolls back, live run is all-or-nothing, backups to
-- dbo._ROCK9086_LavaBackup. Clear the Rock cache afterwards.
--
-- Row                                  Page                         Pattern                                  Fluid today
-- AttributeValue 3241 (block 683)       /MyDashboard (page 418)      `>= aYearAgo  AND ... Status = 'Active'`  parse error - uppercase AND, single =
-- AttributeValue 54526705 (block 4494)  onepoint /documents (1968)   `{% if documentCategory == {{ctgyName}} %}` parse error - {{ }} inside if
-- AttributeValue 328839027 (block 8795) /groups/oncampus (3242)      `Replace:' ':''` (colon between args)     "End of tag '%}' was expected"
-- AttributeValue 361818241 (block 8848) /groups/oncampus/{c}/{m} (3246) same                                   same
-- Every rewrite rendered identically on Fluid and RockLiquid with LavaProbe (cases R2/R4/R6).
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());

INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue)
SELECT 'AttributeValue', [Id], 'Value', [Value] FROM AttributeValue av WHERE av.[Id] IN (3241, 54526705, 328839027, 361818241)
  AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'AttributeValue' AND b.Id = av.[Id]);

-- MyDashboard: `AND` -> `and`, `Status = 'Active'` -> `Status == 'Active'`
UPDATE AttributeValue SET [Value] = REPLACE([Value],
    N'{% if action.Activity.Workflow.CreatedDateTime >= aYearAgo  AND action.Activity.Workflow.Status = ''Active'' %}',
    N'{% if action.Activity.Workflow.CreatedDateTime >= aYearAgo and action.Activity.Workflow.Status == ''Active'' %}')
WHERE [Id] = 3241 AND CHARINDEX(N'{% if action.Activity.Workflow.CreatedDateTime >= aYearAgo  AND action.Activity.Workflow.Status = ''Active'' %}', [Value]) > 0;
INSERT @log VALUES ('AttributeValue', 3241, 'upper-AND-single-eq', @@ROWCOUNT);

-- Portal documents: bare variable inside the if
UPDATE AttributeValue SET [Value] = REPLACE([Value], N'{% if documentCategory == {{ctgyName}} %}', N'{% if documentCategory == ctgyName %}')
WHERE [Id] = 54526705 AND CHARINDEX(N'{% if documentCategory == {{ctgyName}} %}', [Value]) > 0;
INSERT @log VALUES ('AttributeValue', 54526705, 'mustache-in-if', @@ROWCOUNT);

-- Redirect block URLs: filter arguments are comma-separated
UPDATE AttributeValue SET [Value] = REPLACE([Value], N'| Replace:'' '':''''%}', N'| Replace:'' '',''''%}')
WHERE [Id] IN (328839027, 361818241) AND CHARINDEX(N'| Replace:'' '':''''%}', [Value]) > 0;
DECLARE @rc int = @@ROWCOUNT;
INSERT @log VALUES ('AttributeValue', 328839027, 'replace-colon-arg', CASE WHEN @rc >= 1 THEN 1 ELSE 0 END);
INSERT @log VALUES ('AttributeValue', 361818241, 'replace-colon-arg', CASE WHEN @rc = 2 THEN 1 ELSE 0 END);

SELECT * FROM @log ORDER BY Id;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] <> 1) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] <> 1)
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 4: a guarded update did not match exactly one row; nothing committed.', 1; END

SELECT av.Id, CHARINDEX(N' AND ', av.Value) OldAND, CHARINDEX(N'{{ctgyName}}', av.Value) OldMustache, CHARINDEX(N'Replace:'' '':''''', av.Value) OldColon,
       CHARINDEX(N'>= aYearAgo and', av.Value) NewAnd, CHARINDEX(N'== ctgyName', av.Value) NewBare, CHARINDEX(N'Replace:'' '',''''', av.Value) NewComma
FROM AttributeValue av WHERE av.Id IN (3241, 54526705, 328839027, 361818241) ORDER BY av.Id;

IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END
