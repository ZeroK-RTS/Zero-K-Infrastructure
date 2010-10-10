using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using MPIniReadWrite;
using System.Text.RegularExpressions;

namespace LuaManagerLib
{
    public class ModBrowser
    {
        private List<ModInfoDb> modList = new List<ModInfoDb>();
        private string modFolder = "";
        private string springFolder = "";
        private string dataFileName = "";
        
        protected const string widgetKeyFileRegEx = "^(/*|\\*)LuaUI(/*|\\*)Widgets(/*|\\*)([^/|\\*]*)\\.lua$";
        
        private SimpleLuaParser luaParser = null;

        public ModBrowser( string springFolder, string modFolderName)
        {
            this.springFolder = springFolder;
            this.modFolder = Path.Combine(this.springFolder, modFolderName );
            this.dataFileName = Path.Combine(this.springFolder, "sd_modWidgets.dat");
            this.luaParser = new SimpleLuaParser( this.springFolder );

            LoadFileEntries();
        }

        public List<ModInfoDb> getModList()
        {
            return scanFolderAndAdd( );
        }

        private List<ModInfoDb> scanFolderAndAdd()
        {
            Scan();

            return this.modList;
        }

        public void SaveFileEntries()
        {
      //      lock (saveLock)
            {
                string finalName = this.dataFileName; //TODO
                var bf = new BinaryFormatter();
                string tempFileName = Path.GetTempFileName();
                using (var fs = new FileStream(tempFileName, FileMode.Create)) bf.Serialize(fs, modList);
                try
                {
                    File.Delete(finalName);
                }
                catch { }

                File.Move(tempFileName, finalName);
            }
        }

        public void LoadFileEntries()
        {
            string fileCache = this.dataFileName;
            if (File.Exists(fileCache))
            {
                using (var fs = new FileStream(fileCache, FileMode.Open))
                {
                    try
                    {
                        modList = (List<ModInfoDb>)new BinaryFormatter().Deserialize(fs);
                       // InvokeEvent(InstalledVersionsChanged);
                    }
                    catch (Exception e)
                    {
                        //Program.NotifyError(e, "loading cache failed - recreating");
                    }
                }
            }
            //else Program.SaveConfig();
        }


        public void Scan()
        {
            const string status = "Scanning mods";
            //Program.Notify(status);

            var pathsToAddPool = new List<string>();

            long totalSize = 0;

            List<string> searchPatterns = new List<string>();
            searchPatterns.Add("*.sdz");
            searchPatterns.Add("*.sd7");

            foreach (string pattern in searchPatterns)
            {
                foreach (var s in Directory.GetFiles(this.modFolder, pattern, SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(s);

                    if ( modList.Find(delegate(ModInfoDb midb) { return midb.modFilenames.Contains(name); }) != null )
                    //if (modList.Find(m => (m.modFilenames.Contains(name).ToString() )) )
                    {
                        continue;
                    }

                    pathsToAddPool.Add(s);
                }
            }
            
            foreach (var s in pathsToAddPool)
            {
                try
                {
                    //do this first, so we are later able to skip file where error occured
                    ModInfoDb mod = new ModInfoDb();
                    mod.modFilenames.Add(Path.GetFileName(s));
                    this.modList.Add(mod);

                    ZipFile z = new ZipFile(s);

                    //read mod-shortname from modinfo.tdf
                    ZipEntry ze = z.GetEntry("modinfo.tdf");
                    if (ze == null)
                    {
                        //todo: check if modinfo.lua exists instead and parse it
                        continue;
                    }
                    Stream modHeader = z.GetInputStream(ze);
                    MultiplatformIni ini = new MultiplatformIni(modHeader);
                    string shortname = ini.ReadString("MOD", "shortname");

                    if (shortname == null)
                    {
                        //this is not a mod, probably mission or something
                        continue;
                    }

                    mod.abbreviation = shortname.Trim(';');

                    WidgetList wlist = new WidgetList();
                    mod.modWidgets = wlist;               

                    foreach (ZipEntry cur in z)
                    {
                        if (!cur.IsFile)
                        {
                            continue;
                        }

                        Match m = Regex.Match(cur.Name, ModBrowser.widgetKeyFileRegEx, RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            try
                            {
                                TextReader reader = new StreamReader(z.GetInputStream(cur));
                                Dictionary<string, Object> widgetHeader = this.luaParser.getWidgetInfoHeaderString(reader.ReadToEnd());

                                WidgetInfo winfo = new WidgetInfo();
                                winfo.addFileHeaderInfo(widgetHeader);

                                wlist.Add( wlist.Count, winfo);
                            }
                            catch (Exception exp)
                            {
                                Console.WriteLine("ModWidget-Error in file \"" + s + "\" in widget \"" + cur.Name + "\": " + exp.Message );
                            }
                        }
                    }

                    

                    
                    //this.fileManager.readWidgetFile();
                    //  if (TaskManager.Cancel) break;
                    //                AddFile(s, totalSize, ref doneSize);
                }
                catch (Exception exp)
                {
                    Console.WriteLine("(Zip) Exception in file \"" + s + "\": " + exp.Message);
                }
            }


            SaveFileEntries();
            //Program.Notify("{0} complete", status);

            //if (pathsToAddPool.Count > 0 || todel.Count > 0) InvokeEvent(InstalledVersionsChanged);
        }
    }
}
