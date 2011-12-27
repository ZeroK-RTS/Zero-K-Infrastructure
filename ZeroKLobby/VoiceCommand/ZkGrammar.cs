using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Forms;
using LobbyClient;

namespace ZeroKLobby.VoiceCommand
{
	abstract class ZkGrammar
	{
		static List<ZkGrammar> zkGrammars = new List<ZkGrammar>();

		public abstract string Name { get; }
		public abstract GrammarBuilder GrammarBuilder { get; }

		public static void ProcessResult(RecognitionResult result, SpeechSynthesizer speechSynthesizer, TasClient client)
		{
			var grammar = zkGrammars.FirstOrDefault(g => result.Grammar.Name == g.Name);
			if (grammar == null)
			{
				speechSynthesizer.SpeakAsync("What was that?");
			}
			grammar.Aknowledge(speechSynthesizer, result);
			var table = grammar.ToLua(result);
			client.Say(TasClient.SayPlace.User, client.MyBattle.Founder.Name, "!say " + table.Replace("\n", ""), false);
			MessageBox.Show(table);
		}

		public void Initialize(SpeechRecognitionEngine speechEngine)
		{
			var grammar = new Grammar(GrammarBuilder);
			grammar.Name = Name;
			speechEngine.LoadGrammarAsync(grammar);
			zkGrammars.Add(this);
		}

		public virtual void Aknowledge(SpeechSynthesizer speechSynthesizer, RecognitionResult result)
		{
			speechSynthesizer.SpeakAsync("Roger roger!");
		}

		public abstract string ToLua(RecognitionResult result);
	}
}
