using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.UI;
using Microsoft.Linq.Translations;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class LaddersController : Controller
    {
        //
        // GET: /Ladders/
        public ActionResult Index()
        {
            return View("Ladders", Global.AwardCalculator.GetAwards());
        }

        // GET: /Ladders/Full
        public ActionResult Full(LaddersIndexModel model)
        {
            model = model ?? new LaddersIndexModel();
            var db = new ZkDataContext();
            var ret = db.Accounts.Where(x => !x.IsDeleted && x.AccountRatings.Any(r => r.RatingCategory == model.RatingCategory && r.IsRanked)).AsQueryable();

            if (!string.IsNullOrEmpty(model.Name))
            {
                var termLower = model.Name.ToLower();
                ret = ret.Where(x => x.Name.ToLower().Contains(termLower) || x.SteamName.Contains(model.Name));
            }
            if (!string.IsNullOrEmpty(model.Country))
            {
                var termLower = model.Country.ToLower();
                ret = ret.Where(x => x.Country.ToLower().Contains(termLower));
            }

            if (model.RegisteredFrom.HasValue) ret = ret.Where(x => x.FirstLogin >= model.RegisteredFrom);
            if (model.RegisteredTo.HasValue) ret = ret.Where(x => x.FirstLogin <= model.RegisteredTo);

            if (model.LastLoginFrom.HasValue) ret = ret.Where(x => x.LastLogin >= model.LastLoginFrom);
            if (model.LastLoginTo.HasValue) ret = ret.Where(x => x.LastLogin <= model.LastLoginTo);

            if (model.LevelFrom.HasValue) ret = ret.Where(x => x.Level >= model.LevelFrom);
            if (model.LevelTo.HasValue) ret = ret.Where(x => x.Level <= model.LevelTo);

            model.Data = ret.OrderByDescending(x => x.AccountRatings.Where(r => r.RatingCategory == model.RatingCategory).FirstOrDefault().Elo).ToIndexedList().AsQueryable();
            
            return View("LaddersFull", model);
        }


        public class LaddersIndexModel
        {
            public RatingCategory RatingCategory { get; set; } = RatingCategory.Casual;
            public string Name { get; set; }
            public string Country { get; set; }
            public DateTime? RegisteredFrom { get; set; }
            public DateTime? RegisteredTo { get; set; }

            public DateTime? LastLoginFrom { get; set; }
            public DateTime? LastLoginTo { get; set; }

            public int? LevelFrom { get; set; } = 0;
            public int? LevelTo { get; set; } = 1000;

            public IQueryable<Indexed<Account>> Data;
        }
    }
}
