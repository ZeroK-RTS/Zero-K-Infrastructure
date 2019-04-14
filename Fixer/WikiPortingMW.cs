using DotNetWikiBot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ZkData;

namespace Fixer
{
    /// <summary>Ports Zero-K wiki pages to the new MediaWiki.</summary>
    public static class WikiPortingMW
    {
        /*
         * How to update unit pages using this bot, speedy version:
         *  Update zk.sdd
         *  Generate files using SpringRTS-Tools/unitguide/export_units_template.sh
         *  - Edit the shell file to make the specified paths point to your zk.sdd (and your desired output directory)
         *  Modify fileDir variable below to output directory
         *  Run DoStuff() from Fixer's Main() method; by default, it will run UpdatePages() to update wiki pages from output text files
         *  
         *  I recommend looking over UpdatePages() before doing anything with this — Hist.
         */

        public const string WIKI_URL = @"https://zero-k.info";
        public static string fileDir = Environment.OSVersion.Platform == PlatformID.Unix ? @"/media/histidine/My Book/zkwiki/" : @"G:\zkwiki\";
        static Site newWiki;

        public static string BBCodeToMediaWiki(string text)
        {
            Regex regex;

            //bullet points
            regex = new Regex("\\n \\* ");
            text = regex.Replace(text, "\n* ");

            // replace bold
            regex = new Regex("\\[b\\](.*?)\\[/b\\]");
            text = regex.Replace(text, "'''$1'''");
            regex = new Regex("\\*(.*?)\\*");
            text = regex.Replace(text, "'''$1'''");

            //replace italics
            regex = new Regex("\\[i\\](.*?)\\[/i\\]");
            text = regex.Replace(text, "''$1''");
            //regex = new Regex("_(.*?)_"); // trying to remove markdown italics tends to break on filenames
            //text = regex.Replace(text, "''$1''");

            // URLs
            regex = new Regex("\\[url=(.*?)\\](.*?)\\[/url\\]");
            text = regex.Replace(text, "[$1 $2]");

            // remove [img] tags
            regex = new Regex("\\[\\/?img\\]");
            text = regex.Replace(text, "");

            // remove wiki:toc tag
            regex = new Regex("<wiki:toc .*?/>");
            text = regex.Replace(text, "");

            // code blocks
            regex = new Regex("\\{\\{\\{(.*?)\\}\\}\\}", RegexOptions.Singleline);
            text = regex.Replace(text, "<code>$1</code>");

            // Manual link
            regex = new Regex("\\[Manual Back to Manual\\]");
            text = regex.Replace(text, "[[Manual|Back to Manual]]");

            Console.WriteLine(text);
            return text;
        }

        /// <summary>Converts an existing forum wiki page in ZK database to MediaWiki format,
        /// and saves it on remote mediawiki.</summary>
        public static void ConvertPage(string pageName, string newName, bool overwrite = false)
        {
            var db = new ZkDataContext();
            ForumThread thread = db.ForumThreads.FirstOrDefault(x=> x.WikiKey == pageName);
            if (thread == null)
            {
                Console.WriteLine("No ZK wiki page with name {0} found", pageName);
                return;
            }

            string text = thread.ForumPosts.First().Text;
            text = BBCodeToMediaWiki(text);

            Page page = new Page(newWiki, newName);
            page.Load();

            bool update = false;
            if (!page.IsEmpty())
            {
                if (!overwrite)
                {
                    Console.WriteLine("Page already exists, exiting");
                    return;
                }
                else update = true;
            }
            if (newName.StartsWith("Mission Editor", true, System.Globalization.CultureInfo.CurrentCulture))
                page.AddToCategory("Mission Editor");
            page.Save(text, update ? "" : "Ported from ZK wiki by DotNetWikiBot", update);
        }

        /// <summary>Like ConvertPage, except works on an already existing MediaWiki page</summary>
        public static void ReformatPage(string pageName)
        {
            Page page = new Page(newWiki, pageName);
            page.Load();
            string text = BBCodeToMediaWiki(page.text);
            if (pageName.StartsWith("Mission Editor", true, System.Globalization.CultureInfo.CurrentCulture))
                page.AddToCategory("Mission Editor");
            page.Save(text, "Cleanup by DotNetWikiBot", false);
        }

