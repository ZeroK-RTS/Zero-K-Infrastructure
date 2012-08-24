using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Windows;
using System.Xml.Serialization;
using JetBrains.Annotations;
using PlasmaDownloader;
using PlasmaShared;
using ZkData;
using FontStyle = System.Drawing.FontStyle;

namespace ZeroKLobby
{
    public class Config: ICloneable, IPlasmaDownloaderConfig
    {
        public const string BaseUrl = "http://zero-k.info/";
        public const string ConfigFileName = "ZeroKLobbyConfig.xml";
        public const string IpcFileName = "zero-k_args.txt";
        public const string LogFile = "ZeroKLobbyErrors.txt";
        public const string ReportUrl = "http://cadownloader.licho.eu/error.php";


        StringCollection autoJoinChannels = new StringCollection() { KnownGames.GetDefaultGame().Channel };
        bool connectOnStartup = true;
        Color fadeColor = Color.Gray;
        StringCollection friends = new StringCollection(); // lacks events for adding friends immediatly
        int idleTime = 5;
        StringCollection ignoredUsers = new StringCollection();
        string lobbyPlayerName;
        string lobbyPlayerPassword;
        string manualSpringPath = @"C:\Program Files\Spring";
        bool showEmptyBattles = true;
        bool showHourlyChimes = true;
        bool showNonJoinableBattles = true;
        bool showOfficialBattles = true;


        string springServerHost = "springrts.com";
        int springServerPort = 8200;


        [Category("Chat")]
        [DisplayName("Automatically Joined Channels")]
        [Description("Zero-K lobby will automatically join these channels when connecting.")]
        [Browsable(true)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            , typeof(UITypeEditor))]
        public StringCollection AutoJoinChannels { get { return autoJoinChannels; } set { autoJoinChannels = value; } }


       
        [Browsable(false)]
        public string BattleFilter { get; set; }

        [DisplayName("Disable driver check")]
        [Description("Don't warn about incompatible video card drivers")]
        public bool DisableDriverCheck { get; set; }

        [Category("Chat")]
        [DisplayName("Color: Background")]
        [XmlIgnore]
        public Color BgColor
        {
            get { return Color.FromArgb(BgColorInt); }
            set
            {
                BgColorInt = value.ToArgb();
                UpdateFadeColor();
            }
        }
        [Browsable(false)]
        public int BgColorInt = Color.White.ToArgb();
        [Browsable(false)]
        public bool BlockNonFriendPm;


        [Category("Chat")]
        [Description("Chat Font")]
        [DisplayName("Chat Font")]
        [XmlIgnore]
        public Font ChatFont { get { return ChatFontXML.ToFont(); } set { ChatFontXML = new XmlFont(value); } }
        [Browsable(false)]
        public XmlFont ChatFontXML = new XmlFont();
        [Category("Connection")]
        [DisplayName("Connect on startup")]
        [Description("Connect and login player on program start?")]
        public bool ConnectOnStartup { get { return connectOnStartup; } set { connectOnStartup = value; } }
        [Category("General")]
        [DisplayName("Data folder to store all game content")]
        [Description("This path will be used for downloaded maps, games, logs, settings etc.")]
        public string DataFolder { get; set; }

        [Browsable(false)] // hidden because it crashes tasc connection
        public bool EnableServerConsole { get; set; }


        [Browsable(false)]
        public int DefaultPlayerColorInt = 16776960; // default teal color
        [Category("Chat")]
        [DisplayName("Disable Bubble On Channel Highlight")]
        [Description("Disable the system tray bubble when someone says your name in a public channel.")]
        public bool DisableChannelBubble { get; set; }
        [Category("Chat")]
        [DisplayName("Disable Bubble On Private Message")]
        public bool DisablePmBubble { get; set; }


        [Category("Chat")]
        [DisplayName("Color: Emote")]
        [XmlIgnore]
        public Color EmoteColor { get { return Color.FromArgb(EmoteColorInt); } set { EmoteColorInt = value.ToArgb(); } }
        [Browsable(false)]
        public int EmoteColorInt = Color.FromArgb(178, 0, 178).ToArgb();

