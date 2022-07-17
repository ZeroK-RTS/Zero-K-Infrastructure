using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        ///     Number of current users
        /// </summary>
        public int UserCount { get; set; }
        /// <summary>
        ///     Lobby server version
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// RSA challenge token, sign it with priv key of client to prove identity
        /// </summary>
        public string ChallengeToken { get; set; }
        
        /// <summary>
        /// Server pub key, use to encrypt sensitive data
        /// </summary>
        public string ServerPubKey { get; set; }
        
        
        public List<string> Blacklist { get; set; } = new List<string>();
        public List<FactionInfo> Factions { get; set; }

        public bool UserCountLimited { get; set; }

        public class FactionInfo
        {
            public string Name { get; set; }
            public string Shortcut { get; set; }
            public string Color { get; set; }
        }
    }

    [Message(Origin.Server)]
    public class NewsList
    {
        public List<NewsItem> NewsItems { get; set; } = new List<NewsItem>();
    }

    [Message(Origin.Server)]
    public class LadderList
    {
        public List<LadderItem> LadderItems { get; set; } = new List<LadderItem>();
    }


    [Message(Origin.Server)]
    public class ForumList
    {
        public List<ForumItem> ForumItems { get; set; } = new List<ForumItem>();
    }




    [Message(Origin.Server)]
    public class DefaultEngineChanged
    {
        /// <summary>
        ///     Default suggested engine
        /// </summary>
        public string Engine { get; set; }
    }


    [Message(Origin.Server)]
    public class DefaultGameChanged
    {
        /// <summary>
        ///     Default suggested game version
        /// </summary>
        public string Game { get; set; }
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

        public string SteamAuthToken { get; set; }

        public long UserID { get; set; }

        public string InstallID { get; set; }
        
        
        public string ClientPubKey { get; set; }
        public string SignedChallengeToken { get; set; }
        
        public string EncryptedPasswordHash { get; set; }

        public List<ulong> Dlc { get; set; }
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
        
        public string EncryptedPasswordHash { get; set; }
        

        public string SteamAuthToken { get; set; }

        public string Email { get; set; }

        public long UserID { get; set; }
        public string InstallID { get; set; }
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
            NameAlreadyTaken = 2,

            [Description("invalid password")]
            InvalidPassword = 3,

            [Description("banned")]
            Banned = 4,

            [Description("invalid name characters")]
            NameHasInvalidCharacters = 5,

            [Description("invalid steam token, are you in offline mode?")]
            InvalidSteamToken = 6,

            [Description("steam already registered")]
            SteamAlreadyRegistered = 7,

            [Description("missing both password and token")]
            MissingBothPasswordAndToken = 8,

            [Description("banned too many connection attempts")]
            BannedTooManyAttempts = 9,

            [Description("already linked steam, connecting")]
            AlreadyRegisteredWithThisSteamToken = 10,

            [Description("already registered, use login using password")]
            AlreadyRegisteredWithThisPassword = 11
        }


        public Code ResultCode { get; set; }

        public string BanReason { get; set; }

        public RegisterResponse(Code resultCode)
        {
            ResultCode = resultCode;
        }

        public RegisterResponse() { }
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
            Banned = 4,


            [Description("invalid steam token, are you in offline mode?")]
            InvalidSteamToken = 5,

            [Description("banned, too many connection attempts")]
            BannedTooManyConnectionAttempts = 6,

            [Description("your steam account is not linked yet, send ZK login or register")]
            SteamNotLinkedAndLoginMissing = 7,

            [Description("your steam account is already linked to a different account")]
            SteamLinkedToDifferentAccount = 8,

            [Description("sorry, the server is full, please retry later")]
            ServerFull = 9,
            
            [Description("invalid RSA signature")]
            InvalidRsaSignature = 10,
            
            [Description("RSA signature could not be verified (pub key not known)")]
            RsaSignatureCouldNotBeVerified = 11            
            
        }

        public string Name { get; set; }

        public string BanReason { get; set; }

        public Code ResultCode { get; set; }

        /// <summary>
        ///     Use this to login to website
        /// </summary>
        public string SessionToken { get; set; }
    }


    [Message(Origin.Server)]
    public class ChannelHeader
    {
        public string ChannelName { get; set; }
        public bool IsDeluge { get; set; }
        public string Password { get; set; }
        public Topic Topic { get; set; }
        public List<string> Users { get; set; } = new List<string>();

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
        public bool BanVotes { get; set; }
        public bool BanSpecChat { get; set; }
        public int? BattleID { get; set; }
        public string Clan { get; set; }
        public string Country { get; set; }
        public string DisplayName { get; set; }
        public string Faction { get; set; }
        public DateTime? InGameSince { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsBot { get; set; }

        public string LobbyVersion { get; set; }
        public string Name { get; set; }
        public string SteamID { get; set; }
        public List<string> Badges { get; set; }

        public string Icon { get; set; }

        public bool IsAway => AwaySince != null;
        public bool IsInBattleRoom => BattleID != null;
        public bool IsInGame => InGameSince != null;

        [JsonIgnore]
        public string IpAddress;

        [JsonIgnore]
        public int? PartyID;

        [JsonIgnore]
        public int SyncVersion; //sync version for updating user statuses

        [JsonIgnore]
        public int RawMmElo { get; set; }

        public int EffectiveMmElo { get; set; }
        public int EffectiveElo { get; set; }
        public int Level { get; set; }
        public int Rank { get; set; }

        public User Clone()
        {
            return (User)MemberwiseClone();
        }

        public bool CanUserPlanetWars()
        {
            return !string.IsNullOrEmpty(Faction) && Level >= GlobalConst.MinPlanetWarsLevel && EffectiveMmElo > GlobalConst.MinPlanetWarsElo;
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
            EffectiveElo = u.EffectiveElo;
            RawMmElo = u.RawMmElo;
            Faction = u.Faction;
            InGameSince = u.InGameSince;
            IsAdmin = u.IsAdmin;
            IsBot = u.IsBot;
            BanMute = u.BanMute;
            BanVotes = u.BanVotes;
            BanSpecChat = u.BanSpecChat;
            Level = u.Level;
            Rank = u.Rank;
            LobbyVersion = u.LobbyVersion;
            DisplayName = u.DisplayName;
            BattleID = u.BattleID;
            Badges = u.Badges;
            Icon = u.Icon;
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

        public SaySource? Source { get; set; }
    }

    public enum SaySource
    {
        Discord,
        Spring,
        Zk,
        DiscordSpring
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
        public bool? IsMatchMaker { get; set; }
        public bool? IsRunning { get; set; }
        public string Map { get; set; }
        public int? MaxPlayers { get; set; }
        public AutohostMode? Mode { get; set; }
        public string Password { get; set; }
        public int? PlayerCount { get; set; }
        public DateTime? RunningSince { get; set; }
        public int? SpectatorCount { get; set; }
        public string Title { get; set; }
        public bool? TimeQueueEnabled { get; set; }
        public int? MaxEvenPlayers { get; set; }
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
    public class JoinBattleSuccess
    {
        public int BattleID { get; set; }
        public List<UpdateBotStatus> Bots { get; set; }
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
        public List<UpdateUserBattleStatus> Players { get; set; }
        public Dictionary<string, string> MapOptions { get; set; } = new Dictionary<string, string>();
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
        public int? QueueOrder { get; set; }
        public string Name { get; set; }
        public SyncStatuses? Sync { get; set; }
        
        public DateTime? JoinTime { get; set; }
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

    [Message(Origin.Client | Origin.Server)]
    public class SetMapOptions
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


    [Message(Origin.Server)]
    public class SiteToLobbyCommand
    {
        public string Command { get; set; }
    }


    [Message(Origin.Server)]
    public class PwMatchCommand
    {
        public enum ModeType
        {
            Clear = 0,
            Attack = 1,
            Defend = 2
        }

        public string AttackerFaction { get; set; }

        public DateTime Deadline { get; set; }

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
            public string PlanetImage { get; set; }
            public List<string> StructureImages { get; set; }
            public int IconSize { get; set; }
            public string PlanetName { get; set; }
        }
    }

    [Message(Origin.Client)]
    public class PwJoinPlanet
    {
        public int PlanetID { get; set; }
    }

    [Message(Origin.Server)]
    public class PwRequestJoinPlanet
    {
        public int PlanetID { get; set; }
    }


    [Message(Origin.Server)]
    public class PwJoinPlanetSuccess
    {
        public int PlanetID { get; set; }
    }

    [Message(Origin.Server)]
    public class PwAttackingPlanet
    {
        public int PlanetID { get; set; }
    }

    [Message(Origin.Client)]
    public class JoinFactionRequest
    {
        public string Faction { get; set; }
    }


    [Message(Origin.Server)]
    public class PwStatus
    {
        public PlanetWarsModes PlanetWarsMode { get; set; }
        public PlanetWarsModes? PlanetWarsNextMode { get; set; }
        public DateTime? PlanetWarsNextModeTime { get; set; }
        public int MinLevel { get; set; }
    }




    [Message(Origin.Client)]
    public class SetAccountRelation
    {
        public Relation Relation { get; set; }

        public string SteamID { get; set; }
        public string TargetName { get; set; }
    }


    [Message(Origin.Server)]
    public class ConnectSpring
    {
        public string Engine { get; set; }
        public string Game { get; set; }
        public string Ip { get; set; }
        public string Map { get; set; }
        public int Port { get; set; }
        public string ScriptPassword { get; set; }
        public AutohostMode Mode { get; set; }
        public string Title { get; set; }
        public bool IsSpectator { get; set; }
    }

    [Message(Origin.Server)]
    public class RejoinOption
    {
        public int BattleID { get; set; }
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
        public List<FriendEntry> Friends { get; set; } = new List<FriendEntry>();
    }

    public class FriendEntry
    {
        public string Name { get; set; }
        public string SteamID { get; set; }
    }

    [Message(Origin.Server)]
    public class IgnoreList
    {
        public List<string> Ignores { get; set; } = new List<string>();
    }

    [Message(Origin.Server)]
    public class BattleDebriefing
    {
        public Dictionary<string, DebriefingUser> DebriefingUsers { get; set; } = new Dictionary<string, DebriefingUser>();
        public string ChatChannel { get; set; }
        public string Message { get; set; }
        public int ServerBattleID { get; set; }
        public string Url { get; set; }
        public string RatingCategory { get; set; } = "Unrated";

        public class DebriefingAward
        {
            public string Description { get; set; }
            public string Key { get; set; }
            public double? Value { get; set; }
        }

        public class DebriefingUser
        {
            public int AllyNumber { get; set; }
            public object Awards { get; set; }
            public float EloChange { get; set; }
            public bool IsInVictoryTeam { get; set; }
            public bool IsLevelUp { get; set; }
            public bool IsRankup { get; set; }
            public bool IsRankdown { get; set; }
            public float NewElo { get; set; }
            public float NextRankElo { get; set; }
            public float PrevRankElo { get; set; }
            public int NewRank { get; set; }
            public int LoseTime { get; set; }
            public int XpChange { get; set; }
            public int NewXp { get; set; }
            public int NextLevelXp { get; set; }
            public int PrevLevelXp { get; set; }
            public int AccountID { get; set; }
        }
    }

    [Message(Origin.Server)]
    public class UserProfile
    {
        public class UserAward
        {
            public string AwardKey { get; set; }
            public int Collected { get; set; }
        }

        public string Name { get; set; }
        public List<UserAward> Awards { get; set; } = new List<UserAward>();
        public List<string> Badges { get; set; } = new List<string>();

        public int Level { get; set; }
        public string LevelUpRatio { get; set; }

        public int Rank { get; set; }
        public string RankUpRatio { get; set; }

        public int EffectiveElo { get; set; }
        public int EffectiveMmElo { get; set; }
        public int EffectivePwElo { get; set; }

        public int Kudos { get; set; }

        public string PwMetal { get; set; }
        public string PwDropships { get; set; }
        public string PwBombers { get; set; }
        public string PwWarpcores { get; set; }
    }


    [Message(Origin.Server)]
    public class BattlePoll
    {
        public class PollOption
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public int Id { get; set; }
            public int Votes { get; set; }
            public string Url { get; set; } //Empty if not applicable
        }
        
        public string Topic { get; set; } //Null if there is no poll
        public string Url { get; set; } //Url of the Yes option if Yes/No Vote
        public List<PollOption> Options { get; set; } //Null if there is no poll
        public int VotesToWin { get; set; } //If any single option receives this many votes, it will win instantly. -1 if there is no poll
        public bool YesNoVote { get; set; } //Is this a vote with two options, yes and no
        public bool MapSelection { get; set; } //Is this a vote with map options?
        public bool NotifyPoll { get; set; } //true if users should be notified for this poll (e.g. !start)
        public string MapName { get; set; } //MapName if yesno map poll, otherwise null
    }

    [Message(Origin.Server)]
    public class BattlePollOutcome
    {
        public BattlePoll.PollOption WinningOption; //null if no winning option
        public string Topic; //topic of the previous poll
        public string Message; //message to display for the outcome
        public bool Success; //whether there was a winning option and if it was yes in case of yes/no vote
        public bool YesNoVote { get; set; } //Was this a vote with two options, yes and no
        public bool MapSelection { get; set; } //Was this a vote with map options?
    }

    [Message(Origin.Client)]
    public class UserReport
    {
        public string Username;
        public string Text;
    }
    
    
    [Message(Origin.Client)]
    public class GetCustomGameMode
    {
        public string ShortName;
    }

    [Message(Origin.Server)]
    public class CustomGameModeResponse
    {
        public string ShortName;
        public string DisplayName;
        public string GameModeJson;
    }
    
}