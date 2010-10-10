using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceData
{
	public static class Utils
	{
		public static string SecondsToTimeString(int seconds)
		{
			if (seconds < 60) return seconds + "s";
			var minutes = seconds/60;

			if (minutes < 60) return minutes + "m";
		
			var hours = minutes/60;
			minutes %= 60;

			return string.Format("{0:00}:{1:00}", hours, minutes);
		}
	}
}