        [XmlIgnore]
        [Browsable(false)]
        public Color FadeColor { get { return fadeColor; } set { fadeColor = value; } }
        [Category("Chat")]
        [DisplayName("Friend List")]
        [Description("List of friends.")]
        [Browsable(true)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            , typeof(UITypeEditor))]
        public StringCollection Friends { get { return friends; } set { friends = value; } }
        [Browsable(false)]
        public bool HasHosted { get; set; }
        [Browsable(false)]
        public int HostBattle_MaxPlayers { get; set; }
        [Browsable(false)]
        public int HostBattle_MinPlayers { get; set; }
        [Browsable(false)]
        public string HostBattle_RapidTag { get; set; }
        [Browsable(false)]
        public string HostBattle_SpringieCommands { get; set; }
        [Browsable(false)]
        public int HostBattle_Teams { get; set; }
        [Browsable(false)]
        public string HostBattle_Title { get; set; }
        [Browsable(false)]
        public bool HostBattle_UseManage { get; set; }
        [Category("Quickmatching")]
        [DisplayName("Idle User Time")]
        [Description("Idle minutes after which Zero-K lobby assumes the user is gone and quickmatching is stopped.")]
        public int IdleTime { get { return idleTime; } set { idleTime = value; } }

        [Category("Quickmatching")]
        [DisplayName("Auto activate")]
        [Description("Auto activate/deactivate quickmatch when joining starred hosts")]
        public bool AutoActivateQuickmatch { get { return autoActivateQuickmatch; } set { autoActivateQuickmatch = value; } }
        [Category("Chat")]
        [DisplayName("Ignored Users")]
        [Description("The messages of these users are ignored.")]
        [Browsable(true)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
            , typeof(UITypeEditor))]
        public StringCollection IgnoredUsers { get { return ignoredUsers; } set { ignoredUsers = value; } }
        public bool IsFirstRun = true;

        [Browsable(false)]
        public bool AtiMinimapRenderingChecked { get; set; }

        [Category("Chat")]
        [DisplayName("Color: Joins")]
        [XmlIgnore]
        public Color JoinColor { get { return Color.FromArgb(JoinColorInt); } set { JoinColorInt = value.ToArgb(); } }
        [Browsable(false)]
        public int JoinColorInt = Color.FromArgb(42, 140, 42).ToArgb();
        
        [Browsable(false)]
        public WindowState LastWindowState { get; set; }

        [Category("Chat")]
        [DisplayName("Color: Leaves")]
        [XmlIgnore]
        public Color LeaveColor { get { return Color.FromArgb(LeaveColorInt); } set { LeaveColorInt = value.ToArgb(); } }
        [Browsable(false)]
        public int LeaveColorInt = Color.FromArgb(102, 54, 31).ToArgb();
        [Category("Chat")]
        [DisplayName("Disable Context Menu on Leftclick")]
        [Description("Only right clicking shows context menu. Left clicking on a playername will select them in the player list.")]
        public bool LeftClickSelectsPlayer { get; set; }


        [Category("Chat")]
        [DisplayName("Color: Links")]
        [XmlIgnore]
        public Color LinkColor { get { return Color.FromArgb(LinkColorInt); } set { LinkColorInt = value.ToArgb(); } }
        [Browsable(false)]
        public int LinkColorInt = Color.Blue.ToArgb();


        [Category("Account")]
        [DisplayName("Lobby Player Name")]
        [Description("Player name from lobby (tasclient), needed for many features")]
        public string LobbyPlayerName
        {
            get { return lobbyPlayerName; }
            set
            {
                lobbyPlayerName = value;
                try
                {
                    WindowsApi.InternetSetCookie(BaseUrl, GlobalConst.LoginCookieName, value);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Cannot set user cookie: {0}", ex);
                }
            }
        }


        [Category("Account")]
        [DisplayName("Lobby Password")]
        [Description("Player password from lobby (tasclient), needed for widget online profile")]
        public string LobbyPlayerPassword
        {
            get { return lobbyPlayerPassword; }
            set
            {
                lobbyPlayerPassword = value;
                try
                {
                    WindowsApi.InternetSetCookie(BaseUrl, GlobalConst.PasswordHashCookieName, PlasmaShared.Utils.HashLobbyPassword(value));
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Cannot set user cookie: {0}", ex);
                }
            }
        }


        [Browsable(false)]
        public string ManualSpringPath { get { return manualSpringPath; } set { manualSpringPath = value; } }

        [Category("General")]
        [DisplayName("Minimize to tray")]
        [Description("Minimize to system tray instead of taskbar")]
        public bool MinimizeToTray { get; set; }
        [Category("Chat")]
        [DisplayName("Color: Notice")]
        [XmlIgnore]
        public Color NoticeColor { get { return Color.FromArgb(NoticeColorInt); } set { NoticeColorInt = value.ToArgb(); } }
        [Browsable(false)]
        public int NoticeColorInt = Color.Red.ToArgb();


        [Browsable(false)]
        public bool ShowEmptyBattles { get { return showEmptyBattles; } set { showEmptyBattles = value; } }
        [Category("Chat")]
        [DisplayName("Show Friends Window")]
        [Description("Shows an extra windows for your friends")]
        public bool ShowFriendsWindow { get; set; }


        [Category("Chat")]
        [DisplayName("Show Hourly Chat Message")]
        [Description("Show a notification in chat channels every hour.")]
        public bool ShowHourlyChimes { get { return showHourlyChimes; } set { showHourlyChimes = value; } }
        [Browsable(true)]
        public bool ShowNonJoinableBattles { get { return showNonJoinableBattles; } set { showNonJoinableBattles = value; } }
        [Browsable(false)]
        public bool ShowOfficialBattles { get { return showOfficialBattles; } set { showOfficialBattles = value; } }

        [Category("Connection")]
        [DisplayName("Spring Server Address")]
        [Description("Hostname of spring server")]
        public string SpringServerHost { get { return springServerHost; } set { springServerHost = value; } }
        [Category("Connection")]
        [DisplayName("Spring Server Name")]
        [Description("Port of spring server")]
        public int SpringServerPort { get { return springServerPort; } set { springServerPort = value; } }

        [Category("General")]
        [DisplayName("Start Minimized")]
        [Description("Should program start minimized")]
        public bool StartMinimized { get; set; }
        [Category("Chat")]
        [DisplayName("Color: Default text")]
        [XmlIgnore]
        public Color TextColor
        {
            get { return Color.FromArgb(TextColorInt); }
            set
            {
                TextColorInt = value.ToArgb();
                UpdateFadeColor();
            }
        }


        [Browsable(false)]
        public int TextColorInt = Color.Black.ToArgb();
        /// <summary>
        /// Keeps datetime of last topic change for each channel
        /// </summary>
        public SerializableDictionary<string, DateTime> Topics = new SerializableDictionary<string, DateTime>();
        [Browsable(false)]
        public bool ResetUiKeysHack4 { get; set; }
        public Config() {}


        public void UpdateFadeColor()
        {
            FadeColor = Color.FromArgb((TextColor.R + BgColor.R)/2, (TextColor.G + BgColor.G)/2, (TextColor.B + BgColor.B)/2);
        }


        public object Clone()
        {
            return MemberwiseClone();
        }

        [Browsable(false)]
        public int RepoMasterRefresh { get { return 120; } }


        [Browsable(false)]
        public string PackageMasterUrl { get { return " http://repos.springrts.com/"; } }
        
        [Category("General")]
        [DisplayName("Enable voice commands (EXPERIMENTAL)")]
        [Description("Control the game using your voice")]
        public bool EnableVoiceCommands { get ; set; }

        
        
        [Browsable(false)]
        public int AdDelays = 1;
        [Browsable(false)]
        public string AdChannels = "main,newbies";
        [Browsable(false)]
        public string AdSuffix = "\r\n\r\nmodlink: http://zero-k.info/";
        [Browsable(false)]
        public string AdLines = "Faster, smarter, nicer! \r\n";
        [Browsable(false)]
        public string AdPreffix = "Join ZK \r\njoin zero-k. \r\nJoin ZK. \r\n";
        bool autoActivateQuickmatch = true;
        [Browsable(false)]
        public bool UseSafeMode { get; set; }
    }


    /**********************/

    public class XmlFont
    {
        public string FontFamilyName;
        public GraphicsUnit GraphicsUnit;
        public float Size;
        public FontStyle Style;

        public XmlFont([NotNull] Font f)
        {
            if (f == null) throw new ArgumentNullException("f");
            FontFamilyName = f.FontFamily.Name;
            GraphicsUnit = f.Unit;
            Size = f.Size;
            Style = f.Style;
        }

        public XmlFont()
        {
            using (var f = new Font("Microsoft Sans Serif", 10))
            {
                FontFamilyName = f.FontFamily.Name;
                GraphicsUnit = f.Unit;
                Size = f.Size;
                Style = f.Style;
            }
        }

        public Font ToFont()
        {
            return new Font(FontFamilyName, Size, Style, GraphicsUnit);
        }
    }
}