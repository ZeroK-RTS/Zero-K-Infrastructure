using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Notifications
{
	public partial class BattleBar: UserControl, INotifyBar
	{
		bool Automatic
		{
			get { return cbAuto.Checked; }
			set
			{
				cbAuto.Checked = value;
				lbMin.Visible = value;
				numMinValue.Visible = value;
				Program.QuickMatchTracker.AdvertiseMySetup(null);				
			}
		}
		bool IsQuickPlayActive { get { return client.IsLoggedIn && !spring.IsRunning && isVisible && currentBattleMode == BattleMode.QuickMatch; } }
		NotifyBarContainer barContainer;
		readonly TasClient client;
		BattleMode currentBattleMode = BattleMode.Normal;
		int downloadFailedCounter;
		GenericBar followBar;
		string followedPlayer;

		string gameInfosDisplayText;
		readonly Dictionary<int, DateTime> ignoredBattles = new Dictionary<int, DateTime>();

		bool isVisible;
		DateTime lastAlert = DateTime.MinValue;
		DateTime lastBattleSwitch = DateTime.MinValue;
		string lastScript;
		Battle previousBattle;
		GenericBar quickMatchBar;
    string quickMatchFilter = null;
		readonly Random random = new Random();
		GenericBar reconnectBar;

		readonly Spring spring;
		bool suppressSideChangeEvent;
		readonly Timer timer = new Timer();
		string lastBattleFounder;
	  string engineVersionNeeded;
	  public string CommShareWith { get; set; }


	
    /// <summary>
    /// singleton, dont use, internal for designer
    /// </summary>
  	internal BattleBar()
		{
			InitializeComponent();
			Program.ToolTip.SetText(numMinValue, "Choose the minimum number of players you want in your game.");
			Program.ToolTip.SetText(lbMin,
			                        "Choose the minimum number of players you want in your game.\nYou will only ready up if battle has at least that many players.");
			Program.ToolTip.SetText(cbSpectate, "As a spectator you will not participate in the gameplay");
			Program.ToolTip.SetText(cbSide, "Choose the faction you wish to play.");
			Program.ToolTip.SetText(cbAuto, "Automatically ready-up when there is enough players");

			client = Program.TasClient;
			spring = new Spring(Program.SpringPaths);
			spring.SpringExited += (s, e) =>
				{
					client.ChangeMyUserStatus(false, false);


					if (e.Data || IsHostGameRunning()) Program.MainWindow.InvokeFunc(CreateReconnectBar);
				};

			spring.SpringStarted += (s, e) =>
				{
					client.ChangeMyBattleStatus(ready: false);
					client.ChangeMyUserStatus(isInGame: true);

				};

			client.Rang += (s, e) =>
				{
					if (Automatic) AutoRespond();
					else
					{
						MainWindow.Instance.NotifyUser("chat/battle", "Someone demands your attention in battle room!", true, true);
					}
				};
			client.Said += (s, e) =>
				{
					if (Automatic && !e.IsEmote && (e.Place == TasSayEventArgs.Places.Battle) && e.UserName != client.UserName && e.Text == "!specafk")
					{
						if (client.MyBattleStatus == null || (!client.MyBattleStatus.IsSpectator && !client.MyBattleStatus.IsReady))
						{
							if (IsQuickPlayActive)
							{
								Trace.TraceInformation("Leaving battle because of !specafk");
								client.LeaveBattle();
							}
							else
							{
								Trace.TraceInformation("Specced because of !specafk");
								client.ChangeMyBattleStatus(spectate:true);
							}
						}
					}
				};

			client.BattleJoined += (s, e) =>
				{
					downloadFailedCounter = 0;
					if (!isVisible) ManualBattleStarted();
					//client.ChangeMyUserStatus(false, false);
					var battle = client.MyBattle;
					lastBattleFounder = battle.Founder;
					Program.SpringScanner.MetaData.GetModAsync(battle.ModName,
					                                           (mod) =>
					                                           	{
					                                           		if (!Program.CloseOnNext)
					                                           		{
					                                           			Program.MainWindow.Dispatcher.Invoke(new Action(() =>
					                                           				{
					                                           					var previousSide = cbSide.SelectedItem != null ? cbSide.SelectedItem.ToString() : null;
					                                           					cbSide.Items.Clear();
					                                           					var cnt = 0;
					                                           					foreach (var side in mod.Sides) cbSide.Items.Add(new SideItem(side, mod.SideIcons[cnt++]));
					                                           					var pickedItem = cbSide.Items.OfType<SideItem>().FirstOrDefault(x => x.Side == previousSide);

					                                           					suppressSideChangeEvent = true;
					                                           					if (pickedItem != null) cbSide.SelectedItem = pickedItem;
					                                           					else cbSide.SelectedIndex = random.Next(cbSide.Items.Count);
					                                           					cbSide.Visible = true;
					                                           					suppressSideChangeEvent = false;
					                                           				}));
					                                           		}
					                                           	},
					                                           (ex) => { });

					Program.Downloader.GetResource(DownloadType.MAP, battle.MapName);
					Program.Downloader.GetResource(DownloadType.MOD, battle.ModName);
				  var match = Regex.Match(battle.Title, "\\[engine([^\\]]+)\\].*");
          if (match.Success)
          {
            engineVersionNeeded = match.Groups[1].Value;
          } else
          {
            engineVersionNeeded = client.ServerSpringVersion;
          }
          if (engineVersionNeeded != Program.SpringPaths.SpringVersion) {
            Program.Downloader.GetAndSwitchEngine(engineVersionNeeded);
          } else engineVersionNeeded = null;

					if (battle != previousBattle)
					{
						previousBattle = battle;
						if (gameBox.Image != null) gameBox.Image.Dispose();
						gameBox.Image = new Bitmap(BattleIcon.Width, BattleIcon.Height);
						using (var g = Graphics.FromImage(gameBox.Image))
						{
							g.FillRectangle(Brushes.White, 0, 0, BattleIcon.Width, BattleIcon.Height);
							var bi = Program.BattleIconManager.GetBattleIcon(battle.BattleID);
							g.DrawImageUnscaled(bi.Image, 0, 0);
						}
						gameBox.Invalidate();
					}
					RefreshTooltip();
				};

			cbSide.DrawMode = DrawMode.OwnerDrawFixed;
			cbSide.DrawItem += cbSide_DrawItem;

			client.MyBattleHostExited += (s, e) => RemoveReconnectBar();

			client.MyBattleMapChanged += (s, e) =>
				{
					if (client.MyBattle != null && !Program.SpringScanner.HasResource(client.MyBattle.MapName))
					{
						client.ChangeMyBattleStatus(syncStatus: SyncStatuses.Unsynced);
						Program.Downloader.GetResource(DownloadType.MAP, client.MyBattle.MapName);
					}
					RefreshTooltip();
				};

			client.RequestBattleStatus += (s, e) =>
				{
					var battle = client.MyBattle;

					var alliance = Enumerable.Range(0, TasClient.MaxAlliances - 1).FirstOrDefault(allyTeam => !battle.Users.Any(user => user.AllyNumber == allyTeam));
					var team = battle.GetFreeTeamID(client.UserName);

	/*				if (battle)
{
 * 			var b = tas.MyBattle;
return hostedMod.MissionSlots.Where(x => x.IsHuman).OrderByDescending(x => x.IsRequired).Where(
x => !b.Users.Any(y => y.AllyNumber == x.AllyID && y.TeamNumber == x.TeamID && !y.IsSpectator));

	var slot = GetFreeSlots().FirstOrDefault();
	if (slot != null)
	{
		tas.ForceAlly(u.Name, slot.AllyID);
		tas.ForceTeam(u.Name, slot.TeamID);
	}
	else tas.ForceSpectator(u.Name);
}*/
					var status = new UserBattleStatus
					             {
					             	AllyNumber = alliance,
					             	TeamNumber = team,
					             	SyncStatus =
					             		Program.SpringScanner.HasResource(battle.ModName) && Program.SpringScanner.HasResource(battle.MapName)
					             			? SyncStatuses.Synced
					             			: SyncStatuses.Unsynced,
					             	IsSpectator = cbSpectate.Checked,
					             	Side = cbSide.SelectedIndex >= 0 ? cbSide.SelectedIndex : 0,
					             	TeamColor = Program.Conf.DefaultPlayerColorInt
					             };
					if (status.SyncStatus == SyncStatuses.Synced && IsHostGameRunning()) Program.MainWindow.InvokeFunc(CreateReconnectBar);
					client.SendMyBattleStatus(status);
				};

			client.MyBattleStarted += (s, e) =>
				{
					try
					{
						if (client.MyBattleStatus.SyncStatus == SyncStatuses.Synced)
						{
              if (Utils.VerifySpringInstalled()) lastScript = spring.StartGame(client, null, null, null);
						}
						else if (IsQuickPlayActive) client.LeaveBattle(); // battle started without me, lets quit!
					}
					catch (Exception ex)
					{
						MessageBox.Show("Error starting spring: " + ex.Message);
					}
					RefreshTooltip();
				};

			client.BattleMyUserStatusChanged += (s, e) =>
				{
					if (client.MyBattleStatus != null)
					{
						cbReady.Checked = client.MyBattleStatus.IsReady;
						barContainer.btnDetail.Enabled = client.MyBattleStatus.IsReady && client.MyBattleStatus.SyncStatus == SyncStatuses.Synced &&
						                                 !client.ExistingUsers[client.MyBattle.Founder].IsInGame;

						if (client.MyBattleStatus.IsSpectator && !cbSpectate.Checked) // i was spectated
						{
							if (IsQuickPlayActive)
							{
								client.Say(TasClient.SayPlace.Battle,
								           "",
								           "QuickMatching ( http://zero-k.info/lobby ) - leaving because i was specced, I will rejoin later",
								           true);
								client.LeaveBattle(); // we were specced, get out of there}
							}
							else if (currentBattleMode == BattleMode.Normal) ChangeGuiSpectatorWithoutEvent(true);
						}
					}
				};

			client.BattleClosed += (s, e) =>
				{
					CommShareWith = null;
					if (gameBox.Image != null) gameBox.Image.Dispose();
					gameBox.Image = null;
					cbSide.Visible = false;
					RefreshTooltip();
					if (currentBattleMode == BattleMode.QuickMatch) ignoredBattles[e.Data.BattleID] = DateTime.Now;
					if (currentBattleMode == BattleMode.Normal) Stop();
					else JoinBestBattle();
				};

			client.MyBattleEnded += (s, e) =>
				{
					if (currentBattleMode == BattleMode.Normal)
					{
						var t = new DispatcherTimer();
						int tryCount = 0;
						t.Interval = TimeSpan.FromSeconds(1);
						t.Tick += (s2, e2) =>
							{
								tryCount++;
								if (tryCount > 15) t.Stop();
								else if (client.IsLoggedIn && client.MyBattle == null)
								{
									var bat = client.ExistingBattles.Values.FirstOrDefault(x => x.Founder == lastBattleFounder && !x.IsPassworded);
									if (bat != null)
									{
										ActionHandler.JoinBattle(bat.BattleID, null);
										t.Stop();
									}
								}
							};
						t.Start();
					}
				};


			client.ConnectionLost += (s, e) =>
				{
					if (gameBox.Image != null) gameBox.Image.Dispose();
					gameBox.Image = null;
					cbSide.Visible = false;
					RefreshTooltip();
					if (currentBattleMode == BattleMode.Normal) Stop();
				};

			timer.Tick += (s, e) =>
				{
					if (client.IsLoggedIn)
					{
						if (WindowsApi.IdleTime.TotalMinutes > Program.Conf.IdleTime) client.ChangeMyUserStatus(isAway: true);
						else client.ChangeMyUserStatus(isAway: false);

						if (isVisible && Automatic && client.MyBattle != null && !cbSpectate.Checked && WindowsApi.IdleTime.TotalMinutes > Program.Conf.IdleTime)
						{
							ChangeGuiSpectatorWithoutEvent(true);
							client.ChangeMyBattleStatus(spectate:true);
							
							WarningBar.DisplayWarning("User was away for more than " + Program.Conf.IdleTime + " minutes: battle search changed to spectator.");
							MainWindow.Instance.NotifyUser("chat/battle", "Away From Keyboard - setting mode to spectator", true, true);
						}
						else
						{
							CheckMyBattle();
							JoinBestBattle();
						}
					}
				};
			timer.Interval = 5000;
			timer.Start();

			Program.BattleIconManager.BattleChanged += BattleIconManager_BattleChanged;

			picoChat.Font = new Font(Program.Conf.ChatFont.FontFamily, Program.Conf.ChatFont.Size*0.8f);
			picoChat.ShowHistory = false;
			picoChat.ShowJoinLeave = false;
			picoChat.HideScroll = true;

			BattleChatControl.BattleLine += (s, e) => picoChat.AddLine(e.Data);

			picoChat.MouseClick += (s, e) =>NavigationControl.Instance.Path = "chat/battle";
		}

		/// <summary>
		/// Changes user's desired spectator state of battle
		/// </summary>
		/// <param name="state">new desired state</param>
		/// <returns>true if change allowed</returns>
		public bool ChangeDesiredSpectatorState(bool state)
		{
			if (cbSpectate.Enabled && cbSpectate.Visible)
			{
				ChangeGuiSpectatorWithoutEvent(state);
				return true;
			}
			return false;
		}

		public static bool DownloadFailed(string name)
		{
			if (!Program.SpringScanner.HasResource(name))
			{
				var down = Program.Downloader.Downloads.FirstOrDefault(x => x.Name == name);
				if (down == null || down.IsComplete == false || down.IsAborted) return true;
			}
			return false;
		}

		/// <summary>
		/// gets current quickmatch setup
		/// </summary>
		/// <returns></returns>
		public QuickMatchInfo GetQuickMatchInfo()
		{
			if (!isVisible) return new QuickMatchInfo();
			return new QuickMatchInfo(currentBattleMode == BattleMode.Follow ? followedPlayer : gameInfosDisplayText,
			                          Automatic ? (int)numMinValue.Value : 0,
			                          currentBattleMode,
			                          cbSpectate.Checked);
		}

		public void StartFollow(string name)
		{
			if (isVisible) Stop();
			Trace.TraceInformation("Starting to follow {0}", name);
			currentBattleMode = BattleMode.Follow;
			isVisible = true;
			followedPlayer = name;
			ChangeGuiSpectatorWithoutEvent(false);
			cbSpectate.Visible = false;
			numMinValue.Visible = false;
			lbMin.Visible = false;

			Program.QuickMatchTracker.AdvertiseMySetup(null);
			Program.NotifySection.AddBar(this);

			followBar = new GenericBar() { Text = string.Format("Automatically following {0}", name) };
			Program.NotifySection.AddBar(followBar);
			followBar.BarContainer.btnDetail.Text = "Follow";
			followBar.BarContainer.btnDetail.Enabled = false;
			Program.ToolTip.SetText(followBar.BarContainer.btnStop, "Stop Following");
			followBar.CloseButtonClicked += (s, e) => StartManualBattle(client.MyBattleID, null);
		}

		public void StartManualBattle(int battleID, string password)
		{
			Trace.TraceInformation("Joining battle {0}", battleID);
			Program.BattleBar.currentBattleMode = BattleMode.Normal;
			Program.TasClient.LeaveBattle();
			if (!string.IsNullOrEmpty(password)) Program.TasClient.JoinBattle(battleID, password);
			else Program.TasClient.JoinBattle(battleID);
		}


		public void StartQuickMatch(string filter)
		{
			if (isVisible) Stop();
			Trace.TraceInformation("Starting quickmatching");
      this.quickMatchFilter = filter;

			currentBattleMode = BattleMode.QuickMatch;
			cbSpectate.Visible = true;
			ChangeGuiSpectatorWithoutEvent(false);
			numMinValue.Visible = true;
			lbMin.Visible = true;
			Automatic = true;

			gameInfosDisplayText = filter;
			cbSide.Items.Clear();

			isVisible = true;

			Program.QuickMatchTracker.AdvertiseMySetup(null);
			Program.NotifySection.AddBar(this);

			quickMatchBar = new GenericBar() { Text = "QuickMatching - automatically looking for proper game of " + gameInfosDisplayText };
			Program.NotifySection.AddBar(quickMatchBar);
			quickMatchBar.BarContainer.btnDetail.Text = "QuickMatch";
			Program.ToolTip.SetText(quickMatchBar.BarContainer.btnDetail, "Pick another battle");
			Program.ToolTip.SetText(quickMatchBar.BarContainer.btnStop, "Stop QuickMatching");
			quickMatchBar.DetailButtonClicked += (s, e) => StartQuickMatch(quickMatchFilter); // todo show textbox on quickmatch bar and allow change
			quickMatchBar.CloseButtonClicked += (s, e) => StartManualBattle(client.MyBattleID, null);
		}

		public static bool StillDownloading(string name)
		{
			return !Program.SpringScanner.HasResource(name) && !DownloadFailed(name);
		}

		public void Stop()
		{
			Trace.TraceInformation("Closing current battle");
			isVisible = false;
			client.LeaveBattle();

			Program.QuickMatchTracker.AdvertiseMySetup(null);
			Program.NotifySection.RemoveBar(this);
			if (quickMatchBar != null)
			{
				Program.NotifySection.RemoveBar(quickMatchBar);
				quickMatchBar.UnsubscribeEvents(this);
			}

			if (followBar != null)
			{
				Program.NotifySection.RemoveBar(followBar);
				followBar.UnsubscribeEvents(this);
			}
		}

		void AutoRespond()
		{
			if (IsQuickPlayActive)
			{
				client.Say(TasClient.SayPlace.Battle,
				           "",
				           string.Format(
				           	"QuickMatching ( http://zero-k.info/lobby ), waiting for {0} players. Spec me with !specafk if you want to play with less.",
				           	(int)numMinValue.Value),
				           false);
			}
			else
			{
				client.Say(TasClient.SayPlace.Battle,
				           "",
				           string.Format(
				           	"Using Zero-K ( http://zero-k.info/lobby ), waiting for {0} players. Spec me with !specafk if you want to play with less.",
				           	(int)numMinValue.Value),
				           false);
			}

			if (client.MyBattle != null && client.MyBattleStatus != null && client.MyBattleStatus.SyncStatus != SyncStatuses.Synced)
			{
				var moddl = Program.Downloader.Downloads.FirstOrDefault(x => x.Name == client.MyBattle.ModName);
				var mapdl = Program.Downloader.Downloads.FirstOrDefault(x => x.Name == client.MyBattle.MapName);
				if (moddl != null && moddl.IsComplete != true) client.Say(TasClient.SayPlace.Battle, "", string.Format("Mod download progress: {0}%", Math.Round(moddl.TotalProgress)), true);
				if (mapdl != null && mapdl.IsComplete != true) client.Say(TasClient.SayPlace.Battle, "", string.Format("Map download progress: {0}%", Math.Round(mapdl.TotalProgress)), true);
			}
		}

		void ChangeGuiSpectatorWithoutEvent(bool newState)
		{
			cbSpectate.CheckedChanged -= cbSpectate_CheckedChanged;
			cbSpectate.Checked = newState;
			cbSpectate.CheckedChanged += cbSpectate_CheckedChanged;
		}


		void CheckMyBattle()
		{
			var battle = client.MyBattle;
			var currentStatus = client.MyBattleStatus;
			if (battle == null || currentStatus == null) return;

			var newStatus = currentStatus.Clone();

			if (currentStatus.SyncStatus != SyncStatuses.Synced)
			{
				if (Program.SpringScanner.HasResource(battle.MapName) && Program.SpringScanner.HasResource(battle.ModName) && (engineVersionNeeded == null || Program.SpringPaths.SpringVersion == engineVersionNeeded))
				{
					// if didnt have map and have now, set it
					newStatus.SyncStatus = SyncStatuses.Synced;
					if (IsHostGameRunning()) Program.MainWindow.InvokeFunc(CreateReconnectBar);
				}
				else
				{
					if (currentBattleMode == BattleMode.QuickMatch && (DownloadFailed(battle.MapName) || DownloadFailed(battle.ModName)))
					{
						downloadFailedCounter++;
						if (downloadFailedCounter > 3)
						{
							Trace.TraceInformation("Leaving battle because mod/map download failed");
							client.LeaveBattle();
						}
					}
				}
			}

			// fix my id
			var sharePlayer = battle.Users.FirstOrDefault(x => x.Name == CommShareWith);
			if (sharePlayer != null) newStatus.TeamNumber = sharePlayer.TeamNumber;
			else if (battle.Users.Count(x => !x.IsSpectator && x.TeamNumber == currentStatus.TeamNumber) > 1)
			{
				newStatus.TeamNumber = battle.GetFreeTeamID(client.UserName);
			}

			if (currentBattleMode == BattleMode.Follow) // ally followed player
			{
				var buddy = battle.Users.FirstOrDefault(x => x.Name == followedPlayer);
				if (buddy != null)
				{
					newStatus.AllyNumber = buddy.AllyNumber;
					newStatus.IsSpectator = buddy.IsSpectator;
					newStatus.IsReady = buddy.IsReady;
				}
			}
			else // quickmatch or normal mode
			{
				newStatus.IsSpectator = cbSpectate.Checked;
				if (Automatic)
				{
					newStatus.IsReady = !spring.IsRunning && battle.NonSpectatorCount >= numMinValue.Value;

					if (DateTime.Now.Subtract(lastAlert).TotalSeconds > 120)
					{
						var idlerCount = GetQuickMatchIdlerCount(battle, (int)numMinValue.Value);
						// gets number of players unwilling to play at my min value
						if (battle.NonSpectatorCount - idlerCount >= numMinValue.Value && !spring.IsRunning && !client.ExistingUsers[battle.Founder].IsInGame)
						{
							// even without idlers i can still play
							MainWindow.Instance.NotifyUser("chat/battle", "Battle has enough people to start!", true, false);
							lastAlert = DateTime.Now;
						}
					}
				}
			}

			if (newStatus != currentStatus) client.SendMyBattleStatus(newStatus);
		}

		void CreateReconnectBar()
		{
			RemoveReconnectBar();
			reconnectBar = new GenericBar()
			               {
			               	DetailButtonLabel = "Rejoin",
			               	Text = "Wanna (re)join running game? Click left to connect!  WARNING: THIS WILL REPLAY GAME - IT WILL LAG FOR SEVERAL MINUTES"
			               };
			reconnectBar.DetailButtonClicked += (s2, e2) =>
				{
          if (Utils.VerifySpringInstalled())
          {
            if (client.MyBattle != null) spring.StartGame(client, null, null, null);
            else spring.StartGame(client, null, null, lastScript);
            Program.NotifySection.RemoveBar(reconnectBar);
          }
				};
			Program.NotifySection.AddBar(reconnectBar);
		}

		/// <summary>
		/// Gets number of players who will refuse to play game of this size
		/// </summary>
		int GetQuickMatchIdlerCount(Battle battle, int gameSize)
		{
			var count = 0;
			foreach (var user in battle.Users.Where(x => !x.IsSpectator))
			{
				var info = Program.QuickMatchTracker.GetQuickMatchInfo(user.Name);
				if (info != null && info.IsEnabled && info.CurrentMode != BattleMode.Follow && info.MinPlayers > gameSize) count++;
			}
			return count;
		}

		bool IsHostGameRunning()
		{
			if (client != null)
			{
				var bat = client.MyBattle;
				if (bat != null)
				{
					User founder;
					return client.ExistingUsers.TryGetValue(bat.Founder, out founder) && founder.IsInGame;
				}
			}
			return false;
		}


		void JoinBestBattle()
		{
			if (!isVisible || spring.IsRunning || !client.IsLoggedIn || currentBattleMode == BattleMode.Normal) return;
			if (DateTime.Now.Subtract(lastBattleSwitch).TotalSeconds < 15) return;

			Battle bat = null;
			if (currentBattleMode == BattleMode.QuickMatch) bat = PickQuickMatchBattle();
			else if (currentBattleMode == BattleMode.Follow)
			{
				User user;
				if (client.ExistingUsers.TryGetValue(followedPlayer, out user) && user.IsInBattleRoom) bat = client.ExistingBattles.Values.FirstOrDefault(x => x.Users.Any(y => y.Name == followedPlayer));
			}

			if (bat != null)
			{
				if (bat != client.MyBattle)
				{
					// if we are still downloading, and using quickmatch, dont change game
					if ((currentBattleMode == BattleMode.QuickMatch && client.MyBattle != null) &&
					    (StillDownloading(client.MyBattle.MapName) || StillDownloading(client.MyBattle.ModName))) return;

					// current players are capable of starting game and we meet limit
					if (currentBattleMode == BattleMode.QuickMatch && client.MyBattle != null &&
					    client.MyBattle.NonSpectatorCount - GetQuickMatchIdlerCount(client.MyBattle, (int)numMinValue.Value) >= numMinValue.Value) return;

					lastBattleSwitch = DateTime.Now;
					if (client.MyBattle != null) client.LeaveBattle();
					Trace.TraceInformation("Autojoining better battle {0}", bat.BattleID);
					client.JoinBattle(bat.BattleID);
				}
			}
			else if (currentBattleMode == BattleMode.Follow) client.LeaveBattle(); // user not found lets go away
		}

		void ManualBattleStarted()
		{
			if (isVisible) Stop();
			currentBattleMode = BattleMode.Normal;
			isVisible = true;
			cbSpectate.Visible = true;
			cbSpectate.Checked = false;
			numMinValue.Visible = true;
			lbMin.Visible = true;

			Program.QuickMatchTracker.AdvertiseMySetup(null);
			Program.NotifySection.AddBar(this);
		}

		Battle PickQuickMatchBattle()
		{
			// remove old ignored battles entries
			foreach (var bid in ignoredBattles.Where(x => DateTime.Now.Subtract(x.Value).TotalSeconds > 90).Select(x => x.Key).ToList()) ignoredBattles.Remove(bid);

			var mv = 0;

			if (client.MyBattle != null)
			{
				User founder;
				if (client.ExistingUsers.TryGetValue(client.MyBattle.Founder, out founder) && !founder.IsInGame) mv = client.MyBattle.NonSpectatorCount - GetQuickMatchIdlerCount(client.MyBattle, (int)numMinValue.Value);
			}
			Battle bat = null;
			foreach (var b in BattleList.BattleWordFilter(client.ExistingBattles.Values, quickMatchFilter))
			{
				var otherPlayerCount = b.NonSpectatorCount;
				if (b == client.MyBattle && client.MyBattleStatus != null && !client.MyBattleStatus.IsSpectator) otherPlayerCount--; // if its my battle and im not ready dont count self
				if (!ignoredBattles.ContainsKey(b.BattleID) && b.NonSpectatorCount < b.MaxPlayers && !b.IsLocked && !b.IsReplay && !client.ExistingUsers[b.Founder].IsInGame && b.Password == "*" && b.MaxPlayers >= numMinValue.Value && otherPlayerCount >= mv &&
				    (Program.SpringScanner.HasResource(b.ModName) || Program.Downloader.PackageDownloader.GetByInternalName(b.ModName) != null))
				{
					mv = b.NonSpectatorCount;
					bat = b;
				}
			}
			return bat;
		}

		void RefreshTooltip()
		{
			var bat = client.MyBattle;
			if (bat != null) Program.ToolTip.SetBattle(gameBox, bat.BattleID);
			else Program.ToolTip.SetText(gameBox, null);
		}

		void RemoveReconnectBar()
		{
			if (reconnectBar != null)
			{
				reconnectBar.UnsubscribeEvents(this);
				Program.NotifySection.RemoveBar(reconnectBar);
				reconnectBar = null;
			}
		}

		public Control GetControl()
		{
			return this;
		}

		public void AddedToContainer(NotifyBarContainer container)
		{
			barContainer = container;
			container.btnDetail.Image = Resources.Battle;
			container.btnDetail.Text = "Start";
			Program.ToolTip.SetText(container.btnDetail, "Start battle");
			Program.ToolTip.SetText(container.btnStop, "Quit battle");
		}

		public void CloseClicked(NotifyBarContainer container)
		{
			Stop();
		}

		public void DetailClicked(NotifyBarContainer container)
		{
			NavigationControl.Instance.Path = "chat/battle";
			client.Say(TasClient.SayPlace.Battle, "", "!start", false);
		}

		void BattleIconManager_BattleChanged(object sender, EventArgs<BattleIcon> e)
		{
			if (e.Data.Battle == Program.TasClient.MyBattle)
			{
				if (gameBox.Image == null) gameBox.Image = new Bitmap(BattleIcon.Width, BattleIcon.Height);
				using (var g = Graphics.FromImage(gameBox.Image))
				{
					g.FillRectangle(Brushes.White, 0, 0, BattleIcon.Width, BattleIcon.Height);
					g.DrawImageUnscaled(e.Data.Image, 0, 0);
					gameBox.Invalidate();
				}
			}
		}

		void QuickMatchControl_Load(object sender, EventArgs e) {}

		void cbAuto_CheckStateChanged(object sender, EventArgs e)
		{
			if (cbAuto.CheckState == CheckState.Checked) Automatic = true;
			else if (cbAuto.CheckState == CheckState.Unchecked) Automatic = false;
		}

		void cbReady_CheckedChanged(object sender, EventArgs e)
		{
			cbReady.ImageIndex = cbReady.Checked ? 1 : 2;
		}

		void cbReady_Click(object sender, EventArgs e)
		{
			Automatic = false;
			if (client != null && client.MyBattle != null) client.ChangeMyBattleStatus(ready: cbReady.Checked);
		}

		void cbSide_DrawItem(object sender, DrawItemEventArgs e)
		{
			e.DrawBackground();
			e.DrawFocusRectangle();
			if (e.Index < 0 || e.Index >= cbSide.Items.Count) return;
			var item = cbSide.Items[e.Index] as SideItem;
			if (item != null)
			{
				if (item.Image != null) e.Graphics.DrawImage(item.Image, e.Bounds.Left, e.Bounds.Top, 16, 16);
				TextRenderer.DrawText(e.Graphics, item.Side, cbSide.Font, new Point(e.Bounds.Left + 16, e.Bounds.Top), cbSide.ForeColor);
			}
			else TextRenderer.DrawText(e.Graphics, cbSide.Items[e.Index].ToString(), cbSide.Font, new Point(e.Bounds.Left, e.Bounds.Top), cbSide.ForeColor);
		}

		void cbSide_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (suppressSideChangeEvent) return;
			if (!client.IsLoggedIn) return;

			var status = client.MyBattleStatus;
			if (status == null) return;
			client.ChangeMyBattleStatus(side:cbSide.SelectedIndex);
		}

		void cbSide_VisibleChanged(object sender, EventArgs e)
		{
			lbSide.Visible = cbSide.Visible;
		}

		void cbSpectate_CheckedChanged(object sender, EventArgs e)
		{
			client.ChangeMyBattleStatus(spectate:cbSpectate.Checked);
			Program.QuickMatchTracker.AdvertiseMySetup(null);
			if (client.IsConnected && client.IsLoggedIn && client.MyBattle != null && !cbSpectate.Checked) cbSide.Visible = true;
			else cbSide.Visible = false;
		}

		void numMinValue_ValueChanged(object sender, EventArgs e)
		{
			Program.QuickMatchTracker.AdvertiseMySetup(null);
		}
	}


	class SideItem
	{
		public Image Image;
		public string Side;

		public SideItem(string side, byte[] image)
		{
			Side = side;
			if (image != null && image.Length > 0) Image = Image.FromStream(new MemoryStream(image));
			else Image = null;
		}

		public override string ToString()
		{
			return Side;
		}
	}
}