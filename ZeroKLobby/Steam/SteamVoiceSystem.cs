using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Steamworks;

namespace ZeroKLobby
{
    public class SteamVoiceSystem
    {
        /// <summary>
        /// How often to fetch mic data (recommended min 5x per sec)
        /// </summary>
        const int recordingIntervalMs = 100;
        
        /// <summary>
        ///     Buffer to add when voice starts to smooth out irregulaties in sent data
        /// </summary>
        const int silencerBufferMs = 200;
        
        /// <summary>
        /// Set to true to test in loopback mode
        /// </summary>
        const bool testLoopback = false;

        /// <summary>
        ///     Control byte added to first position of buffers transmitted over network
        /// </summary>
        [Flags]
        public enum ControlByteFlags: byte
        {
            IsFirst = 1 // is the first packet after starting talking
        }

        bool isInitialized;
        bool isRecording;
        readonly ConcurrentQueue<byte[]> loopbackBufs = new ConcurrentQueue<byte[]>();
        MixingSampleProvider mixer;
        CSteamID mySteamID;

        Callback<P2PSessionRequest_t> newConnectionCallback;
        byte[] silencer;
        DirectSoundOut soundOut;
        readonly AutoResetEvent talkWaiter = new AutoResetEvent(false);
        readonly ConcurrentDictionary<CSteamID, bool> targetSteamIDs = new ConcurrentDictionary<CSteamID, bool>();
        BufferedWaveProvider waveProvider;

        public void AddListenerSteamID(ulong steamID)
        {
            var cSteamId = new CSteamID(steamID);
            if (cSteamId != mySteamID) {
                targetSteamIDs.TryAdd(cSteamId, true);
                var buf = new byte[1];
                SteamNetworking.SendP2PPacket(cSteamId, buf, 1, EP2PSend.k_EP2PSendUnreliable); // send dummy packet to pre-establish connection
            }
        }


        public void Init(ulong mySteamID)
        {
            if (isInitialized) return;
            isInitialized = true;

            RemoveListenerSteamID(mySteamID);
            this.mySteamID = new CSteamID(mySteamID);
            newConnectionCallback = Callback<P2PSessionRequest_t>.Create(t => SteamNetworking.AcceptP2PSessionWithUser(t.m_steamIDRemote));
            // default accept all
            GlobalHook.RegisterHandler(Keys.CapsLock, (key, pressed) => {
                if (pressed) {
                    if (!isRecording) {
                        SteamUser.StartVoiceRecording();
                        SteamFriends.SetInGameVoiceSpeaking(new CSteamID(mySteamID), true);
                        isRecording = true;
                        talkWaiter.Set();
                    }
                    return true;
                }
                if (isRecording) {
                    SteamUser.StopVoiceRecording();
                    SteamFriends.SetInGameVoiceSpeaking(new CSteamID(mySteamID), false);
                    isRecording = false;
                }
                return false;
            });

            soundOut = new DirectSoundOut();
            waveProvider = new BufferedWaveProvider(new WaveFormat(44100, 1));

            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 1));
            mixer.ReadFully = true;
            mixer.AddMixerInput(new Pcm16BitToSampleProvider(waveProvider));

            silencer = new byte[(int)(waveProvider.WaveFormat.AverageBytesPerSecond*silencerBufferMs/1000.0/2*2)];

            soundOut.Init(mixer);
            soundOut.Play();

            new Thread(RecordingFunc).Start();
            if (testLoopback) new Thread(LoopbackFunc).Start();
            else new Thread(PlayingFunc).Start();
        }

        public void RemoveListenerSteamID(ulong steamID)
        {
            bool val;
            targetSteamIDs.TryRemove(new CSteamID(steamID), out val);
        }

        void LoopbackFunc()
        {
            var inputBuffer = new byte[20000];
            var decompressBuffer = new byte[20000];

            byte[] networkBuf;
            var rand = new Random();
            while (true) {
                if (loopbackBufs.TryDequeue(out networkBuf)) {
                    PlaySoundFromNetworkData(networkBuf, (uint)networkBuf.Length, inputBuffer, decompressBuffer);
                    Thread.Sleep(rand.Next(200));
                }
            }
        }

        void PlaySoundFromNetworkData(byte[] networkBuf, uint networkSize, byte[] inputBuffer, byte[] decompressBuffer)
        {
            var flags = (ControlByteFlags)networkBuf[0];
            Array.Copy(networkBuf, 1, inputBuffer, 0, networkSize - 1);

            uint decompressSize;
            if (SteamUser.DecompressVoice(inputBuffer, networkSize - 1, decompressBuffer, (uint)decompressBuffer.Length, out decompressSize, 44100) ==
                EVoiceResult.k_EVoiceResultOK) {
                if ((flags & ControlByteFlags.IsFirst) > 0) waveProvider.AddSamples(silencer, 0, silencer.Length); // add some delay to minimize jitter on first packet
                waveProvider.AddSamples(decompressBuffer, 0, (int)decompressSize);
            }
        }

        void PlayingFunc()
        {
            uint networkSize;
            var networkBuffer = new byte[8000];
            var inputBuffer = new byte[8000];
            var decompressBuffer = new byte[20000];
            while (true) {
                if (SteamNetworking.IsP2PPacketAvailable(out networkSize)) {
                    CSteamID remotUSer;

                    if (SteamNetworking.ReadP2PPacket(networkBuffer, (uint)networkBuffer.Length, out networkSize, out remotUSer) && networkSize > 1) PlaySoundFromNetworkData(networkBuffer, networkSize, inputBuffer, decompressBuffer);
                }
            }
        }


        void RecordingFunc()
        {
            var buf = new byte[20000];
            var toSend = new byte[20000];
            int counter = 0;

            while (true) {
                talkWaiter.WaitOne(recordingIntervalMs);
                if (!isRecording) {
                    counter = 0;
                    continue;
                }
                counter++;
                uint cbs;
                uint ubs;
                uint sendLength;
                if (SteamUser.GetVoice(true, buf, (uint)buf.Length, out cbs, false, null, 0, out ubs, 44100) == EVoiceResult.k_EVoiceResultOK) {
                    Array.Copy(buf, 0, toSend, 1, cbs);
                    sendLength = cbs + 1;

                    toSend[0] = counter == 1 ? (byte)ControlByteFlags.IsFirst : (byte)0;
                    if (testLoopback) {
                        var data = new byte[sendLength];
                        Array.Copy(toSend, data, sendLength);
                        loopbackBufs.Enqueue(data);
                    } else foreach (var t in targetSteamIDs) SteamNetworking.SendP2PPacket(t.Key, toSend, sendLength, EP2PSend.k_EP2PSendUnreliable);
                }
            }
        }
    }
}