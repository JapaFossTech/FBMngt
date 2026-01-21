USE [MLB]
GO

/****** Object:  Table [dbo].[tblPlayer]    Script Date: 1/21/2026 7:38:12 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[tblPlayer](
	[PlayerID] [int] IDENTITY(1,1) NOT NULL,
	[PlayerName] [nvarchar](127) NULL,
	[Aka1] [nvarchar](127) NULL,
	[Aka2] [nvarchar](127) NULL,
	[FanGraphsID] [int] NULL,
	[PlayerID_MLBAM] [varchar](15) NULL,
	[name_first] [nvarchar](31) NULL,
	[name_middle] [nvarchar](31) NULL,
	[name_last] [nvarchar](31) NULL,
	[season] [int] NULL,
	[organization_id] [varchar](7) NULL,
	[primary_position] [varchar](8) NULL,
	[birth_date] [datetime] NULL,
	[ModifiedDate] [datetime] NULL,
 CONSTRAINT [PK_tblPlayer] PRIMARY KEY CLUSTERED 
(
	[PlayerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
