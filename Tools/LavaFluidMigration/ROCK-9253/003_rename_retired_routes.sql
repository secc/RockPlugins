/*
ROCK-9253 step 2: the Subsplash plugin (com.subsplash SubsplashController) serves
api/subsplash/webhook/{path} from DefinedType 277 without checking IsActive, so
001 alone did not take the routes down (verified after deploy + cache clear).
Rename the 7 retired routes so no request path matches them.
Original values are already in _ROCK9253_DefinedValue_Backup (from 001).
Rollback: 004_rollback_rename.sql. Clear Rock cache afterwards.
*/
SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @t TABLE (Id int PRIMARY KEY, Value nvarchar(250));
INSERT @t VALUES
 (30543,'/bulletin/campuses'),
 (30280,'/bulletin/campuses/{CampusGuid}'),
 (27571,'/campuses'),
 (27570,'/campus/{Id}'),
 (31676,'/checkin/{Id}'),
 (50688,'/sekids/checkin/{id}'),
 (50690,'/sekids/pleaseupdate');

IF (SELECT COUNT(*) FROM @t t
    JOIN DefinedValue dv ON dv.Id = t.Id AND dv.DefinedTypeId = 277 AND dv.Value = t.Value AND dv.IsActive = 0
    JOIN dbo._ROCK9253_DefinedValue_Backup b ON b.Id = t.Id AND b.Value = t.Value) <> 7
BEGIN
    RAISERROR('ROCK-9253: route Id/Value/backup mismatch; nothing changed.', 16, 1);
    ROLLBACK; RETURN;
END

UPDATE dv SET dv.Value = '/_retired-ROCK9253' + t.Value, dv.ModifiedDateTime = GETDATE()
FROM DefinedValue dv JOIN @t t ON t.Id = dv.Id;

SELECT dv.Id, dv.Value, dv.IsActive FROM DefinedValue dv JOIN @t t ON t.Id = dv.Id ORDER BY dv.Value;
COMMIT;
