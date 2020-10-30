using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ZeroKWeb.Models
{
    public class PartialViewEngine : RazorViewEngine
    {
        public PartialViewEngine()
        {
            string[] partialSearchPaths = new[] { "~/Views/Shared/Modules/{0}.cshtml" };
            base.PartialViewLocationFormats = base.PartialViewLocationFormats.Union(partialSearchPaths).ToArray();
        }
    }
}