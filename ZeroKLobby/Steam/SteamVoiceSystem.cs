using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        /// Sample rate for recording and replaying
        /// </summary>
        const int sampleRate = 44100;

        /// <summary>
        /// How often to ask steam for data
        /// </summary>
        const int networkCheckIntervalMs = 50;

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
        MixerWrapper<ulong> mixerWrapper;

        public void AddListenerSteamID(ulong steamID)
        {
            var cSteamId = new CSteamID(steamID);
            if (cSteamId != mySteamID) {
                targetSteamIDs.TryAdd(cSteamId, true);
                if (isInitialized) {
                    SendDummyP2PPacket(cSteamId);
                }
            }
        }

        static void SendDummyP2PPacket(CSteamID cSteamId)
        {
            var buf = new byte[1];
            SteamNetworking.SendP2PPacket(cSteamId, buf, 1, EP2PSend.k_EP2PSendUnreliable); // send dummy packet to pre-establish connection
        }


        /// <summary>
        /// Maintains set number of channels for mixing
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class MixerWrapper<T>
        {
            int maxChannels;
            MixingSampleProvider mixer;
            public MixerWrapper(MixingSampleProvider mixer, int maxChannels = 4)
            {
                this.mixer = mixer;
                this.maxChannels = maxChannels;
            }

            Dictionary<T, Tuple<BufferedWaveProvider,ISampleProvider>>  waveProviders = new Dictionary<T, Tuple<BufferedWaveProvider, ISampleProvider>>();
            
            public BufferedWaveProvider GetWaveProvider(T key)
            {
                Tuple<BufferedWaveProvider, ISampleProvider> val;
                if (waveProviders.TryGetValue(key, out val)) return val.Item1;
                if (waveProviders.Count >= maxChannels) {
                    var keyToDel = waveProviders.Keys.First();
                    var todel = waveProviders[keyToDel];
                    mixer.RemoveMixerInput(todel.Item2);
                    waveProviders.Remove(keyToDel);
                }
                var wave  = new BufferedWaveProvider(new WaveFormat(sampleRate, 1));
                var sample = new Pcm16BitToSampleProvider(wave);
                waveProviders.Add(key, Tuple.Create<BufferedWaveProvider,ISampleProvider>(wave,sample));
                mixer.AddMixerInput(sample);
                return wave;
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

            Program.MainWindow.InvokeFunc(() => GlobalHook.RegisterHandler(Keys.CapsLock, (key, pressed) =>
            {
                if (pressed)
                {
                    if (!isRecording)
                    {
                        SteamUser.StartVoiceRecording();
                        SteamFriends.SetInGameVoiceSpeaking(new CSteamID(mySteamID), true);
                        isRecording = true;
                        talkWaiter.Set();
                    }
                    return true;
                }
                else
                {
                    if (isRecording)
                    {
                        SteamUser.StopVoiceRecording();
                        SteamFriends.SetInGameVoiceSpeaking(new CSteamID(mySteamID), false);
                        isRecording = false;
                    }
                    return true;
                }
            }));

            soundOut = new DirectSoundOut();

            mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1));
            mixer.ReadFully = true;

            mixerWrapper = new MixerWrapper<ulong>(mixer);


            silencer = new byte[(int)(new WaveFormat(sampleRate,1).AverageBytesPerSecond*silencerBufferMs/1000.0/2*2)];

            soundOut.Init(mixer);
            soundOut.Play();

            new Thread(RecordingFunc).Start();
            if (testLoopback) new Thread(LoopbackFunc).Start();
            else new Thread(PlayingFunc).Start();

            foreach (var t in targetSteamIDs) SendDummyP2PPacket(t.Key);
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
                if (Program.CloseOnNext) return;
                if (loopbackBufs.TryDequeue(out networkBuf)) {
                    var wave = mixerWrapper.GetWaveProvider(0);
                    PlaySoundFromNetworkData(wave, networkBuf, (uint)networkBuf.Length, inputBuffer, decompressBuffer);
                    Thread.Sleep(rand.Next(200));
                }
            }
        }

        void PlaySoundFromNetworkData(BufferedWaveProvider waveProvider, byte[] networkBuf, uint networkSize, byte[] inputBuffer, byte[] decompressBuffer)
        {
            var flags = (ControlByteFlags)networkBuf[0];
            Array.Copy(networkBuf, 1, inputBuffer, 0, networkSize - 1);

            uint decompressSize;
            if (SteamUser.DecompressVoice(inputBuffer, networkSize - 1, decompressBuffer, (uint)decompressBuffer.Length, out decompressSize, sampleRate) ==
                EVoiceResult.k_EVoiceResultOK) {
                if ((flags & ControlByteFlags.IsFirst) > 0)  waveProvider.AddSamples(silencer, 0, silencer.Length); // add some delay to minimize jitter on first packet
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
                if (Program.CloseOnNext) return;
                if (SteamAPI.IsSteamRunning() && SteamNetworking.IsP2PPacketAvailable(out networkSize)) {
                    CSteamID remotUSer;
                    if (SteamNetworking.ReadP2PPacket(networkBuffer, (uint)networkBuffer.Length, out networkSize, out remotUSer) && networkSize > 1) {
                        var wave = mixerWrapper.GetWaveProvider(remotUSer.m_SteamID);
                        PlaySoundFromNetworkData(wave, networkBuffer, networkSize, inputBuffer, decompressBuffer);
                    }
                } else Thread.Sleep(networkCheckIntervalMs);
            }
        }


        void RecordingFunc()
        {
            var buf = new byte[20000];
            var toSend = new byte[20000];
            int counter = 0;

            while (true) {
                if (Program.CloseOnNext) return;
                talkWaiter.WaitOne(recordingIntervalMs);
                if (!isRecording) {
                    counter = 0;
                    continue;
                }
                counter++;
                uint cbs;
                uint ubs;
                uint sendLength;
                if (SteamAPI.IsSteamRunning() && SteamUser.GetVoice(true, buf, (uint)buf.Length, out cbs, false, null, 0, out ubs, sampleRate) == EVoiceResult.k_EVoiceResultOK) {
                    Array.Copy(buf, 0, toSend, 1, cbs);
                    sendLength = cbs + 1;

                    toSend[0] = counter == 1 ? (byte)ControlByteFlags.IsFirst : (byte)0;
                    if (testLoopback) {
                        var data = new byte[sendLength];
                        Array.Copy(toSend, data, sendLength);
                        loopbackBufs.Enqueue(data);
                    } else 
                        foreach (var t in targetSteamIDs) SteamNetworking.SendP2PPacket(t.Key, toSend, sendLength, EP2PSend.k_EP2PSendUnreliable);
                }
            }
        }
    }
}