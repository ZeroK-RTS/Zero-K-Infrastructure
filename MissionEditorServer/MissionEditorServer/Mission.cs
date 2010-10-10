#region using

#endregion

#region using

using System;

#endregion

namespace MissionEditorServer
{
	public class MissionData
	{
		#region Properties

		public MissionInfo MissionInfo;
		public byte[] Mutator;

		#endregion

		#region Constructors

		public MissionData()
		{
			MissionInfo = new MissionInfo();
		}

		public MissionData(Mission fromEntity)
		{
			Mutator = fromEntity.Mutator.ToArray();
			MissionInfo = new MissionInfo(fromEntity);
		}

		#endregion
	}

	public class MissionInfo
	{
		#region Properties

		public string Author;
		public int CommentCount;
		public DateTime? CreatedTime;
		public string Description;
		public int DownloadCount;
		public byte[] Image;
		public DateTime? LastCommentTime;
		public string Map;
		public int MissionID;
		public string Mod;
		public DateTime? ModifiedTime;
		public string Name;
		public double Rating;
		public string ScoringMethod;
		public string TopScoreLine;
		public string SpringVersion;
		public string MissionEditorVersion;

		#endregion

		#region Constructors

		public MissionInfo() {}

		public MissionInfo(Mission fromEntity)
		{
			Description = fromEntity.Description;
			Image = fromEntity.Image.ToArray();
			Map = fromEntity.Map;
			Mod = fromEntity.Mod;
			Name = fromEntity.Name;
			MissionID = fromEntity.MissionID;
			Author = fromEntity.Author;
			Rating = fromEntity.Rating;
			TopScoreLine = fromEntity.TopScoreLine;
			ScoringMethod = fromEntity.ScoringMethod;
			DownloadCount = fromEntity.DownloadCount;
			CreatedTime = fromEntity.CreatedTime;
			ModifiedTime = fromEntity.ModifiedTime;
			CommentCount = fromEntity.CommentCount;
			LastCommentTime = fromEntity.LastCommentTime;
			SpringVersion = fromEntity.SpringVersion;
			MissionEditorVersion = fromEntity.MissionEditorVersion;
		}


		

		#endregion
	}

	public class ScoreEntry
	{
		public string PlayerName;
		public int Score;
		public int TimeSeconds;
	}
}