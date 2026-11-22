using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Database
{
    /// <summary>
    /// Runs the [TestMethod]s in this assembly without a test runner.
    ///
    /// Mono has no working vstest.console, so on Linux this is how the suite runs; on
    /// Windows the same methods are discovered by Visual Studio and by the CI runner in
    /// .github/workflows/test_pullrequest.yml. There is one set of tests either way.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Not a test run: produce the rating table for the cross-stack comparison.
            if (args.FirstOrDefault() == "--dump-ratings") return RatingDump.Run(args.Skip(1).FirstOrDefault());

            var filter = args.FirstOrDefault();
            int passed = 0, failed = 0, skipped = 0, matched = 0;

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                         .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                         .OrderBy(t => t.Name))
            {
                var methods = type.GetMethods()
                    .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                    .Where(m => filter == null || m.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(m => m.Name)
                    .ToList();
                if (!methods.Any()) continue;
                matched += methods.Count;

                Console.WriteLine();
                Console.WriteLine(type.Name);

                object instance;
                try
                {
                    instance = Activator.CreateInstance(type);
                    var init = type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);
                    if (init != null) init.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  ! could not set up: " + Unwrap(ex).Message);
                    failed += methods.Count;
                    continue;
                }

                foreach (var m in methods)
                {
                    try
                    {
                        m.Invoke(instance, null);
                        Console.WriteLine("  pass  " + m.Name);
                        passed++;
                    }
                    catch (Exception ex)
                    {
                        var real = Unwrap(ex);
                        if (real is AssertInconclusiveException)
                        {
                            Console.WriteLine("  skip  " + m.Name + " - " + real.Message);
                            skipped++;
                        }
                        else
                        {
                            Console.WriteLine("  FAIL  " + m.Name);
                            Console.WriteLine("        " + real.Message.Replace("\n", "\n        "));
                            failed++;
                        }
                    }
                }

                // [TestCleanup] was declared and never called, which is worse than not
                // supporting it: a test author writes the teardown, reads it back, and gets
                // none. RegistrationFixupTests noticed and hedged - its RemoveTheProbeRows
                // carries BOTH attributes - so its two probe accounts were cleared before it
                // ran and left behind afterwards, for every later test in the run. Harmless
                // there, and it still skewed a number: UsernameLengthTests reported the
                // longest stored name as 22 characters, which is fixup_probe_navigation and
                // not anything in the fixture.
                //
                // Once per type, after its methods, to match the [TestInitialize] above.
                // That is NOT MSTest's per-method granularity, and a test written expecting
                // per-method teardown will not get it here - said out loud because the
                // attribute names promise otherwise.
                try
                {
                    var cleanup = type.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);
                    if (cleanup != null) cleanup.Invoke(instance, null);
                }
                catch (Exception ex)
                {
                    // Not a failure of any test, but it leaves rows behind for the next one.
                    Console.WriteLine("  ! could not clean up: " + Unwrap(ex).Message);
                }
            }

            Console.WriteLine();

            // A filter that matches no test printed "0 passed, 0 failed, 0 skipped" and exited 0.
            // That is indistinguishable from a clean run, and the filter is how somebody points
            // ONE check at a database that is not this fixture - a typo there would answer the
            // question with a green tick and no test having run. The filter matches METHOD names,
            // not class names, which is the typo it is easiest to make.
            if (filter != null && matched == 0)
            {
                Console.WriteLine(string.Format("no test method's name contains \"{0}\" - nothing ran.", filter));
                Console.WriteLine("The filter matches method names. Run with no argument to list them.");
                return 2;
            }

            Console.WriteLine(string.Format("{0} passed, {1} failed, {2} skipped", passed, failed, skipped));
            return failed == 0 ? 0 : 1;
        }

        static Exception Unwrap(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
            return ex;
        }
    }
}
