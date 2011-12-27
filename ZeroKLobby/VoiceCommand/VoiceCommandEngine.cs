using System;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using LobbyClient;

/* 
 * todo: 
 * - deselect, filter out
 * - select
 * - build unit
 * - select building in constructor menu
 * - order (move, attack, repeat, etc)
 * - ally, spec chat?
 */

namespace ZeroKLobby.VoiceCommand
{
    class VoiceCommandEngine: IDisposable
    {
        readonly TasClient client;
        readonly SpeechRecognitionEngine speechEngine = new SpeechRecognitionEngine();
        readonly SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();

        public VoiceCommandEngine(TasClient client)
        {
            this.client = client;
            speechEngine.SetInputToDefaultAudioDevice();
            speechEngine.SpeechRecognized += SpeechEngineSpeechRecognized;
            speechEngine.SpeechRecognitionRejected += SpeechEngineSpeechRecognitionRejected;
            client.MyBattleStarted += (s, e) => { if (!client.MyBattleStatus.IsSpectator) Start(); };
            client.MyBattleEnded += (s, e) => Stop();
            client.PreviewSaid += (s, e) =>
            {
                if (client.MyBattle != null && e.Data.Place == TasSayEventArgs.Places.Normal && e.Data.UserName == client.MyBattle.Founder.Name && e.Data.Text.StartsWith("!transmit ")) e.Cancel = true;
            };

            InitializeGrammars();
        }

        public void Dispose()
        {
            speechEngine.Dispose();
            speechSynthesizer.Dispose();
        }

        public void Start()
        {
            speechEngine.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void Stop()
        {
            speechEngine.RecognizeAsyncStop();
        }

        void InitializeGrammars()
        {
            new BuildUnitGrammar().Initialize(speechEngine);
        }

        void SpeechEngineSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            speechSynthesizer.SpeakAsync("What was that?");
        }


        void SpeechEngineSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            ZkGrammar.ProcessResult(e.Result, speechSynthesizer, client);
        }
    }
}