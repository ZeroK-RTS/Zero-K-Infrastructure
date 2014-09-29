using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LobbyClient;
using ZkData;
using NightWatch;

namespace ZeroKWeb.Controllers
{
    public class LobbyController : Controller
    {
        //
        // GET: /Lobby/

        // should this use [Auth] as well?
        public ActionResult SendCommand(string link) {
            if (Global.Account == null) return Content("You must be logged in to the site");
            var name = Global.Account.Name;
            var tas = Global.Nightwatch.Tas;
            if (!tas.ExistingUsers.ContainsKey(name)) return Content("You need to start your lobby program first - Zero-K lobby or WebLobby");
            Global.Nightwatch.Tas.Extensions.SendJsonData(name, new ProtocolExtension.SiteToLobbyCommand{SpringLink = link});
            return Content("");
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult BlockedVPNs()
        {
            return View("BlockedVPNs");
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult AddBlockedCompany(string companyName, string comment)
        {
            ZkDataContext db = new ZkDataContext();
            if (String.IsNullOrWhiteSpace(companyName)) return Content("Company name cannot be empty");
            db.BlockedCompanies.InsertOnSubmit(new BlockedCompany()
            {
                CompanyName = companyName,
                Comment = comment,
            });
            db.SubmitChanges();

            var str = string.Format("{0} added new blocked VPN company: {1}", Global.Account.Name, companyName);
            Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel, str, true);
            return  RedirectToAction("BlockedVPNs");
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult AddBlockedHost(string hostname, string comment)
        {
            ZkDataContext db = new ZkDataContext();
            if (String.IsNullOrWhiteSpace(hostname)) return Content("Hostname cannot be empty");
            db.BlockedHosts.InsertOnSubmit(new BlockedHost()
            {
                HostName = hostname,
                Comment = comment,
            });
            db.SubmitChanges();

            var str = string.Format("{0} added new blocked VPN host: {1}", Global.Account.Name, hostname);
            Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel, str, true);
            return RedirectToAction("BlockedVPNs");
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult RemoveBlockedCompany(int companyID)
        {
            ZkDataContext db = new ZkDataContext();
            BlockedCompany todel = db.BlockedCompanies.First(x => x.CompanyID == companyID);
            string name = todel.CompanyName;
            db.BlockedCompanies.DeleteOnSubmit(todel);
            db.SubmitAndMergeChanges();
            var str = string.Format("{0} removed blocked VPN company: {1}", Global.Account.Name, name);
            Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel, str, true);
            return RedirectToAction("BlockedVPNs");
        }

        [Auth(Role = AuthRole.LobbyAdmin | AuthRole.ZkAdmin)]
        public ActionResult RemoveBlockedHost(int hostID)
        {
            ZkDataContext db = new ZkDataContext();
            BlockedHost todel = db.BlockedHosts.First(x => x.HostID == hostID);
            string name = todel.HostName;
            db.BlockedHosts.DeleteOnSubmit(todel);
            db.SubmitAndMergeChanges();
            var str = string.Format("{0} removed blocked VPN host: {1}", Global.Account.Name, name);
            Global.Nightwatch.Tas.Say(TasClient.SayPlace.Channel, AuthService.ModeratorChannel, str, true);
            return RedirectToAction("BlockedVPNs");
        }

    }
}
