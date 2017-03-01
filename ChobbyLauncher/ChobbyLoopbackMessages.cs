using System;
using System.Collections.Generic;
using System.Linq;
using ChobbyLauncher;
using GameAnalyticsSDK.Net;

namespace ChobbyLauncher
{
    public class ChobbyMessageAttribute : Attribute { }

    [ChobbyMessage]
    public class OpenUrl
    {
        public string Url { get; set; }
    }

    [ChobbyMessage]
    public class OpenFolder
    {
        public string Folder { get; set; }
    }

    /// <summary>
    ///     Restarts wrapper
    /// </summary>
    [ChobbyMessage]
    public class Restart { }

    /// <summary>
    ///     Flashes spring window
    /// </summary>
    [ChobbyMessage]
    public class Alert
    {
        public string Message { get; set; }
    }

    /// <summary>
    ///     Sets text to speech volume
    /// </summary>
    [ChobbyMessage]
    public class TtsVolume
    {
        /// <summary>
        ///     Min 0, Max 1
        /// </summary>
        public double Volume { get; set; }
    }

    /// <summary>
    ///     Say a text. Name is used as a hint (hashed) for picking a voice
    /// </summary>
    [ChobbyMessage]
    public class TtsSay
    {
        public string Name { get; set; }
        public string Text { get; set; }
    }

    [ChobbyMessage]
    public class DownloadFile
    {
        public string FileType { get; set; }
        public string Name { get; set; }
    }

    [ChobbyMessage]
    public class DownloadFileDone
    {
        public string FileType { get; set; }
        public bool IsSuccess { get; set; }
        public string Name { get; set; }
    }


    [ChobbyMessage]
    public class SteamOnline
    {
        public string AuthToken { get; set; }
        public List<string> Friends { get; set; }
        public string FriendSteamID { get; set; }
        public string SuggestedName { get; set; }
    }


    [ChobbyMessage]
    public class WrapperOnline
    {
        public string UserID { get; set;}
        public string DefaultServerHost { get; set; }
        public int DefaultServerPort { get; set; }
    }


    [ChobbyMessage]
    public class SteamJoinFriend
    {
        public string FriendSteamID { get; set; }
    }

    [ChobbyMessage]
    public class SteamFriendJoinedMe
    {
        public string FriendSteamID { get; set; }
        public string FriendSteamName { get; set; }
    }



    [ChobbyMessage]
    public class SteamHostGameRequest
    {
        public class SteamHostPlayerEntry
        {
            public string SteamID { get; set; }
            public string Name { get; set; }
            public string ScriptPassword { get; set; }
        }
        
        public List<SteamHostPlayerEntry> Players { get; set; } = new List<SteamHostPlayerEntry>();
        public string Map { get; set; }
        public string Game { get; set; }

        public string Engine { get; set; }
    }

    [ChobbyMessage]
    public class SteamHostGameFailed
    {
        public string CausedBySteamID { get; set; }
        public string Reason { get; set; }
    }

    [ChobbyMessage]
    public class SteamHostGameSuccess
    {
        public int HostPort { get; set; }
    
    }

    [ChobbyMessage]
    public class SteamConnectSpring
    {
        public string HostIP { get; set; }
        public int HostPort { get; set; }
        public int ClientPort { get; set; }

        public string Name { get; set; }
        public string ScriptPassword { get; set; }
        public string Map { get; set; }
        public string Game { get; set; }

        public string Engine { get; set; }
    }



    [ChobbyMessage]
    public class SteamOpenOverlaySection
    {
        public SteamClientHelper.OverlayOption? Option { get; set; } = SteamClientHelper.OverlayOption.LobbyInvite;
    }


    [ChobbyMessage]
    public class SteamOpenOverlayWebsite
    {
        public string Url { get; set; }
    }

    [ChobbyMessage]
    public class SteamInviteFriendToGame
    {
        public string SteamID { get; set; }
    }


    [ChobbyMessage]
    public class SteamOverlayChanged
    {
        public bool IsActive { get; set; }
    }

    [ChobbyMessage]
    public class GaAddErrorEvent
    {
        public string Message { get; set; }

        public EGAErrorSeverity Severity { get; set; }
    }


    [ChobbyMessage]
    public class GaAddDesignEvent
    {
        public string EventID { get; set; }

        public double? Value { get; set; }
    }

    [ChobbyMessage]
    public class GaAddProgressionEvent
    {
        public string Progression1 { get; set; }

        public string Progression2 { get; set; }

        public string Progression3 { get; set; }

        public double? Score { get; set; }
        public EGAProgressionStatus Status { get; set; }
    }


    [ChobbyMessage]
    public class StartNewSpring
    {
        public string StartScriptContent { get; set; }

        public string StartDemoName { get; set; }

        public string SpringSettings { get; set;  }
        public string Engine { get; set; }

        public List<DownloadFile> Downloads { get; set; }
    }


    [ChobbyMessage]
    public class GaAddBusinessEvent
    {
        public int Amount { get; set; }
        public string CartType { get; set; }
        public string Currency { get; set; }
        public string ItemId { get; set; }
        public string ItemType { get; set; }
    }

    [ChobbyMessage]
    public class GaAddResourceEvent
    {
        public float Amount { get; set; }
        public string Currency { get; set; }
        public EGAResourceFlowType FlowType { get; set; }
        public string ItemId { get; set; }
        public string ItemType { get; set; }
    }


    [ChobbyMessage]
    public class GaConfigureResourceCurrencies
    {
        public string[] List { get; set; }
    }

    [ChobbyMessage]
    public class GaConfigureResourceItemTypes
    {
        public string[] List { get; set; }
    }


    [ChobbyMessage]
    public class GaConfigureCustomDimensions
    {
        public int Level { get; set; }

        public string[] List { get; set; }
    }


    [ChobbyMessage]
    public class GaSetCustomDimension
    {
        public int Level { get; set; }

        public string Value { get; set; }
    }
}