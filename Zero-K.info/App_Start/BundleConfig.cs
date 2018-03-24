﻿using System.Web.Optimization;

public class BundleConfig
{
    public static void RegisterBundles(BundleCollection bundles)
    {
        bundles.Add(new ScriptBundle("~/bundles/main").Include(
            "~/Scripts/jquery-{version}.js",
            "~/Scripts/jquery.unobtrusive-ajax.js",
            "~/Scripts/browser-css.js",
            "~/Scripts/jquery-ui.min.js",
            "~/Scripts/jquery.ui.stars.js",
            "~/Scripts/jquery.qtip.min.js",
            "~/Scripts/jquery.ba-bbq.js",
            "~/Scripts/jquery.history.js",
            "~/Scripts/jquery.expand.js",
            "~/Scripts/jquery.datetimepicker.full.min.js",
            "~/Scripts/nicetitle.js",
            "~/Scripts/raphael-min.js",
            "~/Scripts/grid.js",
            "~/Scripts/site_main.js"));

        bundles.Add(new StyleBundle("~/bundles/maincss").Include(
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
            "~/Styles/nicetitle.css"));

        
    }
}