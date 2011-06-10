using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
		NotifyBarContainer barContainer;
		readonly TasClient client;
		string engineVersionNeeded;

		bool isVisible;
		string lastBattleFounder;
		string lastScript;
		Battle previousBattle;

		readonly Random random = new Random();
		GenericBar reconnectBar;

		readonly Spring spring;
		bool suppressSideChangeEvent;
		readonly Timer timer = new Timer();
		public string CommShareWith { get; set; }


		/// <summary>
		/// singleton, dont use, internal for designer
		/// </summary>
		internal BattleBar()
		{
			InitializeComponent();
			Program.ToolTip.SetText(cbSpectate, "As a spectator you will not participate in the gameplay");
			Program.ToolTip.SetText(cbSide, "Choose the faction you wish to play.");

			client = Program.TasClient;
			spring = new Spring(Program.SpringPaths);
			var speech = new ChatToSpeech(spring);
			spring.SpringExited += (s, e) =>
				{
					client.ChangeMyUserStatus(isInGame:false);
					client.ChangeMyBattleStatus(ready: true);

					if (e.Data || IsHostGameRunning()) Program.MainWindow.InvokeFunc(CreateReconnectBar);
				};

			spring.SpringStarted += (s, e) =>
				{
					client.ChangeMyBattleStatus(ready: false);
					client.ChangeMyUserStatus(isInGame: true);
				};

			client.Rang += (s, e) => {

				MainWindow.Instance.NotifyUser("chat/battle", "Someone demands your attention in battle room!", true, true);
				AutoRespond();
			};

			client.BattleJoined += (s, e) =>
				{
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
					                                           					cbSide.Visible = mod.Sides.Length > 1;
																															lbSide.Visible = cbSide.Visible;
					                                           					suppressSideChangeEvent = false;
					                                           				}));
					                                           		}
					                                           	},
					                                           (ex) => { });

					Program.Downloader.GetResource(DownloadType.MAP, battle.MapName);
					Program.Downloader.GetResource(DownloadType.MOD, battle.ModName);
					var match = Regex.Match(battle.Title, "\\[engine([^\\]]+)\\].*");
					if (match.Success) engineVersionNeeded = match.Groups[1].Value;
					else engineVersionNeeded = client.ServerSpringVersion;
					if (engineVersionNeeded != Program.SpringPaths.SpringVersion) Program.Downloader.GetAndSwitchEngine(engineVersionNeeded);
					else engineVersionNeeded = null;

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
					             	TeamColor = Program.Conf.DefaultPlayerColorInt,
												IsReady = cbReady.Checked,
					             };
					if (status.SyncStatus == SyncStatuses.Synced && IsHostGameRunning()) Program.MainWindow.InvokeFunc(CreateReconnectBar);
					client.SendMyBattleStatus(status);
				};

			client.MyBattleStarted += (s, e) =>
				{
					try
					{
						if (client.MyBattleStatus.SyncStatus == SyncStatuses.Synced) if (Utils.VerifySpringInstalled()) lastScript = spring.StartGame(client, null, null, null);
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
							ChangeGuiSpectatorWithoutEvent(true);
					}
				};

			client.BattleClosed += (s, e) =>
				{
					CommShareWith = null;
					if (gameBox.Image != null) gameBox.Image.Dispose();
					gameBox.Image = null;
					cbSide.Visible = false;
					RefreshTooltip();
					Stop();
				};

			client.MyBattleEnded += (s, e) =>
				{
					var t = new DispatcherTimer();
					var tryCount = 0;
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
				};

			client.ConnectionLost += (s, e) =>
				{
					if (gameBox.Image != null) gameBox.Image.Dispose();
					gameBox.Image = null;
					cbSide.Visible = false;
					RefreshTooltip();
					Stop();
				};

			timer.Tick += (s, e) =>
				{
					if (client.IsLoggedIn)
					{
						if (WindowsApi.IdleTime.TotalMinutes > Program.Conf.IdleTime) client.ChangeMyUserStatus(isAway: true);
						else client.ChangeMyUserStatus(isAway: false);
						CheckMyBattle();
					}
				};
			timer.Interval = 2500;
			timer.Start();

			Program.BattleIconManager.BattleChanged += BattleIconManager_BattleChanged;

			picoChat.Font = new Font(Program.Conf.ChatFont.FontFamily, Program.Conf.ChatFont.Size*0.8f);
			picoChat.ShowHistory = false;
			picoChat.ShowJoinLeave = false;
			picoChat.HideScroll = true;

			BattleChatControl.BattleLine += (s, e) => picoChat.AddLine(e.Data);

			picoChat.MouseClick += (s, e) => NavigationControl.Instance.Path = "chat/battle";
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

		public void StartManualBattle(int battleID, string password)
		{
			Trace.TraceInformation("Joining battle {0}", battleID);
			Program.TasClient.LeaveBattle();
			if (!string.IsNullOrEmpty(password)) Program.TasClient.JoinBattle(battleID, password);
			else Program.TasClient.JoinBattle(battleID);
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

			Program.NotifySection.RemoveBar(this);
		}

		void AutoRespond()
		{
			if (client.MyBattle != null && client.MyBattleStatus != null && client.MyBattleStatus.SyncStatus != SyncStatuses.Synced)
			{
				var moddl = Program.Downloader.Downloads.FirstOrDefault(x => x.Name == client.MyBattle.ModName);
				var mapdl = Program.Downloader.Downloads.FirstOrDefault(x => x.Name == client.MyBattle.MapName);
				if (moddl != null && moddl.IsComplete != true)
				{
					client.Say(TasClient.SayPlace.Battle,
					           "",
					           string.Format("Mod download progress: {0}%, eta: {1}", Math.Round(moddl.TotalProgress), moddl.TimeRemaining),
					           true);
				}
				if (mapdl != null && mapdl.IsComplete != true)
				{
					client.Say(TasClient.SayPlace.Battle,
					           "",
					           string.Format("Map download progress: {0}%, eta: {1}", Math.Round(mapdl.TotalProgress), mapdl.TimeRemaining),
					           true);
				}
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
				if (Program.SpringScanner.HasResource(battle.MapName) && Program.SpringScanner.HasResource(battle.ModName) &&
				    (engineVersionNeeded == null || Program.SpringPaths.SpringVersion == engineVersionNeeded))
				{
					// if didnt have map and have now, set it
					newStatus.SyncStatus = SyncStatuses.Synced;
					if (IsHostGameRunning()) Program.MainWindow.InvokeFunc(CreateReconnectBar);
				}
			}

			// fix my id
			var sharePlayer = battle.Users.FirstOrDefault(x => x.Name == CommShareWith);
			if (sharePlayer != null) newStatus.TeamNumber = sharePlayer.TeamNumber;
			else if (battle.Users.Count(x => !x.IsSpectator && x.TeamNumber == currentStatus.TeamNumber) > 1) newStatus.TeamNumber = battle.GetFreeTeamID(client.UserName);

			newStatus.IsSpectator = cbSpectate.Checked;

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


		void ManualBattleStarted()
		{
			if (isVisible) Stop();
			isVisible = true;
			cbSpectate.Visible = true;
			cbSpectate.Checked = false;
			cbReady.Checked = true;
			Program.NotifySection.AddBar(this);
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


		void cbReady_CheckedChanged(object sender, EventArgs e)
		{
			cbReady.ImageIndex = cbReady.Checked ? 1 : 2;
		}

		void cbReady_Click(object sender, EventArgs e)
		{
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
			client.ChangeMyBattleStatus(side: cbSide.SelectedIndex);
		}

		void cbSide_VisibleChanged(object sender, EventArgs e)
		{
			lbSide.Visible = cbSide.Visible;
		}

		void cbSpectate_CheckedChanged(object sender, EventArgs e)
		{
			client.ChangeMyBattleStatus(spectate: cbSpectate.Checked);
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