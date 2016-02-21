using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Text;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using PlasmaDownloader;
using ZkData;

namespace ZeroKLobby
{
    public class Config : ICloneable, IPlasmaDownloaderConfig
    {
        public const string ConfigFileName = "ZeroKLobbyConfig.xml";
        public const string LogFile = "ZeroKLobbyErrors.txt";
        public static readonly Font MenuFont = new Font("Verdana", 20, GraphicsUnit.Pixel);
        public static readonly Font ChatFont = new Font("Microsoft Sans Serif", 14, GraphicsUnit.Pixel);
        public static readonly Font GeneralFont = new Font("Microsoft Sans Serif", 14, GraphicsUnit.Pixel);
        public static readonly Font GeneralFontBig = new Font("Microsoft Sans Serif", 15, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font GeneralFontSmall = new Font("Microsoft Sans Serif", 9, GraphicsUnit.Pixel);


        public static readonly Font ToolbarFontBig;
        public static readonly Font ToolbarFont;
        public static readonly Font ToolbarFontSmall;
        public static readonly Font MainPageFontBig;
        public static readonly Font MainPageFont;
        public static readonly Font MainPageFontSmall;


        private static readonly PrivateFontCollection pfc = new PrivateFontCollection();

        public readonly Color BgColor = Color.FromArgb(255, 0, 60, 80);
        public readonly Color EmoteColor = Color.FromArgb(178, 0, 178);
        public readonly Color FadeColor = Color.LightGray;
        public readonly Color JoinColor = Color.FromArgb(42, 140, 42);
        public readonly Color LeaveColor = Color.FromArgb(102, 54, 31);
        public readonly Color NoticeColor = Color.Red;
        public readonly Color TextColor = Color.White;
        public readonly Color TooltipColor = Color.White;


        [Browsable(false)]
        public bool BlockNonFriendPm;

        private string cachedPassword;
        public bool IsFirstRun = true;


        public Color LinkColor = Color.DodgerBlue;

        /// <summary>
        ///     Keeps datetime of last topic change for each channel
        /// </summary>
        public SerializableDictionary<string, DateTime?> Topics = new SerializableDictionary<string, DateTime?>();

        static Config()
        {
            FontFamily fancyFont;

            // TODO copy out from resources
            if (File.Exists("Sm.ttf"))
            {
                pfc.AddFontFile("Sm.ttf");
                fancyFont = pfc.Families[0];
            }
            else fancyFont = FontFamily.GenericSansSerif;

            MenuFont = new Font(fancyFont, 20, GraphicsUnit.Pixel);

            ToolbarFontBig = new Font(fancyFont, 14, GraphicsUnit.Pixel);
            ToolbarFont = new Font(fancyFont, 12, GraphicsUnit.Pixel);
            ToolbarFontSmall = new Font(fancyFont, 9, GraphicsUnit.Pixel);

            MainPageFontBig = new Font(fancyFont, 14, GraphicsUnit.Pixel);
            MainPageFont = new Font(fancyFont, 12, GraphicsUnit.Pixel);
            MainPageFontSmall = new Font(fancyFont, 9, GraphicsUnit.Pixel);
        }

        public Config()
        {
            EnableVoiceChat = true;
            SpringServerHost = GlobalConst.LobbyServerHost;
            SpringServerPort = GlobalConst.LobbyServerPort;
        }

        [Category("Chat")]
        [DisplayName("Automatically Joined Channels")]
        [Description("Zero-K launcher will automatically join these channels when connecting.")]
        [Browsable(true)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            typeof(UITypeEditor))]
        public StringCollection AutoJoinChannels { get; set; } = new StringCollection();


        [Browsable(false)]
        public string BattleFilter { get; set; }


        [Category("Connection")]
        [DisplayName("Connect on startup")]
        [Description("Connect and login player on program start?")]
        public bool ConnectOnStartup { get; set; } = true;

