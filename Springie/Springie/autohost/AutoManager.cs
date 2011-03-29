#region using

using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using LobbyClient;
using Springie.autohost;
using Timer = System.Timers.Timer;

#endregion
using System.Linq;

namespace Springie.AutoHostNamespace
{
	public class AutoManager
	{
		const int KickAfter = 300;
		const double NormalGameGracePeriod = 300;
		const int RingEvery = 60;
		const double ShortGameGracePeriod = 120;
		const int SpecForceAfter = 180;

		readonly AutoHost ah;
		int allyCount; // number of alliances
		bool byClans;
		int from;
		DateTime lastRing = DateTime.Now;
		readonly Dictionary<string, DateTime> problemSince = new Dictionary<string, DateTime>();
		readonly Spring spring;
		readonly TasClient tas;
		readonly Timer timer = new Timer(5000);

		int to;

		public bool Enabled { get { return from > 0; } }

		public AutoManager(AutoHost ah, TasClient tas, Spring spring)
		{
			this.ah = ah;
			this.tas = tas;
			this.spring = spring;
			timer.Elapsed += timer_Elapsed;
			timer.Start();
		}

		public void Manage(int min, int max, int teams, TasSayEventArgs e, bool clanBased)
		{
			if (max < min) max = min;
			from = min;
			to = max;
			byClans = clanBased;
			allyCount = teams;
			if (teams < 1) allyCount = 1;
			if (teams > max) allyCount = max;
			if (min == max && allyCount == 2 && min%2 == 1) allyCount = min; // this means its ffa game with unset ally count (probably)
			if (min > 0 && ah.hostedMod.IsMission)
			{
				from = ah.hostedMod.MissionSlots.Count(x => x.IsHuman && x.IsRequired);
				to = ah.hostedMod.MissionSlots.Count(x => x.IsHuman);
				allyCount = ah.hostedMod.MissionSlots.Where(x => x.IsHuman).GroupBy(x => x.AllyID).Count();
			}
			timer.Start();
			if (min == 0) ah.Respond(e, "managing disabled");
			else ah.Respond(e, "auto managing for " + from + " to " + to + " players and " + allyCount + " teams");
		}

		public void Stop()
		{
			from = 0;
			to = 0;
			lock (timer)
			{
				timer.Stop();
			}
		}

		void timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			lock (timer)
			{
				timer.Stop();
				var isPlanetwars = ah.config.PlanetWarsEnabled && !string.IsNullOrEmpty(Program.main.Config.PlanetWarsServer);
				try
				{
					if (from > 0 && !spring.IsRunning)
					{
						var b = tas.MyBattle;
						if (b != null)
						{
							if (b.IsLocked) tas.ChangeLock(false); // if locked then unlock
							var plrCnt = b.NonSpectatorCount;
							if (plrCnt >= from)
							{
								List<string> notReady;

								if (!isPlanetwars && !ah.AllUniqueTeams(out notReady))
								{
									ah.ComFix(TasSayEventArgs.Default, new string[] { });
									return;
								}

								var isReady = ah.AllReadyAndSynced(out notReady);
								if ((!isPlanetwars && plrCnt%allyCount == 0) || ah.hostedMod.IsMission)
								{
									// should we expect teams can be balanced 
									int allyno;
									int alliances;
									if (!ah.BalancedTeams(out allyno, out alliances) || (alliances != allyCount && !ah.hostedMod.IsMission))
									{
										// teams are balancable but not balanced - fix colors and balance
										ah.BalanceTeams(allyCount, byClans);
										Thread.Sleep(1000);
										ah.ComTeamColors(TasSayEventArgs.Default, new string[] { });
									}
								}
								if (isReady && plrCnt%allyCount == 0)
								{
									Thread.Sleep(1000);
									if (!spring.IsRunning)
									{
										if (isPlanetwars)
										{
											ah.ComTeamColors(TasSayEventArgs.Default, new string[] { });
											Thread.Sleep(1000);
										}
										ah.ComStart(TasSayEventArgs.Default, new string[] { });
									}
								}
								else
								{
									var now = DateTime.Now;

									// we have more than enough people - spec new joiners
									if (plrCnt > to)
									{
										var newestTime = DateTime.MinValue;
										string newestName = null;
										foreach (var u in b.Users)
										{
											if (u.SyncStatus != SyncStatuses.Synced && !u.IsSpectator && u.JoinTime > newestTime)
											{
												newestName = u.Name;
												newestTime = u.JoinTime;
											}
										}
										if (newestName != null)
										{
											ah.ComForceSpectator(TasSayEventArgs.Default, new[] { newestName }); // spec lat joiner
											ah.SayBattle(string.Format("Speccing {0} - joined last. Managing for max {1} players", newestName, to));
										}
									}

									var grace = spring.GameEnded.Subtract(spring.GameStarted).TotalSeconds < NormalGameGracePeriod ? ShortGameGracePeriod : NormalGameGracePeriod;
									if (!isReady && DateTime.Now.Subtract(spring.GameEnded).TotalSeconds > grace && (!isPlanetwars || allyCount == 2))
									{
										if (now.Subtract(lastRing).TotalSeconds > RingEvery)
										{
											// we ring them
											lastRing = now;
											ah.ComRing(TasSayEventArgs.Default, new string[] { });
										}

										var worstTime = DateTime.MaxValue;
										var worstName = "";
										foreach (var s in notReady)
										{
											// find longest offending player
											if (!problemSince.ContainsKey(s)) problemSince[s] = DateTime.Now;
											if (problemSince[s] < worstTime)
											{
												worstTime = problemSince[s];
												worstName = s;
											}
										}
										foreach (var s in new List<string>(problemSince.Keys))
										{
											// delete not offending plaeyrs
											if (!notReady.Contains(s)) problemSince.Remove(s);
										}

										if (worstName != "")
										{
											if (now.Subtract(worstTime).TotalSeconds > SpecForceAfter)
											{
												ah.ComForceSpectator(TasSayEventArgs.Default, new[] { worstName }); // spec longest offending person}
												ah.SayBattle(string.Format("Speccing {0} - has not readied for {1} seconds. Unspec when you can ready up.", worstName, SpecForceAfter));
												tas.Say(TasClient.SayPlace.User, worstName, "I forced you spectator, unspec when ready", false);
												if (now.Subtract(worstTime).TotalSeconds > KickAfter) ah.ComKick(TasSayEventArgs.Default, new[] { worstName }); // kick longest offending
												problemSince.Remove(worstName); // no more problem, specced/kicked him
											}
										}
									}
									else problemSince.Clear(); // all ready clear problems
								}
							}
							else
							{
								// not enough players, make sure we unlock and clear offenders
								problemSince.Clear();
							}
						}
					}
					else
					{
						// spring running, reset timer and delete offenders
						lastRing = DateTime.Now;
						problemSince.Clear();
					}
				}
				finally
				{
					timer.Start();
				}
			}
		}
	}
}