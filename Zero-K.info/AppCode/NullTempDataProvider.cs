using System.Collections.Generic;
using System.Web.Mvc;

namespace ZeroKWeb
{
    public class NullTempDataProvider: ITempDataProvider {
        public IDictionary<string, object> LoadTempData(ControllerContext controllerContext)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(ControllerContext controllerContext, IDictionary<string, object> values)
        {
            // note this provider does not support temp data, it merely fixes internal problem of asp.net mvc where it tries to use it with sessions off
        }
    }
}