using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using LobbyClient;

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
	class VoiceCommandEngine : IDisposable
	{
		readonly TasClient client;
		SpeechRecognitionEngine speechEngine = new SpeechRecognitionEngine();
		SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();

		public VoiceCommandEngine(TasClient client)
		{
			this.client = client;
			speechEngine.SetInputToDefaultAudioDevice();
			speechEngine.SpeechRecognized += SpeechEngineSpeechRecognized;
			speechEngine.SpeechRecognitionRejected += SpeechEngineSpeechRecognitionRejected;
			InitializeGrammars();

		}

		public void Start()
		{
			speechEngine.RecognizeAsync(RecognizeMode.Multiple);
		}

		public void Stop()
		{
			speechEngine.RecognizeAsyncStop();
		}

		void InitializeGrammars()
		{
			new BuildUnitGrammar().Initialize(speechEngine);
		}

		void SpeechEngineSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
		{
			speechSynthesizer.SpeakAsync("What was that?");
		}


		void SpeechEngineSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			ZkGrammar.ProcessResult(e.Result, speechSynthesizer, client);
		}


		public void Dispose()
		{
			speechEngine.Dispose();
			speechSynthesizer.Dispose();
		}
	}

}
