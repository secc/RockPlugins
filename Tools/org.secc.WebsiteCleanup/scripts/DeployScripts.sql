SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[_org_secc_WebPageCleanupLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PageId] [int] NOT NULL,
	[InternalName] [nvarchar](100) NULL,
	[ParentPageId] [int] NULL,
	[LayoutId] [int] NULL,
	[Guid] [uniqueidentifier] NULL,
	[InteractionCount] [int] NULL,
	[PageStatus] [smallint] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[_org_secc_WebPageCleanupLog] ADD PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[_org_secc_WebPageCleanupLog] ADD  CONSTRAINT [UniqueKey_Guid] UNIQUE NONCLUSTERED 
(
	[Guid] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO


SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[_org_secc_WebPageCleanup_Get_SitePages]
(
	@SiteId INT 
)

as 

SELECT 
	p.Id AS PageId, 
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[_org_secc_WebPageCleanup_Get_SitePages]
(
	@SiteId INT 
)

as 

SELECT 
	p.Id AS PageId, 
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[_org_secc_WebPageCleanup_Get_SitePages]
(
	@SiteId INT 
)

as 

SELECT 
	p.Id AS PageId, 
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[_org_secc_WebPageCleanup_Get_SitePages]
(
	@SiteId INT 
)

as 

SELECT 
	p.Id AS PageId, 
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[_org_secc_WebPageCleanup_Get_SitePages]
(
	@SiteId INT 
)

as 

SELECT 
	p.Id AS PageId, 
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
