 using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using PlasmaShared;
using System.Text.RegularExpressions;

namespace ZkData
{
  partial class Resource
  {
    public double MapDiagonal
    {
      get { return Math.Sqrt((MapWidth*MapWidth + MapHeight*MapHeight)??0); }
    }
    
    public enum WaterLevel
    {
      Land = 1,
      Mixed = 2,
      Sea = 3
    }

    public enum Hill
    {
      Flat = 1,
      Hills = 2,
      Mountains = 3
    }

    partial void OnCreated()
    {
      LastChange = DateTime.UtcNow;
    }
    public double? MapRating
    {
      get
      {
        if (MapRatingCount > 0) return MapRatingSum/MapRatingCount;
        else return null;
      }
    }


  	public int PlanetWarsIconSize
  	{
			get { return (int)(25 + MapDiagonal); } }

    public Size ScaledImageSize(int maxSize)
    {
      var s = new Size();
      if (MapSizeRatio > 1)
      {
        s.Width = maxSize;
        s.Height = (int)(maxSize/MapSizeRatio);
      } else if (MapSizeRatio < 1)
      {
        s.Height = maxSize;
        s.Width = (int)(maxSize*MapSizeRatio);
      }
      else
      {
        s.Width = maxSize;
        s.Height = maxSize;
      }
      return s;
    }

    public string ThumbnailName
    {
      get { return string.Concat(InternalName.EscapePath() , ".thumbnail.jpg"); }
    }

    public string MinimapName
    {
      get { return string.Concat(InternalName.EscapePath(), ".minimap.jpg"); }
    }

    public string MetadataName
    {
      get { return string.Concat(InternalName.EscapePath(), ".metadata.xml.gz"); }
    }

    public string HeightmapName
    {
      get { return string.Concat(InternalName.EscapePath(), ".heightmap.jpg"); }
    }

    public string MetalmapName
    {
      get { return string.Concat(InternalName.EscapePath(), ".metalmap.jpg"); }
    }

    // TODO
    public string GetTagSubstring(string tagName)
    {
        if (MapTags == null) return null as string;
        Regex rex = new Regex(tagName + ":?" + "[^;]*;", RegexOptions.IgnoreCase);
        Match match = rex.Match(MapTags);
        String result = match.Value;
        if (!String.IsNullOrEmpty(result)) return result;
        return null as string;
    }

    public bool GetBooleanTag(string tagName)
    {
        return !String.IsNullOrEmpty(GetTagSubstring(tagName));
    }

    public int? GetIntTag(string tagName)
    {
        String substr = GetTagSubstring(tagName);
        if (String.IsNullOrEmpty(substr)) return null;
        string[] result = substr.Split(':');
        return int.Parse(result[1].Trim(';'));
    }

    public bool GetMapSupportedGameTypes(string tag)
    {
        return GetTagSubstring("gametypes").Contains(tag);
    }

    // FIXME: DB columns must be removed or renamed, or these auto properties renamed

    public bool MapIsSupported
    {
        get { return GetBooleanTag("supported"); }
    }
    public bool MapIs1v1
    {
        get { return GetMapSupportedGameTypes("1v1"); }
    }
    public bool MapIsTeams
    {
        get
        { return GetMapSupportedGameTypes("teams"); }
    }
    public bool MapIsFfa
    {
        get { return GetMapSupportedGameTypes("ffa"); }
    }
    public bool MapIsChickens
    {
        get { return GetMapSupportedGameTypes("chickens"); }
    }
    public bool MapIsSpecial
    {
        get { return GetBooleanTag("special"); }
    }
    public bool MapIsAsymmetrical
    {
        get { return GetBooleanTag("asymmetrical"); }
    }
    public int? MapFFAMaxTeams
    {
        get { return GetIntTag("ffaTeams"); }
    }
    public int? MapWaterLevel
    {
        get { return GetIntTag("water"); }
    }
    public int? MapHills
    {
        get { return GetIntTag("hills"); }
    }

    /*  // leave as its own DB column?
    public float? FeaturedOrder
    {
        get {
            String substr = GetTagSubstring("featuredOrder");
            if (String.IsNullOrEmpty(substr)) return null;
            string[] result = substr.Split(':');
            return float.Parse(result[1].Trim(';'));
        }
    }
    */



  }
}
