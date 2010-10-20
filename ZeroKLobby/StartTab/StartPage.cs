using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ZeroKLobby.StartTab;

namespace ZeroKLobby.MicroLobby
{
	public partial class StartPage: UserControl, INavigatable
	{
		public static List<GameInfo> GameList =
			new List<GameInfo>
			{
				new GameInfo
				{
					Shortcut = "BA",
					FullName = "Balanced Annihilation",
					Channel = "ba",
					Regex = "Balanced Annihilation.*",
					Url = "http://springrts.com/phpbb/viewforum.php?f=44",
					RapidTag = "ba:latest",
					Image = "/Resources/GameLogos/ba.jpg",
					Description =
						"BA is the most popular game for the Spring engine and is designed for both 1v1 gameplay and large team games. BA is a mature mod and new versions feature only minor balance tweaks.",
				},
				new GameInfo
				{
					Shortcut = "CA",
					FullName = "Complete Annihilation",
					Channel = "ca",
					Regex = "Complete Annihilation.*",
					Url = "http://www.caspring.org",
					RapidTag = "ca:stable",
					Tutorial = "http://trac.caspring.org/wiki/NewbieGuide",
					Image = "/Resources/GameLogos/ca.jpg",
					Description =
						"Complete Annihilation is a regularly updated game that is often regarded as a showcase of what the Spring Engine can do. It's faster and has a focus on strategic decisions and unit tactics. Complete Annihilation is an open source project with democratic decision making.",
					Profiles =
						new List<SinglePlayerProfile>
						{
							new SinglePlayerProfile()
							{
								Name = "SandBox",
								ModTag = "ca:stable",
								MapName = "IsisDelta_v02",
								Description = "Enter a big sandbox to play in! Take your time to mess about.",
								Image = "/Resources/SinglePlayer/ca_sandbox.png",
								StartScript = SinglePlayerStartScripts.ca_sandbox
							},
							new SinglePlayerProfile()
							{
								Name = "Very easy chickens",
								ModTag = "ca:stable",
								MapName = "Chicken_Nuggets_v4",
								Description = "A small colony of chickens has set up and starts hatching. Take care of the plague!",
								Image = "/Resources/SinglePlayer/ca_chickenveryeasy.png",
								StartScript = SinglePlayerStartScripts.ca_chickenveryeasy
							},
							new SinglePlayerProfile()
							{
								Name = "Easy chickens",
								ModTag = "ca:stable",
								MapName = "Chicken_Farm_v02",
								Description = "Scans revealed chicken presence. Chase them away!",
								Image = "/Resources/SinglePlayer/ca_chickeneasy.png",
								StartScript = SinglePlayerStartScripts.ca_chickeneasy
							},
							new SinglePlayerProfile()
							{
								ModTag = "ca:stable",
								MapName = "Red Comet",
								Name = "2v1",
								Description = "Help your ally to defeat your common opponent, an enemy commander.",
								Image = "/Resources/SinglePlayer/ca_2v1.png",
								StartScript = SinglePlayerStartScripts.ca_2v1
							},
							new SinglePlayerProfile()
							{
								Name = "Normal chickens",
								ModTag = "ca:stable",
								MapName = "Chicken_Roast_v1",
								Description = "A planet close to the chicken's homeworld. Brace yourself!",
								Image = "/Resources/SinglePlayer/ca_chickennormal.png",
								StartScript = SinglePlayerStartScripts.ca_chickennormal
							},
							new SinglePlayerProfile()
							{
								ModTag = "ca:stable",
								MapName = "TitanDuel",
								Name = "1v1",
								Description = "Fight for your survival! Fair duel with enemy commander.",
								Image = "/Resources/SinglePlayer/ca_1v1.png",
								StartScript = SinglePlayerStartScripts.ca_1v1
							},
						}
				},
				new GameInfo
				{
					Shortcut = "NOTA",
					FullName = "NOTA",
					Channel = "nota",
					Regex = "NOTA.*",
					Url = "http://springrts.com/wiki/NOTA",
					RapidTag = "nota:latest",
					Image = "/Resources/GameLogos/nota.jpg",
					Description =
						"Not Original Total Annihilation is a rethink of the Total Annihilation universe with a focus on strategic, base oriented gameplay. With its revised unit scale, huge battles erupt engaging hundreds of units and scores of unit types.",
				},
				new GameInfo
				{
					Shortcut = "SA",
					FullName = "Supreme Annihilation",
					Channel = "sa",
					Regex = "Supreme Annihilation.*",
					Url = "http://springrts.com/phpbb/viewforum.php?f=49",
					RapidTag = "sa:latest",
					Image = "/Resources/GameLogos/sa.jpg",
					Description =
						"Supreme Annihilation is based on Total annhilation and has many of its features, including line bombing, fighters with high agility and a similar approach to game balance. Supreme Annihilation attempts to create a complete experience by replacing many of the old TA models, adding nice new visual effects and a musical score.",
				},
				new GameInfo
				{
					Shortcut = "S44",
					FullName = "Spring: 1944",
					Channel = "s44",
					Regex = "Spring: 1944.*",
					Url = "http://spring1944.net/",
					RapidTag = "s44:latest",
					Image = "/Resources/GameLogos/s44.png",
					Description =
						"Spring:1944 is a WWII themed game with four fully functional sides (US, Germany, USSR, Britain), period-accurate units and strengths. Realism comes second only to creating a game that is fun and accessible to play.",
				},
				new GameInfo
				{
					Shortcut = "Cursed",
					FullName = "The Cursed",
					Channel = "cursed",
					Regex = "The Cursed.*",
					Url = "http://azaremoth.supremedesign.org/index.php",
					RapidTag = "thecursed:latest",
					Image = "/Resources/GameLogos/cursed.jpg",
					Description = "This unique game is about bones, undead, demons and magic settled in a futuristic environment.",
				},
				new GameInfo
				{
					Shortcut = "XTA",
					FullName = "XTA",
					Channel = "xta",
					Regex = "XTA.*",
					Url = "http://www.evolutionrts.info/wordpress/",
					RapidTag = "xta:latest",
					Image = "/Resources/GameLogos/xta.jpg",
					Description =
						"Originally developed as a Total Annihilation mod by the Swedish Yankspankers, XTA is the Original Public Mod released with first versions of Spring. Development of this mod by the SYs has stopped a while ago, but it is still played and actively developed by the community.",
				},
				new GameInfo
				{
					Shortcut = "evo",
					FullName = "Evolution RTS",
					Channel = "evolution",
					Regex = "Evolution RTS.*",
					Url = "http://www.evolutionrts.info/wordpress/",
					Tutorial = "http://www.evolutionrts.info/game-manual",
					Image = "/Resources/GameLogos/evo.png",
					RapidTag = "evo:test",
					Description =
						"Boasting beautiful visuals, intense gameplay, and epic battles: A new war is brewing. A violent conflict, between the Six Colonies, each one convinced that it is in the right, each one sure of its own ability to defeat its enemies. But they need Generals. They need soldiers. They need you.",
				},
				new GameInfo
				{
					Shortcut = "KP",
					FullName = "Kernel Panic",
					Channel = "kp",
					Regex = "Kernel.*Panic.*",
					Url = "http://www.moddb.com/games/kernel-panic/",
					RapidTag = "kp:stable",
					Tutorial = "http://springrts.com/wiki/Kernel_Panic",
					Image = "/Resources/GameLogos/kp.png",
					Description =
						"Kernel Panic is a game about computers. Systems, Hackers and Networks wage war in a matrix of DOOM! The only constraints are time and space; unlike other real time strategy games, no resource economy exists in KP.",
					Profiles =
						new List<SinglePlayerProfile>()
						{
							new SinglePlayerProfile()
							{
								MapName = "Central Hub",
								ModTag = "kp:latest",
								StartScript = SinglePlayerStartScripts.kp_script,
								ManualDependencies =
									new List<string>()
									{
										"Marble_Madness_Map",
										"Major_Madness3.0",
										"Direct Memory Access 0.5c (beta)",
										"Direct Memory Access 0.5e (beta)",
										"Spooler Buffer 0.5 (beta)",
										"DigitalDivide_PT2",
										"Data Cache L1",
										"Speed_Balls_16_Way",
										"Palladium 0.5 (beta)",
										"Central Hub",
										"Corrupted Core",
										"Dual Core",
										"Quad Core",
									}
							}
						}
				},
			}.OrderBy(g => g.FullName).ToList();


		public StartPage()
		{
			InitializeComponent();
			var isDesigner = Process.GetCurrentProcess().ProcessName == "devenv";
			if (isDesigner) return;
			var host = new ElementHost();
			host.Child = new UcStartTab();
			host.Dock = DockStyle.Fill;
			Controls.Add(host);
		}

		public bool TryNavigate(string pathHead, params string[] pathTail)
		{
			return pathHead == "start";
		}
	}

	public class SinglePlayerProfile
	{
		public string Description { get; set; }
		public string Image { get; set; }
		public List<string> ManualDependencies = new List<string>();
		public string MapName { get; set; }
		public string ModTag { get; set; }
		public string Name { get; set; }
		public string StartScript { get; set; }
	}
}