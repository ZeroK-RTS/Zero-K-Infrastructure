using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlasmaShared;
using PlasmaShared.Imaging;
using SixLabors.ImageSharp;

namespace ZkData.Core
{
    /// <summary>
    /// Lists the stored map images that were written by the old ToBytes rule.
    ///
    /// **This is the other half of the 2026-09-25 decision.** Fixing ToBytes corrects newly
    /// registered maps and deliberately leaves stored ones alone; the objection to that was
    /// that the two generations would be indistinguishable. They are not -
    /// <see cref="ImageSizing.LooksLikeLegacyToBytes"/> tells them apart from the map's own
    /// ratio - and this is what applies it to a real Resources directory, so a backfill can be
    /// decided on numbers rather than on a guess.
    ///
    ///     ZK_CONNECTION_STRING=... dotnet run --project ZkData.Core -- minimaps /path/to/Resources
    ///
    /// It only reads. Re-processing anything is a separate decision and, deliberately, a
    /// separate program.
    /// </summary>
    public static class LegacyMinimapReport
    {
        public static int Run(ZkDataContext db, string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
            {
                Console.Error.WriteLine("usage: minimaps <path to the Resources directory>");
                return 2;
            }
            if (!Directory.Exists(resourcesPath))
            {
                Console.Error.WriteLine("no such directory: " + resourcesPath);
                return 2;
            }

            var resources = db.Resources
                .Where(x => x.MapSizeRatio != null)
                .Select(x => new { x.InternalName, x.MapSizeRatio })
                .ToList();

            int legacy = 0, current = 0, square = 0, missing = 0, unreadable = 0;
            var examples = new List<string>();

            foreach (var resource in resources)
            {
                var path = Path.Combine(resourcesPath, resource.InternalName.EscapePath() + ".minimap.jpg");
                if (!File.Exists(path))
                {
                    missing++;
                    continue;
                }

                System.Drawing.Size size;
                try
                {
                    var info = Image.Identify(path);
                    size = new System.Drawing.Size(info.Width, info.Height);
                }
                catch (Exception)
                {
                    unreadable++;
                    continue;
                }

                double ratio = resource.MapSizeRatio.Value;
                if (Math.Abs(ratio - 1) < 0.01)
                {
                    square++;
                }
                else if (ImageSizing.LooksLikeLegacyToBytes(size, ratio))
                {
                    legacy++;
                    if (examples.Count < 10)
                    {
                        // ImageSizing.CorrectedFromLegacy, not a second copy of the arithmetic.
                        // The copy that used to be here read `size.Width, size.Width / ratio`,
                        // which is the LANDSCAPE case written out: it keeps the width and derives
                        // the height. For a portrait map the backfill keeps the HEIGHT and widens,
                        // so this line told an operator the image would get shorter when it was
                        // about to get wider - 455x1024 was reported as "expected about 455x682"
                        // where the backfill does 682x1024. Found by rehearsing the run against
                        // real files rather than by reading either file.
                        var target = ImageSizing.CorrectedFromLegacy(size, ratio);
                        examples.Add(string.Format("  {0}  stored {1}x{2}, map ratio {3:F2}, expected about {4}x{5}",
                            resource.InternalName, size.Width, size.Height, ratio,
                            target.Width, target.Height));
                    }
                }
                else
                {
                    current++;
                }
            }

            Console.WriteLine("stored map images, against ImageSizing.LooksLikeLegacyToBytes:");
            Console.WriteLine();
            Console.WriteLine("  legacy (squashed)   {0}", legacy);
            Console.WriteLine("  current            {0}", current);
            Console.WriteLine("  square (no defect) {0}", square);
            Console.WriteLine("  file missing       {0}", missing);
            Console.WriteLine("  unreadable         {0}", unreadable);

            if (examples.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("first few:");
                foreach (var line in examples) Console.WriteLine(line);
            }

            // Reporting is not failing: a legacy image is the known state of the world, not a
            // broken build. Only being unable to look is an error.
            return 0;
        }
    }
}
