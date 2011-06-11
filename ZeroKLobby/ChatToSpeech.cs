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
		bool isSpeechEnabled = false;
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
				if (text.Contains(Program.Conf.LobbyPlayerName))
				{
					if (text.Contains("ENABLE TTS")) isSpeechEnabled = true;
					else if (text.Contains("DISABLE TTS")) isSpeechEnabled = false;
				}
				if (isSpeechEnabled) {
					var match = Regex.Match(text, "\\] <([^>]+)> (Allies:|added point:) (.+)");
					if (match.Success) {
						var name = match.Groups[1].Value;
						var sayText = match.Groups[2].Value;
						if (name != Program.Conf.LobbyPlayerName)
						{
							speechSynthesizer.SelectVoice(voices[name.GetHashCode()%voices.Count].VoiceInfo.Name);
							speechSynthesizer.SpeakAsync(sayText);
						}
					}
				}
			} catch (Exception ex) {
				Trace.TraceError("Error in text to speech: {0}", ex);
			}
		}
	}
}
