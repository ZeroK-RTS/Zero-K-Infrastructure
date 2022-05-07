﻿#if !(__MonoCS__)

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using LobbyClient;
using PlasmaShared;
using ZkData;

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
                    // things to remember: Spring can add its frame number info at the beginning

                    var m = Regex.Match(text, "^(\\[f=\\d+\\] ?)?[^ ]+ ENABLE TTS$");
                    if (m.Success) isSpeechEnabled = true;

                    m = Regex.Match(text, "^(\\[f=\\d+\\] ?)?[^ ]+ DISABLE TTS$");
                    if (m.Success) isSpeechEnabled = false;

                    m = Regex.Match(text, "^(\\[f=\\d+\\] ?)?[^ ]+ TTS VOLUME ([0-9]+)$");
                    if (m.Success)
                    {
                        int volume;
                        if (int.TryParse(m.Groups[2].Value, out volume))
                        {
                            volume = volume.Clamp(0, 100);
                            speechSynthesizer.Volume = volume;
                        }
                    }
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
                            if (voices.Count > 1) speechSynthesizer.SelectVoice(voices[Math.Abs(name.GetHashCode() % voices.Count)].VoiceInfo.Name);

                            sayText = new string(sayText.ToCharArray().Where(c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-' || c=='?' || c=='!')).ToArray());
                            
                            
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

        void spring_SpringExited(object sender, SpringBattleContext springBattleContext)
        {
            if (speechSynthesizer!=null) speechSynthesizer.SpeakAsyncCancelAll();
        }
    }
}

#endif