        [Category("General")]
        [DisplayName("Content DATA FOLDER")]
        [Description("Place where all the content is saved")]
        public string DataFolder { get; set; }


        [Category("Debugging")]
        [DisplayName("Disable Lobby Auto Update")]
        [Description("Lobby will not update itself to latest release version. Use this if you are compiling your own lobby")]
        public bool DisableAutoUpdate { get; set; }

        [Category("Chat")]
        [DisplayName("Disable Bubble On Channel Highlight")]
        [Description("Disable the system tray bubble when someone says your name in a public channel.")]
        public bool DisableChannelBubble { get; set; }


        [Category("Chat")]
        [DisplayName("Disable Bubble On Private Message")]
        public bool DisablePmBubble { get; set; }

        [Category("Tooltip")]
        [DisplayName("Disable player tooltip")]
        [Description("Disable ZKL tooltip that display player status.")]
        public bool DisablePlayerTooltip { get; set; }

        [Category("Tooltip")]
        [DisplayName("Disable battle tooltip")]
        [Description("Disable ZKL tooltip that display room information.")]
        public bool DisableBattleTooltip { get; set; }

        [Category("Tooltip")]
        [DisplayName("Disable map tooltip")]
        [Description("Disable ZKL tooltip that display map and minimap information.")]
        public bool DisableMapTooltip { get; set; }

        [Category("Tooltip")]
        [DisplayName("Disable text tooltip")]
        [Description("Disable ZKL tooltip that display tips on buttons and options.")]
        public bool DisableTextTooltip { get; set; }

        [Category("General")]
        [DisplayName("Enable voice chat (push to talk)")]
        [Description("Needs steam running")]
        public bool EnableVoiceChat { get; set; }

        [Category("Devving")]
        [DisplayName("Enable UnitSync Dialog Box")]
        [Description(
            "Allow ZKL to process new mod/map information without connecting to server, " +
            "and give user the choice to keep this information only in local cache rather than sharing it with server. This option is meant to be used with Skirmisher Tab. This option is force disabled on Linux"
            )]
        public bool EnableUnitSyncPrompt { get; set; }


        [Category("Chat")]
        [DisplayName("Friend List")]
        [Description("List of friends.")]
        [Browsable(true)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            typeof(UITypeEditor))]
        public StringCollection Friends { get; set; } = new StringCollection();
        [Browsable(false)]
        public bool HasHosted { get; set; }
        [Browsable(false)]
        public string HostBattle_SpringieCommands { get; set; }
        [Browsable(false)]
        public string HostBattle_Title { get; set; }
        [Browsable(false)]
        public bool HideEmptyBattles { get; set; } = false;
        [Browsable(false)]
        public bool HideNonJoinableBattles { get; set; } = false;
        [Browsable(false)]
        public bool HidePasswordedBattles { get; set; } = false;

        [Category("Quickmatching")]
        [DisplayName("Idle User Time")]
        [Description("Idle minutes after which Zero-K launcher assumes the user is gone and quickmatching is stopped.")]
        public int IdleTime { get; set; } = 5;

