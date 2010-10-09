using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
		ObservableCollection<string> counters = new ObservableCollection<string>();
		string description = "Default Mission";
		ObservableCollection<string> disabledGadgets = new ObservableCollection<string>();
		ObservableCollection<string> disabledUnits = new ObservableCollection<string>();
		ObservableCollection<string> disabledWidgets = new ObservableCollection<string>();
		Map map;
		string mapName;
		int maxUnits = 5000;
		Mod mod;
		string modName;
		string name;
		ObservableCollection<Player> players = new ObservableCollection<Player>();
		string scoringMethod = "";
		int startingEnergy = 1000;
		int startingMetal = 1000;
		Player startingPlayer = new Player {Name = "Player 1", Color = Colors.Blue, Alliance = "1", IsHuman = true};
		ObservableCollection<Trigger> triggers = new ObservableCollection<Trigger>();

		public Mission(string name, Mod game, Map map)
		{
			Mod = game;
			Map = map;
			Name = name;
			ModName = game.Name;
			MapName = map.Name;
			var player1 = new Player { Name = "Player 1", Color = Colors.Blue, Alliance = "1", IsHuman = true };
			var player2 = new Player {Name = "Player 2", Color = Colors.Red, Alliance = "2", IsHuman = false};
			StartingPlayer = player1;
			Players.Add(player1);
			Players.Add(player2);
			var gameStartTrigger = new Trigger();
			Triggers.Add(gameStartTrigger);
			gameStartTrigger.Logic.Add(new GameStartedCondition());
			var unitType = game.UnitDefs.First();
			var startUnits = new[]
				{
					new UnitStartInfo(unitType, player1, 100, 200),
					new UnitStartInfo(unitType, player1, 200, 300),
					new UnitStartInfo(unitType, player1, 140, 220),
					new UnitStartInfo(unitType, player2, 162, 121),
					new UnitStartInfo(unitType, player2, 223, 142),
				};
			gameStartTrigger.Logic.Add(new CreateUnitsAction(startUnits));
			var widgets = new[]
				{
					"autoquit.lua", "camera_lockcamera.lua", "dbg_dcicon.lua", "gui_ally_cursors.lua",
					"gui_center_n_select.lua",
					"gui_limit_dgun.lua", "gui_loadscreens.lua", "gui_point_tracker.lua", "unit_comm_nametags.lua",
					"gui_comm_marker.lua",
					"cmd_unit_mover.lua"
				};
			foreach (var widget in widgets) DisabledWidgets.Add(widget);
			var gadgets = new[]
				{
					"awards.lua", "planetwars.lua", "unit_does_not_count.lua", "unit_spawner.lua",
					"unit_replace_comm.lua"
				};
			foreach (var gadget in gadgets) DisabledGadgets.Add(gadget);
		}

		[DataMember]
		public ObservableCollection<Trigger> Triggers
		{
			get { return triggers; }
			set { triggers = value; }
		}

		[DataMember]
		public ObservableCollection<string> DisabledGadgets
		{
			get { return disabledGadgets; }
			set { disabledGadgets = value; }
		}

		[DataMember]
		public ObservableCollection<string> DisabledWidgets
		{
			get { return disabledWidgets; }
			set { disabledWidgets = value; }
		}

		[DataMember]
		public ObservableCollection<string> DisabledUnits
		{
			get { return disabledUnits; }
			set { disabledUnits = value; }
		}

		[DataMember]
		public ObservableCollection<Player> Players
		{
			get { return players; }
			set { players = value; }
		}

		[DataMember]
		public int StartingMetal
		{
			get { return startingMetal; }
			set { startingMetal = value; }
		}

		[DataMember]
		public int StartingEnergy
		{
			get { return startingEnergy; }
			set { startingEnergy = value; }
		}

		[DataMember]
		public string ModName
		{
			get { return modName; }
			set { modName = value; }
		}

		[DataMember]
		public string Name
		{
			get { return name; }
			set { name = !value.StartsWith("Mission: ") ? "Mission: " + value : value; }
		}

		[DataMember]
		public int MaxUnits
		{
			get { return maxUnits; }
			set { maxUnits = value; }
		}

		[DataMember]
		public string MapName
		{
			get { return mapName; }
			set { mapName = value; }
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
		public ObservableCollection<string> Counters
		{
			get { return counters; }
			set
			{
				counters = value;
				RaisePropertyChanged("Counters");
			}
		}

		public IEnumerable<string> TriggerNames
		{
			get { return triggers.Cast<INamed>().Select(t => t.Name); }
		}

		public IEnumerable<UnitStartInfo> AllUnits
		{
			get { return triggers.SelectMany(t => t.AllUnits); }
		}

		public IEnumerable<TriggerLogic> AllLogic
		{
			get { return triggers.SelectMany(t => t.Logic); }
		}

		public IEnumerable<string> AllGroups
		{
			get { return AllUnits.SelectMany(u => u.Groups).Distinct(); }
		}

		public IEnumerable<string> Alliances
		{
			get { return players.Select(p => p.Alliance).Distinct(); }
		}

		public IEnumerable<string> Countdowns
		{
			get { return AllLogic.OfType<StartCountdownAction>().Select(u => u.Countdown).Distinct(); }
		}

		[DataMember]
		public Player StartingPlayer
		{
			get { return startingPlayer; }
			set { startingPlayer = value; }
		}

		public double ToIngameX(double x)
		{
			return x*Map.Size.Width/Map.Texture.Width;
		}

		public double ToIngameY(double y)
		{
			return y*Map.Size.Height/Map.Texture.Height;
		}

		/// <summary>
		/// Fixes unitdef references in triggers
		/// </summary>
		public void RestoreUnitDefs()
		{
			// when a mission is deserialized or the game is changed, the triggers hold null or old references to unitdefs
			foreach (var unit in AllUnits)
			{
				var unitDef = Mod.UnitDefs.FirstOrDefault(ud => unit.UnitDefName == ud.Name);
				unit.UnitDef = unitDef ?? Mod.UnitDefs.First(); // if unit not found, use first valid unitdef - show warning?
			}
		}

		public Trigger FindLogicOwner(TriggerLogic l)
		{
			return triggers.Single(t => t.Logic.Contains(l));
		}

		public override string ToString()
		{
			return name;
		}

		LuaTable GetLuaTable()
		{
			var luaMap = new Dictionary<string, object>
				{
					{"triggers", new LuaTable(triggers.Select(t => t.GetLuaMap(this)))},
					{"startPlayer", Players.IndexOf(StartingPlayer)},
					{"disabledUnits", new LuaTable(DisabledUnits)},
					{"scoringMethod", scoringMethod},
					{"counters", new LuaTable(Counters)},
				};
			return new LuaTable(luaMap);
		}

		public string SerializeToLua()
		{
			var luaTable = GetLuaTable();
			return "return " + luaTable;
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
				sb.AppendFormat("\t\tShortName=Mission AI;\n");
				sb.AppendFormat("\t\tVersion=<not-versioned>;\n");
				sb.AppendFormat("\t\tTeam={0};\n", index);
				sb.AppendFormat("\t\tIsFromDemo=0;\n");
				sb.AppendFormat("\t\tHost=1;\n");
				sb.AppendFormat("\t\t[Options] {{}}\n");
				sb.AppendLine("\t}");
			}

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
			line("MyPlayerName", StartingPlayer.Name.Replace(" ", "_"));
			line("NumPlayers", Players.Count(p => p.IsHuman));
			line("NumTeams", Players.Count);
			line("NumUsers", Players.Count);
			line("NumRestrictions", "0");
			line("MaxSpeed", "20");
			line("MinSpeed", "0.1");

			foreach (var player in Players)
			{
				WritePlayer(sb, player);
			}
			foreach (var player in Players) WriteTeam(sb, player);
			for (var i = 0; i < allianceCount; i++) MakeAllyTeam(sb, i);
			sb.AppendLine("}");
			return sb.ToString();
		}

		string GetModInfo()
		{
			var sb = new StringBuilder();
			sb.AppendLine("local modinfo = {");
			sb.AppendFormat("	name		=	[[{0}]],\n", Name);
			sb.AppendFormat("	description	=	[[{0}]],\n", Description);
			sb.AppendLine("modtype		=	[[0]],");
			sb.AppendLine("  depend = {");
			sb.AppendFormat("    [[{0}]]\n", Mod.Name);
			sb.AppendLine("  },");
			sb.AppendLine("}");
			sb.AppendLine("return modinfo");
			return sb.ToString();
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

		void MakeAllyTeam(StringBuilder sb, int index)
		{
			sb.AppendFormat("\t[ALLYTEAM{0}]\n", index);
			sb.AppendLine("\t{");
			sb.AppendFormat("\t\tNumAllies=0;\n"); // it seems that NumAllies has no effect
			sb.AppendLine("\t}");
		}

		public void SaveToXmlFile(string path)
		{
			using (var writer = XmlWriter.Create(path, new XmlWriterSettings {Indent = true, CheckCharacters = true}))
			{
				new NetDataContractSerializer().WriteObject(writer, this);
			}
		}

		public static Mission FromFile(string path)
		{
			if (path.ToLower().EndsWith(".xml"))
			{
				using (var stream = File.OpenRead(path))
				{
					return (Mission) new NetDataContractSerializer().ReadObject(stream);
				}
			}
			else
			{
				var projectFileName = "project.mission.xml";
				using (var zip = ZipFile.Read(path))
				{
					var entry = zip.First(e => e.FileName == projectFileName);
					var memoryStream = new MemoryStream();
					entry.Extract(memoryStream);
					memoryStream.Position = 0;
					return (Mission)new NetDataContractSerializer().ReadObject(memoryStream);
				}
			}
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

				var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				zip.AddDirectory(Path.Combine(assemblyLocation, "MissionBase"));

				zip.AddEntry("modinfo.lua", textEncoding.GetBytes(GetModInfo()));
				zip.AddEntry("mission.lua", textEncoding.GetBytes(SerializeToLua()));
				zip.AddEntry("script.txt", textEncoding.GetBytes(GetScript()));
				zip.AddEntry("dependencies.txt", String.Join(";", Mod.Dependencies)); // FIXME

				var stream = new MemoryStream();
				using (var writer = XmlWriter.Create(stream, new XmlWriterSettings {Indent = true, CheckCharacters = true}))
				{
					new NetDataContractSerializer().WriteObject(writer, this);
				}
				stream.Position = 0;
				zip.AddEntry("project.mission.xml", stream);

				// disable scripts by hiding them with a blank file
				var blank = textEncoding.GetBytes("-- intentionally left blank --");
				foreach (var widget in disabledWidgets) zip.AddEntry("LuaUI/Widgets/" + widget, blank);
				foreach (var gadget in disabledGadgets) zip.AddEntry("LuaRules/Gadgets/" + gadget, blank);

				// include media in mod archive
				foreach (var item in AllLogic)
				{
					if (item is GuiMessageAction)
					{
						var action = (GuiMessageAction) item;
						if (!String.IsNullOrEmpty(action.ImagePath))
						{
							zip.AddFile(action.ImagePath, "LuaUI/Images/");
						}
					}
					else if (item is SoundAction)
					{
						var action = (SoundAction) item;
						if (!String.IsNullOrEmpty(action.SoundPath))
						{
							zip.AddFile(action.SoundPath, "LuaUI/Sounds/");
						}
					}
				}

				zip.Save(mutatorPath);
			}
		}
	}
}