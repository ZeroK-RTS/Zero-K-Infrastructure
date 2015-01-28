using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Essential.Diagnostics;

namespace ZkLobbyServer
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ColoredConsoleTraceListener() { Template = "{DateTime:HH:mm:ss} {message}" });
            var server = new Server();
            server.Run();
        }
    }
}