        /// <summary>Adds a template to the end of a page.</summary>
        public static void AddTemplate(string pageName, string template)
        {
            Page page = new Page(newWiki, pageName);
            page.Load();
            AddTemplate(page, template);
        }

        /// <summary>Adds a template to the end of a page.</summary>
        public static void AddTemplate(Page page, string template)
        {
            string text = page.text;
            if (page.GetTemplates(false, false).Contains(template))
            {
                Console.WriteLine("Page {0} already has template {1}", page.title, template);
                return;
            }
            page.AddTemplate("{{" + template + "}}");
            page.Save(text, "Infobox added by DotNetWikiBot", true);
        }

        // unused
        public static bool UpdateTemplate(Page page, KeyValuePair<string, object> kvp)
        {
            bool changed = false;
            string key = kvp.Key;
            if (kvp.Value is string)
            {
                string newValue = (string)kvp.Value;
                string currentValue = page.GetFirstTemplateParameter("Infobox zkunit", key);
                if (!string.IsNullOrEmpty(currentValue) && currentValue != newValue)
                {
                    changed = true;
                    Console.WriteLine("Value {0} has changed: {1}, {2}", key, currentValue, newValue);
                }
            }
            else if (kvp.Value is Dictionary<string, object>)
            {
                //changed = Updateemplate
                foreach (KeyValuePair<string, object> kvp2 in (Dictionary <string, object>)kvp.Value)
                {
                    changed = changed || UpdateTemplate(page, kvp2);
                }
            }

            return changed;
        }

        public static void RenamePage(string pageName, string newName, string reason, bool testOnly = false)
        {
            Page page = new Page(newWiki, pageName);
            page.Load();
            if (!page.Exists())
            {
                Console.WriteLine("Page {0} does not exist", pageName);
                return;
            }
            Page newPage = new Page(newWiki, newName);
            newPage.Load();
            if (newPage.Exists())
            {
                Console.WriteLine("Page {0} already exists", newName);
                return;
            }

            if (!testOnly)
            {
                try
                {
                    page.RenameTo(newName, reason, true, false);
                }
                catch (WikiBotException wbex)
                {
                    Console.WriteLine(wbex);
                }
            }
            Console.WriteLine("Renamed page {0} to {1}", pageName, newName);
        }

        /// <summary>Replaces all instances of a specified template from page text with the provided text. Does nothing if old and new templates are identical.</summary>
        /// <param name="templateTitle">Title of template to remove.</param>
        /// <param name="replacement">The text to insert in place of the template.</param>
        /// <returns>True if old and new page texts are different and page text was modified, false otherwise.</returns>
        public static bool ReplaceTemplate(this Page page, string templateTitle, string replacement)
        {
            if (string.IsNullOrEmpty(templateTitle))
                throw new ArgumentNullException("templateTitle");
            templateTitle = Regex.Escape(templateTitle);
            templateTitle = "(" + Char.ToUpper(templateTitle[0]) + "|" +
                Char.ToLower(templateTitle[0]) + ")" +
                (templateTitle.Length > 1 ? templateTitle.Substring(1) : "");
            string currentText = page.text;

            // regex find infobox to replace it
            string tailTag = @"==\s?Description\s?==";
            string newText = Regex.Replace(page.text, @"(?s)\{\{\s*" + templateTitle +
                @"(.*?)}}\r?\n?" + tailTag, replacement + "==Description==");

            // different content ordering (usually manually created page)
            tailTag = @"\s*The '''";
            newText = Regex.Replace(newText, @"(?s)\{\{\s*" + templateTitle +
                @"(.*?)}}\r?\n?" + tailTag, replacement + "The '''");

            if (currentText.Equals(newText))
            {
                return false;
            }
            else
            {
                page.text = newText;
                return true;
            }
        }

