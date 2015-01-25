#region using

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Timers;
using System.Web.Services.Description;
using System.Xml.Serialization;
using LobbyClient;
using NightWatch;
using ZkData;

#endregion

namespace CaTracker
{


    public class Nightwatch
    {
        TasClient tas;
        
        public TasClient Tas { get { return tas; } }
        public static Config config;
        AdminCommands adminCommands;
        OfflineMessages offlineMessages;
        PlayerMover playerMover;
        ChatRelay chatRelay;
        public PayPalInterface PayPalInterface { get; protected set; }

        public AuthService Auth { get; private set; }

        public NwSteamHandler SteamHandler { get; private set; }

        public List<Battle> GetPlanetWarsBattles() {
            if (tas==null || tas.ExistingBattles == null) return new List<Battle>();
            else return tas.ExistingBattles.Values.Where(x => x.Founder.Name.StartsWith("PlanetWars")).ToList();
        }

        public List<Battle> GetPlanetBattles(Planet planet) {
            return GetPlanetWarsBattles().Where(x => x.MapName == planet.Resource.InternalName).ToList();
        }

        public Nightwatch()
		
        {
            tas = new TasClient(null, "NightWatch", GlobalConst.ZkLobbyUserCpu);
			config = new Config();
            Trace.Listeners.Add(new NightwatchTraceListener(tas));
        }


		public bool Start()
		{
			tas.Connected += tas_Connected;
			tas.LoginAccepted += tas_LoginAccepted;

            Auth = new AuthService(tas);
            adminCommands = new AdminCommands(tas);
            offlineMessages = new OfflineMessages(tas);
            playerMover = new PlayerMover(tas);
            SteamHandler = new NwSteamHandler(tas, new Secrets().GetSteamWebApiKey());
            chatRelay = new ChatRelay(tas, new Secrets().GetNightwatchPassword(), new List<string>() { "zkdev", "sy", "moddev" }); 

		    PayPalInterface = new PayPalInterface();
		    PayPalInterface.Error += (e) =>
		        { tas.Say(TasClient.SayPlace.Channel, "zkdev", "PAYMENT ERROR: " + e.ToString(), true); };

		    PayPalInterface.NewContribution += (c) =>
		        {
		            tas.Say(TasClient.SayPlace.Channel,
		                    "zkdev",
		                    string.Format("WOOHOO! {0:d} New contribution of {1:F2}€ by {2} - for the jar {3}", c.Time, c.Euros, c.Name.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(), c.ContributionJar.Name),
		                    true);
		            if (c.AccountByAccountID == null)
		                tas.Say(TasClient.SayPlace.Channel,
		                        "zkdev",
                                string.Format("Warning, user account unknown yet, payment remains unassigned. If you know user name, please assign it manually {0}/Contributions", GlobalConst.BaseSiteUrl),
		                        true);
                    else tas.Say(TasClient.SayPlace.Channel,
                                "zkdev",
                                string.Format("It is {0} {2}/Users/Detail/{1}", c.AccountByAccountID.Name, c.AccountID, GlobalConst.BaseSiteUrl),
                                true);
		        };
            

			try
			{
				tas.Connect(config.ServerHost, config.ServerPort);
			}
			catch (Exception ex)
			{
                Trace.TraceError("{0}",ex);
			}


			return true;
		}

       
		void tas_Connected(object sender, TasEventArgs e)
		{
			tas.Login(config.AccountName, config.AccountPassword);
		}

		void tas_LoginAccepted(object sender, TasEventArgs e)
		{
			for (var i = 0; i < config.JoinChannels.Length; ++i) tas.JoinChannel(config.JoinChannels[i]);
		}
	}
}