using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
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

        [ValidateInput(false)]
        public async Task<ActionResult> Index()
        {
            var sr = new StreamReader(Request.InputStream);
            var line = await sr.ReadToEndAsync();
            if (string.IsNullOrEmpty(line))
                return Content("Please send request in POST body in command line format:ClassName JsonSerializedClassContent");
            var response = await implementation.Process(line);
            return Content(response, "application/json");
        }
    }
}