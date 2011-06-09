using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using LobbyClient;

namespace ZeroKLobby
{
	class ChatToSpeech
	{
		bool isSpeechEnabled = false;
		SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
		ReadOnlyCollection<InstalledVoice> voices;

		public ChatToSpeech()
		{
			var spring = new Spring(Program.SpringPaths);
			spring.LogLineAdded += spring_LogLineAdded;
			voices = speechSynthesizer.GetInstalledVoices();
		}

		void spring_LogLineAdded(string text, bool isError)
		{
			if (text == "ENABLE TTS") isSpeechEnabled = true;
			else if (text == "DISABLE TTS") isSpeechEnabled = false;
			else
			{
				speechSynthesizer.SpeakAsync(text); 
			}
		}
	}
}
