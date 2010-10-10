using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ModelBase
{
	public static class Global
	{
		public static string BaseUrl = "http://planet-wars.eu/ModelBase/";

		public static DatabaseDataContext Db
		{
			get
			{
				return DataContextFactory.GetScopedDataContext<DatabaseDataContext>();
			}
		}

		public static string PrintTags(IEnumerable<ModelTag> tags)
		{
			var sb = new StringBuilder();
			int cnt = 0;
			foreach (var m in tags.OrderBy(x=>x.Tag.Name))
			{
				if (cnt++ > 0) sb.Append(", ");
				sb.Append(m.Tag.Name);
			}
			return sb.ToString();
		}


		public static int? LoggedUserID
		{
			get
			{
                if (HttpContext.Current == null) return null;
				return HttpContext.Current.Session["UserID"] as int?;
			}
			set
			{
				HttpContext.Current.Session["UserID"] = value;
			}
		}

		public static User LoggedUser
		{
			get
			{
				return Global.Db.Users.SingleOrDefault(x => x.UserID == LoggedUserID);
			}
		}


		public static Color GenColor(int value, int max)
		{
			if (max <= 0) max = 1;
			if (value < 0 || value > max) value = 0;
			return Color.FromArgb((int)(255*((max - value)/(double)max)), (int)(255*(value/(double)max)), 0);
		}

		public static Color GenColor(int value)
		{
			return GenColor(value, 100);
		}


		public static string Linkify(string html)
		{
			if (html == null) return null;
			var ret = Regex.Replace(html, @"(\bhttp://[^ ]+\b)", @"<a href=""$0"">$0</a>");

			return ret;
		}


		public static void AddEvent(EventType evtype, int? commentID, int? unitID, int? modelID, string text)
		{
			AddEvent(evtype, commentID, unitID, modelID, text, 0);
		}

		public static void AddEvent(EventType evtype, int? commentID, int? unitID, int? modelID, string text, int userID)
		{
			var e = new Event();
			if (userID == 0) e.UserID = LoggedUserID; else e.UserID = userID;
			e.Type = evtype;
			e.CommentID = commentID;
			e.UnitID = unitID;
			e.ModelID = modelID;
			e.Text = text;
			e.Time = DateTime.UtcNow;
			Db.Events.InsertOnSubmit(e);
		}

	}
}
