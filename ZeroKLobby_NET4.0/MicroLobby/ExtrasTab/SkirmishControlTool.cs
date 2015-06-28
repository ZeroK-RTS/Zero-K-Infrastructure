using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZkData;
using ZkData.UnitSyncLib;
using System.IO;
using System.Globalization;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
    static class SkirmishControlTool
    {

        /// <summary>
        /// This function sort the content of a List<string>
        /// using the first 2 character of every word 
        /// and numbers as versions.
        /// 
        /// The words (and numbers) are assumed to be separated by
        /// character such as ".", "-", "_", and " "
        /// </summary>
        public static List<string> SortListByVersionName(List<string> nameList)
        {
            List<object[]> objList = new List<object[]>();
            for (int i = 0; i < nameList.Count; i++)
                objList.Add(new object[1] { nameList[i] });

            objList = SortListByVersionName(objList);

            for (int i = 0; i < nameList.Count; i++)
                nameList[i] = (string)objList[i][0];

            return nameList;
        }


        /// <summary>
        /// This function sort the content of a List<object[]> table
        /// using the string on the first index of that object array
        /// 
        /// Content of the object array is not be altered but will 
        /// be sorted together with the string on first index.
        /// 
        /// It sort based on the first 2 character of every word, and
        /// numbers as version.
        /// 
        /// The words (and numbers) are assumed to be separated by
        /// character such as ".", "-", "_", and " "
        /// </summary>
        public static List<object[]> SortListByVersionName(List<object[]> nameList)
        {
            char[] charIndex = new char[36]
            {
                '0','1','2','3','4',
                '5','6','7','8','9',
                'a','b','c','d','e',
                'f','g','h','i','j',
                'k','l','m','n','o',
                'p','q','r','s','t',
                'u','v','w',
                'x','y','z',
            };
            //initialize score table
            List<object[]> scoredItem = new List<object[]>();
            for (int i = 0; i < nameList.Count; i++)
            {
                string[] textComponent = ((string)nameList[i][0]).ToLower().Split(new char[4] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                List<int> charScore = new List<int>();
                for (int j = 0; j < textComponent.Length; j++)
                {
                    int charCount = textComponent[j].Length;
                    int isInteger = -1;
                    int.TryParse(textComponent[j], out isInteger);
                    if (isInteger != 0 || charCount == 1)
                    {
                        charScore.Add(isInteger);
                    }
                    else //text, but probably versioning char (like 'v' or 'r')
                    {
                        int.TryParse(textComponent[j].Substring(1), out isInteger); //it return '0' when can't parse or when its really '0'
                        if (isInteger != 0 || (textComponent[j][1] == '0' && charCount == 2))  //trailing integer is probably version number
                        {
                            charScore.Add(isInteger);
                        }
                        else
                        {  // score for 1st,2nd char in a word
                            int score = GetMatchingCharIndex(charIndex, textComponent[j][0]);
                            charScore.Add(score);
                            score = GetMatchingCharIndex(charIndex, textComponent[j][1]);
                            charScore.Add(score);
                        }
                    }
                }
                scoredItem.Add(new object[2] { 
                    charScore.ToArray(),
                    nameList[i] });
            }


            scoredItem.Sort(delegate(object[] x, object[] y) //Reference: http://msdn.microsoft.com/en-us/library/b0zbh7b6(v=vs.110).aspx
            {
                int[] tableX = (int[])x[0];
                int[] tableY = (int[])y[0];
                int loopCount = Math.Max(tableX.Length, tableY.Length);
                for (int i = 0; i < loopCount; i++)
                {
                    int scoreX = -2;
                    int scoreY = -2;
                    if (i < tableX.Length)
                         scoreX = tableX[i];

                    if (i < tableY.Length)
                         scoreY = tableY[i];

                    if (scoreX > scoreY || scoreY==-2) return 1; //is "1" when X is bigger than Y or when Y is empty
                    if (scoreX == -2 || scoreX < scoreY) return -1; //is "-1" when X is smaller than Y or when X is empty
                }
                return 0; //same chars or numbers
            });

            for (int i = 0; i < nameList.Count; i++)
                nameList[i] = (object[])scoredItem[i][1];

            return nameList;
        }

        private static int GetMatchingCharIndex(char[] charIndex, char aChar)
        {
            for (int k = 0; k < charIndex.Length; k++)
                if (charIndex[k] == aChar)
                    return k;

            return -1;
        }

        /// <summary>
        /// Create a list of available Spring AIs by reading "AI" directory structure in DataDir & engine folder.
        /// </summary>
        public static List<Ai> GetSpringAIs(string engineFolder)
        {
            //GET AI list from 2 location
            var customSpringAis = GetSpringAis_Internal(Program.SpringPaths.WritableDirectory); //for AI placed in Spring/AI/Skirmish
            var springAis = GetSpringAis_Internal(engineFolder);

            if (customSpringAis.Count == 0 && springAis.Count == 0) //empty (wrong directory)
                return springAis;
            
            //JOIN list of AI from DataDir with the one from Engine folder
            bool duplicate = false;
            List<Ai> toJoinIn = new List<Ai>(springAis.Count);
            for (int i = 0; i < springAis.Count; i++)
            {
                duplicate = false;
                for (int j = 0; j < customSpringAis.Count; j++)
                {
                    if (springAis[i].ShortName == customSpringAis[j].ShortName && springAis[i].Version == customSpringAis[j].Version)
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate)
                    toJoinIn.Add(springAis[i]);
            }
            customSpringAis.AddRange(toJoinIn);

            //SORT joined list by name & version
            List<object[]> objList = new List<object[]>();
            for (int i = 0; i < customSpringAis.Count; i++)
                objList.Add(new object[2] { string.Format("{0} {1}", customSpringAis[i].ShortName, customSpringAis[i].Version), customSpringAis[i] });

            objList = SortListByVersionName(objList);

            for (int i = 0; i < objList.Count; i++)
                customSpringAis[i] = (Ai)objList[i][1];

            return customSpringAis;
        }

        private static List<Ai> GetSpringAis_Internal(string engineFolder)
        {
            //Example Unitsync method: (not compatible with Linux environment & probably abit CPU expensive )
            //var springAis = new List<Ai>();
            //Program.SpringScanner.VerifyUnitSync();
            //if (Program.SpringScanner.unitSync != null)
            //{
            //    foreach (var bot in Program.SpringScanner.unitSync.GetAis()) //IEnumberable can't be serialized, so convert to List. Ref: http://stackoverflow.com/questions/9102234/xmlserializer-in-c-sharp-wont-serialize-ienumerable 
            //        springAis.Add(bot);
            //    Program.SpringScanner.UnInitUnitsync();
            //}

            var springAis = new List<Ai>();
            string aiSkirmishFolder = ZkData.Utils.MakePath(engineFolder, "AI", "Skirmish"); //eg: Spring/engine/98.0/AI/Skirmish

            if (!Directory.Exists(aiSkirmishFolder))
                return springAis;

            var aiName = System.IO.Directory.EnumerateDirectories(aiSkirmishFolder, "*").ToList<string>();
            string aiFolder;
            string aiLibFolder;
            string aiVerFolder;
            List<string> aiVerFolderS;
            string aiInfoFile;
            for (int i = 0; i < aiName.Count; i++)
            {
                aiFolder = aiName[i]; //eg: Spring/engine/98.0/AI/Skirmish/AAI
                aiVerFolderS = System.IO.Directory.EnumerateDirectories(aiFolder, "*").ToList<string>();
                for (int j = 0; j < aiVerFolderS.Count; j++)
                {
                    aiVerFolder = GetFolderOrFileName(aiVerFolderS[j]);
                    aiLibFolder = ZkData.Utils.MakePath(aiFolder, aiVerFolder); //eg: Spring/engine/98.0/AI/Skirmish/AAI/0.9
                    aiInfoFile = ZkData.Utils.MakePath(aiLibFolder, "AIInfo.lua"); //eg: Spring/engine/98.0/AI/Skirmish/AAI/0.9/AIInfo.lua
                    var bot = CrudeLUAReader.GetAIInfo(aiInfoFile);
                    if (bot!=null)
                        springAis.Add(bot);
                }
            }
            return springAis;
        }

        /// <summary>
        /// Convert "path/path/path/name" or "path\\path\\path\\name" into "name"
        /// </summary>
        public static string GetFolderOrFileName(string input)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return input.Substring(input.LastIndexOf("/") + 1); //remove full path, leave only folder name
            else
                return input.Substring(input.LastIndexOf("\\") + 1); //remove full path, leave only folder name
        }
        
        public static List<Mod> GetPartialSddMods()
        {
            var sddMods = new List<Mod>();
            var modFolder = Utils.MakePath(Program.SpringPaths.DataDirectories.First(), "games");
            var sddFolder = System.IO.Directory.EnumerateDirectories(modFolder, "*.sdd"); //.ToList<string>();
            foreach (string path in sddFolder)
            {
                var modinfo = Utils.MakePath(path,"modinfo.lua");
                if (File.Exists(modinfo))
                {
                    var mod = new Mod();
                
                    mod.ArchiveName = GetFolderOrFileName(path);
                    
                    CrudeLUAReader.ParseModInfo(modinfo,ref mod);

                    sddMods.Add(mod);
                }
            }
            return sddMods;
        }
        
        public static Mod GetOneSddMod(Mod mod)
        {
            if (mod.Options!=null || mod.ModAis!=null || mod.MissionScript!=null ||mod.Sides!=null)
                return mod;

            var modFolder = Utils.MakePath(Program.SpringPaths.DataDirectories.First(), "games");
            var path = Utils.MakePath(modFolder, mod.ArchiveName );

            String engineOptions = null;
            String modOptions = null;
            String luaAI = null;
            String missionScriptPath = null;
            String missionSlotPath = null;
            
            var rootFiles = Directory.EnumerateFiles(path);
            foreach(var pathOfFile in rootFiles)
            {
                var fileName = GetFolderOrFileName(pathOfFile);
                if(fileName.StartsWith("EngineOptions.lua",StringComparison.InvariantCultureIgnoreCase))
                    engineOptions = pathOfFile;
                else if(fileName.StartsWith("ModOptions.lua",StringComparison.InvariantCultureIgnoreCase))
                    modOptions = pathOfFile;
                else if(fileName.StartsWith("LuaAI.lua",StringComparison.InvariantCultureIgnoreCase))
                    luaAI = pathOfFile;
                else if(fileName.StartsWith("script.txt",StringComparison.InvariantCultureIgnoreCase))
                    missionScriptPath = pathOfFile;
                else if(fileName.StartsWith("slots.lua",StringComparison.InvariantCultureIgnoreCase))
                    missionSlotPath = pathOfFile;
            }
            
            if (modOptions!=null)
                CrudeLUAReader.ReadOptionsTable(modOptions, ref mod);
            if (engineOptions!=null)
                CrudeLUAReader.ReadOptionsTable(engineOptions, ref mod);
            if (luaAI!=null)
                CrudeLUAReader.ReadLuaAI(luaAI,ref mod);
            if (missionSlotPath!=null)
                CrudeLUAReader.ReadMissionSlot(missionSlotPath,ref mod);

            if (missionScriptPath!=null)
            {
                try{
                using (FileStream fileStream = File.OpenRead(missionScriptPath))
                using (var stream = new StreamReader(fileStream))
                {
                    mod.MissionScript = stream.ReadToEnd();

                    var open = mod.MissionScript.IndexOf("mapname", 7, mod.MissionScript.Length - 8, StringComparison.InvariantCultureIgnoreCase) + 8;
                    var close = mod.MissionScript.IndexOf(';', open);
                    var mapname = mod.MissionScript.Substring(open, close - open);
                    mod.MissionMap = mapname.Trim(new char[3]{' ','=','\t'});
                }
                }catch(Exception e)
                {}
            }

            var sideData = Utils.MakePath(path,"gamedata","sidedata.lua");
            var picPath = Utils.MakePath(path,"sidepics");
            CrudeLUAReader.ReadSideInfo(sideData,picPath,ref mod);

            return mod;
        }
    }
    static class CrudeLUAReader
    {
        /// <summary>
        /// Parse sides name from gamedata/sidedata.lua and side icon (.bmp format with same name as side name) from sidepics/
        /// </summary>
        public static void ReadSideInfo(string sidedataluaPath, string picPath, ref Mod modInfo)
        {
            if (!File.Exists(sidedataluaPath))
            {
                modInfo.Sides = new string[0];
                //modInfo.SideIcons = (new List<Byte[]>()).ToArray();
                //modInfo.StartUnits = new PlasmaShared.SerializableDictionary<string, string>(new Dictionary<string,string>());
                return;
            }
            
            using (FileStream fileStream = File.OpenRead(sidedataluaPath))
            using (var stream = new StreamReader(fileStream))
            {
                var allText = stream.ReadToEnd();
                int offset =0;
                var config = new TableReaderConfig();
                var table = TableReader.ParseTable(config,0,allText,sidedataluaPath,out offset);
                
                List<String> sides = new List<String>();
                List<byte[]> sideIcons = new List<byte[]>();
                var startUnits = new Dictionary<string,string>(); //PlasmaShared.SerializableDictionary<string,string>();
                
                foreach (var kvp in table)
                {
                    String name = "";
                    String startunit = "";
                    foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                    {
                        var value = (kvp2.Value as String);
                        
                        switch(kvp2.Key)
                        {
                            case "name":
                                name = value;
                                break;
                            case "startunit":
                                startunit = value;
                                break;
                        }
                    }
                    if (name!="")
                    {
                        var picBytes = new Byte[0];
                        try{
                        var picList = Directory.EnumerateFiles(picPath);
                        using(FileStream fileStream2 = File.OpenRead(picList.First(x => SkirmishControlTool.GetFolderOrFileName(x).StartsWith(name,StringComparison.InvariantCultureIgnoreCase))))
                        {
                            picBytes = new Byte[fileStream2.Length];
                            fileStream2.Read(picBytes,0,picBytes.Length);
                        }
                        }catch(Exception e) {System.Diagnostics.Trace.TraceError(e.ToString());}
                        sides.Add(name);
                        sideIcons.Add(picBytes);
                        startUnits.Add(name,startunit);
                    }
                }
                modInfo.Sides = sides.ToArray();
                modInfo.SideIcons = sideIcons.ToArray();
                modInfo.StartUnits = new SerializableDictionary<string, string>(startUnits);
            }
        }
        
        /// <summary>
        /// Read mission.lua file.
        /// </summary>
        public static void ReadMissionSlot(string filePath, ref Mod modInfo)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            using (FileStream fileStream = File.OpenRead(filePath))
            using (var stream = new StreamReader(fileStream))
            {
                var allText = stream.ReadToEnd();
                int offset =0;
                var config = new TableReaderConfig();
                var table = TableReader.ParseTable(config,0,allText,filePath,out offset);
                
                var allSlots = new List<MissionSlot>();
                
                foreach (var kvp in table)
                {
                    var slot = new MissionSlot();
                    int numbers = 0;
                    foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                    {
                        var value = (kvp2.Value as String);
                        switch(kvp2.Key)
                        {
                            case "AllyID":
                                int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out numbers);
                                slot.AllyID = numbers;
                                break;
                            case "AllyName":
                                slot.AllyName = value;
                                break;
                            case "IsHuman":
                                slot.IsHuman = (value=="true");
                                break;
                            case "IsRequired":
                                slot.IsRequired = (value =="true");
                                break;
                            case "TeamID":
                                int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out numbers);
                                slot.TeamID = numbers;
                                break;
                            case "TeamName":
                                slot.TeamName = value;
                                break;
                            case "Color":
                                int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out numbers);
                                slot.Color = numbers;
                                //var springColor = (MyCol)numbers;
                                //slot.ColorR = springColor.R;
                                //slot.ColorG = springColor.G;
                                //slot.ColorB = springColor.B;
                                break;
                            //case "ColorR":
                            //    break;
                            //case "ColorG":
                            //    break;
                            //case "ColorB":
                            //    break;
                            case "AiShortName":
                                slot.AiShortName = value;
                                break;
                            case "AiVersion":
                                slot.AiVersion = value;
                                break;
                        }
                    }
                    if (slot.AiShortName!=null)
                    {
                        allSlots.Add(slot);
                    }
                }
                modInfo.MissionSlots = allSlots;
            }
        }

        /// <summary>
        /// Get AI description from AIInfo.lua
        /// </summary>
        public static Ai GetAIInfo(string filePath)
        {
            if (!File.Exists(filePath)) 
            {
                return null;
            }
            
            var aiInfoList = new List<AiInfoPair>();
            
            using (FileStream fileStream = File.OpenRead(filePath))
            using (var stream = new StreamReader(fileStream))
            {
                var allText = stream.ReadToEnd();
                int offset =0;
                var config = new TableReaderConfig();
                var table = TableReader.ParseTable(config,0,allText,filePath,out offset);
                
                foreach (var kvp in table)
                {
                    String key="";
                    String valueIn="";
                    String desc="";
                    var aiInfoOne =  new AiInfoPair();
                    foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                    {
                        var value = (kvp2.Value as String);
                        switch(kvp2.Key)
                        {
                            case "key":
                                key = value;
                                break;
                            case "value":
                                valueIn= value;
                                break;
                            case "desc":
                                desc = value;
                                break;
                        }
                    }
                    if (key!="" && valueIn!="")
                    {
                        aiInfoOne.Key = key;
                        aiInfoOne.Value = valueIn;
                        aiInfoOne.Description = desc;
                        aiInfoList.Add(aiInfoOne);
                    }
                }
            }
            return new Ai(){
                Info= aiInfoList.ToArray()
            };
        }
        
        /// <summary>
        /// Read mod luaAI from LuaAI.lua
        /// </summary>
        public static void ReadLuaAI(string filePath, ref Mod modInfo)
        {
            if (!File.Exists(filePath)) 
            {
                return;
            }
               
            using (FileStream fileStream = File.OpenRead(filePath))
            using (var stream = new StreamReader(fileStream))
            {
                var allText = stream.ReadToEnd();
                int offset =0;
                var config = new TableReaderConfig();
                var table = TableReader.ParseTable(config,0,allText,filePath,out offset);
                
                List<Ai> modAis = new List<Ai>();
                
                foreach (var kvp in table)
                {
                    String name = "";
                    String desc = "";
                    foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                    {
                        var value = (kvp2.Value as String);
                        
                        switch(kvp2.Key)
                        {
                            case "name":
                                name = value;
                                break;
                            case "desc":
                                desc = value;
                                break;
                        }
                    }
                    if (name!="" && desc!="")
                    {
                        var bot = new Ai()
                        {
                            Info = new AiInfoPair[2] 
                        { 
                            new AiInfoPair { Key = "shortName", Value = name } , //usually equivalent to AI folder name (aiName[i])
                            new AiInfoPair { Key = "description", Value = desc } ,
                        }
                        };
                        modAis.Add(bot);
                    }
                }
                modInfo.ModAis = modAis.ToArray();
            }
        }
    
        /// <summary>
        /// Read mod information from modInfo.lua
        /// </summary>
        public static void ParseModInfo(string filePath, ref Mod modInfo)
        {
            if (!File.Exists(filePath))
            {
                return;
            }
            
            using (FileStream fileStream = File.OpenRead(filePath))
            using (var stream = new StreamReader(fileStream))
            {
                var allText = stream.ReadToEnd();
                int offset =0;
                var config = new TableReaderConfig();
                var table = TableReader.ParseTable(config,0,allText,filePath,out offset);
                
                foreach (var kvp in table)
                {
                    var value = (kvp.Value as String);
                    switch(kvp.Key)
                    {
                        case "name":
                            modInfo.Name = value;
                            break;
                        case "description":
                            modInfo.Description = value;
                            break;
                        case "shortname":
                            modInfo.ShortName = value;
                            break;
                        case "version":
                            modInfo.PrimaryModVersion = value;
                            break;
                        case "mutator":
                            modInfo.Mutator = value;
                            break;
                        case "game":
                            modInfo.Game = value;
                            break;
                        case "shortGame":
                            modInfo.ShortGame = value;
                            break;
                        case "modtype":
                            //TODO modtype??
                            break;
                        case "depend":
                            var listDepend = new List<string>();
                            foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                            {
                                var value2 = (kvp2.Value as String);
                                listDepend.Add(value2);
                            }
                            
                            modInfo.Dependencies = listDepend.ToArray();
                            break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Read options from EngineOptions.lua or ModOptions.lua
        /// </summary>
        public static void ReadOptionsTable(String filePath, ref Mod modInfo)
        {
            if (!File.Exists(filePath)) 
            {
                return;
            }
            
            var allOptions = new List<Option>();
            using (FileStream fileStream = File.OpenRead(filePath))
            using (var stream = new StreamReader(fileStream))
            {
                var allText = stream.ReadToEnd();
                int offset =0;
                var config = new TableReaderConfig();
                var table = TableReader.ParseTable(config,0,allText,filePath,out offset);
                
                foreach (var kvp in table)
                {
                    var anOption = new Option();
                    bool isBoolOption = false;
                    foreach(var kvp2 in (kvp.Value as Dictionary<String,Object>))
                    {
                        var value = (kvp2.Value as String);
                        float numbers;
                        //uncomment to take a peek on what it read!: 
                        //System.Diagnostics.Trace.TraceWarning("key: " + kvp2.Key + " value:" + value);
                        switch(kvp2.Key)
                        {
                            case "key":
                                anOption.Key = value;
                                break;
                            case "name":
                                anOption.Name = value;
                                break;
                            case "desc":
                                anOption.Description = value;
                                break;
                            case "type":
                                switch(value)
                                {
                                    case "bool":
                                        anOption.Type = OptionType.Bool;
                                        isBoolOption = true;
                                        break;
                                    case "list":
                                        anOption.Type = OptionType.List;
                                        break;
                                    case "number":
                                        anOption.Type = OptionType.Number;
                                        break;
                                    case "string":
                                        anOption.Type = OptionType.String;
                                        break;
                                    case "section":
                                        anOption.Type = OptionType.Section;
                                        continue;
                                    default:
                                        anOption.Type = OptionType.Undefined;
                                        break;
                                }
                                break;
                            case "def":
                                anOption.Default = value;
                                if (isBoolOption)
                                {
                                    if (value=="false")
                                        anOption.Default = "0";
                                    else
                                        anOption.Default = "1";
                                }
                                break;
                            case "min":
                                float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out numbers);
                                anOption.Min = numbers;
                                break;
                            case "max":
                                float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out numbers);
                                anOption.Max = numbers;
                                break;
                            case "step":
                                float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out numbers);
                                anOption.Step = numbers;
                                break;
                            case "maxlen":
                                float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out numbers);
                                anOption.StrMaxLen = numbers;
                                break;
                            case "items":
                                var listOptions = new List<ListOption>();
                                foreach(var kvp3 in (kvp2.Value as Dictionary<String,Object>))
                                {
                                    var listOption = new ListOption();
                                    foreach(var kvp4 in (kvp3.Value as Dictionary<String,Object>))
                                    {
                                        var value2 = (kvp4.Value as String);
                                        switch(kvp4.Key)
                                        {
                                            case "key":
                                                listOption.Key = value2;
                                                break;
                                            case "name":
                                                listOption.Name = value2;
                                                break;
                                            case "desc":
                                                listOption.Description = value2;
                                                break;
                                        }
                                    }

                                    if (listOption.Key!=null)
                                        listOptions.Add(listOption);
                                }
                                anOption.ListOptions = listOptions;
                                break;
                            case "scope":
                                anOption.Scope = value;
                                break;
                            case "section":
                                anOption.Section = value;
                                break;
                        }     
                    }

                    if (anOption.Key!=null)
                        allOptions.Add(anOption);   
                }
            }
            if (modInfo.Options!=null)
                modInfo.Options = modInfo.Options.ToList().Concat(allOptions).ToArray();
            else 
                modInfo.Options = allOptions.ToArray();
        }
    }
}
