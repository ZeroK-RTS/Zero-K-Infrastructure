using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ChobbyLauncher
{
    public abstract class TextToSpeechBase
    {
        public static TextToSpeechBase Create()
        {
            TextToSpeechBase ret = null;
            try
            {
                if (Environment.OSVersion.Platform != PlatformID.Unix) ret = Activator.CreateInstance(Type.GetType("ChobbyLauncher.TextToSpeechWindows")) as TextToSpeechBase;
                else ret = new TextToSpeechLinux();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failed to init VoiceCommands:{0}", ex.Message);
            }

            return ret;
        }

        public abstract void Say(string name, string text);
        public abstract void SetVolume(double volume); // 0-1

        protected static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return
                new string(
                    input.ToCharArray()
                        .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || (c == '-') || (c == '?') || (c == '!'))
                        .ToArray());
        }
    }
}