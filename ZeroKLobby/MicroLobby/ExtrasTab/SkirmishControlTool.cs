using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlasmaShared.UnitSyncLib;
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
        /// using the first entry of that object array (index 0) as
        /// a string and uses the character (a-z) of the first 3 word
        /// (of that string) and the 4 first numbers (0-50000) in that
        /// string as versions.
        /// 
        /// Content of the object array will not be altered but
        /// will be sorted together with the string at first index.
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
            List<object[]> scoredItem = new List<object[]>(nameList.Count);
            for (int i = 0; i < nameList.Count; i++)
                scoredItem.Add(new object[2] { 0, nameList[i] });


            //score each entry
            for (int i = 0; i < scoredItem.Count; i++)
            {
                string[] textComponent = ((string)nameList[i][0]).ToLower().Split(new char[4] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                double scoreMultiplier = 0.01;
                double score = 0;
                // score for 1st,2nd,3rd & 4th integer mapped this way: 0.11223344
                int maxOperation = Math.Min(textComponent.Length, 4);
                int j = 0;
                int count = 0;
                while (j < textComponent.Length && count < maxOperation)
                {
                    int isInteger = -1;
                    int.TryParse(textComponent[j], out isInteger);
                    if (isInteger != 0 || textComponent[j] == "0") //score integer
                    {
                        score = score + (1 - 1 * Math.Exp(-isInteger / 130.0) + 0.00002 * isInteger) * 99 * scoreMultiplier;
                        scoreMultiplier = scoreMultiplier / 100; //go to next lower significant integer
                        count = count + 1;
                    }
                    else if (textComponent[j].Length >= 2) //text, but probably versioning char (like 'v' or 'r')
                    {
                        int.TryParse(textComponent[j].Substring(1), out isInteger);
                        if (isInteger != 0 || textComponent[j].Substring(1) == "0")  //trailing integer is probably version number
                        {
                            score = score + (1 - 1 * Math.Exp(-isInteger / 130.0) + 0.00002 * isInteger) * 99 * scoreMultiplier;
                            scoreMultiplier = scoreMultiplier / 100; //go to next lower significant integer
                            count = count + 1;

                            // For reference, equation:
                            // (1 - 1 * Math.Exp(-isInteger / 140.0) + 0.00002 * isInteger) 
                            // behave as following:
                            //   ^
                            //  2|                                     x 
                            //   |                                x  
                            //   |                          x  
                            //   |                    x   
                            //   |              x  
                            //  1|        x  
                            //   |    x
                            //   |   x                                    
                            //   |  x                                      
                            //   | x                                       
                            //   |x                                       
                            // 0 ----------------------------------------> isInteger
                            //   0   600                               50000
                            //
                            //  Targeted properties:
                            //  1) have exaggerated sensitivity for isInteger <600 (to avoid small value like 1 or 2 approaching 0)
                            //  2) low sensitivity for higher isInteger (to avoid big value like 50000 from filling up all the score's digit)
                            //
                            // "Score Digit":
                            // If we have "Zero-K v1.10.0.500"
                            // then we will use the following digit to score the numbers:
                            // v1 --> 0.xx
                            // .10 --> 0.00xx
                            // .0 ---> 0.0000xx
                            // .500 ---> 0.000000xx
                            // If we literally translate versioning number digit by digit, then:
                            // v1.10.0.500 --> 0.01100500
                            // v1.10.1.0 --> 0.01100100 (notice that previous version yield a bigger score-digit than current version)
                            //
                            // So we have to scale down the "500" to fit its score digit, 
                            // but can't scale down "1" using the same amount because it will reach 0.
                            // So we use this equation: "(1 - 1 * Math.Exp(-isInteger / 140.0) + 0.00002 * isInteger)"
                        }
                    }
                    j = j + 1;
                }

                scoreMultiplier = 01000000;
                // score for 1st,2nd & 3rd word mapped this way: 11112233
                maxOperation = Math.Min(textComponent.Length, 3);
                j = 0;
                while (j < maxOperation)
                {
                    int isInteger = -1;
                    int.TryParse(textComponent[j], out isInteger);
                    if (isInteger == 0 && textComponent[j] != "0") //score text
                    {
                        int letterToCheck = Math.Min(textComponent[j].Length, 2);
                        char[] letters = textComponent[j].Substring(0, letterToCheck).ToCharArray();

                        if (j >= 1)
                        { //for next 2 word, score only first character
                            for (int k = 0; k < charIndex.Length; k++)
                                if (charIndex[k] == letters[0])
                                {
                                    score = score + k * scoreMultiplier;
                                    break;
                                }
                            scoreMultiplier = scoreMultiplier / 100; //go to next lower significant digit
                        }
                        else
                        { //for first word, score 2 character at once
                            double tempScoreMultiplier = scoreMultiplier;
                            for (int L = 0; L < letters.Length; L++)
                            {
                                for (int k = 0; k < charIndex.Length; k++)
                                    if (charIndex[k] == letters[L])
                                    {
                                        score = score + k * tempScoreMultiplier;
                                        tempScoreMultiplier = tempScoreMultiplier / 100;
                                        break;
                                    }
                            }
                            scoreMultiplier = scoreMultiplier / 10000; //go to next lower significant digit
                        }
                    }
                    j = j + 1;
                }
                scoredItem[i][0] = score;
            }

            scoredItem.Sort(delegate(object[] x, object[] y) //Reference: http://msdn.microsoft.com/en-us/library/b0zbh7b6(v=vs.110).aspx
            {
                if ((double)x[0] < (double)y[0]) return -1;
                else if ((double)x[0] == (double)y[0]) return 0;
                else return 1;
            });
            for (int i = 0; i < nameList.Count; i++)
                nameList[i] = (object[])scoredItem[i][1];// +" " + ((double)scoredItem[i][0]).ToString();

            return nameList;
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
            string aiSkirmishFolder = PlasmaShared.Utils.MakePath(engineFolder, "AI", "Skirmish"); //eg: Spring/engine/98.0/AI/Skirmish

            if (!Directory.Exists(aiSkirmishFolder))
                return springAis;

            var aiName = System.IO.Directory.EnumerateDirectories(aiSkirmishFolder, "*").ToList<string>();
            for (int i = 0; i < aiName.Count; i++)
                aiName[i] = GetFolderName(aiName[i]);

            string aiFolder;
            string aiLibFolder;
            string aiVerFolder;
            List<string> aiVerFolderS;
            string aiInfoFile;
            for (int i = 0; i < aiName.Count; i++)
            {
                aiFolder = PlasmaShared.Utils.MakePath(aiSkirmishFolder, aiName[i]); //eg: Spring/engine/98.0/AI/Skirmish/AAI
                aiVerFolderS = System.IO.Directory.EnumerateDirectories(aiFolder, "*").ToList<string>();
                for (int j = 0; j < aiVerFolderS.Count; j++)
                {
                    aiVerFolder = GetFolderName(aiVerFolderS[j]);
                    aiLibFolder = PlasmaShared.Utils.MakePath(aiFolder, aiVerFolder); //eg: Spring/engine/98.0/AI/Skirmish/AAI/0.9
                    aiInfoFile = PlasmaShared.Utils.MakePath(aiLibFolder, "AIInfo.lua"); //eg: Spring/engine/98.0/AI/Skirmish/AAI/0.9/AIInfo.lua
                    var info = GetAIInfo(aiInfoFile);
                    if (info[0] != null && info[1] != null)
                    {
                        var bot = new Ai()
                        {
                            Info = new AiInfoPair[3] 
                        { 
                            new AiInfoPair { Key = "shortName", Value = info[0] } , //usually equivalent to AI folder name (aiName[i])
                            new AiInfoPair { Key = "version", Value = info[1] } , //usually equivalent to sub- AI folder name (aiVerFolder)
                            new AiInfoPair { Key = "description", Value = info[2] } ,
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
        private static string[] GetAIInfo(string filePath)
        {
            string[] info = new string[3];
            if (!File.Exists(filePath)) return info;

            bool descriptionFound = false;
            bool versionFound = false;
            bool shortNameFound = false;
            //Open the stream and read
            using (FileStream fileStream = File.OpenRead(filePath))
            {
                string currentLine;
                string description;
                //int open;
                int close;
                using (var stream = new StreamReader(fileStream))
                    while (!stream.EndOfStream)
                    {
                        currentLine = stream.ReadLine();
                        if (!shortNameFound && currentLine.Contains("key    = 'shortName',"))
                        {
                            shortNameFound = true;
                            currentLine = stream.ReadLine();
                            //open = currentLine.IndexOf("value  = ", 0, currentLine.Length, StringComparison.InvariantCultureIgnoreCase) + 9;
                            //close = currentLine.IndexOf(',', open);
                            //currentLine = currentLine.Substring(open, close - open);
                            //info[0] = currentLine.Trim(new char[1] { ' ' });
                            info[0] = currentLine.Substring(12, currentLine.Length - 12 - 2); // eg: "		value  = 'KAIK',"
                            currentLine = stream.ReadLine();
                        }
                        if (!versionFound && currentLine.Contains("key    = 'version',"))
                        {
                            versionFound = true;
                            currentLine = stream.ReadLine();
                            close = currentLine.IndexOf(',', 12); //eg: "		value  = '0.13', -- AI version - !This comment is used for parsing!"
                            info[1] = currentLine.Substring(12, close - 12 - 1);
                            currentLine = stream.ReadLine();
                        }
                        if (!descriptionFound && currentLine.Contains("key    = 'description',"))
                        {
                            descriptionFound = true;
                            currentLine = stream.ReadLine();
                            description = currentLine;
                            currentLine = stream.ReadLine();
                            while (!currentLine.Contains("desc   = "))
                            {
                                description = description + currentLine; //multiple line, such as RAI's description.
                                currentLine = stream.ReadLine();
                            }
                            //open = description.IndexOf("value  = ", 0, description.Length, StringComparison.InvariantCultureIgnoreCase) + 9;
                            //close = description.IndexOf(',', open);
                            //description = description.Substring(open, close - open);
                            description = description.Substring(12, description.Length - 12 - 2);
                            info[2]= description.Trim(new char[5] { ' ', '\t', '[', ']', '\'' });
                        }
                        if (shortNameFound && versionFound && descriptionFound) return info;
                    }
            }
            return info;
        }
    }
}
