using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ZeroKWeb;
using ZkData;

namespace System.Web.Mvc
{
	public enum StarType
	{
		RedStarSmall,
		GreenStarSmall,
		WhiteStarSmall,
		RedSkull,
		WhiteSkull
	}

	public class SelectOption
	{
		public string Name;
		public string Value;
	}


	public static class HtmlHelperExtensions
	{
		public static MvcHtmlString AccountAvatar(this HtmlHelper helper, int accountID)
		{
			var picList = (string[])HttpContext.Current.Application["unitpics"];
			return new MvcHtmlString(string.Format("<img src='/img/unitpics/{0}' class='avatar'>", Path.GetFileName(picList[accountID%picList.Length])));
		}

		public static MvcHtmlString AccountAvatar(this HtmlHelper helper, Account account)
		{
			return AccountAvatar(helper, account.AccountID);
		}

		public static MvcHtmlString BBCode(this HtmlHelper helper, string str)
		{
			if (str == null) return null;
			Regex exp;
			// format the bold tags: [b][/b]
			// becomes: <strong></strong>
			exp = new Regex(@"\[b\](.+?)\[/b\]");
			str = exp.Replace(str, "<strong>$1</strong>");

			// format the italic tags: [i][/i]
			// becomes: <em></em>
			exp = new Regex(@"\[i\](.+?)\[/i\]");
			str = exp.Replace(str, "<em>$1</em>");

			// format the underline tags: [u][/u]
			// becomes: <u></u>
			exp = new Regex(@"\[u\](.+?)\[/u\]");
			str = exp.Replace(str, "<u>$1</u>");

			// format the strike tags: [s][/s]
			// becomes: <strike></strike>
			exp = new Regex(@"\[s\](.+?)\[/s\]");
			str = exp.Replace(str, "<strike>$1</strike>");

			// format the url tags: [url=www.website.com]my site[/url]
			// becomes: <a href="www.website.com">my site</a>
			exp = new Regex(@"\[url\=([^\]]+)\]([^\]]+)\[/url\]");
			str = exp.Replace(str, "<a href=\"$1\">$2</a>");

			// format the img tags: [img]www.website.com/img/image.jpeg[/img]
			// becomes: <img src="www.website.com/img/image.jpeg" />
			exp = new Regex(@"\[img\]([^\]]+)\[/img\]");
			str = exp.Replace(str, "<img src=\"$1\" />");

			// format img tags with alt: [img=www.website.com/img/image.jpeg]this is the alt text[/img]
			// becomes: <img src="www.website.com/img/image.jpeg" alt="this is the alt text" />
			exp = new Regex(@"\[img\=([^\]]+)\]([^\]]+)\[/img\]");
			str = exp.Replace(str, "<img src=\"$1\" alt=\"$2\" />");

			//format the colour tags: [color=red][/color]
			// becomes: <font color="red"></font>
			// supports UK English and US English spelling of colour/color
			exp = new Regex(@"\[color\=([^\]]+)\]([^\]]+)\[/color\]");
			str = exp.Replace(str, "<font color=\"$1\">$2</font>");
			exp = new Regex(@"\[colour\=([^\]]+)\]([^\]]+)\[/colour\]");
			str = exp.Replace(str, "<font color=\"$1\">$2</font>");

			// format the size tags: [size=3][/size]
			// becomes: <font size="+3"></font>
			exp = new Regex(@"\[size\=([^\]]+)\]([^\]]+)\[/size\]");
			str = exp.Replace(str, "<font size=\"+$1\">$2</font>");

			str = Regex.Replace(str, @"((mailto|spring|http|https|ftp|ftps)\://\S+)", @"<a href='$1'>$1</a>");

			// lastly, replace any new line characters with <br />
			str = str.Replace("\r\n", "<br />\r\n");

			return new MvcHtmlString(str);
		}

		public static MvcHtmlString BoolSelect(this HtmlHelper helper, string name, bool? selected, string anyItem)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<select name='{0}'>", helper.Encode(name));
			if (anyItem != null) sb.AppendFormat("<option {1}>{0}</option>", helper.Encode(anyItem), selected == null ? "selected" : "");
			sb.AppendFormat("<option value='True' {0}>Yes</option>", selected == true ? "selected" : "");
			sb.AppendFormat("<option value='False' {0}>No</option>", selected == false ? "selected" : "");

			sb.Append("</select>");
			return new MvcHtmlString(sb.ToString());
		}

