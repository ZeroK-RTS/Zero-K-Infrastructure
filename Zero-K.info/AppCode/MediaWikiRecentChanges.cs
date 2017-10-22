using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;

namespace ZeroKWeb
{
	public class MediaWikiRecentChanges
	{
		public static List<MediaWikiEdit> cache = null;
		public static DateTime cacheTimestamp = DateTime.UtcNow;
		public const int CACHE_TIME = 10;

		public static List<MediaWikiEdit> LoadRecentChanges() {
			if (cache != null && cacheTimestamp > DateTime.UtcNow.AddMinutes(-CACHE_TIME))
				return cache;

			var edits = new List<MediaWikiEdit>();
			try
			{
				string query = new WebClient().DownloadString("https://zero-k.info/mediawiki/api.php?action=query&format=json&list=recentchanges&rcprop=title%7Cflags%7Cuser%7Ctimestamp&rcshow=!bot&rclimit=5&rctoponly=1");
				JObject queryJson = JObject.Parse(query);
				var recentChanges = queryJson["query"]["recentchanges"];
				foreach (JToken editJson in recentChanges)
				{
					MediaWikiEdit edit = new MediaWikiEdit{Title = (string)editJson["title"], Username = (string)editJson["user"]};
					DateTime timestamp = DateTime.Parse((string)editJson["timestamp"], null, System.Globalization.DateTimeStyles.RoundtripKind);
					edit.AgoString = HtmlHelperExtensions.ToAgoString(timestamp);

					edits.Add(edit);
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("Error generating MediaWiki recent changes: {0}", ex);
				throw ex;
			}

			cache = edits;
			cacheTimestamp = DateTime.UtcNow;
			return edits;
		}

		public class MediaWikiEdit
		{
			public string Title;
			public string Username;
			public string AgoString;
		}
	}
}