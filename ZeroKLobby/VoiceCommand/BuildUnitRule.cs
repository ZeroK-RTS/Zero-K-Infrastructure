using System;
using System.Collections.Generic;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Windows.Forms;

namespace ZeroKLobby.VoiceCommand
{
	/* grammar for ordering the construction of mobile units in factories
	 * examples:
	 * - build a bandit
	 * - make 20 drones and repeat
	 * - build one weaver now
	 */


	class BuildUnitRule : VoiceRule
	{
		const int MAX_ORDERS = 50;

		Dictionary<string, string> unitNames = new Dictionary<string, string>();

		public override GrammarBuilder GetGrammarBuilder()
		{
			if (unitNames.Count == 0) return null;

			var verb = new Choices("build", "construct", "make", "start", "make me");

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

			grammarBuilder.Append("Factories, ", 0, 1);

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

		public override void SpringLine(string paramsString, out Grammar load, out Grammar unload)
		{
			load = null;
			unload = null;

			var parameters = paramsString.Split(';');

			var mode = parameters[0];

			if (parameters.Length == 1)
			{
				if (mode == "reload")
				{
					unload = Grammar;
					CreateGrammar();
					load = Grammar;
				}
			}

			if (parameters.Length > 1)
			{
				var unitName = parameters[1];

				if (mode == "add")
				{
					var unitFullName = parameters[2];
					if (!unitNames.ContainsKey(unitName) && parameters.Length == 3) unitNames[unitName] = unitFullName;
				}
				else if (mode == "remove") if (unitNames.ContainsKey(unitName)) unitNames.Remove(unitName);
			}
		}
	}
}