		public static MvcHtmlString IncludeFile(this HtmlHelper helper, string name)
		{
			if (name.StartsWith("http://"))
			{
				var ret = new WebClient().DownloadString(name);
				return new MvcHtmlString(ret);
			}
			else
			{
				var path = HttpContext.Current.Server.MapPath(name);
				return new MvcHtmlString(File.ReadAllText(path));
			}
		}

		public static MvcHtmlString IncludeWiki(this HtmlHelper helper, string node)
		{
			return new MvcHtmlString(WikiHandler.LoadWiki(node));
		}

		public static MvcHtmlString PrintAccount(this HtmlHelper helper, Account account, bool colorize = true)
		{
			if (account == null) return new MvcHtmlString("?");
			else
			{
				return
					new MvcHtmlString(
						string.Format(
							"<a href='/Users/{2}' style='color:{3}'><img src='/img/flags/{0}.png' class='flag'><img src='/img/ranks/{1}.png'  class='icon16'>{2}</a>",
							account.Country,
							account.LobbyTimeRank + 1,
							account.Name,
							colorize ? Clan.TreatyColor(Global.Clan, account.Clan): ""));
			}
		}


		public static MvcHtmlString PrintBattle(this HtmlHelper helper, SpringBattlePlayer battlePlayer)
		{
			var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
			var icon = "";
			if (battlePlayer.IsInVictoryTeam) icon = "battlewon.png";
			else if (battlePlayer.IsSpectator) icon = "spec.png";
			else icon = "battlelost.png";
			icon = string.Format("<img src='/img/battles/{0}'/>", icon);

			var battle = battlePlayer.SpringBattle;

			if (battle.IsMission) icon += " <img src='/img/battles/mission.png' alt='Mission' />";
			if (battle.HasBots) icon += " <img src='/img/battles/robot.png' alt='Bots' />";

			if (battle.BattleType == "Multiplayer") icon += " <img src='/img/battles/multiplayer.png' alt='Multiplayer' />";
			else if (battle.BattleType == "Singleplayer") icon += " <img src='/img/battles/singleplayer.png' alt='Singleplayer' />";

			return
				new MvcHtmlString(string.Format("<a href='{0}'>{4} B{1}</a> {2} on {3}",
				                                url.Action("Detail", "Battles", new { id = battle.SpringBattleID }),
				                                battle.SpringBattleID,
				                                battle.PlayerCount,
				                                PrintMap(helper, battle.ResourceByMapResourceID.InternalName),
				                                icon));
		}


		public static MvcHtmlString PrintClan(this HtmlHelper helper, Clan clan, bool colorize = true)
		{
			var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
			if (clan == null) return new MvcHtmlString(string.Format("<a href='{0}'>??</a>", url.Action("ClanList", "Planetwars")));
			{
				return
					new MvcHtmlString(string.Format("<a href='{0}'><img src='{1}' width='16'><span style='color:{2}'>{3}</span></a>",
					                                url.Action("Clan", "Planetwars", new { id = clan.ClanID }),
					                                clan.GetImageUrl(),
																					colorize ? Clan.TreatyColor(clan, Global.Clan):"",
					                                clan.Shortcut));
			}
		}

		public static MvcHtmlString PrintInfluence(this HtmlHelper helper, AccountPlanet accountPlanet)
		{
			return PrintInfluence(helper, accountPlanet.Account.Clan, accountPlanet.Influence, accountPlanet.ShadowInfluence);
		}

		public static MvcHtmlString PrintInfluence(this HtmlHelper helper, Clan clan, int influence, int shadowInfluence)
		{
			return
				new MvcHtmlString(string.Format("<span style='color:{0}'>{1}</span>&nbsp({2}&nbsp+&nbsp<span style='color:gray'>{3}</span>)",
																				Clan.TreatyColor(clan, Global.Clan),
																				influence + shadowInfluence,
																				influence,
																				shadowInfluence));
		}


		public static MvcHtmlString PrintLines(this HtmlHelper helper, string text)
		{
			return new MvcHtmlString(helper.Encode(text).Replace("\n", "<br/>"));
		}

		public static MvcHtmlString PrintLines(this HtmlHelper helper, IEnumerable<object> lines)
		{
			var sb = new StringBuilder();
			foreach (var line in lines) sb.AppendFormat("{0}<br/>", line);
			return new MvcHtmlString(sb.ToString());
		}

