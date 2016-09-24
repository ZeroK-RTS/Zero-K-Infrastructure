using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    /// <summary>
    /// Relays chat from old spring server and back
    /// </summary>
    public class ServerTextCommands
    {
        readonly ZkLobbyServer server;

        public ServerTextCommands(ZkLobbyServer server)
        {
            this.server = server;
            server.Said += OnZkServerSaid;
        }

        void OnZkServerSaid(object sender, Say say)
        {
            try
            {
                if (say.Text?.StartsWith("!") == true)
                {
                    ConnectedUser conus;
                    if (server.ConnectedUsers.TryGetValue(say.User, out conus) && conus.User.IsAdmin)
                    {
                        var parts = say.Text.Split(new[] {' '}, 2);
                        var command = parts.FirstOrDefault();
                        var argument = parts.Skip(1).FirstOrDefault();

                        switch (command)
                        {
                            case "!announce":
                                server.GhostSay(new Say() { Text = argument, User = say.User, Place = SayPlace.MessageBox, Ring = true, });
                            break;

                            case "!topic":
                                if (say.Place == SayPlace.Channel && !string.IsNullOrEmpty(say.Target))
                                {
                                    server.SetTopic(say.Target, argument, say.User);
                                }
                            break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error processing message  {0} {1} : {2}", say?.User, say?.Text, ex);
            }
        }

    }
}