using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using LuaSharp;

namespace LuaManagerLib
{
    public class SimpleLuaParser
    {
        private class CachedWidgetInfo
        {
            public string fullFilePath;
            public DateTime lastWrite = new DateTime(0);
            public Dictionary<string, Object> widgetHeader;
        }

        private class CachedOrderInfo
        {
            public DateTime lastWrite = new DateTime(0);
            public Dictionary<string, Double> widgets;
        }
    
        private Dictionary<String, CachedWidgetInfo> widgetHeaderCache;
        private Dictionary<String, CachedOrderInfo> orderFileCache = new Dictionary<String, CachedOrderInfo>(); //key is orderConfigFile
        private Lua lua = new Lua();

        private Dictionary<String, String> luaBaseFiles = new Dictionary<string, string>();

        //on windows, both point to the same path (e.g. d:\games\spring)
        //on linux: home is something like /home/peter/.spring and sharePath /usr/share/spring
        public SimpleLuaParser(String springHomePath, string[] springDataPathes)
        {
            //this.springPath = springPath ;
            widgetHeaderCache = new Dictionary<String, CachedWidgetInfo>();

            //setup a list of luas we need
            List<String> neededLuas = new List<string>();
            neededLuas.Add("utils.lua");
            neededLuas.Add("main.lua");
            neededLuas.Add("fonts.lua");
            neededLuas.Add("system.lua");
            neededLuas.Add("Headers/keysym.h.lua");
            neededLuas.Add("Headers/colors.h.lua");
            neededLuas.Add("savetable.lua");

            //find needed lua base files in data pathes
            foreach (String baseFile in neededLuas)
            {
                foreach (String dataPath in springDataPathes)
                {
                    String fullpath = dataPath + Path.DirectorySeparatorChar + "LuaUI" + Path.DirectorySeparatorChar + baseFile;
                    if (File.Exists(fullpath))
                    {
                        //found it here
                        luaBaseFiles.Add(baseFile, fullpath);
                        break;
                    }
                }

                if ( !luaBaseFiles.ContainsKey(baseFile) )
                {
                    String exMsg = "Could not find \"" + baseFile + "\" in any of springs data pathes! ";
                    exMsg += " I tried the following pathes: ";
                    foreach (String dataPath in springDataPathes)
                    {
                        exMsg += dataPath + Path.DirectorySeparatorChar + "LuaUI" + Path.DirectorySeparatorChar + baseFile;
                        exMsg += ",";
                    }

                    throw new Exception(exMsg);
                }
            }
        }

        //call this after creating a fresh lua object to setup a "fake" spring-lua-environment
        private void setupDummySpringLuaEnvironment()
        {
            string cmd = "";
            cmd += "LUAUI_DIRNAME = \"/\" ";
            cmd += "LUAUI_VERSION = \"1.0\" ";

            cmd += "Spring = {} ";
            cmd += "mt = { __index = function (t,k) local function f() end return f end} ";
            cmd += "setmetatable( Spring, mt ) ";
            
            cmd += "function setDummyMeta( t ) mt = { __index = function (t,k) local function f() end return f end} setmetatable(t,mt) end ";
            cmd += "widget = {} ";
            cmd += "setDummyMeta( widget ) ";

            cmd += "VFS = {}" ;
            cmd += "setDummyMeta( VFS ) ";

            cmd += "GL = {}" ;
            cmd += "setDummyMeta( GL ) ";

            cmd += "gl = {}" ;
            cmd += "setDummyMeta( gl ) ";

            cmd += "widgetHandler = {} ";
            cmd += "setDummyMeta( widgetHandler ) ";

            cmd += "widgetHandler = {} ";
            cmd += "setDummyMeta( widgetHandler ) ";

            cmd += "Script = {} ";
            cmd += "setDummyMeta( Script ) ";

            cmd += "Game = {} ";
            cmd += "setDummyMeta( Game ) ";

            cmd += "CMD = {} ";
            cmd += "setDummyMeta( CMD ) ";

            cmd += "CMDTYPE = {}" ;
            cmd += "setDummyMeta( CMDTYPE ) ";
            
            cmd += "math = {}" ;
            cmd += "setDummyMeta( math ) ";

            lua.DoString(cmd);

            lua.DoFile(luaBaseFiles["utils.lua"]);
            lua.DoFile(luaBaseFiles["main.lua"]);
            lua.DoFile(luaBaseFiles["fonts.lua"]);
            lua.DoFile(luaBaseFiles["system.lua"]);
            lua.DoFile(luaBaseFiles["Headers/keysym.h.lua"]);
            lua.DoFile(luaBaseFiles["Headers/colors.h.lua"]);
        }

