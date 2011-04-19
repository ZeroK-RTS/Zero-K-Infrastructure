#region using

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Timers;
using LobbyClient;
using PlasmaShared.UnitSyncLib;
using Springie.autohost;

#endregion

namespace Springie.PlanetWars
{
    public class PlanetWarsHandler: IDisposable
    {

        AutoHost autoHost;



        TasClient tas;
        Timer timer = new Timer();
    	Spring spring;


    	public PlanetWarsHandler(AutoHost autoHost, TasClient tas, AutoHostConfig config, Spring spring)
        {
        	this.spring = spring;
            this.autoHost = autoHost;
            this.tas = tas;

            timer.Interval = 2000;
            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Elapsed -= timer_Elapsed;
        }

        public void MapChanged()
        {
            try
            {
                string name = tas.MyBattle.MapName;


                autoHost.SayBattle(String.Format("Welcome to "));
								//ICollection<string> notifyList = server.GetPlayersToNotify(account, name, ReminderEvent.OnBattlePreparing);
            }
            catch (Exception ex)
            {
                autoHost.SayBattle(string.Format("Error setting planet starting boxes: {0}", ex));
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


        public static void SpringMessage(string text)
        {
        }

        public bool StartGame(TasSayEventArgs e)
        {
            try
            {
                if (!autoHost.ComFix(e, "silent"))
                {
                    autoHost.Respond(e, "Teams were not fixed, fixing");
                    return false;
                }

                Battle bat = tas.MyBattle;
                Mod mod = Program.main.UnitSyncWrapper.GetModInfo(bat.ModName);
                foreach (var grouping in bat.Users.Where(x => !x.IsSpectator).GroupBy(x => x.AllyNumber))
                {
                }

                string currentMapName = bat.MapName;
                autoHost.Respond(e, "This mod does not support PlanetWars");
                return false;
            }
            catch (Exception ex)
            {
                autoHost.SayBattle(string.Format("Error when checking PlanetWars teams: {0}", ex), false);
                return false;
            }
        }

        public void UserJoined(string name)
        {
            try
            {
            }
            catch (Exception ex)
            {
                autoHost.SayBattle("PlanetWars error: " + ex);
            }
        }



        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                Battle b = tas.MyBattle;
                if (!autoHost.ComFix(TasSayEventArgs.Default, "silent")) return;


                Mod mod = Program.main.UnitSyncWrapper.GetModInfo(b.ModName);

                List<string> sides = mod.Sides.ToList();
                bool teamsOk = true;
            }
            catch (Exception ex)
            {
                autoHost.SayBattle("Problem with PlanetWars:" + ex);
            }
            finally
            {
                timer.Start();
            }
        }
    }
}