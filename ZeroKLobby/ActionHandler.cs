using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Annotations;
using LobbyClient;
using PlasmaDownloader;
using ZkData;
using ZkData.UnitSyncLib;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;

namespace ZeroKLobby
{
    /// <summary>
    /// Central place for gui-centric actions invloving one or more controls
    /// </summary>
    public static class ActionHandler
    {
        /// <summary>
        /// Changes user's desired spectator state of battle - does not actually send tasclient state change
        /// </summary>
        /// <param name="state">new desired state</param>
        /// <returns>true if change allowed</returns>
        public static bool ChangeDesiredSpectatorState(bool state)
        {
            return Program.BattleBar.ChangeDesiredSpectatorState(state);
        }

        /// <summary>
        /// Closes a channel tab (not PM tab) in the chat tab
        /// </summary>
        public static void CloseChannel(string key)
        {
            Program.MainWindow.ChatTab.CloseChannelTab(key);
        }

        /// <summary>
        /// Closes a private message tab in the chat tab
        /// </summary>
        public static void ClosePrivateChat(string key)
        {
            Program.MainWindow.ChatTab.ClosePrivateTab(key);
        }


        /// <summary>
        /// Hides the next PM that has a specific string as message
        /// </summary>
        public static void HidePM(string text)
        {
            EventHandler<CancelEventArgs<TasSayEventArgs>> hideMessage = null;
            hideMessage = (s, e) =>
              {
                  if (e.Data.Place == SayPlace.User && e.Data.Text == text)
                  {
                      e.Cancel = true;
                      Program.TasClient.PreviewSaid -= hideMessage;
                  }
              };
            Program.TasClient.PreviewSaid += hideMessage;
        }

        /// <summary>
        /// Make this client join an ally team, join a free team, and unspec
        /// </summary>
        /// <param name="allyTeam"></param>
        public static void JoinAllyTeam(int allyTeam)
        {
            if (ChangeDesiredSpectatorState(false))
            {
                Program.TasClient.ChangeMyBattleStatus(false, team:Program.TasClient.MyBattle.GetFreeTeamID(Program.TasClient.UserName), ally:allyTeam);
            }
        }


        /// <summary>
        /// Joins battle manually
        /// </summary>
        public static void JoinBattle(int battleID, string password)
        {
            EventHandler<Battle> battleJoinHandler = null;

            battleJoinHandler = ((s, e) =>
              {
                  Program.TasClient.BattleJoined -= battleJoinHandler;
                  if (Program.TasClient.MyBattle == null || !Program.TasClient.MyBattle.IsQueue) NavigationControl.Instance.Path = "chat/battle";
              });


            Program.TasClient.BattleJoined += battleJoinHandler;

            //Program.JugglerBar.Deactivate();

            Program.BattleBar.StartManualBattle(battleID, password);
        }


        /// <summary>
        /// Joins same battle as player
        /// </summary>
        public static void JoinPlayer(string name)
        {
            var client = Program.TasClient;
            if (!client.IsLoggedIn) return;
            User user;
            if (client.ExistingUsers.TryGetValue(name, out user) && user.IsInBattleRoom)
            {
                var bat = client.ExistingBattles.Values.FirstOrDefault(x => x.Users.ContainsKey(name));
                if (bat != null)
                {
                    string password = null;
                    // this could possibly use braces, but I'm crazy
                    if (bat.IsPassworded)
                        using (var form = new AskBattlePasswordForm(bat.Founder.Name))
                            if (form.ShowDialog() == DialogResult.OK)
                                password = form.Password;

                    JoinBattle(bat.BattleID, password);
                }
            }
        }

        public static void JoinSlot(MissionSlot slot)
        {
            if (ChangeDesiredSpectatorState(false))
            {
                Program.TasClient.ChangeMyBattleStatus(false,null,slot.AllyID,slot.TeamID);
            }
        }

        /// <summary>
        /// Selects Next Button
        /// </summary>
        public static void NextButton()
        {
            Program.MainWindow.navigationControl.Path = Program.MainWindow.ChatTab.GetNextTabPath();
        }

