using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using PlasmaShared;

namespace ZkData
{
  partial class Resource
  {
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
			get { return MapPlanetWarsIconSizeOverride ?? (MapWidth ?? 0 + MapHeight ?? 0)*2; } }

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


  }
}
