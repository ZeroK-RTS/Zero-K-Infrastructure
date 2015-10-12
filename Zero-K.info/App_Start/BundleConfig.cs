using System.Web.Optimization;

public class BundleConfig
{
    public static void RegisterBundles(BundleCollection bundles)
    {
        bundles.Add(new ScriptBundle("~/bundles/main").Include(
            "~/Scripts/jquery-1.11.3.min.js",
            "~/Scripts/jquery-ui.min.js",
            "~/Scripts/jquery.ui.stars.js",
            "~/Scripts/jquery.qtip.min.js",
            "~/Scripts/base.js",
            "~/Scripts/browser-css.js",
            "~/Scripts/nicetitle.js",
            "~/Scripts/raphael-min.js",
            "~/Scripts/site_main.js"));

        bundles.Add(new StyleBundle("~/bundles/maincss").Include(
            "~/Styles/fonts.css",
            "~/Styles/base.css",
            "~/Styles/menu.css",
            "~/Styles/stars.css",
            "~/Styles/levelrank.css",
            "~/Styles/jquery.ui.stars.css",
            "~/Styles/style.css",
            "~/Styles/jquery.qtip.min.css",
            "~/Styles/dark-hive/jquery-ui.css",
            "~/Styles/nicetitle.css"));

        
    }
}