        public static void PerformAction(string actionString)
        {
            if (!String.IsNullOrEmpty(actionString))
            {
                var idx = actionString.IndexOf(':');

                var command = actionString;
                var arg = "";
                if (idx > -1)
                {
                    command = actionString.Substring(0, idx);
                    arg = actionString.Substring(idx + 1);
                }
                switch (command)
                {
                    case "logout":
                        Program.TasClient.RequestDisconnect();
                        Program.Conf.LobbyPlayerName = "";
                        Program.Conf.LobbyPlayerPassword = "";
                        Program.ConnectBar.TryToConnectTasClient();
                        break;

                    case "start_mission":
                        StartMission(arg);
                        break;

                    case "start_replay":
                        var parts = arg.Split(',');
                        StartReplay(parts[0], parts[1], parts[2], parts[3]);
                        break;

                    case "host_mission":
                        SpawnAutohost(arg, String.Format("{0}'s {1}", Program.Conf.LobbyPlayerName, arg), null, null);
                        break;
                    case "start_script_mission":
                        StartScriptMission(arg);
                        break;

                    case "select_map":
                        if (Program.TasClient.MyBattle != null) Program.TasClient.Say(SayPlace.Battle, null, "!map " + arg, false);
                        else
                        {
                            var name = String.Format("{0}'s game", Program.Conf.LobbyPlayerName);
                            SpawnAutohost(KnownGames.List.First(x => x.IsPrimary).RapidTag, name, null, new List<string> { "!map " + arg });
                        }
                        break;

                    case "add_friend":
                        Program.FriendManager.AddFriend(arg);
                        break;

                    case "join_battle":
                        JoinPlayer(arg);
                        break;
                    case "join_player":
                        JoinPlayer(arg);
                        break;

                    case "benchmark":
                        var bench = new Benchmarker.MainForm(Program.SpringPaths, Program.SpringScanner, Program.Downloader);
                        bench.Show();
                        break;

                }
            }
        }

        /// <summary>
        /// Selects Previous Button
        /// </summary>
        public static void PrevButton()
        {
            Program.MainWindow.navigationControl.Path = Program.MainWindow.ChatTab.GetPrevTabPath();
        }


        /// <summary>
        /// Displays a window with the debug log
        /// </summary>
        public static void ShowLog()
        {
            Program.MainWindow.DisplayLog();
        }

        static SpringsettingForm currentForm = null; //using static field so that we know whether the same window is already open or not
        /// <summary>
        /// Displays springsettings window
        /// </summary>
        public static void ShowSpringsetting()
        {
            if (currentForm != null && !currentForm.IsDisposed) currentForm.Dispose();
            currentForm = new SpringsettingForm();
            currentForm.Show();
        }

        static TextColoringPanel currentColorForm = null;
        /// <summary>
        /// Displays colouring window
        /// </summary>
        public static void ShowColoringPanel(SendBox currentSendbox)
        {
            if (currentColorForm != null && !currentColorForm.IsDisposed) currentColorForm.Dispose();
            currentColorForm = new TextColoringPanel(currentSendbox);
            currentColorForm.Show();
        }

        static HexToUnicodeConverter currentTranslatorForm = null;
        /// <summary>
        /// Displays number-to-unicode-char translator window
        /// </summary>
        public static void ShowUnicodeTranslator()
        {
            if (currentTranslatorForm != null && !currentTranslatorForm.IsDisposed) currentTranslatorForm.Dispose();
            currentTranslatorForm = new HexToUnicodeConverter();
            currentTranslatorForm.Show();
        }

