// A namespace named by a using in a linked controller and never mentioned again in it. One
// dead line is enough to keep a whole controller - and the views wanting its view models -
// out of the port, so it is supplied rather than deleted, for the reason in WebGreaseCompat.cs.
namespace ZeroKWeb.SpringieInterface { internal static class DeadUsingMarker { } }

// Deliberately NOT shimmed, though three more controllers would link if they were:
//
//   ZkLobbyServer               BattlesController, TourneyController
//   System.Web.Helpers          ChartsController
//   EntityFramework.Extensions  PlanetwarsAdminController
//
// ZkLobbyServer is the reason. In BattlesController the using really is dead, but the
// controller then calls ReplayStorage.Instance - which lives in ZkLobbyServer - and
// TourneyController casts and mutates live TourneyBattle objects eight times. An empty
// namespace would let both compile while hiding exactly the coupling Phase 1 exists to
// remove. The other two are only unshimmed because they are no use without a controller that
// needs them.

// PostHistoryController's `using System.Web.Services.Description;` - a WSDL type it never names.
// One dead line, the same shape as the one above, and it was the file's only blocker.
namespace System.Web.Services.Description { internal static class DeadUsingMarker { } }


// ContentServiceController's `using System.Web.Http;` - Web API, which it never names. The
// namespace is no longer only a marker: ApiControllerCompat.cs puts a real ApiController in it,
// so that WhrController - the site's one Web API controller, and an endpoint outside this
// repository calls it - links into the port as well.
namespace System.Web.Http { internal static class DeadUsingMarker { } }
