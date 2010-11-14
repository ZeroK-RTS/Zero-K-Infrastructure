using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlasmaShared;

namespace ZkData
{
  partial class Resource
  {
    partial void OnCreated()
    {
      LastChange = DateTime.UtcNow;
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
