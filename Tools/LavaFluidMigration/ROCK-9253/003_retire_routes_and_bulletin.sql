/*
ROCK-9253 step 2. The Subsplash plugin (com.subsplash SubsplashController) serves
api/subsplash/webhook/{path} from DefinedType 277 without checking IsActive
(verified 2026-11-06 after deploy restart + manual cache clear), so routes are
retired by deactivating AND renaming them under /_retired-ROCK9253, so no request
path matches. Original values are kept in _ROCK9253_DefinedValue_Backup.

Also expires the two "Bulletin" children (2088, 2551) of the old Homepage item
2087, so the few old-home devices stop showing a tile that links to the retired
/bulletin/campuses route. The original values are kept in _ROCK9253_ContentChannelItem_Backup.

Routes (32): the 7 deactivated by 001, plus 25 that no Subsplash tab links to and that
have had no real use in 12 months (Subsplash App interaction channel), or that are broken
(/home/tabs and /weekend: LavaFileSystem Template Not Found). See the commit message.

Kept on purpose: /prayer and /sermons/{page} (linked from the old home), the
/advent routes (seasonal), and the staff-built 2025-26 routes /sermons-2,
/sermons/series-2, /sekids/checkin-2, /test and /testnewsermon (confirm with their owners first).

Rollback: 004_rollback.sql. Clear the Rock cache afterwards.
*/
SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID('dbo._ROCK9253_ContentChannelItem_Backup') IS NULL
    CREATE TABLE dbo._ROCK9253_ContentChannelItem_Backup (
        Id int NOT NULL, Title nvarchar(200) NULL, ExpireDateTime datetime NULL,
        ModifiedDateTime datetime NULL, BackedUpAt datetime2 NOT NULL);

DECLARE @r TABLE (Id int PRIMARY KEY, Value nvarchar(250));
INSERT @r VALUES
 (30543,N'/bulletin/campuses'),
 (30280,N'/bulletin/campuses/{CampusGuid}'),
 (27571,N'/campuses'),
 (27570,N'/campus/{Id}'),
 (31676,N'/checkin/{Id}'),
 (50688,N'/sekids/checkin/{id}'),
 (50690,N'/sekids/pleaseupdate'),
 (37258,N'/home/initial'),
 (37257,N'/home/tabs'),
 (37256,N'/weekend'),
 (50686,N'/sekids/home'),
 (50685,N'/sekids/home/biblestory'),
 (50467,N'/sekids/home/biblestory/earlychildhood'),
 (50486,N'/sekids/home/biblestory/elementary'),
 (54028,N'/sekids/home/biblestory/nocategory'),
 (50468,N'/sekids/home/biblestory/video/{Id}'),
 (50487,N'/sekids/home/resources'),
 (50488,N'/sekids/home/resources/video/{Id}'),
 (50687,N'/sekids/home/video'),
 (50683,N'/sekids/home/worship'),
 (50684,N'/sekids/home/worship/video/{Id}'),
 (27195,N'/connect'),
 (27197,N'/more'),
 (30530,N'/grow'),
 (27196,N'/experience'),
 (32999,N'/sermondiscussion'),
 (27959,N'/tvapp/livestream'),
 (31184,N'/tabs/series/{series}'),
 (31629,N'/grow/resources/familyresources'),
 (31631,N'/grow/resources/familyresources/message/{id}'),
 (31632,N'/grow/resources/familyresources/message/{id}/resources'),
 (31630,N'/grow/resources/familyresources/tab/{id}');

-- Guard: all 32 routes still have their expected values.
IF (SELECT COUNT(*) FROM @r r JOIN DefinedValue dv
        ON dv.Id = r.Id AND dv.DefinedTypeId = 277 AND dv.Value = r.Value) <> 32
BEGIN
    RAISERROR('ROCK-9253: route Id/Value mismatch; nothing changed.', 16, 1);
    ROLLBACK; RETURN;
END
-- Guard: both Bulletin items are still unexpired children of Homepage 2087.
IF (SELECT COUNT(*) FROM ContentChannelItem i
    JOIN ContentChannelItemAssociation a ON a.ChildContentChannelItemId = i.Id AND a.ContentChannelItemId = 2087
    WHERE i.Id IN (2088, 2551) AND i.Title = 'Bulletin' AND i.ExpireDateTime IS NULL) <> 2
BEGIN
    RAISERROR('ROCK-9253: Bulletin item state mismatch; nothing changed.', 16, 1);
    ROLLBACK; RETURN;
END

-- Back up the routes that 001 has not already backed up.
INSERT dbo._ROCK9253_DefinedValue_Backup (Id, DefinedTypeId, Value, IsActive, ModifiedDateTime, BackedUpAt)
SELECT dv.Id, dv.DefinedTypeId, dv.Value, dv.IsActive, dv.ModifiedDateTime, SYSDATETIME()
FROM DefinedValue dv JOIN @r r ON r.Id = dv.Id
WHERE NOT EXISTS (SELECT 1 FROM dbo._ROCK9253_DefinedValue_Backup b WHERE b.Id = dv.Id);

INSERT dbo._ROCK9253_ContentChannelItem_Backup (Id, Title, ExpireDateTime, ModifiedDateTime, BackedUpAt)
SELECT Id, Title, ExpireDateTime, ModifiedDateTime, SYSDATETIME() FROM ContentChannelItem WHERE Id IN (2088, 2551);

UPDATE dv SET dv.IsActive = 0, dv.Value = '/_retired-ROCK9253' + r.Value, dv.ModifiedDateTime = GETDATE()
FROM DefinedValue dv JOIN @r r ON r.Id = dv.Id;

UPDATE ContentChannelItem SET ExpireDateTime = GETDATE(), ModifiedDateTime = GETDATE() WHERE Id IN (2088, 2551);

SELECT dv.Id, dv.Value, dv.IsActive FROM DefinedValue dv JOIN @r r ON r.Id = dv.Id ORDER BY dv.Value;
SELECT Id, Title, ExpireDateTime FROM ContentChannelItem WHERE Id IN (2088, 2551);
COMMIT;
