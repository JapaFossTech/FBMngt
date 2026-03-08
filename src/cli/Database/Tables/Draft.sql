USE [MLB]
GO

/****** Object:  View [dbo].[Draft]    Script Date: 2/23/2026 7:39:43 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE OR ALTER VIEW [dbo].[Draft]
AS
SELECT     tblDraft.DraftID, tblDraft.Season, tblDraft.CreatedDate AS DraftDate, lktblDraftType.DraftTypeDesc, tblDraftPick.DraftPickID, tblDraftPick.PickNumber, 
CASE WHEN (dbo.tblDraftPick.PickNumber % 12) = 0 THEN dbo.tblDraftPick.PickNumber / 12 ELSE dbo.tblDraftPick.PickNumber / 12 + 1 END AS Round, 
tblDraftPick.PlayerID, tblDraftPick.PlayerName_Ref, tblDraftPick.IsMyPick, tblDraftPick.LineData, tblDraftPick.AvgPick
, COALESCE(tblPlayer.PlayerName, tblDraftPick.PlayerName_Ref) as PlayerName
FROM         tblDraftPick INNER JOIN
tblDraft ON tblDraftPick.DraftID = tblDraft.DraftID INNER JOIN
lktblDraftType ON tblDraft.DraftTypeID = lktblDraftType.DraftTypeID LEFT OUTER JOIN
tblPlayer ON tblDraftPick.PlayerID = tblPlayer.PlayerID
GO