        private static Dictionary<String, Object> luaTable2DictionaryObject(LuaTable lTable)
        {
            var ht = new Dictionary<string, Object>();

            IDictionaryEnumerator ienum = (IDictionaryEnumerator)lTable.GetEnumerator();
            while (ienum.MoveNext())
            {
                ht.Add((String)ienum.Key, ienum.Value);
            }
            
            return ht;
        }

        private static Dictionary<string, Double> luaOrderTable2DictionaryDouble(LuaTable lTable)
        {
            Dictionary<string, Double> ht = new Dictionary<string, Double>();

            //probably any mod can do its custom format for the order config file
            //but the standard seems to be to return two tables ("order" and "data");
            //CA uses a dedicated order file returning the order table directly (like in older spring)
            LuaTable orderTab = (LuaTable)lTable.GetValue("order");
            LuaTable dataTab = (LuaTable)lTable.GetValue("data");

            if (orderTab != null && dataTab != null)
            {
                //we have standard format
                IDictionaryEnumerator ienum = (IDictionaryEnumerator)orderTab.GetEnumerator();
                while (ienum.MoveNext())
                {
                    ht.Add((String)ienum.Key, (Double)ienum.Value);
                }
            }
            else
            {
                //we have CA-format: dedicated order file
                IDictionaryEnumerator ienum = (IDictionaryEnumerator)lTable.GetEnumerator();
                while (ienum.MoveNext())
                {
                    ht.Add((String)ienum.Key, (Double)ienum.Value);
                }
            }
            
            return ht;
        }

        public Dictionary<string, Object> getWidgetInfoHeaderFile(String filename)
        {
            if (!System.IO.File.Exists(filename))
            {
                throw new Exception("Widget file not found: " + filename);
            }

            DateTime lastWrite = File.GetLastWriteTime(filename);
            if (widgetHeaderCache.ContainsKey(filename) && widgetHeaderCache[filename].lastWrite == lastWrite )
            {
                return widgetHeaderCache[filename].widgetHeader;
            }
            else
            {
                CachedWidgetInfo cwinfo = new CachedWidgetInfo();
                cwinfo.fullFilePath = filename;
                cwinfo.lastWrite = lastWrite;
                cwinfo.widgetHeader = getWidgetInfoHeaderEx(filename, false);

                if ( widgetHeaderCache.ContainsKey( filename ) )
                {
                    widgetHeaderCache.Remove(filename);
                }
                widgetHeaderCache.Add(filename, cwinfo);

                return cwinfo.widgetHeader;
            }
        }


        public Dictionary<string, Object> getWidgetInfoHeaderString(String data)
        {
            return getWidgetInfoHeaderEx(data, true);
        }

        private Dictionary<string, Object> getWidgetInfoHeaderEx(String str, bool isString)
        {
            this.newLua();

            try
            {
                if (isString)
                {
                    lua.DoString(str);
                }
                else
                {
                    lua.DoFile(str);
                }
            }
            catch (Exception exp)
            {
                //errror parsing widget, lets hope the info header was already read...
                Console.WriteLine("Non-crititcal widget parser error: " + exp.Message );
            }
           
            lua.DoString("____getInfoResultTable = widget:GetInfo()");
            LuaTable winfo = (LuaTable)lua.GetValue("____getInfoResultTable");

            if (winfo == null)
            {
                throw new Exception("Widget file info header could not be parsed correctly");
            }

            return SimpleLuaParser.luaTable2DictionaryObject(winfo);
        }

        private void newLua()
        {
            lua.Dispose();
            lua = new Lua();
            setupDummySpringLuaEnvironment();
        }

        public LuaTable ReadConfigOrder(String orderFilename)
        {
            this.newLua();

            //dont know how to get the return value since its a global call
            //dirty workaround: wrap the file into a function and call it to get the value
            //please close your eyes now
            try
            {
                String fileContent = File.ReadAllText(orderFilename);

                String wrapped = "";
                wrapped += "function getResult()";
                wrapped += fileContent;
                wrapped += "end";
                lua.DoString(wrapped);
            }
            catch (Exception exp)
            {
                //errror parsing widget, lets hope the relevant part was already read...
                Console.WriteLine("Non-crititcal order-file parser error: " + exp.Message);
            }

            /*
            LuaFunction func = lua.GetFunction("getResult");
            LuaTable ltab = (LuaTable)func.Call().GetValue(0);
            */
            lua.DoString("____getInfoResultTable = getResult()");
            return (LuaTable)lua.GetValue("____getInfoResultTable");
        }

