using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        /// <summary>
        /// Used to start a <see cref="Mission"/>, replay or such in ZKL or Weblobby
        /// </summary>
        /// <param name="link"></param>
        public async Task<ActionResult> SendCommand(string link) {
            if (Global.Account == null) return Content("You must be logged in to the site");
            if (!Global.Server.IsLobbyConnected(Global.Account.Name)) return Content("Your lobby program is not running");
            await Global.Server.SendSiteToLobbyCommand(Global.Account.Name, new SiteToLobbyCommand() { Command = link });
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
            Global.Server.GhostChanSay(AuthService.ModeratorChannel, str);
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
            Global.Server.GhostChanSay(AuthService.ModeratorChannel, str);
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
            Global.Server.GhostChanSay(AuthService.ModeratorChannel, str);
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
            Global.Server.GhostChanSay(AuthService.ModeratorChannel, str);
            return RedirectToAction("BlockedVPNs");
        }

    }
}
