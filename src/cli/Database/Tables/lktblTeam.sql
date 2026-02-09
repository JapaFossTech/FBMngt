USE [MLB]
GO

/****** Object:  Table [dbo].[lktblTeam]    Script Date: 2/6/2026 8:18:44 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[lktblTeam](
	[TeamID] [int] IDENTITY(1,1) NOT NULL,
	[Team] [varchar](31) NOT NULL,
	[TeamDesc] [nvarchar](50) NOT NULL,
	[Team_3Letter] [char](3) NULL,
	[mlb_org_id] [varchar](7) NULL,
	[mlb_org_abbrev] [varchar](3) NULL,
	[mlb_org_short] [nvarchar](23) NULL,
	[mlb_org] [nvarchar](23) NULL,
	[league] [varchar](3) NULL,
 CONSTRAINT [PK_lktblTeam] PRIMARY KEY CLUSTERED 
(
	[TeamID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET ANSI_PADDING ON
GO

/****** Object:  Index [lktblTeam_MlbOrgIDU]    Script Date: 2/6/2026 8:19:05 PM ******/
CREATE UNIQUE NONCLUSTERED INDEX [lktblTeam_MlbOrgIDU] ON [dbo].[lktblTeam]
(
	[mlb_org_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
