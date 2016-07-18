using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using ManagedLzma.SevenZip;
using SharpCompress.Archive;
using SharpCompress.Common;
using ZkData;

namespace PlasmaDownloader
{
    public class EngineDownload : Download
    {
        readonly SpringPaths springPaths;


        public EngineDownload(string version, SpringPaths springPaths)
        {
            this.springPaths = springPaths;
            Name = version;
        }

        public static List<string> GetEngineList()
        {
            var engineDownloadPath = GlobalConst.EngineDownloadPath;
            var branchData = new WebClient().DownloadString(string.Format("{0}buildbot/default/", engineDownloadPath));

            var comparer = new VersionNumberComparer();

            var branches = Regex.Matches(branchData,
                              "<img src=\"/icons/folder.gif\" alt=\"\\[DIR\\]\"></td><td><a href=\"([^\"]+)/\">\\1/</a>",
                              RegexOptions.IgnoreCase).OfType<Match>().Select(x => x.Groups[1].Value).OrderBy(x => x, comparer).ToList();

            string data = "";
            foreach (string branch in branches)
            {
                data += new WebClient().DownloadString(string.Format("{0}buildbot/default/{1}/", engineDownloadPath, branch));
            }

            var list =
                Regex.Matches(data,
                              "<img src=\"/icons/folder.gif\" alt=\"\\[DIR\\]\"></td><td><a href=\"([^\"]+)/\">\\1/</a>",
                              RegexOptions.IgnoreCase).OfType<Match>().Select(x => x.Groups[1].Value).OrderBy(x => x, comparer).ToList();
            return list;
        }

