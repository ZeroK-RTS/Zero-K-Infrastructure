using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZkData;

namespace ChobbyLauncher
{
    public class TextToSpeechLinux : TextToSpeechBase
    {
        private double volume = 1.0;

        public TextToSpeechLinux() { }

        public override void Say(string name, string text)
        {
            var volint = ((int)volume.Clamp(0, 1) * 200.0 - 100.0).Clamp(-100, 100);
            Process.Start("spd-say", $"-i {volint} \"{Sanitize(text)}\"");
        }

        public override void SetVolume(double volume)
        {
            this.volume = volume;
        }
    }
}