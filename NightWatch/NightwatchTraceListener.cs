using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LobbyClient;
using ZkData;

namespace NightWatch
{
    
    public class NightwatchTraceListener:TraceListener
    {
        TasClient tas;
        ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

        public NightwatchTraceListener(TasClient tas)
        {
            this.tas = tas;
        }

        private void SendError(string text)
        {
            if (tas.IsConnected && tas.IsLoggedIn)
            {
                //tas.JoinChannel("zkerror");
                string history;
                while (queue.TryDequeue(out history)) tas.Say(TasClient.SayPlace.Channel, "zkdev", history, true);
                tas.Say(TasClient.SayPlace.Channel, "zkdev", text, true);
            }
            else queue.Enqueue(text); 

        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            SendError(string.Format("{0} {1} {2}", DateTime.Now, eventType, message));
        }

        public override void TraceEvent(TraceEventCache eventCache,
                                        string source,
                                        TraceEventType eventType,
                                        int id,
                                        string format,
                                        params object[] args)
        {
            SendError(string.Format("{0} {1} {2}", DateTime.Now, eventType, string.Format(format,args)));
        }

        public override void Write(string message)
        {
            SendError(string.Format("{0} DEBUG {1}", DateTime.Now, message));
        }

        public override void WriteLine(string message)
        {
            SendError(string.Format("{0} DEBUG {1}", DateTime.Now, message));
        }

    }
}
