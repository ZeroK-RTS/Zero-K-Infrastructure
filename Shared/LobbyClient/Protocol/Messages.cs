using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;

namespace LobbyClient
{
    [Flags]
    public enum Origin
    {
        Server = 1,
        Client = 2
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class MessageAttribute : Attribute
    {
        public Origin Direction { get; set; }

        public MessageAttribute(Origin direction)
        {
            Direction = direction;
        }
    }

    /// <summary>
    ///     Initial message sent by server to client on connect
    /// </summary>
    [Message(Origin.Server)]
    public class Welcome
    {
        /// <summary>
        ///     Default suggested engine
        /// </summary>
        public string Engine { get; set; }
        /// <summary>
        ///     Default suggested game version
        /// </summary>
        public string Game { get; set; }
        /// <summary>
        ///     Lobby server version
        /// </summary>
        public string Version { get; set; }
    }


    /// <summary>
    ///     Login request
    /// </summary>
    [Message(Origin.Client)]
    public class Login
    {
        [Flags]
        public enum ClientTypes
        {
            ZeroKLobby = 1,
            Linux = 2,
        }

        public ClientTypes ClientType { get; set; }

        public string LobbyVersion { get; set; }
        /// <summary>
        ///     User name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///     base64(md5(password))
        /// </summary>
        public string PasswordHash { get; set; }

        public long UserID { get; set; }
    }

    /// <summary>
    ///     Registration request
    /// </summary>
    [Message(Origin.Client)]
    public class Register
    {
        /// <summary>
        ///     User name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///     base64(md5(password))
        /// </summary>
        public string PasswordHash { get; set; }
    }

    [Message(Origin.Server)]
    public class RegisterResponse
    {
        public enum Code
        {
            Ok = 0,

            [Description("already connected")]
            AlreadyConnected = 1,

            [Description("name already exists")]
            InvalidName = 2,

            [Description("invalid password")]
            InvalidPassword = 3,

            [Description("banned")]
            Banned = 4,

            [Description("invalid name characters")]
            InvalidCharacters = 5,
        }

        /// <summary>
        ///     Additional text (ban reason)
        /// </summary>
        public string Reason { get; set; }

        public Code ResultCode { get; set; }
    }

    [Message(Origin.Server)]
    public class LoginResponse
    {
        public enum Code
        {
            Ok = 0,

            [Description("invalid name")]
            InvalidName = 2,

            [Description("invalid password")]
            InvalidPassword = 3,

            [Description("banned")]
            Banned = 4
        }

        /// <summary>
        ///     Additional text (ban reason)
        /// </summary>
        public string Reason { get; set; }

        public Code ResultCode { get; set; }
    }


    [Message(Origin.Server)]
    public class ChannelHeader
    {
        public string Password { get; set; }
        public List<string> Users { get; set; } = new List<string>();
        public string ChannelName { get; set; }
        public Topic Topic { get; set; }

        public ChannelHeader()
        {
            Topic = new Topic();
        }
    }

    public class Topic
    {
        public string SetBy { get; set; }
        public DateTime? SetDate { get; set; }
        public string Text { get; set; }
    }

    [Message(Origin.Client | Origin.Server)]
    public class ChangeTopic
    {
        public string ChannelName { get; set; }
        public Topic Topic { get; set; }

        public ChangeTopic()
        {
            Topic = new Topic();
        }
    }


    /// <summary>
    ///     Attempts to join a room
    /// </summary>
    [Message(Origin.Client)]
    public class JoinChannel
    {
        public string ChannelName { get; set; }
        public string Password { get; set; }
    }

    [Message(Origin.Client)]
    public class LeaveChannel
    {
        public string ChannelName { get; set; }
    }


    [Message(Origin.Server)]
    public class ChannelUserAdded
    {
        public string ChannelName { get; set; }
        public string UserName { get; set; }
    }

    [Message(Origin.Server)]
    public class ChannelUserRemoved
    {
        public string ChannelName { get; set; }
        public string UserName { get; set; }
    }

    [Message(Origin.Server)]
    public class JoinChannelResponse
    {
        public ChannelHeader Channel { get; set; }
        public string ChannelName { get; set; }
        public string Reason { get; set; }
        public bool Success { get; set; }
    }


    [Message(Origin.Server | Origin.Client)]
    public class User
    {
        public int AccountID { get; set; }
        public string Avatar { get; set; }
        public DateTime? AwaySince { get; set; }

