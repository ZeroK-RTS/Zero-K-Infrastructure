using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SpringDownloader.MicroLobby
{
    public partial class StartPage: UserControl
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
										Image = "/Resources/GameLogos/ca.jpg",
                    Description =
                        "Complete Annihilation is a regularly updated game that is often regarded as a showcase of what the Spring Engine can do. The game is designed with a flat technology tree, where the effectiveness of a unit is based on its appropriateness to the situation rather than lighter units being superseded by heavier ones.",
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
										Image = "/Resources/GameLogos/kp.png",
                    Description =
                        "Kernel Panic is a game about computers. Systems, Hackers and Networks wage war in a matrix of DOOM! The only constraints are time and space; unlike other real time strategy games, no resource economy exists in KP.",
                },

            }.OrderBy(g => g.FullName).ToList();

        public StartPage()
        {
            InitializeComponent();
            var selector = new GameSelector(GameList) { Dock = DockStyle.Fill };
            Controls.Add(selector);
        }
    }
}