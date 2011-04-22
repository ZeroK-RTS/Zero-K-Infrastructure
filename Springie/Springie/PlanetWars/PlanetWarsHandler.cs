#region using

using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.ContentService;
using Springie.autohost;

#endregion

namespace Springie.PlanetWars
{
	public class PlanetWarsHandler: IDisposable
	{
		readonly AutoHost autoHost;
		readonly ContentService serv = new ContentService();
		readonly Spring spring;


		readonly TasClient tas;
		readonly Timer timer = new Timer();
		int timerCnt = 0;


		public PlanetWarsHandler(AutoHost autoHost, TasClient tas, AutoHostConfig config, Spring spring)
		{
			if (Debugger.IsAttached) serv.Url = "http://localhost:49576/ContentService.asmx";
			this.spring = spring;
			this.autoHost = autoHost;
			this.tas = tas;

			spring.SpringExited += spring_SpringExited;

			timer.Interval = 30000;
			timer.Elapsed += timer_Elapsed;
			timer.AutoReset = true;
			timer.Start();
		}


		public void Dispose()
		{
			timer.Stop();
			timer.Elapsed -= timer_Elapsed;
		}


		public void SpringExited()
		{
			try
			{
				//tas.Say();
			}
			catch (Exception ex)
			{
				autoHost.SayBattle("Error notifying game end:" + ex);
			}
		}


		public bool StartGame(TasSayEventArgs e)
		{
			return BalanceTeams();
		}

		public void UserJoined(string name)
		{
			try {}
			catch (Exception ex)
			{
				autoHost.SayBattle("PlanetWars error: " + ex);
			}
		}

		public bool BalanceTeams()
		{
			try
			{
				var userList =
					tas.MyBattle.Users.Where(x => !x.IsSpectator && x.SyncStatus == SyncStatuses.Synced).Select(
						x => new AccountTeam() { AccountID = x.LobbyUser.AccountID, Name = x.Name, AllyID = x.AllyNumber, TeamID = x.TeamNumber }).ToArray();

				if (userList.Length <= 1)
				{
					autoHost.SayBattle("Not enough players in game");
					return false;
				}

				var balance = serv.BalanceTeams(tas.UserName,
				                                tas.MyBattle.MapName,userList,
				                                AutohostMode.Planetwars);
				autoHost.SayBattle(balance.Message);
				if (balance.BalancedTeams != null)
				{
					foreach (var user in balance.BalancedTeams) tas.ForceTeam(user.Name, user.TeamID);
					foreach (var user in balance.BalancedTeams) tas.ForceAlly(user.Name, user.AllyID);
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				autoHost.SayBattle("Problem with PlanetWars:" + ex);
				Trace.TraceError(ex.ToString());
				return false;
			}
		}

		void VerifyMap()
		{
			try
			{
				var map = serv.GetRecommendedMap(tas.UserName, AutohostMode.Planetwars);
				if (map.MapName != null)
				{
					if (tas.MyBattle.MapName != map.MapName)
					{
						autoHost.ComMap(TasSayEventArgs.Default, map.MapName);
						autoHost.SayBattle(map.Message);
					}
				}
			}
			catch (Exception ex)
			{
				autoHost.SayBattle("Problem with PlanetWars:" + ex);
				Trace.TraceError(ex.ToString());
			}
		}

		void spring_SpringExited(object sender, EventArgs<bool> e)
		{
			VerifyMap();
		}

		void timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (spring.IsRunning) return;
			timer.Stop();
			timerCnt++;
			try
			{
				VerifyMap();
				if (timerCnt%3 == 0) BalanceTeams();
			}
			catch (Exception ex)
			{
				autoHost.SayBattle("Problem with PlanetWars:" + ex);
				Trace.TraceError(ex.ToString());
			}
			finally
			{
				timer.Start();
			}
		}
	}
}