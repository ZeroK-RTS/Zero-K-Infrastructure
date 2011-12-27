using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;

/* 
 * todo: 
 * - deselect, filter out
 * - select
 * - build unit
 * - select building in constructor menu
 * - order (move, attack, repeat, etc)
 * - ally, spec chat?
 */

namespace ZeroKLobby.VoiceCommand
{
	class VoiceCommand : IDisposable
	{
			SpeechRecognizer speechRecognizer = new SpeechRecognizer();
			SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();

			public VoiceCommand()
			{
				speechRecognizer.SpeechRecognized += speechRecognizer_SpeechRecognized;
				speechRecognizer.SpeechRecognitionRejected += speechRecognizer_SpeechRecognitionRejected;
				// new BuildUnitGrammar().Initialize(speechRecognizer);
			}

			void speechRecognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
			{
				speechSynthesizer.SpeakAsync("What was that?");
			}


			void speechRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
			{
				ZkGrammar.ProcessResult(e.Result, speechSynthesizer);
			}

		public bool Enabled
		{
			get { return speechRecognizer.Enabled; }
			set { speechRecognizer.Enabled = value; }
		}


		public void Dispose()
		{
			speechRecognizer.Dispose();
			speechSynthesizer.Dispose();
		}
	}

}
