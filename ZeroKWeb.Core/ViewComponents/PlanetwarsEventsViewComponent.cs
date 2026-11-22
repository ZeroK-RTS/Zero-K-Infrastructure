using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ZeroKWeb.Compat;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb.ViewComponents
{
    /// <summary>
    /// ASP.NET Core's replacement for <c>@Html.Action("Events", "Planetwars", ...)</c>.
    ///
    /// Third of seven, and the one with the most callers: six of the fourteen child-action call
    /// sites are this action, from Factions/Detail, Battles/BattleDetail, Clans/Detail,
    /// Planetwars/Galaxy, Planetwars/Planet and Shared/UserDetail. Each passes a different
    /// filter and nothing else, which is why every parameter here has a default - a view
    /// component is invoked with an anonymous object and the binder needs the rest to be optional.
    ///
    /// The body is <see cref="PlanetwarsController.Events"/>'s. Two notes on what did not change:
    ///
    /// - <c>Request.IsAjaxRequest()</c> is kept. A view component sees the parent request, which
    ///   is what a child action saw, so the question means the same thing and the answer comes
    ///   from the same header.
    /// - <c>factionID</c> is a parameter of the action but not a field of EventsResult, so the
    ///   view cannot round-trip it through its own form. That is the existing behaviour, faithful
    ///   rather than fixed: correcting it here would make the .NET 9 page behave differently from
    ///   the MVC 5 one, which is not this port's job.
    /// </summary>
    public class PlanetwarsEventsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int? planetID = null, int? accountID = null, int? springBattleID = null,
                                           int? clanID = null, int? factionID = null, string filter = null,
                                           int pageSize = 0, int page = 0, bool partial = false)
        {
            var db = new ZkDataContext();
            if (Request.IsAjaxRequest()) partial = true;
            if (pageSize == 0)
            {
                if (!partial) pageSize = 40;
                else pageSize = 10;
            }
            IQueryable<Event> res = db.Events.AsQueryable();
            if (planetID.HasValue) res = res.Where(x => x.Planets.Any(y => y.PlanetID == planetID));
            if (accountID.HasValue) res = res.Where(x => x.Accounts.Any(y => y.AccountID == accountID));
            if (clanID.HasValue) res = res.Where(x => x.Clans.Any(y => y.ClanID == clanID));
            if (springBattleID.HasValue) res = res.Where(x => x.SpringBattles.Any(y => y.SpringBattleID == springBattleID));
            if (factionID.HasValue) res = res.Where(x => x.Factions.Any(y => y.FactionID == factionID));
            if (!string.IsNullOrEmpty(filter)) res = res.Where(x => x.Text.Contains(filter));
            res = res.OrderByDescending(x => x.EventID);

            var ret = new EventsResult
                      {
                          PageCount = (res.Count() / pageSize) + 1,
                          Page = page,
                          Events = res.Skip(page * pageSize).Take(pageSize),
                          PlanetID = planetID,
                          AccountID = accountID,
                          SpringBattleID = springBattleID,
                          Filter = filter,
                          ClanID = clanID,
                          Partial = partial,
                          PageSize = pageSize
                      };

            return View("~/Views/Planetwars/Events.cshtml", ret);
        }
    }
}
