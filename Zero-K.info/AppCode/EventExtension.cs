using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb
{
	public static class EventExtension
	{	
		public static Event Create(string format, params object[] args)
		{
			var ev = new Event() { Time = DateTime.UtcNow };

			for (int i=0; i< args.Length; i++)
			{
				var arg = args[i];
				var url = new UrlHelper(HttpContext.Current.Request.RequestContext);

				if (arg is Account)
				{
					args[i] = HtmlHelperExtensions.PrintAccount(null, (Account)arg);
					ev.EventAccounts.Add(new EventAccount() { Account = (Account)arg});
				} else if (arg is Clan)
				{
					args[i] = HtmlHelperExtensions.PrintClan(null, (Clan)arg);
					ev.EventClans.Add(new EventClan() { Clan = (Clan)arg});
				} else if (arg is Planet)
				{
					args[i] = ((Planet)arg).Name; // todo proper html helper extension
					ev.EventPlanets.Add(new EventPlanet() {Planet =(Planet)arg });
				} else if (arg is SpringBattle)
				{
					var bat = (SpringBattle)arg;
					args[i] = string.Format("<a href='{0}'>B{1}</a>", url.Action("Detail", "Battles", new { id = bat.SpringBattleID }));   //todo no propoer helper for this
					ev.EventSpringBattles.Add(new EventSpringBattle() { SpringBattle = bat});
				}

			}
			ev.Text = string.Format(format, args);
			return ev;
		}

	}
}