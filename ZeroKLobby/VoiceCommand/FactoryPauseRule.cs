using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;

namespace ZeroKLobby.VoiceCommand
{
	/* grammar for pausing and unpausing factoires (wait order)
	 * examples:
	 * - factories, pause
	 * - factories, wait
	 * - stop factories
	 * - resume factories
	 * - suspend factory construction
	 */

	class FactoryPauseRule : VoiceRule
	{
		public override string Name { get { return "factoryPause"; } }
		public override GrammarBuilder GetGrammarBuilder()
		{
			var builder = new GrammarBuilder();

			var pauseChoices = new Choices("pause", "wait", "stop", "suspend");
			var resumeChoices = new Choices("unpause", "resume", "stop waiting", "start");

			var pauseOrResume = new Choices();
			pauseOrResume.Add(new SemanticResultKey("suspend", pauseChoices));
			pauseOrResume.Add(new SemanticResultKey("resume", resumeChoices));

			builder.Append("Factories, ", 0, 1);
			builder.Append(pauseOrResume);
			builder.Append(new Choices("factories", "factory construction"), 0, 1);

			return builder;
			
		}


		public override void Aknowledge(SpeechSynthesizer speechSynthesizer, RecognitionResult result)
		{
			speechSynthesizer.SpeakAsync(result.Text + ", yes sir!");
		}

		public override string ToLua(RecognitionResult result)
		{
			if (!result.Semantics.ContainsKey("resume") && !result.Semantics.ContainsKey("suspend")) throw new Exception("Command is neither pause nor resume");

			var table = "{\n";
			table += String.Format("  commandName = \"{0}\",\n", Name);
			table += String.Format("  mode = \"{0}\",\n", result.Semantics.ContainsKey("suspend") ? "suspend" : "resume");
			table += "}";
			return table;
		}

		public override void SpringLine(string paramsString, out Grammar load, out Grammar unload)
		{
			throw new NotImplementedException();
		}
	}
}
