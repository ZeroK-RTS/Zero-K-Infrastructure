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
        private static Random chickenRand = new Random();
        private static bool UseNormalMapForChickens { get { return chickenRand.Next(3) == 0; } }
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
        public static Resource GetRecommendedMap(LobbyHostingContext context)
        {
            var mode = context.Mode;
            using (var db = new ZkDataContext())
            {
                List<Resource> list = null;
                var humanPlayers = context.Players.Count(x => !x.IsSpectator);
                var botPlayers = context.Bots.Count;
                var allyteams = context.Players.Where(x => !x.IsSpectator).Select(p => p.AllyID).Union(context.Bots.Select(b => b.AllyID)).Distinct().Count();

                var level = context.IsMatchMakerGame ? MapSupportLevel.MatchMaker : MapSupportLevel.Featured;


                switch (mode) {
                    case AutohostMode.GameChickens:
                        if (!context.Bots.Any(b => b.IsChicken)) mode = AutohostMode.Teams;
                        break;

                    case AutohostMode.GameFFA:
                        allyteams = humanPlayers;
                        break;

                    case AutohostMode.None:
                        if (allyteams > 2) mode = AutohostMode.GameFFA;
                        if (context.Players.Where(x => !x.IsSpectator).Select(p => p.AllyID).Distinct().Count() == 1 && botPlayers > 0 && context.Bots.Any(b => b.IsChicken)) mode = AutohostMode.GameChickens;
                        if (humanPlayers == 2 && botPlayers == 0 && allyteams == 2) mode = AutohostMode.Game1v1;
                        break;
                }
                switch (mode)
                {
                    case AutohostMode.Teams:
                    case AutohostMode.None:
                        var ret =
                            db.Resources.Where(
                                x => x.TypeID == ResourceType.Map && x.MapSupportLevel >= level && x.MapIsTeams != false && x.MapIsSpecial != true);
                        if (humanPlayers > 11) ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth > 16*16);
                        else if (humanPlayers > 8)
                            ret =
                                ret.Where(
                                    x =>
                                        x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth > 16*16 &&
                                        x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 24*24);
                        else if (humanPlayers > 5) ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 24*24 || x.MapIs1v1 == true);
                        else ret = ret.Where(x => x.MapHeight*x.MapHeight + x.MapWidth*x.MapWidth <= 16*16 || x.MapIs1v1 == true);
                        list = ret.ToList();

                        break;
                    case AutohostMode.Game1v1:
                        list =
                            db.Resources.Where(
                                x => x.TypeID == ResourceType.Map && x.MapSupportLevel >= level && x.MapIs1v1 == true && x.MapIsSpecial != true).ToList();
                        break;
                    case AutohostMode.GameChickens:
                        if (UseNormalMapForChickens)
                        {
                            ret =
                            db.Resources.Where(
                                x =>
                                    x.TypeID == ResourceType.Map && x.MapSupportLevel >= level && x.MapIsSpecial != true &&
                                    (x.MapWaterLevel == 1));
                            if (humanPlayers > 5) ret = ret.Where(x => x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth > 16 * 16);
                            else if (humanPlayers > 4)
                                ret =
                                    ret.Where(
                                        x =>
                                            x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth > 16 * 16 &&
                                            x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth <= 24 * 24);
                            else if (humanPlayers > 2) ret = ret.Where(x => x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth <= 24 * 24 || x.MapIs1v1 == true);
                            else ret = ret.Where(x => x.MapHeight * x.MapHeight + x.MapWidth * x.MapWidth <= 16 * 16 || x.MapIs1v1 == true);
                        }else
                        {
                            ret =
                            db.Resources.Where(
                                x =>
                                    x.TypeID == ResourceType.Map && x.MapSupportLevel >= level && x.MapIsSpecial != true && x.MapIsChickens == true);
                        }
                        
                        list = ret.ToList();

                        break;
                    case AutohostMode.GameFFA:
                        list =
                            db.Resources.Where(
                                x => x.TypeID == ResourceType.Map && x.MapSupportLevel >= level && x.MapIsFfa == true && x.MapFFAMaxTeams == allyteams)
                                .ToList();
                        if (!list.Any())
                            list =
                                db.Resources.Where(
                                    x =>
                                        x.TypeID == ResourceType.Map && x.MapSupportLevel>=level && x.MapIsFfa == true &&
                                        (humanPlayers%x.MapFFAMaxTeams == 0)).ToList();
                        if (!list.Any()) list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.MapSupportLevel>=level && x.MapIsFfa == true).ToList();

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

        public static IQueryable<Resource> FindResources(ResourceType type, string[] words)
        {
            return FindResources(type, string.Join(" ", words));
        }

        public static IQueryable<Resource> FindResources(ResourceType type, string term, MapSupportLevel minimumSupportLevel = MapSupportLevel.None)
        {
            var db = new ZkDataContext();
            
            var ret = db.Resources.AsQueryable();
            ret = ret.Where(x => x.TypeID == type && x.MapSupportLevel >= minimumSupportLevel);

            int i;
            var words = term.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1 && int.TryParse(words[0], out i)) ret = ret.Where(x => x.ResourceID == i);
            else
            {
                foreach (var w in words)
                {
                    var w1 = w;
                    ret = ret.Where(x => SqlFunctions.PatIndex("%" + w1 + "%", x.InternalName) > 0);
                }
            }

            return ret.OrderByDescending(x => x.MapSupportLevel).ThenByDescending(x=>x.InternalName == term || x.RapidTag == term).ThenByDescending(x=>x.ResourceID);
        }
    }
}