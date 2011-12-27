using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby.VoiceCommand
{
	abstract class ZkGrammar
	{
		static List<ZkGrammar> zkGrammars = new List<ZkGrammar>();

		public abstract string Name { get; }
		public abstract GrammarBuilder GrammarBuilder { get; }

		public static void ProcessResult(RecognitionResult result, SpeechSynthesizer speechSynthesizer)
		{
			var table = zkGrammars.FirstOrDefault(g => result.Grammar.Name == g.Name);
			if (table == null)
			{
				speechSynthesizer.SpeakAsync("What was that?");
				MessageBox.Show("Not recognized");
			}
			table.Aknowledge(speechSynthesizer);
			MessageBox.Show(table.ToLua(result));
		}

		public void Initialize(SpeechRecognizer speechRecognizer)
		{
			var grammar = new Grammar(GrammarBuilder);
			grammar.Name = Name;
			speechRecognizer.LoadGrammarAsync(grammar);
			zkGrammars.Add(this);
		}

		public virtual void Aknowledge(SpeechSynthesizer speechSynthesizer)
		{
			speechSynthesizer.SpeakAsync("Yes sir!");
		}

		public abstract string ToLua(RecognitionResult result);
	}
}
