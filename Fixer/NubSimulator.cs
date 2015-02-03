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

            tas.ConnectionLost += (sender, args) => { Console.WriteLine("disconnected"); };


            tas.LoginAccepted += (sender, args) => { Console.WriteLine(name + " accepted"); };
            tas.LoginDenied += (sender, args) => { tas.Register(name, "dummy"); };

            

            tas.UserAdded += (sender, args) => {
                if (args.Name == name) {
                    tas.JoinChannel("bots");
                    if (num%16 == 0)
                        tas.OpenBattle(new Battle("91.0", null, 4955, 16, "SmallDivide", "Test " + ord,"Zero-K v1.3.1.15"));
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
        }

        public void RunNub2(int num)
        {
            var con = new ServerConnection();
            con.CommandRecieved += (sender, args) => {
                if (args.Command == "TASServer") {
                    con.SendCommand("LOGIN");
                }
            };

            con.Connect(GlobalConst.LobbyServerHost, GlobalConst.LobbyServerPort);
            
        }


        public async Task SpawnMany()
        {
            ThreadPool.SetMaxThreads(1000, 1000);
            for (int i = 0; i < 100; i++) {
                int i1 = i;
                //Thread.Sleep(100);
                RunNub(i1);
            }

         }


    }
}