        public static void SpawnAutohost(string gameName, string battleTitle, string password, IEnumerable<string> springieCommands)
        {
            var hostSpawnerName = SpringieCommand.GetHostSpawnerName(gameName);
            if (hostSpawnerName == null) {
                MessageBox.Show("Unable to locate AutoHost for given game type", "Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }
            

            var spawnCommand = SpringieCommand.Spawn(gameName, battleTitle, password);

            var waitingBar = WarningBar.DisplayWarning("Waiting for AutoHost to start","Waiting for room");

            EventHandler<CancelEventArgs<TasSayEventArgs>> joinGame = null;
            joinGame = (s, e) =>
              {
                  if (e.Data.Place == SayPlace.User && (e.Data.Text == spawnCommand.Reply))
                  {
                      e.Cancel = true;
                      Program.NotifySection.RemoveBar(waitingBar);
                      Program.TasClient.PreviewSaid -= joinGame;
                      var myHostName = e.Data.UserName;
                      var battle = Program.TasClient.ExistingBattles.Values.First(b => b.Founder.Name == myHostName);

                      EventHandler<Battle> battleJoined = null;
                      battleJoined = (s2, e2) =>
                        {
                            if (e2.BattleID == battle.BattleID)
                            {
                                if (springieCommands != null)
                                {
                                    foreach (var command in springieCommands)
                                    {
                                        HidePM(command);
                                        Program.TasClient.Say(SayPlace.User, myHostName, command, false);
                                    }
                                }
                                HidePM("!map");
                                Program.TasClient.Say(SayPlace.User, myHostName, "!map", false);
                                Program.TasClient.BattleJoined -= battleJoined;
                            }
                        };

                      Program.TasClient.BattleJoined += battleJoined;
                      JoinBattle(battle.BattleID, password);
                      NavigationControl.Instance.Path = "chat/battle";
                  }
              };

            Program.TasClient.PreviewSaid += joinGame;
            HidePM(spawnCommand.Command);
            Program.TasClient.Say(SayPlace.User, hostSpawnerName, spawnCommand.Command, false);
        }


        /// <summary>
        /// Set this client as spectator
        /// </summary>
        public static void Spectate()
        {
            if (ChangeDesiredSpectatorState(true))
            {
                Program.TasClient.ChangeMyBattleStatus(true);
            }
        }

        public static void StartMission(string name)
        {
            Program.NotifySection.AddBar(new MissionBar(name));
        }


        public static void StartReplay(string url, string mod, string map, string engine)
        {
            Program.NotifySection.AddBar(new ReplayBar(url, mod, map, engine));
        }

        public static void StartScriptMission(string name)
        {
            try
            {
                var serv = GlobalConst.GetContentService();
                SinglePlayerBar.DownloadAndStartMission(serv.GetScriptMissionData(name));
            }
            catch (WebException ex)
            {
                Trace.TraceWarning("Problem starting script mission {0}: {1}", name, ex);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error starting mission {0}: {1}", name, ex);
            }
        }

        public static void StopBattle()
        {
            Program.BattleBar.Stop();
        }


        /// <summary>
        /// Unspec this client
        /// </summary>
        public static void UnSpec()
        {
            if (ChangeDesiredSpectatorState(false))
            {
                if (Program.TasClient.MyBattle != null) {
                    Program.TasClient.ChangeMyBattleStatus(false);
                }
            }
        }

        /*public static void JoinBattleSpec(int battleId)
        {
            Battle bat;
            if (!Program.TasClient.ExistingBattles.TryGetValue(battleId, out bat)) return;
            Program.TasClient.Say(SayPlace.User, bat.Founder.Name, string.Format("!adduser {0}", Program.TasClient.UserName),false);

            var de = Program.Downloader.GetAndSwitchEngine(bat.EngineVersion);
            var dm = Program.Downloader.GetResource(DownloadType.MAP, bat.MapName);
            var dg = Program.Downloader.GetResource(DownloadType.MOD, bat.ModName);
        
            ZkData.Utils.StartAsync(() =>
            {
                if (de != null)
                {
                    de.WaitHandle.WaitOne();
                    if (de.IsComplete == false) return;
                }
                if (dm != null)
                {
                    dm.WaitHandle.WaitOne();
                    if (dm.IsComplete == false) return;
                }
                if (dg != null)
                {
                    dg.WaitHandle.WaitOne();
                    if (dg.IsComplete == false) return;
                }


                var spring = new Spring(Program.SpringPaths);
                
                Thread.Sleep(200);// give host time to adduser
                spring.HostGame(Program.TasClient, null, null, null, Program.Conf.UseSafeMode, battleOverride: bat);
            });
        }*/
    }
}