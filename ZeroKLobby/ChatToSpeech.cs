using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using LobbyClient;

namespace ZeroKLobby
{
	class ChatToSpeech
	{
		bool isSpeechEnabled = true;
		SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
		ReadOnlyCollection<InstalledVoice> voices;
		
		public ChatToSpeech(Spring spring)
		{
			spring.LogLineAdded += spring_LogLineAdded;
			voices = speechSynthesizer.GetInstalledVoices();
		}

		void spring_LogLineAdded(string text, bool isError)
		{
			if (string.IsNullOrEmpty(text) || isError) return;
			try {

				if (text == "ENABLE TTS") isSpeechEnabled = true;
				else if (text == "DISABLE TTS") isSpeechEnabled = false;
				else {
					var match = Regex.Match(text, "\\] <([^>]+)> Allies: (.+)");
					if (match.Success) {
						var name = match.Groups[1].Value;
						var sayText = match.Groups[2].Value;

						speechSynthesizer.SelectVoice(voices[name.GetHashCode() % voices.Count].VoiceInfo.Name);
						speechSynthesizer.SpeakAsync(sayText);
					}
				}
			} catch (Exception ex) {
				Trace.TraceError("Error in text to speech: {0}", ex);
			}
		}
	}
}
