DECLARE @i INT = 1;
DECLARE @j INT = 1;
DECLARE @MaxCodeId int = 0;
DECLARE @MinCodeId int = 0;

WHILE (@j <= 1000)
	BEGIN

	select @MaxCodeId = max(AttendanceCodeId) 
				FROM [RockDB].[dbo].[Attendance]
	select @MinCodeId = min(AttendanceCodeId) 
				FROM [RockDB].[dbo].[Attendance]
				where DeviceId = 145;

	delete
		FROM [RockDB].[dbo].[Attendance]
		where DeviceId = 145  
			and AttendanceCodeId >= @MinCodeId 
			and AttendanceCodeId <= @MaxCodeId;

	delete
		FROM [RockDB].[dbo].[AttendanceCode] where
		id not in (
		select id
			FROM [RockDB].[dbo].[AttendanceCode] where
			id in (
			select AttendanceCodeId
				FROM [RockDB].[dbo].[Attendance])
				and id >= @MinCodeId)
			and id >= @MinCodeId and id <= @MaxCodeId;

	RAISERROR( 'Stuff Deleted between %d and %d',0,1, @MinCodeId, @MaxCodeId) WITH NOWAIT
	
	SET @i = 0;
	WHILE (@i <= 60)
	 BEGIN
	  WAITFOR DELAY '00:00:01'
	 SET  @i = @i + 1;
	END 
END 