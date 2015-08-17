using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using PlasmaDownloader;
using PlasmaShared;
using ZkData.UnitSyncLib;
using LobbyClient;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ZkData;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
    public partial class SkirmishControl : UserControl
    { //Mix match from BattleChatControl.cs, Benchmarker/MainForm.cs, Springie/AutoHost_commands.cs
        private PictureBox minimapBox;
        Image minimap;
        Size minimapSize;
        private Spring spring;
        List<SpringScanner.CacheItem> modCache;
        List<SpringScanner.CacheItem> mapCache;
        List<Mod> modCache_folder; //mods in *.sdd folder located in datadir/games
        private List<BotBattleStatus> Bots = new List<BotBattleStatus>();
        Map currentMap;
        private List<UserBattleStatus> allUser = new List<UserBattleStatus>();
        private PlayerListItem myItem;
        private Ai[] aiList;
        private Mod currentMod;
        private Dictionary<int, BattleRect> Rectangles;
        private List<string> DisabledUnits;
        private Dictionary<string, string> ModOptions;
        private List<MissionSlot> missionSlots;
        private List<MissionSlot> botsMissionSlot;
        private float[,] presetStartPos;
        private List<Ai> springAi;
        private System.Windows.Forms.Timer uiTimer;
        private const int timerFPS = 60;
        private bool requestMinimapRefresh = false;

        public SkirmishControl()
        {
            Paint += Event_SkirmishControl_Enter;
            Program.Downloader.DownloadAdded += Event_Downloader_DownloadAdded;
        }

        private void Event_SkirmishControl_Enter(object sender, EventArgs e)
        {
            modCache = new List<SpringScanner.CacheItem>();
            mapCache = new List<SpringScanner.CacheItem>();
            modCache_folder = new List<Mod>();
            Bots = new List<BotBattleStatus>();
            allUser = new List<UserBattleStatus>();
        	
            //MessageBox.Show("Work in progress");
            //Note: always manually remove "((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();" from
            //splitcontainer, it have history to cause crash in Linux. Unknown reason.
            InitializeComponent();
            minimapBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage };
            minimapPanel.Controls.Add(minimapBox);
            minimapBox.MouseDown += Event_MinimapBox_MouseDown;
            minimapBox.MouseMove += Event_MinimapBox_MouseMove;
            minimapBox.MouseUp += Event_MinimapBox_MouseUp;
            skirmPlayerBox.IsBattle = true;
            skirmPlayerBox.MouseDown += Event_PlayerBox_MouseDown;
            skirmPlayerBox.MouseUp += Event_PlayerBox_MouseUp;

            Program.SpringScanner.LocalResourceAdded += Event_SpringScanner_LocalResourceAdded;
            Program.SpringScanner.LocalResourceRemoved += Event_SpringScanner_LocalResourceAdded;

            Setup_MyInfo();
            DisabledUnits = new List<string>();
            ModOptions = new Dictionary<string, string>();
            Rectangles = new Dictionary<int, BattleRect>();
            springAi = new List<Ai>();
            presetStartPos = new float[25, 4]{
            //  left    right   bottom  top
                {0,     0.1f,   0,      0.1f},
                {0.9f,  1,      0.9f,   1},
                {0.9f,  1,      0,      0.1f},
                {0,     0.1f,   0.9f,    1}, //corners
                {0.45f, 0.55f,  0,      0.1f},
                {0.45f, 0.55f,  0.9f,   1},
                {0,     0.1f,   0.45f,  0.55f},
                {0.9f,  1f,     0.45f,  0.55f},//half corner
                {0.20f, 0.30f,  0,      0.1f},
                {0.20f, 0.30f,  0.9f,   1},
                {0.70f, 0.80f,  0,      0.1f},
                {0.70f, 0.80f,  0.9f,   1}, //quarter corner
                {0,     0.1f,   0.20f,  0.30f},
                {0.9f,  1,      0.20f,  0.30f},
                {0,     0.1f,   0.70f,  0.80f},
                {0.9f,  1,      0.70f,  0.80f},//quarter corner
                {0.20f, 0.30f,  0.20f,  0.30f},
                {0.70f, 0.80f,  0.70f,  0.80f},
                {0.70f, 0.80f,  0.20f,  0.30f},
                {0.20f, 0.30f,  0.70f,  0.80f},//inner corner
                {0.40f, 0.50f,  0.20f,  0.30f},
                {0.40f, 0.50f,  0.70f,  0.80f},
                {0.20f, 0.30f,  0.40f,  0.50f},
                {0.70f, 0.80f,  0.40f,  0.50f},//inner half
                {0.40f, 0.50f,  0.40f,  0.50f},//center
            };

            Rectangles.Add(0, new BattleRect(presetStartPos[0, 0], presetStartPos[0, 2], presetStartPos[0, 1], presetStartPos[0, 3]));
            infoLabel.Text = "";
            Refresh_PlayerBox(); //initialize playerlist window

            lblSide.Visible = false;
            sideCB.VisibleChanged += (s, e2) =>
            {
                lblSide.Visible = (s as ComboBox).Visible;
            };
            sideCB.Visible = false;
            sideCB.DrawMode = DrawMode.OwnerDrawFixed;
            sideCB.DrawItem += Event_SideCB_DrawItem;

            uiTimer = new System.Windows.Forms.Timer();
            uiTimer.Interval = 1000 / timerFPS; //timer tick to update minimpan & add micro delay to Layout update.
            uiTimer.Tick += Event_uiTimer_Tick;
            uiTimer.Start();

            //linux compatibility 
            //the text color is White on White when parent have DimGrey background
            engine_comboBox.ForeColor = Color.Black;
            game_comboBox.ForeColor = Color.Black;
            map_comboBox.ForeColor = Color.Black;
            sideCB.ForeColor = Color.Black;
            skirmPlayerBox.ForeColor = Color.Black;

            Setup_ComboBox();
            if (!string.IsNullOrEmpty(Program.Conf.SkirmisherEngine))
                engine_comboBox.SelectedItem = Program.Conf.SkirmisherEngine;
            else 
                engine_comboBox.SelectedItem = GlobalConst.DefaultEngineOverride ?? Program.TasClient.ServerSpringVersion;
            
            if (!string.IsNullOrEmpty(Program.Conf.SkirmisherGame))
                game_comboBox.SelectedItem = Program.Conf.SkirmisherGame;
            else
            {
                var gameVer = Program.Downloader.PackageDownloader.GetByTag(KnownGames.GetDefaultGame().RapidTag);
                if (gameVer!=null)
                    game_comboBox.SelectedItem = gameVer.InternalName;
            }
            
            if (!string.IsNullOrEmpty(Program.Conf.SkirmisherMap))
                map_comboBox.SelectedItem = Program.Conf.SkirmisherMap;
            
            this.OnResize(new EventArgs()); //to fix control not filling the whole window at start
            Paint -= Event_SkirmishControl_Enter;
        }

        private void Setup_MyInfo()
        {
            string myName = Program.Conf.LobbyPlayerName == null ? "unnamed" : Program.Conf.LobbyPlayerName;
            User myUser = new User { Name = myName };
            myUser.Country = "Unknown";
            UserBattleStatus myBattleStatus = new UserBattleStatus(myName, myUser) { AllyNumber = 0, SyncStatus = SyncStatuses.Unknown, IsSpectator = spectateCheckBox.Checked };
            myItem = new PlayerListItem
            {
                UserName = myBattleStatus.Name,
                AllyTeam = myBattleStatus.AllyNumber,
                isOfflineMode = true,
                isZK = false,
            };
            myItem.offlineUserInfo = myUser;
            myItem.offlineUserBattleStatus = myBattleStatus;

            botsMissionSlot = new List<MissionSlot>();
        }

        private Download download;
        private int timerCount = 0;
        private void Event_Downloader_DownloadAdded(object sender, EventArgs<Download> e)
        {
            download = e.Data;
            timerCount = 0;
        }

        private bool requestComboBoxRefresh = false;
        void Event_SpringScanner_LocalResourceAdded(object sender, SpringScanner.ResourceChangedEventArgs e)
        {
            requestComboBoxRefresh = true;
        }

        void Event_uiTimer_Tick(object sender, EventArgs e)
        {
            if (!Visible) return;

            //update minimap when startbox changed or when map changed
            if (requestMinimapRefresh || (mouseIsDown & mouseOnStartBox > -1))
            {
                Refresh_MinimapImage();
                requestMinimapRefresh = false;
            }

            //update combobox
            bool downloadFinished = (download != null && download.IsComplete.GetValueOrDefault());
            if (downloadFinished || requestComboBoxRefresh)
            {
                timerCount++;
                //wait 3 second after LocalResourceAdded() to avoid ComboBox flicker if its called too often, 
                //and wait 3 second for download finish just in case the file is extracting
                if (timerCount >= timerFPS * 3)
                {
                    suppressEvent_SelectedIndexChanged = true;
                    Setup_ComboBox_AndRestore();
                    suppressEvent_SelectedIndexChanged = false;
                    
                    download = null;
                    requestComboBoxRefresh = false;
                    timerCount = 0;
                }
            }
        }

        private void Setup_ComboBox_AndRestore()
        {
            string gameName = (string)game_comboBox.SelectedItem;
            string mapName = (string)map_comboBox.SelectedItem;
            string engineName = (string)engine_comboBox.SelectedItem;
            Setup_ComboBox();
            game_comboBox.SelectedItem = gameName;
            map_comboBox.SelectedItem = mapName;
            engine_comboBox.SelectedItem = engineName;
        }

        private void Setup_ComboBox() //code from Benchmarker.MainForm.cs
        {
            try
            {
                List<string> engineList = new List<string>();
                List<string> modList = new List<string>();
                List<string> mapList = new List<string>();
                try
                {
                    string engineFolder = ZkData.Utils.MakePath(Program.SpringPaths.WritableDirectory, "engine");
                    engineList = System.IO.Directory.EnumerateDirectories(engineFolder, "*").ToList<string>();
                    for (int i = 0; i < engineList.Count; i++)
                        engineList[i] = SkirmishControlTool.GetFolderOrFileName(engineList[i]);

                    engineList = SkirmishControlTool.SortListByVersionName(engineList);

                    modCache = Program.SpringScanner.GetAllModResource();
                    for (int i = 0; i < modCache.Count; i++) modList.Add(modCache[i].InternalName);
                    modCache_folder.Clear();
                    modCache_folder = SkirmishControlTool.GetPartialSddMods();
                    for (int i = 0; i < modCache_folder.Count; i++)
                    {
                        var version = modCache_folder[i].PrimaryModVersion;
                        version = string.IsNullOrWhiteSpace(version) ? "" : " " + version;
                        modList.Add(modCache_folder[i].Name + version);
                    }
                    modList = SkirmishControlTool.SortListByVersionName(modList);

                    mapCache = Program.SpringScanner.GetAllMapResource();
                    for (int i = 0; i < mapCache.Count; i++) mapList.Add(mapCache[i].InternalName);
                    mapList = SkirmishControlTool.SortListByVersionName(mapList);

                    engine_comboBox.Items.Clear();
                    game_comboBox.Items.Clear();
                    map_comboBox.Items.Clear();
                    engine_comboBox.Items.AddRange(engineList.ToArray());
                    game_comboBox.Items.AddRange(modList.ToArray());
                    map_comboBox.Items.AddRange(mapList.ToArray());
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                InvokeIfNeeded(() =>
                { //for multithreading stuff?
                    engine_comboBox.Items.Clear();
                    game_comboBox.Items.Clear();
                    map_comboBox.Items.Clear();
                    engine_comboBox.Items.AddRange(engineList.ToArray());
                    game_comboBox.Items.AddRange(modList.ToArray());
                    map_comboBox.Items.AddRange(mapList.ToArray());
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public void InvokeIfNeeded(Action acc) // come with Setup_ComboBox() (originally called SetupAutoComplete()), code from Benchmarker.MainForm.cs 
        {
            try
            {
                if (InvokeRequired) Invoke(acc);
                else acc();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        private bool suppressEvent_minimapRadiobutton = false;
        private void Event_MinimapRadioButton_CheckedChanged(object sender, EventArgs e) //shared by all 3 minimap radiobutton
        {
            if (!suppressEvent_minimapRadiobutton)
            {
                suppressEvent_minimapRadiobutton = true;
                elevationRadioButton.Checked = false;
                metalmapRadioButton.Checked = false;
                normalRadioButton.Checked = false;
                if ((sender as Control).Name == "normalRadioButton") normalRadioButton.Checked = true;
                else if ((sender as Control).Name == "metalmapRadioButton") metalmapRadioButton.Checked = true;
                else if ((sender as Control).Name == "elevationRadioButton") elevationRadioButton.Checked = true;
                suppressEvent_minimapRadiobutton = false;

                if (map_comboBox.SelectedItem != null)
                {
                    int selectedView = normalRadioButton.Checked ? 0 : (elevationRadioButton.Checked ? 1 : 2);
                    Set_MapImages((string)map_comboBox.SelectedItem, selectedView);
                }
                else
                {
                    Set_InfoLabel();
                }
            }
        }

        private void Event_SkirmishControl_Resize(object sender, EventArgs e)
        {
            if (map_comboBox.SelectedItem != null)
            {
                int selectedView = normalRadioButton.Checked ? 0 : (elevationRadioButton.Checked ? 1 : 2);
                Set_MapImages((string)map_comboBox.SelectedItem, selectedView);
            }
        }

        private void Set_MapImages(string mapName, int mapView)
        {
            //Program.ToolTip.SetMap(minimapBox, mapName);
            Program.ToolTip.SetMap(map_comboBox, mapName);
            string springVersion = (engine_comboBox.SelectedItem != null) ? (string)engine_comboBox.SelectedItem : null;
            // todo add check before calling invoke invokes!!!
            Program.SpringScanner.MetaData.GetMapAsync(mapName,
                                                       (map, minimap, heightmap, metalmap) => Program.MainWindow.InvokeFunc(() =>
                                                       {
                                                           if (map == null) return;
                                                           currentMap = map;
                                                           if (mapView == 1) minimap = heightmap;
                                                           else if (mapView == 2) minimap = metalmap;
                                                           if (minimap == null || minimap.Length == 0)
                                                           {
                                                                minimapBox.Image = null;
                                                               this.minimap = null;
                                                           }
                                                           else
                                                           {
                                                               this.minimap = Image.FromStream(new MemoryStream(minimap));
                                                               minimapSize = map.Size;
                                                               requestMinimapRefresh = true; //Refresh_MinimapImage();
                                                           }
                                                       }),
                                                       a => Program.MainWindow.InvokeFunc(() =>
                                                       { //exceptions
                                                           minimapBox.Image = null;
                                                           minimap = null;
                                                       }));
        }

        private void Refresh_MinimapImage(bool invalidate= true)
        {
            try
            {
                if (minimap == null) return;
                var boxColors = new[]
                                {
                                    Color.Green, Color.Red, Color.Blue, Color.Cyan, Color.Yellow, Color.Magenta, Color.Gray, Color.Lime, Color.Maroon,
                                    Color.Navy, Color.Olive, Color.Purple, Color.Silver, Color.Teal, Color.White,
                                };
                var xScale = (double)minimapBox.Width / minimapSize.Width;
                // todo remove minimapSize and use minimap image directly when plasmaserver stuff fixed
                var yScale = (double)minimapBox.Height / minimapSize.Height;
                var scale = Math.Min(xScale, yScale);
                minimapBox.Image = minimap.GetResized((int)(scale * minimapSize.Width), (int)(scale * minimapSize.Height), InterpolationMode.HighQualityBicubic);

                if (currentMod != null && currentMod.IsMission)
                { //skip drawing startbox for Mission Mod (also, we didn't read their startbox). Also disabled startbox drag in Event_MinimapBox_MouseMove().
                    minimapBox.Invalidate();
                    return;
                }
                
                using (var g = Graphics.FromImage(minimapBox.Image))
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    //var positions = currentMap.Positions != null ? currentMap.Positions.ToList() : new List<StartPos>();
                    //foreach (var pos in positions)
                    //{
                    //    var left = ((pos.z - 1000)/currentMap.Size.Width) * minimapBox.Image.Width / BattleRect.Max;
                    //    var top = ((pos.x - 1000) / currentMap.Size.Height) * minimapBox.Image.Height / BattleRect.Max;
                    //    var right = ((pos.z + 1000) / currentMap.Size.Width) * minimapBox.Image.Width / BattleRect.Max;
                    //    var bottom = ((pos.z + 1000) / currentMap.Size.Height) * minimapBox.Image.Height / BattleRect.Max;
                    //    var width = right - left;
                    //    var height = bottom - top;
                    //    if (width < 1 || height < 1) continue;
                    //    var drawRect = new Rectangle(left, top, width, height);
                    //    var color =  Color.Black;
                    //    using (var brush = new SolidBrush(color)) g.FillRectangle(brush, drawRect);

                    //}
                    foreach (var kvp in Rectangles)
                    {
                        BattleRect startRect = kvp.Value;
                        var allyTeam = kvp.Key;
                        var left = startRect.Left * minimapBox.Image.Width / BattleRect.Max;
                        var top = startRect.Top * minimapBox.Image.Height / BattleRect.Max;
                        var right = startRect.Right * minimapBox.Image.Width / BattleRect.Max;
                        var bottom = startRect.Bottom * minimapBox.Image.Height / BattleRect.Max;
                        var width = right - left;
                        var height = bottom - top;
                        if (width < 1 || height < 1) continue;
                        var drawRect = new Rectangle(left, top, width, height);
                        var color = allyTeam < boxColors.Length
                                        ? Color.FromArgb(255 / 2, boxColors[allyTeam].R, boxColors[allyTeam].G, boxColors[allyTeam].B)
                                        : Color.Black;
                        using (var brush = new SolidBrush(color)) g.FillRectangle(brush, drawRect);
                        var middleX = left + width / 2;
                        var middleY = top + height / 2;
                        const int numberSize = 40;
                        var numberRect = new Rectangle(middleX - numberSize / 2, middleY - numberSize / 2, numberSize, numberSize);
                        using (var format = new StringFormat())
                        {
                            format.Alignment = StringAlignment.Center;
                            format.LineAlignment = StringAlignment.Center;

                            using (var font = new Font("Arial", 13f, FontStyle.Bold)) g.DrawStringWithOutline((allyTeam + 1).ToString(), font, Brushes.White, Brushes.Black, numberRect, format, 5);
                        }
                    }
                }
                if (invalidate) minimapBox.Invalidate();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error updating minimap: {0}", ex);
            }
        }

        private void Refresh_PlayerBox() //from BattleChatControl.SortByTeam()
        {
            SyncStatuses iamSynced = ((game_comboBox.SelectedItem != null && engine_comboBox.SelectedItem != null && map_comboBox.SelectedItem != null) ? SyncStatuses.Synced : SyncStatuses.Unsynced);
            bool gameIsZK = game_comboBox.SelectedItem != null ? ((string)game_comboBox.SelectedItem).Contains("Zero-K") : false;

            myItem.offlineUserBattleStatus.SyncStatus = iamSynced;
            myItem.isZK = gameIsZK;

            var newList = new List<PlayerListItem>();
            newList.Add(myItem);
            var playerListItems = new List<PlayerListItem>();

            if (!myItem.UserBattleStatus.IsSpectator || Bots.Count > 0) playerListItems.Add(myItem);
            var existingTeams = playerListItems.GroupBy(i => i.UserBattleStatus.AllyNumber).Select(team => team.Key).ToList();

            if (botsMissionSlot.Count > 0)
            {
                for (int i = 0; i < botsMissionSlot.Count; i++)
                {
                    BotBattleStatus bot = Bots[i];
                    newList.Add(new PlayerListItem { BotBattleStatus = bot, SortCategory = bot.AllyNumber * 2 + 1 + (int)PlayerListItem.SortCats.Uncategorized, AllyTeam = bot.AllyNumber, MissionSlot = botsMissionSlot[i] });
                    existingTeams.Add(bot.AllyNumber);
                }
                for (int i = botsMissionSlot.Count; i < Bots.Count; i++) //include any extra bots added by user
                {
                    BotBattleStatus bot = Bots[i];
                    newList.Add(new PlayerListItem { BotBattleStatus = bot, SortCategory = bot.AllyNumber * 2 + 1 + (int)PlayerListItem.SortCats.Uncategorized, AllyTeam = bot.AllyNumber, MissionSlot = null });
                    existingTeams.Add(bot.AllyNumber);
                }
            }
            else
            {
                for (int i = 0; i < Bots.Count; i++)
                {
                    BotBattleStatus bot = Bots[i];
                    newList.Add(new PlayerListItem { BotBattleStatus = bot, SortCategory = bot.AllyNumber * 2 + 1 + (int)PlayerListItem.SortCats.Uncategorized, AllyTeam = bot.AllyNumber, MissionSlot = null });
                    existingTeams.Add(bot.AllyNumber);
                }
            }

            // add section headers
            if (playerListItems.Any(i => i.UserBattleStatus != null && i.UserBattleStatus.IsSpectator)) newList.Add(new PlayerListItem { Button = "Spectators", SortCategory = (int)PlayerListItem.SortCats.SpectatorTitle, IsSpectatorsTitle = true, Height = 25 });

            var rectangles2 = new Dictionary<int, BattleRect>();

            var buttonTeams = existingTeams.Distinct();
            if (missionSlots != null) buttonTeams = buttonTeams.Concat(missionSlots.Select(s => s.AllyID)).Distinct();
            foreach (var team in buttonTeams)
            {
                int numPlayers = myItem.UserBattleStatus.IsSpectator ? 0 : 1;
                int numBots = Bots.Where(p => p.AllyNumber == team).Count();
                int numTotal = numPlayers + numBots;

                rectangles2.Add(team, new BattleRect(presetStartPos[team % 25, 0], presetStartPos[team % 25, 2], presetStartPos[team % 25, 1], presetStartPos[team % 25, 3]));

                var allianceName = "Team " + (team + 1) + (numTotal > 3 ? "  (" + numTotal + ")" : "");
                if (missionSlots != null)
                {
                    var slot = missionSlots.FirstOrDefault(s => s.AllyID == team);
                    if (slot != null) allianceName = slot.AllyName;
                }
                newList.Add(new PlayerListItem { Button = allianceName, SortCategory = team * 2 + (int)PlayerListItem.SortCats.Uncategorized, AllyTeam = team, Height = 25 });
            }

            //copy new startBox position, but keep old one and remove any extras
            {
                bool haveChanges = false;
                List<int> toRemove = new List<int>();
                foreach (var battleRect in Rectangles)
                {
                    if (!rectangles2.ContainsKey(battleRect.Key)) toRemove.Add(battleRect.Key); //remove extra entry
                    else rectangles2.Remove(battleRect.Key); //keep current entry
                }
                for (int i = 0; i < toRemove.Count; i++)
                {
                    Rectangles.Remove(toRemove[i]);
                    haveChanges = true;
                }
                foreach (var battleRect in rectangles2)
                {
                    Rectangles.Add(battleRect.Key, battleRect.Value); //add missing entry
                    haveChanges = true;
                }
                toRemove = null;
                if (haveChanges) requestMinimapRefresh = true; // Refresh_MinimapImage();
            }

            newList = newList.OrderBy(x => x.ToString()).ToList();

            allUser.Clear();
            allUser.Add(myItem.offlineUserBattleStatus);
            foreach (var bot in Bots)
                allUser.Add(bot);

            if (Bots.Count > 0 && infoLabel.Text.StartsWith("Add bot"))
                infoLabel.Text = "";

            skirmPlayerBox.BeginUpdate();
            skirmPlayerBox.Items.Clear();
            foreach (var item in newList) skirmPlayerBox.Items.Add(item);
            skirmPlayerBox.EndUpdate();
        }

        bool suppressEvent_spectateCheckBox = false;
        private void Event_SpectateCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressEvent_spectateCheckBox) return;
            myItem.offlineUserBattleStatus.IsSpectator = spectateCheckBox.Checked;
            Refresh_PlayerBox();
        }

        private MissionSlot Get_SlotByTeamID(int teamID)
        {
            if (missionSlots != null) return missionSlots.SingleOrDefault(s => s.TeamID == teamID);
            return null;
        }

        private int Get_FreeTeamID(string exceptUser) //from LobbyClient/Battle
        {
            return Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(teamID => myItem.offlineUserBattleStatus.TeamNumber != teamID && !Bots.Any(x => x.TeamNumber == teamID));
        }

        private void Set_MyBattleStatus(int? allyNumber, int? teamNumber, bool? isSpectator)
        {
            if (allyNumber.HasValue)
            {
                myItem.AllyTeam = allyNumber.Value;
                myItem.offlineUserBattleStatus.AllyNumber = allyNumber.Value;
            }
            if (teamNumber.HasValue) myItem.offlineUserBattleStatus.TeamNumber = teamNumber.Value;
            if (isSpectator.HasValue)
            {
                myItem.offlineUserBattleStatus.IsSpectator = isSpectator.Value;
                suppressEvent_spectateCheckBox = true;
                spectateCheckBox.Checked = isSpectator.Value;
                suppressEvent_spectateCheckBox = false;
            }
            Refresh_PlayerBox();
        }

        private bool isClick = false;
        private void Event_PlayerBox_MouseDown(object sender, MouseEventArgs mea)    {    isClick=true;    }

        //using MouseUp because it allow the PlayerBox's "HoverItem" to return correct value when we click on it rapidly
        private void Event_PlayerBox_MouseUp(object sender, MouseEventArgs mea) //from BattleChatControl
        {
            if (!isClick) return;
            isClick = false;

            if (currentMod != null && currentMod.IsMission) return; //disable shorcuts for mission mod
            //change ally
            if (mea.Button == MouseButtons.Left)
            {
                if (skirmPlayerBox.HoverItem != null)
                {
                    if (skirmPlayerBox.HoverItem.IsSpectatorsTitle)
                        Set_MyBattleStatus(null, null, true); //spectator
                    else if (skirmPlayerBox.HoverItem.SlotButton != null) //mission
                    {
                        MissionSlot slot = skirmPlayerBox.HoverItem.MissionSlot;
                        Set_MyBattleStatus(slot.AllyID, slot.TeamID, false);
                        return;
                    }
                    else if (skirmPlayerBox.HoverItem.Button!=null) //alliance
                        Set_MyBattleStatus(skirmPlayerBox.HoverItem.AllyTeam.Value, Get_FreeTeamID(myItem.UserName), false);
                }
            }
            //context menu
            if (mea.Button == MouseButtons.Right || !Program.Conf.LeftClickSelectsPlayer)
            {
                if (skirmPlayerBox.HoverItem != null && skirmPlayerBox.HoverItem.BotBattleStatus != null) //on bot name
                {
                    skirmPlayerBox.SelectedItem = skirmPlayerBox.HoverItem;

                    var cm = Get_BotContextMenu(skirmPlayerBox.HoverItem.BotBattleStatus.Name);
                    Program.ToolTip.Visible = false;
                    try
                    {
                        cm.Show(skirmPlayerBox, mea.Location);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error displaying tooltip: {0}", ex);
                    }
                    finally
                    {
                        Program.ToolTip.Visible = true;
                    }
                }else //on name or empty space
                {
                    skirmPlayerBox.SelectedItem = skirmPlayerBox.HoverItem;

                    var cm = Get_PlayerContextMenu(myItem.User);
                    Program.ToolTip.Visible = false;
                    try
                    {
                        cm.Show(skirmPlayerBox, mea.Location);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error displaying tooltip: {0}", ex);
                    }
                    finally
                    {
                        Program.ToolTip.Visible = true;
                    }
                }
            }
        }

        private List<int> Get_ExistingTeams(out int freeAllyTeam)
        {
            var existingTeams = allUser.GroupBy(p => p.AllyNumber).Select(team => team.Key).ToList();
            freeAllyTeam = Enumerable.Range(0, 100).First(allyTeam => !existingTeams.Contains(allyTeam));
            return existingTeams;
        }

        private ContextMenu Get_BotContextMenu(string botName)
        {
            var contextMenu = new ContextMenu();
            try
            {
                var botStatus = Enumerable.Single<BotBattleStatus>(Bots, b => b.Name == botName);

                {
                    var item = new System.Windows.Forms.MenuItem("Remove") { Enabled = botStatus.owner == myItem.UserName };
                    item.Click += (s, e) =>
                    {
                        Bots.RemoveAll(b=> b.Name == botName);
                        Refresh_PlayerBox();
                    };
                    contextMenu.MenuItems.Add(item);
                }
                {
                    var item = new System.Windows.Forms.MenuItem("Ally With") { Enabled = botStatus.owner == myItem.UserName };
                    int freeAllyTeam;

                    var existingTeams = Get_ExistingTeams(out freeAllyTeam).Where(t => t != botStatus.AllyNumber).Distinct();
                    if (existingTeams.Any())
                    {
                        foreach (var allyTeam in existingTeams)
                        {
                            var at = allyTeam;
                            if (allyTeam != botStatus.AllyNumber)
                            {
                                var subItem = new System.Windows.Forms.MenuItem("Join Team " + (allyTeam + 1));
                                subItem.Click += (s, e) =>
                                {
                                    botStatus.AllyNumber = at;
                                    Bots.RemoveAll(u => u.Name == botName);
                                    Bots.Add(botStatus);
                                    Refresh_PlayerBox();
                                };
                                item.MenuItems.Add(subItem);
                            }
                        }
                        item.MenuItems.Add("-");
                    }
                    var newTeamItem = new System.Windows.Forms.MenuItem("New Team");
                    newTeamItem.Click += (s, e) =>
                    {
                        botStatus.AllyNumber = freeAllyTeam;
                        Bots.RemoveAll(u => u.Name == botName);
                        Bots.Add(botStatus);
                        Refresh_PlayerBox();
                        //Program.TasClient.UpdateBot(botName, newStatus, botStatus.TeamColor);
                    };
                    item.MenuItems.Add(newTeamItem);
                    contextMenu.MenuItems.Add(item);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error generating bot context menu: " + e);
            }
            return contextMenu;
        }

        private ContextMenu Get_PlayerContextMenu(User user) //from ContextMenu.cs
        {
            var contextMenu = new ContextMenu();
            try
            {
                var headerItem = new MenuItem("Player - " + user.Name) { Enabled = false, DefaultItem = true };
                // default is to make it appear bold
                contextMenu.MenuItems.Add(headerItem);

                var battleStatus = allUser.SingleOrDefault(u => u.Name == user.Name);

                contextMenu.MenuItems.Add("-");

                if (user.Name != myItem.UserName)
                {
                    var allyWith = new MenuItem("Ally")
                    {
                        Enabled =
                            !battleStatus.IsSpectator &&
                            (battleStatus.AllyNumber != myItem.AllyTeam || myItem.offlineUserBattleStatus.IsSpectator)
                    };
                    allyWith.Click += (s, e) =>
                        {
                            myItem.AllyTeam = battleStatus.AllyNumber;
                            myItem.offlineUserBattleStatus.AllyNumber = battleStatus.AllyNumber;
                            spectateCheckBox.Checked = false;
                            myItem.offlineUserBattleStatus.TeamNumber = Get_FreeTeamID(myItem.UserName);
                            Refresh_PlayerBox();

                        };

                    contextMenu.MenuItems.Add(allyWith);
                }

                contextMenu.MenuItems.Add(Get_SetAllyTeamItem(user));

                contextMenu.MenuItems.Add("-");
                contextMenu.MenuItems.Add(Get_ShowOptions());
                contextMenu.MenuItems.Add(Get_ModBotItem());
                contextMenu.MenuItems.Add(Get_SpringBotItem());
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error generating player context menu: " + e);
            }
            return contextMenu;
        }

        private void Event_GameOptionButton_Click(object sender, EventArgs e)
        {
            if (game_comboBox.SelectedItem != null)
            {
                Get_ModOptionsControl();
                return;
            }
            //no mods! show "Change Game Options (Not available)"
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(Get_ShowOptions());
            menu.Show(gameOptionButton, new Point(0, 0));
        }

        private MenuItem Get_ShowOptions()
        {
            bool enabled = (game_comboBox.SelectedItem!=null);
            var modOptions = new System.Windows.Forms.MenuItem("Change Game Options " + (enabled ? String.Empty : " (Not available)")) { Enabled = true };
            if (enabled)
            {
                modOptions.Click += (s, e) =>
                {
                    Get_ModOptionsControl();
                };
            }
            return modOptions;
        }

        private void Get_ModOptionsControl()
        {
            var form = new Form { Width = 1000, Height = 300, Icon = ZklResources.ZkIcon, Text = "Game options" };
            var optionsControl = new ModOptionsControl(currentMod, ModOptions) { Dock = DockStyle.Fill };
            form.Controls.Add(optionsControl);
            this.Leave += (s2, e2) =>
            {
                form.Close();
                form.Dispose();
                optionsControl.Dispose();
            };
            if (!currentMod.IsMission) //disable option changing for Mission Mod
                optionsControl.ChangeApplied += (s3, e3) =>
                {
                    ModOptions.Clear();
                    foreach (var option in (s3 as System.Collections.Generic.IEnumerable<KeyValuePair<string, string>>)) //IEnumberable can't be serialized, so convert to List.
                        ModOptions.Add(option.Key, option.Value);
                };
            form.Show(); //show menu
        }

        private void Set_BotBattleStatus(string shortname,string ownerName, int? teamNumber, int? allyNumber,int? botColor, string version)
        {
            var aiLib = shortname;
            if (version != null) aiLib = aiLib + "|" + version; //splitter defined in Battle.cs/ScriptAddBot();
            var botNumber = Enumerable.Range(1, 9000).First(j => !Bots.Any(bt => bt.Name == "Bot_" + j));
            BotBattleStatus botStatus = new BotBattleStatus("Bot_" + botNumber,ownerName, aiLib);
            
            if (teamNumber.HasValue) botStatus.TeamNumber = teamNumber.Value;
            else botStatus.TeamNumber = Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(x => !allUser.Any(y => y.TeamNumber == x));
            
            if (allyNumber.HasValue) botStatus.TeamNumber = allyNumber.Value;
            else botStatus.AllyNumber = Enumerable.Range(0, TasClient.MaxAlliances - 1).FirstOrDefault(x => x != botStatus.AllyNumber);
            
            Bots.Add(botStatus);
            Refresh_PlayerBox();
        }

        private void Event_AddAIButton_Click(object sender, EventArgs e)
        {
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(Get_ModBotItem());
            menu.MenuItems.Add(Get_SpringBotItem());
            menu.Show(addAIButton, new Point(0, 0));
        }

        private MenuItem Get_SpringBotItem()
        {
            bool msWindows = Environment.OSVersion.Platform != PlatformID.Unix;
            bool enabled = (engine_comboBox.SelectedItem != null) && (msWindows || (string)engine_comboBox.SelectedItem != "91.0"); //linux don't have static build for Spring 91
            var addSpringBot = new MenuItem("Add spring's AI (Bot)" + (enabled ? String.Empty : " (Not available)")) { Visible = true };
            if (engine_comboBox.SelectedItem != null && springAi.Count > 0)
            {
                var ais = springAi;
                MenuItem item; string description;// int descLength;
                for (int i = 0; i < ais.Count; i++)
                {
                    description = ais[i].Description;
                    string shortName = ais[i].ShortName;
                    string version = ais[i].Version;
                    //descLength = 65 - shortName.Length;
                    //item = new System.Windows.Forms.MenuItem(string.Format("{0} ({1}" + (description.Length > descLength ? "..." : ")"), shortName, description.Substring(0, Math.Min(descLength, description.Length)))); //description too long 
                    item = new System.Windows.Forms.MenuItem(string.Format("{0} ({1})", shortName, version)); //description too long 
                    item.Click += (s, e2) =>
                    {
                        Set_BotBattleStatus(shortName, myItem.UserName, null, null, null, version);
                    };
                    addSpringBot.MenuItems.Add(item);
                }
            }
            return addSpringBot;
        }

        private MenuItem Get_ModBotItem()
        {
            var enabled = true && aiList != null && aiList.Any();
            var addBotItem = new MenuItem("Add game's AI (Bot)" + (enabled ? String.Empty : " (Not available)")) { Visible = true };
            if (aiList != null)
            {
                foreach (var bot in aiList)
                {
                    var item = new MenuItem(string.Format("{0} ({1})", bot.ShortName, bot.Description));
                    var b = bot; //to maintain reference to object
                    item.Click += (s, e) =>
                    {
                        Set_BotBattleStatus(b.ShortName, myItem.UserName, null, null, null,b.Version);
                    };
                    addBotItem.MenuItems.Add(item);
                }
            }
            return addBotItem;
        }

        private MenuItem Get_SetAllyTeamItem(User user)
        {
            var setAllyTeamItem = new System.Windows.Forms.MenuItem("Select Team");

            if (user.Name != myItem.UserName) setAllyTeamItem.Enabled = false;
            else
            {
                int freeAllyTeam;

                foreach (var allyTeam in Get_ExistingTeams(out freeAllyTeam).Distinct())
                {
                    var at = allyTeam; //to maintain reference to this object
                    if (allyTeam != myItem.offlineUserBattleStatus.AllyNumber)
                    {
                        var item = new MenuItem("Join Team " + (allyTeam + 1));
                        item.Click += (s, e) =>
                            {
                                Set_MyBattleStatus(at, Get_FreeTeamID(user.Name), false);
                            };
                        setAllyTeamItem.MenuItems.Add(item);
                    }
                }

                setAllyTeamItem.MenuItems.Add("-");

                var newTeamItem = new System.Windows.Forms.MenuItem("Start New Team");
                newTeamItem.Click += (s, e) =>
                    {
                        Set_MyBattleStatus(freeAllyTeam, Get_FreeTeamID(myItem.UserName), false);
                    };
                setAllyTeamItem.MenuItems.Add(newTeamItem);

                if (!myItem.offlineUserBattleStatus.IsSpectator)
                {
                    var specItem = new System.Windows.Forms.MenuItem("Spectate");
                    specItem.Click += (s, e) =>
                        {
                            Set_MyBattleStatus(null,null,true);
                        };
                    setAllyTeamItem.MenuItems.Add(specItem);
                }
            }

            return setAllyTeamItem;
        }

        private bool wasMissingEntry = true;
        private bool suppressEvent_SelectedIndexChanged = false;
        private void Event_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressEvent_SelectedIndexChanged) return;
            if ((sender as Control).Name == "map_comboBox" && map_comboBox.SelectedItem!= null)
            {
                string mapName = (string)map_comboBox.SelectedItem;
                int selectedView = normalRadioButton.Checked ? 0 : (elevationRadioButton.Checked ? 1 : 2);
                Set_MapImages(mapName, selectedView);
                
                if (infoLabel.Text.StartsWith("Select map"))
                    infoLabel.Text = "";

                Set_InfoLabel();
                Program.Conf.SkirmisherMap = mapName;
            }
            else if ((sender as Control).Name == "game_comboBox" && game_comboBox.SelectedItem != null)
            {
                string gameName = (string)game_comboBox.SelectedItem;
                
                bool foundLocally=false;
                for(int i=0; i<modCache_folder.Count;i++)
                {
                    var mod = modCache_folder[i];
                    var version = modCache_folder[i].PrimaryModVersion;
                    version = string.IsNullOrWhiteSpace(version) ? "" : " " + version;
                    var modName = mod.Name + version;
                    if (gameName == modName)
                    {

                        modCache_folder[i] = SkirmishControlTool.GetOneSddMod(mod);
                        CallBack_Mod(modCache_folder[i]);
                        foundLocally = true;
                        break;
                    }
                }

                if (!foundLocally)
                {
                    //run GetMod() in new thread, then call "CallBack_Mod()" in current thread when finish(?). 
                    //TODO: GetModAsync() is not an offline method, it rely on downloading server generated mod/map information to work. This can cause (minor) error if user downloaded a unique map or mod that server haven't process yet!
                    Program.SpringScanner.MetaData.GetModAsync(
                        gameName,
                        mod =>
                        Invoke(new Action(() => {
                            try {
                                CallBack_Mod(mod);
                            } catch (Exception ex) {
                                Trace.TraceError("CallBack_Mod(mod) error: {0}", ex.ToString());
                            }
                        })), 
                        exception => Trace.TraceError("CallBack_Mod(mod) error: {0}", exception.ToString()));
                    //Program.SpringScanner.MetaData.GetModAsync(
                    //   (string)game_comboBox.SelectedItem,
                    //   mod=>{
                    //       try { CallBack_Mod(mod); }
                    //       catch (Exception ex) { Trace.TraceError("CallBack_Mod(mod) error: {0}", ex.ToString()); }
                    //       },
                    //   exception => { Trace.TraceError("CallBack_Mod(mod) error: {0}", exception.ToString()); },
                    //   (string)engine_comboBox.SelectedItem);
                }
                
                if (infoLabel.Text.StartsWith("Select game"))
                    infoLabel.Text = "";

                Set_InfoLabel();
                
                var defGameVer = Program.Downloader.PackageDownloader.GetByTag(KnownGames.GetDefaultGame().RapidTag);
                if (defGameVer!=null && gameName == defGameVer.InternalName)
                    Program.Conf.SkirmisherGame = null; //tell Skirmisher to use default in next startup
                else
                    Program.Conf.SkirmisherGame = gameName;

            }
            else if ((sender as Control).Name == "engine_comboBox" && engine_comboBox.SelectedItem != null)
            {
                string springVersion = (string)engine_comboBox.SelectedItem;
                string engineFolder = ZkData.Utils.MakePath(Program.SpringPaths.WritableDirectory, "engine");
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                    engineFolder = engineFolder + "\\" + springVersion;
                else
                    engineFolder = engineFolder + "/" + springVersion;

                if (Program.SpringPaths.HasEngineVersion(springVersion))
                    Program.SpringPaths.SetEnginePath (engineFolder);
                spring = new Spring(Program.SpringPaths);
                

                if (infoLabel.Text.StartsWith("Select engine"))
                    infoLabel.Text = "";

                Set_InfoLabel();
                
                if ((string)engine_comboBox.SelectedItem == (GlobalConst.DefaultEngineOverride ?? Program.TasClient.ServerSpringVersion))
                    Program.Conf.SkirmisherEngine = null; //tell Skirmihser to use default in next run
                else
                    Program.Conf.SkirmisherEngine = (string)engine_comboBox.SelectedItem;

                if (Program.SpringPaths.HasEngineVersion(springVersion))
                    springAi = SkirmishControlTool.GetSpringAIs(engineFolder);
                else
                    springAi.Clear();
            }
            //check if we have entered game, map and engine value so that we can update the Sync icon.
            bool missingEntry = (game_comboBox.SelectedItem == null || engine_comboBox.SelectedItem == null || map_comboBox.SelectedItem == null);
            if (missingEntry != wasMissingEntry) Refresh_PlayerBox();
            wasMissingEntry = missingEntry;
        }

        private void CallBack_Mod(Mod mod)
        {
            if (mod != null)
            {
                //Initialize "side" (faction) dropdown list. copied from Notification.BattleBar.cs
                {
                    sideCB.Visible = mod.Sides.Length > 1;
                    if (sideCB.Visible)
                    {
                        var previousSide = sideCB.SelectedItem != null ? sideCB.SelectedItem.ToString() : null;
                        sideCB.Items.Clear();
                        var cnt = 0;
                        foreach (var side in mod.Sides) sideCB.Items.Add(new ZeroKLobby.Notifications.SideItem(side, mod.SideIcons[cnt++]));
                        var pickedItem = sideCB.Items.OfType<ZeroKLobby.Notifications.SideItem>()
                              .FirstOrDefault(x => x.Side == previousSide);
                        if (pickedItem != null) sideCB.SelectedItem = pickedItem;
                        else if (sideCB.Items.Count > 0) sideCB.SelectedIndex = sideCB.Items.Count-1; // random.Next(sideCB.Items.Count);
                    }
                }
                //end "side" (faction) dropdown list

                ModOptions.Clear();
                currentMod = mod;
                aiList = mod.ModAis;
                missionSlots = mod.MissionSlots;
                Setup_MissionModInfo(mod);
                Set_InfoLabel();
            }
            else
            {
                sideCB.Visible = false;
                currentMod = null;
                aiList = null;
                missionSlots = null;
                myItem.MissionSlot = null;
                botsMissionSlot.Clear();
                if (currentMod != null) Refresh_PlayerBox();
            }
        }

        private void Setup_MissionModInfo(Mod mod)
        {
            myItem.MissionSlot = null;
            if (missionSlots.Count == 0) missionSlots = null;
            if (missionSlots != null)
            {
                foreach (MissionSlot slot in missionSlots.Where(s => s.IsHuman))
                {
                    myItem.MissionSlot = slot;
                    Set_MyBattleStatus(slot.AllyID, slot.TeamID, false);
                    break;
                }

                botsMissionSlot.Clear();
                Bots.Clear(); 
                foreach (var slot in missionSlots.Where(s => s.AiShortName != null))
                {
                    botsMissionSlot.Add(slot);
                    Set_BotBattleStatus(slot.AiShortName, myItem.UserName, slot.TeamID, slot.AllyID, (int)(MyCol)slot.Color,slot.AiVersion);
                }
            }
            else
            {
                if (botsMissionSlot.Count > 0)
                {
                    Bots.Clear();
                    botsMissionSlot.Clear();
                }
            }
            if (currentMod.IsMission)
            {
                //disable some button for mission mod. playerBox shortcut is disabled in Event_PlayerBox_MouseDown()
                spectateCheckBox.Enabled = false;
                addAIButton.Enabled = false;
                editTeamButton.Enabled = false;

                //get targeted map for mission mod
                string mapname;
                var script = mod.MissionScript;
                if (mod.MissionMap != null)
                    mapname = mod.MissionMap;
                else
                {
                    var open = script.IndexOf("mapname", 7, script.Length - 8, StringComparison.InvariantCultureIgnoreCase) + 8;
                    var close = script.IndexOf(';', open);
                    mapname = script.Substring(open, close - open);
                    mapname = mapname.Trim(new char[3]{' ','=','\t'});
                }
                suppressEvent_SelectedIndexChanged = true;
                map_comboBox.SelectedItem = mapname;
                suppressEvent_SelectedIndexChanged = false;
                int selectedView = normalRadioButton.Checked ? 0 : (elevationRadioButton.Checked ? 1 : 2);
                Set_MapImages(mapname, selectedView);

                //get customized modoptions
                {
                    var open = script.IndexOf("[MODOPTIONS]", 7, script.Length - 8, StringComparison.InvariantCultureIgnoreCase) + 12;
                    open = script.IndexOf("{", open) + 1;
                    var close = script.IndexOf("}", open);
                    var options = script.Substring(open, close - open);
                    options = options.Trim();
                    string[] optionList = options.Split(new char[1]{';'}, StringSplitOptions.RemoveEmptyEntries);
                    string[] keypair;
                    for (int i = 0; i < optionList.Length; i++)
                    {
                        optionList[i] = optionList[i].Trim();
                        keypair = optionList[i].Split(new char[1] { '=' });
                        if (keypair[1] != null)
                            ModOptions.Add(keypair[0], keypair[1]);
                    }
                }
            }
            else
            {
                spectateCheckBox.Enabled = true;
                addAIButton.Enabled = true;
                editTeamButton.Enabled = true;
            }
            Refresh_PlayerBox();//update playerbox (in case there's mission slot, or when there was mission slot but no longer, or to update some related icons)
        }

        private void Event_SideCB_DrawItem(object sender, DrawItemEventArgs e) //copied from Notification.BattleBar.cs
        {
            e.DrawBackground();
            e.DrawFocusRectangle();
            if (e.Index < 0 || e.Index >= sideCB.Items.Count) return;
            var item = sideCB.Items[e.Index] as ZeroKLobby.Notifications.SideItem;
            if (item != null)
            {
                if (item.Image != null) e.Graphics.DrawImage(item.Image, e.Bounds.Left, e.Bounds.Top, 16, 16);
                TextRenderer.DrawText(e.Graphics, item.Side, sideCB.Font, new Point(e.Bounds.Left + 16, e.Bounds.Top), sideCB.ForeColor);
            }
            else
                TextRenderer.DrawText(e.Graphics,
                                      sideCB.Items[e.Index].ToString(),
                                      sideCB.Font,
                                      new Point(e.Bounds.Left, e.Bounds.Top),
                                      sideCB.ForeColor);
        }

        private void Set_InfoLabel()
        { 
            if (engine_comboBox.SelectedItem == null)
                infoLabel.Text = "Select engine";
            else if (currentMod!=null && currentMod.IsMission) 
                infoLabel.Text = "Start Mission!";
            else if (map_comboBox.SelectedItem == null)
                infoLabel.Text = "Select map";
            else if (game_comboBox.SelectedItem == null)
                infoLabel.Text = "Select game";
            else if (Bots.Count == 0)
                infoLabel.Text = "Add bots";
        }

        private BattleContext Get_StartContext() //From LobbyClient/Battle.cs
        {
            return new BattleContext() {
                Map = map_comboBox.SelectedItem.ToString(),
                Mod = game_comboBox.SelectedItem.ToString(),
                IsMission = currentMod.IsMission,
                Players = allUser.Where(x=>!Bots.Contains(x)).Select(x=>x.ToPlayerTeam()).ToList(),
                Bots = Bots.Select(x=>x.ToBotTeam()).ToList(),
                Rectangles = Rectangles,
                EngineVersion = engine_comboBox.SelectedItem.ToString()
            };

        }

        private void Event_Startbutton_Click(object sender, EventArgs e)
        {
            if (map_comboBox.SelectedItem == null || engine_comboBox.SelectedItem == null || game_comboBox.SelectedItem == null)
                Set_InfoLabel();
            else
            {
                if (spring.IsRunning) spring.ExitGame();
                spring.SpringExited += Event_SpringExited;
                infoLabel.Text = "Spring starting ...";
                spring.HostGame(Get_StartContext(), "127.0.0.1", 7452, myItem.UserName);
            }
        }

        private void Event_SpringExited(object sender, EventArgs<bool> e)
        {
            this.Invoke(new Action(()=>
            {
                if (infoLabel.Text.StartsWith("Spring starting"))
                    infoLabel.Text = "";
                if (e.Data)
                    infoLabel.Text = "Spring crashed";
            }));

            spring.SpringExited -= Event_SpringExited;
        }

        private void Event_EditTeamButton_Click(object sender, EventArgs e)
        {
            ContextMenu menu = new ContextMenu();

            int freeAllyTeam;
            bool iAmSpectator = myItem.UserBattleStatus.IsSpectator;
            foreach (var allyTeam in Get_ExistingTeams(out freeAllyTeam).Distinct())
            {
                if (iAmSpectator || allyTeam != myItem.UserBattleStatus.AllyNumber)
                {
                    var item = new System.Windows.Forms.MenuItem("Join Team " + (allyTeam + 1));
                    item.Click += (s, e2) => 
                        {
                            Set_MyBattleStatus(allyTeam, Get_FreeTeamID(myItem.UserName),false);
                        };
                    menu.MenuItems.Add(item);
                }
            }

            menu.MenuItems.Add("-");

            var newTeamItem = new System.Windows.Forms.MenuItem("Start New Team");
            newTeamItem.Click += (s, e2) => 
                {
                    myItem.AllyTeam = freeAllyTeam;
                    myItem.offlineUserBattleStatus.AllyNumber = freeAllyTeam;
                    spectateCheckBox.Checked = false;
                    myItem.offlineUserBattleStatus.TeamNumber = Get_FreeTeamID(myItem.UserName);
                    Refresh_PlayerBox();
                };
            menu.MenuItems.Add(newTeamItem);

            if (!myItem.UserBattleStatus.IsSpectator)
            {
                var specItem = new System.Windows.Forms.MenuItem("Spectate");
                specItem.Click += (s, e2) =>
                {
                    spectateCheckBox.Checked = true;
                };
                menu.MenuItems.Add(specItem);
            }

            menu.Show(editTeamButton, new Point(0, 0));
        }

        bool mouseIsDown = false;
        int mouseOnStartBox = -1;
        void Event_MinimapBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Button == MouseButtons.Left)
            {
                mouseIsDown = true;
                //Program.ToolTip.Clear(minimapBox);
            }
        }

        void Event_MinimapBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentMod!=null && currentMod.IsMission) return;
            if (minimapPanel == null || minimapBox.Image == null) return;

            if (map_comboBox.SelectedItem == null)
            {
                if (!infoLabel.Text.StartsWith("Select map"))
                    infoLabel.Text = "Select map";
            }
            else if (mouseIsDown & mouseOnStartBox > -1)
            {
                Cursor = Cursors.Cross;
                BattleRect startRect = Rectangles[mouseOnStartBox];

                //undo the offset due to "Centering" (alignment) of the pictureBox relative to minimapPanel
                float diffWidth_half = (float)(minimapPanel.Width - minimapBox.Image.Width) / 2;
                float diffHeight_half = (float)(minimapPanel.Height - minimapBox.Image.Height) / 2;
                float adjustedX = (e.X - diffWidth_half);
                float adjustedY = (e.Y - diffHeight_half);
                //convert pixel count to 0-200 standard used in Spring infrastructure
                float rectPerImgWidth = (float)BattleRect.Max / minimapBox.Image.Width;
                float rectPerImgHeight = (float)BattleRect.Max / minimapBox.Image.Height;
                //clamp position to within map
                float x = Math.Min(Math.Max (adjustedX * rectPerImgWidth, 10),BattleRect.Max-10);
                float y = Math.Min(Math.Max (adjustedY * rectPerImgHeight, 10),BattleRect.Max-10);
                //set our startbox coordinate
                startRect.Left = (int)(x - 10);
                startRect.Top = (int)(y - 10);
                startRect.Right = (int)(x + 10);
                startRect.Bottom = (int)(y + 10);

                Rectangles[mouseOnStartBox] = startRect;

            }
            else if (mouseOnStartBox > -1)
            {
                BattleRect startRect = Rectangles[mouseOnStartBox];

                float imgWidthPerRect = (float)minimapBox.Image.Width / BattleRect.Max;
                float imgHeightPerRect = (float)minimapBox.Image.Height/BattleRect.Max;

                var left = startRect.Left * imgWidthPerRect;
                var top = startRect.Top * imgHeightPerRect;
                var right = startRect.Right * imgWidthPerRect;
                var bottom = startRect.Bottom * imgHeightPerRect;
                int diffWidth_half = (minimapPanel.Width - minimapBox.Image.Width)/2;
                int diffHeight_half = (minimapPanel.Height - minimapBox.Image.Height)/2;
                if (e.X < left + diffWidth_half || e.X > right + diffWidth_half || e.Y < top + diffHeight_half || e.Y > bottom + diffHeight_half)
                {
                    Cursor = Cursors.Default;
                    mouseOnStartBox = -1;
                }
            }
            else
            {
                float imgWidthPerRect = (float)minimapBox.Image.Width / BattleRect.Max;
                float imgHeightPerRect = (float)minimapBox.Image.Height / BattleRect.Max;
                int diffWidth_half = (minimapPanel.Width - minimapBox.Image.Width) / 2;
                int diffHeight_half = (minimapPanel.Height - minimapBox.Image.Height) / 2;

                foreach (var kvp in Rectangles)
                {
                    BattleRect startRect = kvp.Value;
                    var allyTeam = kvp.Key;
                    var left = startRect.Left * imgWidthPerRect;
                    var top = startRect.Top * imgHeightPerRect;
                    var right = startRect.Right * imgWidthPerRect;
                    var bottom = startRect.Bottom * imgHeightPerRect;
                    
                    if (e.X > left + diffWidth_half && e.X < right + diffWidth_half && e.Y > top + diffHeight_half && e.Y < bottom + diffHeight_half)
                    {
                        Cursor = Cursors.Hand;
                        mouseOnStartBox = allyTeam;
                        break;
                    }
                }
            }
        }

        void Event_MinimapBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                mouseIsDown = false;
                //if (map_comboBox.SelectedItem != null)
                    //Program.ToolTip.SetMap(minimapBox, (string)map_comboBox.SelectedItem);
            }
        }
    }
}
