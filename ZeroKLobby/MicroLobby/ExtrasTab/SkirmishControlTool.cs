using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZkData.UnitSyncLib;
using System.IO;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
    static class SkirmishControlTool
    {

        /// <summary>
        /// This function sort the content of a List<string>
        /// using the first character (a-z) of the first 3 word 
        /// and the 4 first numbers (0-50000) as versions.
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
                    aiVerFolder = GetFolderName(aiVerFolderS[j]);
                    aiLibFolder = ZkData.Utils.MakePath(aiFolder, aiVerFolder); //eg: Spring/engine/98.0/AI/Skirmish/AAI/0.9
                    aiInfoFile = ZkData.Utils.MakePath(aiLibFolder, "AIInfo.lua"); //eg: Spring/engine/98.0/AI/Skirmish/AAI/0.9/AIInfo.lua
                    var info = GetAIInfo(aiInfoFile);
                    if (info.ContainsKey("shortName") && info.ContainsKey("version"))
                    {
                        var bot = new Ai()
                        {
                            Info = new AiInfoPair[3] 
                        { 
                            new AiInfoPair { Key = "shortName", Value = info.ContainsKey("shortName")?info["shortName"]:"" } , //usually equivalent to AI folder name (aiName[i])
                            new AiInfoPair { Key = "version", Value = info.ContainsKey("version")?info["version"]:"" } , //usually equivalent to sub- AI folder name (aiVerFolder)
                            new AiInfoPair { Key = "description", Value = info.ContainsKey("description")?info["description"]:"" } ,
                        }
                        };
                        springAis.Add(bot);
                    }
                }
            }
            return springAis;
        }

        /// <summary>
        /// Convert "path/path/path/name" or "path\\path\\path\\name" into "name"
        /// </summary>
        public static string GetFolderName(string input)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return input.Substring(input.LastIndexOf("/") + 1); //remove full path, leave only folder name
            else
                return input.Substring(input.LastIndexOf("\\") + 1); //remove full path, leave only folder name
        }

        /// <summary>
        /// Get AI description from AIInfo.lua
        /// </summary>
        private static Dictionary<string, string> GetAIInfo(string filePath)
        {
            Dictionary<string, string> info = new Dictionary<string, string>();
            if (!File.Exists(filePath)) return info;

            //Open the stream and read
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                string currentLine;
                string longValue;
                string shortValue;
                string keyName;
                using (var stream = new StreamReader(fileStream))
                    while (!stream.EndOfStream)
                    {
                        currentLine = stream.ReadLine();
                        if (currentLine.Contains("key    = "))
                        {
                            keyName = GetValue(currentLine);
                            currentLine = stream.ReadLine();
                            longValue = currentLine;
                            currentLine = stream.ReadLine();
                            while (!currentLine.Contains("desc   = "))
                            {
                                longValue = longValue + currentLine; //can contain multiple line, such as RAI's description, so we append line until some sign that tell us to stop
                                currentLine = stream.ReadLine();
                            }
                            shortValue = GetValue(longValue).Trim(new char[5] { ' ', '\t', '[', ']', '\'' });
                            info.Add(keyName, shortValue);
                        }
                    }
            }
            return info;
        }

        private static string GetValue(string line)
        {
            //eg1: "		value  = 'KAIK',"
            //eg2: "		value  = '0.13', -- AI version - !This comment is used for parsing!"
            //eg3: "		value  = [[Plays a nice, slow game, concentrating on base building.\nUses ground, air, hovercrafts and water well.\nDoes some smart moves and handles T2+ well.\n
            //                       Realtive CPU usage: 1.0\nScales well with growing unit numbers.\nKnown to Support: BA, SA, XTA, NOTA]],"
            int open = line.IndexOf("= ", 0, line.Length) + 2 + 1;
            int close = line.IndexOf(',', open) - 1;
            return line.Substring(open, close - open); 
        }
    }
}
