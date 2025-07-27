SET IDENTITY_INSERT [dbo].[SpringBattles] ON
INSERT INTO [dbo].[SpringBattles] ([SpringBattleID], [EngineGameID], [HostAccountID], [Title], [MapResourceID], [ModResourceID], [StartTime], [Duration], [PlayerCount], [HasBots], [IsMission], [ReplayFileName], [EngineVersion], [IsEloProcessed], [WinnerTeamXpChange], [LoserTeamXpChange], [ForumThreadID], [Mode], [IsMatchMaker], [ApplicableRatings], [Rank]) 
VALUES (5, 0, 1, 'test', 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, null, 0, 0, 0, 0)
SET IDENTITY_INSERT [dbo].[SpringBattles] OFF
