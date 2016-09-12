using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;

namespace LobbyClient
{
    /// <summary>
    ///     Initial message sent by server to client on connect
    /// </summary>
    [Message(Origin.Server)]
    public class Welcome
    {
        /// <summary>
        ///     Default suggested engine
        /// </summary>
        public string Engine;
        /// <summary>
        ///     Default suggested game version
        /// </summary>
        public string Game;
        /// <summary>
        ///     Lobby server version
        /// </summary>
        public string Version;
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

        public ClientTypes ClientType;

        public string LobbyVersion;
        /// <summary>
        ///     User name
        /// </summary>
        public string Name;
        /// <summary>
        ///     base64(md5(password))
        /// </summary>
        public string PasswordHash;

        public long UserID;
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
        public string Name;
        /// <summary>
        ///     base64(md5(password))
        /// </summary>
        public string PasswordHash;
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
        public string Reason;

        public Code ResultCode;
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
        public string Reason;

        public Code ResultCode;
    }


    [Message(Origin.Server)]
    public class ChannelHeader
    {
        public string Password;
        public List<string> Users = new List<string>();
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
        public string ChannelName;
        public string Password;
    }

    [Message(Origin.Client)]
    public class LeaveChannel
    {
        public string ChannelName;
    }


    [Message(Origin.Server)]
    public class ChannelUserAdded
    {
        public string ChannelName;
        public string UserName;
    }

    [Message(Origin.Server)]
    public class ChannelUserRemoved
    {
        public string ChannelName;
        public string UserName;
    }

    [Message(Origin.Server)]
    public class JoinChannelResponse
    {
        public ChannelHeader Channel;
        public string ChannelName;
        public string Reason;
        public bool Success;
    }


    [Message(Origin.Server | Origin.Client)]
    public class User
    {
        public int AccountID;
        public string Avatar;
        public DateTime? AwaySince;

        public bool BanMute;
        public bool BanSpecChat;
        public string Clan;
        public Login.ClientTypes ClientType;
        public string Country;
        public string DisplayName;
        public int Effective1v1Elo;
        public int EffectiveElo;
        public string Faction;
        public DateTime? InGameSince;
        public bool IsAdmin;
        public bool IsBot;
        public bool IsInBattleRoom;
        public int Level;
        public string LobbyVersion;
        public string Name;
        public ulong? SteamID;
        public bool IsAway => AwaySince != null;
        public bool IsInGame => InGameSince != null;

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
            EffectiveElo = u.EffectiveElo;
            Effective1v1Elo = u.Effective1v1Elo;
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
        }
    }

    [Message(Origin.Server)]
    public class UserDisconnected
    {
        public string Name;
        public string Reason;
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
        public bool IsEmote;
        public SayPlace Place;
        public bool Ring;
        public string Target;
        public string Text;
        public DateTime? Time;
        public string User;
    }

    [Message(Origin.Client)]
    public class OpenBattle
    {
        public BattleHeader Header;
    }

    public class BattleHeader
    {
        public int? BattleID;
        public string Engine;
        public string Founder;
        public string Game;
        public bool? IsRunning;
        public string Map;
        public int? MaxPlayers;
        public AutohostMode? Mode;
        public string Password;
        public DateTime? RunningSince;
        public int? SpectatorCount;
        public string Title;
    }

    [Message(Origin.Server)]
    public class BattleAdded
    {
        public BattleHeader Header;
    }

    [Message(Origin.Server | Origin.Client)]
    public class BattleUpdate
    {
        public BattleHeader Header;
    }

    [Message(Origin.Server)]
    public class BattleRemoved
    {
        public int BattleID;
    }


    [Message(Origin.Server)]
    public class LeftBattle
    {
        public int BattleID;
        public string User;
    }

    [Message(Origin.Server)]
    public class JoinedBattle
    {
        public int BattleID;
        public string User;
    }


    [Message(Origin.Client)]
    public class JoinBattle
    {
        public int BattleID;
        public string Password;
    }

    [Message(Origin.Client)]
    public class LeaveBattle
    {
        public int? BattleID;
    }

    [Message(Origin.Client | Origin.Server)]
    public class UpdateUserBattleStatus
    {
        public int? AllyNumber;
        public bool? IsSpectator;
        public string Name;
        public SyncStatuses? Sync;
    }


    [Message(Origin.Client | Origin.Server)]
    public class UpdateBotStatus
    {
        public string AiLib;
        public int? AllyNumber;
        public string Name;
        public string Owner;
    }

    [Message(Origin.Client | Origin.Server)]
    public class RemoveBot
    {
        public string Name;
    }


    [Message(Origin.Client)]
    public class ChangeUserStatus
    {
        public bool? IsAfk;
        public bool? IsInGame;
    }

    [Message(Origin.Client | Origin.Server)]
    public class SetModOptions
    {
        public Dictionary<string, string> Options = new Dictionary<string, string>();
    }

    [Message(Origin.Client)]
    public class KickFromBattle
    {
        public int? BattleID;
        public string Name;
        public string Reason;
    }

    [Message(Origin.Client)]
    public class KickFromServer
    {
        public string Name;
        public string Reason;
    }

    [Message(Origin.Client)]
    public class KickFromChannel
    {
        public string ChannelName;
        public string Reason;
        public string UserName;
    }

    [Message(Origin.Client)]
    public class ForceJoinChannel
    {
        public string ChannelName;
        public string UserName;
    }

    [Message(Origin.Client)]
    public class ForceJoinBattle
    {
        public int BattleID;
        public string Name;
    }

    [Message(Origin.Client | Origin.Server)]
    public class Ping {}

    [Message(Origin.Server)]
    public class SiteToLobbyCommand
    {
        public string Command;
    }

    [Message(Origin.Client)]
    public class LinkSteam
    {
        public string Token;
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
    public class MatchMakerSetup
    {
        public class Queue
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<string> Maps { get; set; } = new List<string>();
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
        public int CurrentEloWidth { get; set; }
        public DateTime JoinedTime { get; set; }
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