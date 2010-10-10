using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared;
using SpringDownloader;

namespace SpringDownloader.MicroLobby
{
    class BattleIconManager
    {
        readonly List<BattleIcon> battleIcons = new List<BattleIcon>();
        Control control;
        public IEnumerable<BattleIcon> BattleIcons { get { return battleIcons; } }
        public bool Running { get; set; }
        public event EventHandler<EventArgs<BattleIcon>> BattleAdded = delegate { };
        public event EventHandler<EventArgs<BattleIcon>> BattleChanged = delegate { };
        public event EventHandler<EventArgs<BattleIcon>> RemovedBattle = delegate { };

        public BattleIconManager(Control control)
        {
            this.control = control;
            Program.TasClient.BattleFound += TasClient_BattleFound;
            Program.TasClient.BattleEnded += TasClient_BattleEnded;
            Program.TasClient.BattleMapChanged += TasClient_BattleMapChanged;
            Program.TasClient.BattleInfoChanged += TasClient_BattleInfoChanged;
            Program.TasClient.BattleUserJoined += TasClient_BattleUserJoined;
            Program.TasClient.BattleUserLeft += TasClient_BattleUserLeft;
            Program.TasClient.UserStatusChanged += TasClient_UserStatusChanged;
            Program.FriendManager.FriendAdded += HandleFriendChanged;
            Program.FriendManager.FriendRemoved += HandleFriendChanged;
            Program.TasClient.ConnectionLost += TasClient_ConnectionLost;
            Program.TasClient.LoginAccepted += TasClient_LoginAccepted;
            foreach (var battle in Program.TasClient.ExistingBattles.Values) AddBattle(battle.BattleID);
            Running = true;
        }


        public BattleIcon GetBattleIcon(int battleID)
        {
            return BattleIcons.SingleOrDefault(b => b.Battle.BattleID == battleID);
        }

        public bool HasBattleIcon(int battleID)
        {
            return BattleIcons.Any(b => b.Battle.BattleID == battleID);
        }


        BattleIcon AddBattle(int battleID)
        {
            var battle = Program.TasClient.ExistingBattles[battleID];
            string modName = null;
            foreach (var game in StartPage.GameList) if (Regex.IsMatch(battle.ModName, game.Regex)) modName = game.Shortcut;
            var founder = Program.TasClient.ExistingUsers[battle.Founder];
            var battleIcon = new BattleIcon(battle) { IsInGame = founder.IsInGame, ModShortcut = modName };
            try
            {
                battleIcons.Add(battleIcon);
                LoadMinimap(battle.MapName, battleIcon);
                battleIcon.SetPlayers();
            } catch
            {
                battleIcon.Dispose();
                throw;
            }
            return battleIcon;
        }

        BattleIcon GetBattleIcon(Battle battle)
        {
            return BattleIcons.SingleOrDefault(b => b.Battle == battle);
        }

        void LoadMinimap(string mapName, BattleIcon battleIcon)
        {
            Program.SpringScanner.MetaData.GetMapAsync(mapName,
                                                       (map, minimap, heightmap, metalmap) =>
                                                       	{
																													if (map != null && map.Name != battleIcon.Battle.MapName) return;
                                                       		Image image = null;
																													if (minimap != null && minimap.Length != 0) image = Image.FromStream(new MemoryStream(minimap));
                                                       		control.Invoke(new Action(() =>
                                                       			{
                                                       				battleIcon.MinimapImage = image;
                                                       				BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
                                                       			}));
                                                       	},
                                                       a => control.Invoke(new Action(() =>
                                                           {
                                                               if (battleIcon != null)
                                                               {
                                                                   battleIcon.MinimapImage = null;
                                                                   BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
                                                               }
                                                           })));
        }


        void RemoveBattleIcon(Battle battle)
        {
            var battleIcon = GetBattleIcon(battle);
            if (battleIcon != null)
            {
                battleIcon.Dispose();
                battleIcons.Remove(battleIcon);
                RemovedBattle(this, new EventArgs<BattleIcon>(battleIcon));
            }
        }

        void Reset()
        {
            var icons = battleIcons.ToArray();
            battleIcons.Clear();
            foreach (var icon in icons) RemovedBattle(this, new EventArgs<BattleIcon>(icon));
        }

        void HandleFriendChanged(object sender, EventArgs<string> e)
        {
            var battle = Program.TasClient.ExistingBattles.Values.SingleOrDefault(b => b.Users.Any(u => u.Name == e.Data));
            if (battle != null)
            {
                var battleIcon = GetBattleIcon(battle.BattleID);
                battleIcon.SetPlayers();
                BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
            }
        }

        void TasClient_BattleEnded(object sender, EventArgs<Battle> e)
        {
            RemoveBattleIcon(e.Data);
        }

        void TasClient_BattleFound(object sender, TasEventArgs e)
        {
            var battleID = Int32.Parse(e.ServerParams[0]);
            var battleIcon = AddBattle(battleID);
            BattleAdded(this, new EventArgs<BattleIcon>(battleIcon));
        }

        void TasClient_BattleInfoChanged(object sender, BattleInfoEventArgs e1)
        {
            var battleID = e1.BattleID;
            var battle = Program.TasClient.ExistingBattles[battleID];
            var founder = Program.TasClient.ExistingUsers[battle.Founder];
            var battleIcon = GetBattleIcon(battleID);
            battleIcon.SetPlayers();
            battleIcon.IsInGame = founder.IsInGame;
            BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
        }

        void TasClient_BattleMapChanged(object sender, BattleInfoEventArgs e1)
        {
            var battleIcon = GetBattleIcon(e1.BattleID);
            LoadMinimap(e1.MapName, battleIcon);
        }

        void TasClient_BattleUserJoined(object sender, BattleUserEventArgs e1)
        {
            var battleID = e1.BattleID;
            var battleIcon = GetBattleIcon(battleID);
            battleIcon.SetPlayers();
            BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
        }

        void TasClient_BattleUserLeft(object sender, BattleUserEventArgs e)
        {
            var battleID = e.BattleID;
            var battleIcon = GetBattleIcon(battleID);
            battleIcon.SetPlayers();
            BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
        }

        void TasClient_ConnectionLost(object sender, TasEventArgs e)
        {
            Reset();
        }

        void TasClient_LoginAccepted(object sender, TasEventArgs e)
        {
            Reset();
        }

        void TasClient_UserStatusChanged(object sender, TasEventArgs e)
        {
            var userName = e.ServerParams[0];
            var battle = Program.TasClient.ExistingBattles.Values.SingleOrDefault(b => b.Founder == userName);
            if (battle == null) return;
            var founder = Program.TasClient.ExistingUsers[battle.Founder];
            var battleIcon = GetBattleIcon(battle);
            battleIcon.IsInGame = founder.IsInGame;
            BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
        }
    }
}