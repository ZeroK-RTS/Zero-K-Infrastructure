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
