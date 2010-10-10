using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Annotations;
using LuaManagerLib;
using LuaManagerLib.Tools;
using System.Diagnostics;

namespace SpringDownloader.LuaMgr
{
    public class WidgetHandler
    {
        const String luaZipTempFilename = "LuaFolderTemp.zip";
        public static readonly String springHome = Program.SpringPaths.WritableDirectory;
        public Dictionary<int, Category> categories = new Dictionary<int, Category>();
        //public static readonly String springHomeLuaUi = springHome + Path.DirectorySeparatorChar + "LuaUI";
        //public static ModBrowser modBrowser = new ModBrowser( Program.SpringPaths.WritableDirectory, "Mods");

        public Dictionary<String, Dictionary<String, Double>> configOrder = new Dictionary<String, Dictionary<String, Double>>();
        public static WidgetBrowser fetcher = new WidgetBrowser("http://widgetdb.springrts.de");
        public static FileManager fileManager = new FileManager(fetcher, springHome, Program.SpringPaths.DataDirectories.ToArray());
        public Dictionary<string, ModInfoDb> mods = new Dictionary<string, ModInfoDb>();
        //the mods listed in the db we have information what widgets are included
        public WidgetList widgetsActivate = new WidgetList();
        public WidgetList widgetsInstall = new WidgetList();
        public ArrayList widgetsUnknown = new ArrayList();

        public void activateDeactivateWidget(int luaId, bool activate, String modShort)
        {
            var ar = new List<int>();
            ar.Add(luaId);

            activateDeactivateWidgetsEx(ar, activate, modShort);
        }

        public void activateDeactivateWidgetsEx([NotNull] List<int> luaIds, bool activate, String modShort)
        {
            if (luaIds == null) throw new ArgumentNullException("luaIds");
            if (!configOrder.ContainsKey(modShort)) Trace.TraceWarning("WidgetHandler: Mod not found: " + modShort);

            var headerNames = getHeaderNamesInstalledByIds(luaIds);
            var nextId = getHighestLayerfromOrderConfig(300, modShort) + 1;

            var newOderConfig = new Dictionary<String, Double>();
            foreach( KeyValuePair<string,double> dict in configOrder[modShort] )
            {
                if (headerNames.Contains(dict.Key))
                {
                    if (activate && (dict.Value == 0.0)) newOderConfig[dict.Key] = Convert.ToDouble(nextId++);
                    else if (!activate && (dict.Value > 0.0)) newOderConfig[dict.Key] = 0.0;
                    else newOderConfig[dict.Key] = dict.Value;
                }
                else newOderConfig[dict.Key] = dict.Value;
            }

            configOrder[modShort] = newOderConfig;

            //update installed luas list
            foreach( int id in luaIds )
            {
                ((WidgetInfo)widgetsActivate[id]).activatedState[modShort] = activate;
            }

            //write new config file to disk
            try
            {
                fileManager.writeWidgetOrderConfig(springHome + Path.DirectorySeparatorChar + mods[modShort].configOrderFilename,
                                                   modShort,
                                                   configOrder[modShort]);
            }
            catch (Exception exp)
            {
                Trace.TraceWarning("Writing of config_lua.order failed! Error: " + exp.Message);
            }
        }

        public void addActivationStateInfoToInstalledList()
        {
            foreach (WidgetInfo winfo in widgetsActivate.Values)
            {
                foreach (var modKey in mods.Keys)
                {
                    var modShort = mods[modKey].abbreviation;
                    if (configOrder[modShort].ContainsKey(winfo.headerName))
                    {
                        var intState = configOrder[modShort][winfo.headerName];

                        if (intState > 0.0) winfo.activatedState[modShort] = true;
                        else winfo.activatedState[modShort] = false;
                    }
                    else winfo.activatedState[modShort] = winfo.headerDefaultEnable;
                }
            }
        }

        public void addComment(int nameId, string comment, string username, string password)
        {
            fetcher.addComment(nameId, comment, username, password);
        }

        public void addRating(int nameId, float rating, string username, string password)
        {
            fetcher.addRating(nameId, rating, username, password);
        }

