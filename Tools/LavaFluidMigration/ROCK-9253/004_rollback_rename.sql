/* ROCK-9253: undo 003 (restore original route values from the 001 backup). Clear Rock cache afterwards.
   To also re-activate, run 002_rollback.sql. */
SET XACT_ABORT ON;
BEGIN TRAN;
UPDATE dv SET dv.Value = b.Value, dv.ModifiedDateTime = GETDATE()
FROM DefinedValue dv
JOIN (SELECT Id, Value, ROW_NUMBER() OVER (PARTITION BY Id ORDER BY BackedUpAt) rn
      FROM dbo._ROCK9253_DefinedValue_Backup WHERE DefinedTypeId = 277) b ON b.Id = dv.Id AND b.rn = 1
WHERE dv.Value LIKE '/_retired-ROCK9253/%';
SELECT dv.Id, dv.Value, dv.IsActive FROM DefinedValue dv WHERE dv.Id IN (SELECT Id FROM dbo._ROCK9253_DefinedValue_Backup WHERE DefinedTypeId = 277);
COMMIT;
