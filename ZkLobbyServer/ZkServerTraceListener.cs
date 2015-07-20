using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class ZkServerTraceListener: TraceListener
    {
        public SharedServerState SharedServerState { get; set; }

        ConcurrentQueue<Say> queue = new ConcurrentQueue<Say>();

        public ZkServerTraceListener(SharedServerState sharedServerState = null)
        {
            this.SharedServerState = sharedServerState;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            ProcessEvent(eventType, message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format,
                                        params object[] args)
        {
            ProcessEvent(eventType, string.Format(format, args));
        }

        public override void Write(string message)
        {
            ProcessEvent(TraceEventType.Verbose, message);
        }

        public override void WriteLine(string message)
        {
            ProcessEvent(TraceEventType.Verbose, message);
        }

        async Task ProcessEvent(TraceEventType type, string text)
        {
            using (var db = new ZkDataContext()) {
                db.LogEntries.Add(new LogEntry() { Time = DateTime.UtcNow, Message = text, TraceEventType = type });
                await db.SaveChangesAsync();
            }

            // write error and critical logs to server
            if (type == TraceEventType.Error || type == TraceEventType.Critical) { 
                var say = new Say() { Place = SayPlace.Channel, Target = "zkerror", Text = text, User = GlobalConst.NightwatchName, Time=DateTime.UtcNow};

                if (SharedServerState != null) {
                    // server runnin, flush queue and add new say
                    Say history;
                    while (queue.TryDequeue(out history)) await SharedServerState.GhostSay(history);
                    await SharedServerState.GhostSay(say);
                } else queue.Enqueue(say); // server not running (stuff intiializing) store in queueu
            }
        }
    }
}