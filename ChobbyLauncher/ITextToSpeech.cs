using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChobbyLauncher
{
    public abstract class TextToSpeechBase
    {
        public abstract void SetVolume(double volume); // 0-1
        public abstract void Say(string name, string text);

        public static TextToSpeechBase Create()
        {
            TextToSpeechBase ret = null;
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                try
                {
                    // silly way to create speech and voice engines on runtime - needed due to mono crash
                    ret = Activator.CreateInstance(Type.GetType("ChobbyLauncher.TextToSpeechWindows")) as TextToSpeechBase;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Failed to init VoiceCommands:{0}", ex.Message);
                }
            }

            return ret;
        }

        protected static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return new string(input.ToCharArray().Where(c => (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c == '-' || c == '?' || c == '!')).ToArray());
        }
    }


}