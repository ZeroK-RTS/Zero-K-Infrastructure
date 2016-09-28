#if !(__MonoCS__)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using ZkData;

namespace ChobbyLauncher
{
    public class TextToSpeechWindows:TextToSpeechBase
    {
        readonly SpeechSynthesizer speechSynthesizer;
        readonly ReadOnlyCollection<InstalledVoice> voices;

        public TextToSpeechWindows()
        {
            try
            {
                speechSynthesizer = new SpeechSynthesizer();
                voices = speechSynthesizer.GetInstalledVoices();
            }
            catch (Exception ex)
            {
                Trace.TraceError("voice synthetizer failed: {0}", ex);
            }
        }

        public override void SetVolume(double volume)
        {
            volume = volume.Clamp(0, 1);
            volume = volume*100.0;
            volume = volume.Clamp(0, 100);
            speechSynthesizer.Volume = (int)volume;
        }

        public override void Say(string name, string text)
        {
            if (voices.Count > 1)
            {
                var voiceName = voices[Math.Abs(name.GetHashCode() % voices.Count)].VoiceInfo.Name;
                speechSynthesizer.SelectVoice(voiceName);
            }
            text = Sanitize(text);
            speechSynthesizer.SpeakAsync(text);
        }
    }
}

#endif