        public Dictionary<String, Double> getWidgetConfigOrder(String orderFilename)
        {
            if (!System.IO.File.Exists(orderFilename ))
            {
                throw new Exception("Order file not found: " + orderFilename);
            }

            DateTime lastWrite = File.GetLastWriteTime(orderFilename);

            if (!orderFileCache.ContainsKey(orderFilename) || (orderFileCache[orderFilename].lastWrite != lastWrite))
            {
                if (!orderFileCache.ContainsKey(orderFilename))
                {
                    orderFileCache.Add(orderFilename, new CachedOrderInfo());
                }
                this.orderFileCache[orderFilename].lastWrite = lastWrite;

                LuaTable ltab = ReadConfigOrder(orderFilename);
                this.orderFileCache[orderFilename].widgets = SimpleLuaParser.luaOrderTable2DictionaryDouble(ltab);
            }
            return orderFileCache[orderFilename].widgets;
        }



        #region Write Functions    

        private void execOrderFileWrite(String targetFile, Dictionary<String, Double> config, String originalOrderFile )
        {
            //check which format it is
            LuaTable tab = ReadConfigOrder(originalOrderFile);
            LuaTable orderTab = (LuaTable)tab.GetValue("order");
            LuaTable dataTab = (LuaTable)tab.GetValue("data");

            if (orderTab != null && dataTab != null)
            {
                //we have standard format
                this.execOrderFileWriteNewStyle(targetFile, config, tab);
            }
            else
            {
                this.execOrderFileWriteOldStyle(targetFile, config);
            }
        }

        private void execOrderFileWriteOldStyle(String targetFile, Dictionary<String, Double> config)
        {
           this.saveOrderData( targetFile, generateOrderTable(config) );
        }

        private void execOrderFileWriteNewStyle(String targetFile, Dictionary<String, Double> config, LuaTable tableData)
        {
            String tableStr = "{";
            tableStr += Environment.NewLine;

            tableStr += "data = ";
            tableStr += (tableData.GetValue("data") as LuaTable).MyToString();
            tableStr += ",";

            tableStr += Environment.NewLine;
            
            tableStr += "order = ";
            tableStr += generateOrderTable(config);

            tableStr += Environment.NewLine;
            tableStr += "}";

            this.saveOrderData(targetFile, tableStr);
        }

        private String generateOrderTable(Dictionary<String, Double> config)
        {
            String tableStr = "";
            int nextId = 1;
            IDictionaryEnumerator ienum = config.GetEnumerator();
            while (ienum.MoveNext())
            {
                String key = (String)ienum.Key;
                Double val = (Double)ienum.Value;

                if (tableStr.Length > 0)
                {
                    tableStr += ", ";
                    tableStr += Environment.NewLine;
                }
                else
                {
                    tableStr += "{";
                    tableStr += Environment.NewLine;
                }

                tableStr += "[";
                if (key.GetType() == typeof(Double))
                {
                    tableStr += key;
                }
                else
                {
                    tableStr += "\"" + key + "\"";
                }
                tableStr += "] = ";

                if (val > 0.0)
                {
                    //append next id
                    tableStr += nextId;
                }
                else
                {
                    //its disabled just append 0
                    tableStr += "0";
                }

                nextId++;
            }


            if (tableStr.Length > 0)
            {
                tableStr += Environment.NewLine;
                tableStr += "}";
            }

            return tableStr;
        }

        private void saveOrderData(String targetFile, String data)
        {
            try
            {
                this.newLua();
                lua.DoFile(luaBaseFiles["savetable.lua"]);

                String tableStr = "myTable = ";
                tableStr += data;

                lua.DoString(tableStr);

                //hack to sanitize filename
                targetFile = targetFile.Replace('\\', '/' );
                
                String saveStr = "table.save( myTable, '" + targetFile + "', '-- Widget Order List  (0 disables a widget)')";
                lua.DoString(saveStr);
            }
            catch (Exception exp)
            {
                Console.WriteLine("Writing of " + targetFile + " failed! Error: " + exp.Message);
            }
        }

        public void writeOrderConfigFile(String modsOrderFilename, Dictionary<String, Double> config)
        {
            String orderFilename = modsOrderFilename;
            String orderFilenameTemp = orderFilename + "_temp";
            String orderFilenameBackup = orderFilename + "_backup";

            execOrderFileWrite(orderFilenameTemp, config, orderFilename);

            //delete a possible old backup
            File.Delete(orderFilenameBackup);
            //make a new backup
            File.Copy(orderFilename, orderFilenameBackup);

            //if we got this far, copy the new config to actual config
            File.Delete(orderFilename);
            try
            {
                //its critical here, the original file was deleted!
                File.Copy(orderFilenameTemp, orderFilename);
            }
            catch (Exception exp)
            {
                //something went wrong, put the backup in place again
                File.Copy(orderFilenameBackup, orderFilename);
                throw exp;
            }
            finally
            {
                //clean up temp file
                File.Delete(orderFilenameTemp);
            }
        }
        #endregion
    }


}