        [Category("Chat")]
        [DisplayName("Ignored Users")]
        [Description("The messages of these users are ignored.")]
        [Browsable(true)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            typeof(UITypeEditor))]
        public StringCollection IgnoredUsers { get; set; } = new StringCollection();


        [Category("Chat")]
        [DisplayName("Disable Context Menu on Leftclick")]
        [Description("Only right clicking shows context menu. Left clicking on a playername will select them in the player list.")]
        public bool LeftClickSelectsPlayer { get; set; }

        [Category("Account")]
        [DisplayName("Lobby Player Name")]
        [Description("Player name from lobby (tasclient), needed for many features")]
        public string LobbyPlayerName { get; set; }


        [Category("Account")]
        [DisplayName("Lobby Password")]
        [PasswordPropertyText(true)]
        [XmlIgnore]
        [Description("Player password from lobby (tasclient), needed for widget online profile")]
        public string LobbyPlayerPassword
        {
            get
            {
                if (!string.IsNullOrEmpty(cachedPassword)) return cachedPassword;
                try
                {
                    var isoStore = GetIsolatedStorage();
                    using (var file = isoStore.OpenFile("zkl_password.txt", FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite)) using (var sr = new StreamReader(file)) cachedPassword = sr.ReadToEnd().Trim();
                    return cachedPassword;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error loading password from local storage: {0}", ex);
                    return null;
                }
            }
            set
            {
                try
                {
                    var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
                    using (var file = isoStore.OpenFile("zkl_password.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite)) using (var sr = new StreamWriter(file)) sr.Write(value);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Error saving password to local storage: {0}", ex);
                }
                cachedPassword = value;
            }
        }

        [Category("Account")]
        [DisplayName("Forget Player Name")]
        [Description("Tell ZKL to forget your Player Name and re-ask it each time it start. (Note: If ZKL crashed or forced to exit this might fail)")
        ]
        public bool DiscardPlayerName { get; set; }

        [Category("Account")]
        [DisplayName("Forget Password")]
        [Description("Tell ZKL to forget your Password and re-ask it each time it start. (Note: If ZKL crashed or forced to exit this might fail)")]
        public bool DiscardPassword { get; set; }


        [Category("Chat")]
        [DisplayName("Show Hourly Chat Message")]
        [Description("Show a notification in chat channels every hour.")]
        public bool ShowHourlyChimes { get; set; } = true;

        [Browsable(false)]
        public bool ShowOfficialBattles { get; set; } = true;

        [Browsable(false)]
        public string SkirmisherEngine { get; set; }
        [Browsable(false)]
        public string SkirmisherGame { get; set; }
        [Browsable(false)]
        public string SkirmisherMap { get; set; }

        [Category("Connection")]
        [DisplayName("Spring Server Address")]
        [Description("Hostname of spring server")]
        public string SpringServerHost { get; set; } = GlobalConst.LobbyServerHost;
        [Category("Connection")]
        [DisplayName("Spring Server Name")]
        [Description("Port of spring server")]
        public int SpringServerPort { get; set; } = GlobalConst.LobbyServerPort;


        [Browsable(false)]
        public bool UseSafeMode { get; set; }


        public object Clone()
        {
            return MemberwiseClone();
        }

        [Browsable(false)]
        public int RepoMasterRefresh { get { return 0; } }


        [Browsable(false)]
        public string PackageMasterUrl { get { return "http://repos.springrts.com/"; } }

        [Browsable(false)]
        public bool InterceptPopup { get; set; } = true;

        public bool UseExternalBrowser { get; set; } = false;
        public bool SingleInstance { get; set; } = false;

        private static IsolatedStorageFile GetIsolatedStorage()
        {
            var isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);
            return isoStore;
        }

        public static Config Load(string path)
        {
            Config conf;
            if (File.Exists(path))
            {
                var xs = new XmlSerializer(typeof(Config));
                try
                {
                    conf = (Config)xs.Deserialize(new StringReader(File.ReadAllText(path)));
                    conf.SpringServerHost = GlobalConst.LobbyServerHost;
                    conf.SpringServerPort = GlobalConst.LobbyServerPort;

                    return conf;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error reading config file: {0}", ex);
                }
            }
            conf = new Config { IsFirstRun = true };
            return conf;
        }

        public void Save(string path)
        {
            try
            {
                var cols = new StringCollection();
                cols.AddRange(AutoJoinChannels.OfType<string>().Distinct().ToArray());
                AutoJoinChannels = cols;
                var xs = new XmlSerializer(typeof(Config));
                var sb = new StringBuilder();
                using (var stringWriter = new StringWriter(sb)) xs.Serialize(stringWriter, this);
                File.WriteAllText(path, sb.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error saving config: {0}", ex);
            }
        }
    }
}