using System;
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
    }

    /// <summary>
    /// Registration request
    /// </summary>
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


    /// <summary>
    /// Attempts to create a channel
    /// </summary>
    public class CreateChannel
    {
        public Channel Channel;
    }


    public class Channel
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
    public class JoinChannel
    {
        public string Name;
        public string Password;
    }


    public class JoinChannelResponse
    {
        public string Name;
        public bool Success;
        public string Reason;

        public Channel Channel;
    }

    public class CreateRoomResponse
    {
        public string RoomID;
        public bool Success;
        public string Reason;

        public Channel Channel;
    }

    public class User
    {
        public int LobbyID;
        public int SpringieLevel = 1;
        public ulong? SteamID;
        //This is only called once
        public DateTime? AwaySince;
        public string Clan;
        public string Avatar;
        public string Country;
        public int Cpu;
        public int EffectiveElo;
        public string Faction;
        public DateTime? InGameSince;
        public bool IsAdmin;
        public bool IsAway;
        public bool IsBot;
        public bool IsInBattleRoom;
        public bool IsInGame;
        public bool BanMute;
        public int Level;

        public bool IsZkLobbyUser { get { return Cpu == GlobalConst.ZkLobbyUserCpu || Cpu == GlobalConst.ZkSpringieManagedCpu || Cpu == GlobalConst.ZkLobbyUserCpuLinux; } }
        public bool IsZkLinuxUser { get { return Cpu == GlobalConst.ZkLobbyUserCpuLinux; } }
        public bool IsSpringieManaged { get { return Cpu == GlobalConst.ZkSpringieManagedCpu; } }
        public bool ISSwlUser { get { return Cpu == 7777 || Cpu == 7778 || Cpu == 7779; } }
        public bool IsFlobby { get { return Cpu == 4607052 || Cpu == 4607063 || Cpu == 4607053; } }

        public string Name;
        
        public string DisplayName { get; protected set; }

        public User Clone()
        {
            return (User)MemberwiseClone();
        }

        public override string ToString()
        {
            return Name;
        }

    }

    public class UserDisconnected
    {
        public string Name;
        public string Reason;
    }



}
