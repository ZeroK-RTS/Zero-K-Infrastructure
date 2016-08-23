// Contact: Jan Lichovník  licho@licho.eu, tel: +420 604 935 349,  www.itl.cz
// Last change by: licho  07.08.2016

using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public static class MapPicker
    {
        /// <summary>
        ///     Picks a map and writes a message if applicable
        /// </summary>
        /// <param name="context">The battle whose map needs selection</param>
        /// <remarks>
        ///     <para>
        ///         For Planetwars, picks the map given by the Planetwars matchmaker; else picks a featured map with the
        ///         appropriate tags
        ///     </para>
        ///     <para>For team and chickens games, picks a map of appropriate size based on current player count</para>
        ///     <para>
        ///         For FFA, prefer maps that have a number of boxes equal to player count, or at least a number of boxes that is
        ///         a multiple of player count
        ///     </para>
        /// </remarks>
        public static Resource GetRecommendedMap(BattleContext context)
        {
            var mode = context.GetMode();
            using (var db = new ZkDataContext())
            {
                List<Resource> list = null;
                var players = context.Players.Count(x => !x.IsSpectator);
                switch (mode)
                {
                    case AutohostMode.Teams:
                    case AutohostMode.None:
                        var ret =
                            db.Resources.Where(
                                x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsTeams != false && x.MapIsSpecial != true);
                        if (players > 11) ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth > 16*16);
                        else if (players > 8)
                            ret =
                                ret.Where(
                                    x =>
                                        x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth > 16*16 &&
                                        x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 24*24);
                        else if (players > 5) ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 24*24 || x.MapIs1v1 == true);
                        else ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 16*16 || x.MapIs1v1 == true);
                        list = ret.ToList();

                        break;
                    case AutohostMode.Game1v1:
                        list =
                            db.Resources.Where(
                                x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIs1v1 == true && x.MapIsSpecial != true).ToList();
                        break;
                    case AutohostMode.GameChickens:
                        ret =
                            db.Resources.Where(
                                x =>
                                    x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsSpecial != true &&
                                    (x.MapIsChickens == true || x.MapWaterLevel == 1));
                        if (players > 5) ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth > 16*16);
                        else if (players > 4)
                            ret =
                                ret.Where(
                                    x =>
                                        x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth > 16*16 &&
                                        x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 24*24);
                        else if (players > 2) ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 24*24 || x.MapIs1v1 == true);
                        else ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 16*16 || x.MapIs1v1 == true);
                        list = ret.ToList();

                        break;
                    case AutohostMode.GameFFA:
                        list =
                            db.Resources.Where(
                                x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true && x.MapFFAMaxTeams == players)
                                .ToList();
                        if (!list.Any())
                            list =
                                db.Resources.Where(
                                    x =>
                                        x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true &&
                                        (players%x.MapFFAMaxTeams == 0)).ToList();
                        if (!list.Any()) list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true).ToList();

                        break;
                }
                if (list != null)
                {
                    var r = new Random();
                    if (list.Count > 0)
                    {
                        var resource = list[r.Next(list.Count)];
                        return resource;
                    }
                }
            }
            return null;
        }

        
        public static IQueryable<Resource> FindResources(ResourceType resource, params string[] words)
        {
            var db = new ZkDataContext();
            string joinedWords = string.Join(" ", words);


            var ret = db.Resources.AsQueryable();
            ret = ret.Where(x => x.TypeID == resource);

            
            var test = ret.Where(x => x.InternalName == joinedWords);
            if (test.Any()) return test.OrderByDescending(x => -x.FeaturedOrder);
            int i;
            if (words.Length == 1 && int.TryParse(words[0], out i)) ret = ret.Where(x => x.ResourceID == i);
            else
            {
                foreach (var w in words)
                {
                    var w1 = w;
                    ret = ret.Where(x => SqlFunctions.PatIndex("%" + w1 + "%", x.InternalName) > 0);
                }
            }
            ret = ret.Where(x => x.ResourceContentFiles.Any(y => y.LinkCount > 0));
            return ret.OrderByDescending(x => -x.FeaturedOrder);
        }
    }
}