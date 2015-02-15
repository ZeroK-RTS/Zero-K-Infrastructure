using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web;
using System.Web.Caching;
using ZkData;

namespace ZeroKWeb
{
	public class SpotlightHandler
	{
		public static UnitSpotlight GetRandom()
		{
			var spotlights = HttpContext.Current.Cache["spotlight"] as List<UnitSpotlight>;
			if (spotlights == null)
			{
				spotlights = new List<UnitSpotlight>();
				try
				{
					var unitData = new WebClient().DownloadString("http://packages.springrts.com/zkmanual/featured.txt");
					foreach (var line in unitData.Lines())
					{
						var parts = line.Split('\t');
					    if (parts.Length >= 4)
					    {
					        var spotlight = new UnitSpotlight() { Unitname = parts[0], Name = parts[1], Title = parts[2], Description = parts[3] };
					        spotlights.Add(spotlight);
					    } 
					}
				}
				catch (Exception ex)
				{
					Trace.TraceError("Error generating unit spotlight: {0}", ex);
				}
				HttpContext.Current.Cache.Insert("spotlight", spotlights, null, DateTime.UtcNow.AddHours(1), Cache.NoSlidingExpiration);
			}
            if (spotlights.Count > 0) return spotlights[new Random().Next(spotlights.Count)];
            else return new UnitSpotlight();
			
		}

		public class UnitSpotlight
		{
			public string Description;
			public string Name;
			public string Title;
			public string Unitname;
		}
	}
}