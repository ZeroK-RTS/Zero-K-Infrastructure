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
				if (mode == AutohostMode.Planetwars) {
				    var blockedMaps = Global.Nightwatch.GetPlanetWarsBattles().Where(x=>x.IsInGame).Select(x => x.MapName).ToList();
					var playerAccounts = context.Players.Where(x => !x.IsSpectator).Select(x => db.Accounts.First(z => z.LobbyID == x.LobbyID)).ToList();

				    var validAttackerFactionIDs = playerAccounts.Where(x => x.FactionID != null).GroupBy(x => x.FactionID).Select(x => x.Key).ToList();

				    var valid =
				        db.PlanetFactions.Where(
				            x => x.Dropships > 0 && !blockedMaps.Contains(x.Planet.Resource.InternalName) && validAttackerFactionIDs.Contains(x.FactionID)).
				            Select(x => new
				                        {
                                            Planet = x.Planet,
                                            Attacker = x.Faction,

                                            FreeShips = x.Dropships - (x.Planet.PlanetStructures.Where(y => y.IsActive && (y.Planet.OwnerFactionID != x.FactionID)).Sum(y => y.StructureType.EffectDropshipDefense) ?? 0),
                                            TotalShips = x.Dropships,
                                            LastAdded = x.DropshipsLastAdded

				                        }).Where(x=>x.FreeShips > 0).OrderByDescending(x=>x.FreeShips).ThenBy(x=>x.LastAdded).FirstOrDefault();

                    if (valid == null) return new RecommendedMapResult()
                                               {
                                                   Message = "Use dropships to attack some planet!"
                                               };

				    var planet = valid.Planet;
                    res.MapName = planet.Resource.InternalName;
					var owner = "";
					if (planet.Account != null) owner = planet.Account.Name;

					var shipInfo = String.Format("{0} ships from {1}", valid.TotalShips, valid.Attacker.Name);

					res.Message = String.Format("Welcome to {0} planet {1} http://zero-k.info/PlanetWars/Planet/{2} attacked by {3}",
					                            owner,
					                            planet.Name,
					                            planet.PlanetID,
					                            shipInfo);

                    if (res.MapName != context.Map)
                    {
                        if (planet.OwnerFactionID != null)
                        {
                            Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, planet.Faction.Shortcut, string.Format("Your planet {0} is about to be attacked, defend it! Come to PlanetWars spring://@join_player:{1} ", planet.Name, context.AutohostName), true);
                        }
                    }
				    db.SubmitChanges();
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
						case AutohostMode.BigTeams:
                            var ret = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa != true && x.MapIsChickens != true && x.MapIsSpecial == true);
                            list = ret.ToList();
                            break;

						case AutohostMode.SmallTeams:
                            case AutohostMode.Experienced:
							ret = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa != true && x.MapIsChickens != true && x.MapIsSpecial != true);
							if (players > 11) ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) > 16 && x.MapIs1v1 != true);
							else if (players > 7) ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) > 16 && Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) <= 24 && x.MapIs1v1 != true);
							else ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) <= 16 || x.MapIs1v1 == true);
							list = ret.ToList();

							break;
						case AutohostMode.Game1v1:
							list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIs1v1 == true && x.MapIsFfa != true && x.MapIsChickens != true && x.MapIsSpecial != true).ToList();
							break;
						case AutohostMode.GameChickens:
							ret = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && (x.MapIsChickens == true || x.MapWaterLevel == 1));
							if (players > 11) ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) > 16);
							else if (players > 6) ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) > 16 && Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) <= 24);
							else ret = ret.Where(x => Math.Sqrt((x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth) ?? 0) <= 16);
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