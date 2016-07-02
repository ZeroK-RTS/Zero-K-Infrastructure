// Write your own bot scripts and functions in this file.
// Run "Compile & Run.bat" file - it will compile this file as executable and launch it.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Xml;
using DotNetWikiBot;

class MyBot : Bot
{
	public void MyFunction1()
	{
		// Write your own function here
	}

	/// The entry point function. Start coding here.
	public static void Main()
	{
		// Compiled documentation is available in Documentation.chm file.

		// A very compehensive DotNetWikiBot usage examples can be found
		// in unit testing file called DebugBot.cs:
		// http://sourceforge.net/p/dotnetwikibot/svn/HEAD/tree/DebugBot.cs

		// Bot scripts repository is being created at
		// https://sourceforge.net/apps/mediawiki/dotnetwikibot/index.php?title=BSR
		// You are welcome to share your scripts.

		// And here you can find some basic usage examples:

		Site site = new Site("https://en.wikipedia.org", "YourBotLogin", "YourBotPassword");
		//Site site = new Site("http://mywikisite.com", "YourBotLogin", "YourBotPassword");
		//Site site = new Site("https://sourceforge.net/apps/mediawiki/YourProjectName/",
								//"YourSourceForgeLogin", "YourSourceForgePassword");

		site.ShowNamespaces();
		Page p = new Page(site, "Wikipedia:Sandbox");
		p.LoadWithMetadata();
		if (p.Exists())
			Console.WriteLine(p.text);
		p.SaveToFile("MyArticles\\file.txt");
		p.LoadFromFile("MyArticles\\file.txt");
		p.ResolveRedirect();
		Console.WriteLine(p.GetNamespace());
		p.text = "new text";
		site.defaultEditComment = "saving test";
		site.minorEditByDefault = true;
		p.Save();

		/**
		string[] arr = {"Art", "Poetry", "Cinematography", "Camera", "Image"};
		PageList pl = new PageList(site, arr);
		pl.LoadWithMetadata();
		pl.FillFromAllPages("Sw", 0, true, 100);
		pl.SaveTitlesToFile("MyArticles\\list.txt");
		pl.FillFromFile("MyArticles\\list.txt");
		pl.FillFromCategory("Category:Cinematography");
		pl.FillFromLinksToPage("Cinematography");
		pl.RemoveEmpty();
		pl.RemoveDisambigs();
		pl.ResolveRedirects();
		Console.WriteLine(pl[2].text);
		pl[1].text = "#REDIRECT [[Some Page]]";
		pl.FilterNamespaces(new int[] {0,3});
		pl.RemoveNamespaces(new int[] {2,4});
		pl.Clear();
		site.defaultEditComment = "my edit comment";
		site.minorEditByDefault = true;
		pl.Save();
		/**/
	}
}