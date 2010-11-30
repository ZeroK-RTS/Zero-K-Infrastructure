using System;
using System.Text.RegularExpressions;

namespace Floe.UI
{
	public static class Constants
	{
		private static Regex urlRegex = new Regex(@"(www\.|(http|https|ftp)+\:\/\/)[^\s]+", RegexOptions.IgnoreCase);

		public static Regex UrlRegex { get { return urlRegex; } }
	}
}