        public bool BanMute { get; set; }
        public bool BanSpecChat { get; set; }
        public string Clan { get; set; }
        public Login.ClientTypes ClientType { get; set; }
        public string Country { get; set; }
        public string DisplayName { get; set; }
        public int EffectiveMmElo { get; set; }
        public int? CompetitiveRank { get; set; }
        public string Faction { get; set; }
        public DateTime? InGameSince { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsBot { get; set; }
        public bool IsInBattleRoom { get; set; }
        public int Level { get; set; }
        public string LobbyVersion { get; set; }
        public string Name { get; set; }
        public ulong? SteamID { get; set; }
        public bool IsAway => AwaySince != null;
        public bool IsInGame => InGameSince != null;

        [JsonIgnore]
        public string IpAddress;

        public User Clone()
        {
            return (User)MemberwiseClone();
        }

        public override string ToString()
        {
            return Name;
        }

        public void UpdateWith(User u)
        {
            AccountID = u.AccountID;
            SteamID = u.SteamID;
            AwaySince = u.AwaySince;
            Clan = u.Clan;
            Avatar = u.Avatar;
            Country = u.Country;
            EffectiveMmElo = u.EffectiveMmElo;
            Faction = u.Faction;
            InGameSince = u.InGameSince;
            IsAdmin = u.IsAdmin;
            IsBot = u.IsBot;
            BanMute = u.BanMute;
            BanSpecChat = u.BanSpecChat;
            Level = u.Level;
            ClientType = u.ClientType;
            LobbyVersion = u.LobbyVersion;
            DisplayName = u.DisplayName;
            CompetitiveRank = u.CompetitiveRank;
        }
    }

    [Message(Origin.Server)]
    public class UserDisconnected
    {
        public string Name { get; set; }
        public string Reason { get; set; }
    }

    public enum SayPlace
    {
        Channel,
        Battle,
        User,
        BattlePrivate,
        Game,
        MessageBox
    };


    [Message(Origin.Server | Origin.Client)]
    public class Say
    {
        /// <summary>
        ///     Do not relay to old spring
        /// </summary>
        [JsonIgnore]
        public bool AllowRelay = true; // a bit ugly, move to other place, its only needed in Said event in server internals

        public bool IsEmote { get; set; }
        public SayPlace Place { get; set; }
        public bool Ring { get; set; }
        public string Target { get; set; }
        public string Text { get; set; }
        public DateTime? Time { get; set; }
        public string User { get; set; }
    }

    [Message(Origin.Client)]
    public class OpenBattle
    {
        public BattleHeader Header { get; set; }
    }

    public class BattleHeader
    {
        public int? BattleID { get; set; }
        public string Engine { get; set; }
        public string Founder { get; set; }
        public string Game { get; set; }
        public bool? IsRunning { get; set; }
        public string Map { get; set; }
        public int? MaxPlayers { get; set; }
        public AutohostMode? Mode { get; set; }
        public string Password { get; set; }
        public DateTime? RunningSince { get; set; }
        public int? SpectatorCount { get; set; }
        public string Title { get; set; }
        public bool? IsMatchMaker { get; set; }
    }

    [Message(Origin.Server)]
    public class BattleAdded
    {
        public BattleHeader Header { get; set; }
    }

    [Message(Origin.Server | Origin.Client)]
    public class BattleUpdate
    {
        public BattleHeader Header { get; set; }
    }

    [Message(Origin.Server)]
    public class BattleRemoved
    {
        public int BattleID { get; set; }
    }


    [Message(Origin.Server)]
    public class LeftBattle
    {
        public int BattleID { get; set; }
        public string User { get; set; }
    }

    [Message(Origin.Server)]
    public class JoinedBattle
    {
        public int BattleID { get; set; }
        public string User { get; set; }
    }


    [Message(Origin.Client)]
    public class JoinBattle
    {
        public int BattleID { get; set; }
        public string Password { get; set; }
    }

    [Message(Origin.Client)]
    public class LeaveBattle
    {
        public int? BattleID { get; set; }
    }

    [Message(Origin.Client | Origin.Server)]
    public class UpdateUserBattleStatus
    {
        public int? AllyNumber { get; set; }
        public bool? IsSpectator { get; set; }
        public string Name { get; set; }
        public SyncStatuses? Sync { get; set; }
    }


    [Message(Origin.Client | Origin.Server)]
    public class UpdateBotStatus
    {
        public string AiLib { get; set; }
        public int? AllyNumber { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
    }

    [Message(Origin.Client | Origin.Server)]
    public class RemoveBot
    {
        public string Name { get; set; }
    }


    [Message(Origin.Client)]
    public class ChangeUserStatus
    {
        public bool? IsAfk { get; set; }
        public bool? IsInGame { get; set; }
    }

    [Message(Origin.Client | Origin.Server)]
    public class SetModOptions
    {
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }

    [Message(Origin.Client)]
    public class KickFromBattle
    {
        public int? BattleID { get; set; }
        public string Name { get; set; }
        public string Reason { get; set; }
    }

    [Message(Origin.Client)]
    public class KickFromServer
    {
        public string Name { get; set; }
        public string Reason { get; set; }
    }

    [Message(Origin.Client)]
    public class KickFromChannel
    {
        public string ChannelName { get; set; }
        public string Reason { get; set; }
        public string UserName { get; set; }
    }

    [Message(Origin.Client)]
    public class ForceJoinChannel
    {
        public string ChannelName { get; set; }
        public string UserName { get; set; }
    }

