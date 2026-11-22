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
    /// Stretches stored map images back to the shape they should have had, for maps registered
    /// before the 2026-09-25 ToBytes fix.
    ///
    ///     ZK_CONNECTION_STRING=... dotnet run --project ZkData.Core -- backfill-minimaps &lt;dir&gt; [--apply] [--touch]
    ///
    /// **It reports and changes nothing unless told to.** `--apply` is what writes, and every file
    /// it overwrites is copied to `&lt;name&gt;.legacy` first, so a run can be undone with `mv`. That
    /// matters more here than usual: these are the only copies of images whose originals live in
    /// map archives that would have to be re-scanned with unitsync to reproduce.
    ///
    /// **What it cannot do** is recover detail. The old rule resized the short axis away, and no
    /// stretch brings it back - a corrected 2:1 minimap is geometrically right and softer than one
    /// registered today. Re-registering every map from its archive is the lossless option and is
    /// not this program.
    ///
    /// It is idempotent because <see cref="ImageSizing.LooksLikeLegacyToBytes"/> stops recognising
    /// an image once it has been corrected, which the tests assert as a property rather than an
    /// example.
    ///
    /// `--touch` also moves each corrected resource's LastChange, so clients re-sync rather than
    /// keeping the image they already have. It is off by default because it is a database write
    /// and because a run may well be a rehearsal.
    /// </summary>
    public static class LegacyMinimapBackfill
    {
        private const int ThumbnailSize = 96;   // as PlasmaServer.ThumbnailSize

        private static readonly string[] Kinds = { "minimap", "metalmap", "heightmap" };

        public static int Run(ZkDataContext db, string[] arguments)
        {
            var path = arguments.FirstOrDefault(x => !x.StartsWith("--"));
            var apply = arguments.Contains("--apply");
            var touch = arguments.Contains("--touch");

            // A first run over a real Resources directory should be a handful of maps that somebody
            // then looks at on the site. Without these that is not possible: the choice was the
            // whole library or nothing, which is a bad way to start rewriting images whose
            // originals only exist inside map archives.
            var limit = int.MaxValue;
            var limitArg = arguments.FirstOrDefault(x => x.StartsWith("--limit="));
            if (limitArg != null && !int.TryParse(limitArg.Substring("--limit=".Length), out limit))
            {
                Console.Error.WriteLine("--limit wants a number, as in --limit=5");
                return 2;
            }

            var onlyArg = arguments.FirstOrDefault(x => x.StartsWith("--only="));
            var only = onlyArg == null
                ? null
                : new HashSet<string>(
                    onlyArg.Substring("--only=".Length).Split(',').Select(x => x.Trim()).Where(x => x.Length > 0),
                    StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(path))
            {
                Console.Error.WriteLine("usage: backfill-minimaps <path to the Resources directory> [--apply] [--touch] [--limit=N] [--only=name,name]");
                return 2;
            }
            if (!Directory.Exists(path))
            {
                Console.Error.WriteLine("no such directory: " + path);
                return 2;
            }

            var resources = db.Resources.Where(x => x.MapSizeRatio != null).ToList();

            int corrected = 0, alreadyRight = 0, missing = 0, failed = 0, touched = 0, correctedResources = 0;
            var examples = new List<string>();

            foreach (var resource in resources)
            {
                var ratio = resource.MapSizeRatio.Value;
                if (Math.Abs(ratio - 1) < 0.01) continue;   // square: the defect was a no-op
                if (only != null && !only.Contains(resource.InternalName)) continue;
                if (correctedResources >= limit) break;

                var changedAny = false;
                foreach (var kind in Kinds)
                {
                    var file = Path.Combine(path,
                        string.Format("{0}.{1}.jpg", resource.InternalName.EscapePath(), kind));
                    if (!File.Exists(file))
                    {
                        missing++;
                        continue;
                    }

                    try
                    {
                        var info = Image.Identify(file);
                        var stored = new System.Drawing.Size(info.Width, info.Height);

                        if (!ImageSizing.LooksLikeLegacyToBytes(stored, ratio))
                        {
                            alreadyRight++;
                            continue;
                        }

                        var target = ImageSizing.CorrectedFromLegacy(stored, ratio);
                        if (examples.Count < 10)
                            examples.Add(string.Format("  {0}.{1}  {2}x{3} -> {4}x{5}",
                                resource.InternalName, kind, stored.Width, stored.Height, target.Width, target.Height));

                        if (apply)
                        {
                            var bytes = File.ReadAllBytes(file);
                            File.WriteAllBytes(file + ".legacy", bytes);
                            // Quality 100: this image has been through one JPEG generation already
                            // and is about to go through another, which is the cost of correcting
                            // it at all. There is no reason to add to it.
                            Images.Processor.SaveResizedJpeg(bytes, target, file, 100);
                        }

                        corrected++;
                        changedAny = true;

                        if (kind == "minimap")
                        {
                            var thumbnail = Path.Combine(path,
                                string.Format("{0}.thumbnail.jpg", resource.InternalName.EscapePath()));
                            if (apply)
                            {
                                // Regenerated the way registration makes it, from the CORRECTED
                                // image - the stored thumbnail has the right dimensions and
                                // distorted content, which is the defect at its least visible.
                                if (File.Exists(thumbnail)) File.Copy(thumbnail, thumbnail + ".legacy", true);
                                Images.Processor.SaveResizedJpeg(File.ReadAllBytes(file),
                                    ImageSizing.ScaledToFit(ratio, ThumbnailSize), thumbnail, 100);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Console.Error.WriteLine("  {0}.{1}: {2}", resource.InternalName, kind, ex.Message);
                    }
                }

                if (changedAny) correctedResources++;

                if (changedAny && touch && apply)
                {
                    resource.LastChange = DateTime.UtcNow;
                    touched++;
                }
            }

            if (touch && apply && touched > 0) db.SaveChanges();

            Console.WriteLine();
            Console.WriteLine(apply ? "applied:" : "dry run - nothing was written:");
            Console.WriteLine("  corrected      {0}", corrected);
            Console.WriteLine("  already right  {0}", alreadyRight);
            Console.WriteLine("  file missing   {0}", missing);
            Console.WriteLine("  failed         {0}", failed);
            if (limit != int.MaxValue) Console.WriteLine("  maps touched   {0} (--limit={1})", correctedResources, limit);
            if (only != null) Console.WriteLine("  restricted to  {0} map(s) named with --only", only.Count);
            if (touch) Console.WriteLine("  LastChange set {0}", touched);

            if (examples.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("first few:");
                foreach (var line in examples) Console.WriteLine(line);
            }

            if (!apply && corrected > 0)
            {
                Console.WriteLine();
                Console.WriteLine("re-run with --apply to write, and --touch to make clients re-sync.");
            }

            return failed > 0 ? 1 : 0;
        }
    }
}
