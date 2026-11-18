-- ROCK-9253: undo 001 and 003 (restore route Value/IsActive and the Bulletin
-- items' ExpireDateTime from the backup tables). Clear the Rock cache afterwards.
SET XACT_ABORT ON;
BEGIN TRAN;
UPDATE dv SET dv.Value = b.Value, dv.IsActive = b.IsActive, dv.ModifiedDateTime = GETDATE()
FROM DefinedValue dv
JOIN (SELECT Id, Value, IsActive, ROW_NUMBER() OVER (PARTITION BY Id ORDER BY BackedUpAt) rn
      FROM dbo._ROCK9253_DefinedValue_Backup) b ON b.Id = dv.Id AND b.rn = 1;
IF OBJECT_ID('dbo._ROCK9253_ContentChannelItem_Backup') IS NOT NULL
    UPDATE i SET i.ExpireDateTime = b.ExpireDateTime, i.ModifiedDateTime = GETDATE()
    FROM ContentChannelItem i
    JOIN (SELECT Id, ExpireDateTime, ROW_NUMBER() OVER (PARTITION BY Id ORDER BY BackedUpAt) rn
          FROM dbo._ROCK9253_ContentChannelItem_Backup) b ON b.Id = i.Id AND b.rn = 1;
SELECT dv.Id, dv.Value, dv.IsActive FROM DefinedValue dv WHERE dv.Id IN (SELECT Id FROM dbo._ROCK9253_DefinedValue_Backup);
COMMIT;
