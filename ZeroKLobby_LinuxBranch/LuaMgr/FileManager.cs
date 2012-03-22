using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Annotations;
using LuaManagerLib;
using LuaManagerLib.Tools;
using PlasmaShared;
using FileInfo=LuaManagerLib.FileInfo;
using Utils=PlasmaShared.Utils;

namespace ZeroKLobby
{
    public class FileManager
    {
        protected const string widgetKeyFileRegEx = "^(/*|\\*)LuaUI(/*|\\*)Widgets(/*|\\*)([^/|\\*]*)\\.lua$";
        protected WidgetBrowser fetcher;
        readonly SimpleLuaParser luaParser;
        protected Md5Cached md5Cached = new Md5Cached();
        readonly Dictionary<string, bool> springDataPathes = new Dictionary<string, bool>();
        readonly String springHomePath;
        //spring home directory. this is where new wigdgets get installed. e.g. My Documents/My Games/Spring, or ~/.spring. Can also be the same as startup path when there is write access to spring startup folder

        public FileManager(WidgetBrowser fetcher, String springHomePath, [NotNull] string[] springDataPathes) //, String[] luaAddOnDirs)
        {
            if (springDataPathes == null) throw new ArgumentNullException("springDataPathes");
            this.fetcher = fetcher;
            this.springHomePath = springHomePath;
            foreach (var path in springDataPathes)
            {
                var writable = false;
                if (SpringPaths.IsDirectoryWritable(path)) writable = true;
                this.springDataPathes.Add(path, writable);
            }

            //add the home path manually for convenience so we dont have to deal with it separately
            if (!this.springDataPathes.ContainsKey(this.springHomePath)) this.springDataPathes.Add(this.springHomePath, true);

            luaParser = new SimpleLuaParser(this.springHomePath, springDataPathes);
        }

        public void addHeaderFileInfoFromUserWidget([NotNull] ref WidgetInfo info)
        {
            if (info == null) throw new ArgumentNullException("info");
            var keyFile = getKeyFileName(info.fileList);
            try
            {
                var fullpath = PlasmaShared.Utils.MakePath(springHomePath, keyFile);
                if (!File.Exists(fullpath)) return;

                var headerInfo = readWidgetFile(fullpath);
                info.addFileHeaderInfo(headerInfo);
            }
            catch (Exception exp)
            {
            	Trace.TraceError("Widget header file info error: " + info.name + "! Error: " + exp.Message);
              return;
            }
        }

        public bool checkInstalledLua([NotNull] LinkedList<FileInfo> files)
        {
            if (files == null) throw new ArgumentNullException("files");
            var installed = true;
            foreach (var file in files)
            {
                string foundFilePath = null;
                foreach (var item in springDataPathes)
                {
                    var path = PlasmaShared.Utils.MakePath(item.Key, file.localPath);
                    if (File.Exists(path))
                    {
                        foundFilePath = path;
                        break;
                    }
                }

                if (foundFilePath == null)
                {
                    installed = false;
                    continue;
                }

                var md5str = md5Cached.file2Md5(foundFilePath);
                if (md5str != file.Md5)
                {
                    installed = false;
                    break;
                }
            }
            return installed;
        }

        public WidgetList checkInstalledLuas([NotNull] WidgetList luaList, [NotNull] ref ArrayList unknownLuas)
        {
            if (luaList == null) throw new ArgumentNullException("luaList");
            if (unknownLuas == null) throw new ArgumentNullException("unknownLuas");
            md5Cached.clearCache(); //clear cache, maybe files have changed
            unknownLuas.Clear();

            //prefetch file lists
            var luaIds = new List<int>();
            foreach (WidgetInfo winfo in luaList.Values) luaIds.Add(winfo.id);
            var fileInfos = fetcher.getFileListByLuaIds(luaIds);
            var dict = new Dictionary<int, LinkedList<FileInfo>>();
            foreach (var fi in fileInfos)
            {
                if (!dict.ContainsKey(fi.luaId)) dict.Add(fi.luaId, new LinkedList<FileInfo>());
                dict[fi.luaId].AddLast(fi);
            }

            var installedLuas = new WidgetList();

            var foundNameIds = new ArrayList();
            foreach (WidgetInfo info in luaList.Values)
            {
                if (foundNameIds.Contains(info.nameId)) continue; //this widget is installed, there cant be a second version

                if (!dict.ContainsKey(info.id))
                {
                    //this widget has no files. (???)
                    continue;
                }

                // var doc = new XmlDocument();
                var list = dict[info.id]; // fetcher.getFileListByLuaId(info.id);

                if (checkInstalledLua(list) == true)
                {
                    //read widget file header
                    //WidgetInfo widgetFileInfo = this.readWidgetFile( keyfile, false );
                    //info.addFileHeaderInfo(widgetFileInfo);

                    //add widgets file list
                    info.fileList = list;

                    installedLuas.Add(info.id, info);
                    foundNameIds.Add(info.nameId);

                    unknownLuas.Remove(info.nameId); //remove if another unknown version was found
                }
                else
                {
                    //check if widgets key file exists -> unknonw version
                    //add the widgets NAME ID to the unknownLuas                   
                    var keyfile = getKeyFileName(list);
                    foreach (var item in springDataPathes)
                    {
                        var path = PlasmaShared.Utils.MakePath(item.Key, keyfile);
                        if (File.Exists(path))
                        {
                            unknownLuas.Add(info.nameId);
                            break;
                        }
                    }
                }
            }

            return installedLuas;
        }

