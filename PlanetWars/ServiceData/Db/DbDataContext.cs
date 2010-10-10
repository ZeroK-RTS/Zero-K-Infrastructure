using System;
using System.Linq;

namespace ServiceData
{
	partial class DbDataContext
	{
		Config lastConfig;


		public Event1 AddEvent(EventType type, string message, params object[] args)
		{
			var e = new Event1() { EventType = (int)type, Text = string.Format(message, args), BattleTurn = GetConfig().CombatTurn, Time = DateTime.UtcNow };
			Events.InsertOnSubmit(e);
			return e;
		}

		public Config GetConfig()
		{
			if (lastConfig != null) return lastConfig;
			var c = Configs.FirstOrDefault();
			if (c == null) {
				c = new Config();
				Configs.InsertOnSubmit(c);
			}
			lastConfig = c;
			return c;
		}

		public Position GetCurrentPosition(CelestialObject body)
		{
			var conf = GetConfig();
			return body.GetPosition(conf.GameSecond, conf.SecondsPerTurn);
		}

		public Player GetPlayer(string login, string password)
		{
			return
				SpringAccounts.Where(x => x.Name == login && x.Password == SpringAccount.HashPassword(password) && x.Players != null && x.Players.IsActive).Select
					(x => x.Players).SingleOrDefault();
		}

		public void MarkDirty()
		{
			var conf = GetConfig();
			conf.DirtySecond = conf.GameSecond;
		}

	}
}