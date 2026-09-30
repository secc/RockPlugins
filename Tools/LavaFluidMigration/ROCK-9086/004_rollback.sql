-- ROCK9086 rollback: restore the original text of every touched row from the backup table.
-- A row can have more than one backup (passes before 014 guarded with NOT EXISTS, 014 did not), so each row is restored
-- from its EARLIEST backup, which is the value before any ROCK9086 pass touched it.
SET NOCOUNT ON; SET XACT_ABORT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; -- filtered indexes on AttributeValue need QUOTED_IDENTIFIER ON (SSMS default; sqlcmd/DBeaver may not)
BEGIN TRAN;
IF OBJECT_ID('tempdb..#orig') IS NOT NULL DROP TABLE #orig;
SELECT Tbl, Id, Col, OldValue INTO #orig FROM (
    SELECT Tbl, Id, Col, OldValue, ROW_NUMBER() OVER (PARTITION BY Tbl, Id, Col ORDER BY BackedUpAt) rn FROM dbo._ROCK9086_LavaBackup
) b WHERE rn = 1;
UPDATE t SET t.[DefaultValue] = b.OldValue FROM Attribute t JOIN #orig b ON b.Tbl = 'Attribute' AND b.Col = 'DefaultValue' AND b.Id = t.[Id]; PRINT 'Attribute.DefaultValue: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Value] = b.OldValue FROM AttributeValue t JOIN #orig b ON b.Tbl = 'AttributeValue' AND b.Col = 'Value' AND b.Id = t.[Id]; PRINT 'AttributeValue.Value: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Content] = b.OldValue FROM HtmlContent t JOIN #orig b ON b.Tbl = 'HtmlContent' AND b.Col = 'Content' AND b.Id = t.[Id]; PRINT 'HtmlContent.Content: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[PreHtml] = b.OldValue FROM Block t JOIN #orig b ON b.Tbl = 'Block' AND b.Col = 'PreHtml' AND b.Id = t.[Id]; PRINT 'Block.PreHtml: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Markup] = b.OldValue FROM LavaShortcode t JOIN #orig b ON b.Tbl = 'LavaShortcode' AND b.Col = 'Markup' AND b.Id = t.[Id]; PRINT 'LavaShortcode.Markup: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Selection] = b.OldValue FROM ReportField t JOIN #orig b ON b.Tbl = 'ReportField' AND b.Col = 'Selection' AND b.Id = t.[Id]; PRINT 'ReportField.Selection: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Body] = b.OldValue FROM SystemCommunication t JOIN #orig b ON b.Tbl = 'SystemCommunication' AND b.Col = 'Body' AND b.Id = t.[Id]; PRINT 'SystemCommunication.Body: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Header] = b.OldValue FROM WorkflowActionForm t JOIN #orig b ON b.Tbl = 'WorkflowActionForm' AND b.Col = 'Header' AND b.Id = t.[Id]; PRINT 'WorkflowActionForm.Header: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[Footer] = b.OldValue FROM WorkflowActionForm t JOIN #orig b ON b.Tbl = 'WorkflowActionForm' AND b.Col = 'Footer' AND b.Id = t.[Id]; PRINT 'WorkflowActionForm.Footer: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[PostHtml] = b.OldValue FROM WorkflowActionFormAttribute t JOIN #orig b ON b.Tbl = 'WorkflowActionFormAttribute' AND b.Col = 'PostHtml' AND b.Id = t.[Id]; PRINT 'WorkflowActionFormAttribute.PostHtml: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[NoActionMessage] = b.OldValue FROM WorkflowType t JOIN #orig b ON b.Tbl = 'WorkflowType' AND b.Col = 'NoActionMessage' AND b.Id = t.[Id]; PRINT 'WorkflowType.NoActionMessage: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
UPDATE t SET t.[SummaryViewText] = b.OldValue FROM WorkflowType t JOIN #orig b ON b.Tbl = 'WorkflowType' AND b.Col = 'SummaryViewText' AND b.Id = t.[Id]; PRINT 'WorkflowType.SummaryViewText: ' + CAST(@@ROWCOUNT AS varchar) + ' rows restored';
-- a backup row no statement above restores is a table/column this script does not know yet: stop instead of half-restoring
IF EXISTS (SELECT 1 FROM #orig WHERE Tbl + '.' + Col NOT IN ('Attribute.DefaultValue', 'AttributeValue.Value', 'Block.PreHtml', 'HtmlContent.Content',
    'LavaShortcode.Markup', 'ReportField.Selection', 'SystemCommunication.Body', 'WorkflowActionForm.Header', 'WorkflowActionForm.Footer',
    'WorkflowActionFormAttribute.PostHtml', 'WorkflowType.NoActionMessage', 'WorkflowType.SummaryViewText'))
    THROW 50000, 'Backup table has a Tbl/Col this rollback does not handle; add it before running.', 1;
COMMIT; -- then clear the Rock cache
