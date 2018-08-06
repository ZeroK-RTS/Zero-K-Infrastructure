using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Caching;
using ZkData;

namespace ZeroKWeb
{
	public class SpotlightHandler
	{
		public static UnitSpotlight GetRandom() {
		    var spotlights = MemCache.GetCached("spotlight", LoadSpotlights, 60*60*24);

            if (spotlights.Count > 0) return spotlights[new Random().Next(spotlights.Count)];
            else return new UnitSpotlight();
		}

	    static List<UnitSpotlight> LoadSpotlights() {
	        var s = new List<UnitSpotlight>();
	        try
	        {
	            var unitData = new WebClient().DownloadString("http://manual.zero-k.info/featured.txt");
	            s.AddRange(from line in unitData.Lines()
	                select line.Split('\t')
	                into parts
	                where parts.Length >= 4
	                select new UnitSpotlight() { Unitname = parts[0], Name = parts[1], Title = parts[2], Description = parts[3] });
	        }
	        catch (Exception ex)
	        {
	            Trace.TraceError("Error generating unit spotlight: {0}", ex);
	        }
	        return s;
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