        public String encodeLobbyPassword(String plainPw)
        {
            return Convert.ToBase64String(Md5Cached.string2Md5Binary(plainPw));
        }

        public void exportActivationPreset(String filename)
        {
            if (filename.Length <= 0) throw new Exception("Export: No filename!");

            var xml = new XmlDocument();
            XmlNode root = xml.CreateElement("widgetActivationPreset");
            xml.AppendChild(root);

            foreach (var modKey in configOrder.Keys)
            {
                XmlNode modNode = xml.CreateElement(modKey);
                foreach( KeyValuePair<string,double> entry in configOrder[modKey] )
                {
                    XmlNode name = xml.CreateElement("ActivationState");

                    var b = xml.CreateAttribute("Name");
                    b.InnerText = entry.Key;
                    name.Attributes.Append(b);

                    var a = xml.CreateAttribute("Layer");
                    a.InnerText = Convert.ToString(entry.Value);
                    name.Attributes.Append(a);

                    modNode.AppendChild(name);
                }
                root.AppendChild(modNode);
            }

            xml.Save(filename);
        }

        public void exportWidgetList(String filename)
        {
            if (filename.Length <= 0) throw new Exception("Export: No filename!");

            var xml = new XmlDocument();
            XmlNode root = xml.CreateElement("widgetListRoot");
            xml.AppendChild(root);
            foreach (WidgetInfo curWidget in widgetsInstall.Values)
            {
                if (curWidget.state == WidgetState.INSTALLED)
                {
                    XmlNode name = xml.CreateElement("NameID");

                    var a = xml.CreateAttribute("Id");
                    a.InnerText = curWidget.nameId.ToString();
                    name.Attributes.Append(a);
                    root.AppendChild(name);
                }
            }
            xml.Save(filename);
        }

        /*
         * You have to update installedLuas first before calling this!
         */

        public float? getPersonalRating(string username, string password, int nameId)
        {
            return fetcher.getUserRating(username, password, nameId);
        }

        public WidgetInfo getWidgetFromDbListRelatedToHddList(int luaIdInHddList)
        {
            return widgetsInstall.getByNameId((widgetsActivate[luaIdInHddList] as WidgetInfo).nameId);
        }

        public void installLua(int luaId, Boolean incDownload)
        {
            var ovwLuas = widgetsInstall;
            var installedLuas = widgetsActivate;
            if (installedLuas.ContainsKey(luaId)) return;

            //install files
            var list = fetcher.getFileListByLuaId(luaId);
            fileManager.installFiles(list);

            //update internal variables
            var newWidget = fetcher.getLuaById(luaId);
            newWidget.fileList = list;
            fileManager.addHeaderFileInfoFromUserWidget(ref newWidget);
            createModListInWidget(ref newWidget);

            var wasConfd = false;
            var confValue = false;

            foreach (var modKey in mods.Keys)
            {
                if (configOrder[modKey].ContainsKey(newWidget.headerName))
                {
                    //the activation information is still in configOrder, use it
                    newWidget.activatedState[modKey] = false;
                    var confOrderVal = Convert.ToInt32(configOrder[modKey][newWidget.headerName]);
                    if (confOrderVal > 0) newWidget.activatedState[modKey] = true;
                }
                else
                {
                    //activation state was read from defaultenable before here
                    if (newWidget.activatedState[modKey] == false)
                    {
                        if (wasConfd == false)
                        {
                            //we should not be doing gui stuff here, but no quick way around
                            if (
                                MessageBox.Show(
                                    "\"" + newWidget.name + "\" is deactivated by default. Do you want to activate it now for all games?",
                                    "Activation",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question) == DialogResult.Yes) confValue = true;
                            wasConfd = true;
                        }

                        if (confValue) newWidget.activatedState[modKey] = true;
                    }
                }
            }

            installedLuas.Add(luaId, newWidget);

            var ovwViewWidget = ovwLuas.getByNameId(newWidget.nameId);
            if (ovwViewWidget.id == newWidget.id) ovwViewWidget.state = WidgetState.INSTALLED;
            else
            {
                //seems to be not the latest version -> its an older version
                ovwViewWidget.state = WidgetState.OUTDATED;
            }

            if (incDownload)
            {
                ovwLuas.getByNameId(newWidget.nameId).downloadCount++;
                fetcher.incDownloadCounter(luaId);
            }
        }

