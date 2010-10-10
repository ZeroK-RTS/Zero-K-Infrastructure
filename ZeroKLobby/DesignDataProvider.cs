using System;
using System.Collections.Generic;
using System.Linq;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
	public class DesignDataProvider {
		public IEnumerable<SinglePlayerProfile> GetSinglePlayerProfiles()
		{
			return StartPage.GameList.First(x => x.Profiles.Count > 1).Profiles;
		}
	}
}