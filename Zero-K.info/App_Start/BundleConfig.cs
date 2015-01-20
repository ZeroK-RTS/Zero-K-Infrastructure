using System.Web.Optimization;

public class BundleConfig
{
    public static void RegisterBundles(BundleCollection bundles)
    {
        bundles.Add(new ScriptBundle("~/bundles/main").Include(
            "~/Scripts/MicrosoftAjax.js",
            "~/Scripts/MicrosoftMvcAjax.js",
            "~/Scripts/jquery-1.5.2.min.js",
            "~/Scripts/jquery-ui-1.8.14.custom.min.js",
            "~/Scripts/jquery.ui.stars.js",
            "~/Scripts/jquery.tablesorter.min.js",
            "~/Scripts/jquery.qtip.pack.js",
            "~/Scripts/site_main.js",
            "~/Scripts/base.js",
            "~/Scripts/browser-css.js",
            "~/Scripts/nicetitle.js",
            "~/Scripts/raphael-min.js"));

        bundles.Add(new StyleBundle("~/bundles/maincss").Include(
            "~/Styles/fonts.css",
            "~/Styles/base.css",
            "~/Styles/menu.css",
            "~/Styles/nicetitle.css",
            "~/Styles/stars.css",
            "~/Styles/levelrank.css",
            "~/Styles/jquery.ui.stars.css",
            "~/Styles/style.css",
            "~/Styles/jquery.qtip.min.css",
            "~/Styles/dark-hive/jquery-ui-1.8.14.custom.css",
            "~/Styles/nicetitle.css"));
    }
}