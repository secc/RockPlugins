/* ROCK-9253 rollback: restore IsActive from the backup taken by 001. Clear Rock cache afterwards. */
SET XACT_ABORT ON;
BEGIN TRAN;
UPDATE dv SET dv.IsActive = b.IsActive, dv.ModifiedDateTime = GETDATE()
FROM DefinedValue dv
JOIN (SELECT Id, IsActive, ROW_NUMBER() OVER (PARTITION BY Id ORDER BY BackedUpAt) rn
      FROM dbo._ROCK9253_DefinedValue_Backup) b ON b.Id = dv.Id AND b.rn = 1;
SELECT dv.Id, dv.Value, dv.IsActive FROM DefinedValue dv WHERE dv.Id IN (SELECT Id FROM dbo._ROCK9253_DefinedValue_Backup);
COMMIT;
