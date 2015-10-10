#if !(__MonoCS__)

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using LobbyClient;

namespace ZeroKLobby
{
    class ChatToSpeech
    {
        bool isSpeechEnabled = false;
        readonly SpeechSynthesizer speechSynthesizer;
        readonly ReadOnlyCollection<InstalledVoice> voices;

        public ChatToSpeech(Spring spring)
        {
            try
            {
                speechSynthesizer = new SpeechSynthesizer();
                voices = speechSynthesizer.GetInstalledVoices();
                spring.LogLineAdded += spring_LogLineAdded;
                spring.SpringExited += spring_SpringExited;
            }
            catch (Exception ex)
            {
                Trace.TraceError("voice synthetizer failed: {0}", ex);
            }
        }

        void spring_LogLineAdded(string text, bool isError)
        {
            if (string.IsNullOrEmpty(text) || isError || voices == null) return;
            try
            {
                if (text.Contains(Program.Conf.LobbyPlayerName))
                {
                    if (text.Contains("ENABLE TTS")) isSpeechEnabled = true;
                    else if (text.Contains("DISABLE TTS")) isSpeechEnabled = false;
                }
                if (isSpeechEnabled)
                {
                    var match = Regex.Match(text, "\\] <([^>]+)> Allies: (.+)");
                    if (!match.Success) match = Regex.Match(text, "\\] ([^ ]+) added point: (.+)");
                    if (match.Success)
                    {
                        var name = match.Groups[1].Value;
                        User userData;
                        bool ban = Program.TasClient.ExistingUsers.TryGetValue(name, out userData) && userData.BanMute;
                        var sayText = match.Groups[2].Value;
                        
                        bool validText = (name != Program.Conf.LobbyPlayerName && !string.IsNullOrEmpty(text) && !Regex.IsMatch(sayText, "Start [0-9]+$") && !sayText.StartsWith("I choose: "));
                        if (validText && !ban)
                        {
                            if (voices.Count > 1) speechSynthesizer.SelectVoice(voices[name.GetHashCode() % voices.Count].VoiceInfo.Name);
                            speechSynthesizer.SpeakAsync(sayText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in text to speech: {0}", ex);
            }
        }

        void spring_SpringExited(object sender, ZkData.EventArgs<bool> e)
        {
            if (speechSynthesizer!=null) speechSynthesizer.SpeakAsyncCancelAll();
        }
    }
}

#endif