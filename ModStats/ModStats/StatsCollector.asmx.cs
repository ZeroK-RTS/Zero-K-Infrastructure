#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Services;

#endregion

namespace ModStats
{
	/// <summary>
	/// Summary description for StatsCollector
	/// </summary>
	[WebService(Namespace = "http://planet-wars.eu/ModStats")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
		// [System.Web.Script.Services.ScriptService]
	public class StatsCollector : WebService
	{
		#region Public methods

		[WebMethod]
		public void SubmitGame(string gameIDString, string mod,List<string> data)
		{
			SubmitGameEx(gameIDString, mod, null, data);
		}


		[WebMethod]
		public void SubmitGameEx(string gameIDString, string mod, string map, List<string> data)
		{
			if (Global.Db.Games.Any(x => x.SpringGameIDString == gameIDString)) return; // data for this game already submitted

			Game game = new Game();
			game.SpringGameIDString = gameIDString;
			game.Mod = mod;
			game.Created = DateTime.UtcNow;
			game.IP = GetUserIP();
			game.Map = map;

			string bname;
			double version;
			Global.ExtractNameAndVersion(mod, out bname, out version);
			game.Version = version;


			Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

			foreach (string line in data) {
				string[] parts = line.Split(',');
				switch (parts[0]) {
					case "teams":
						game.Players = Convert.ToInt32(parts[1]);
						game.Teams = Convert.ToInt32(parts[2]);
						break;

					case "unit":
						Unit unit = new Unit();
						unit.Unit1 = parts[1];
						unit.Cost = Convert.ToInt32(parts[2]);
						unit.Created = Convert.ToInt32(parts[3]);
						unit.Destroyed = Convert.ToInt32(parts[4]);
						unit.Health = Convert.ToInt32(parts[5]);
						game.Units.Add(unit);
						break;

					case "dmg":
						Damage damage = new Damage();
						damage.AttackerUnit = parts[1];
						damage.VictimUnit = parts[2];
						damage.Damage1 = Convert.ToDouble(parts[3]);
						damage.Paralyze = Convert.ToDouble(parts[4]);
						game.Damages.Add(damage);
						break;
					case "plist":
						game.PlayerList = line.Substring(6);
						break;
				}
			}
			if (game.Players <= 0 || game.Teams <= 0) return;
			var split = (game.PlayerList ?? "").Split(',');
			if (split.Count() != game.Players || split.Contains("Player")) return; // AI game or crap
			Global.Db.Games.InsertOnSubmit(game);
			Global.Db.SubmitChanges();
		}

		#endregion

		#region Other methods

		private string GetUserIP()
		{
			string ip = Context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
			if (string.IsNullOrEmpty(ip) || ip.Equals("unknown", StringComparison.OrdinalIgnoreCase)) ip = Context.Request.ServerVariables["REMOTE_ADDR"];
			return ip;
		}

		#endregion
	}
}