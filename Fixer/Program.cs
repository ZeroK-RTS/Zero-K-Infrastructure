using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fixer
{
  public static class Program
  {

      public static void FixHashes() {
          var db = new ZkDataContext();
          var lo = new DataLoadOptions();
          lo.LoadWith<Resource>(x=>x.ResourceSpringHashes);
          db.LoadOptions = lo;
          foreach (var r in db.Resources) {
              var h84 = r.ResourceSpringHashes.Where(x => x.SpringVersion == "84").Select(x => x.SpringHash).SingleOrDefault();
              var h840 = r.ResourceSpringHashes.Where(x => x.SpringVersion == "84.0").Select(x => x.SpringHash).SingleOrDefault();

              if (h84 != h840) {
                  var entry = r.ResourceSpringHashes.SingleOrDefault(x => x.SpringVersion == "84.0");
                  if (h84 != 0)
                  {
                        if (entry == null)
                      {
                          entry = new ResourceSpringHash() { SpringVersion = "84.0" };
                          r.ResourceSpringHashes.Add(entry);
                      }
                      entry.SpringHash = h84;
                  }
                  else {
                      if (entry != null) db.ResourceSpringHashes.DeleteOnSubmit(entry);
                  }
              }
            }
          db.SubmitChanges();
      }


      public static void SetFFATeams() {
          var db = new ZkDataContext();
          foreach (var m in db.Resources.Where(x => x.FeaturedOrder != null && x.TypeID == ResourceType.Map)) {
              var lg = m.SpringBattlesByMapResourceID.Take(100).ToList();
              double cnt = lg.Count;
              if (cnt == 0) continue; ;
              if (lg.Count(x => x.HasBots) / (double)cnt > 0.5)
              {
                  m.MapIsChickens = true;
              }
              
                  if (lg.Count(x => x.PlayerCount == 2) / cnt > 0.4)
                  {
                      m.MapIs1v1 = true;
                  }
              
                      var teams = m.SpringBattlesByMapResourceID.Take(100).GroupBy(x => x.SpringBattlePlayers.Where(y => !y.IsSpectator).Select(y => y.AllyNumber).Distinct().Count()).OrderByDescending(x => x.Count()).Select(x => x.Key).FirstOrDefault();
                      if (teams > 2)
                      {
                          m.MapIsFfa = true;
                          m.MapFFAMaxTeams = teams;
                      }
                      else
                      {
                      }

              

          }
          db.SubmitChanges();

      }


      public static void FixDemoFiles() {
          var db = new ZkDataContext();
          foreach (var sb in db.SpringBattles) {
              //sb.ReplayFileName = sb.ReplayFileName.Replace("http://springdemos.licho.eu/","http://zero-k.info/replays/");
          }
          //db.SubmitChanges();

      }


      static void Main(string[] args)
    {
        var nw = new CaTracker.Nightwatch(@"c:\temp");
        nw.Start();
        Console.ReadLine();
          
          

          //FixDemoEngineVersion();

      //ImportSpringiePlayers();
      //RecalculateBattleElo();
      //FixMaps();

        //PickHomworldOwners();

		//PurgeGalaxy(21, false);
        //RandomizeMaps(21);
		//GenerateStructures(21);

			//AddWormholes();
        //TestPrediction();
    }

      static void PickHomworldOwners()
      {

          var db = new ZkDataContext();
          foreach (var a in db.Accounts.Where(x=>x.ClanID != null && x.Clan.FactionID != x.FactionID)) {
              a.FactionID = a.Clan.FactionID;
          }
          db.SubmitChanges();


          db.ExecuteCommand("update clan set homeworldplanetid=null");
          foreach (var f in db.SpringBattles.Where(x => x.StartTime >= DateTime.Now.AddDays(-14)).SelectMany(x => x.SpringBattlePlayers).Where(x=>x.Account.Clan!= null).GroupBy(x => x.Account.Clan.Faction)) {
              foreach (var topclan in f.GroupBy(x=>x.Account.Clan).OrderByDescending(x => x.Count()).Take(4))
              {
                  topclan.Key.CanMakeHomeworld = true;
                  Console.WriteLine("{0}  :   {1}", f.Key.Name, topclan.Key.ClanName);

              }
          
          }
          db.SubmitChanges();
      }

      static void FixDemoEngineVersion()
      {
          var db = new ZkDataContext();
          foreach (var b in db.SpringBattles.Where(x=>x.Title.Contains("[engine"))) {
              var match = Regex.Match(b.Title, @"\[engine([^\]]+)\]");
              if (match.Success) {
                  var eng = match.Groups[1].Value;
                  if (eng != b.EngineVersion) b.EngineVersion = eng;
              }
          }
          db.SubmitChanges();
      }


      public static void TestPrediction() {
        var db = new ZkDataContext();
        var factWinCnt= 0;
        var factWinSum = 0.0;
        var predWinSum = 0.0;
        var predWinCnt = 0;
        var err = 0.0;
        var i = 0;
        var nsum = 0.0;
        foreach (var sb in db.SpringBattles.Where(x=>!x.IsMission && !x.HasBots && !x.IsFfa && x.IsEloProcessed && x.EventSpringBattles.Any()).OrderByDescending(x => x.SpringBattleID)) {

            var losers = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();
            var winners = sb.SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();

            if (losers.Count == 0 || winners.Count == 0 || losers.Count == winners.Count) continue;

            var winnerEloSum = winners.Sum(x => x.Account.EffectiveElo);
            var loserEloSum = losers.Sum(x => x.Account.EffectiveElo);

            /*if (winners.Count > losers.Count) loserEloSum += loserEloSum / losers.Count;
            else if (losers.Count > winners.Count) winnerEloSum += winnerEloSum / winners.Count;

            var ts = Math.Sqrt(Math.Max(winners.Count, losers.Count));
            var winnerElo = winnerEloSum / ts;
            var loserElo = loserEloSum / ts;*/


            var winnerElo = 1.0;
            foreach (var w in winners) winnerElo *= w.Account.EffectiveElo;
            winnerElo = Math.Pow(winnerElo, 1.0 / winners.Count);

            var loserElo = 1.0;
            foreach (var l in losers) loserElo *= l.Account.EffectiveElo;
            loserElo = Math.Pow(loserElo, 1.0 / losers.Count);

            //var winnerElo = winnerEloSum / Math.Sqrt(winners.Count);
            //var loserElo = loserEloSum / Math.Sqrt(losers.Count);

            //var winnerElo = winnerEloSum / winners.Count;
            //var loserElo = loserEloSum / losers.Count;

            var eWin = 1 / (1 + Math.Pow(10, (loserElo - winnerElo) / 400));
            var eLose = 1 / (1 + Math.Pow(10, (winnerElo - loserElo) / 400));

            if (eWin > eLose)
            {
                factWinSum += eWin * 100.0;
                nsum += eWin * 100;
                factWinCnt++;
            }
            else {
                err += Math.Abs(eWin - eLose);
                predWinSum += eWin * 100.0;
                nsum += eLose * 100;
                predWinCnt++;
            }
            i++;
            if (i >= 100) break;
        }
        var fwp = factWinSum / factWinCnt;
        var pwp = predWinSum / predWinCnt;
        var cnt = factWinCnt + predWinCnt;
        var ns = nsum / cnt;
        var predGood = 100.0*factWinCnt / cnt;

        Console.WriteLine("fwin: {0},  ns: {1} factwin%: {2}  predwin%: {3}: err: {4}", predGood, ns,fwp ,pwp, err );

    }

      public static void PurgeGalaxy(int galaxyID, bool resetclans = false, bool setPlanetOwners = false) {
			using (var db = new ZkDataContext())
			{
				db.CommandTimeout = 300;

                if (setPlanetOwners && db.AccountPlanets.Any(x => x.Influence > 0))
                {
                    db.ExecuteCommand("update clan set canmakehomeworld=0");
                    foreach (
                        var clan in
                            db.Clans.Where(x => !x.IsDeleted).OrderByDescending(
                                x =>
                                x.Accounts.SelectMany(y => y.AccountPlanets).Sum(y => y.Influence + y.ShadowInfluence)*15 +
                                x.Accounts.Sum(y => y.Credits)).Take(20))
                    {
                        clan.CanMakeHomeworld = true;
                    }
                    db.SubmitChanges();
                }

			    var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
                foreach (var p in gal.Planets)
                {
                    p.ForumThread = null;
                    p.OwnerAccountID = null;
                }
                db.SubmitChanges();

                db.ExecuteCommand("update account set dropshipcount=1, credits=0, wasgivencredits=0");
                db.ExecuteCommand("update clan set homeworldplanetid=null");
                if (resetclans) db.ExecuteCommand("update account set clanid=null,isclanfounder=0, hasclanrights=0");
				db.ExecuteCommand("delete from event");
				db.ExecuteCommand("delete from planetinfluencehistory");
				db.ExecuteCommand("delete from planetownerhistory");
				db.ExecuteCommand("delete from accountplanet");
				db.ExecuteCommand("delete from marketoffer");
				db.ExecuteCommand("delete from treatyoffer");
				
				db.ExecuteCommand("delete from forumthread where forumcategoryid={0}", db.ForumCategories.Single(x => x.IsPlanets).ForumCategoryID);

                if (resetclans)
                {
                    db.ExecuteCommand("delete from clan");
                    db.ExecuteCommand("delete from forumthread where forumcategoryid={0}", db.ForumCategories.Single(x => x.IsClans).ForumCategoryID);
                }
			}
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
				gal.Turn = 0;
				gal.Started = DateTime.UtcNow;
				gal.IsDirty = true;
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
			var names = Resources.names.Lines().ToList();

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
			int militia = 594;
		
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
				p.Name = names[rand.Next(names.Count)];
				names.Remove(p.Name);
  			//if (rand.Next(50) == 0 ) p.AddStruct(wormhole2);
				//else 
				//if (rand.Next(10)<8) 
					p.AddStruct(wormhole);
					p.AddStruct(militia);

				//if (rand.Next(30) ==0) p.AddStruct(mine3);
				//else if (rand.Next(20)==0) p.AddStruct(mine2);
				//else 
				//if (rand.Next(20) ==0) p.AddStruct(mine);

				//if (rand.Next(20) == 0) p.AddStruct(dfac);
				//if (rand.Next(20) == 0) p.AddStruct(ddepot);
				//if (rand.Next(20) == 0) p.AddStruct(warp);

				if (p.Resource.MapIsChickens == true) p.AddStruct(chicken);

				// tech structures
				/*if (rand.Next(8) ==0)
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
				}*/
			}
			
			// artefacts
			foreach (var p in gal.Planets.Where(x => x.Resource.MapIsChickens!=true && !x.Resource.MapIsFfa != true && x.Resource.MapIs1v1 != true).Shuffle().Take(5)) p.AddStruct(artefact);

			// jump gates
			//foreach (var p in gal.Planets.Shuffle().Take(6)) p.AddStruct(warp);

			db.SubmitChanges();
			Galaxy.RecalculateShadowInfluence(db);
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
