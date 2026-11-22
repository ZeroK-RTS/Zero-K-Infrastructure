using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace System.Web.Optimization
{
    /// <summary>
    /// The two bundling calls Shared/_SiteLayout.cshtml makes, which is every page on the
    /// site.
    ///
    /// System.Web.Optimization is .NET Framework only and has no ASP.NET Core successor -
    /// §3 of the modernization plan lists it as "Replace". The eventual answer is a build
    /// step (esbuild, or ASP.NET Core's own bundling), and choosing one is a decision about
    /// the frontend toolchain, not about this port.
    ///
    /// So this emits the bundle's files individually, unminified, in the order BundleConfig
    /// declares them. That is exactly what the real bundler does in debug mode, so it is a
    /// faithful development-time answer rather than a stub.
    ///
    /// **The build step exists now** - esbuild, via tools/build-assets.mjs. When its output is
    /// present, <see cref="Bundles.BuiltPath"/> is set and this emits ONE tag per bundle instead,
    /// which is what the container ships. Unset, the development answer above is unchanged.
    ///
    /// The file list is duplicated from BundleConfig.cs because that file cannot be linked -
    /// it is written against BundleCollection, which does not exist here. Duplicated lists
    /// drift, so tools/check-bundles.py compares the two and runs in CI.
    ///
    /// This comment used to say the check lived in ZeroKWeb.Host. It did not live anywhere: the
    /// only two files that mentioned ~/bundles/main were this one and BundleConfig.cs. A comment
    /// promising a guard is worse than no comment, because the next reader stops looking for it.
    /// </summary>
    public static class Bundles
    {
        public static readonly IReadOnlyDictionary<string, string[]> Contents =
            new Dictionary<string, string[]>
            {
                ["~/bundles/main"] = new[]
                {
                    "~/Scripts/jquery-{version}.js",
                    "~/Scripts/jquery.unobtrusive-ajax.js",
                    "~/Scripts/browser-css.js",
                    "~/Scripts/jquery-ui.min.js",
                    "~/Scripts/jquery.ui.stars.js",
                    "~/Scripts/jquery.qtip.min.js",
                    "~/Scripts/jquery.expand.js",
                    "~/Scripts/jquery.datetimepicker.full.min.js",
                    "~/Scripts/nicetitle.js",
                    "~/Scripts/grid.js",
                    "~/Scripts/site_main.js",
                },
                ["~/bundles/maincss"] = new[]
                {
                    "~/Styles/fonts.css",
                    "~/Styles/base.css",
                    "~/Styles/jquery.datetimepicker.min.css",
                    "~/Styles/menu.css",
                    "~/Styles/stars.css",
                    "~/Styles/levelrank.css",
                    "~/Styles/jquery.ui.stars.css",
                    "~/Styles/style.css",
                    "~/Styles/jquery.qtip.min.css",
                    "~/Styles/dark-hive/jquery-ui.css",
                    "~/Styles/nicetitle.css",
                },
            };

        internal static IEnumerable<string> Files(string bundle)
            => Contents.TryGetValue(bundle, out var files) ? files : Enumerable.Empty<string>();

        internal static string Href(string tildePath) => tildePath.TrimStart('~');

        /// <summary>
        /// Where the built bundles are served from, or null when there are none.
        ///
        /// **Set, this emits one tag per bundle instead of eleven** - what the real bundler does
        /// outside debug mode. Unset, nothing changes: the files are listed individually and
        /// unminified, which is what a developer wants and what the host harness asserts.
        ///
        /// The host sets it when tools/build-assets.mjs has produced output. Deliberately a
        /// property rather than a probe of the file system from here: this library is linked into
        /// three projects, and which of them has built assets is the host's business.
        /// </summary>
        public static string BuiltPath { get; set; }

        /// <summary>The built file for a bundle - "~/bundles/main" becomes "/bundles/main.js".</summary>
        internal static string Built(string bundle, string extension)
        {
            if (string.IsNullOrEmpty(BuiltPath)) return null;
            var name = bundle.Substring(bundle.LastIndexOf('/') + 1);
            return BuiltPath.TrimEnd('/') + "/" + name + extension;
        }
    }

    public static class Scripts
    {
        public static IHtmlContent Render(params string[] bundles)
        {
            var sb = new StringBuilder();
            foreach (var bundle in bundles)
            {
                var built = Bundles.Built(bundle, ".js");
                if (built != null)
                {
                    sb.Append($"<script src=\"{built}\"></script>");
                    continue;
                }

                foreach (var file in Bundles.Files(bundle))
                    sb.Append($"<script src=\"{Bundles.Href(file)}\"></script>");
            }
            return new HtmlString(sb.ToString());
        }
    }

    public static class Styles
    {
        public static IHtmlContent Render(params string[] bundles)
        {
            var sb = new StringBuilder();
            foreach (var bundle in bundles)
            {
                var built = Bundles.Built(bundle, ".css");
                if (built != null)
                {
                    sb.Append($"<link href=\"{built}\" rel=\"stylesheet\"/>");
                    continue;
                }

                foreach (var file in Bundles.Files(bundle))
                    sb.Append($"<link href=\"{Bundles.Href(file)}\" rel=\"stylesheet\"/>");
            }
            return new HtmlString(sb.ToString());
        }
    }
}
