using System.Collections.Generic;

namespace SpringDownloader.StartTab
{
	public class SinglePlayerProfiles
	{
		public static List<SinglePlayerProfile> Profiles = new List<SinglePlayerProfile>
		                                               { new SinglePlayerProfile() { Name = "Chickens Easy", Description = "Difficulty: Easy\nEnemy: Chickens\nMap size: medium\n\nIntroduction to chicken defense", Image = "/Resources/SinglePlayer/chickeneasy.png"}, 
																									 new SinglePlayerProfile() { Name = "x", Description = "Hard chickens invisible map", Image="/Resources/SinglePlayer/chickenhard.png" },
																									 new SinglePlayerProfile() { Name = "x", Description = "Hard chickens invisible map", Image="/Resources/SinglePlayer/chickenhard.png" },
																									 new SinglePlayerProfile() { Name = "x", Description = "Hard chickens invisible map", Image="/Resources/SinglePlayer/chickenhard.png" },
																									 new SinglePlayerProfile() { Name = "x", Description = "Hard chickens invisible map", Image="/Resources/SinglePlayer/chickenhard.png" },
																									 new SinglePlayerProfile() { Name = "x", Description = "Hard chickens invisible map", Image="/Resources/SinglePlayer/chickenhard.png" },
																									 };

		
		
	}

	public class SinglePlayerProfile
	{
		public string Description { get; set; }
		public string MapName { get; set; }
		public string ModTag { get; set; }
		public string Name { get; set; }
		public string Image { get; set; }
		public string StartScript { get; set; }
	}
}