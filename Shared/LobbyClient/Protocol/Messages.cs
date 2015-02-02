using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using ZkData;

namespace LobbyClient
{
    /// <summary>
    /// Initial message sent by server to client on connect
    /// </summary>
    [Message(Origin.Server)]
    public class Welcome
    {
        /// <summary>
        /// Default suggested engine
        /// </summary>
        public string Engine;
        /// <summary>
        /// Default suggested game version
        /// </summary>
        public string Game;
        /// <summary>
        /// Lobby server version
        /// </summary>
        public string Version;
    }


    /// <summary>
    /// Login request
    /// </summary>
    [Message(Origin.Client)]
    public class Login
    {
        /// <summary>
        /// User name
        /// </summary>
        public string Name;
        /// <summary>
        /// base64(md5(password))
        /// </summary>
        public string PasswordHash;

        public string UserID;

        [Flags]
        public enum ClientTypes
        {
            ZeroKLobby = 1,
            Linux = 2,
            SpringieManaged = 4,
            Springie = 8,

        }

        public ClientTypes ClientType;


    }

    /// <summary>
    /// Registration request
    /// </summary>
    [Message(Origin.Client)]
    public class Register
    {
        /// <summary>
        /// User name
        /// </summary>
        public string Name;
        /// <summary>
        /// base64(md5(password))
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
            Banned = 4
        }

        public Code ResultCode;

        /// <summary>
        /// Additional text (ban reason)
        /// </summary>
        public string Reason;
    }

    [Message(Origin.Server)]
    public class LoginResponse
    {
        public enum Code
        {
            Ok = 0,

            [Description("already connected")]
            AlreadyConnected = 1,

            [Description("invalid name")]
            InvalidName = 2,

            [Description("invalid password")]
            InvalidPassword = 3,

            [Description("banned")]
            Banned = 4
        }

        public Code ResultCode;

        /// <summary>
        /// Additional text (ban reason)
        /// </summary>
        public string Reason;
    }



    public class ChannelHeader
    {
        public List<string> Users = new List<string>();
        public string Name { get; set; }
        public string Topic { get; set; }
        public string TopicSetBy { get; set; }
        public DateTime? TopicSetDate { get; set; }
        public string Password;
    }


    /// <summary>
    /// Attempts to join a room
    /// </summary>
    [Message(Origin.Client)]
    public class JoinChannel
    {
        public string Name;
        public string Password;
    }

    [Message(Origin.Client)]
    public class LeaveChannel
    {
        public string Name;
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
        public string Name;
        public bool Success;
        public string Reason;

        public ChannelHeader Channel;
    }


    [Message(Origin.Server | Origin.Client)]
    public class User
    {
        public int AccountID;
        public int SpringieLevel;
        public ulong? SteamID;
        public DateTime? AwaySince;
        public string Clan;
        public string Avatar;
        public string Country;
        public int EffectiveElo;
        public int Effective1v1Elo;
        public string Faction;
        public DateTime? InGameSince;
        public bool IsAdmin;
        public bool IsAway;
        public bool IsBot;
        public bool IsInBattleRoom;
        public bool IsInGame;
        public bool BanMute;
        public int Level;
        public Login.ClientTypes ClientType;
        public string Name;
        public string DisplayName;

        public User Clone()
        {
            return (User)MemberwiseClone();
        }

        public override string ToString()
        {
            return Name;
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
        public SayPlace Place;
        public string Target;
        public string User;
        public bool IsEmote;
        public string Text;
        public bool Ring;
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
        public string Game;
        public string Map;
        public int? MaxPlayers;
        public int? SpectatorCount;
        public string Password;
        public string Title;
        public int? Port;
        public string Ip;
        public string Founder;
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
        public string Password;
        public int BattleID;
    }

    [Message(Origin.Client)]
    public class LeaveBattle
    {
        public int BattleID;
    }

    [Message(Origin.Client | Origin.Server)]
    public class UpdateUserBattleStatus
    {
        public int? AllyNumber;
        public bool? IsSpectator;
        public string Name;
        public SyncStatuses? Sync;
        public int? TeamNumber;
    }


    [Message(Origin.Client | Origin.Server)]
    public class UpdateBotStatus
    {
        public int? AllyNumber;
        public string Name;
        public int? TeamNumber;
        public string AiLib;
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
        public bool? IsInGame;
        public bool? IsAfk;
    }

    [Message(Origin.Client | Origin.Server)]
    public class SetRectangle
    {
        public int Number;
        public BattleRect Rectangle;
    }

    [Message(Origin.Client | Origin.Server)]
    public class SetModOptions
    {
        public Dictionary<string,string> Options = new Dictionary<string, string>();
    }

    [Message(Origin.Client)]
    public class KickFromBattle
    {
        public string Name;
        public int BattleID;
    }

    [Message(Origin.Client)]
    public class KickFromServer
    {
        public string Name;
    }

    [Message(Origin.Client)]
    public class KickFromChannel
    {
        public string Name;
        public string Channel;
    }

    [Message(Origin.Client)]
    public class ForceJoinChannel
    {
        public string Name;
        public string Channel;
    }

    [Message(Origin.Client)]
    public class ForceJoinBattle
    {
        public string Name;
        public int BattleID;
    }

}
