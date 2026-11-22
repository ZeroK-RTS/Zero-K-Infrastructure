using System.IO;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace ZeroKWeb.Controllers
{
    /// <summary>
    /// <c>/MissionService</c> - the JSON endpoint the mission editor can use instead of
    /// <c>MissionService.svc</c>, which is WCF and therefore has no future on .NET 9.
    ///
    /// Deliberately identical in shape to <see cref="ContentServiceController"/>, down to
    /// disabling session state: this is the same job and a second convention for it would help
    /// nobody. The `.svc` endpoint stays for editors that are already out there.
    /// </summary>
    public class MissionServiceController : AsyncController
    {
        public MissionServiceController()
        {
            TempDataProvider = new NullTempDataProvider(); // this disable session state upkeep
        }

        private static readonly MissionServiceImplementation implementation = new MissionServiceImplementation();

        // As ContentServiceController: the body is the request, MissionServiceJsonClient
        // PostAsyncs it, and Process writes missions.
        [HttpPost]
        [NoAntiForgeryTokenByDesign("the caller is the mission editor, not a browser")]
        [ValidateInput(false)]
        public async Task<ActionResult> Index()
        {
            var reader = new StreamReader(this.RequestInputStream());
            var line = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(line))
                return Content("Please send request in POST body in command line format:ClassName JsonSerializedClassContent");
            var response = await implementation.Process(line);
            return Content(response, "application/json");
        }
    }
}