        public void installWidgetList(WidgetList luaList)
        {
            uninstallAll();

            foreach (WidgetInfo curWidget in luaList.Values) installLua(curWidget.id, true);
        }

        public void loadActivationPreset(String filename)
        {
            var filePreset = loadActivationPresetFromFile(filename);

            //build two list for each mod. one contains widgets to active, one contains widgets to deactivate
            var activateList = new Dictionary<string, Dictionary<bool, List<int>>>();

            foreach (var modShort in filePreset.Keys)
            {
                activateList.Add(modShort, new Dictionary<bool, List<int>>());
                activateList[modShort].Add(true, new List<int>());
                activateList[modShort].Add(false, new List<int>());

                foreach (var name in filePreset[modShort].Keys)
                {
                    var winfo = widgetsActivate.getByHeaderName(name);
                    if (winfo != null)
                    {
                        var luaId = winfo.id;
                        var active = filePreset[modShort][name];
                        activateList[modShort][active].Add(luaId);
                    }
                }
            }

            //execute each mod. each order file will be written twice (my bad)
            //once for activation, once for deactivation
            foreach (var modKey in activateList.Keys)
            {
                activateDeactivateWidgetsEx(activateList[modKey][true], true, modKey);
                activateDeactivateWidgetsEx(activateList[modKey][false], false, modKey);
            }
        }

        public WidgetList loadWidgetListFromXmlFile(String filename)
        {
            var widgets = new WidgetList();

            var xml = new XmlDocument();
            xml.Load(filename);

            XmlNode root = xml.DocumentElement;
            foreach( XmlNode widgetXml in root )
            {
                var strId = widgetXml.Attributes.GetNamedItem("Id").InnerText;
                widgets.Add(int.Parse(strId), widgetsInstall.getByNameId(int.Parse(strId)));
            }

            return widgets;
        }

        public void refreshCategories()
        {
            categories = fetcher.getCategories();
        }

        /*
         * installedLuas list has to be uptodate before calling this
         */

        public void refreshDbWidgetList()
        {
            //1. Get ALL widgets ALL version and check exactly which ones are installed
            var allList = fetcher.getAllLuasActive();
            widgetsActivate = fileManager.checkInstalledLuas(allList, ref widgetsUnknown);

            //3. Fetch DB list of _latest versions_
            refreshDbList();
        }

        public void refreshHddWidgetList()
        {
            addLocalWidgets();

            addModWidgets();

            addActivationStateInfoToInstalledList();
        }

        /*
         * Used for widget activation
         */

        public void refreshModList()
        {
            //change this lines to switch from db-mod-mode to mod-parse-mode
            // this.mods = modBrowser.getModList();
            Dictionary<string, ModInfoDb> allKnownMods = fetcher.getActivationMods();
            addInstalledMods(allKnownMods);
            readAllOrderConfigs();
        }

        /*
         * Uninstall widget
         */

        public void removeLua(int luaId)
        {
            if (!widgetsActivate.ContainsKey(luaId)) return;

            var list = fetcher.getFileListByLuaId(luaId);

            fileManager.removeFiles(list);
            var listedWidget = widgetsInstall.getByNameId((widgetsActivate[luaId] as WidgetInfo).nameId);
            listedWidget.state = WidgetState.NOT_INSTALLED;

            widgetsActivate.Remove(luaId);
        }

        public void uninstallWidget(int luaId)
        {
            var listedWidget = widgetsInstall.getByNameId((widgetsActivate[luaId] as WidgetInfo).nameId);
            removeLua(luaId);
        }

        public void updateWidget(WidgetInfo newWidget)
        {
            var current = widgetsActivate.getByNameId(newWidget.nameId);

            //remove old one
            removeLua(current.id);

            //install latest widget
            installLua(newWidget.id, false);
        }