        public void Start()
        {
            Utils.StartAsync(() =>
                {
                    var paths = new List<string>();
                    var platform = "win32";
                    var archiveName = "minimal-portable+dedicated.zip";
                    var archiveNameAlt = "minimal-portable.7z";

                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        var response = Utils.ExecuteConsoleCommand("uname", "-m") ?? "";
                        platform = response.Contains("64") ? "linux64" : "linux32";
                        archiveName = string.Format("minimal-portable-{0}-static.7z", platform);
                    }

                    var engineDownloadPath = GlobalConst.EngineDownloadPath;


                    paths.Add(string.Format("{0}/engine/{2}/{1}.zip", GlobalConst.BaseSiteUrl, Name, platform));

                    paths.Add(string.Format("{0}buildbot/default/master/{1}/spring_{1}_{2}", engineDownloadPath, Name, archiveName));
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/spring_{{develop}}{1}_{2}", engineDownloadPath, Name, archiveName));
                    paths.Add(string.Format("{0}buildbot/default/release/{1}/spring_{{release}}{1}_{2}", engineDownloadPath, Name, archiveName));

                    // new format with portable.7z downloads for post 101.0.1-414-g6a6a528 dev versions
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/{3}/spring_{{develop}}{1}_{3}-{2}",
                                            engineDownloadPath,
                                            Name,
                                            archiveNameAlt,
                                            platform));
                    // 101.0.1-414-g6a6a528 dev versions and earlier dev versions (now that minimal-portable+dedicated.zip is no longer available)
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/{3}/spring_{1}_{2}",
                                            engineDownloadPath,
                                            Name,
                                            archiveNameAlt,
                                            platform));
                    // dunno if this will be the format for 102.0 but may as well put it in now
                    paths.Add(string.Format("{0}buildbot/default/master/{1}/{3}/spring_{1}_{3}_{2}",
                                            engineDownloadPath,
                                            Name,
                                            archiveNameAlt,
                                            platform));
                    paths.Add(string.Format("{0}buildbot/default/release/{1}/{3}/spring_{{release}}{1}_{2}",
                                            engineDownloadPath,
                                            Name,
                                            archiveName,
                                            platform));

                    // maybe just us pr-downloader instead already
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/{3}/spring_{{develop}}{1}_{3}-{2}",
                        engineDownloadPath,
                        Name,
                        archiveNameAlt,
                        platform));



                    paths.Add(string.Format("{0}buildbot/syncdebug/develop/{1}/spring_[syncdebug]{1}_{2}", engineDownloadPath, Name, archiveName));
                    paths.Add(string.Format("{0}buildbot/default/master/{1}/{3}/spring_{1}_{2}", engineDownloadPath, Name, archiveName, platform));
                    paths.Add(string.Format("{0}buildbot/default/develop/{1}/{3}/spring_{{develop}}{1}_{2}",
                                            engineDownloadPath,
                                            Name,
                                            archiveName,
                                            platform));

                    var source = paths.FirstOrDefault(VerifyFile) ?? paths.FirstOrDefault(VerifyFile);

                    if (source != null)
                    {
                        var extension = source.Substring(source.LastIndexOf('.'));
                        var wc = new WebClient() { Proxy = null };
                        var assemblyName = Assembly.GetEntryAssembly()?.GetName();
                        if (assemblyName != null) wc.Headers.Add("user-agent", string.Format("{0} {1}", assemblyName.Name, assemblyName.Version));
                        var target = Path.GetTempFileName() + extension;
                        wc.DownloadProgressChanged += (s, e) =>
                            {
                                Length = (int)(e.TotalBytesToReceive);
                                IndividualProgress = 10 + 0.8 * e.ProgressPercentage;
                            };
                        wc.DownloadFileCompleted += (s, e) =>
                            {
                                if (e.Cancelled)
                                {
                                    Trace.TraceInformation("Download {0} cancelled", Name);
                                    Finish(false);
                                }
                                else if (e.Error != null)
                                {
                                    Trace.TraceWarning("Error downloading {0}: {1}", Name, e.Error);
                                    Finish(false);
                                }
                                else
                                {
                                    Trace.TraceInformation("Installing {0}", source);
                                    var timer = new Timer((o) => { IndividualProgress += (100 - IndividualProgress) / 10; }, null, 1000, 1000);

                                    if (extension == ".exe")
                                    {
                                        var p = new Process();
                                        p.StartInfo = new ProcessStartInfo(target,
                                                                           string.Format("/S /D={0}", springPaths.GetEngineFolderByVersion(Name)));
                                        p.Exited += (s2, e2) =>
                                            {
                                                timer.Dispose();
                                                if (p.ExitCode != 0)
                                                {
                                                    Trace.TraceWarning("Install of {0} failed: {1}", Name, p.ExitCode);
                                                    Finish(false);
                                                }
                                                else
                                                {
                                                    Trace.TraceInformation("Install of {0} complete", Name);
                                                    springPaths.SetEnginePath(springPaths.GetEngineFolderByVersion(Name));
                                                    Finish(true);
                                                    // run unitsync after engine download; for more info see comments in Program.cs
                                                    //new PlasmaShared.UnitSyncLib.UnitSync(springPaths); // put it after Finish() so it doesn't hold up the download bar
                                                    //^ is commented because conflict/non-consensus. See: https://code.google.com/p/zero-k/source/detail?r=12394 for some issue/discussion
                                                }
                                            };

                                        p.EnableRaisingEvents = true;
                                        p.Start();
                                    }
                                    else
                                    {
                                        var targetDir = springPaths.GetEngineFolderByVersion(Name);
                                        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                                        try
                                        {
                                            if (extension == ".7z")
                                            {
                                                if (Environment.OSVersion.Platform == PlatformID.Unix)
                                                {
                                                    var proc = Process.Start("7z", string.Format("x -r -y -o\"{1}\" \"{0}\"", target, targetDir));
                                                    if (proc != null) proc.WaitForExit();
                                                    if (proc == null || proc.ExitCode != 0)
                                                    {
                                                        Trace.TraceWarning("7z extraction failed, fallback to SharpCompress");
                                                        Unpack7zArchive(target, targetDir);
                                                    }
                                                }
                                                else
                                                {
                                                    ExtractZipArchive(target, targetDir);
                                                }

                                                Trace.TraceInformation("Install of {0} complete", Name);
                                                springPaths.SetEnginePath(targetDir);
                                                Finish(true);
                                            }
                                            else
                                            {
                                                ExtractZipArchive(target, targetDir);
                                                Trace.TraceInformation("Install of {0} complete", Name);
                                                springPaths.SetEnginePath(targetDir);
                                                Finish(true);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            try
                                            {
                                                Directory.Delete(targetDir, true);
                                            }
                                            catch { }
                                            Trace.TraceWarning("Install of {0} failed: {1}", Name, ex);
                                            Finish(false);
                                        }
                                    }
                                }
                            };
                        Trace.TraceInformation("Downloading {0}", source);
                        wc.DownloadFileAsync(new Uri(source), target, this);
                        return;
                    }
                    Trace.TraceInformation("Cannot find {0}", Name);
                    Finish(false);
                });
        }

        void ExtractZipArchive(string target, string targetDir)
        {
            using (var archive = ArchiveFactory.Open(target))
            {
                long done = 0;
                var totalSize = archive.Entries.Count() + 1;
                archive.EntryExtractionEnd += (sender, args) =>
                    {
                        done++;
                        IndividualProgress = 90 + (10 * done / totalSize);
                    };

                archive.WriteToDirectory(targetDir, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }
        }


        static bool VerifyFile(string url)
        {
            try
            {
                var request = WebRequest.Create(url);
                request.Method = "HEAD";
                request.Timeout = 5000;
                var res = request.GetResponse();
                var len = res.ContentLength;
                request.Abort();
                return len > 100000;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public class VersionNumberComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                var pa = a.Split(new char[] { '.', '-' });
                var pb = b.Split(new char[] { '.', '-' });

                for (var i = 0; i < Math.Min(pa.Length, pb.Length); i++)
                {
                    int va;
                    int vb;
                    if (int.TryParse(pa[i], out va) && int.TryParse(pb[i], out vb) && va != vb) return va.CompareTo(vb);
                    else if (pa[i] != pb[i]) return String.Compare(pa[i], pb[i], StringComparison.Ordinal);
                }
                return pa.Length.CompareTo(pb.Length);
            }
        }

        private static void Unpack7zArchive(string archiveFileName, string targetDirectory, string password = null)
        {
            Unpack7zArchive(archiveFileName, targetDirectory, password != null ? ManagedLzma.PasswordStorage.Create(password) : null);
        }

        private static void Unpack7zArchive(string archiveFileName, string targetDirectory, ManagedLzma.PasswordStorage password)
        {
            if (!File.Exists(archiveFileName))
                throw new FileNotFoundException("Archive not found.", archiveFileName);

            // Ensure that the target directory exists.
            Directory.CreateDirectory(targetDirectory);

            using (var archiveStream = new FileStream(archiveFileName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {
                var archiveMetadataReader = new ManagedLzma.SevenZip.FileModel.ArchiveFileModelMetadataReader();
                var archiveFileModel = archiveMetadataReader.ReadMetadata(archiveStream, password);
                var archiveMetadata = archiveFileModel.Metadata;

                for (int sectionIndex = 0; sectionIndex < archiveMetadata.DecoderSections.Length; sectionIndex++)
                {
                    var sectionReader = new ManagedLzma.SevenZip.Reader.DecodedSectionReader(archiveStream, archiveMetadata, sectionIndex, password);
                    var sectionFiles = archiveFileModel.GetFilesInSection(sectionIndex);

                    // The section reader is constructed from metadata, if the counts do not match there must be a bug somewhere.
                    System.Diagnostics.Debug.Assert(sectionFiles.Count == sectionReader.StreamCount);

                    // The section reader iterates over all files in the section. NextStream advances the iterator.
                    for (; sectionReader.CurrentStreamIndex < sectionReader.StreamCount; sectionReader.NextStream())
                    {
                        var fileMetadata = sectionFiles[sectionReader.CurrentStreamIndex];

                        // The ArchiveFileModelMetadataReader we used above processes special marker nodes and resolves some conflicts
                        // in the archive metadata so we don't have to deal with them. In these cases there will be no file metadata
                        // produced and we should skip the stream. If you want to process these cases manually you should use a different
                        // MetadataReader subclass or write your own subclass.
                        if (fileMetadata == null)
                            continue;

                        // These asserts need to hold, otherwise there's a bug in the mapping the metadata reader produced.
                        System.Diagnostics.Debug.Assert(fileMetadata.Stream.SectionIndex == sectionIndex);
                        System.Diagnostics.Debug.Assert(fileMetadata.Stream.StreamIndex == sectionReader.CurrentStreamIndex);

                        // Ensure that the target directory is created.
                        var filename = Path.Combine(targetDirectory, fileMetadata.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(filename));

                        // NOTE: you can have two using-statements here if you want to be explicit about it, but disposing the
                        //       stream provided by the section reader is not mandatory, it is owned by the the section reader
                        //       and will be auto-closed when moving to the next stream or when disposing the section reader.
                        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Delete))
                            sectionReader.OpenStream().CopyTo(stream);

                        SetFileAttributes(filename, fileMetadata);
                    }
                }

                // Create empty files and empty directories.
                UnpackArchiveStructure(archiveFileModel.RootFolder, targetDirectory);
            }
        }

        private static void UnpackArchiveStructure(ManagedLzma.SevenZip.FileModel.ArchivedFolder folder, string targetDirectory)
        {
            if (folder.Items.IsEmpty)
            {
                // Empty folders need to be created manually since the unpacking code doesn't try to write into it.
                Directory.CreateDirectory(targetDirectory);
            }
            else
            {
                foreach (var item in folder.Items)
                {
                    var file = item as ManagedLzma.SevenZip.FileModel.ArchivedFile;
                    if (file != null)
                    {
                        // Files without content are not iterated during normal unpacking so we need to create them manually.
                        if (file.Stream.IsUndefined)
                        {
                            System.Diagnostics.Debug.Assert(file.Length == 0); // If the file has no content then its length should be zero, otherwise something is wrong.

                            var filename = Path.Combine(targetDirectory, file.Name);
                            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Delete))
                            {
                                // Nothing to do, FileMode.Create already truncates the file on opening.
                            }

                            SetFileAttributes(filename, file);
                        }
                    }

                    var subfolder = item as ManagedLzma.SevenZip.FileModel.ArchivedFolder;
                    if (subfolder != null)
                        UnpackArchiveStructure(subfolder, Path.Combine(targetDirectory, subfolder.Name));
                }
            }
        }

        private static void SetFileAttributes(string path, ManagedLzma.SevenZip.FileModel.ArchivedFile file)
        {
            if (file.Attributes.HasValue)
            {
                // When calling File.SetAttributes we need to preserve existing attributes which are not part of the archive

                var attr = File.GetAttributes(path);
                const FileAttributes kAttrMask = ArchivedAttributesExtensions.FileAttributeMask;
                attr = (attr & ~kAttrMask) | (file.Attributes.Value.ToFileAttributes() & kAttrMask);
                File.SetAttributes(path, attr);
            }
        }
    }
}

