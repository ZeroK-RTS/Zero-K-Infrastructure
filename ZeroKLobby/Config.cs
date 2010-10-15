using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Xml.Serialization;
using JetBrains.Annotations;
using PlasmaDownloader;
using PlasmaShared;

namespace ZeroKLobby
{
	public class Config: ICloneable, IPlasmaDownloaderConfig
	{
		public const string ConfigFileName = "SpringDownloaderConfig.xml";
		public const string ErrorsUploadSite = "http://files.caspring.org/caupdater/spring_errors/upload.php";
		public const string LogFile = "ZeroKLobbyErrors.txt";
		public const string MonoTorrentVersion = "117187-modded";
		public const string ReportUrl = "http://cadownloader.licho.eu/error.php";
		public const string SelfUpdateSite = "http://files.caspring.org/caupdater/";
		StringCollection autoJoinChannels = new StringCollection();
		bool connectOnStartup = true;
		Color fadeColor = Color.Gray;
		StringCollection friends = new StringCollection(); // lacks events for adding friends immediatly
		int idleTime = 10;
		StringCollection ignoredUsers = new StringCollection();
		string manualSpringPath = @"C:\Program Files\Spring";
		List<string> selectedGames = new List<string>();
		bool showHourlyChimes = true;

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


		[Category("Chat")]
		[DisplayName("Color: Joins")]
		[XmlIgnore]
		public Color JoinColor { get { return Color.FromArgb(JoinColorInt); } set { JoinColorInt = value.ToArgb(); } }
		[Browsable(false)]
		public int JoinColorInt = Color.FromArgb(42, 140, 42).ToArgb();

		[Category("Chat")]
		[DisplayName("Color: Leaves")]
		[XmlIgnore]
		public Color LeaveColor { get { return Color.FromArgb(LeaveColorInt); } set { LeaveColorInt = value.ToArgb(); } }
		[Browsable(false)]
		public int LeaveColorInt = Color.FromArgb(102, 54, 31).ToArgb();
		[Category("Chat")]
		[DisplayName("Left click on player in chat selects them")]
		[Description("Left clicking on player in the chat selects them. Right clicking shows context menu.")]
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
		public string LobbyPlayerName { get; set; }

		[Category("Account")]
		[DisplayName("Lobby Password")]
		[Description("Player password from lobby (tasclient), needed for widget online profile")]
		public string LobbyPlayerPassword { get; set; }

		[Category("General")]
		[DisplayName("Spring Path")]
		[Description("Path to spring")]
		public string ManualSpringPath { get { return manualSpringPath; } set { manualSpringPath = value; } }
		[Category("Chat")]
		[DisplayName("Color: Notice")]
		[XmlIgnore]
		public Color NoticeColor { get { return Color.FromArgb(NoticeColorInt); } set { NoticeColorInt = value.ToArgb(); } }
		[Browsable(false)]
		public int NoticeColorInt = Color.Red.ToArgb();
		[Category("General")]
		[DisplayName("Selected games")]
		[Description("Games selected on the first tab")]
		[Browsable(true)]
		[Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
			typeof(UITypeEditor))]
		public List<string> SelectedGames { get { return selectedGames; } set { selectedGames = value; } }

		[Category("General")]
		[DisplayName("Show Empty Battles")]
		[Description("Show battles with no players in the battle list.")]
		public bool ShowEmptyBattles { get; set; }
		[Category("Chat")]
		[DisplayName("Show Hourly Chat Message")]
		[Description("Show a notification in chat channels every hour.")]
		public bool ShowHourlyChimes { get { return showHourlyChimes; } set { showHourlyChimes = value; } }
		[Browsable(true)]
		public bool ShowNonJoinableBattles = true;

		[Category("General")]
		[DisplayName("Sort Battles by Players")]
		[Description("Show battles with the most players first.")]
		public bool SortBattlesByPlayers { get; set; }
		public static string SpringName { get { return "spring.exe"; } }
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