    [Message(Origin.Client)]
    public class ForceJoinBattle
    {
        public int BattleID { get; set; }
        public string Name { get; set; }
    }

    [Message(Origin.Client | Origin.Server)]
    public class Ping {}

    [Message(Origin.Server)]
    public class SiteToLobbyCommand
    {
        public string Command { get; set; }
    }

    [Message(Origin.Client)]
    public class LinkSteam
    {
        public string Token { get; set; }
    }


    [Message(Origin.Client | Origin.Server)]
    public class PwMatchCommand
    {
        public enum ModeType
        {
            Clear = 0,
            Attack = 1,
            Defend = 2
        }

        public string AttackerFaction { get; set; }

        public int DeadlineSeconds { get; set; }
        public List<string> DefenderFactions { get; set; }

        public ModeType Mode { get; set; }

        public List<VoteOption> Options { get; set; }

        public PwMatchCommand(ModeType mode)
        {
            Mode = mode;
            Options = new List<VoteOption>();
            DefenderFactions = new List<string>();
        }

        public class VoteOption
        {
            public int Count { get; set; }
            public string Map { get; set; }
            public int Needed { get; set; }
            public int PlanetID { get; set; }
            public string PlanetName { get; set; }
        }
    }


    [Message(Origin.Client)]
    public class SetAccountRelation
    {
        public Relation Relation { get; set; }
        public string TargetName { get; set; }
    }


    [Message(Origin.Server)]
    public class ConnectSpring
    {
        public string Engine { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string Map { get; set; }
        public string Game { get; set; }
        public string ScriptPassword { get; set; }
    }


    [Message(Origin.Client)]
    public class RequestConnectSpring
    {
        public int BattleID { get; set; }
        public string Password { get; set; }
    }

    [Message(Origin.Server)]
    public class FriendList
    {
        public List<string> Friends { get; set; } = new List<string>();
    }

    [Message(Origin.Server)]
    public class IgnoreList
    {
        public List<string> Ignores { get; set; } = new List<string>();
    }

    [Message(Origin.Server)]
    public class BattleDebriefing
    {
        public string Url { get; set; }
        public string Message { get; set; }
        public string ChatChannel { get; set; }
        public int ServerBattleID { get; set; }
        public Dictionary<string, DebriefingUser> DebriefingUsers { get; set; } = new Dictionary<string, DebriefingUser>();

        public class DebriefingUser
        {
            public float? EloChange { get; set; }
            public int? XpChange { get; set; }
            public bool IsInVictoryTeam { get; set; }
            public int? LoseTime { get; set; }
            public int AllyNumber { get; set; }
            public bool IsLevelUp { get; set; }
            public object Awards { get; set; }
        }

        public class DebriefingAward
        {
            public string Key { get; set; }
            public double? Value { get; set; }
            public string Description { get; set; }
        }
    }



    [Message(Origin.Server)]
    public class MatchMakerSetup
    {
        public class Queue
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Maps { get; set; } = new List<string>();
            public string Game { get; set; }
            public int MaxPartySize { get; set; }

            [JsonIgnore]
            public int MaxSize { get; set; }

            [JsonIgnore]
            public int MinSize { get; set; }

            [JsonIgnore]
            public AutohostMode Mode { get; set; }
        }
        public List<Queue> PossibleQueues { get; set; }
    }

    [Message(Origin.Client)]
    public class MatchMakerQueueRequest
    {
        public List<string> Queues { get; set; } = new List<string>();
    }


    [Message(Origin.Server)]
    public class MatchMakerStatus
    {
        public bool MatchMakerEnabled => JoinedQueues?.Count > 0;
        public List<string> JoinedQueues { get; set; } = new List<string>();
        public Dictionary<string,int> QueueCounts { get; set; } = new Dictionary<string, int>();
        public int? CurrentEloWidth { get; set; }
        public DateTime? JoinedTime { get; set; }
        public int? BannedSeconds { get; set; }
        public List<string> InstantStartQueues { get; set; } = new List<string>();

        public Dictionary<string, int> IngameCounts { get; set; } = new Dictionary<string, int>();
    }

    

    [Message(Origin.Server)]
    public class AreYouReady
    {
        public int SecondsRemaining { get; set; } = 10;
    }

    [Message(Origin.Server)]
    public class AreYouReadyUpdate
    {
        public bool ReadyAccepted { get; set; }
        public bool LikelyToPlay { get; set; }
        public Dictionary<string, int> QueueReadyCounts { get; set; } = new Dictionary<string, int>();
        public int? YourBattleSize { get; set; }
        public int? YourBattleReady { get; set; }
    }

    [Message(Origin.Server)]
    public class AreYouReadyResult
    {
        public bool IsBattleStarting { get; set; }
        public bool AreYouBanned { get; set; }
    }


    [Message(Origin.Client)]
    public class AreYouReadyResponse
    {
        public bool Ready { get; set; }
    }

}