        /*
        public void resetLuaFolder()
        {
            String folderSuffixOld = "/LuaUI_old";
            String folderSuffixCur = "/LuaUI";
            String folderSuffixOrg = "/LuaUI_original";

            String path = Program.SpringPaths.WritableDirectory + System.IO.Path.DirectorySeparatorChar + luaZipTempFilename;
            try
            {
                fetcher.downloadOriginalLuaFolder(path);
            }
            catch (Exception exp)
            {
                throw new Exception("Error downloading file! Reason: " + exp.Message);
            }

            try
            {
                try
                {
                    Directory.Delete(Program.SpringPaths.WritableDirectory + folderSuffixOld, true);
                }
                catch (Exception)
                {
                }

                Directory.Move(Program.SpringPaths.WritableDirectory+ folderSuffixCur, Program.SpringPaths.WritableDirectory + folderSuffixOld);
            }
            catch (Exception ex)
            {
                File.Delete(path);
                throw new Exception("Could rename current lua folder! Reason: " + ex.Message);
            }


            ZipTools.UnZipFiles(path, Program.SpringPaths.WritableDirectory, "", true);

            try
            {
							Directory.Move(Program.SpringPaths.WritableDirectory + folderSuffixOrg, Program.SpringPaths.WritableDirectory + folderSuffixCur);
            }
            catch (Exception ex)
            {
                //rename old folder back
                Directory.Move(Program.SpringPaths.WritableDirectory + folderSuffixOld, Program.SpringPaths.WritableDirectory + folderSuffixCur);
                throw new Exception("Could rename new lua folder! Reason: " + ex.Message);
            }

            try
            {
                Directory.Delete(Program.SpringPaths.WritableDirectory + folderSuffixOld, true);
            }
            catch (Exception ex)
            {
                throw new Exception("Could delete folder LuaUI_old! Reason: " + ex.Message);
            }
        }
        */


        public void zipDownloadWidget(int luaId, String localPath)
        {
            var wc = new WebClient { Proxy = null };
            var url = fetcher.getZipDownloadUrl(luaId);
            wc.DownloadFile(url, localPath);
        }

        void addLocalWidgets()
        {
            var path = Path.Combine(springHome, "LuaUI/Widgets/");

            if (!Directory.Exists(path)) return;

            //can clash with overviewList: int fakeId = installedLuas.getHighestId() + 1;
            //better use the big hammer:
            var fakeId = 100000;
            foreach (var s in Directory.GetFiles(path, "*.lua"))
            {
                try
                {
                    var winfo = fileManager.readWidgetFile(s);

                    var curId = widgetsActivate.getIdByContainingFilename(s, springHome);
                    if (curId != -1)
                    {
                        var curWidget = (WidgetInfo)widgetsActivate[curId];
                        curWidget.addFileHeaderInfo(winfo);
                    }
                    else
                    {
                        //only add widgets with unique headerName
                        if (widgetsActivate.getByHeaderName(winfo.headerName) == null)
                        {
                            winfo.id = fakeId++;
                            widgetsActivate.Add(winfo.id, winfo);
                        }
                    }
                }
                catch (Exception exp)
                {
                    Trace.TraceInformation("Widget parse error: " + exp.Message);
                }
            }
        }

        void addModWidgets()
        {
            //can clash with overviewList: int fakeId = installedLuas.getHighestId() + 1;
            //better use the big hammer:
            var fakeId = 200000;
            foreach (var modElem in mods.Values)
            {
                var modName = modElem.abbreviation;
                var widgetList = modElem.modWidgets;
                foreach (WidgetInfo modWidget in widgetList.Values)
                {
                    if (widgetsActivate.getByHeaderName(modWidget.headerName) != null)
                    {
                        //do not add widget, its already there -> installed by user
                        continue;
                    }

                    // WidgetInfo info = new WidgetInfo();
                    //info.headerDescription = modWidget.headerDescription;
                    modWidget.oderIsAvail = true;
                    modWidget.orderName = modWidget.headerName;
                    //info.headerName = name;
                    modWidget.modName = modName;
                    modWidget.id = fakeId++;
                    modWidget.activatedState[modName] = true; //this can be wrong since we dont know the widgets' default enable state

                    //check if widget is found in order_config.lua
                    if (configOrder[modName].ContainsKey(modWidget.headerName) && configOrder[modName][modWidget.headerName] == 0.0) modWidget.activatedState[modName] = false;
                    widgetsActivate.Add(modWidget.id, modWidget);
                }
            }
        }

