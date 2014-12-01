using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
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
        ConcurrentDictionary<CSteamID, bool> targetSteamIDs = new ConcurrentDictionary<CSteamID, bool>();
        CSteamID mySteamID;
        
        AutoResetEvent waiter = new AutoResetEvent(false);
        Callback<P2PSessionRequest_t> newConnectionCallback;

        public void AddListenerSteamID(ulong steamID)
        {
            var cSteamId = new CSteamID(steamID);
            if (cSteamId != mySteamID)
            {
                targetSteamIDs.TryAdd(cSteamId, true);
                SteamNetworking.AcceptP2PSessionWithUser(cSteamId);
            }
        }


        public void RemoveListenerSteamID(ulong steamID)
        {
            bool val;
            targetSteamIDs.TryRemove(new CSteamID(steamID), out val);
        }

        public void Init(ulong mySteamID)
        {
            if (isInitialized) return;
            isInitialized = true;

            SteamNetworking.AllowP2PPacketRelay(true);
            RemoveListenerSteamID(mySteamID);
            this.mySteamID = new CSteamID(mySteamID);
            newConnectionCallback = Callback<P2PSessionRequest_t>.Create(t => SteamNetworking.AcceptP2PSessionWithUser(t.m_steamIDRemote)); // default accept all
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
                            waiter.Set();
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
            new Thread(PlayingFunc).Start();
        }


        private void PlayingFunc()
        {
            uint networkSize;
            var networkBuffer = new byte[8000];
            var decompressBuffer = new byte[20000];
            while (true)
            {
                if (SteamNetworking.IsP2PPacketAvailable(out networkSize))
                {
                    CSteamID remotUSer;
                    if (SteamNetworking.ReadP2PPacket(networkBuffer, (uint)networkBuffer.Length, out networkSize, out remotUSer))
                    {
                        uint decompressSize;
                        if (
                            SteamUser.DecompressVoice(networkBuffer,
                                networkSize,
                                decompressBuffer,
                                (uint)decompressBuffer.Length,
                                out decompressSize,
                                44100) == EVoiceResult.k_EVoiceResultOK) waveProvider.AddSamples(decompressBuffer, 0, (int)decompressSize);
                    }
                }
            }
        }


        private void RecordingFunc()
        {
            var buf = new byte[20000];
            
            while (true)
            {
                waiter.WaitOne(100);
                if (!isRecording) continue;
                uint cbs;
                uint ubs;
                if (SteamUser.GetVoice(true, buf, (uint)buf.Length, out cbs, false, null, 0, out ubs, 44100) == EVoiceResult.k_EVoiceResultOK) 
                {
                    foreach (var t in targetSteamIDs)
                    {
                        SteamNetworking.SendP2PPacket(t.Key, buf, cbs, EP2PSend.k_EP2PSendUnreliable);
                    }

                }
            }
        }
    }
}