        public static string GetExistingTemplate(this Page page, string templateTitle)
        {
            if (string.IsNullOrEmpty(templateTitle))
                throw new ArgumentNullException("templateTitle");

            templateTitle = Regex.Escape(templateTitle);
            templateTitle = "(" + Char.ToUpper(templateTitle[0]) + "|" +
                Char.ToLower(templateTitle[0]) + ")" +
                (templateTitle.Length > 1 ? templateTitle.Substring(1) : "");
            string currentText = page.text;

            // regex find infobox to replace it
            string tailTag = @"==\s?Description\s?==";
            Match match = Regex.Match(currentText, @"(?s)\{\{\s*" + templateTitle +
                @"(.*?)}}\r?\n?" + tailTag);
            
            if (match == null || !match.Success)
            {
                // different content ordering (usually manually created page)
                tailTag = @"\s*The '''";
                match = Regex.Match(currentText, @"(?s)\{\{\s*" + templateTitle +
                    @"(.*?)}}\r?\n?" + tailTag);
            }

            if (match == null || !match.Success)
                return null;

            string result = match.Value;
            result = Regex.Replace(result, tailTag, "");
            return result;
        }

        /// <summary>Updates a unit page using the contents of the specified file.</summary>
        /// <param name="filePath">Text file to read from.</param>
        /// <param name="infoboxOnly">True if we're only replacing the infobox. 
        /// The unit template exporter can also include the intro sentence
        /// and unit helptext, in addition to the infobox.</param>
        /// <returns>True if old and new page texts are different and page text was modified, false otherwise.</returns>
        public static bool UpdateUnitPage(Page page, string filePath, bool infoboxOnly)
        {
            string text = page.text;
            string newText = text;

            // try to isolate the part we want to update
            if (infoboxOnly)
            {
                bool result = page.ReplaceTemplate("Infobox zkunit", File.ReadAllText(filePath));
                return result;
            }
            else
            {
                string tailText = @"==\s?Tactics and Strategy\s?==";
                Match match = Regex.Match(text, tailText);
                if (match.Success)
                {
                    int index = match.Index;
                    newText = text.Remove(0, index).Insert(0, File.ReadAllText(filePath) + "\n\n");
                }
                else
                {
                    tailText = @"{{\s?Navbox";
                    match = Regex.Match(text, tailText);
                    if (match.Success)
                    {
                        int index = match.Index;
                        newText = text.Remove(0, index).Insert(0, File.ReadAllText(filePath) + "\n\n");
                    }
                }

                if (text.Equals(newText))
                {
                    //Console.WriteLine("Did nothing");
                    return false;
                }
                else
                {
                    Console.WriteLine(">> Did something!");
                    Console.WriteLine(newText);
                    page.text = newText;
                    return true;
                }
            }
        }

        /// <summary>Updates unit name references in a page.</summary>
        /// <param name="renames">Array of renames. Each entry is a comma-delimited string (oldName, newName).</param>
        public static void ApplyRenamesToPageText(string pageName, string[] renames)
        {
            Page page = new Page(newWiki, pageName);
            page.Load();
            string newText = page.text;
            foreach (string renameLine in renames)
            {
                string[] kvp = renameLine.Split(',');
                string oldName = kvp[0];
                string newName = kvp[1];
                newText = newText.Replace(oldName, newName);
            }

            if (page.text != newText)
            {
                page.text = newText;
                page.Save("Update unit names", true);
            }
        }

        /// <summary>Updates unit name references in a navbox.</summary>
        /// <param name="renames">Array of renames. Each entry is a comma-delimited string (oldName, newName).</param>
        public static void UpdateUnitNavbox(Page navbox, string[] renames)
        {
            navbox.Load();
            string newText = navbox.text;
            foreach (string renameLine in renames)
            {
                string[] kvp = renameLine.Split(',');
                string oldName = kvp[0];
                string newName = kvp[1];
                newText = newText.Replace(oldName, newName);
            }
            if (navbox.text != newText)
            {
                navbox.text = newText;
                navbox.Save("Update unit names", false);
            }
        }

