USE [MLB]
GO

/****** Object:  Table [dbo].[tblDraftPick]    Script Date: 2/23/2026 7:38:30 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[tblDraftPick](
	[DraftPickID] [int] IDENTITY(1,1) NOT NULL,
	[PickNumber] [int] NOT NULL,
	[LineData] [nvarchar](127) NOT NULL,
	[PlayerName_Ref] [nvarchar](64) NULL,
	[IsMyPick] [bit] NULL,
	[DraftID] [int] NOT NULL,
	[PlayerID] [int] NULL,
	[AvgPick] [decimal](6, 2) NULL,
 CONSTRAINT [PK_tblDraftPick] PRIMARY KEY CLUSTERED 
(
	[DraftPickID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[tblDraftPick]  WITH CHECK ADD  CONSTRAINT [FK_tblDraftPick_tblDraft] FOREIGN KEY([DraftID])
REFERENCES [dbo].[tblDraft] ([DraftID])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[tblDraftPick] CHECK CONSTRAINT [FK_tblDraftPick_tblDraft]
GO


