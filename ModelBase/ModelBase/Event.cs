using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ModelBase
{
	public partial class Event
	{
		public static string TypeToText(EventType evt)
		{
			switch (evt) {
				case EventType.ModelAdded:
					return "added model";
				case EventType.ModelCommented:
					return "added comment to model";
				case EventType.ModelDeleted:
					return "deleted model";
				case EventType.ModelUpdated:
					return "updated model";
				case EventType.UnitAddded:
					return "added unit";
				case EventType.UnitCommented:
					return "added comment to unit";
				case EventType.UnitDeleted:
					return "deleted unit";
				case EventType.UnitUpdated:
					return "updated unit";
			}
			return evt.ToString();
		}

		public string Summary
		{
			get
			{
				return string.Format("{0} {1} {2}", User != null ? User.Login : "admin", TypeToText(Type), UrlName);
			}
		}

		public string SummaryLinked
		{
			get
			{
				return string.Format("{0} {1} <a href='{3}'>{2}</a>", User != null ? User.Login : "admin", TypeToText(Type), UrlName, Url);
			}
		}


		private string UrlName
		{
			get
			{
				if (Unit != null) return Unit.Game.Shortcut + " " + Unit.Name;
				if (Model != null) return Model.User.Login + "'s "  +Model.Name;
				return "link";
			}
		}
		public string Url
		{
			get
			{
				if (UnitID != null) return string.Format("{0}UnitDetail.aspx?UnitID={1}", Global.BaseUrl, UnitID);
				else if (ModelID != null) return string.Format("{0}ModelDetail.aspx?ModelID={1}", Global.BaseUrl, ModelID);
				return Global.BaseUrl;
			}
		}
	}
}
