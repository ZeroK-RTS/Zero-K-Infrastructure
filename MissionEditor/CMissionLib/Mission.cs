using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;
using CMissionLib.Actions;
using CMissionLib.Conditions;
using Ionic.Zip;
using Microsoft.SqlServer.Server;
using MissionEditor2;
using ZkData.UnitSyncLib;
using CMissionLib.UnitSyncLib;
using ZkData;
using Map = CMissionLib.UnitSyncLib.Map;
using Mod = CMissionLib.UnitSyncLib.Mod;

namespace CMissionLib
{
	[DataContract]
	public class Mission: PropertyChanged
	{
		string author = "Default Author";
		string contentFolderPath;
		ObservableCollection<string> counters = new ObservableCollection<string>();
        string customModOptions = String.Empty;
		string description = String.Empty;
        string descriptionStory = String.Empty;
		ObservableCollection<string> disabledGadgets = new ObservableCollection<string>();
		ObservableCollection<string> disabledUnits = new ObservableCollection<string>();
		ObservableCollection<string> disabledWidgets = new ObservableCollection<string>();
		Dictionary<string, string> folders = new Dictionary<string, string>();
		string imagePath;
		CompositeObservableCollection<Trigger, Region> items;
		Map map;
		string mapName;
		int maxUnits = 5000;
		Mod mod;
		string modName;
        Dictionary<string, string> modOptions;
		string name;
		ObservableCollection<Player> players = new ObservableCollection<Player>();
		string rapidTag = "zk:stable";
		ObservableCollection<Region> regions = new ObservableCollection<Region>();
		string scoringMethod = "";
		int startingEnergy = 1000;
		int startingMetal = 1000;
		Player startingPlayer = new Player { Name = "Player 1", Color = Colors.Blue, Alliance = "1", IsHuman = true };
		ObservableCollection<Trigger> triggers = new ObservableCollection<Trigger>();
        bool modifiedSinceLastSave = false;	// FIXME: is never true

