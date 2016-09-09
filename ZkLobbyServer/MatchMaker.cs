using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class MatchMaker
    {
        private ConcurrentDictionary<string, UserEntry> mmUsers = new ConcurrentDictionary<string, UserEntry>();

        public List<MatchMakerSetup.Queue> PossibleQueues = new List<MatchMakerSetup.Queue>();
        private ZkLobbyServer server;


        public MatchMaker(ZkLobbyServer server)
        {
            this.server = server;
            using (var db = new ZkDataContext())
            {
                PossibleQueues.Add(new MatchMakerSetup.Queue()
                {
                    Name = "1v1",
                    Description = "Duels with reasonable skill difference",
                    MaxFriendCount = 0,
                    MinSize = 2,
                    MaxSize = 2,
                    Maps =
                        db.Resources.Where(
                                x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.MapIs1v1 == true) && (x.TypeID == ResourceType.Map))
                            .Select(x => x.InternalName)
                            .ToList()
                });

                PossibleQueues.Add(new MatchMakerSetup.Queue()
                {
                    Name = "Teams",
                    Description = "Small teams 2v2 to 4v4 with reasonable skill difference",
                    MaxFriendCount = 3,
                    MinSize = 4,
                    MaxSize = 8,
                    Maps =
                        db.Resources.Where(
                                x => (x.MapSupportLevel >= MapSupportLevel.MatchMaker) && (x.MapIsTeams == true) && (x.TypeID == ResourceType.Map))
                            .Select(x => x.InternalName)
                            .ToList()
                });
            }
        }


        public async Task OnLoginAccepted(ICommandSender client)
        {
            await client.SendCommand(new MatchMakerSetup() { PossibleQueues = PossibleQueues });
        }

        public async Task StartMatchMaker(ConnectedUser user, StartMatchMaker cmd)
        {
            var wantedQueueNames = cmd.Queues?.ToList() ?? new List<string>();
            var wantedQueues = PossibleQueues.Where(x => wantedQueueNames.Contains(x.Name)).ToList();
            wantedQueueNames = wantedQueues.Select(x => x.Name).ToList();

            if (wantedQueues.Count == 0) // delete
            {
                UserEntry entry;
                mmUsers.TryRemove(user.Name, out entry);
                await user.SendCommand(new MatchMakerStatus()); // left queue

                return;
            }

            if ((cmd.InviteFriends != null) && (cmd.InviteFriends.Count > 0))
            {
                var notOnline = cmd.InviteFriends.Where(y => !server.ConnectedUsers.ContainsKey(y)).ToList();
                if (notOnline.Count > 0)
                {
                    await user.SendCommand(new MatchMakerStartFailed() { Reason = $"Invite failed, {string.Join(", ", notOnline)} not online" });
                    return;
                }

                foreach (var f in cmd.InviteFriends)
                {
                    ConnectedUser fUser;
                    if (server.ConnectedUsers.TryGetValue(f, out fUser))
                    {
                        await
                            fUser.SendCommand(new MatchMakerInvite()
                            {
                                Founder = user.Name,
                                Queues = wantedQueueNames,
                                InvitedFriends = cmd.InviteFriends,
                                SecondsRemaining = 20
                            });

                        return; // todo handle invite wait time
                    }
                }
            }

            var userEntry = mmUsers.AddOrUpdate(user.Name,
                (str) => new UserEntry(user.Name, wantedQueues, cmd.InviteFriends),
                (str, usr) =>
                {
                    usr.UpdateTypes(wantedQueues);
                    return usr;
                });

            await user.SendCommand(ToMatchMakerStatus(userEntry));
        }


        private MatchMakerStatus ToMatchMakerStatus(UserEntry entry)
        {
            return new MatchMakerStatus()
            {
                Text = "In queue",
                JoinedFriends = entry.Friends,
                JoinedQueues = entry.QueueTypes.Select(x => x.Name).ToList()
            };
        }


        public class UserEntry
        {
            public int EloWidth = 50;
            public int Size = 1;
            public List<string> Friends { get; private set; } = new List<string>();
            public string Name { get; private set; }
            public List<MatchMakerSetup.Queue> QueueTypes { get; private set; } = new List<MatchMakerSetup.Queue>();
            public List<int> WantedGameSizes { get; private set; } = new List<int>();


            public UserEntry(string name, List<MatchMakerSetup.Queue> queueTypes, List<string> friends)
            {
                Name = name;
                QueueTypes = queueTypes;
                Friends = friends;
                WantedGameSizes = GetWantedSizes();
            }

            public List<int> GetWantedSizes()
            {
                var ret = new List<int>();
                foreach (var qt in QueueTypes) for (var i = qt.MinSize; i <= qt.MaxSize; i++) ret.Add(i);
                return ret.Distinct().OrderByDescending(x => x).ToList();
            }

            public void UpdateTypes(List<MatchMakerSetup.Queue> queueTypes)
            {
                QueueTypes = queueTypes;
                WantedGameSizes = GetWantedSizes();
            }
        }
    }
}