		public static MvcHtmlString PrintMap(this HtmlHelper helper, string name)
		{
			var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
			return new MvcHtmlString(string.Format("<a href='{0}' title='$map${1}'>{1}</a>", url.Action("DetailName", "Maps", new { name }), name));
		}

		public static MvcHtmlString PrintMetalCost(this HtmlHelper helper, int? cost)
		{
			const string metalIcon = "http://zero-k.googlecode.com/svn/trunk/mods/zk/LuaUI/Images/ibeam.png";
			return new MvcHtmlString(string.Format("<span style='color:#00FFFF;'>{0}<img src='{1}' width='20' height='20'/></span>", cost, metalIcon));
		}

		public static MvcHtmlString PrintPlanet(this HtmlHelper helper, Planet planet)
		{
			if (planet == null) return new MvcHtmlString("?");
			var url = new UrlHelper(HttpContext.Current.Request.RequestContext);
			return
				new MvcHtmlString(string.Format("<a href='{0}'><img src='/img/planets/{1}' width='{2}'>{3}</a>",
				                                url.Action("Planet", "Planetwars", new { id = planet.PlanetID }),
				                                planet.Resource.MapPlanetWarsIcon,
				                                planet.Resource.PlanetWarsIconSize/3,
				                                planet.Name));
		}

		public static MvcHtmlString Select(this HtmlHelper helper, string name, Type etype, int? selected, string anyItem)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<select name='{0}'>", helper.Encode(name));
			var names = Enum.GetNames(etype);
			var values = (int[])Enum.GetValues(etype);
			if (anyItem != null) sb.AppendFormat("<option {1}>{0}</option>", helper.Encode(anyItem), selected == null ? "selected" : "");
			for (var i = 0; i < names.Length; i++) sb.AppendFormat("<option value='{0}' {2}>{1}</option>", helper.Encode(values[i]), helper.Encode(names[i]), selected == values[i] ? "selected" : "");
			sb.Append("</select>");
			return new MvcHtmlString(sb.ToString());
		}


		public static MvcHtmlString Select(this HtmlHelper helper, string name, IEnumerable<SelectOption> items, string selected)
		{
			var sb = new StringBuilder();
			sb.AppendFormat("<select name='{0}'>", helper.Encode(name));
			foreach (var item in items)
				sb.AppendFormat("<option value='{0}' {2}>{1}</option>",
				                helper.Encode(item.Value),
				                helper.Encode(item.Name),
				                selected == item.Value ? "selected" : "");
			sb.Append("</select>");
			return new MvcHtmlString(sb.ToString());
		}

		public static MvcHtmlString Stars(this HtmlHelper helper, StarType type, double? rating)
		{
			if (rating.HasValue)
			{
				var totalWidth = 5*14;
				var starWidth = (int)(rating*14.0);
				return
					new MvcHtmlString(string.Format("<span title='{2:F2}'><span class='{0}' style='width:{1}px'></span><span style='width:{3}px'></span></span>",
					                                type,
					                                starWidth,
					                                rating,
					                                totalWidth - starWidth));
			}
			else
			{
				return
					new MvcHtmlString(string.Format("<span class='{0}' style='width:70px' title='No votes'></span>",
					                                type == StarType.RedSkull ? StarType.WhiteSkull : StarType.WhiteStarSmall));
			}
		}


		public static string ToAgoString(this DateTime? utcDate)
		{
			if (utcDate.HasValue) return ToAgoString(DateTime.UtcNow.Subtract(utcDate.Value));
			else return "";
		}

		public static string ToAgoString(this DateTime utcDate)
		{
			return ToAgoString(DateTime.UtcNow.Subtract(utcDate));
		}

		public static string ToAgoString(this TimeSpan timeSpan)
		{
			return string.Format("{0} ago", timeSpan.ToNiceString());
		}

		public static string ToNiceString(this TimeSpan timeSpan)
		{
			if (timeSpan.TotalMinutes < 2) return string.Format("{0} seconds", (int)timeSpan.TotalSeconds);
			if (timeSpan.TotalHours < 2) return string.Format("{0} minutes", (int)timeSpan.TotalMinutes);
			if (timeSpan.TotalDays < 2) return string.Format("{0} hours", (int)timeSpan.TotalHours);
			if (timeSpan.TotalDays < 60) return string.Format("{0} days", (int)timeSpan.TotalDays);
			return string.Format("{0} months", (int)(timeSpan.TotalDays/30));
		}
	}
}