USE [MLB]
GO

/****** Object:  Table [dbo].[lktblDraftType]    Script Date: 2/23/2026 7:32:26 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[lktblDraftType](
	[DraftTypeID] [int] IDENTITY(1,1) NOT NULL,
	[DraftType] [varchar](31) NOT NULL,
	[DraftTypeDesc] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_lktblDraftType] PRIMARY KEY CLUSTERED 
(
	[DraftTypeID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

SET IDENTITY_INSERT dbo.lktblDraftType ON;
GO

INSERT INTO dbo.lktblDraftType (DraftTypeID, DraftType, DraftTypeDesc)
VALUES
(1, 'Mockup', 'Mockup'),
(2, 'Live', 'Live'),
(3, 'FPro_All', 'FPro All'),
(4, 'FPro_Top10', 'FPro Top10'),
(5, 'FPro_Top20', 'FPro Top20'),
(6, 'MyPreDraftRank', 'My Pre-Draft Ranking');
GO

SET IDENTITY_INSERT dbo.lktblDraftType OFF;
GO

