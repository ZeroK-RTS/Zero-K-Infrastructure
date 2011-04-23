using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fixer
{
  class Program
  {
    static void Main(string[] args)
    {
      //ImportSpringiePlayers();
      //RecalculateBattleElo();
      //FixMaps();
			AddWormholes();
    }

		public static void AddWormholes()
		{
			var db = new ZkDataContext();
			var wormhole = db.StructureTypes.Where(x => x.EffectLinkStrength > 0).OrderBy(x => x.EffectLinkStrength).First();
			foreach (var p in db.Planets.Where(x => !x.PlanetStructures.Any(y => y.StructureType.EffectLinkStrength > 0)))
			{
				p.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = wormhole.StructureTypeID});
			}
			db.SubmitChanges();
		}

  	static void RecalculateBattleElo()
    {
			using (var db = new ZkDataContext())
			{
				foreach (var b in db.SpringBattles.Where(x => !x.IsEloProcessed).ToList())
				{
					Console.WriteLine(b.SpringBattleID);
					b.CalculateElo();
				}
				db.SubmitChanges();
			}
    }

    static void ImportSpringiePlayers()
    {
      var db = new ZkDataContext();
    	foreach (var line in File.ReadLines("springie.csv").AsParallel())
      {
        var m = Regex.Match(line, "\"[0-9]+\";\"([^\"]+)\";\"[0-9]+\";\"[0-9]+\";\"([^\"]+)\";\"([^\"]+)\"");
        if (m.Success)
        {
          string name = m.Groups[1].Value;
          double elo = double.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture);
          double w = double.Parse(m.Groups[3].Value,CultureInfo.InvariantCulture);
          if (elo != 1500 || w != 1)
          {
            foreach (var a in db.Accounts.Where(x => x.Name == name))
            {
              a.Elo = (float)elo;
              a.EloWeight = (float)w;
            }
            Console.WriteLine(name);
          }
        }

      }
      db.SubmitChanges();

    }

    static void FixMaps()
    {
      try
      {
        var db = new ZkDataContext();
        foreach (var r in db.Resources.Where(x=>x.LastChange == null)) r.LastChange = DateTime.UtcNow;
        db.SubmitChanges();
        return;

        foreach (var resource in db.Resources.Where(x => x.TypeID == ResourceType.Map ))//&&x.MapSizeSquared == null))
        {
          var file = String.Format("{0}/{1}.metadata.xml.gz", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());
          var map = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(File.ReadAllBytes(file).Decompress()));

          resource.MapWidth = map.Size.Width/512;
          resource.MapHeight = map.Size.Height/512;

          if (string.IsNullOrEmpty(resource.AuthorName))
          {
          
            if (!string.IsNullOrEmpty(map.Author)) resource.AuthorName = map.Author;
            else
            {
              Console.WriteLine("regex test");
              var m = Regex.Match(map.Description, "by ([\\w]+)", RegexOptions.IgnoreCase);
              if (m.Success) resource.AuthorName = m.Groups[1].Value;
            }
          }
          Console.WriteLine("author: " + resource.AuthorName);


          if (resource.MapIsSpecial == null) resource.MapIsSpecial = map.ExtractorRadius > 120 || map.MaxWind > 40;
          resource.MapSizeSquared = (map.Size.Width/512)*(map.Size.Height/512);
          resource.MapSizeRatio = (float)map.Size.Width/map.Size.Height;

          var minimap = String.Format("{0}/{1}.minimap.jpg", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());

          using (var im = Image.FromFile(minimap))
          {

            int w, h;

            if (resource.MapSizeRatio > 1)
            {
              w = 96;
              h = (int)(w/resource.MapSizeRatio);
            }
            else
            {
              h = 96;
              w = (int)(h*resource.MapSizeRatio);
            }

            using (var correctMinimap = new Bitmap(w, h, PixelFormat.Format24bppRgb))
            {
              using (var graphics = Graphics.FromImage(correctMinimap))
              {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(im, 0, 0, w, h);
              }

              var jgpEncoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
              var encoderParams = new EncoderParameters(1);
              encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

              var target = String.Format("{0}/{1}.thumbnail.jpg", @"d:\zero-k.info\www\Resources", resource.InternalName.EscapePath());
              correctMinimap.Save(target, jgpEncoder, encoderParams);
            }
          }
          Console.WriteLine(string.Format("{0}", resource.InternalName));
        }
        db.SubmitChanges();
      } catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }
  }
}
