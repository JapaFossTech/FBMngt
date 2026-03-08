USE [MLB]
GO

/****** Object:  Table [dbo].[tblDraft]    Script Date: 2/23/2026 7:36:03 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[tblDraft](
	[DraftID] [int] IDENTITY(1,1) NOT NULL,
	[Season] [int] NOT NULL,
	[DraftTypeID] [int] NOT NULL,
	[CreatedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_tblDraft] PRIMARY KEY CLUSTERED 
(
	[DraftID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[tblDraft]  WITH CHECK ADD  CONSTRAINT [FK_tblDraft_lktblDraftType] FOREIGN KEY([DraftTypeID])
REFERENCES [dbo].[lktblDraftType] ([DraftTypeID])
GO

ALTER TABLE [dbo].[tblDraft] CHECK CONSTRAINT [FK_tblDraft_lktblDraftType]
GO