		public IEnumerable<string> AllGroups
		{
			get
			{
				var groups = new List<string>();
				groups.AddRange(AllUnits.SelectMany(u => u.Groups));
				groups.AddRange(AllLogic.OfType<GiveFactoryOrdersAction>().SelectMany(a => a.BuiltUnitsGroups));
				foreach (var player in players) groups.Add("Latest Factory Built Unit (" + player.Name + ")");
                foreach (var player in players) groups.Add("Any Unit (" + player.Name + ")");
				foreach (var region in Regions)
				{
					foreach (var player in players) groups.Add(string.Format("Units in {0} ({1})", region.Name, player.Name));
				}
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
        public IEnumerable<string> Objectives { get { return AllLogic.OfType<AddObjectiveAction>().Select(u => u.ID).Distinct(); } }
        public IEnumerable<string> Cutscenes 
        { 
            get 
            { 
                var ret = AllLogic.OfType<EnterCutsceneAction>().Where(x => !String.IsNullOrEmpty(x.ID)).Select(u => u.ID).Distinct().ToList();
                ret.Add("Current Cutscene");
                ret.Add("Any Cutscene");
                return ret;
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
        [DataMember]
        public string CustomModOptions
        {
            get { return customModOptions; }
            set
            {
                customModOptions = value;
                RaisePropertyChanged("CustomModOptions");
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
        public string DescriptionStory
        {
            get { return descriptionStory; }
            set
            {
                descriptionStory = value;
                RaisePropertyChanged("DescriptionStory");
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
		public CompositeObservableCollection<Trigger, Region> Items
		{
			get { return items; }
			set
			{
				items = value;
				RaisePropertyChanged("Items");
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
                /*ModOptions = new Dictionary<string, string>();
                var options = mod.Options;
                foreach (CMissionLib.UnitSyncLib.Option option in options)
                {
                    if (option.Type != CMissionLib.UnitSyncLib.OptionType.Section) ModOptions.Add(option.Key, option.Name);
                }
                */
                RaisePropertyChanged("Mod"); 
			}
		}
		[DataMember]
		public string ModName { get { return modName; } set { modName = value; } }
        //[DataMember]
        //public Dictionary<string, string> ModOptions { get { return modOptions; } set { modOptions = value; } }
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
		public Player StartingPlayer
		{
			get { return startingPlayer; } 
			set
			{
				startingPlayer = value;
				RaisePropertyChanged("StartingPlayer");
			}
		}

		public IEnumerable<string> TriggerNames { get { return triggers.Cast<INamed>().Select(t => t.Name); } }
		[DataMember]
		public ObservableCollection<Trigger> Triggers { get { return triggers; } set { triggers = value; } }

        public bool ModifiedSinceLastSave { get { return modifiedSinceLastSave; } set { modifiedSinceLastSave = value; } }

		public Mission(string name, Mod game, Map map)
		{
			Mod = game;
			Map = map;
			Name = name;
			ModName = game.Name;
			MapName = map.Name;
			var testAI = game.AllAis.FirstOrDefault(ai => ai.ShortName.Contains("NullAI"));
			var player1 = new Player { Name = "Player 1", Color = Colors.Blue, Alliance = "Alliance 1", IsHuman = true, IsRequired = true };
			var player2 = new Player { Name = "Player 2", Color = Colors.Red, Alliance = "Alliance 2", IsHuman = false, };
			StartingPlayer = player1;
			Players.Add(player1);
			Players.Add(player2);
			Regions.Add(new Region { Name = "Region 1" });

            var gamePreloadTrigger = new Trigger();
			Triggers.Add(gamePreloadTrigger);
			gamePreloadTrigger.Logic.Add(new GamePreloadCondition());
            gamePreloadTrigger.Name = "Initialization";

            var gameStartTrigger = new Trigger();
            Triggers.Add(gameStartTrigger);
            gameStartTrigger.Logic.Add(new GameStartedCondition());
            gameStartTrigger.Name = "Game Start";
			var unitType = game.UnitDefs.First();
			var startUnits = new UnitStartInfo[]  {};
			gamePreloadTrigger.Logic.Add(new CreateUnitsAction(startUnits));

            var widgets = new[] { "gui_pauseScreen.lua", "cmd_unit_mover.lua", "init_startup_info_selector.lua", "gui_center_n_select.lua", "gui_take_remind.lua", "gui_startup_info_selector.lua", "gui_local_colors.lua", "spring_direct_launch.lua" };
			foreach (var widget in widgets) DisabledWidgets.Add(widget);
			var gadgets = new string[] { "game_over.lua", "game_end.lua", "awards.lua" };
			foreach (var gadget in gadgets) DisabledGadgets.Add(gadget);
			if (game.Name.Contains("Zero-K")) RapidTag = "zk:stable";
			Items = new CompositeObservableCollection<Trigger, Region>(Triggers, Regions);
		}

		public void CreateArchive(string mutatorPath, bool hideFromModList = false)
		{
			var script = GetScript();
			var modInfo = GetModInfo(hideFromModList);
			var luaMissionData = SerializeToLua();
			if (Debugger.IsAttached)
			{
				File.WriteAllText("startscript.txt", script);
				File.WriteAllText("modinfo.txt", modInfo);
				//File.WriteAllText("mission.lua", luaMissionData);
			}
			var textEncoding = Encoding.GetEncoding("iso-8859-1"); // ASCIIEncoding()
			using (var zip = new ZipFile())
			{
				if (!String.IsNullOrEmpty(ContentFolderPath) && Directory.Exists(ContentFolderPath)) zip.SafeAddDirectory(ContentFolderPath);

				var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var basePath = Path.Combine(assemblyLocation, "MissionBase");
				zip.SafeAddDirectory(basePath);

				zip.SafeAddEntry("modinfo.lua", textEncoding.GetBytes(modInfo));
				zip.SafeAddEntry("mission.lua", textEncoding.GetBytes(luaMissionData));
				zip.SafeAddEntry("script.txt", textEncoding.GetBytes(script));
				zip.SafeAddEntry(GlobalConst.MissionScriptFileName, textEncoding.GetBytes(script));
				zip.SafeAddEntry("slots.lua", textEncoding.GetBytes(GetLuaSlots().ToString()));
				zip.SafeAddEntry("dependencies.txt", String.Join(";", Mod.Dependencies)); // FIXME

				{
					var stream = new MemoryStream();
					using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, CheckCharacters = true })) new NetDataContractSerializer().WriteObject(writer, this);
					stream.Position = 0;
					zip.SafeAddEntry("project.mission.xml", stream);
				}

				{
					var serializer = new XmlSerializer(typeof(List<MissionSlot>));
					var stream = new MemoryStream();
					serializer.Serialize(stream, GetSlots());
					stream.Position = 0;
					zip.SafeAddEntry(GlobalConst.MissionSlotsFileName, stream);
				}

				// disable scripts by hiding them with a blank file
				var blank = textEncoding.GetBytes("-- intentionally left blank --");
				foreach (var widget in disabledWidgets.Distinct()) zip.SafeAddEntry("LuaUI/Widgets/" + widget, blank);
				foreach (var gadget in disabledGadgets.Distinct()) zip.SafeAddEntry("LuaRules/Gadgets/" + gadget, blank);

				// include media in mod archive
				foreach (var item in AllLogic)
				{
                    // note we want a silent failure if file not found (with the new read-from-archive system it often won't fail anyway)
					if (item is GuiMessageAction)
					{
						var action = (GuiMessageAction)item;
						if (!String.IsNullOrEmpty(action.ImagePath))
						{
							if (File.Exists(action.ImagePath)) zip.SafeAddFile(action.ImagePath, "LuaUI/Images/");
						}
					}
                    else if (item is GuiMessagePersistentAction)
                    {
                        var action = (GuiMessagePersistentAction)item;
                        if (!String.IsNullOrEmpty(action.ImagePath))
                        {
                            if (File.Exists(action.ImagePath)) zip.SafeAddFile(action.ImagePath, "LuaUI/Images/");
                        }
                    }
                    else if (item is ConvoMessageAction)
                    {
                        var action = (ConvoMessageAction)item;
                        if (!String.IsNullOrEmpty(action.ImagePath))
                        {
                            if (File.Exists(action.ImagePath)) zip.SafeAddFile(action.ImagePath, "LuaUI/Images/");
                        }
                        if (!String.IsNullOrEmpty(action.SoundPath) && File.Exists(action.SoundPath))
                        {
                            if (File.Exists(action.SoundPath)) zip.SafeAddFile(action.SoundPath, "LuaUI/Sounds/convo");
                        }
                    }
                    else if (item is SoundAction)
                    {
                        var action = (SoundAction)item;
                        if (!String.IsNullOrEmpty(action.SoundPath) && File.Exists(action.SoundPath))
                        {
                            if (File.Exists(action.SoundPath)) zip.SafeAddFile(action.SoundPath, "LuaUI/Sounds/");
                        }
                    }
                    else if (item is MusicAction)
                    {
                        var action = (MusicAction)item;
                        if (!String.IsNullOrEmpty(action.TrackPath) && File.Exists(action.TrackPath))
                        {
                            zip.SafeAddFile(action.TrackPath, "LuaUI/Sounds/music/");
                        }
                    }
                    else if (item is MusicLoopAction)
                    {
                        var action = (MusicLoopAction)item;
                        if (!String.IsNullOrEmpty(action.TrackIntroPath) && File.Exists(action.TrackIntroPath))
                        {
                            zip.SafeAddFile(action.TrackIntroPath, "LuaUI/Sounds/music/");
                        }
                        if (!String.IsNullOrEmpty(action.TrackLoopPath) && File.Exists(action.TrackLoopPath))
                        {
                            zip.SafeAddFile(action.TrackLoopPath, "LuaUI/Sounds/music/");
                        }
                    }
				}
                string directory = Path.GetDirectoryName(mutatorPath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

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
			return x*Map.Texture.Width/Map.Size.Width;
		}

		public double FromIngameY(double y)
		{
			return y*Map.Texture.Height/Map.Size.Height;
		}

		public LuaTable GetLuaSlots()
		{
			var alliances = Players.Select(p => p.Alliance).Distinct().ToList();
			var slots = new List<LuaTable>();
			foreach (var player in Players)
			{
				var map = new Dictionary<object, object>
				          {
				          	{ "AllyID", alliances.IndexOf(player.Alliance) },
				          	{ "AllyName", player.Alliance },
				          	{ "IsHuman", player.IsHuman },
				          	{ "IsRequired", player.IsRequired },
				          	{ "TeamID", Players.IndexOf(player) },
				          	{ "TeamName", player.Name },
				          	{ "Color", (int)(MyCol)player.Color },
				          	{ "ColorR", player.Color.R },
				          	{ "ColorG", player.Color.G },
				          	{ "ColorB", player.Color.B },
				          };
				if (player.AIDll != null) map.Add("AiShortName", player.AIDll);
				if (player.AIVersion != null) map.Add("AiVersion", player.AIVersion);
				slots.Add(new LuaTable(map));
			}
			return LuaTable.CreateArray(slots);
		}

		public string GetScript()
		{
			var sb = new StringBuilder();
			var allianceCount = Players.Select(p => p.Alliance).Distinct().Count();
			sb.AppendFormat("[GAME]\n");
			sb.AppendLine("{");
			Action<string, object> line = (key, value) => sb.AppendFormat("\t{0}={1};\n", key, value);
			line("MapName", MapName);
			line("StartposType", "2");
			line("GameType", Name);
			//line("HostIP", "127.0.0.1");
			//line("HostPort", "8452");
			line("IsHost", "1");
            line("OnlyLocal", "1");
			//line("MyPlayerNum", Players.IndexOf(StartingPlayer));
			line("MyPlayerName", StartingPlayer.Name.Replace(' ', '_'));

            sb.AppendLine("\t[MODOPTIONS]");
            sb.AppendLine("\t{");

            if (customModOptions != null)
            {
                List<string> modopts = new List<string>(customModOptions.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries));
                foreach (string modopt in modopts) sb.Append("\t\t" + modopt + ";");
            }
            /*
            // put standard modoptions to options dictionary
            var options = new Dictionary<string, string>();
            foreach (var o in mod.Options.Where(x => x.Type != CMissionLib.UnitSyncLib.OptionType.Section))
            {
                var v = o.Default;
                options[o.Key] = v;
            }

            // write final options to script
            foreach (var kvp in options) line(kvp.Key, kvp.Value);
            */
            sb.AppendLine("\n\t}");

			foreach (var player in Players) WritePlayer(sb, player);
			foreach (var player in Players) WriteTeam(sb, player);
			for (var i = 0; i < allianceCount; i++) WriteAllyTeam(sb, i);
			sb.AppendLine("}");
			return sb.ToString();
		}

		public List<MissionSlot> GetSlots()
		{
			var alliances = Players.Select(p => p.Alliance).Distinct().ToList();
			var slots = new List<MissionSlot>();
			foreach (var player in Players)
			{
				var missionSlot = new MissionSlot
				                  {
				                  	AiShortName = player.AIDll,
				                  	AiVersion = player.AIVersion,
				                  	AllyID = alliances.IndexOf(player.Alliance),
				                  	AllyName = player.Alliance,
				                  	IsHuman = player.IsHuman,
				                  	IsRequired = player.IsRequired,
				                  	TeamID = Players.IndexOf(player),
				                  	TeamName = player.Name,
				                  	Color = (int)(MyCol)player.Color
				                  };
				slots.Add(missionSlot);
			}
			return slots;
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
                //if (unitDef == null) unitDef = Mod.UnitDefs.FirstOrDefault(ud => unit.UnitDef.FullName == ud.FullName);   // this doesn't work - unit.UnitDef will be null
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

            foreach (Trigger trigger in Triggers)
            {
                foreach (Action action in trigger.Actions)
                {
                    if (action is LockUnitsAction && ((LockUnitsAction)action).Players == null)
                    {
                        ((LockUnitsAction)action).Players = new ObservableCollection<Player>();
                    }
                    else if (action is UnlockUnitsAction && ((UnlockUnitsAction)action).Players == null)
                    {
                        ((UnlockUnitsAction)action).Players = new ObservableCollection<Player>();
                    }
                }
            }

			// get rid of legacy dummies
			foreach (var trigger in triggers) foreach (var item in trigger.Logic.ToArray()) if (item is DummyAction || item is DummyCondition) trigger.Logic.Remove(item);
			Items = new CompositeObservableCollection<Trigger, Region>(Triggers, Regions);
		}

		public bool SaveToXmlFile(string path, bool auto = false)
		{
            bool success = false;
			var backupPath = path + ".backup.mission.xml";
			if (File.Exists(path)) File.Copy(path, backupPath, true);
			using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true, CheckCharacters = true })) new NetDataContractSerializer().WriteObject(writer, this);
            try
            {
                if (File.Exists(backupPath)) File.Delete(backupPath);
                if (!auto) modifiedSinceLastSave = false;
                success = true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return success;
		}

		public string SerializeToLua()
		{
			var luaTable = GetLuaTable();
			return "return " + luaTable;
		}

		public double ToIngameX(double x)
		{
			return x/Map.Texture.Width*Map.Size.Width;
		}

		public double ToIngameY(double y)
		{
			return y/Map.Texture.Height*Map.Size.Height;
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
			             	{ "map", Map.Name },
			             	{ "players", LuaTable.CreateArray(players.Select(p => p.Name)) },
			             	{ "triggers", LuaTable.CreateArray(triggers.Select(t => t.GetLuaMap(this))) },
			             	{ "startPlayer", Players.IndexOf(StartingPlayer) },
			             	{ "disabledUnits", LuaTable.CreateArray(DisabledUnits) },
			             	{ "scoringMethod", scoringMethod },
			             	{ "counters", LuaTable.CreateArray(Counters) },
							{ "regions", LuaTable.CreateArray(Regions.Select(r => r.GetLuaTable(this)))}
			             };
			if (Debugger.IsAttached) luaMap["debug"] = true;
			return new LuaTable(luaMap);
		}

		string GetModInfo(bool hideFromModList)
		{
			var sb = new StringBuilder();
			sb.AppendLine("local modinfo = {");
			sb.AppendFormat("  name          =	[[{0}]],\n", Name);
			sb.AppendFormat("  description   =	[[{0}]],\n", "Mission Mutator"); // the real description might break archivecache.lua
			sb.AppendFormat("  modtype       =	[[{0}]],\n", hideFromModList ? 0 : 1);
			sb.AppendFormat("  shortname     =	[[{0}]],\n", mod.ShortName);
			sb.AppendFormat("  shortgame     =	[[{0}]],\n", mod.ShortGame);
			sb.AppendFormat("  shortbasename =	[[{0}]],\n", mod.ShortBaseName);
			sb.AppendLine("  depend = {");
			sb.AppendFormat("    [[{0}]]\n", Mod.Name);
			sb.AppendLine("  },");
			sb.AppendLine("}");
			sb.AppendLine("return modinfo");
			return sb.ToString();
		}

		void WriteAllyTeam(StringBuilder sb, int index)
		{
			sb.AppendFormat("\t[ALLYTEAM{0}]\n", index);
			sb.AppendLine("\t{");
			sb.AppendFormat("\t\tNumAllies=0;\n"); // it seems that NumAllies has no effect
            sb.AppendFormat("\t\tStartRectTop=0;");
            sb.AppendFormat("\t\tStartRectBottom=0;");
            sb.AppendFormat("\t\tStartRectLeft=1;");
            sb.AppendFormat("\t\tStartRectRight=1;");
			sb.AppendLine("\t}");
		}

		void WritePlayer(StringBuilder sb, Player player)
		{
			if (player.IsHuman)
			{
                var index = Players.Where(x => x.IsHuman).ToList().IndexOf(player);
                var teamIndex = Players.IndexOf(player);
				sb.AppendFormat("\t[PLAYER" + index + "]\n");
				sb.AppendLine("\t{");
				sb.AppendFormat("\t\tName={0};\n", player.Name.Replace(' ', '_'));
				//sb.AppendFormat("\t\tSpectator=0;\n");
                sb.AppendFormat("\t\tTeam={0};\n", teamIndex);
				sb.AppendLine("\t}");
			}
			else
			{
				var index = Players.Where(x=> !x.IsHuman).ToList().IndexOf(player);
                var teamIndex = Players.IndexOf(player);
				sb.AppendFormat("\t[AI" + index + "]\n");
				sb.AppendLine("\t{");
				sb.AppendFormat("\t\tName={0};\n", player.Name.Replace(' ', '_'));
				sb.AppendFormat("\t\tShortName={0};\n", player.AIDll);
				// sb.AppendFormat("\t\tVersion={0};\n", String.IsNullOrEmpty(player.AIVersion) ? "0.1" : player.AIVersion);
                sb.AppendFormat("\t\tTeam={0};\n", teamIndex);
				sb.AppendFormat("\t\tHost={0};\n", Players.IndexOf(StartingPlayer));
				//sb.AppendFormat("\t\t[Options] {{}}\n");
				sb.AppendLine("\t}");
			}
		}

		void WriteTeam(StringBuilder sb, Player player) // no commshares for now
		{
			var index = Players.IndexOf(player);
			var alliances = Players.Select(p => p.Alliance).Distinct().ToList();
			sb.AppendFormat("\t[TEAM{0}]\n", index);
			sb.AppendLine("\t{");
			sb.AppendFormat("\t\tTeamLeader={0};\n", Players.IndexOf(StartingPlayer)); // todo: verify if correct
			sb.AppendFormat("\t\tAllyTeam={0};\n", alliances.IndexOf(player.Alliance));
			sb.AppendFormat("\t\tRGBColor={0} {1} {2};\n", player.Color.ScR, player.Color.ScG, player.Color.ScB); // range: 0-1
			//sb.AppendFormat("\t\tSide={0};\n", Mod.Sides.First());
			sb.AppendLine("\t}");
		}

        
        public void CopyTrigger(Trigger source)
        {
            /*
            MemoryStream stream = new MemoryStream();
            System.Runtime.Serialization.Formatters.Binary.BinaryFormatter format = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            format.Serialize(stream, source);
            Trigger copy = (CMissionLib.Trigger)format.Deserialize(stream);
            copy.Name = copy.Name + " (Copy)";
            Triggers.Add(copy);
            RaisePropertyChanged(String.Empty);
            */
            string path = Path.GetTempFileName();
            using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true, CheckCharacters = true })) new NetDataContractSerializer().WriteObject(writer, source);
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    Trigger copy = (Trigger)new NetDataContractSerializer().ReadObject(stream);
                    copy.Name = copy.Name + " (Copy)";

                    // reconstruct object references
                    foreach (CreateUnitsAction action in copy.Actions.Where(x => x.GetType() == typeof(CreateUnitsAction)))
                    {
                        foreach (UnitStartInfo unit in action.Units)
                        {
                            unit.Player = Players.First(x => x.Name == unit.Player.Name);
                            unit.UnitDef = Mod.UnitDefs.First(x => x.Name == unit.UnitDefName);
                        }
                    }
                    foreach (UnitsAreInAreaCondition condition in copy.Conditions.Where(x => x.GetType() == typeof(UnitsAreInAreaCondition)))
                    {
                        var players = condition.Players.Select(x => Players.First(p => p.Name == x.Name));
                        condition.Players = new ObservableCollection<Player>(players);
                    }
                    foreach (UnitCreatedCondition condition in copy.Conditions.Where(x => x.GetType() == typeof(UnitCreatedCondition)))
                    {
                        var players = condition.Players.Select(x => Players.First(p => p.Name == x.Name));
                        condition.Players = new ObservableCollection<Player>(players);
                    }
                    foreach (UnitFinishedCondition condition in copy.Conditions.Where(x => x.GetType() == typeof(UnitFinishedCondition)))
                    {
                        var players = condition.Players.Select(x => Players.First(p => p.Name == x.Name));
                        condition.Players = new ObservableCollection<Player>(players);
                    }
                    foreach (PlayerDiedCondition condition in copy.Conditions.Where(x => x.GetType() == typeof(PlayerDiedCondition)))
                    {
                        condition.Player = Players.First(p => p.Name == condition.Player.Name);
                    }
                    foreach (PlayerJoinedCondition condition in copy.Conditions.Where(x => x.GetType() == typeof(PlayerJoinedCondition)))
                    {
                        condition.Player = Players.First(p => p.Name == condition.Player.Name);
                    }
                    foreach (LockUnitsAction action in copy.Actions.Where(x => x.GetType() == typeof(LockUnitsAction)))
                    {
                        var players = action.Players.Select(x => Players.First(p => p.Name == x.Name));
                        action.Players = new ObservableCollection<Player>(players);
                    }
                    foreach (UnlockUnitsAction action in copy.Actions.Where(x => x.GetType() == typeof(UnlockUnitsAction)))
                    {
                        var players = action.Players.Select(x => Players.First(p => p.Name == x.Name));
                        action.Players = new ObservableCollection<Player>(players);
                    }

                    Triggers.Add(copy);
                    RaisePropertyChanged(String.Empty);
                }
                if (File.Exists(path)) File.Delete(path);
            }
            catch(Exception ex) {
                throw ex;
            }
        }

        // TODO
        public void CopyAction(Action source)
        {
            string path = Path.GetTempFileName();
            using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true, CheckCharacters = true })) new NetDataContractSerializer().WriteObject(writer, source);
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    dynamic copy = (Action)new NetDataContractSerializer().ReadObject(stream);
                    // reconstruct object references
                    if (source is LockUnitsAction)
                    {
                        var players = ((LockUnitsAction)(source)).Players.Select(x => Players.First(p => p.Name == x.Name));
                        copy.Players = new ObservableCollection<Player>(players);
                    }
                    else if (source is UnlockUnitsAction)
                    {
                        var players = ((UnlockUnitsAction)(source)).Players.Select(x => Players.First(p => p.Name == x.Name));
                        copy.Players = new ObservableCollection<Player>(players);
                    }

                    RaisePropertyChanged(String.Empty);
                }
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void CopyCondition(Condition source)
        {
        }
	}
}