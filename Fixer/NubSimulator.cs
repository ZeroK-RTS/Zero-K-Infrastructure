using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;

namespace Fixer
{
    class NubSimulator
    {
        public void RunNub(int num)
        {
            var tas = new TasClient("Nubotron");

            var maps = AutoRegistrator.RegistratorRes.campaignMaps.Split('\n');
            var name = "TestNub" + num;
            var ord = num / 16;

            //tas.Input += (sender, args) => { Console.WriteLine(" < {0}", args); };
            //tas.Output += (sender, args) => { Console.WriteLine(" > {0}", args); };

            tas.Connected += (sender, args) =>
            {
                tas.Login(name, "dummy");
            };

            tas.ConnectionLost += (sender, args) => { tas.Connect(GlobalConst.LobbyServerHost, GlobalConst.LobbyServerPort); Console.WriteLine("disconnected"); };


            tas.LoginAccepted += (sender, args) => { Console.WriteLine(name + " accepted"); };
            tas.LoginDenied += (sender, args) => { tas.Register(name, "dummy"); };

            tas.RegistrationAccepted += (sender, args) => { tas.Login(name, "dummy"); };
            tas.RegistrationDenied += (sender, response) => { Console.WriteLine(name + "registration denied"); };



            tas.Connect(GlobalConst.LobbyServerHost, GlobalConst.LobbyServerPort);

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(rand.Next(10000));
                    if (tas.IsLoggedIn)
                    {
                        await tas.LeaveBattle();
                        if (tas.ExistingBattles.Count < 20)
                            await tas.OpenBattle(new BattleHeader()
                            {
                                Title = "" + name,
                                MaxPlayers = 16,
                                Mode = AutohostMode.None,
                                Engine = tas.ServerWelcome.Engine,
                                Game = tas.ServerWelcome.Game,
                                Map = maps[rand.Next(maps.Length)],
                            });
                        else
                        {
                            var bats = tas.ExistingBattles.Values.ToList();
                            if (bats.Count > 0)
                            {
                                var bat = bats[rand.Next(bats.Count)];
                                if (bat != null) tas.JoinBattle(bat.BattleID);
                            }
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);


            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(rand.Next(50000));
                    if (tas.IsLoggedIn)
                    {
                        tas.Say(SayPlace.Channel, "zk", sent.GetNext(), false);
                    }
                }
            }, TaskCreationOptions.LongRunning);

            Task.Factory.StartNew(async () =>
            {
                bool cycler = false;
                while (true)
                {
                    await Task.Delay(rand.Next(5000));
                    if (tas.IsLoggedIn)
                    {
                        await tas.ChangeMyUserStatus(cycler, cycler);
                        //await tas.ChangeMyBattleStatus(cycler, SyncStatuses.Synced, 1);
                        cycler = !cycler;
                    }
                }
            }, TaskCreationOptions.LongRunning);

        }

        SentenceGenerator sent = new SentenceGenerator();
        Random rand = new Random();

        public async Task SpawnMany()
        {
            SynchronizationContext.SetSynchronizationContext(null);
            ThreadPool.SetMaxThreads(1000, 1000);
            for (int i = 0; i < 100; i++)
            {
                int i1 = i;
                Thread.Sleep(100);
                RunNub(i1 + 100);
            }

        }


    }

    public class SentenceGenerator
    {
        Random rand = new Random();
        string[] article = { "the", "a", "one", "some", "any", };
        string[] noun = { "boy", "girl", "dog", "town", "car", };
        string[] verb = { "drove", "jumped", "ran", "walked", "skipped", };
        string[] preposition = { "to", "from", "over", "under", "on", };


        public string GetNext()
        {
            int randomarticle = rand.Next(article.Length);
            int randomnoun = rand.Next(noun.Length);
            int randomverb = rand.Next(verb.Length);
            int randompreposition = rand.Next(preposition.Length);
            int randomarticle2 = rand.Next(article.Length);
            int randomnoun2 = rand.Next(noun.Length);

            var txt = String.Format("{0} {1} {2} {3} {4} {5}", article[randomarticle], noun[randomnoun], verb[randomverb], preposition[randompreposition], article[randomarticle2], noun[randomnoun2]);
            return txt;
        }
    }
}
