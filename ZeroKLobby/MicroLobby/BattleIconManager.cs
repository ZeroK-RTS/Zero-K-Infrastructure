using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using LobbyClient;
using ZkData;
using ZeroKLobby;

namespace ZeroKLobby.MicroLobby
{
    class BattleIconManager
    {
        readonly ObservableCollection<BattleIcon> battleIcons = new ObservableCollection<BattleIcon>();
        public ObservableCollection<BattleIcon> BattleIcons { get { return battleIcons; } }
        public bool Running { get; set; }
        public event EventHandler<EventArgs<BattleIcon>> BattleAdded = delegate { };
        public event EventHandler<EventArgs<BattleIcon>> BattleChanged = delegate { };
        public event EventHandler<EventArgs<BattleIcon>> RemovedBattle = delegate { };

        public BattleIconManager()
        {
            Program.TasClient.BattleFound += TasClient_BattleFound;
            Program.TasClient.BattleRemoved += TasClient_BattleEnded;
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
            //string modName = null;
            //foreach (var game in KnownGames.List) if (game.Regex.IsMatch(battle.ModName)) modName = game.Shortcut;
            var founder = battle.Founder;
            var battleIcon = new BattleIcon(battle) { IsInGame = founder.IsInGame};
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
                                                       		Program.MainWindow.InvokeFunc(() =>
                                                       			{
                                                       				battleIcon.MinimapImage = image;
                                                       				BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
                                                       			});
                                                       	},
                                                       a => Program.MainWindow.InvokeFunc(() =>
                                                           {
                                                               if (battleIcon != null)
                                                               {
                                                                   battleIcon.MinimapImage = null;
                                                                   BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
                                                               }
                                                           }));
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
            var battle = Program.TasClient.ExistingBattles.Values.SingleOrDefault(b => b.Users.ContainsKey(e.Data));
            if (battle != null)
            {
                var battleIcon = GetBattleIcon(battle.BattleID);
                battleIcon.SetPlayers();
                BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
            }
        }

        void TasClient_BattleEnded(object sender, Battle battle)
        {
            RemoveBattleIcon(battle);
        }

        void TasClient_BattleFound(object sender, Battle battle)
        {
            var battleID = battle.BattleID;
            var battleIcon = AddBattle(battleID);
            BattleAdded(this, new EventArgs<BattleIcon>(battleIcon));
        }

        void TasClient_BattleInfoChanged(object sender, OldNewPair<Battle> pair)
        {
            var battle = pair.New;
            var founder = battle.Founder;
            var battleIcon = GetBattleIcon(battle.BattleID);
            battleIcon.SetPlayers();
            battleIcon.IsInGame = founder.IsInGame;
            BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
        }

        void TasClient_BattleMapChanged(object sender, OldNewPair<Battle> pair)
        {
            var battleIcon = GetBattleIcon(pair.New.BattleID);
            LoadMinimap(pair.New.MapName, battleIcon);
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

        void TasClient_UserStatusChanged(object sender, OldNewPair<User> p)
        {
            var battle = Program.TasClient.ExistingBattles.Values.SingleOrDefault(b => b.Founder.Name == p.New.Name);
            if (battle == null) return;
            var founder = battle.Founder;
            var battleIcon = GetBattleIcon(battle);
            battleIcon.IsInGame = founder.IsInGame;
            BattleChanged(this, new EventArgs<BattleIcon>(battleIcon));
        }
    }
}