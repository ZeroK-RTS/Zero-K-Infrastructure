using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using NAudio.Wave;
using Steamworks;

namespace ZeroKLobby
{
    public class SteamVoiceSystem
    {
        bool isRecording;
        bool isInitialized;
        BufferedWaveProvider waveProvider;
        public List<ulong> TargetSteamIDs = new List<ulong>();
        ulong mySteamID;
        

        public void Init(ulong mySteamID)
        {
            if (isInitialized) return;
            this.mySteamID = mySteamID;
            isInitialized = true;
            GlobalHook.RegisterHandler(Keys.CapsLock,
                (key, pressed) =>
                {
                    if (pressed)
                    {
                        if (!isRecording)
                        {
                            SteamUser.StartVoiceRecording();
                            SteamFriends.SetInGameVoiceSpeaking(new CSteamID(mySteamID), true);
                            isRecording = true;
                        }
                    }
                    else
                    {
                        if (isRecording)
                        {
                            SteamUser.StopVoiceRecording();
                            SteamFriends.SetInGameVoiceSpeaking(new CSteamID(mySteamID), false);
                            isRecording = false;
                        }
                    }
                    return true;
                });

            var na = new DirectSoundOut();
            waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1));
            na.Init(waveProvider);
            na.Play();

            new Thread(RecordingFunc).Start();
        }

        private void RecordingFunc()
        {
            var buf = new byte[20000];
            var dest = new byte[20000];
            while (true)
            {
                Thread.Sleep(100);
                if (!isRecording) return;
                uint cbs;
                uint ubs;
                if (SteamUser.GetVoice(true, buf, (uint)buf.Length, out cbs, false, null, 0, out ubs, 44100) == EVoiceResult.k_EVoiceResultOK) 
                {

                    uint writ;
                    if (SteamUser.DecompressVoice(buf, cbs, dest, (uint)dest.Length, out writ, 44100) == EVoiceResult.k_EVoiceResultOK) waveProvider.AddSamples(dest, 0, (int)writ);
                }
            }
        }
    }
}