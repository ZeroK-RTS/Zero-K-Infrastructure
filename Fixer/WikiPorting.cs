using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Fixer
{
    public static class WikiPorting
    {
        public static string ConvertText(string str)
        {
            Regex rex;

            // first get rid of the summary and label lines if present
            rex = new Regex(@"^#summary .+\n", RegexOptions.IgnoreCase);
            str = rex.Replace(str, "");
            rex = new Regex(@"^#labels .+\n", RegexOptions.IgnoreCase);
            str = rex.Replace(str, "");

            // headers
            rex = new Regex(@"==== *(.+) *====", RegexOptions.IgnoreCase);   //h4
            str = rex.Replace(str, "#### $1");

            rex = new Regex(@"=== *(.+) *===", RegexOptions.IgnoreCase);   //h3
            str = rex.Replace(str, "### $1");

            rex = new Regex(@"== *(.+) *==", RegexOptions.IgnoreCase);   //h2
            str = rex.Replace(str, "## $1");

            rex = new Regex(@"= *(.+) *=", RegexOptions.IgnoreCase);   //h1
            str = rex.Replace(str, "# $1");

            // bullet points - no change required
            // rex = new Regex(@"\n* ((.|)+?)", RegexOptions.IgnoreCase);

            // numbered lists: FIX MANUALLY

            // bawld
            rex = new Regex(@"\*[^ ]((.|\n)+?)\*", RegexOptions.IgnoreCase);  // check for space after first asterisk to ensure not a bulletpoint
            str = rex.Replace(str, "*$0*");

            // italics - no change required
            // rex = new Regex(@"_[^ ]((.|\n)+?)_", RegexOptions.IgnoreCase);
            // str = rex.Replace(str, "*{1}*");

            // wikilinks
            rex = new Regex(@"\[(.*) (.*)\]", RegexOptions.IgnoreCase);
            str = rex.Replace(str, "[$2]($1)");

            // URLs
            rex = new Regex(@"http://zero-k.googlecode.com/svn/trunk/mods/zk", RegexOptions.IgnoreCase);
            str = rex.Replace(str, @"https://raw.githubusercontent.com/ZeroK-RTS/Zero-K/master/");

            rex = new Regex(@"http://zero-k.googlecode.com/svn/trunk/other/", RegexOptions.IgnoreCase);
            str = rex.Replace(str, @"https://raw.githubusercontent.com/ZeroK-RTS/Zero-K-Infrastructure/master/");

            rex = new Regex(@"http://zero-k.googlecode.com/svn/trunk/tools/", RegexOptions.IgnoreCase);
            str = rex.Replace(str, "https://raw.githubusercontent.com/ZeroK-RTS/SpringRTS-Tools/master/");

            rex = new Regex(@"http://zero-k.googlecode.com/svn/trunk/Artwork/", RegexOptions.IgnoreCase);
            str = rex.Replace(str, @"https://raw.githubusercontent.com/ZeroK-RTS/Zero-K-Artwork/master/");

            rex = new Regex(@"\[(.*) (.*)\]", RegexOptions.IgnoreCase);
            str = rex.Replace(str, "[$2]($1)");
            
            // images: FIX MANUALLY

            return str;
        }

        public static void ConvertFile(string source)
        {
            string dir = Path.GetDirectoryName(source);
            string filename = Path.GetFileNameWithoutExtension(source);
            string outputFile = Path.Combine(dir, filename + ".txt");

            StreamReader sourceFile = new StreamReader(source);
            string input = sourceFile.ReadToEnd();
            sourceFile.Close();
            string output = ConvertText(input);

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(outputFile))
            {
                file.Write(output);
            }
        }

        public static void ConvertFiles(string dir)
        {
            string[] files = Directory.GetFiles(dir, "*.wiki");
            foreach (string file in files)
            {
                ConvertFile(file);
            }
        }
    }
}
