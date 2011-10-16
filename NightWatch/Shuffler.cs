using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using LobbyClient;
using ZkData;

namespace NightWatch
{
	class Shuffler
	{
		const int shufflerIntervalMinutes = 2;
		private readonly TasClient tascClient;

		public Shuffler(TasClient tasclient)
		{
			tascClient = tasclient;
			var timer = new Timer { Interval = shufflerIntervalMinutes*60*1000, AutoReset = true };
			timer.Elapsed += (s, e) => Shuffle();
			// timer.Start();         /************************ UNCOMMENT THIS LINE TO ACTIVATE! *****************************/
		}
		bool IsPlanetWarsHost(string userName)
		{
			return userName.StartsWith("PlanetWars");
		}


		// players to relocate:
		// 1) the game is full and the player is a spectator
		// 2) the game is not full but has started, player is not spec
		// also, never relocate an ingame player
		void Shuffle()
		{
			using (var db = new ZkDataContext())
			{
				var planetWarsBattles = tascClient.ExistingBattles.Values.Where(b => IsPlanetWarsHost(b.Founder.Name));

				var nonFullHosts = planetWarsBattles.Where(b => b.MaxPlayers > b.NonSpectatorCount).Select(b => new HostInfo(b)).ToList();
				if (nonFullHosts.Count == 0) return;

				// 1) the game is full and the player is a spectator
				var players1 = 
						from battle in planetWarsBattles
						where battle.MaxPlayers == battle.NonSpectatorCount // battle is full
						from user in battle.Users
						where !tascClient.ExistingUsers[user.Name].IsInGame // user is not ingame
						where user.IsSpectator // user is spectator
						select user.Name;

				// 2) the game is not full but has started, player is not spec
				var players2 = from host in nonFullHosts
							   where host.Battle.IsInGame // battle has started
							   from user in host.Battle.Users
							   where !tascClient.ExistingUsers[user.Name].IsInGame // user is not ingame
							   where !user.IsSpectator // user is *not* a spec
				               select user.Name;
				
				
				foreach (var player in players1.Union(players2))
				{
					RelocatePlayer(nonFullHosts, player);
					if (!nonFullHosts.Any(h => h.FreeSpots > 0)) return;
				}
			}
		}


		void RelocatePlayer(IEnumerable<HostInfo> nonFullHosts, string playerName) 
		{
			var targetHost = nonFullHosts.Where(h => h.FreeSpots > 0).OrderByDescending(h => h.FreeSpots).FirstOrDefault();
			if (targetHost == null) return;
			targetHost.FreeSpots--;
			// TODO: forgemsg
		}

		class HostInfo 
		{
			public HostInfo(Battle battle)
			{
				Battle = battle;
				FreeSpots = battle.MaxPlayers - battle.NonSpectatorCount;
			}

			public Battle Battle { get; set; }
			public int FreeSpots { get; set; }
		}
	}
}