        public static void UpdateUnitNavboxes(string[] renames)
        {
            Page unitBox = new Page(newWiki, "Template:Navbox_units");
            Page buildingBox = new Page(newWiki, "Template:Navbox_buildings");
            UpdateUnitNavbox(unitBox, renames);
            UpdateUnitNavbox(buildingBox, renames);
        }

        /// <summary>Updates unit pages, either specified ones or all that were outputted by the unit tempalte exporter.</summary>
        public static void UpdatePages()
        {
            int count = 0;
            string dir = "";
            bool infoBoxOnly = true;
            if (infoBoxOnly)
                dir = Path.Combine(fileDir, "raw_infobox/markup");
            else
                dir = Path.Combine(fileDir, "raw/markup");

            string downloadDir = Path.Combine(fileDir, "raw_infobox/web");
            Directory.CreateDirectory(downloadDir);

            List<string> filesUpdate = new List<string>();

            // comment this out to not do all the units at once
            filesUpdate = new List<string>(Directory.GetFiles(dir));

            var filesUpdateManuallySelected = new List<string>(new string[] {
                //"Kodachi", "Reaver", "Blitz", "Ogre", "Pyro", "Grizzly", "Siren", "Dante", "Faraday"
            });
            foreach (string path in filesUpdateManuallySelected)
            {
                string newPath = Path.Combine(dir, path + ".txt");
                filesUpdate.Add(newPath);
                Console.WriteLine(newPath);
            }

            foreach (string path in filesUpdate)
            {
                string unitname = Path.GetFileNameWithoutExtension(path);
                unitname = unitname.Replace("&#47;", "/");
                var page = new Page(newWiki, unitname);
                page.Load();
                if (page.Exists())
                {
                    // Downloads the existing infoboxes from wiki.
                    // It is recommended that you do this and then use a merge tool
                    // to compare with the unit exporter output; some unit infoboxes
                    // have custom content in them (e.g. Wind/Tidal Generator).
                    /*
                    string existingTemplate = page.GetExistingTemplate("Infobox zkunit");
                    if (existingTemplate != null)
                    {
                        String writePath = Path.Combine(downloadDir, Path.GetFileNameWithoutExtension(path) + ".txt");
                        Console.WriteLine("Saving template to: " + writePath);
                        File.WriteAllText(writePath, existingTemplate);
                        count++;
                    }
                    */

                    // Actually modify the pages.
                    bool result = UpdateUnitPage(page, path, infoBoxOnly);
                    if (result)
                    {
                        page.Save("Page auto-updated with DotNetWikiBot", true);
                        //Console.WriteLine("Page " + unitname + " changed");
                        count++;
                    }
                    // Stop and pause every 10 pages, so that you can review the changes on live wiki (check the Recent changes page).
                    if (count >= 10)
                    {
                        count = 0;
                        Console.WriteLine("-- INTERMISSION --");
                        Console.WriteLine("-- Review changes on wiki, then press Enter to continue --");
                        Console.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine("Page " + page.title + " doesn't exist!");
                }
            }
        }

        /// <summary>Update unitpic URLs on unit pages: use manual.zero-k.info instead of packages.springrts.com.</summary>
        public static void ReplaceUnitpics()
        {
            int count = 0;
            string dir = Environment.OSVersion.Platform == PlatformID.Unix ? @"/media/histidine/zkwiki/raw_infobox/markup" : @"G:\zkwiki\raw_infobox\markup";
            var filesUpdate = new List<string>(Directory.GetFiles(dir));
            foreach (string path in filesUpdate)
            {
                string unitname = Path.GetFileNameWithoutExtension(path);
                unitname = unitname.Replace("&#47;", "/");
                var page = new Page(newWiki, unitname);
                page.Load();
                if (page.Exists() && !page.IsRedirect())
                {
                    string oldText = page.text;
                    string text = page.text.Replace(@"http://packages.springrts.com/zkmanual/unitpics/", @"http://manual.zero-k.info/unitpics/");
                    if (!oldText.Equals(text))
                        page.Save(text, "Modified image path with DotNetWikiBot", true);
                }
                count++;
            }
        }

        /// <summary>Opens all wiki pages with offsite image hosting 
        /// (other than zero-k.info, github or licho.eu) in browser.</summary>
        public static void FindOffsiteImages()
        {
            var allPages = File.ReadAllLines(Path.Combine(fileDir, "allpages.txt"));
            foreach (string pageName in allPages){
                Page page = new Page(newWiki, pageName);
                page.LoadTextOnly();
                MatchCollection matches = Regex.Matches(page.text, @"https?://.*?\.(jpe?g|png|gif)");
                if (matches.Count <= 0) continue;
                Console.WriteLine("Trying page " + page.title);

                bool any = false;
                foreach (Match match in matches)
                {
                    foreach (Capture capture in match.Captures)
                    {
                        if (capture.Value.Contains(".github")) continue;
                        if (capture.Value.Contains("zero-k.info")) continue;
                        if (capture.Value.Contains("licho.eu")) continue;
                        Console.WriteLine("\t{0}", capture.Value);
                        any = true;
                    }
                }
                if (any) System.Diagnostics.Process.Start(@"http://zero-k.info/mediawiki/index.php?title=" + page.title);
            }
        }

        public static void RenameUnits()
        {
            string renamedFilesListPath = Path.Combine(fileDir, "renames.csv");
            string[] renames = File.ReadAllLines(renamedFilesListPath);
            
            foreach (string renameLine in renames)
            {
                string[] kvp = renameLine.Split(',');
                string oldName = kvp[0];
                string newName = kvp[1];
                RenamePage(oldName, newName, "Unit renamed", false);
            }
            
            UpdateUnitNavboxes(renames);
            ApplyRenamesToPageText("Cloak", renames);
            ApplyRenamesToPageText("Newbie Guide", renames);
            ApplyRenamesToPageText("Newbie Guide 2", renames);
            ApplyRenamesToPageText("Shield", renames);
        }

        public static void DoStuff()
        {
            string username = "";
            string password = "";

            Console.WriteLine("Enter wiki username: ");
            username = Console.ReadLine();
            Console.WriteLine("Enter wiki password: ");
            password = Console.ReadLine();
            password = password.Trim();

            newWiki = new Site(WIKI_URL, username, password);

            // find pages with offsite images
            //FindOffsiteImages();

            int count = 0;  // increment this when we actually create a page
            string dir = "";

            // create new pages
            /*
            List<string> newFiles = null;    //new List<string>(Directory.GetFiles(dir));
            dir = Environment.OSVersion.Platform == PlatformID.Unix ? @"/media/histidine/My Book/zkwiki/raw/markup" : @"G:\zkwiki\raw\markup";
            newFiles = null;new List<string>(Directory.GetFiles(dir));
            newFiles = files.Shuffle();
            
            foreach (string path in newFiles)
            {
                string unitname = Path.GetFileNameWithoutExtension(path);
                unitname = unitname.Replace("&#47;", "/");
                var page = new Page(newWiki, unitname);
                page.Load();
                if (page.Exists())
                {
                    // do nothing
                }
                else
                {
                    var text = File.ReadAllText(path);
                    Console.WriteLine("-- Making page {0} --", unitname);
                    page.Save(text, "Created page from unitguide builder export", false);
                    count++;
                    //if (count >= 20) break;
                }
            }
            */

            // unit renamer
            //RenameUnits();

            // unit page updater
            UpdatePages();

            // unitpic replacer
            //ReplaceUnitpics();

            // page porting
            /*
            string[,] toPort = 
            {
                //{"MissionEditorCompatibility", "Mission Editor game compatibility"},
                //{"MissionEditorStartPage", "Mission Editor"},
            };
            for (int i=0; i<toPort.GetLength(0); i++)
            {
                ConvertPage(toPort[i, 0], toPort[i, 1], false);
            }

            //
            string[] toReformat =
            {
                //"Mission Editor Cutscenes Tutorial"
            };
            for (int i = 0; i < toReformat.GetLength(0); i++)
            {
                ReformatPage(toReformat[i]);
            }
            */
        }
    }
}
