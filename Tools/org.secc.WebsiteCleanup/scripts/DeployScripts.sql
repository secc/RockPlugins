IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES where TABLE_NAME = '_org_secc_WebPageCleanupLog')
BEGIN
	CREATE TABLE dbo._org_secc_WebPageCleanupLog
	(
		Id INT NOT NULL Identity(1,1) PRIMARY KEY,
		PageId INT NOT NULL,
		InternalName NVARCHAR(100),
		ParentPageId INT, 
		LayoutId INT,
		[Guid] UNIQUEIDENTIFIER,
		InteractionCount INT,
		PageStatus SMALLINT
	)
	

	ALTER TABLE dbo._org_secc_WebPageCleanupLog 
	ADD CONSTRAINT UniqueKey_Guid UNIQUE ([Guid])
END 
GO

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE RoutineName='_org_secc_WebPageCleanupLog_Insert')
BEGIN
	DROP PROCEDURE [dbo].[_org_secc_WebPageCleanupLog_Insert]
END

GO

/****** Object:  StoredProcedure [dbo].[_org_secc_WebPageCleanupLog_Insert]    Script Date: 10/7/2024 2:21:19 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[_org_secc_WebPageCleanupLog_Insert]
(
	@PageId INT, 
	@InternalName NVARCHAR(100),
	@ParentPageId INT,
	@LayoutId INT,
	@InteractionCount INT NULL,
	@PageStatus SMALLINT NULL,
	@Id INT OUTPUT
)
AS

INSERT INTO dbo._org_secc_WebPageCleanupLog
(
	PageId,
	InternalName,
	ParentPageId,
	LayoutId,
	Guid,
	InteractionCount,
	PageStatus
)
VALUES
(
	@PageId,
	@InternalName,
	@ParentPageId,
	@LayoutId,
	NewId(),
	@InteractionCount,
	@PageStatus
)

SET @ID = SCOPE_IDENTITY()
GO

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.ROUTINES WHERE RoutineName='_org_secc_WebPageCleanup_Get_SitePages')
BEGIN
	DROP PROCEDURE [dbo].[_org_secc_WebPageCleanup_Get_SitePages]
END

GO

CREATE PROCEDURE dbo._org_secc_WebPageCleanup_Get_SitePages
(
	@SiteId INT 
)

AS 

SELECT 
	p.Id, 
	ParentPageId,
	InternalName,
	LayoutId
FROM	
	[dbo].[Page] p
	INNER JOIN [dbo].[Layout] l on p.LayoutId = l.Id
	INNER JOIN [dbo].[Site] s on l.SiteId = s.Id
WHERE 
	s.Id = @SiteId

GO