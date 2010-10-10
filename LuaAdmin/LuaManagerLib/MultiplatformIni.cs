/* 
 * Copyright (C) 2009 Iskren Slavov (site@dotfusion.net).
 * 
 * This code (Class): MultipatformIni.cs must be redistributed 
 * and edited under the terms of GNU LGPL v3.0. For more 
 * information read LGPL-3.0.TXT, supplied with the Class.
 * 
 * The class is tested with .NET framework 2.0 and Mono 2.2.
 * Hope it is helpful to you. Have fun!
 * 
 * http://www.dotfusion.net/
 * 
 */

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MPIniReadWrite
{
    class MultiplatformIni
    {
       // private String iniFilePath;
        private Stream dataStream;

       
        private static Char decSep = NumberFormatInfo.CurrentInfo.NumberDecimalSeparator.ToCharArray()[0];
        private static Regex keyValRegex = new Regex(
          @"((\s)*(?<Key>([^\=^\n]+))[\s^\n]*\=(\s)*(?<Value>([^\n]+(\n){0,1})))",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled | 
            RegexOptions.CultureInvariant
        );
        private static Regex secRegex = new Regex(
          @"(\[)*(?<Section>([^\]^\n]+))(\])" ,
          RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

        private ArrayList newValues = new ArrayList();
        struct sectKeyVal
        {
            public String section, key, value;
            public sectKeyVal(String section, String key, String value)
            {
                this.section = section;
                this.key = key;
                this.value = value;
            }
        }

        public MultiplatformIni(Stream data)
        {
            this.dataStream = data;
        }
        ~MultiplatformIni()
        {
       //     Flush();
        }

        private bool isThisTheSection(String line, ref String sectionName) 
        {
            Match match = secRegex.Match(line);
            if (match.Success)
            {
                String currSection = match.Groups["Section"].Value as String;
                return (currSection != null && currSection.CompareTo(sectionName) == 0);
            }
            return false;
        }
        private bool isThisTheKey(String line, ref String key, ref String value)
        {
            Match match = keyValRegex.Match(line);
            if (match.Success)
            {
                String currKey = match.Groups["Key"].Value as String;
                if (currKey != null && currKey.CompareTo(key) == 0)
                {
                    value = match.Groups["Value"].Value as String;
                    return true;
                }
            } else if (secRegex.Match(line).Success)
                value = "Section"; // Oops, we're at the next section
            return false;
        }

        public String ReadString(String section, String key ) //, String defaultValue)
        {
          //  FileStream fileStream = new FileStream(iniFilePath, FileMode.OpenOrCreate, FileAccess.Read);
            TextReader reader = new StreamReader(dataStream);
            bool sectionFound = false;

            String line;
            while ((line = reader.ReadLine()) != null)
            {
                /* First we find our section,
                   then - the key */
                if (!sectionFound)
                    sectionFound = isThisTheSection(line, ref section);
                else
                {
                    String value = "";
                    if (isThisTheKey(line, ref key, ref value))
                    {
                       // fileStream.Close();
                        return value;
                    }
                }
            }

            //fileStream.Close();
            return null;
        }
        public int ReadInt(String section, String key)
        {
            return Int32.Parse(ReadString(section, key));
        }
        public Double ReadDouble(String section, String key)
        {
            return Double.Parse(
                ReadString(section, key).Replace('.', decSep).Replace(',', decSep)
            );
        }
        public void WriteString(String section, String key, String value)
        {
            int sameKeyNum = -1;
            for (int i = 0; i < newValues.Count; i++)
            {
                sectKeyVal s = (sectKeyVal)newValues[i];
                if ((s.section == section) && (s.key == key))
                    sameKeyNum = i;
            }
            if (sameKeyNum != -1)
                newValues.RemoveAt(sameKeyNum);

            newValues.Add(new sectKeyVal(section, key, value));
        }
        //public void Flush()
        //{
        //    /* This is where all the magic is done
        //       We read our INI file, parse the
        //       new values and add them to the file
        //       before we write it */

        //    // First we read our file
        //    FileStream fileStream = new FileStream(iniFilePath, FileMode.OpenOrCreate, FileAccess.Read);
        //    TextReader reader = new StreamReader(fileStream);

        //    String[] lines = reader.ReadToEnd().Replace('\r', '\0').Split('\n');
        //    fileStream.Close();

        //    // Create our work list and fill it
        //    List<String> linesList = new List<String>();
        //    foreach (String s in lines)
        //        linesList.Add(s);

        //    foreach (sectKeyVal s in newValues)
        //    {
        //        String section = s.section;
        //        String key = s.key;
        //        String value = s.value;

        //        bool sectionFound = false, keyFound = false;
        //        String keyval = "";

        //        List<String> tempList = new List<String>();
        //        for (int i = 0; i < linesList.Count; i++)
        //        {
        //            String line = linesList[i];

        //            // Did we find our section yet?
        //            if (!sectionFound)
        //                sectionFound = isThisTheSection(linesList[i], ref section);
        //            else
        //            {
        //                // Did we find our key?
        //                if (isThisTheKey(linesList[i], ref key, ref keyval))
        //                {
        //                    if (value != keyval)
        //                        line = key + "=" + value;
        //                    keyFound = true;
        //                }
        //                else if (keyval == "Section" && !keyFound)
        //                {
        //                    /* We found the beginning of 
        //                       the next section. */
        //                    keyval = "";
        //                    keyFound = true;

        //                    if (linesList[i - 1].Trim() == "")
        //                        tempList[tempList.Count - 1] = key + "=" + value;
        //                    else
        //                        tempList.Add(key + "=" + value);
        //                    tempList.Add("");
        //                }
        //                else if (i == linesList.Count - 1 && !keyFound)
        //                {   /* Looks like we're at the end of file
        //                       and we found our section */
        //                    tempList.Add(key + "=" + value);
        //                }
        //            }
        //            tempList.Add(line.TrimEnd('\0'));
        //        }
        //        linesList = tempList;

        //        /* The section or the key weren't found
        //           so we add them */
        //        if (!sectionFound && !keyFound)
        //        {
        //            if (linesList.Count == 1 && linesList[linesList.Count - 1].Trim() == "")
        //                linesList.RemoveAt(0);
        //            else
        //                linesList.Add("");

        //            linesList.Add("[" + section + "]");
        //            linesList.Add(key + "=" + value);
        //        }
        //    }

        //    // Finally write our file
        //    fileStream = new FileStream(iniFilePath, FileMode.Create, FileAccess.Write);
        //    TextWriter writer = new StreamWriter(fileStream);

        //    for (int i = 0; i < linesList.Count; i++)
        //        if (!((i == linesList.Count - 1) && (linesList[i].Trim() == "")))
        //            writer.WriteLine(linesList[i]);

        //    writer.Flush();
        //    fileStream.Close();
        //}
        public void WriteInt(String section, String key, int value)
        {
            WriteString(section, key, value.ToString());
        }
        public void WriteDouble(String section, String key, Double value)
        {
            WriteString(section, key, value.ToString());
        }
    }
}