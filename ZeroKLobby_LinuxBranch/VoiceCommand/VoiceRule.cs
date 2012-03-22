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
	abstract class VoiceRule
	{
		public Grammar Grammar { get; private set; }

		public abstract string Name { get; }
		public abstract GrammarBuilder GetGrammarBuilder();

		public VoiceRule()
		{
			CreateGrammar();
		}

		protected void CreateGrammar()
		{
			var builder = GetGrammarBuilder();
			if (builder != null)
			{
				Grammar = new Grammar(builder);
				Grammar.Name = Name;
			}
			else
			{
				Grammar = null;
			}
		}


		public abstract void Aknowledge(SpeechSynthesizer speechSynthesizer, RecognitionResult result);
		public abstract string ToLua(RecognitionResult result);

		/// <summary>
		/// Called with the widget sends a message to this voice rule.
		/// </summary>
		/// <param name="paramsString"></param>
		/// <param name="load">New grammar to be loaded. Set to null to load no new rule.</param>
		/// <param name="unload">Grammar to be unloaded. Set to null to unload no rule. </param>
		public abstract void SpringLine(string paramsString, out Grammar load, out Grammar unload);
	}
}
