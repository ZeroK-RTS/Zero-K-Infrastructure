namespace WebGrease.Configuration
{
    // Shared/_SiteLayout.cshtml opens with `@using WebGrease.Configuration` and then never
    // mentions WebGrease again - it is a leftover from System.Web.Optimization's minifier,
    // and the layout is the only page on the site, so this one dead line blocks every page.
    //
    // Deleting the line would be the right fix and is not available here: nothing in this
    // repository compiles MVC 5 views, so there is no way to prove the removal is safe from
    // Linux. The Windows build is the only thing that would catch it, and it does not run on
    // this fork. So the namespace is supplied instead, which costs nothing and cannot break
    // the Framework build - and the line is recorded here for whoever can compile a view.
    //
    // Empty on purpose. If anything ever needs a type out of it, that is a different
    // problem and this file should not be where it gets solved.
    internal static class DeadUsingMarker { }
}
