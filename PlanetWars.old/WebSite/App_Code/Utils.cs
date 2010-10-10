using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using PlanetWarsShared.UnitSyncLib;

/// <summary>
/// Summary description for Utils
/// </summary>
public static class Utils
{
    public static MapInfo GetMapInfoCached(string mapName) 
    {
        var cached = HttpContext.Current.Application["mi_" + mapName];
        if (cached != null) return (MapInfo)cached;
        MapInfo mi;
        try {
            mi = MapInfo.FromFile(HttpContext.Current.Server.MapPath(string.Format("mapinfo/{0}.xml", mapName)));
        }  catch
        {
            mi = new MapInfo();
        }
        HttpContext.Current.Application["mi_" + mapName] = mi;
        return mi;
    }

    public static Point GetImageDimensionsCached(string imageName)
    {
        var cached = HttpContext.Current.Application["di_" + imageName];
        if (cached != null) return (Point)cached;
        var im = Image.FromFile(HttpContext.Current.Server.MapPath(imageName));
        var dimensions = new Point(im.Width, im.Height);
        HttpContext.Current.Application["di_" + imageName] = dimensions;
        return dimensions;
    }
}
