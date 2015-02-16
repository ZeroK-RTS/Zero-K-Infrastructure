using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace ZeroKLobby.Controls
{
    public class SoundPalette
    {
        public enum SoundType
        {
            None = 0,
            Click = 1,
            Servo =2 
        }


        public static void Play(SoundType type)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix) return;
            if (type != SoundType.None) {
                Stream sound;
                switch (type) {
                    case SoundType.Click:
                        sound = Sounds.button_click;
                        break;
                    case SoundType.Servo:
                        sound = Sounds.panel_move;
                        break;
                    default:
                        sound = null;
                        break;
                }


                if (sound != null) {
                    var sp = new SoundPlayer(sound);
                    sp.Play();
                }
            }
        }
    }
}
