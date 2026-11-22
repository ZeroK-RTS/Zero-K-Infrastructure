using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using Microsoft.AspNetCore.Mvc;
using ZeroKWeb.Controllers;
using ZkData;

namespace ZeroKWeb.ViewComponents
{
    /// <summary>
    /// ASP.NET Core's replacement for <c>Html.RenderAction("ChatNotification", "Lobby")</c> in
    /// TopMenu.cshtml.
    ///
    /// **This one is not optional, and authentication is what made it urgent.** TopMenu is part
    /// of the layout, so it is on every page, and its call is guarded by
    /// `Global.IsAccountAuthorized`. While nobody could sign in, the guard was never true and the
    /// tripwire in ChildActionCompat never fired. The moment signing in worked, the same URL
    /// answered 200 anonymous and 500 signed in - every page, because the layout threw.
    ///
    /// ChildActionCompat has said since it was written that TopMenu's guard "is why the site
    /// layout can render at all before this is resolved". That was exactly right, and it stopped
    /// being true the day identity started working.
    ///
    /// Second of the three [Auth] child actions. The check is explicit for the same reason as
    /// PlanetwarsMatchMaker: a view component inherits nothing from a filter pipeline. Here the
    /// caller guards as well, so this is the inner of two - and it is the one that holds if the
    /// view's guard is ever edited away.
    ///
    /// The body is LobbyController.ChatNotification's, including its two details that look like
    /// accidents and are not: the whole read is wrapped in a try/catch that traces and continues,
    /// because a chat-history timeout must not take the layout down with it, and LastChatRead is
    /// advanced in a SEPARATE context afterwards, so it is written even when the read failed.
    /// </summary>
    public class LobbyChatNotificationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            // [Auth], made explicit. TopMenu guards this too; the duplication is deliberate.
            if (Global.Account == null) return Content("");

            var model = new LobbyController.ChatModel();

            try
            {
                using (var db = new ZkDataContext())
                {
                    db.Database.SetCommandTimeoutCompat(5);
                    var acc = db.Accounts.Where(x => x.AccountID == Global.AccountID).First();
                    var ret = db.LobbyChatHistories.AsQueryable();
                    ret = ret.Where(x => x.Target == Global.Account.Name && x.SayPlace == SayPlace.User && x.Time > acc.LastChatRead);
                    if (ret.Count() != 0)
                    {
                        var ignoredIds = db.AccountRelations.Where(x => (x.Relation == Relation.Ignore) && (x.OwnerAccountID == acc.AccountID)).Select(x => x.TargetAccountID).ToList();
                        var ignoredNames = db.Accounts.Where(x => ignoredIds.Contains(x.AccountID)).Select(x => x.Name).ToHashSet();
                        model.Data = ret.OrderByDescending(x => x.Time).ToList().Where(x => !ignoredNames.Contains(x.User)).AsQueryable();
                        model.Channel = "";
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error loading chat notifications for {Global.Account.Name}: {ex.Message}\n{ex.StackTrace}");
            }

            using (var db = new ZkDataContext())
            {
                var acc = db.Accounts.Where(x => x.AccountID == Global.AccountID).First();
                acc.LastChatRead = DateTime.UtcNow;
                db.SaveChanges();
            }

            return View("~/Views/Shared/ChatNotification.cshtml", model);
        }
    }
}