        void createModListInWidget(ref WidgetInfo widget)
        {
            widget.activatedState.Clear();
            foreach (var modKey in mods.Keys) widget.activatedState.Add(modKey, widget.headerDefaultEnable);
        }

        void addInstalledMods(Dictionary<string, ModInfoDb> allKnownMods)
        {
            foreach (var modKey in allKnownMods.Keys) if (File.Exists(springHome + Path.DirectorySeparatorChar + allKnownMods[modKey].configOrderFilename)) mods.Add(modKey, allKnownMods[modKey]);
        }

        ArrayList getHeaderNamesInstalledByIds(List<int> luaIds)
        {
            var names = new ArrayList();

            foreach( WidgetInfo info in widgetsActivate.Values )
            {
                if (luaIds.Contains(info.id)) names.Add(info.headerName);
            }

            return names;
        }

        int getHighestLayerfromOrderConfig(int max, string modShort)
        {
            var maxLayer = 0.0;
            foreach( double val in configOrder[modShort].Values )
            {
                if (val < Convert.ToDouble(max)) maxLayer = Math.Max(maxLayer, Convert.ToInt32(val));
            }
            return Convert.ToInt32(maxLayer);
        }

        Dictionary<String, Dictionary<String, bool>> loadActivationPresetFromFile(String filename)
        {
            var modTab = new Dictionary<String, Dictionary<String, bool>>();

            var xml = new XmlDocument();
            xml.Load(filename);

            XmlNode root = xml.DocumentElement;
            foreach( XmlNode modXml in root )
            {
                var modShort = modXml.Name;

                var tab = new Dictionary<String, bool>();
                foreach( XmlNode widgetXml in modXml )
                {
                    var name = widgetXml.Attributes.GetNamedItem("Name").InnerText;
                    var layer = widgetXml.Attributes.GetNamedItem("Layer").InnerText;

                    var active = false;
                    if (Convert.ToInt32(layer) > 0) active = true;
                    tab.Add(name, active);
                }
                modTab.Add(modShort, tab);
            }

            return modTab;
        }

        void readAllOrderConfigs()
        {
            foreach (var mod in mods.Values)
            {
                try
                {
                    configOrder[mod.abbreviation] =
                        fileManager.readWidgetOrderConfig(springHome + Path.DirectorySeparatorChar + mod.configOrderFilename);
                }
                catch (Exception exp)
                {
                    Trace.TraceWarning("Reading widget_order.lua failed! Error: " + exp.Message);
                    //put an empty list in place to cover the error. some activation stuff wont work though.
                    configOrder[mod.abbreviation] = new Dictionary<String, Double>();
                }
            }
        }

        void refreshDbList()
        {
            widgetsInstall = fetcher.getOverviewList();

            //Add information if widgets is currently installed
            foreach( WidgetInfo info in widgetsInstall.Values )
            {
                if (widgetsActivate.ContainsKey(info.id))
                {
                    info.state = WidgetState.INSTALLED;
                    //set also for the installed version
                    ((WidgetInfo)widgetsActivate[info.id]).state = info.state;
                }
                else if (widgetsActivate.getOlderVersion(info) != null)
                {
                    info.state = WidgetState.OUTDATED;
                    //set also for the installed version
                    widgetsActivate.getOlderVersion(info).state = info.state;
                }
                else if (widgetsUnknown.Contains(info.nameId)) info.state = WidgetState.UNKNOWN_VERSION;
                else info.state = WidgetState.NOT_INSTALLED;
            }
        }

        void uninstallAll()
        {
            foreach (WidgetInfo curWidget in widgetsInstall.Values) if (curWidget.state == WidgetState.INSTALLED) removeLua(curWidget.id);
        }
    }
}