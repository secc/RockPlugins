-- ROCK9086 rollback: restore the original text of every touched row from the backup table.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; -- filtered indexes on AttributeValue need QUOTED_IDENTIFIER ON (SSMS default; sqlcmd/DBeaver may not)
BEGIN TRAN;
UPDATE t SET t.[DefaultValue] = b.OldValue FROM Attribute t JOIN dbo._ROCK9086_LavaBackup b ON b.Tbl = 'Attribute' AND b.Col = 'DefaultValue' AND b.Id = t.[Id]; PRINT 'Attribute: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Value] = b.OldValue FROM AttributeValue t JOIN dbo._ROCK9086_LavaBackup b ON b.Tbl = 'AttributeValue' AND b.Col = 'Value' AND b.Id = t.[Id]; PRINT 'AttributeValue: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Content] = b.OldValue FROM HtmlContent t JOIN dbo._ROCK9086_LavaBackup b ON b.Tbl = 'HtmlContent' AND b.Col = 'Content' AND b.Id = t.[Id]; PRINT 'HtmlContent: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Markup] = b.OldValue FROM LavaShortcode t JOIN dbo._ROCK9086_LavaBackup b ON b.Tbl = 'LavaShortcode' AND b.Col = 'Markup' AND b.Id = t.[Id]; PRINT 'LavaShortcode: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Header] = b.OldValue FROM WorkflowActionForm t JOIN dbo._ROCK9086_LavaBackup b ON b.Tbl = 'WorkflowActionForm' AND b.Col = 'Header' AND b.Id = t.[Id]; PRINT 'WorkflowActionForm: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[PostHtml] = b.OldValue FROM WorkflowActionFormAttribute t JOIN dbo._ROCK9086_LavaBackup b ON b.Tbl = 'WorkflowActionFormAttribute' AND b.Col = 'PostHtml' AND b.Id = t.[Id]; PRINT 'WorkflowActionFormAttribute: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
COMMIT; -- then clear the Rock cache
