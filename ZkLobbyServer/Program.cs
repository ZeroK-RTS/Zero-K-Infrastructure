using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var pr = new Program();
            pr.AcceptClients();
            Console.ReadLine();
        }

        public static Encoding Encoding = new UTF8Encoding(false);

        public async Task AcceptClients()
        {
            var listener = new TcpListener(IPAddress.Any, GlobalConst.LobbyServerPort);
            
            listener.Start();
            while (true) {
                if (listener.Pending()) {
                    var client = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("Accepting conneciton {0}", client);
                    Task.Run(() => { ProcessClient(client); });

                }
            }
        }

        public async Task ProcessClient(TcpClient client)
        {
            try {
                var stream = client.GetStream();
                var reader = new StreamReader(stream, Encoding);
                await Write(stream, "TASServer 91.0 Test 123");
                while (true) {
                    var line = await reader.ReadLineAsync();
                    Console.WriteLine(line);
                    if (line != null && line.StartsWith("LOGIN")) {
                        await Task.Run(async () => {
                            Account acc;
                            using (var db = new ZkDataContext()) {
                                acc = Account.AccountVerify(db, "test", "test");
                            }
                            await Write(stream, "LOGINACCEPTED " + acc.Name);
                        });
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }

        public static async Task Write(Stream stream, string text)
        {
            Console.WriteLine("sending " + text);
            var data = Encoding.GetBytes(text + "\r\n");
            await stream.WriteAsync(data,0,data.Length);
            Console.WriteLine("sent " + text);
        }
    }
}
