using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Media;
using System.Xml;
using CMissionLib.Actions;
using CMissionLib.Conditions;
using CMissionLib.UnitSyncLib;
using Ionic.Zip;

namespace CMissionLib
{
	[DataContract]
	public class Mission : PropertyChanged
	{
		string author = "Default Author";
		string contentFolderPath;
		ObservableCollection<string> counters = new ObservableCollection<string>();
		string description = String.Empty;
		ObservableCollection<string> disabledGadgets = new ObservableCollection<string>();
		ObservableCollection<string> disabledUnits = new ObservableCollection<string>();
		ObservableCollection<string> disabledWidgets = new ObservableCollection<string>();
		Dictionary<string, string> folders = new Dictionary<string, string>();
		string imagePath;
		Map map;
		string mapName;
		int maxUnits = 5000;
		Mod mod;
		string modName;
		string name;
		ObservableCollection<Player> players = new ObservableCollection<Player>();
		string rapidTag;
		ObservableCollection<Region> regions = new ObservableCollection<Region>();
		string scoringMethod = "";
		int startingEnergy = 1000;
		int startingMetal = 1000;
		Player startingPlayer = new Player { Name = "Player 1", Color = Colors.Blue, Alliance = "1", IsHuman = true };
		ObservableCollection<Trigger> triggers = new ObservableCollection<Trigger>();
		public IEnumerable<string> AllGroups
		{
			get
			{
				var groups = new List<string>();
				groups.AddRange(AllUnits.SelectMany(u => u.Groups));
				groups.AddRange(AllLogic.OfType<GiveFactoryOrdersAction>().SelectMany(a => a.BuiltUnitsGroups));
				foreach (var player in players) groups.Add("Latest Factory Built Unit (" + player.Name + ")");
				return groups.Distinct();
			}
		}
		public IEnumerable<TriggerLogic> AllLogic { get { return triggers.SelectMany(t => t.Logic); } }
		public IEnumerable<UnitStartInfo> AllUnits { get { return triggers.SelectMany(t => t.AllUnits); } }
		public IEnumerable<string> Alliances { get { return players.Select(p => p.Alliance).Distinct(); } }
		[DataMember]
		public string Author
		{
			get { return author; }
			set
			{
				author = value;
				RaisePropertyChanged("Author");
			}
		}
		[DataMember]
		public string ContentFolderPath
		{
			get { return contentFolderPath; }
			set
			{
				contentFolderPath = value;
				RaisePropertyChanged("ContentFolderPath");
			}
		}
		public IEnumerable<string> Countdowns { get { return AllLogic.OfType<StartCountdownAction>().Select(u => u.Countdown).Distinct(); } }
		[DataMember]
		public ObservableCollection<string> Counters
		{
			get { return counters; }
			set
			{
				counters = value;
				RaisePropertyChanged("Counters");
			}
		}
		[DataMember]
		public string Description
		{
			get { return description; }
			set
			{
				description = value;
				RaisePropertyChanged("Description");
			}
		}

		[DataMember]
		public ObservableCollection<string> DisabledGadgets { get { return disabledGadgets; } set { disabledGadgets = value; } }

		[DataMember]
		public ObservableCollection<string> DisabledUnits { get { return disabledUnits; } set { disabledUnits = value; } }
		[DataMember]
		public ObservableCollection<string> DisabledWidgets { get { return disabledWidgets; } set { disabledWidgets = value; } }
		public Dictionary<string, string> Folders
		{
			get { return folders; }
			set
			{
				folders = value;
				RaisePropertyChanged("Folders");
			}
		}
		[DataMember]
		public string ImagePath
		{
			get { return imagePath; }
			set
			{
				imagePath = value;
				RaisePropertyChanged("ImagePath");
			}
		}

		public Map Map
		{
			get { return map; }
			set
			{
				map = value;
				RaisePropertyChanged("Map");
				mapName = value.Name;
			}
		}
		[DataMember]
		public string MapName { get { return mapName; } set { mapName = value; } }
		[DataMember]
		public int MaxUnits { get { return maxUnits; } set { maxUnits = value; } }

		public Mod Mod
		{
			get { return mod; }
			set
			{
				mod = value;
				modName = mod.Name;
				RaisePropertyChanged("Mod");
			}
		}
		[DataMember]
		public string ModName { get { return modName; } set { modName = value; } }

		[DataMember]
		public string Name { get { return name; } set { name = value; } }
		[DataMember]
		public ObservableCollection<Player> Players { get { return players; } set { players = value; } }
		[DataMember]
		public string RapidTag
		{
			get { return rapidTag; }
			set
			{
				rapidTag = value;
				RaisePropertyChanged("RapidTag");
			}
		}
		[DataMember]
		public ObservableCollection<Region> Regions
		{
			get { return regions; }
			set
			{
				regions = value;
				RaisePropertyChanged("Regions");
			}
		}

