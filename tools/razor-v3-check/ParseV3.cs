// not-in-a-project: compiled by tools/razor-v3-check/check.sh under mono, against Razor v3
// Parses a .cshtml with MVC 5's own Razor, and reports what it rejects.
//
// This repository has said from the beginning that nothing here can check an MVC 5 view - mono
// ships no aspnet_compiler.exe - and has therefore treated every view edit as unverifiable on
// the Framework side. That is true of COMPILATION. It is not true of SYNTAX: System.Web.Razor
// 3.2.3 is in the NuGet cache the mono build already restores, and its parser will say whether
// Razor v3 accepts a construct.
//
// That is exactly the question a view edit raises. @helper exists in v3 and not in ASP.NET Core;
// a templated Razor delegate may exist in both. Guessing which is what this tool removes.
//
// What it does NOT do is bind names or types. `@await Component.InvokeAsync("X")` parses here
// perfectly happily - it is syntactically fine and would fail to COMPILE, which this never sees.
// It answers "is this Razor v3 syntax?", not "does this build?". That is still worth having,
// because it is what separates @helper from a construct both stacks accept, and it caught a
// nested @{ } that ASP.NET Core's parser tolerates and Razor v3 rejects.

using System;
using System.IO;
using System.Linq;
using System.Web.Razor;
using System.Web.WebPages.Razor;

public static class ParseV3
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("usage: ParseV3 <view.cshtml> [more...]");
            return 2;
        }

        var failures = 0;
        foreach (var path in args)
        {
            // WebPageRazorHost, not the bare RazorEngineHost. @helper is a WebPages construct,
            // and the bare host throws rather than reporting it - which would have made every
            // @helper view look like a crash in this tool instead of a finding.
            var host = new WebPageRazorHost(path);
            var engine = new RazorTemplateEngine(host);

            try
            {
                using (var reader = new StreamReader(path))
                {
                    var results = engine.GenerateCode(reader);
                    var errors = results.ParserErrors.ToList();
                    if (errors.Count == 0)
                    {
                        Console.WriteLine("ok    " + path);
                    }
                    else
                    {
                        failures++;
                        foreach (var error in errors)
                            Console.WriteLine(string.Format("FAIL  {0}({1},{2}): {3}", path,
                                error.Location.LineIndex + 1, error.Location.CharacterIndex + 1, error.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                // Reported rather than thrown: a parser that dies on one file must not take the
                // rest of the run with it, and "it crashed" is itself an answer about the syntax.
                failures++;
                Console.WriteLine("CRASH " + path + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }
        return failures == 0 ? 0 : 1;
    }
}
