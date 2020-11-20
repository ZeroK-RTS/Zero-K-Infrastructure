using System;
using System.Linq;
using System.Web.Mvc;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class GameModesController: Controller
    {
        public class GameModesModel
        {
            public string SearchName { get; set; }
            public bool? IsFeaturedFilter { get; set; }

            public IQueryable<GameMode> Data;
        }
        
        
        // GET
        public ActionResult Index(GameModesModel model)
        {
            model = model ?? new GameModesModel();
            
            
            return View("GameModesIndex", model);
        }
    }
}