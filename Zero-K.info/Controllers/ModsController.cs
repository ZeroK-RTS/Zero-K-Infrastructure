using System;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class ModsController: Controller
    {
        public class GameModesModel
        {
            public string SearchName { get; set; }
            public bool? IsFeaturedFilter { get; set; } = true;

            public IQueryable<GameMode> Data;

            public void FillData(IQueryable<GameMode> source)
            {
                Data = source;

                if (!string.IsNullOrEmpty(SearchName)) Data = Data.Where(x => x.DisplayName.Contains(SearchName) || x.ShortName.Contains(SearchName));
                if (IsFeaturedFilter != null) Data = Data.Where(x => x.IsFeatured == IsFeaturedFilter);

                Data = Data.OrderByDescending(x => x.GameModeID);
            }
        }
        
        
        // GET
        public ActionResult Index(GameModesModel model)
        {
            model = model ?? new GameModesModel();
            
            var db = new ZkDataContext();
            model.FillData(db.GameModes.AsQueryable());
            
            return View("GameModesIndex", model);
        }

        [Auth]
        public ActionResult Edit(int? id)
        {
            var db = new ZkDataContext();
            var mode = new GameMode()
            {
                MaintainerAccountID = Global.AccountID,
            };
            if (id != null) mode = db.GameModes.FirstOrDefault(x=>x.GameModeID == id);


            return View("GameModeEdit", mode);
        }

        [Auth]
        public ActionResult EditSubmit(GameMode newGameMode)
        {
            if (!Global.IsModerator && newGameMode.MaintainerAccountID != Global.AccountID)
            {
                return RedirectToAction("NotLoggedIn", "Home"); // access denied
            }

            if (string.IsNullOrEmpty(newGameMode.DisplayName) || string.IsNullOrEmpty(newGameMode.ShortName) || string.IsNullOrEmpty(newGameMode.GameModeJson))
            {
                ViewBag.Error = "Please fill the fields properly";
                return View("GameModeEdit", newGameMode);
            }

            if (!Account.IsValidLobbyName(newGameMode.ShortName))
            {
                ViewBag.Error = "Please use only sane characters for game mode short name";
                return View("GameModeEdit", newGameMode);
            }

            if (newGameMode.DisplayName.Length > 250)
            {
                ViewBag.Error = "Shorten the name please";
                return View("GameModeEdit", newGameMode);
            }
            
            
            if (newGameMode.GameModeID == 0) // create new game mode
            {
                using (var db = new ZkDataContext())
                {
                    if (db.GameModes.Any(x =>
                        x.ShortName.ToLower() == newGameMode.ShortName.ToLower() || x.DisplayName.ToLower() == newGameMode.DisplayName.ToLower()))
                    {
                        ViewBag.Error = "This game mode already exist, edit it instead";
                        return View("GameModeEdit", newGameMode);
                    }

                    db.Entry(newGameMode).State = EntityState.Added;
                    newGameMode.Created = DateTime.UtcNow;
                    newGameMode.LastModified = DateTime.UtcNow;
                    if (!Global.IsModerator) newGameMode.IsFeatured = false;


                    db.SaveChanges();
                    return RedirectToAction("Index"); 
                }
            }
            else
            {  // edit existing game mode

                using (var db = new ZkDataContext())
                {
                    var existingMode = db.GameModes.First(x => x.GameModeID == newGameMode.GameModeID);

                    if (!Global.IsModerator && existingMode.MaintainerAccountID != Global.AccountID) return RedirectToAction("NotLoggedIn", "Home");

                    existingMode.DisplayName = newGameMode.DisplayName;
                    existingMode.GameModeJson = newGameMode.GameModeJson;
                    existingMode.LastModified = DateTime.UtcNow;
                    existingMode.ShortName = newGameMode.ShortName;

                    if (Global.IsModerator)
                    {
                        existingMode.ShortName = newGameMode.ShortName;
                        existingMode.MaintainerAccountID = newGameMode.MaintainerAccountID;
                        existingMode.IsFeatured = newGameMode.IsFeatured;
                    }

                    db.SaveChanges();
                    return RedirectToAction("Index");
                } 
            }
        }

        public ActionResult Detail(int id)
        {
            var db = new ZkDataContext();
            var gameMode = db.GameModes.Find(id);
            return View("GameModeDetail", gameMode);
        }

        public ActionResult Download(int id)
        {
            var db = new ZkDataContext();
            var gameMode = db.GameModes.Find(id);
            gameMode?.ForumThread?.UpdateLastRead(Global.AccountID, false);
            db.SaveChanges();
            return File(Encoding.UTF8.GetBytes(gameMode.GameModeJson), "application/json", $"{gameMode.ShortName}.json");
        }
    }
}