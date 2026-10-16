-- ROCK9086 pass 5: the View Case page (page 3136, HtmlContent "Case Summary", block 6995).
-- Production logged "(Block: workflow) A value was expected" under Fluid verification. The workflow tag is fine; the parse
-- fails on line 51 of the block, `{% assign wStatus = {{Workflow.Status}} %}` - a {{ }} inside an assign. DotLiquid resolves it,
-- Fluid's expression parser stops at `{{`, and the whole {% workflow %} block renders empty. Fix: the bare property.
-- Verified identical on both engines with LavaProbe (V1/V2). Same shape as 005-007: guarded REPLACE, dry run by default,
-- all-or-nothing live run, backup to dbo._ROCK9086_LavaBackup. Clear the Rock cache afterwards.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;
DECLARE @DryRun bit = 1;
DECLARE @log TABLE (Tbl sysname, Id int, FixRule varchar(40), [Rows] int);
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9086_LavaBackup') IS NULL
    CREATE TABLE dbo._ROCK9086_LavaBackup (Tbl sysname NOT NULL, Id int NOT NULL, Col sysname NOT NULL, OldValue nvarchar(max) NULL, BackedUpAt datetime NOT NULL DEFAULT GETDATE());

INSERT dbo._ROCK9086_LavaBackup (Tbl, Id, Col, OldValue)
SELECT 'HtmlContent', [Id], 'Content', [Content] FROM HtmlContent WHERE [Id] = 10557
  AND NOT EXISTS (SELECT 1 FROM dbo._ROCK9086_LavaBackup b WHERE b.Tbl = 'HtmlContent' AND b.Id = 10557);

UPDATE HtmlContent SET [Content] = REPLACE([Content], N'{% assign wStatus = {{Workflow.Status}} %}', N'{% assign wStatus = Workflow.Status %}')
WHERE [Id] = 10557 AND [Version] = (SELECT MAX([Version]) FROM HtmlContent WHERE BlockId = 6995)
  AND CHARINDEX(N'{% assign wStatus = {{Workflow.Status}} %}', [Content]) > 0;
INSERT @log VALUES ('HtmlContent', 10557, 'mustache-in-assign', @@ROWCOUNT);

SELECT * FROM @log;
IF EXISTS (SELECT 1 FROM @log WHERE [Rows] <> 1) PRINT 'WARNING: some statements did not update exactly one row - review before committing.';
IF @DryRun = 0 AND EXISTS (SELECT 1 FROM @log WHERE [Rows] <> 1)
BEGIN ROLLBACK; THROW 50000, 'ROCK9086 pass 5: a guarded update did not match exactly one row; nothing committed.', 1; END

SELECT Id, CHARINDEX(N'{{Workflow.Status}}', [Content]) StillHasOld, CHARINDEX(N'wStatus = Workflow.Status %}', [Content]) HasNew FROM HtmlContent WHERE Id = 10557;

IF @DryRun = 1 BEGIN ROLLBACK; PRINT 'Dry run - rolled back.'; END ELSE BEGIN COMMIT; PRINT 'Committed. Now clear the Rock cache.'; END
