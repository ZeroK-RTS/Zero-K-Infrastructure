using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace ZeroKLobby.VoiceCommand
{
	/* grammar for ordering the construction of mobile units in factories
	 * examples:
	 * - build a bandit
	 * - make 20 drones and repeat
	 * - build one weaver now
	 */


	class BuildUnitGrammar : ZkGrammar
	{
		const int MAX_ORDERS = 50;


		// todo: don't hardcode
		Dictionary<string, string> unitNames = new Dictionary<string, string>
		                                   {
		                                   	{ "spherecloaker", "Eraser" },
		                                   	{ "arm_spider", "Weaver" },
		                                   	{ "cormart", "Pillager" },
		                                   	{ "corcrw", "Krow" },
		                                   	{ "armflea", "Flea" },
		                                   	{ "chicken_pigeon", "Pigeon" },
		                                   	{ "blastwing", "Blastwing" },
		                                   	{ "gorg", "Jugglenaut" },
		                                   	{ "chickenlandqueen", "Chicken Queen" },
		                                   	{ "correap", "Reaper" },
		                                   	{ "armcom1", "Strike Commander" },
		                                   	{ "chicken_listener", "Listener" },
		                                   	{ "corcom1", "Battle Commander" },
		                                   	{ "chicken_drone_starter", "Drone" },
		                                   	{ "armtick", "Tick" },
		                                   	{ "cormak", "Outlaw" },
		                                   	{ "armroy", "Crusader" },
		                                   	{ "chickenc", "Basilisk" },
		                                   	{ "corsh", "Scrubber" },
		                                   	{ "armrock", "Rocko" },
		                                   	{ "chicken_listener_b", "Listener (burrowed)" },
		                                   	{ "corsub", "Snake" },
		                                   	{ "chicken_leaper", "Leaper" },
		                                   	{ "shieldarty", "Racketeer" },
		                                   	{ "chickenr", "Lobber" },
		                                   	{ "armpw", "Glaive" },
		                                   	{ "commrecon1", "Recon Commander" },
		                                   	{ "hoverminer", "Dampener" },
		                                   	{ "corgol", "Goliath" },
		                                   	{ "corch", "Hovercon" },
		                                   	{ "armorco", "Detriment" },
		                                   	{ "chicken_roc", "Roc" },
		                                   	{ "corfast", "Freaker" },
		                                   	{ "chicken_blimpy", "Blimpy" },
		                                   	{ "chicken_drone", "Drone" },
		                                   	{ "corpyro", "Pyro" },
		                                   	{ "arm_venom", "Venom" },
		                                   	{ "cornukesub", "Leviathan" },
		                                   	{ "armpb", "Pit Bull" },
		                                   	{ "chicken_digger_b", "Digger (burrowed)" },
		                                   	{ "corfav", "Dart" },
		                                   	{ "hoveraa", "Flail" },
		                                   	{ "armham", "Hammer" },
		                                   	{ "tiptest", "TIP test unit" },
		                                   	{ "armsptk", "Recluse" },
		                                   	{ "blackdawn", "Black Dawn" },
		                                   	{ "amphskirm", "Duck" },
		                                   	{ "puppy", "Puppy" },
		                                   	{ "fakeunit_aatarget", "Fake AA target" },
		                                   	{ "chickenflyerqueen", "Chicken Flyer Queen" },
		                                   	{ "tawf114", "Banisher" },
		                                   	{ "spideraa", "Tarantula" },
		                                   	{ "armamd", "Protector" },
		                                   	{ "striderhub", "Strider Hub" },
		                                   	{ "armcrabe", "Crabe" },
		                                   	{ "spiderassault", "Cudgel" },
		                                   	{ "subscout", "Lancelet" },
		                                   	{ "armrectr", "Rector" },
		                                   	{ "spherepole", "Scythe" },
		                                   	{ "armsnipe", "Sharpshooter" },
		                                   	{ "shieldfelon", "Felon" },
		                                   	{ "serpent", "Serpent" },
		                                   	{ "corclog", "Dirtbag" },
		                                   	{ "amphaa", "Angler" },
		                                   	{ "corsent", "Copperhead" },
		                                   	{ "armkam", "Banshee" },
		                                   	{ "scorpion", "Scorpion" },
		                                   	{ "corgator", "Scorcher" },
		                                   	{ "armnanotc", "Caretaker" },
		                                   	{ "pw_generic", "Generic Neutral Structure" },
		                                   	{ "corlevlr", "Leveler" },
		                                   	{ "funnelweb", "Funnelweb" },
		                                   	{ "panther", "Panther" },
		                                   	{ "hoverassault", "Halberd" },
		                                   	{ "armwar", "Warrior" },
		                                   	{ "cafus", "Singularity Reactor" },
		                                   	{ "corbats", "Warlord" },
		                                   	{ "chicken_spidermonkey", "Spidermonkey" },
		                                   	{ "chickens", "Spiker" },
		                                   	{ "firewalker", "Firewalker" },
		                                   	{ "neebcomm", "Neeb Comm" },
		                                   	{ "logkoda", "Kodachi" },
		                                   	{ "armtboat", "Surfboard" },
		                                   	{ "attackdrone", "Firefly" },
		                                   	{ "dante", "Dante" },
		                                   	{ "hoverriot", "Mace" },
		                                   	{ "chickenq", "Chicken Queen" },
		                                   	{ "chickenblobber", "Blobber" },
		                                   	{ "armjeth", "Jethro" },
		                                   	{ "firebug", "firebug" },
		                                   	{ "fighter", "Avenger" },
		                                   	{ "armmerl", "Merl" },
		                                   	{ "bladew", "Gnat" },
		                                   	{ "chickenf", "Talon" },
		                                   	{ "corstorm", "Rogue" },
		                                   	{ "corshad", "Shadow" },
		                                   	{ "chicken_shield", "Toad" },
		                                   	{ "chicken_dodo", "Dodo" },
		                                   	{ "armcybr", "Licho" },
		                                   	{ "corarch", "Shredder" },
		                                   	{ "chickenwurm", "Wurm" },
		                                   	{ "corroy", "Enforcer" },
		                                   	{ "cremcom1", "Strike Commander" },
		                                   	{ "amphcon", "Clam" },
		                                   	{ "corvamp", "Vamp" },
		                                   	{ "armzeus", "Zeus" },
		                                   	{ "corvalk", "Valkyrie" },
		                                   	{ "corsumo", "Sumo" },
		                                   	{ "armbrawl", "Brawler" },
		                                   	{ "corthud", "Thug" },
		                                   	{ "corned", "Mason" },
		                                   	{ "corape", "Rapier" },
		                                   	{ "armraz", "Razorback" },
		                                   	{ "chickena", "Cockatrice" },
		                                   	{ "dclship", "Hunter" },
		                                   	{ "corroach", "Roach" },
		                                   	{ "nsaclash", "Scalpel" },
		                                   	{ "corraid", "Ravager" },
		                                   	{ "armpt", "Skeeter" },
		                                   	{ "corsktl", "Skuttle" },
		                                   	{ "armcarry", "Reef" },
		                                   	{ "slowmort", "Moderator" },
		                                   	{ "chicken", "Chicken" },
		                                   	{ "cornecro", "Necro" },
		                                   	{ "capturecar", "Dominatrix" },
		                                   	{ "armbanth", "Bantha" },
		                                   	{ "chicken_sporeshooter", "Sporeshooter" },
		                                   	{ "chicken_digger", "Digger" },
		                                   	{ "coracv", "Welder" },
		                                   	{ "carrydrone", "Gull" },
		                                   	{ "corhurc2", "Firestorm" },
		                                   	{ "corsilo", "Silencer" },
		                                   	{ "hovershotgun", "Punisher" },
		                                   	{ "armspy", "Infiltrator" },
		                                   	{ "armstiletto_laser", "Stiletto" },
		                                   	{ "core_spectre", "Aspis" },
		                                   	{ "chicken_dragon", "White Dragon" },
		                                   	{ "trem", "Tremor" },
		                                   	{ "corvrad", "Informant" },
		                                   	{ "commsupport1", "Support Commander" },
		                                   	{ "chickenbroodqueen", "Chicken Brood Queen" },
		                                   	{ "coresupp", "Typhoon" },
		                                   	{ "armca", "Crane" },
		                                   	{ "corcrash", "Vandal" },
		                                   	{ "corak", "Bandit" },
		                                   	{ "corgarp", "Wolverine" },
		                                   	{ "armcomdgun", "Ultimatum" },
		                                   	{ "armaak", "Archangel" },
		                                   	{ "corcan", "Jack" },
		                                   	{ "armcsa", "Athena" },
		                                   	{ "cormist", "Slasher" },
		                                   	{ "amphraider", "Grebe" },
		                                   	{ "chicken_tiamat", "Tiamat" },
		                                   	{ "corcs", "Mariner" },
		                                   	{ "armmanni", "Penetrator" },
		                                   	{ "corbtrans", "Vindicator" },
		                                   	{ "corawac", "Vulture" },
		                                   	{ "armraven", "Catapult" },
		                                   };

		public override GrammarBuilder GrammarBuilder
		{
			get
			{
				var verb = new Choices("Build", "Construct", "Make", "Start", "Make me");

				// add numbers
				var numbers = new Choices();
				for (var i = 1; i <= MAX_ORDERS; i++) numbers.Add(new SemanticResultValue(i.ToString(), i));
				numbers.Add(new SemanticResultValue("a", 1));
				numbers.Add(new SemanticResultValue("an", 1));

				// add units
				var units = new Choices();
				foreach (var kvp in unitNames)
				{
					units.Add(new SemanticResultValue(kvp.Value, kvp.Key));
				}

				var grammarBuilder = new GrammarBuilder();

				grammarBuilder.Append(verb);

				grammarBuilder.Append(new SemanticResultKey("number", numbers));
				grammarBuilder.Append(new SemanticResultKey("unit", units));

				var modifier = new Choices();
				modifier.Add(new SemanticResultKey("repeat", " and repeat"));
				modifier.Add(new SemanticResultKey("insert", "now"));
				modifier.Add(new SemanticResultKey("insert", "fast"));
				modifier.Add(new SemanticResultKey("insert", "first"));
				grammarBuilder.Append(modifier, 0, 1);

				return grammarBuilder;
			}
		}
		public override string Name { get { return "buildUnit"; } }

		public override void Aknowledge(SpeechSynthesizer speechSynthesizer, RecognitionResult result)
		{
			var unit = (string)result.Semantics["unit"].Value;
			var number = (int)result.Semantics["number"].Value;

			var unitName = unitNames[unit];
			if (number > 1)
			{
				unitName = Pluralizer.ToPlural(unitName);
			}
			var repeat = result.Semantics.ContainsKey("repeat") ? "and repeat" : "";
			var insert = result.Semantics.ContainsKey("insert") ? "now" : "";
			var reply = string.Format("Build {0} {1} {2}{3}, yes sir!", number, unitName, repeat, insert);
			speechSynthesizer.SpeakAsync(reply);
		}

		public override string ToLua(RecognitionResult result)
		{
			var unit = (string)result.Semantics["unit"].Value;
			var number = (int)result.Semantics["number"].Value;
			var table = "{\n";
			table += String.Format("  commandName = \"{0}\",\n", Name);
			table += String.Format("  unit = \"{0}\",\n", unit);
			table += String.Format("  number = \"{0}\",\n", number);
			table += String.Format("  [\"repeat\"] = {0},\n", result.Semantics.ContainsKey("repeat") ? "true" : "false");
			table += String.Format("  insert = {0},\n", result.Semantics.ContainsKey("insert") ? "true" : "false");
			table += "}";
			return table;
		}
	}
}