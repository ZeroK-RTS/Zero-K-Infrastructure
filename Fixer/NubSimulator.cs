using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using Newtonsoft.Json;
using ZkData;
using ZkData.UnitSyncLib;

namespace Fixer
{
    class NubSimulator
    {
        public void RunNub(int num)
        {
            var tas = new TasClient("Nubotron");
            var name = "TestNub" + num;
            var ord = num / 16;
            var batname = "Test " + ord;

            //tas.Input += (sender, args) => { Console.WriteLine(" < {0}", args); };
            //tas.Output += (sender, args) => { Console.WriteLine(" > {0}", args); };

            tas.Connected += (sender, args) => {
                tas.Login(name, "dummy");
            };

            tas.ConnectionLost += (sender, args) => { tas.Connect(GlobalConst.LobbyServerHost, GlobalConst.LobbyServerPort); Console.WriteLine("disconnected"); };


            tas.LoginAccepted += (sender, args) => { Console.WriteLine(name + " accepted"); };
            tas.LoginDenied += (sender, args) => { tas.Register(name, "dummy"); };

            tas.RegistrationAccepted += (sender, args) => { tas.Login(name, "dummy"); };
            tas.RegistrationDenied += (sender, response) => { Console.WriteLine(name + "registration denied"); };

            

            tas.UserAdded += (sender, args) => {
                if (args.Name == name) {
                    tas.JoinChannel("bots");
                    if (num%16 == 0) tas.OpenBattle(new BattleHeader()
                    {
                        Title = batname,
                        MaxPlayers = 16,
                    });
                    else {
                        var bat = tas.ExistingBattles.Values.FirstOrDefault(x => x.Title == batname);
                        if (bat != null) tas.JoinBattle(bat.BattleID);
                    }
                }
            };
            tas.BattleFound += (sender, args) => {
                if (args.Title == batname) {
                    //await Task.Delay(200);
                    tas.JoinBattle(args.BattleID);
                }
            };
            

            tas.Connect(GlobalConst.LobbyServerHost, GlobalConst.LobbyServerPort);
            /*Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(rand.Next(400000));
                    tas.Say(SayPlace.Channel, "zk", sent.GetNext(), false);
                }
            }, TaskCreationOptions.LongRunning);*/
        }

        SentenceGenerator sent = new SentenceGenerator();
        Random rand = new Random();

        public async Task SpawnMany()
        {
            SynchronizationContext.SetSynchronizationContext(null);
            ThreadPool.SetMaxThreads(1000, 1000);
            for (int i = 0; i < 400; i++) {
                int i1 = i;
                //Thread.Sleep(100);
                RunNub(i1);
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
