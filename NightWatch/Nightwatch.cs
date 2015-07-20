#region using

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
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

        public AuthService Auth { get; private set; }


        public List<Battle> GetPlanetWarsBattles() {
            if (tas==null || tas.ExistingBattles == null) return new List<Battle>();
            else return tas.ExistingBattles.Values.Where(x => x.Founder.Name.StartsWith("PlanetWars")).ToList();
        }

        public List<Battle> GetPlanetBattles(Planet planet) {
            return GetPlanetWarsBattles().Where(x => x.MapName == planet.Resource.InternalName).ToList();
        }

        public Nightwatch()
		
        {
            tas = new TasClient("NightWatch");
        }


		public bool Start()
		{


            Auth = new AuthService(tas);


			return true;
		}

	}
}
