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
        static ContentServiceImplementation implementation = new ContentServiceImplementation();

        [System.Web.Mvc.HttpPost]
        public async Task<ActionResult> Index()
        {
            var sr = new StreamReader(Request.InputStream);
            var line = await sr.ReadToEndAsync();
            var response = await implementation.Process(line);
            return Content(response, "application/json");
        }
    }
}