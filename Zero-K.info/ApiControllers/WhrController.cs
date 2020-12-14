using LobbyClient;
using PlasmaShared;
using Ratings;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class WhrController : ApiController
    {
        [HttpPost]
        [Route("api/whr/battles")]
        public IEnumerable<BattleModel> Get([FromBody]BattleRequest request)
        {
            using (var db = new ZkDataContext())
            {
                return db.SpringBattles.Where(x => request.battleIds.Contains(x.SpringBattleID)).Include(x => x.SpringBattlePlayers).ToList().Select(bat => new BattleModel(bat)).ToList();
            }
        }

        public class BattleRequest
        {
            public int[] battleIds { get; set; }
        }

        public class BattleModel
        {
            public class PlayerModel
            {
                public float? rating { get; set; }
                public float? stdev { get; set; }
                public int accountId { get; set; }
            }

            public List<PlayerModel> players { get; set; }
            public int id { get; set; }

            public BattleModel(SpringBattle bat)
            {
                WholeHistoryRating whr = RatingSystems.GetRatingSystem(bat.GetRatingCategory()) as WholeHistoryRating;
                players = bat.SpringBattlePlayers.Where(x => !x.IsSpectator).Select(player => new PlayerModel()
                {
                    rating = whr.GetInternalRating(player.AccountID, bat.StartTime)?.GetElo() + WholeHistoryRating.RatingOffset,
                    stdev = whr.GetInternalRating(player.AccountID, bat.StartTime)?.GetEloStdev(),
                    accountId = player.AccountID,
                }).ToList();
                id = bat.SpringBattleID;
            }
        }
    }
}
