-- Removes every row fixture.sql created (ForeignKey = 'attendance-cache-harness') and reports residuals (all must be 0).
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRAN;
DECLARE @fk nvarchar(100) = N'attendance-cache-harness';
DELETE FROM Attendance WHERE ForeignKey = @fk;
DELETE FROM AttendanceOccurrence WHERE ForeignKey = @fk;
DELETE FROM AttendanceCode WHERE ForeignKey = @fk;
COMMIT;
SELECT 'residual_attendance' k, COUNT(*) v FROM Attendance WHERE ForeignKey = @fk
UNION ALL SELECT 'residual_occurrence', COUNT(*) FROM AttendanceOccurrence WHERE ForeignKey = @fk
UNION ALL SELECT 'residual_code', COUNT(*) FROM AttendanceCode WHERE ForeignKey = @fk;
