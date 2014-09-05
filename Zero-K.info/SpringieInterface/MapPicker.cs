using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
	public class RecommendedMapResult
	{
		public string MapName;
		public string Message;
	}

	public class MapPicker
	{

		public static RecommendedMapResult GetRecommendedMap(BattleContext context, bool pickNew) {
			var mode = context.GetMode();
		    var config = context.GetConfig();
			var res = new RecommendedMapResult();
			using (var db = new ZkDataContext()) {
				if (mode == AutohostMode.Planetwars)
				{

				    var info = Global.PlanetWarsMatchMaker.GetBattleInfo(context.AutohostName);
				    if (info != null)
				    {
				        res.MapName = info.Map;
                        res.Message = String.Format("Welcome to planet {0} http://zero-k.info/PlanetWars/Planet/{1} attacked", info.Name, info.PlanetID);
				    } else res.MapName = context.Map;
				}
				else { 
					if (!pickNew) {
						// autohost is not managed or has valid featured map - check disabled
    					res.MapName = context.Map;
						return res;
					}
					List<Resource> list = null;
					var players = context.Players.Count(x => !x.IsSpectator);
                    if (config != null && config.SplitBiggerThan != null && players > config.SplitBiggerThan) players = players/2; // expect the split
					switch (mode) {
                        case AutohostMode.HighSkill:
                        case AutohostMode.LowSkill:
                        case AutohostMode.SmallTeams:
                        case AutohostMode.Teams:
                        case AutohostMode.None:
							var ret = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsTeams != false && x.MapIsSpecial != true);
							if (players > 11) ret = ret.Where(x => (x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) > 16*16);
							else if (players > 8) ret = ret.Where(x => (x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) > 16*16 && (x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) <= 24*24);
                            else if (players > 5) ret = ret.Where(x => (x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth) <= 24 * 24 || x.MapIs1v1 == true);
							else ret = ret.Where(x => (x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) <= 16*16 || x.MapIs1v1 == true);
							list = ret.ToList();

							break;
						case AutohostMode.Game1v1:
							list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIs1v1 == true && x.MapIsSpecial != true).ToList();
							break;
						case AutohostMode.GameChickens:
							ret = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsSpecial != true && (x.MapIsChickens == true || x.MapWaterLevel == 1));
                            if (players > 5) ret = ret.Where(x => (x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth) > 16 * 16);
                            else if (players > 4) ret = ret.Where(x => (x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth) > 16 * 16 && (x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth) <= 24 * 24);
                            else if (players > 2) ret = ret.Where(x => (x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth) <= 24 * 24 || x.MapIs1v1 == true);
                            else ret = ret.Where(x => (x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth) <= 16 * 16 || x.MapIs1v1 == true);
							list = ret.ToList();

							break;
						case AutohostMode.GameFFA:
							list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true && x.MapFFAMaxTeams == players).ToList();
							if (!list.Any()) list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true && (players%x.MapFFAMaxTeams == 0)).ToList();
							if (!list.Any()) list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true).ToList();

							break;
					}
					if (list != null) {
						var r = new Random();
						res.MapName = list[r.Next(list.Count)].InternalName;
					}
				}
			}
			return res;
		}

		public class PlanetPickEntry
		{
			private readonly Planet planet;
			private readonly int weight;
			public Planet Planet {
				get { return planet; }
			}
			public int Weight {
				get { return weight; }
			}

			public PlanetPickEntry(Planet planet, int weight) {
				this.planet = planet;
				this.weight = weight;
			}
		}
	}
}