using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ZkData;

namespace System.Web.Mvc
{
	public enum StarType
	{
		RedStarSmall,
		GreenStarSmall,
		WhiteStarSmall
	}

	public static class HtmlHelperExtensions
	{
		public static string Stars(this HtmlHelper helper, StarType type, double? rating)
		{
			if (rating.HasValue) return string.Format("<span class='{0}' style='width:{1}px'></span>", type, rating *14);
			else return string.Format("<span class='WhiteStarSmall' style='width:70px'></span>");
		}

	}
}