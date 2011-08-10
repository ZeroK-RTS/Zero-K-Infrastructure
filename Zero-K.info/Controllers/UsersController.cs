using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
  public class UsersController: Controller
  {
    //
    // GET: /Users/

    public ActionResult Index()
    {
      return View("UserList");
    }
    
    public class UserDetail
    {
      public Account Account;
    }

      [Auth]
      public ActionResult ChangeLobbyID(int accountID, int? newLobbyID)
      {
          var db = new ZkDataContext();
          var account = db.Accounts.Single(x => x.AccountID == accountID);
          var oldLobbyID = account.LobbyID;
          account.LobbyID = newLobbyID;
          db.SubmitChanges();
          string response = string.Format("{0} lobby ID change from {1} -> {2}", account.Name, oldLobbyID, account.LobbyID);
          foreach (var duplicate in db.Accounts.Where(x => x.LobbyID == newLobbyID && x.AccountID != accountID)) {

              response += string.Format("\n Duplicate: {0} - {1} {2}", duplicate.Name, duplicate.AccountID, Url.Action("Detail", new { id = duplicate.AccountID }));
          }
          return Content(response);
      }

      public ActionResult Detail(int id)
      {
          var db = new ZkDataContext();
          return View("UserDetail", db.Accounts.FirstOrDefault(x => x.AccountID == id));
      }

      public ActionResult LobbyDetail(int id)
      {
          var db = new ZkDataContext();
          return View("UserDetail", db.Accounts.FirstOrDefault(x => x.LobbyID == id));
      }

  }
}