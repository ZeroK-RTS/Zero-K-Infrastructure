using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using ZkData.UnitSyncLib;
using ZeroKLobby.Lines;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
	class BattleChatControl: ChatControl
	{
        private bool finishLoad = false; //wait for element to initialize before manipulate them in OnResize()
		Image minimap;
		readonly PictureBox minimapBox;
        readonly ZeroKLobby.Controls.MinimapFuncBox minimapFuncBox;
		Size minimapSize;
        List<MissionSlot> missionSlots;
		public static event EventHandler<EventArgs<IChatLine>> BattleLine = delegate { };


	    public BattleChatControl(): base("Battle")
		{
			Program.TasClient.Said += TasClient_Said;
			Program.TasClient.BattleJoined += TasClient_BattleJoined;
			Program.TasClient.BattleUserLeft += TasClient_BattleUserLeft;
			Program.TasClient.BattleUserJoined += TasClient_BattleUserJoined;
			Program.TasClient.BattleUserStatusChanged += TasClient_BattleUserStatusChanged;
			Program.TasClient.BattleClosed += (s, e) => Reset();
			Program.TasClient.ConnectionLost += (s, e) => Reset();
			Program.TasClient.BattleBotAdded += (s, e) => SortByTeam();
			Program.TasClient.BattleBotRemoved += (s, e) => SortByTeam();
			Program.TasClient.BattleBotUpdated += (s, e) => SortByTeam();
			Program.TasClient.BattleMapChanged += TasClient_BattleMapChanged;
			Program.TasClient.StartRectAdded += (s, e) => DrawMinimap();
			Program.TasClient.StartRectRemoved += (s, e) => DrawMinimap();
			Program.ModStore.ModLoaded += ModStoreModLoaded;


			if (Program.TasClient.MyBattle != null) foreach (var user in Program.TasClient.MyBattle.Users.Values) AddUser(user.Name);
			ChatLine += (s, e) => { if (Program.TasClient.IsLoggedIn) Program.TasClient.Say(SayPlace.Battle, null, e.Data, false); };
			playerBox.IsBattle = true;

            minimapFuncBox = new ZeroKLobby.Controls.MinimapFuncBox();

			minimapBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage };
			minimapBox.Cursor = Cursors.Hand;
			minimapBox.Click +=
				(s, e) => { if (Program.TasClient.MyBattle != null) Program.MainWindow.navigationControl.Path = string.Format("{1}/Maps/DetailName?name={0}", Program.TasClient.MyBattle.MapName, GlobalConst.BaseSiteUrl); };

            //playerBoxSearchBarContainer.Controls.Add(battleFuncBox);
            playerListMapSplitContainer.Panel2.Controls.Add(minimapFuncBox);
            minimapFuncBox.mapPanel.Controls.Add(minimapBox);

            minimapFuncBox.Visible = false; //hide button before joining game 
            playerListMapSplitContainer.Panel2Collapsed = false; //show mappanel when in battleroom
            finishLoad = true;
		}

		protected override void Dispose(bool disposing)
		{
			if (Program.TasClient != null) Program.TasClient.UnsubscribeEvents(this);
			base.Dispose(disposing);
		}

		public override void AddLine(IChatLine line)
		{
			base.AddLine(line);
			BattleLine(this, new EventArgs<IChatLine>(line));
		}

        protected override void OnLoad(EventArgs ea)
		{
			base.OnLoad(ea);
		}


		// todo: check if this is called when joining twice the same mission

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
            if (finishLoad)
            {
                DpiMeasurement.DpiXYMeasurement(this);
                minimapFuncBox.minimapSplitContainer1.SplitterDistance = Math.Min(DpiMeasurement.ScaleValueY(23),minimapFuncBox.minimapSplitContainer1.Height); //always show button fully
                DrawMinimap();
            }
		}

		public override void Reset()
		{
            playerListMapSplitContainer.Panel2Collapsed = false;
            minimapFuncBox.Visible = true; //show button when joining game 
			base.Reset();
			missionSlots = null;
			minimapBox.Image = null;
			minimap = null;
			Program.ToolTip.Clear(minimapBox);
		}

		protected override void SortByTeam() {
			if (filtering || Program.TasClient.MyBattle == null) return;

			var newList = new List<PlayerListItem>();

			foreach (var us in PlayerListItems) newList.Add(us);

            
			if (missionSlots != null)
			{
				foreach (var slot in missionSlots.Where(s => s.IsHuman))
				{
					var playerListItem =
						newList.FirstOrDefault(p => p.UserBattleStatus != null && !p.UserBattleStatus.IsSpectator && p.UserBattleStatus.TeamNumber == slot.TeamID);
					if (playerListItem == null) newList.Add(new PlayerListItem { SlotButton = slot.TeamName, MissionSlot = slot });
					else playerListItem.MissionSlot = slot;
				}
			}

			var nonSpecs = PlayerListItems.Where(p => p.UserBattleStatus != null && !p.UserBattleStatus.IsSpectator);
			var existingTeams = nonSpecs.GroupBy(i => i.UserBattleStatus.AllyNumber).Select(team => team.Key).ToList();

			foreach (var bot in Program.TasClient.MyBattle.Bots.Values)
			{
				var missionSlot = GetSlotByTeamID(bot.TeamNumber);
				newList.Add(new PlayerListItem
				            { BotBattleStatus = bot, SortCategory = bot.AllyNumber * 2 + 1 + (int)PlayerListItem.SortCats.Uncategorized, AllyTeam = bot.AllyNumber, MissionSlot = missionSlot });
				existingTeams.Add(bot.AllyNumber);
			}

			// add section headers
		    if (Program.TasClient.MyBattle.IsQueue)
		    {
                newList.Add(new PlayerListItem { Button = "Spectators", SortCategory = (int)PlayerListItem.SortCats.SpectatorTitle, IsSpectatorsTitle = true, Height = 25 });
                newList.Add(new PlayerListItem { Button = "Queued players", SortCategory = (int)PlayerListItem.SortCats.QueueTitle, AllyTeam = 0, IsSpectatorsTitle = false, Height = 25 });
		        newList = newList.OrderBy(x => x.UserName).ToList();
		    }
		    else
		    {
		        if (PlayerListItems.Any(i => i.UserBattleStatus != null && i.UserBattleStatus.IsSpectator)) newList.Add(new PlayerListItem { Button = "Spectators", SortCategory = (int)PlayerListItem.SortCats.SpectatorTitle, IsSpectatorsTitle = true, Height = 25 });

		        var buttonTeams = existingTeams.Distinct();
		        if (missionSlots != null) buttonTeams = buttonTeams.Concat(missionSlots.Select(s => s.AllyID)).Distinct();
		        foreach (var team in buttonTeams)
		        {
		            int numPlayers = nonSpecs.Where(p => p.UserBattleStatus.AllyNumber == team).Count();
		            int numBots = Program.TasClient.MyBattle.Bots.Values.Where(p => p.AllyNumber == team).Count();
		            int numTotal = numPlayers + numBots;

		            var allianceName = "Team " + (team + 1) + (numTotal > 3 ? "  (" + numTotal + ")" : "");
		            if (missionSlots != null)
		            {
		                var slot = missionSlots.FirstOrDefault(s => s.AllyID == team);
		                if (slot != null) allianceName = slot.AllyName;
		            }
		            newList.Add(new PlayerListItem { Button = allianceName, SortCategory = team * 2 + (int)PlayerListItem.SortCats.Uncategorized, AllyTeam = team, Height = 25 });
		        }

		        newList = newList.OrderBy(x => x.ToString()).ToList();
		    }
            
		    playerBox.BeginUpdate();
		    int currentScroll = playerBox.TopIndex;

            playerBox.Items.Clear();
            foreach (var item in newList) playerBox.Items.Add(item);

            playerBox.TopIndex = currentScroll;
            playerBox.EndUpdate();
		}

		protected override void client_ChannelUserAdded(object sender, ChannelUserInfo e) {}

		protected override void client_ChannelUserRemoved(object sender, ChannelUserRemovedInfo e) {}

		void DrawMinimap()
		{
		    try {
		        if (minimap == null || Program.TasClient.MyBattle == null) return;
		        var boxColors = new[]
		                        {
		                            Color.Green, Color.Red, Color.Blue, Color.Cyan, Color.Yellow, Color.Magenta, Color.Gray, Color.Lime, Color.Maroon,
		                            Color.Navy, Color.Olive, Color.Purple, Color.Silver, Color.Teal, Color.White,
		                        };
		        var xScale = (double)minimapBox.Width/minimapSize.Width;
		        // todo remove minimapSize and use minimap image directly when plasmaserver stuff fixed
		        var yScale = (double)minimapBox.Height/minimapSize.Height;
		        var scale = Math.Min(xScale, yScale);
		        minimapBox.Image = minimap.GetResized((int)(scale*minimapSize.Width), (int)(scale*minimapSize.Height), InterpolationMode.HighQualityBicubic);
		        using (var g = Graphics.FromImage(minimapBox.Image)) {
		            g.TextRenderingHint = TextRenderingHint.AntiAlias;
		            g.SmoothingMode = SmoothingMode.HighQuality;
		            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
		            foreach (var kvp in Program.TasClient.MyBattle.Rectangles) {
		                var startRect = kvp.Value;
		                var allyTeam = kvp.Key;
		                var left = startRect.Left*minimapBox.Image.Width/BattleRect.Max;
		                var top = startRect.Top*minimapBox.Image.Height/BattleRect.Max;
		                var right = startRect.Right*minimapBox.Image.Width/BattleRect.Max;
		                var bottom = startRect.Bottom*minimapBox.Image.Height/BattleRect.Max;
		                var width = right - left;
		                var height = bottom - top;
		                if (width < 1 || height < 1) continue;
		                var drawRect = new Rectangle(left, top, width, height);
		                var color = allyTeam < boxColors.Length
		                                ? Color.FromArgb(255/2, boxColors[allyTeam].R, boxColors[allyTeam].G, boxColors[allyTeam].B)
		                                : Color.Black;
		                using (var brush = new SolidBrush(color)) g.FillRectangle(brush, drawRect);
		                var middleX = left + width/2;
		                var middleY = top + height/2;
		                const int numberSize = 40;
		                var numberRect = new Rectangle(middleX - numberSize/2, middleY - numberSize/2, numberSize, numberSize);
		                using (var format = new StringFormat()) {
		                    format.Alignment = StringAlignment.Center;
		                    format.LineAlignment = StringAlignment.Center;

		                    using (var font = new Font("Arial", 13f, FontStyle.Bold)) g.DrawStringWithOutline((allyTeam + 1).ToString(), font, Brushes.White, Brushes.Black, numberRect, format, 5);
		                }
		            }
		        }
		        minimapBox.Invalidate();
		    } catch (Exception ex) {
		        Trace.TraceError("Error updating minimap: {0}",ex);
		    }
		}

		MissionSlot GetSlotByTeamID(int teamID)
		{
			if (missionSlots != null) return missionSlots.SingleOrDefault(s => s.TeamID == teamID);
			return null;
		}

		void RefreshBattleUser(string userName)
		{
			if (Program.TasClient.MyBattle == null) return;
			UserBattleStatus userBattleStatus;
            Program.TasClient.MyBattle.Users.TryGetValue(userName, out userBattleStatus);
			if (userBattleStatus != null)
			{
				AddUser(userName);
				SortByTeam();
			}
		}

		void SetMapImages(string mapName)
		{
			Program.ToolTip.SetMap(minimapBox, mapName);

			// todo add check before calling invoke invokes!!!
			Program.SpringScanner.MetaData.GetMapAsync(mapName,
			                                           (map, minimap, heightmap, metalmap) => Program.MainWindow.InvokeFunc(() =>
			                                           	{
			                                           		if (Program.TasClient.MyBattle == null) return;
			                                           		if (map != null && map.Name != Program.TasClient.MyBattle.MapName) return;
			                                           		if (minimap == null || minimap.Length == 0)
			                                           		{
			                                           			minimapBox.Image = null;
			                                           			this.minimap = null;
			                                           		}
			                                           		else
			                                           		{
			                                           			this.minimap = Image.FromStream(new MemoryStream(minimap));
			                                           			minimapSize = map.Size;
			                                           			DrawMinimap();
			                                           		}
			                                           	}),
			                                           a => Program.MainWindow.InvokeFunc(() =>
			                                           	{
			                                           		minimapBox.Image = null;
			                                           		minimap = null;
			                                           	}));
		}

		void ModStoreModLoaded(object sender, EventArgs<Mod> e)
		{
			if (InvokeRequired) Invoke(new ThreadStart(() => ModStoreModLoaded(sender, e)));
			else
			{
				missionSlots = e.Data.MissionSlots;
				SortByTeam();
			}
		}


		void TasClient_BattleJoined(object sender, Battle battle)
		{
			Reset();
			SetMapImages(battle.MapName);
		    minimapFuncBox.QueueMode = battle.IsQueue;
			foreach (var user in Program.TasClient.MyBattle.Users.Values) AddUser(user.Name);
			base.AddLine(new SelfJoinedBattleLine(battle));
		}

		void TasClient_BattleMapChanged(object sender, OldNewPair<Battle> pair)
		{
		    var tas = (TasClient)sender;
		    if (tas.MyBattle == pair.New) {
		        SetMapImages(pair.New.MapName);    
		    }
		}

		void TasClient_BattleUserJoined(object sender, BattleUserEventArgs e1)
		{
			var battleID = e1.BattleID;
		    var tas = (TasClient)sender;
			if (tas.MyBattle != null && battleID == tas.MyBattle.BattleID)
			{
				var userName = e1.UserName;
				UserBattleStatus userBattleStatus;
			    if (tas.MyBattle.Users.TryGetValue(userName, out userBattleStatus)) {
			        AddUser(userBattleStatus.Name);
			        AddLine(new JoinLine(userName));
			    }
			}
		}

		void TasClient_BattleUserLeft(object sender, BattleUserEventArgs e)
		{
			var userName = e.UserName;
			if (userName == Program.Conf.LobbyPlayerName)
			{
                minimapFuncBox.Visible = false; //hide buttons when leaving game 
				playerListItems.Clear();
				playerBox.Items.Clear();
				filtering = false;
				playerSearchBox.Text = string.Empty;
			}
			if (PlayerListItems.Any(i => i.UserName == userName))
			{
				RemoveUser(userName);
				AddLine(new LeaveLine(userName));
			}
		}

		void TasClient_BattleUserStatusChanged(object sender, UserBattleStatus ubs)
		{
			RefreshBattleUser(ubs.Name);
		}


		void TasClient_Said(object sender, TasSayEventArgs e)
		{
			if (e.Place == SayPlace.Battle || e.Place == SayPlace.BattlePrivate)
			{
				if (e.Text.Contains(Program.Conf.LobbyPlayerName) && !Program.TasClient.MyUser.IsInGame && !e.IsEmote && e.UserName != GlobalConst.NightwatchName &&
					!e.Text.StartsWith(string.Format("[{0}]", Program.TasClient.UserName)))
				{
					Program.MainWindow.NotifyUser("chat/battle", string.Format("{0}: {1}", e.UserName, e.Text), false, true);
				}
				if (!e.IsEmote) AddLine(new SaidLine(e.UserName, e.Text, e.Time));
				else AddLine(new SaidExLine(e.UserName, e.Text, e.Time));
			}
		}

		protected override void PlayerBox_MouseClick(object sender, MouseEventArgs mea)
		{
			if (mea.Button == MouseButtons.Left)
			{
				if (playerBox.HoverItem != null)
				{
					if (playerBox.HoverItem.IsSpectatorsTitle) ActionHandler.Spectate();
					else if (playerBox.HoverItem.SlotButton != null) ActionHandler.JoinSlot(playerBox.HoverItem.MissionSlot);
					else if (playerBox.HoverItem.Button!=null) ActionHandler.JoinAllyTeam(playerBox.HoverItem.AllyTeam.Value);
				}
			}

			if (mea.Button == MouseButtons.Right || !Program.Conf.LeftClickSelectsPlayer)
			{
				if (playerBox.HoverItem == null && mea.Button == MouseButtons.Right)
				{ //right click on empty space
					var cm = ContextMenus.GetPlayerContextMenu(Program.TasClient.MyUser, true);
					Program.ToolTip.Visible = false;
				    try {
				        cm.Show(playerBox, mea.Location);
				    } catch (Exception ex) {
				        Trace.TraceError("Error displaying tooltip: {0}", ex);
				    } finally {
				        Program.ToolTip.Visible = true;
				    }
					return;
				}
                //NOTE: code that display player's context menu on Left-mouse-click is in ChatControl.playerBox_MouseClick();
			}
			if (playerBox.HoverItem != null)
			{
				if (playerBox.HoverItem.BotBattleStatus != null)
				{
					playerBox.SelectedItem = playerBox.HoverItem;
					var cm = ContextMenus.GetBotContextMenu(playerBox.HoverItem.BotBattleStatus.Name);
					Program.ToolTip.Visible = false;
				    try {
				        cm.Show(playerBox, mea.Location);
				    } catch (Exception ex) {
				        Trace.TraceError("Error displaying tooltip: {0}", ex);
				    } finally {
				        Program.ToolTip.Visible = true;
				    }
					return;
				}
				/*
					if (playerBox.HoverItem.UserBattleStatus != null) {
						playerBox.SelectedItem = playerBox.HoverItem;
						var cm = ContextMenus.GetPlayerContextMenu(playerBox.HoverItem.User, true);
						Program.ToolTip.Visible = false;
						cm.Show(playerBox, mea.Location);
						Program.ToolTip.Visible = true;
					}*/
			}
			base.PlayerBox_MouseClick(sender, mea);
		}

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BattleChatControl
            // 
            this.Name = "BattleChatControl";
            this.Size = new System.Drawing.Size(246, 242);
            this.ResumeLayout(false);

        }
	}
}

