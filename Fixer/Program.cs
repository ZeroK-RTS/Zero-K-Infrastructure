using System;
using System.Collections.Generic;
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
  public static class Program
  {
    static void Main(string[] args)
    {
      //ImportSpringiePlayers();
      //RecalculateBattleElo();
      //FixMaps();

    	//RandomizeMaps(9);
			GenerateStructures(9);

			//AddWormholes();
    }

		static void RandomizeMaps(int galaxyID)
		{
			using (var db = new ZkDataContext())
			{
				var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
				var maps = db.Resources.Where(x => x.FeaturedOrder > 0 && x.MapPlanetWarsIcon!=null).ToList().Shuffle();
				int cnt = 0;
				foreach (var p in gal.Planets)
				{
					p.MapResourceID = maps[cnt++].ResourceID;
				}
				db.SubmitChanges();
			}
		}

		public static void AddStruct(this Planet p, int structID)
		{
			p.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = structID });
		}

  	static void GenerateStructures(int galaxyID)
  	{
			var rand = new Random();
			var db = new ZkDataContext();
  		var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
  		var wormhole = 16;
  		var wormhole2 = 19;


  		var mine = 1;
  		var mine2 = 3;
  		var mine3 = 4;
  		var warp = 10;
  		var chicken = 20;
  		var dfac = 6;
  		var ddepot = 7;
  		int artefact = 9;
		
			/*
			567	Jumpjet/Specialist Plant
568	Screamer
569	Athena
570	Heavy Tank Factory
571	Airplane Plant
572	Krow
573	Bantha
574	Jugglenaut
575	Detriment
576	Singularity Reactor
577	Annihilator
578	Doomsday Machine
579	Behemoth
580	Starlight
581	Big Bertha
582	Goliath
583	Licho
584	Reef
585	Scythe
586	Panther
587	Black Dawn
588	Dominatrix
589	Newton
590	Shield Bot Factory
591	Silencer
592	Disco Rave Party*/

	
  		List<int> bannedStructures = new List<int>(){};// { 568, 577, 578, 584, 585, 586, 588, 589, 571, 590 };

  		var structs = db.StructureTypes.Where(x => x.Unlock != null && !bannedStructures.Contains(x.StructureTypeID));
  		List<Tuple<int, int>> costs = new List<Tuple<int, int>>();
			foreach (var s in structs)
			{
				costs.Add(Tuple.Create(5000 / s.Cost, s.StructureTypeID)); // probabality is relative to 1200-cost
			}
  		var sumCosts = costs.Sum(x => x.Item1);

  		foreach (var p in gal.Planets) {
				p.PlanetStructures.Clear();
				p.Name = Resources.names.Lines()[rand.Next(Resources.names.Lines().Length)];
  			//if (rand.Next(50) == 0 ) p.AddStruct(wormhole2);
				//else 
				if (rand.Next(10)<8) p.AddStruct(wormhole);

				//if (rand.Next(30) ==0) p.AddStruct(mine3);
				//else if (rand.Next(20)==0) p.AddStruct(mine2);
				//else 
				if (rand.Next(20) ==0) p.AddStruct(mine);

				if (rand.Next(20) == 0) p.AddStruct(dfac);
				if (rand.Next(20) == 0) p.AddStruct(ddepot);
				if (rand.Next(20) == 0) p.AddStruct(warp);

				if (p.Resource.MapIsChickens == true) p.AddStruct(chicken);

				// structures
				if (rand.Next(8) ==0)
				{

					var probe = rand.Next(sumCosts);
					foreach (var s in costs)
					{
						probe -= s.Item1;
						if (probe <= 0)
						{
							p.AddStruct(s.Item2);
							break;
						}
					}
				}
			}
			
			// artefacts
			foreach (var p in gal.Planets.Where(x => x.Resource.MapIsChickens!=true && !x.Resource.MapIsFfa != true && x.Resource.MapIs1v1 != true).Shuffle().Take(3)) p.AddStruct(artefact);

			db.SubmitChanges();
			
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
