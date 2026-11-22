using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using LobbyClient;

namespace ZeroKWeb.Controllers
{
    public class ContentServiceController: AsyncController
    {
        public ContentServiceController()
        {
            TempDataProvider = new NullTempDataProvider(); // this disable session state upkeep
        }

        static ContentServiceImplementation implementation = new ContentServiceImplementation();

        // The body IS the request, as the error text below says, and every client POSTs it
        // (IContentService.cs uses httpClient.PostAsync). A GET only ever reached the "please
        // send a POST" line; saying so in an attribute is what lets the GET-write check see
        // that ContentServiceImplementation.Process - which registers resources, and writes -
        // is not reachable by one.
        [HttpPost]
        [NoAntiForgeryTokenByDesign("the callers are game clients - ZKL, Chobby, the autoregistrator - not browsers, and carry no session to forge")]
        [ValidateInput(false)]
        public async Task<ActionResult> Index()
        {
            var sr = new StreamReader(this.RequestInputStream());
            var line = await sr.ReadToEndAsync();
            if (string.IsNullOrEmpty(line))
                return Content("Please send request in POST body in command line format:ClassName JsonSerializedClassContent");
            var response = await implementation.Process(line);
            return Content(response, "application/json");
        }
    }
}