		[DataMember]
		public string ScoringMethod
		{
			get { return scoringMethod; }
			set
			{
				scoringMethod = value;
				RaisePropertyChanged("ScoringMethod");
			}
		}
		[DataMember]
		public int StartingEnergy { get { return startingEnergy; } set { startingEnergy = value; } }
		[DataMember]
		public int StartingMetal { get { return startingMetal; } set { startingMetal = value; } }

		[DataMember]
		public Player StartingPlayer { get { return startingPlayer; } set { startingPlayer = value; } }
		public IEnumerable<string> TriggerNames { get { return triggers.Cast<INamed>().Select(t => t.Name); } }
		[DataMember]
		public ObservableCollection<Trigger> Triggers { get { return triggers; } set { triggers = value; } }

		public Mission(string name, Mod game, Map map)
		{
			Mod = game;
			Map = map;
			Name = name;
			ModName = game.Name;
			MapName = map.Name;
			var testAI = game.AllAis.FirstOrDefault(ai => ai.ShortName.Contains("NullAI"));
			var player1 = new Player { Name = "Player 1", Color = Colors.Blue, Alliance = "Alliance 1", IsHuman = true, };
			var player2 = new Player { Name = "Player 2", Color = Colors.Red, Alliance = "Alliance 2", IsHuman = false, };
			StartingPlayer = player1;
			Players.Add(player1);
			Players.Add(player2);
			var gameStartTrigger = new Trigger();
			Triggers.Add(gameStartTrigger);
			gameStartTrigger.Logic.Add(new GameStartedCondition());
			var unitType = game.UnitDefs.First();
			var startUnits = new[]
			                 {
			                 	new UnitStartInfo(unitType, player1, 100, 200), new UnitStartInfo(unitType, player1, 200, 300),
			                 	new UnitStartInfo(unitType, player1, 140, 220), new UnitStartInfo(unitType, player2, 162, 121),
			                 	new UnitStartInfo(unitType, player2, 223, 142),
			                 };
			gameStartTrigger.Logic.Add(new CreateUnitsAction(startUnits));
			var widgets = new string[] { "gui_pauseScreen.lua", "gui_center_n_select.lua", "gui_take_remind.lua", "gui_startup_info_selector.lua" };
			foreach (var widget in widgets) DisabledWidgets.Add(widget);
			var gadgets = new string[] { "start_unit_setup.lua" };
			foreach (var gadget in gadgets) DisabledGadgets.Add(gadget);
		}



		public void CreateArchive(string mutatorPath)
		{
#if DEBUG
			File.WriteAllText("startscript.txt", GetScript());
			File.WriteAllText("modinfo.txt", GetModInfo());
			File.WriteAllText("mission.lua", SerializeToLua());
#endif
			var textEncoding = Encoding.GetEncoding("iso-8859-1"); // ASCIIEncoding()
			using (var zip = new ZipFile())
			{
				if (!String.IsNullOrEmpty(ContentFolderPath) && Directory.Exists(ContentFolderPath))
				{
					zip.SafeAddDirectory(ContentFolderPath);
				}

				var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var basePath = Path.Combine(assemblyLocation, "MissionBase");
				zip.SafeAddDirectory(basePath);

				zip.SafeAddEntry("modinfo.lua", textEncoding.GetBytes(GetModInfo()));
				zip.SafeAddEntry("mission.lua", textEncoding.GetBytes(SerializeToLua()));
				zip.SafeAddEntry("script.txt", textEncoding.GetBytes(GetScript()));
				zip.SafeAddEntry("dependencies.txt", String.Join(";", Mod.Dependencies)); // FIXME

				var stream = new MemoryStream();
				using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, CheckCharacters = true })) new NetDataContractSerializer().WriteObject(writer, this);
				stream.Position = 0;
				zip.SafeAddEntry("project.mission.xml", stream);

				// disable scripts by hiding them with a blank file
				var blank = textEncoding.GetBytes("-- intentionally left blank --");
				foreach (var widget in disabledWidgets.Distinct()) zip.SafeAddEntry("LuaUI/Widgets/" + widget, blank);
				foreach (var gadget in disabledGadgets.Distinct()) zip.SafeAddEntry("LuaRules/Gadgets/" + gadget, blank);

