using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PlasmaShared;
using Ratings;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb.ViewComponents
{
    /// <summary>
    /// ASP.NET Core's replacement for <c>@Html.Action("Ladder", "Planetwars")</c>.
    ///
    /// The second of seven, and the first that can be RUN: <c>Planetwars/Ladder.cshtml</c>
    /// compiles on .NET 9 as of the commit that linked PlanetwarsController, and the action
    /// carries no filters. ZeroKWeb.Render invokes this one by name, through the real
    /// IViewComponentHelper, and checks the HTML - see CheckViewComponents there.
    ///
    /// The body is <see cref="PlanetwarsController.Ladder"/>'s, unchanged apart from building
    /// the model itself rather than receiving one, and returning the view by full path. The
    /// MemCache call is kept: it is the action's behaviour, the key is the same, and dropping
    /// it here would make the .NET 9 page do more database work than the MVC 5 one.
    ///
    /// Its only call site is Galaxy.cshtml, which also calls Planetwars/MatchMaker and
    /// Planetwars/Events. Those two partials do not compile yet, so Galaxy cannot be diverged
    /// and this component has no caller in a view. It is exercised directly instead, which is
    /// the same thing the diverged view will do.
    /// </summary>
    public class PlanetwarsLadderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var ret = MemCache.GetCached("pwLadder",
                () =>
                {
                    ZkDataContext db = new ZkDataContext();
                    var gal = db.Galaxies.First(x => x.IsDefault);
                    DateTime minDate = gal.Started ?? DateTime.UtcNow;
                    List<PwLadder> items = db.Accounts.Where(x => x.FactionID != null && x.LastLogin > minDate && x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > minDate && !y.IsSpectator && y.SpringBattle.Mode == AutohostMode.Planetwars)).ToList().GroupBy(x => x.Faction)
                            .Select(
                                x =>
                                    new PwLadder
                                    {
                                        Faction = x.Key,
                                        Top10 =
                                            x.OrderByDescending(y => y.PwAttackPoints)
                                                .ThenByDescending(y => y.AccountRatings.Where(r => r.RatingCategory == RatingCategory.Planetwars).Select(r => r.LadderElo).DefaultIfEmpty(WholeHistoryRating.DefaultRating.RealElo).FirstOrDefault())
                                                .Take(10)
                                                .ToList()
                                    })
                            .ToList();
                    return items;
                },
                60 * 2);

            return View("~/Views/Planetwars/Ladder.cshtml", ret);
        }
    }
}
