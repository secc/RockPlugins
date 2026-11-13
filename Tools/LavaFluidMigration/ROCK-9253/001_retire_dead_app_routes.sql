/*
ROCK-9253: retire Subsplash webhook routes and Html To Image templates that
still point at the churchseimaging Azure Function and are no longer used.

Deactivates (IsActive = 0); deletes nothing. Lava.ashx only serves active
DefinedValues, so the routes return 404 once Rock's cache is cleared.
Rollback: 002_rollback.sql.

Evidence (prod, 2026-11-06):
  Subsplash App interaction channel (12 months) + churchseimaging App Insights (90 days)
  30543 /bulletin/campuses                3 views, last 2026-01-08; 0 image hits
  30280 /bulletin/campuses/{CampusGuid}   1 view,  last 2025-12-10; 0 image hits
  27571 /campuses                         1 view,  last 2026-03-29; 0 image hits
  27570 /campus/{Id}                      0 views;                  0 image hits
  31676 /checkin/{Id}                     1 view,  last 2025-11-10; replaced by /checkin-redirect + /sekids/checkin-2
  50688 /sekids/checkin/{id}              not logged; only link (/sekids/initial) is inside {% comment %}; 0 image hits
  50690 /sekids/pleaseupdate              not logged; no live route links to it; 0 image hits
  Html To Image (DefinedType 290) used only by the routes above, 0 hits in 90 days:
  30535 31211 31214 31216 31218 (location routes), 30544 30545 32123 49813 (bulletin routes),
  31675 (checkin), 50689 (pleaseupdate)

After running: Admin Tools > Settings > System Settings > Clear Cache (DefinedValueCache).
*/
SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9253_DefinedValue_Backup') IS NULL
    CREATE TABLE dbo._ROCK9253_DefinedValue_Backup (
        Id int NOT NULL, DefinedTypeId int NOT NULL, Value nvarchar(250) NOT NULL,
        IsActive bit NOT NULL, ModifiedDateTime datetime NULL, BackedUpAt datetime2 NOT NULL);

DECLARE @t TABLE (Id int PRIMARY KEY, DefinedTypeId int, Value nvarchar(250));
INSERT @t VALUES
 (30543,277,'/bulletin/campuses'),
 (30280,277,'/bulletin/campuses/{CampusGuid}'),
 (27571,277,'/campuses'),
 (27570,277,'/campus/{Id}'),
 (31676,277,'/checkin/{Id}'),
 (50688,277,'/sekids/checkin/{id}'),
 (50690,277,'/sekids/pleaseupdate'),
 (30535,290,'location/{Guid}'),
 (31211,290,'location/info/{Campus}'),
 (31214,290,'location/info/threeslice/{PublishGroupId}'),
 (31216,290,'location/info/twoslice/{Guid}'),
 (31218,290,'location/info/oneslice/{Guid}'),
 (30544,290,'bulletin/campusIntro/{Guid}'),
 (30545,290,'bulletin/connect/{Guid}'),
 (32123,290,'bulletin/campusFigures/{Guid}'),
 (49813,290,'bulletin/connectdemo/{Guid}'),
 (31675,290,'checkin'),
 (50689,290,'pleaseupdate');

-- Guard: every Id must still be the route we expect.
IF (SELECT COUNT(*) FROM @t t JOIN DefinedValue dv
        ON dv.Id = t.Id AND dv.DefinedTypeId = t.DefinedTypeId AND dv.Value = t.Value) <> 18
BEGIN
    RAISERROR('ROCK-9253: DefinedValue Id/Value mismatch; nothing changed.', 16, 1);
    ROLLBACK; RETURN;
END

INSERT dbo._ROCK9253_DefinedValue_Backup (Id, DefinedTypeId, Value, IsActive, ModifiedDateTime, BackedUpAt)
SELECT dv.Id, dv.DefinedTypeId, dv.Value, dv.IsActive, dv.ModifiedDateTime, SYSDATETIME()
FROM DefinedValue dv JOIN @t t ON t.Id = dv.Id;

UPDATE dv SET dv.IsActive = 0, dv.ModifiedDateTime = GETDATE()
FROM DefinedValue dv JOIN @t t ON t.Id = dv.Id
WHERE dv.IsActive = 1;

SELECT dv.Id, dv.DefinedTypeId, dv.Value, dv.IsActive FROM DefinedValue dv JOIN @t t ON t.Id = dv.Id ORDER BY dv.DefinedTypeId, dv.Value;

COMMIT;
