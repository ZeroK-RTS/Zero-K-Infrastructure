using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using LobbyClient;

/* 
 * todo: 
 * - deselect, filter out
 * - select
 * - build unit
 * - select building in constructor menu
 * - order (move, attack, repeat, etc)
 * - ally, spec chat?
 * 
 * 
 * - reset voice engine state on new game
 */

namespace ZeroKLobby.VoiceCommand
{
    class VoiceCommandEngine: IDisposable
    {
        readonly TasClient client;
    	readonly SpeechRecognitionEngine speechEngine = new SpeechRecognitionEngine();
        readonly SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer();
		List<VoiceRule> voiceRules = new List<VoiceRule>();

        public VoiceCommandEngine(TasClient client, Spring spring)
        {
            this.client = client;
        	speechEngine.SetInputToDefaultAudioDevice();
            speechEngine.SpeechRecognized += SpeechEngineSpeechRecognized;
            speechEngine.SpeechRecognitionRejected += SpeechEngineSpeechRecognitionRejected;
            spring.SpringStarted += (s, e) => {
                try
                {
                    if (!client.MyBattleStatus.IsSpectator) Start();
                }
                catch (Exception ex) {
                    Trace.TraceError("Error in speech recognizer:{0}",ex);
                }
            };
            spring.SpringExited += (s, e) =>
            {
                try
                {
                    Stop();
                }
                catch (Exception ex) {
                    Trace.TraceError("Error in speech recognizer:{0}", ex);
                }
            };
            client.PreviewSaid += (s, e) =>
            {
                if (client.MyBattle != null && e.Data.Place == TasSayEventArgs.Places.Normal && e.Data.UserName == client.MyBattle.Founder.Name && e.Data.Text.StartsWith("!transmit ")) e.Cancel = true;
            };

        	voiceRules.Add(new BuildUnitRule());
        	// voiceRules.Add(new FactoryPauseRule());
			foreach (var rule in voiceRules)
			{
				if (rule.Grammar != null) speechEngine.LoadGrammar(rule.Grammar);
			}
			speechEngine.LoadGrammar(new Grammar(new GrammarBuilder("If no grammar is loaded, the speech engine will not start")));
			spring.LogLineAdded += spring_LogLineAdded;
        }

		void spring_LogLineAdded(string text, bool isError)
		{
			if (text == null) return;
			if (!text.Contains("!transmitlobby @voice@")) return;
			var logLineRegex = @"^\[f=\d{7}]\ !transmitlobby @voice@(?<ruleName>\w+?)@(?<parameters>.+?)$";
			var matches = Regex.Matches(text, logLineRegex, RegexOptions.Multiline);
			foreach (Match match in matches)
			{
				var ruleName = match.Groups["ruleName"].Value;
				var parameters = match.Groups["parameters"].Value;
				var rule = voiceRules.FirstOrDefault(g => ruleName == g.Name);
				if (rule != null)
				{
					Grammar load;
					Grammar unload;
					rule.SpringLine(parameters, out load, out unload);
					if (load != null) speechEngine.LoadGrammar(load);
					if (unload != null) speechEngine.UnloadGrammar(unload);
				}
			}
		}

    	public void Dispose()
        {
            speechEngine.Dispose();
            speechSynthesizer.Dispose();
        }

        public void Start()
        {
            if (speechEngine.AudioState == AudioState.Stopped)
            {
                speechEngine.RecognizeAsyncStop();
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }
		
        public void Stop()
        {
            if (speechEngine.AudioState!= AudioState.Stopped) speechEngine.RecognizeAsyncStop();
        }

        void SpeechEngineSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            speechSynthesizer.SpeakAsync("What was that?");
        }

        void SpeechEngineSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
			var rule = voiceRules.FirstOrDefault(g => e.Result.Grammar.Name == g.Name);
			if (rule == null)
			{
				speechSynthesizer.SpeakAsync("What was that?");
				return;
			}
			rule.Aknowledge(speechSynthesizer, e.Result);
			var table = rule.ToLua(e.Result);
			client.Say(TasClient.SayPlace.User, client.MyBattle.Founder.Name, "!transmit voice" + table.Replace("\n", ""), false);
        }
    }
}