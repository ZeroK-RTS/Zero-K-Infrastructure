using DotNetWikiBot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ZkData;

namespace Fixer
{
    /// <summary>Ports Zero-K wiki pages to the new MediaWiki.</summary>
    public static class WikiPortingMW
    {
        public const string WIKI_URL = "http://zero-k.info";
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

        public static void ReformatPage(string pageName)
        {
            Page page = new Page(newWiki, pageName);
            page.Load();
            string text = BBCodeToMediaWiki(page.text);
            if (pageName.StartsWith("Mission Editor", true, System.Globalization.CultureInfo.CurrentCulture))
                page.AddToCategory("Mission Editor");
            page.Save(text, "Cleanup by DotNetWikiBot", false);
        }

        public static void AddTemplate(string pageName, string template)
        {
            Page page = new Page(newWiki, pageName);
            page.Load();
            AddTemplate(page, template);
        }

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

        /// <summary>Replaces all instances of a specified template from page text with the provided text. Does nothing if old and new templates are identical.</summary>
        /// <param name="templateTitle">Title of template to remove.</param>
        /// <param name="replacement">The text to insert in place of the template.</param>
        /// <returns>True if old and new page texts are different and page text was modified, false otherwise.</returns>
        public static bool ReplaceTemplate(this Page page, string templateTitle, string replacement, string tailTag = null)
        {
            tailTag = tailTag ?? @"==\s?Description\s?==";
            if (string.IsNullOrEmpty(templateTitle))
                throw new ArgumentNullException("templateTitle");
            templateTitle = Regex.Escape(templateTitle);
            templateTitle = "(" + Char.ToUpper(templateTitle[0]) + "|" +
                Char.ToLower(templateTitle[0]) + ")" +
                (templateTitle.Length > 1 ? templateTitle.Substring(1) : "");
            string currentText = page.text;
            string newText = Regex.Replace(page.text, @"(?s)\{\{\s*" + templateTitle +
                @"(.*?)}}\r?\n?" + tailTag, replacement + "==Description==");
            if (currentText.Equals(newText))
            {
                Console.WriteLine("Did nothing");
                return false;
            }
            else
            {
                Console.WriteLine("Did something!");
                //Console.WriteLine(currentText);
                //Console.WriteLine(newText);
                page.text = newText;
                return true;
            }
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

            int count = 0;  // increment this when we actually create a page
            string dir = Environment.OSVersion.Platform == PlatformID.Unix ? @"home/media/My Book/zkwiki/raw/markup" : @"G:\zkwiki\raw\markup";
            var newFiles = new List<string>(Directory.GetFiles(dir));
            //newFiles = files.Shuffle();
            /*
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

            /*
            count = 0;
            dir = Environment.OSVersion.Platform == PlatformID.Unix ? @"home/media/My Book/zkwiki/raw_infobox/markup" : @"G:\zkwiki\raw_infobox\markup";
            var filesUpdate = new List<string>(Directory.GetFiles(dir));
            foreach (string path in filesUpdate)
            {
                string unitname = Path.GetFileNameWithoutExtension(path);
                unitname = unitname.Replace("&#47;", "/");
                var page = new Page(newWiki, unitname);
                page.Load();
                if (page.Exists())
                {
                    //Dictionary<string, object> unitData = new Dictionary<string, object>();
                    //JsonConvert.PopulateObject(File.ReadAllText(path), unitData);
                    //foreach (KeyValuePair<string, object> kvp in unitData)
                    //{
                    //    UpdateTemplate(page, kvp);
                    //}
                    
                    bool result = page.ReplaceTemplate("Infobox zkunit", File.ReadAllText(path));
                    if (result)
                    {
                        page.Save(page.text, "Page auto-updated with DotNetWikiBot", true);
                        count++;
                    }
                    if (count >= 5)
                    {
                        count = 0;
                        Console.WriteLine("-- INTERMISSION --");
                        Console.WriteLine("-- Review changes on wiki, then press Enter to continue --");
                        Console.ReadLine();
                    }
                }
            }
            */

            /*
            count = 0;
            dir = Environment.OSVersion.Platform == PlatformID.Unix ? @"home/media/My Book/zkwiki/raw_infobox/markup" : @"G:\zkwiki\raw_infobox\markup";
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
            */

            string[,] toPort = 
            {
                //{"MissionEditorCompatibility", "Mission Editor game compatibility"},
                //{"MissionEditorStartPage", "Mission Editor"},
                //{"MissionEditorWINE", "Mission Editor in WINE"},
                //{"FactoryOrdersTutorial", "Mission Editor Factory Orders Tutorial"},
                //{"MissionEditorTutorial", "Mission Editor Tutorial"},
                //{"MissionEditorCutsceneTutorial", "Mission Editor Cutscenes Tutorial"}
            };
            for (int i=0; i<toPort.GetLength(0); i++)
            {
                ConvertPage(toPort[i, 0], toPort[i, 1], false);
            }

            string[] toReformat =
            {
                //"Mission Editor Cutscenes Tutorial"
            };
            for (int i = 0; i < toReformat.GetLength(0); i++)
            {
                ReformatPage(toReformat[i]);
            }
        }
    }
}