				// include media in mod archive
				foreach (var item in AllLogic)
				{
					if (item is GuiMessageAction)
					{
						var action = (GuiMessageAction)item;
						if (!String.IsNullOrEmpty(action.ImagePath))
						{
							if (!File.Exists(action.ImagePath)) throw new Exception("Image not found: " + action.ImagePath);
							zip.SafeAddFile(action.ImagePath, "LuaUI/Images/");
						}
					}
					else if (item is SoundAction)
					{
						var action = (SoundAction)item;
						if (!String.IsNullOrEmpty(action.SoundPath) && File.Exists(action.SoundPath))
						{
							if (!File.Exists(action.SoundPath)) throw new Exception("Sound not found: " + action.SoundPath);
							zip.SafeAddFile(action.SoundPath, "LuaUI/Sounds/");
						}
					}
				}

				zip.Save(mutatorPath);
			}
		}

		public Trigger FindLogicOwner(TriggerLogic l)
		{
			return triggers.Single(t => t.Logic.Contains(l));
		}

		public static Mission FromFile(string path)
		{
			if (path.ToLower().EndsWith(".xml")) using (var stream = File.OpenRead(path)) return (Mission)new NetDataContractSerializer().ReadObject(stream);
			else
			{
				var projectFileName = "project.mission.xml";
				using (var zip = ZipFile.Read(path))
				{
					var entry = zip.First(e => e.FileName == projectFileName);
					using (var memoryStream = new MemoryStream())
					{
						entry.Extract(memoryStream);
						memoryStream.Position = 0;
						return (Mission)new NetDataContractSerializer().ReadObject(memoryStream);
					}
				}
			}
		}

		public double FromIngameX(double x)
		{
			return x * Map.Texture.Width / Map.Size.Width;
		}

		public double FromIngameY(double y)
		{
			return y * Map.Texture.Height / Map.Size.Height;
		}

		public string GetScript()
		{
			var sb = new StringBuilder();
			var allianceCount = Players.Select(p => p.Alliance).Distinct().Count();
			sb.AppendFormat("[GAME]\n");
			sb.AppendLine("{");
			Action<string, object> line = (key, value) => sb.AppendFormat("\t{0}={1};\n", key, value);
			line("MapName", MapName);
			line("StartMetal", StartingMetal);
			line("StartEnergy", StartingEnergy);
			line("StartposType", "1");
			line("GameMode", "0");
			line("GameType", Name);
			line("LimitDGun", "0");
			line("DiminishingMMs", "0");
			line("GhostedBuildings", "1");
			line("HostIP", "localhost");
			line("HostPort", "8452");
			line("IsHost", "1");
			line("MyPlayerNum", Players.IndexOf(StartingPlayer));
			line("MyPlayerName", StartingPlayer.Name.Replace(' ', '_'));
			line("NumPlayers", Players.Count(p => p.IsHuman));
			line("NumTeams", Players.Count);
			line("NumUsers", Players.Count);
			line("NumRestrictions", "0");
			line("MaxSpeed", "20");
			line("MinSpeed", "0.1");

			foreach (var player in Players) WritePlayer(sb, player);
			foreach (var player in Players) WriteTeam(sb, player);
			for (var i = 0; i < allianceCount; i++) MakeAllyTeam(sb, i);
			sb.AppendLine("}");
			return sb.ToString();
		}

		/// <summary>
		/// Post-deserialization tasks
		/// </summary>
		public void PostLoad()
		{
			// when a mission is deserialized or the game is changed, the triggers hold null or old references to unitdefs
			foreach (var unit in AllUnits)
			{
				var unitDef = Mod.UnitDefs.FirstOrDefault(ud => unit.UnitDefName == ud.Name);
				unit.UnitDef = unitDef ?? Mod.UnitDefs.First(); // if unit not found, use first valid unitdef - show warning?
			}

			// create AI references
			foreach (var player in Players)
			{
				// try finding an ai with same name and version
				var ai = mod.AllAis.FirstOrDefault(a => a.Version == player.AIVersion && a.ShortName == player.AIDll);
				if (ai == null)
				{
					// just try to find an ai with the same name
					ai = mod.AllAis.FirstOrDefault(a => a.ShortName == player.AIDll);
				}
				player.AI = ai;
			}

			// compatibility
			if (regions == null) regions = new ObservableCollection<Region>();
			if (folders == null) folders = new Dictionary<string, string>();
			// get rid of legacy dummies
			foreach (var trigger in triggers) foreach (var item in trigger.Logic.ToArray()) if (item is DummyAction || item is DummyCondition) trigger.Logic.Remove(item);
		}

		public void SaveToXmlFile(string path)
		{
			using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true, CheckCharacters = true })) new NetDataContractSerializer().WriteObject(writer, this);
		}

		public string SerializeToLua()
		{
			var luaTable = GetLuaTable();
			return "return " + luaTable;
		}

		public double ToIngameX(double x)
		{
			return x / Map.Texture.Width * Map.Size.Width;
		}

		public double ToIngameY(double y)
		{
			return y / Map.Texture.Height * Map.Size.Height;
		}

		public string VerifyCanPublish()
		{
			if (ImagePath == null || !File.Exists(ImagePath)) return "A mission image needs to be set in the Mission Settings dialog.";
			if (String.IsNullOrEmpty(Description)) return "A description needs to be set in the Mission Settings dialog.";
			return null;
		}

		public override string ToString()
		{
			return name;
		}

		LuaTable GetLuaTable()
		{
			var luaMap = new Dictionary<object, object>
			             {
#if DEBUG
			             	{ "debug", true },
#endif
			             	{ "map", Map.Name },
			             	{ "players", LuaTable.CreateArray(players.Select(p => p.Name)) },
			             	{ "triggers", LuaTable.CreateArray(triggers.Select(t => t.GetLuaMap(this))) },
			             	{ "startPlayer", Players.IndexOf(StartingPlayer) },
			             	{ "disabledUnits", LuaTable.CreateArray(DisabledUnits) },
			             	{ "scoringMethod", scoringMethod },
			             	{ "counters", LuaTable.CreateArray(Counters) },
			             };
			return new LuaTable(luaMap);
		}

		string GetModInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("local modinfo = {");
			sb.AppendFormat("  name		=	[[{0}]],\n", Name);
			sb.AppendFormat("  description	=	[[{0}]],\n", Description);
			sb.AppendLine("  modtype		=	[[0]],");
			sb.AppendLine("  depend = {");
			sb.AppendFormat("    [[{0}]]\n", Mod.Name);
			sb.AppendLine("  },");
			sb.AppendLine("}");
			sb.AppendLine("return modinfo");
			return sb.ToString();
		}

		void MakeAllyTeam(StringBuilder sb, int index)
		{
			sb.AppendFormat("\t[ALLYTEAM{0}]\n", index);
			sb.AppendLine("\t{");
			sb.AppendFormat("\t\tNumAllies=0;\n"); // it seems that NumAllies has no effect
			sb.AppendLine("\t}");
		}

		void WritePlayer(StringBuilder sb, Player player)
		{
			if (player.IsHuman)
			{
				var index = Players.IndexOf(player);
				sb.AppendFormat("\t[PLAYER" + (index + 1) + "]\n");
				sb.AppendLine("\t{");
				sb.AppendFormat("\t\tName={0};\n", player.Name.Replace(' ', '_'));
				sb.AppendFormat("\t\tSpectator=0;\n");
				sb.AppendFormat("\t\tTeam={0};\n", index);
				sb.AppendLine("\t}");
			}
			else
			{
				var index = Players.IndexOf(player);
				sb.AppendFormat("\t[AI" + index + "]\n");
				sb.AppendLine("\t{");
				sb.AppendFormat("\t\tName={0};\n", player.Name.Replace(' ', '_'));
				sb.AppendFormat("\t\tShortName={0};\n", String.IsNullOrEmpty(player.AIDll) ? "NullAI" : player.AIDll);
				sb.AppendFormat("\t\tVersion={0};\n", String.IsNullOrEmpty(player.AIVersion) ? "0.1" : player.AIVersion);
				sb.AppendFormat("\t\tTeam={0};\n", index);
				sb.AppendFormat("\t\tIsFromDemo=0;\n");
				sb.AppendFormat("\t\tHost=1;\n");
				sb.AppendFormat("\t\t[Options] {{}}\n");
				sb.AppendLine("\t}");
			}
		}

		void WriteTeam(StringBuilder sb, Player player) // no commshares for now
		{
			var index = Players.IndexOf(player);
			var alliances = Players.Select(p => p.Alliance).Distinct().ToList();
			sb.AppendFormat("\t[TEAM{0}]\n", index);
			sb.AppendLine("\t{");
			sb.AppendFormat("\t\tTeamLeader={0};\n", 1); // todo: verify if correct
			sb.AppendFormat("\t\tAllyTeam={0};\n", alliances.IndexOf(player.Alliance));
			sb.AppendFormat("\t\tRGBColor={0} {1} {2};\n", player.Color.ScR, player.Color.ScG, player.Color.ScB); // range: 0-1
			sb.AppendFormat("\t\tSide={0};\n", Mod.Sides.First());
			sb.AppendFormat("\t\tHandicap=0;\n");
			sb.AppendLine("\t}");
		}
	}
}