        public void installFiles(LinkedList<FileInfo> fileList)
        {
            IEnumerator ienum = fileList.GetEnumerator();
            //check if there are already existing and ask for overwrite confirmation
            foreach (var file in fileList)
            {
                var fullFilePath = PlasmaShared.Utils.MakePath(springHomePath, file.localPath);
                if (File.Exists(fullFilePath))
                {
                    if (
                        MessageBox.Show("The file \"" + fullFilePath + "\" does already exist. Overwrite?",
                                        "Confirmation",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Question) == DialogResult.No) throw new Exception("Installation aborted!");
                }
            }

            //download and install each file
            foreach (var file in fileList)
            {
                var fullFilePath = PlasmaShared.Utils.MakePath(springHomePath, file.localPath);

                createSubdirsForFile(fullFilePath);

                using (var wc = new WebClient() { Proxy = null }) {
                    wc.DownloadFile(file.Url, fullFilePath);
                }

                var md5str = Md5Cached.file2Md5Uncached(fullFilePath);
                if (md5str != file.Md5) throw new Exception("MD5 mismatch!");
            }
        }

        /// <summary>
        /// Reads the given Widget file and returns its info header as WigetInfo ojbect
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="itsFullPath"></param>
        /// <returns></returns>
        public WidgetInfo readWidgetFile(String fullFilename)
        {
            var winfo = new WidgetInfo();
            winfo.headerIsAvail = true;
            winfo.state = WidgetState.INSTALLED_LOCALLY;
            //winfo.headerSourceFile = filename;

            var ar = luaParser.getWidgetInfoHeaderFile(fullFilename);
            winfo.addFileHeaderInfo(ar);
            //winfo.activatedState = winfo.headerDefaultEnable; 

            return winfo;
        }

        /// <summary>
        /// Get widget order configuration as Hashtable
        /// </summary>
        /// <returns>Hashtable</returns>
        public Dictionary<String, Double> readWidgetOrderConfig(String orderConfigFile)
        {
            return luaParser.getWidgetConfigOrder(orderConfigFile);
            ;
        }

        public void removeFiles(LinkedList<FileInfo> fileList)
        {
            foreach (var file in fileList)
            {
                string fullFilePath = null;
                foreach (var item in springDataPathes)
                {
                    var path = PlasmaShared.Utils.MakePath(item.Key, file.localPath);
                    if (File.Exists(path))
                    {
                        //we found the file
                        if (!item.Value)
                        {
                            //but in a non-writable direcory
                            var destPath = PlasmaShared.Utils.MakePath(springHomePath, file.localPath);
                            throw new Exception("The file \"" + path +
                                                "\" is located in a directory without write access. Please move this file (and all other files belonging to this widget) to \"" +
                                                destPath + "\"");
                        }
                        fullFilePath = path;
                        break;
                    }
                }

                if (fullFilePath == null)
                {
                    //we should not get here
                    throw new Exception("File to remove not found: \"" + file.localPath + "\"!");
                }

                var md5str = Md5Cached.file2Md5Uncached(fullFilePath);
                if (md5str != file.Md5) throw new Exception("MD5 mismatch!");

                File.Delete(fullFilePath);
            }
        }

        /// <summary>
        /// Write widget order configuration from Hashtable
        /// </summary>
        /// <returns></returns>
        public void writeWidgetOrderConfig(String orderConfigFile, String modShort, Dictionary<String, Double> config)
        {
            luaParser.writeOrderConfigFile(orderConfigFile, config);
        }

        static void createSubdirsForFile(string fullFilePath)
        {
            var finfo = new System.IO.FileInfo(fullFilePath);
            Directory.CreateDirectory(finfo.DirectoryName);
        }

        protected static string getKeyFileName([NotNull] IEnumerable<FileInfo> files)
        {
            if (files == null) throw new ArgumentNullException("files");
            foreach (var file in files)
            {
                var m = Regex.Match(file.localPath, widgetKeyFileRegEx, RegexOptions.IgnoreCase);
                if (m.Success) return file.localPath;
            }

            return null;
        }
    }
}