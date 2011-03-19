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

    StringCollection autoJoinChannels = new StringCollection();
    bool connectOnStartup = true;
    Color fadeColor = Color.Gray;
    StringCollection friends = new StringCollection(); // lacks events for adding friends immediatly
    int idleTime = 10;
    StringCollection ignoredUsers = new StringCollection();
    bool limitedMode = false;
    string lobbyPlayerName;
    string lobbyPlayerPassword;
    string manualSpringPath = @"C:\Program Files\Spring";
    List<string> selectedGames = new List<string>();
    bool showHourlyChimes = true;
    bool showNonJoinableBattles = true;


    string springServerHost = "springrts.com";
    int springServerPort = 8200;

    [Category("Widgets")]
    [Description("Auto-Install Widgets")]
    [DisplayName("Auto-Install Widgets")]
    public bool AutoInstallWidgets { get; set; }
    [Category("Chat")]
    [DisplayName("Automatically Joined Channels")]
    [Description("Zero-K lobby will automatically join these channels when connecting.")]
    [Browsable(true)]
    [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
      typeof(UITypeEditor))]
    public StringCollection AutoJoinChannels { get { return autoJoinChannels; } set { autoJoinChannels = value; } }


    [Category("Widgets")]
    [Description("Auto-Update Widgets")]
    [DisplayName("Auto-Update Widgets")]
    public bool AutoUpdateWidgets { get; set; }
    [Category("General")]
    [DisplayName("Default filter for battlelist")]
    [Description("This is the filter entered in battle list by default")]
    public string BattleFilter { get; set; }


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
    [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
      typeof(UITypeEditor))]
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
    [Category("Chat")]
    [DisplayName("Ignored Users")]
    [Description("The messages of these users are ignored.")]
    [Browsable(true)]
    [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
      typeof(UITypeEditor))]
    public StringCollection IgnoredUsers { get { return ignoredUsers; } set { ignoredUsers = value; } }
    public bool IsFirstRun = true;
    [Browsable(false)]
    public bool JoinChannelsSetupDone;


    [Category("Chat")]
    [DisplayName("Color: Joins")]
    [XmlIgnore]
    public Color JoinColor { get { return Color.FromArgb(JoinColorInt); } set { JoinColorInt = value.ToArgb(); } }
    [Browsable(false)]
    public int JoinColorInt = Color.FromArgb(42, 140, 42).ToArgb();
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
    public bool LimitedMode
    {
      get { return limitedMode; }
      set
      {
        if (limitedMode != value) JoinChannelsSetupDone = false;

        limitedMode = value;
        try
        {
          WindowsApi.InternetSetCookie(BaseUrl, GlobalConst.LimitedModeCookieName, value ? "1" : "0");
        }
        catch (Exception ex)
        {
          Trace.TraceError("Cannot set user cookie: {0}", ex);
        }
      }
    }


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


    [Category("General")]
    [DisplayName("Spring Path")]
    [Description("Path to spring")]
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

    [Category("General")]
    [DisplayName("Show Empty Battles")]
    [Description("Show battles with no players in the battle list.")]
    public bool ShowEmptyBattles { get; set; }
    [Category("Chat")]
    [DisplayName("Show Hourly Chat Message")]
    [Description("Show a notification in chat channels every hour.")]
    public bool ShowHourlyChimes { get { return showHourlyChimes; } set { showHourlyChimes = value; } }
    [Browsable(true)]
    public bool ShowNonJoinableBattles { get { return showNonJoinableBattles; } set { showNonJoinableBattles = value; } }

    [Category("General")]
    [DisplayName("Sort Battles by Players")]
    [Description("Show battles with the most players first.")]
    public bool SortBattlesByPlayers { get; set; }
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
  	public bool BlockNonFriendPm;
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
    public string PackageMasterUrl { get { return "http://repos.caspring.org/"; } }
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