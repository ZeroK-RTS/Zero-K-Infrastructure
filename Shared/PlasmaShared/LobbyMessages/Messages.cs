using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PlasmaShared.LobbyMessages
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

}
