-- AttendanceCache harness fixture (LOCAL DEV DATABASES ONLY). See README.md in this folder.
-- Inserts @count Attendance rows created today on 5 AttendanceOccurrences (@groupId / @scheduleId at @locationIds),
-- plus 3 rows with no PersonAlias. Every row is tagged ForeignKey = 'attendance-cache-harness'; cleanup.sql removes them.
-- @groupId must be a check-in group whose group locations have @scheduleId, so the rows map to an OccurrenceCache entry.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @count int = 8615;          -- Sunday-sized key list (PROD 2026-10-04)
DECLARE @groupId int = 24;
DECLARE @scheduleId int = 6;
DECLARE @locationIds TABLE (n int IDENTITY(1, 1), LocationId int);
INSERT INTO @locationIds (LocationId) VALUES (11), (4), (5), (6), (7);

BEGIN TRAN;

DECLARE @fk nvarchar(100) = N'attendance-cache-harness';
DECLARE @now datetime = GETDATE();
DECLARE @today date = CAST(@now AS date);
DECLARE @withParent int = (SELECT Id FROM DefinedValue WHERE Guid = '25B4B1A5-9B20-4EDF-920B-A33098438FC3');

IF EXISTS (SELECT 1 FROM Attendance WHERE ForeignKey = @fk)
    RAISERROR('Fixture rows already exist; run cleanup.sql first.', 16, 1);

;WITH n AS (SELECT TOP 10 ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) rn FROM sys.objects)
INSERT INTO AttendanceCode (IssueDateTime, Code, Guid, ForeignKey)
SELECT @now, 'Z9' + RIGHT('00' + CAST(rn AS varchar(2)), 2), NEWID(), @fk FROM n;

DECLARE @codes TABLE (n int, Id int);
INSERT INTO @codes SELECT ROW_NUMBER() OVER (ORDER BY Id), Id FROM AttendanceCode WHERE ForeignKey = @fk;

INSERT INTO AttendanceOccurrence (GroupId, LocationId, ScheduleId, OccurrenceDate, DidNotOccur, Guid, CreatedDateTime, ModifiedDateTime, ForeignKey)
SELECT @groupId, l.LocationId, @scheduleId, @today, 0, NEWID(), @now, @now, @fk FROM @locationIds l;

DECLARE @occ TABLE (n int, Id int);
INSERT INTO @occ SELECT ROW_NUMBER() OVER (ORDER BY Id), Id FROM AttendanceOccurrence WHERE ForeignKey = @fk;
DECLARE @occCount int = (SELECT COUNT(*) FROM @occ);

DECLARE @pa TABLE (n int, Id int);
INSERT INTO @pa SELECT ROW_NUMBER() OVER (ORDER BY pa.Id), pa.Id FROM PersonAlias pa JOIN Person p ON p.Id = pa.PersonId WHERE p.RecordTypeValueId = 1;
DECLARE @paCount int = (SELECT COUNT(*) FROM @pa);

-- i % 10: 5 -> EnRoute (DidAttend 0); 6 -> EnRoute (DidAttend null); 7 -> InRoom WithParent;
--         8 -> MobileReserve; 9 -> CheckedOut; others -> InRoom. i % 3 = 0 -> no AttendanceCode.
;WITH seq AS (SELECT TOP (@count) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) i FROM sys.all_columns a CROSS JOIN sys.all_columns b)
INSERT INTO Attendance (OccurrenceId, PersonAliasId, AttendanceCodeId, QualifierValueId, StartDateTime, EndDateTime, DidAttend, Guid, CreatedDateTime, ModifiedDateTime, ForeignKey)
SELECT o.Id, pa.Id,
       CASE WHEN i % 3 = 0 THEN NULL ELSE c.Id END,
       CASE WHEN i % 10 IN (7, 8) THEN @withParent ELSE NULL END,
       DATEADD(second, -(i % 3600), @now),
       CASE WHEN i % 10 = 9 THEN DATEADD(second, -(i % 600), @now) ELSE NULL END,
       CASE WHEN i % 10 IN (5, 8) THEN 0 WHEN i % 10 = 6 THEN NULL ELSE 1 END,
       NEWID(), DATEADD(second, -(i % 3600), @now), @now, @fk
FROM seq
JOIN @occ o ON o.n = (i % @occCount) + 1
JOIN @pa pa ON pa.n = (i % @paCount) + 1
LEFT JOIN @codes c ON c.n = (i % 10) + 1;

-- Attendance with no person: InRoom, no PersonAlias (cached as PersonId 0 / "Unnamed Person").
INSERT INTO Attendance (OccurrenceId, PersonAliasId, StartDateTime, DidAttend, Guid, CreatedDateTime, ModifiedDateTime, ForeignKey)
SELECT o.Id, NULL, @now, 1, NEWID(), @now, @now, @fk FROM @occ o WHERE o.n <= 3;

SELECT 'attendance' k, COUNT(*) v FROM Attendance WHERE ForeignKey = @fk
UNION ALL SELECT 'null_alias', COUNT(*) FROM Attendance WHERE ForeignKey = @fk AND PersonAliasId IS NULL
UNION ALL SELECT 'today_attendance_total', COUNT(*) FROM Attendance WHERE CreatedDateTime >= @today;

COMMIT;
