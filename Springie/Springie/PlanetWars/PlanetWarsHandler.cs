#region using

using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using PlasmaShared.ContentService;
using PlasmaShared.SpringieInterfaceReference;
using Springie.autohost;

#endregion

namespace Springie.PlanetWars
{
    public class PlanetWarsHandler: IDisposable
    {
        readonly AutoHost autoHost;
        readonly SpringieService serv = new SpringieService();
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

        public bool BalanceTeams()
        {
            try
            {
                if (tas.MyBattle.NonSpectatorCount < 1) return false;

                var balance = serv.BalanceTeams(tas.MyBattle.GetContext());
                if (!string.IsNullOrEmpty(balance.Message))  autoHost.SayBattle(balance.Message);
                if (balance != null && balance.Players != null)
                {
                    foreach (var user in tas.MyBattle.Users.Where(x => !x.IsSpectator && !balance.Players.Any(y => y.Name == x.Name))) tas.ForceSpectator(user.Name); // spec those that werent in response
                    foreach (var user in balance.Players.Where(x => x.IsSpectator)) tas.ForceSpectator(user.Name);
                    foreach (var user in balance.Players.Where(x => !x.IsSpectator))
                    {
                        tas.ForceTeam(user.Name, user.TeamID);
                        tas.ForceAlly(user.Name, user.AllyID);
                    }
                    if (balance.DeleteBots) foreach (var b  in tas.MyBattle.Bots) tas.RemoveBot(b.Name);
                    foreach (var b in balance.Bots)
                    {
                        var botStatus = tas.MyBattleStatus.Clone();
                        botStatus.TeamNumber = b.TeamID;
                        botStatus.AllyNumber = b.AllyID;
                        tas.AddBot(b.BotName, botStatus, botStatus.TeamColor, b.BotAI);
                    }

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
            try
            {
                var ret = serv.AutohostPlayerJoined(tas.MyBattle.GetContext(), tas.ExistingUsers[name].LobbyID);
                if (ret != null)
                {
                    if (!string.IsNullOrEmpty(ret.PrivateMessage))
                    {
                        tas.Say(TasClient.SayPlace.User, name, ret.PrivateMessage, false);
                    }
                    if (!string.IsNullOrEmpty(ret.PublicMessage))
                    {
                        tas.Say(TasClient.SayPlace.Battle, "", ret.PublicMessage, true);
                    }
                    if (ret.ForceSpec) tas.ForceSpectator(name);
                    if (ret.Kick) tas.Kick(name);
                }
            }
            catch (Exception ex)
            {
                autoHost.SayBattle("PlanetWars error: " + ex);
            }
        }

        void VerifyMap()
        {
            try
            {
                if (tas.MyBattle != null && !spring.IsRunning)
                {
                    var botList =
                        tas.MyBattle.Bots.Where(x => !x.IsSpectator).Select(
                            x => new BotTeam() { AllyID = x.AllyNumber, BotName = x.Name, BotAI = x.aiLib, Owner = x.owner, TeamID = x.TeamNumber }).
                            ToArray();

                    var map = serv.GetRecommendedMap(tas.MyBattle.GetContext());

                    if (map.MapName != null)
                    {
                        if (tas.MyBattle.MapName != map.MapName)
                        {
                            autoHost.ComMap(TasSayEventArgs.Default, map.MapName);
                            autoHost.SayBattle(map.Message);
                            foreach (var c in map.SpringieCommands.Split('\n').Where(x => !string.IsNullOrEmpty(x))) {
                                autoHost.RunCommand(c);
                            }
                        }
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