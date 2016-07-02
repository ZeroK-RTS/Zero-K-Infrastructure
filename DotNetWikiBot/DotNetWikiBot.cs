// DotNetWikiBot Framework 3.15 - designed to make robots for MediaWiki-powered wiki sites
// Requires Microsoft .NET Framework 3.5+ or Mono 1.9+.
// Distributed under the terms of the GNU GPL 2.0 license: http://www.gnu.org/licenses/gpl-2.0.html
// Copyright (c) Iaroslav Vassiliev (2006-2016) codedriller@gmail.com

using System;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using System.Xml.XPath;
using System.Web;
using System.Linq;
using System.Xml.Linq;
using System.Security;
using System.Security.Permissions;

namespace DotNetWikiBot
{
	/// <summary>Class defines wiki site object.</summary>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	[Serializable]
	public class Site
	{
		/// <summary>Site's URI.</summary>
		public string address;
		/// <summary>User's account to login with.</summary>
		public string userName;
		/// <summary>User's password to login with.</summary>
		public string userPass;
		/// <summary>Default domain for LDAP authentication, if such authentication is allowed on
		/// this site. Additional information can be found
		/// <see href="http://www.mediawiki.org/wiki/Extension:LDAP_Authentication">here</see>.
		/// </summary>
		public string userDomain = "";
		/// <summary>Site title, e.g. "Wikipedia".</summary>
		public string name;
		/// <summary>Site's software identificator, e.g. "MediaWiki 1.21".</summary>
		public string software;
		/// <summary>MediaWiki version as Version object.</summary>
		public Version version;
		/// <summary>If set to false, bot will use MediaWiki's common user interface where
		/// possible, instead of using special API interface for robots (api.php). Default is true.
		/// Set it to false manually if some problem with API interface arises on site.</summary>
		public bool useApi = true;
		/// <summary>Page title capitalization rule on this site.
		/// On most sites capitalization rule is "first-letter".</summary>
		public string capitalization;
		/// <summary>Site's time offset from UTC.</summary>
		public string timeOffset;
		/// <summary>Absolute path to MediaWiki's "index.php" file on the server.</summary>
		public string indexPath;
		/// <summary>Absolute path to MediaWiki's "api.php" file on the server.</summary>
		public string apiPath;
		/// <summary>Short relative path to wiki pages (if such alias is set on the server), e.g.
		/// "/wiki/". See <see href="http://www.mediawiki.org/wiki/Manual:Short URL">this page</see>
		/// for details.</summary>
		public string shortPath;
		/// <summary>User's watchlist. This <see cref="PageList"/> is not filled automatically when
		/// Site object is constructed, you need to call <see cref="PageList.FillFromWatchList()"/>
		/// function to fill it.</summary>
		public PageList watchList;
		/// <summary>MediaWiki system messages (those listed on "Special:Allmessages" page),
		/// user-modified versions. This dictionary is not filled automatically when Site object
		/// is constructed, you need to call <see cref="LoadMediawikiMessages(bool)"/> 
		/// function with "true" parameter to load messages into this dictionary.</summary>
		public Dictionary<string,string> messages;
		/// <summary>Default edit comment. You can set it to whatever you would like.</summary>
		/// <example><code>mySite.defaultEditComment = "My default edit comment";</code></example>
		public string defaultEditComment = "Automatic page editing by robot";
		/// <summary>If set to true, all bot's edits are marked as minor by default.</summary>
		public bool minorEditByDefault = true;
		/// <summary>This is a maximum degree of server load when bot is
		/// still allowed to edit pages. Higher values mean more aggressive behaviour.
		/// See <see href="https://www.mediawiki.org/wiki/Manual:Maxlag_parameter">this page</see>
		/// for details.</summary>
		public int maxLag = 5;
		/// <summary>Number of times to retry bot web action in case of temporary connection
		///  failure or some server problems.</summary>
		public int retryTimes = 3;
		/// <summary>Number of seconds to pause for between edits on this site.
		/// Adjust this variable if required, but it may be overriden by the site policy.</summary>
		public int forceSaveDelay = 0;
		/// <summary>Number of list items to fetch at a time. This settings concerns special pages
		/// output and API lists output. Default is 500. Bot accounts are allowed to fetch
		/// up to 5000 items at a time. Adjust this number if required.</summary>
		public int fetchRate = 500;
		/// <summary>Templates, which are used to distinguish disambiguation pages. Set this
		/// variable manually if required. Multiple templates can be specified, use '|'
		/// character as the delimeter. Letters case doesn't matter.</summary>
		/// <example><code>site.disambig = "disambiguation|disambig|disam";</code></example>
		public string disambig;
		/// <summary>A set of regular expressions for parsing pages. Usually there is no need
		/// to edit these regular expressions manually.</summary>
		public Dictionary<string,Regex> regexes = new Dictionary<string,Regex>();
		/// <summary>Site's cookies.</summary>
		public CookieContainer cookies = new CookieContainer();
		/// <summary>Local namespaces, default namespaces and local namespace aliases, joined into
		/// strings, enclosed in and delimited by '|' character.</summary>
		public Dictionary<int,string> namespaces;
		/// <summary>Parsed supplementary data, mostly localized strings.</summary>
		public Dictionary<string,string> generalData;
		/// <summary>Parsed API session-wide security tokens for editing.</summary>
		public Dictionary<string, string> tokens;
		/// <summary>Site's language.</summary>
		public string language;
		/// <summary>Site's language culture. Required for string comparison.</summary>
		public CultureInfo langCulture;
		/// <summary>Randomly chosen regional culture for this site's language.
		/// Required to parse dates.</summary>
		public CultureInfo regCulture;

		/// <summary>Supplementary data, mostly localized strings.</summary>
		/// <exclude/>
		public XElement generalDataXml;
		/// <summary>Time of last page saving operation on this site expressed in UTC.
		/// This internal parameter is used to prevent server overloading.</summary>
		/// <exclude/>
		public DateTime lastWriteTime = DateTime.MinValue;
		/// <summary>Wiki server's time offset from local computer's time in seconds.
		/// Timezones difference is omitted, UTC time is compared with UTC time.</summary>
		/// <exclude/>
		public int timeOffsetSeconds;


		/// <summary>This constructor uses default userName and password for site, if
		/// userName and password are found in "Defaults.dat" file in bot's "Cache"
		/// subdirectory. File must be UTF8-encoded and must contain user names and passwords in
		/// the following format:
		/// <code>
		/// https://en.wikipedia.org|MyUserName|MyPassword
		/// https://de.wikipedia.org|MyUserName|MyPassword|MyDomain
		/// </code>
		/// Each site's accouint must be on the new line.
		/// This function allows distributing compiled bots without revealing passwords.</summary>
		/// <param name="address">Wiki site's URI. It must point to the main page of the wiki, e.g.
		/// "https://en.wikipedia.org" or "http://127.0.0.1:80/w/index.php?title=Main_page".</param>
		/// <returns>Returns Site object.</returns>
		public Site(string address)
		{
			string defaultsFile = Bot.cacheDir + Path.DirectorySeparatorChar + "Defaults.dat";
			if (File.Exists(defaultsFile) == true)
			{
				string[] lines = File.ReadAllLines(defaultsFile, Encoding.UTF8);
				foreach (string line in lines) {
					if (line.StartsWith(address + '|')) {
						string[] tokens = line.Split('|');
						if (tokens.GetUpperBound(0) < 2) {
							throw new WikiBotException(
								Bot.Msg("\"\\Cache\\Defaults.dat\" file is invalid."));
						}
						this.address = tokens[0];
						this.userName = tokens[1];
						this.userPass = tokens[2];
						if (tokens.GetUpperBound(0) >= 3)
							this.userDomain = tokens[3];
					}
				}
				if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userPass))
					throw new WikiBotException(string.Format(
						Bot.Msg("Site \"{0}\" was not found in \"\\Cache\\Defaults.dat\" file."),
						address));
			}
			else
				throw new WikiBotException(Bot.Msg("\"\\Cache\\Defaults.dat\" file not found."));

			Initialize();
		}

		/// <summary>This constructor is used to generate most Site objects.</summary>
		/// <param name="address">Wiki site's URI. It must point to the main page of the wiki, e.g.
		/// "https://en.wikipedia.org" or "http://127.0.0.1:80/w/index.php?title=Main_page".</param>
		/// <param name="userName">User name to log in.</param>
		/// <param name="userPass">Password.</param>
		/// <returns>Returns Site object.</returns>
		public Site(string address, string userName, string userPass)
			: this(address, userName, userPass, "") { }

		/// <summary>This constructor is used for LDAP authentication. Additional information can
		/// be found <see href="http://www.mediawiki.org/wiki/Extension:LDAP_Authentication">here
		/// </see>.</summary>
		/// <param name="address">Wiki site's URI. It must point to the main page of the wiki, e.g.
		/// "https://en.wikipedia.org" or "http://127.0.0.1:80/w/index.php?title=Main_page".</param>
		/// <param name="userName">User name to log in.</param>
		/// <param name="userPass">Password.</param>
		/// <param name="userDomain">Domain name for LDAP authentication.</param>
		/// <returns>Returns Site object.</returns>
		public Site(string address, string userName, string userPass, string userDomain)
		{
			this.address = address;
			this.userName = userName;
			this.userPass = userPass;
			this.userDomain = userDomain;

			Initialize();
		}

		/// <summary>This internal function initializes Site object.</summary>
		/// <exclude/>
		private void Initialize()
		{
			// Correct the address if required
			if (!address.StartsWith("http"))
				address = "http://" + address;
			if (Bot.CountMatches(address, "/", false) == 3 && address.EndsWith("/"))
				address = address.Remove(address.Length - 1);

			regexes["titleLink"] =
				new Regex("<a [^>]*title=\"(?<title>.+?)\"");
			regexes["titleLinkInList"] =
				new Regex("<li(?: [^>]*)?>\\s*<a [^>]*title=\"(?<title>.+?)\"");
			regexes["titleLinkInTable"] =
				new Regex("<td(?: [^>]*)?>\\s*<a [^>]*title=\"(?<title>.+?)\"");
			regexes["titleLinkShown"] =
				new Regex("<a [^>]*title=\"([^\"]+)\"[^>]*>\\s*\\1\\s*</a>");
			regexes["linkToSubCategory"] =
				new Regex(">([^<]+)</a></div>\\s*<div class=\"CategoryTreeChildren\"");
			regexes["linkToImage"] =
				new Regex("<div class=\"gallerytext\">\n<a href=\"[^\"]*?\" title=\"([^\"]+?)\">");
			regexes["wikiLink"] =
				new Regex(@"\[\[(?<link>(?<title>.+?)(?<params>\|.+?)?)]]");
			regexes["wikiTemplate"] =
				new Regex(@"(?s)\{\{(.+?)((\|.*?)*?)}}");
			regexes["webLink"] =
				new Regex("(https?|t?ftp|news|nntp|telnet|irc|gopher)://([^\\s'\"<>]+)");
			regexes["noWikiMarkup"] =
				new Regex("(?is)<nowiki>(.*?)</nowiki>");
			regexes["editToken"] =
				new Regex("(?i)value=\"([^\"]+)\"[^>]+name=\"wpEditToken\"" +
					"|name=\"wpEditToken\"[^>]+value=\"([^\"]+)\"");
			regexes["editTime"] =
				new Regex("(?i)value=\"([^\"]+)\"[^>]+name=\"wpEdittime\"" +
					"|name=\"wpEdittime\"[^>]+value=\"([^\"]+)\"");
			regexes["startTime"] =
				new Regex("(?i)value=\"([^\"]+)\"[^>]+name=\"wpStarttime\"" +
					"|name=\"wpStarttime\"[^>]+value=\"([^\"]+)\"");
			regexes["baseRevId"] =
				new Regex("(?i)value=\"([^\"]+)\"[^>]+name=\"baseRevId\"" +
					"|name=\"baseRevId\"[^>]+value=\"([^\"]+)\"");

			// Find path to index.php
			string cacheFile = Bot.cacheDir + Path.DirectorySeparatorChar +
				Bot.UrlEncode(address.Replace("://", ".").Replace("/", ".")) + ".xml";
			if (!Directory.Exists(Bot.cacheDir))
				Directory.CreateDirectory(Bot.cacheDir);
			XElement cache;
			if (File.Exists(cacheFile) == true) {
				cache = XElement.Load(cacheFile);
				indexPath = cache.Descendants("indexPath").FirstOrDefault().Value;
			}
			else {
				string src = GetWebPage(address + "/mediawiki");    // FIXME: ZK hax
				Uri addressUri = new Uri(address);
				Regex hrefRegex = new Regex("(?i) href=\"(([^\"]*)(index|api)\\.php)");
				try {
					foreach (Match m in hrefRegex.Matches(src)) {
						if (m.Groups[1].Value.StartsWith(address)) {
							indexPath = m.Groups[2].Value + "index.php";
							break;
						}
						else if (m.Groups[1].Value.StartsWith("//" + addressUri.Authority)) {
							if (address.StartsWith("https:"))
								indexPath = "https:" + m.Groups[2].Value + "index.php";
							else
								indexPath = "http:" + m.Groups[2].Value + "index.php";
							break;
						}
						else if (m.Groups[1].Value[0] == '/' && m.Groups[1].Value[1] != '/') {
							indexPath = address + m.Groups[2].Value + "index.php";
							break;
						}
						else if (string.IsNullOrEmpty(m.Groups[2].Value)) {
							indexPath = address + "/index.php";
							break;
						}
					}
				}
				catch {
					throw new WikiBotException(Bot.Msg("Can't find path to index.php."));
				}
				if (indexPath == null)
					throw new WikiBotException(Bot.Msg("Can't find path to index.php."));
				if (indexPath.Contains("api.php"))
					indexPath = indexPath.Replace("api.php", "index.php");

				cache = new XElement("siteInfo", new XElement("indexPath", indexPath));
				cache.Save(cacheFile);
			}
			apiPath = indexPath.Replace("index.php", "api.php");

			Console.WriteLine(Bot.Msg("Logging in..."));

			LogIn();

			LoadGeneralInfo();

			// Load API security tokens if available
			string tokensXmlSrc = GetWebPage(apiPath + "?action=query&format=xml&meta=tokens" +
				"&type=csrf|deleteglobalaccount|patrol|rollback|setglobalaccountstatus" +
				"|userrights|watch&curtimestamp");
			XElement tokensXml = XElement.Parse(tokensXmlSrc);
			if (tokensXml.Element("query") != null) {
				tokens = (
					from attr in tokensXml.Element("query").Element("tokens").Attributes()
					select new {
						attrName = attr.Name.ToString(),
						attrValue = attr.Value
					}
				).ToDictionary(s => s.attrName, s => s.attrValue);
			}

			if (!Bot.isRunningOnMono)
				Bot.DisableCanonicalizingUriAsFilePath();    // .NET bug evasion

			Bot.lastSite = this;

			Console.WriteLine(Bot.Msg("Site: {0} ({1})"), name, software);
		}

		/// <summary>This internal function gets general information about the site.</summary>
		/// <exclude/>
		private void LoadGeneralInfo()
		{
			string src = GetWebPage(apiPath + "?action=query&format=xml" +
				"&meta=siteinfo&siprop=general|namespaces|namespacealiases|magicwords|" +
				"interwikimap|fileextensions|variables");
			generalDataXml = XElement.Parse(src).Element("query");

			// Load namespaces
			namespaces = (
				from el in generalDataXml.Element("namespaces").Descendants("ns")
				//where el.Attribute("id").Value != "0"
				select new {
					code = int.Parse(el.Attribute("id").Value),
					name = ('|' + (el.IsEmpty ? "" : el.Value) +
							'|' + (!el.IsEmpty && el.Value != el.Attribute("canonical").Value
									? el.Attribute("canonical").Value + '|' : "" )
							).ToString()
				}
			).ToDictionary(s => s.code, s => s.name);

			// Load and add namespace aliases
			var aliases = (
				from el in generalDataXml.Element("namespacealiases").Descendants("ns")
				select new {
					code = int.Parse(el.Attribute("id").Value),
					name = el.Value.ToString()
				}
			);
			foreach (var alias in aliases)
				namespaces[alias.code] += alias.name + '|';
					// namespace 0 may have an alias (!)

			// Load general site properties
			generalData = (
				from attr in generalDataXml.Element("general").Attributes()
				select new {
					attrName = attr.Name.ToString(),
					attrValue = attr.Value
				}
			).ToDictionary(s => s.attrName, s => s.attrValue);

			// Load interwiki which are recognized locally, interlanguage links are included
			// Prefixes are combined into string delimited by '|'
			generalData["interwiki"] = string.Join( "|", (
				from el in generalDataXml.Descendants("iw")
				select el.Attribute("prefix").Value
			).ToArray());

			// Load MediaWiki variables (https://www.mediawiki.org/wiki/Help:Magic_words)
			// These are used in double curly brackets, like {{CURRENTVERSION}} and must
			// be distinguished from templates.
			// Variables are combined into string delimited by '|'.
			generalData["variables"] = string.Join("|", (
				from el in generalDataXml.Descendants("v")
				select el.Value
			).ToArray());

			// Load MediaWiki magic words (https://www.mediawiki.org/wiki/Help:Magic_words)
			// These include MediaWiki variables and parser functions which are used in
			// double curly brackets, like {{padleft:xyz|stringlength}} or
			// {{#formatdate:date}} and must be distinguished from templates.
			// Magic words are combined into string delimited by '|'.
			generalData["magicWords"] = string.Join("|", (
				from el in generalDataXml.Element("magicwords").Descendants("alias")
				select el.Value
			).ToArray());

			// Set Site object's properties
			if (generalData.ContainsKey("articlepath"))
				shortPath = generalData["articlepath"].Replace("$1", "");
			if (generalData.ContainsKey("generator")) {
				version = new Version(Regex.Replace(generalData["generator"], @"[^\d\.]", ""));
				if (version < new Version("1.20"))
					Console.WriteLine(Bot.nl + Bot.nl + Bot.Msg("WARNING: This MediaWiki " +
						"version is outdated, some bot functions may not work properly. Please " +
						"consider downgrading to DotNetWikiBot {0} to work with " +
						"this site.") + Bot.nl + Bot.nl, "2.x");
			}
			language = generalData["lang"];
			capitalization = generalData["case"];
			timeOffset = generalData["timeoffset"];
			name = generalData["sitename"];
			software = generalData["generator"];

			DateTime wikiServerTime = DateTime.ParseExact(generalData["time"],
				"yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
			timeOffsetSeconds = (int)(wikiServerTime - DateTime.UtcNow).TotalSeconds - 2;
				// 2 seconds are substracted so we never get time in the future on the server

			// Select general and regional CultureInfo, mainly for datetime parsing
			try {
				langCulture = new CultureInfo(language, false);
			}
			catch (Exception) {
				langCulture = new CultureInfo("");
			}
			if (langCulture.Equals(CultureInfo.CurrentUICulture.Parent))
				regCulture = CultureInfo.CurrentUICulture;
			else {
				try {
					regCulture = CultureInfo.CreateSpecificCulture(language);
				}
				catch (Exception) {
					foreach (CultureInfo ci in
						CultureInfo.GetCultures(CultureTypes.SpecificCultures)) {
						if (langCulture.Equals(ci.Parent)) {
							regCulture = ci;
							break;
						}
					}
					if (regCulture == null)
						regCulture = CultureInfo.InvariantCulture;
				}
			}

			// Load local redirection tags
			generalData["redirectTags"] = (
				from el in Bot.commonDataXml.Element("RedirectionTags").Descendants("rd")
				where el.Attribute("lang").Value == language
				select el.Value
			).SingleOrDefault();

			// Construct regular expressions
			regexes["redirect"] = new Regex(@"(?i)^ *#(?:" + generalData["redirectTags"] +
				@")\s*:?\s*\[\[(.+?)(\|.+)?]]");
				// RegexOptions.Compiled option seems to be too slow

			regexes["magicWordsAndVars"] = new Regex(@"^(?:" +
				generalData["magicWords"].ToLower() + '|' + generalData["variables"] + ')');

			string allNsPrefixes = string.Join("|",
				namespaces.Select(x => x.Value.Substring(1, x.Value.Length-2)).ToArray());
			allNsPrefixes = allNsPrefixes.Replace("||", "|");    // remove empty string from ns 0
			regexes["allNsPrefixes"] = new Regex(@"(?i)^(?:" + allNsPrefixes + "):");
				// (?i)^(?:Media|Special|Talk|User|U|User talk|UT|...|Module talk):

			regexes["interwikiLink"] = new Regex(@"(?i)\[\[((" + generalData["interwiki"] +
				"):(.+?))]]");

			regexes["wikiCategory"] = new Regex(
				@"(?i)\[\[\s*(((" + GetNsPrefixes(14) + @"):(.+?))(\|.+?)?)]]");

			regexes["wikiImage"] = new Regex(@"\[\[(?i)((" + GetNsPrefixes(6) +
				@"):(.+?))(\|(.+?))*?]]");

			regexes["linkToImage2"] = new Regex("<a href=\"[^\"]*?\" title=\"(" +
				Regex.Escape(GetNsPrefix(6)) + "[^\"]+?)\">");
					// that's right, localized namespace prefix only

			// Parser functions are now loaded from site (among magicWords)
			//generalData.Add("parserFunctions",
				//Bot.commonDataXml.Descendants("pf").Select(el => el.Value).ToArray());
		}

		/// <summary>Gets MediaWiki system messages
		/// (those listed on "Special:Allmessages" page).</summary>
		/// <param name="modified">If true, the user-customized messages are returned.
		/// If false, original unmodified messages from MediaWiki package are returned.
		/// The latter is required very rarely.</param>
		/// <returns>Returns dictionary, where keys are message identifiers (all in lower case)
		/// and values are message texts.</returns>
		public Dictionary<string,string> LoadMediawikiMessages(bool modified)
		{
			Console.WriteLine(Bot.Msg("Updating MediaWiki messages. Please, wait..."));
			Dictionary<string,string> mediaWikiMessages;

			if (useApi && modified) {    // there is no way to get unmodified versions via API
				// no paging is required, all messages are returned in one chunk
				string src = GetWebPage(apiPath + "?action=query" +
					"&meta=allmessages&format=xml&amenableparser=1&amcustomised=all");
						// "&amcustomised=all" query actually brings users-modified messages
				XElement messagesXml = XElement.Parse(src);
				
				mediaWikiMessages = (
					from el in messagesXml.Descendants("message")
					select new {
						id = el.Attribute("name").Value,
						body = el.Value
					}
				).ToDictionary(s => s.id, s => s.body);
			}
			else {
				// paging may be broken
				string res = indexPath + "?title=Special:AllMessages&limit=50000&filter=all";
				string src = "", messageId = "";
				mediaWikiMessages = new Dictionary<string,string>();
				Regex nextPortionRegex =
					new Regex("offset=([^\"&]+)[^\"]*?\" title=\"[^\"]+\" rel=\"next\"");
				do {
					src = GetWebPage(res + (!string.IsNullOrEmpty(src) ? "&offset=" +
						HttpUtility.HtmlDecode(nextPortionRegex.Match(src).Groups[1].Value) : ""));
					src = Bot.GetSubstring(src, "<tbody>", "</tbody>");
					using (XmlReader reader = Bot.GetXMLReader(src)) {
						reader.ReadToFollowing("tbody");
						while (reader.Read()) {
							if (reader.Name == "tr"
								&& reader.NodeType == XmlNodeType.Element
								&& reader["id"] != null)
								messageId = reader["id"].Replace("msg_", "");
							else if (reader.Name == "td"
								&& reader.NodeType == XmlNodeType.Element
								&& reader["class"] == "am_default")
								mediaWikiMessages[messageId] = reader.ReadString();
							else if (modified
								&& reader.Name == "td"
								&& reader.NodeType == XmlNodeType.Element
								&& reader["class"] == "am_actual")
								mediaWikiMessages[messageId] = reader.ReadString();
							else if (reader.Name == "tbody"
								&& reader.NodeType == XmlNodeType.EndElement)
								break;
						}
					}
				} while (nextPortionRegex.IsMatch(src));
			}

			if (modified)
				messages = mediaWikiMessages;
			Console.WriteLine(Bot.Msg("MediaWiki system messages have been loaded successfully."));
			return mediaWikiMessages;
		}

		/// <summary>Logs in and retrieves cookies.</summary>
		private void LogIn()
		{
			if (!useApi) {
				string loginPageSrc = PostDataAndGetResult(indexPath +
					"?title=Special:Userlogin", "", true, true);
				string loginToken = "";
				int loginTokenPos = loginPageSrc.IndexOf(
					"<input type=\"hidden\" name=\"wpLoginToken\" value=\"");
				if (loginTokenPos != -1)
					loginToken = loginPageSrc.Substring(loginTokenPos + 48, 32);

				string postData = string.Format("wpName={0}&wpPassword={1}&wpDomain={2}" +
					"&wpLoginToken={3}&wpRemember=1&wpLoginattempt=Log+in",
					Bot.UrlEncode(userName), Bot.UrlEncode(userPass),
					Bot.UrlEncode(userDomain), Bot.UrlEncode(loginToken));
				string respStr = PostDataAndGetResult(indexPath +
					"?title=Special:Userlogin&action=submitlogin&type=login",
					postData, true, false);
				if (respStr.Contains("<div class=\"errorbox\">"))
					throw new WikiBotException(
						"\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");
				Console.WriteLine(Bot.Msg("Logged in as {0}."), userName);
			}
			else {
				string postData = string.Format("lgname={0}&lgpassword={1}&lgdomain={2}",
						Bot.UrlEncode(userName), Bot.UrlEncode(userPass),
						Bot.UrlEncode(userDomain));

				// At first load login security token
				string tokenXmlSrc = PostDataAndGetResult(apiPath +
					"?action=query&meta=tokens&type=login&format=xml", "", true, false);
				XElement tokenXml = XElement.Parse(tokenXmlSrc);
				string respStr = "", loginToken = "";
				try {
					loginToken = tokenXml.Element("query").Element("tokens")
						.Attribute("logintoken").Value;
				}
				catch {
					// old fallback method
					respStr = PostDataAndGetResult(apiPath +
						"?action=login&format=xml", postData, true, false);
					if (respStr.Contains("result=\"Success\"")) {
						Console.WriteLine(Bot.Msg("Logged in as {0}."), userName);
						return;
					}
					int tokenPos = respStr.IndexOf("token=\"");
					if (tokenPos < 1)
						throw new WikiBotException(
							"\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");
					loginToken = respStr.Substring(tokenPos + 7, 32);
				}

				postData += "&lgtoken=" + Bot.UrlEncode(loginToken);
				respStr = PostDataAndGetResult(apiPath +
					"?action=login&format=xml", postData, true, false);
				if (!respStr.Contains("result=\"Success\""))
					throw new WikiBotException(
						"\n\n" + Bot.Msg("Login failed. Check your username and password.") + "\n");
				Console.WriteLine(Bot.Msg("Logged in as {0}."), userName);
			}
		}

		/// <summary>Gets the list of all WikiMedia Foundation wiki sites as listed
		/// <see href="http://meta.wikimedia.org/wiki/Special:SiteMatrix">here</see>.</summary>
		/// <param name="officialOnly">If set to false, function also returns special and private
		/// WikiMedia projects.</param>
		/// <returns>Returns list of strings.</returns>
		public List<string> GetWikimediaProjects(bool officialOnly)
		{
			string src = GetWebPage("http://meta.wikimedia.org/wiki/Special:SiteMatrix");
			if (officialOnly)
				src = Bot.GetSubstring(src, "<a id=\"aa\" name=\"aa\">",
					"<a id=\"total\" name=\"total\">");
			else
				src = Bot.GetSubstring(src, "<a id=\"aa\" name=\"aa\">",
					"class=\"printfooter\"");
			Regex siteRegex = new Regex("<a href=\"(?://)?([^\"/]+)");
			return siteRegex.Matches(src).Cast<Match>().Select(m => m.Groups[1].Value).ToList();
		}

		/// <summary>Gets the text of page from web.</summary>
		/// <param name="pageURL">Absolute or relative URI of page to get.</param>
		/// <returns>Returns source code.</returns>
		public string GetWebPage(string pageURL)
		{
			return PostDataAndGetResult(pageURL, "", false, true);
		}

		/// <summary>Posts specified string to requested resource
		/// and gets the result text.</summary>
		/// <param name="pageURL">Absolute or relative URI of page to get.</param>
		/// <param name="postData">String to post to site with web request.</param>
		/// <returns>Returns text.</returns>
		public string PostDataAndGetResult(string pageURL, string postData)
		{
			return PostDataAndGetResult(pageURL, postData, false, true);
		}

		/// <summary>Posts specified string to requested resource
		/// and gets the result text.</summary>
		/// <param name="pageURL">Absolute or relative URI of page to get.</param>
		/// <param name="postData">String to post to site with web request.</param>
		/// <param name="getCookies">If set to true, gets cookies from web response and
		/// saves it in Site.cookies container.</param>
		/// <param name="allowRedirect">Allow auto-redirection of web request by server.</param>
		/// <returns>Returns text.</returns>
		public string PostDataAndGetResult(string pageURL, string postData, bool getCookies,
			bool allowRedirect)
		{
			if (string.IsNullOrEmpty(pageURL))
				throw new ArgumentNullException("pageURL", Bot.Msg("No URL specified."));
			if (pageURL.StartsWith("/") && !pageURL.StartsWith("//"))
				pageURL = address + pageURL;

			int retryDelaySeconds = 60;
			HttpWebResponse webResp = null;
			for (int errorCounter = 0; true; errorCounter++) {
				HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(pageURL);
				webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
				webReq.UseDefaultCredentials = true;
				webReq.ContentType = "application/x-www-form-urlencoded";
				webReq.Headers.Add("Cache-Control", "no-cache, must-revalidate");
				webReq.UserAgent = Bot.botVer;
				webReq.AllowAutoRedirect = allowRedirect;
				if (cookies.Count == 0)
					webReq.CookieContainer = new CookieContainer();
				else
					webReq.CookieContainer = cookies;
				if (Bot.unsafeHttpHeaderParsingUsed == 0) {
					webReq.ProtocolVersion = HttpVersion.Version10;
					webReq.KeepAlive = false;
				}
				if (!Bot.isRunningOnMono) {    // Mono bug evasion
					// last checked in January 2015 on Mono 3.12 for Windows
					// http://mono.1490590.n4.nabble.com/...
					// ...EntryPointNotFoundException-CreateZStream-td4661364.html
					webReq.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
				}
				if (!string.IsNullOrEmpty(postData)) {
					if (Bot.isRunningOnMono)    // Mono bug 636219 evasion
						webReq.AllowAutoRedirect = false;
							// https://bugzilla.novell.com/show_bug.cgi?id=636219
					webReq.Method = "POST";
					//webReq.Timeout = 180000;
					postData += "&maxlag=" + maxLag;
					byte[] postBytes = Encoding.UTF8.GetBytes(postData);
					webReq.ContentLength = postBytes.Length;
					Stream reqStrm = webReq.GetRequestStream();
					reqStrm.Write(postBytes, 0, postBytes.Length);
					reqStrm.Close();
				}		
				try {
					webResp = (HttpWebResponse)webReq.GetResponse();
					if (webResp.Headers["Retry-After"] != null)
						throw new WebException("Service is unavailable due to high load.");
							// API can return HTTP code 200 (OK) along with "Retry-After"
					break;
				}
				catch (WebException e) {

					if (webResp == null)
						throw;

					if (webReq.AllowAutoRedirect == false &&
						webResp.StatusCode == HttpStatusCode.Redirect)    // Mono bug 636219 evasion
							return "";

					if (e.Message.Contains("Section=ResponseStatusLine")) {   // Known Squid problem
						Bot.SwitchUnsafeHttpHeaderParsing(true);
						return PostDataAndGetResult(pageURL, postData, getCookies, allowRedirect);
					}

					if (webResp.Headers["Retry-After"] != null) {    // Server is very busy
						if (errorCounter > retryTimes)
							throw;
						// See https://www.mediawiki.org/wiki/Manual:Maxlag_parameter
						int seconds;
						Int32.TryParse(webResp.Headers["Retry-After"], out seconds);
						if (seconds > 0)
							retryDelaySeconds = seconds;
						Console.Error.WriteLine(e.Message);
						Console.Error.WriteLine(string.Format(Bot.Msg(
							"Retrying in {0} seconds..."), retryDelaySeconds));
						Bot.Wait(retryDelaySeconds);
					}
					else if (e.Status == WebExceptionStatus.ProtocolError) {
						int code = (int) webResp.StatusCode;
						if (code == 500 || code == 502 || code == 503 || code == 504)
						{
							// Remote server problem, retry
							if (errorCounter > retryTimes)
								throw;
							Console.Error.WriteLine(e.Message);
							Console.Error.WriteLine(string.Format(Bot.Msg(
								"Retrying in {0} seconds..."), retryDelaySeconds));
							Bot.Wait(retryDelaySeconds);

						}
						else
							throw;
					}
					else
						throw;
				}
			}
			Stream respStream = webResp.GetResponseStream();
			if (webResp.ContentEncoding.ToLower().Contains("gzip"))
				respStream = new GZipStream(respStream, CompressionMode.Decompress);
			else if (webResp.ContentEncoding.ToLower().Contains("deflate"))
				respStream = new DeflateStream(respStream, CompressionMode.Decompress);
			if (getCookies == true) {
				Uri siteUri = new Uri(address);
				foreach (Cookie cookie in webResp.Cookies) {
					if (cookie.Domain[0] == '.' &&
						cookie.Domain.Substring(1) == siteUri.Host)
							cookie.Domain = cookie.Domain.TrimStart(new char[] {'.'});
					cookies.Add(cookie);
				}
			}
			StreamReader strmReader = new StreamReader(respStream, Encoding.UTF8);
			string respStr = strmReader.ReadToEnd();
			strmReader.Close();
			webResp.Close();
			return respStr;
		}

		/// <summary>Gets and parses results of specified custom API query.
		/// Only some basic queries are supported and can be parsed automatically.</summary>
		/// <param name="query">Type of query, e.g. "list=logevents" or "prop=links".</param>
		/// <param name="queryParams">Additional query parameters, specific to the
		/// query. Options and their descriptions can be obtained by calling api.php on target site
		/// without parameters, e.g. http://en.wikipedia.org/w/api.php,
		/// <see href="http://en.wikipedia.org/wiki/Special:ApiSandbox">API Sandbox</see>
		/// is also very useful for experiments.
		/// Parameters' values must be URL-encoded with <see cref="Bot.UrlEncode(string)"/> function
		/// before calling this function.</param>
		/// <param name="limit">Maximum number of resultant strings to fetch.</param>
		/// <example><code>
		/// GetApiQueryResult("list=categorymembers",
		/// 	"cmnamespace=0|14&amp;cmcategory=" + Bot.UrlEncode("Physical sciences"),
		/// 	int.MaxValue);
		/// </code></example>
		/// <example><code>
		/// GetApiQueryResult("list=logevents",
		/// 	"letype=patrol&amp;titles=" + Bot.UrlEncode("Physics"),
		/// 	200);
		/// </code></example>
		/// <example><code>
		/// GetApiQueryResult("prop=links",
		/// 	"titles=" + Bot.UrlEncode("Physics"),
		/// 	int.MaxValue);
		/// </code></example>
		/// <returns>List of dictionary objects is returned. Dictionary keys will contain the names
		/// of attributes of each found target element, and dictionary values will contain values
		/// of those attributes. If target element is not empty element, it's value will be
		/// included into dictionary under special "_Value" key.</returns>
		public List<Dictionary<string,string>> GetApiQueryResult(string query,
			string queryParams, int limit)
		{
			if (string.IsNullOrEmpty(query))
				throw new ArgumentNullException("query");
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit");

			var queryXml =
				from el in Bot.commonDataXml.Element("ApiOptions").Descendants("query")
				where el.Value == query
				select el;
			if (queryXml == null)
				throw new WikiBotException(
					string.Format(Bot.Msg("The list \"{0}\" is not supported."), query));

			string prefix = queryXml.FirstOrDefault().Attribute("prefix").Value;
			string targetTag = queryXml.FirstOrDefault().Attribute("tag").Value;
			string targetAttribute = queryXml.FirstOrDefault().Attribute("attribute").Value;
			if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(targetTag))
				throw new WikiBotException(
					string.Format(Bot.Msg("The list \"{0}\" is not supported."), query));

			List<Dictionary<string, string>> results = new List<Dictionary<string,string>>();
			string continueFromAttr = prefix + "from";
			string continueAttr = prefix + "continue";
			string queryUri = apiPath + "?format=xml&action=query&" + query +
				'&' + prefix + "limit=" + (limit > fetchRate ? fetchRate : limit).ToString();
			string src = "", next = "", queryFullUri = "";
			do
			{
				queryFullUri = queryUri;
				if (next != "")
					queryFullUri += '&' + prefix + "continue=" + Bot.UrlEncode(next);
				src = PostDataAndGetResult(queryFullUri, queryParams);
				using (XmlTextReader reader = new XmlTextReader(new StringReader(src)))
				{
					next = "";
					while (reader.Read())
					{
						if (reader.NodeType == XmlNodeType.Element && reader.Name == targetTag) {
							Dictionary<string,string> dict = new Dictionary<string,string>();
							if (!reader.IsEmptyElement) {
								dict["_Value"] = HttpUtility.HtmlDecode(reader.Value);
								if (targetAttribute == null)
									dict["_Target"] = dict["_Value"];
							}
							for (int i = 0; i < reader.AttributeCount; i++) {
								reader.MoveToAttribute(i);
								dict[reader.Name] = HttpUtility.HtmlDecode(reader.Value);
								if (targetAttribute != null && reader.Name == targetAttribute)
									dict["_Target"] = dict[reader.Name];
							}
							results.Add(dict);
						}
						else if (reader.IsEmptyElement && reader[continueFromAttr] != null)
							next = reader[continueFromAttr];
						else if (reader.IsEmptyElement && reader[continueAttr] != null)
							next = reader[continueAttr];
					}
				}
			}
			while (next != "" && results.Count < limit);

			if (results.Count > limit) {
				results.RemoveRange(limit, results.Count - limit);
			}
			return results;
		}

		/// <summary>Gets main local prefix for specified namespace and colon.</summary>
		/// <param name="nsIndex">Index of namespace to get prefix for.</param>
		/// <returns>Returns the prefix with colon, e.g., "Kategorie:".</returns>
		public string GetNsPrefix(int nsIndex)
		{
			if (nsIndex == 0)
				return "";
			if (!namespaces.Keys.Contains(nsIndex))
				throw new ArgumentOutOfRangeException("nsIndex");
			return namespaces[nsIndex].Substring(1, namespaces[nsIndex].IndexOf('|', 1) - 1) + ':';
		}

		/// <summary>Gets canonical default English prefix for specified namespace and colon.
		/// If default prefix is not found the main local prefix is returned.</summary>
		/// <param name="nsIndex">Index of namespace to get prefix for.</param>
		/// <returns>Returns the prefix with colon, e.g., "Category:".</returns>
		public string GetEnglishNsPrefix(int nsIndex)
		{
			if (nsIndex == 0)
				return "";
			if (!namespaces.Keys.Contains(nsIndex))
				throw new ArgumentOutOfRangeException("nsIndex");
			int secondDelimPos = namespaces[nsIndex].IndexOf('|', 1);
			int thirdDelimPos = namespaces[nsIndex].IndexOf('|', secondDelimPos + 1);
			if (thirdDelimPos == -1)
				return namespaces[nsIndex].Substring(1, secondDelimPos - 1) + ':';
			else
				return namespaces[nsIndex].Substring(secondDelimPos + 1,
					thirdDelimPos - secondDelimPos - 1) + ':';
		}

		/// <summary>Gets all names and aliases for specified namespace delimited by '|' character
		/// and escaped for use within Regex patterns.</summary>
		/// <param name="nsIndex">Index of namespace to get prefixes for.</param>
		/// <returns>Returns prefixes string, e.g. "Category|Kategorie".</returns>
		public string GetNsPrefixes(int nsIndex)
		{
			if (!namespaces.Keys.Contains(nsIndex))
				throw new ArgumentOutOfRangeException("nsIndex");
			string str = namespaces[nsIndex].Substring(1, namespaces[nsIndex].Length - 2);
			str = str.Replace('|', '%');    // '%' is not escaped
			str = Regex.Escape(str);    // escapes only \*+?|{[()^$.# and whitespaces
			str = str.Replace('%', '|');    // return back '|' delimeter
			return str;
		}

		/// <summary>Identifies the namespace of the page.</summary>
		/// <param name="pageTitle">Page title to identify the namespace of.</param>
		/// <returns>Returns the integer key of the namespace.</returns>
		public int GetNamespace(string pageTitle)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			int colonPos = pageTitle.IndexOf(':');
			if (colonPos == -1 || colonPos == 0)
				return 0;
			string pageNS = '|' + pageTitle.Substring(0, colonPos) + '|';
			foreach (KeyValuePair<int, string> ns in namespaces) {
				if (ns.Value.Contains(pageNS))
					return ns.Key;
			}
			return 0;
		}

		/// <summary>Removes the namespace prefix from page title.</summary>
		/// <param name="pageTitle">Page title to remove prefix from.</param>
		/// <param name="nsIndex">Integer key of namespace to remove. If this parameter is 0
		/// any found namespace prefix is removed.</param>
		/// <returns>Page title without prefix.</returns>
		public string RemoveNsPrefix(string pageTitle, int nsIndex)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			if (!namespaces.Keys.Contains(nsIndex))
				throw new ArgumentOutOfRangeException("nsIndex");
			if (pageTitle[0] == ':')
				pageTitle = pageTitle.TrimStart(new char[] { ':' });
			int colonPos = pageTitle.IndexOf(':');
			if (colonPos == -1)
				return pageTitle;
			string pagePrefixPattern = '|' + pageTitle.Substring(0, colonPos) + '|';
			if (nsIndex != 0) {
				if (namespaces[nsIndex].Contains(pagePrefixPattern))
					return pageTitle.Substring(colonPos + 1);
			}
			else {
				foreach (KeyValuePair<int, string> ns in namespaces) {
					if (ns.Value.Contains(pagePrefixPattern))
						return pageTitle.Substring(colonPos + 1);
				}
			}
			return pageTitle;
		}

		/// <summary>Function changes default English namespace prefixes and local namespace aliases
		/// to canonical local prefixes (e.g. for German wiki-sites it changes "Category:..."
		/// to "Kategorie:...").</summary>
		/// <param name="pageTitle">Page title to correct prefix in.</param>
		/// <returns>Page title with corrected prefix.</returns>
		public string CorrectNsPrefix(string pageTitle)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			if (pageTitle[0] == ':')
				pageTitle = pageTitle.TrimStart(new char[] { ':' });
			int ns = GetNamespace(pageTitle);
			if (ns == 0)
				return pageTitle;
			return GetNsPrefix(ns) + RemoveNsPrefix(pageTitle, ns);
		}

		/// <summary>Shows names and integer keys of local and default namespaces and namespace
		/// aliases.</summary>
		public void ShowNamespaces()
		{
			foreach (KeyValuePair<int, string> ns in namespaces) {
				Console.WriteLine(ns.Key.ToString() + '\t' + ns.Value.Replace("|", Bot.nl + '\t'));
			}
		}
	}



	/// <summary>Class defines wiki page object.</summary>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	[Serializable]
	public class Page
	{
		/// <summary>Page's title, including namespace prefix.</summary>
		public string title;
		/// <summary>Page's text.</summary>
		public string text;
		/// <summary>Site, on which this page is located.</summary>
		public Site site;
		/// <summary>Page's ID in MediaWiki database.</summary>
		public string pageId;
		/// <summary>Username or IP-address of last page contributor.</summary>
		public string lastUser;
		/// <summary>Last contributor's ID in MediaWiki database.</summary>
		public string lastUserId;
		/// <summary>Page revision ID in the MediaWiki database.</summary>
		public string revision;
		/// <summary>True, if last edit was minor edit.</summary>
		public bool lastMinorEdit;
		/// <summary>Number of bytes modified during last edit.</summary>
		public int lastBytesModified;
		/// <summary>Last edit comment.</summary>
		public string comment;
		/// <summary>Date and time of last edit expressed in UTC (Coordinated Universal Time).
		/// Call "timestamp.ToLocalTime()" to convert to local time if it is necessary.</summary>
		public DateTime timestamp;
		/// <summary>Time of last page load (UTC). Used to detect edit conflicts.</summary>
		public DateTime lastLoadTime;
		/// <summary>True, if this page is in bot account's watchlist.</summary>
		public bool watched;

		/// <summary>This constructor creates Page object with specified title and specified
		/// Site object. This is preferable constructor. Basic title normalization occurs during
		/// construction.
		/// When constructed, new Page object doesn't contain text, use <see cref="Page.Load()"/>
		/// method to get text and metadata from live wiki.</summary>
		/// <param name="site">Site object, it must be constructed beforehand.</param>
		/// <param name="title">Page title as string.</param>
		/// <returns>Returns Page object.</returns>
		public Page(Site site, string title)
		{
			if (string.IsNullOrEmpty(title))
				throw new ArgumentNullException("title");
			if (title[0] == ':')
				title = title.TrimStart(new char[] { ':' });
			if (title.Contains('_'))
				title = title.Replace('_', ' ');

			this.site = site;
			this.title = title;

			/* // RESERVED, may interfere user intentions
			int ns = GetNamespace();
			RemoveNsPrefix();
			if (site.capitalization == "first-letter")
				title = Bot.Capitalize(title);
			title = site.namespaces[ns] + title;
			*/
		}

		/// <summary>This constructor creates empty Page object with specified Site object,
		/// but without title. Avoid using this constructor needlessly.</summary>
		/// <param name="site">Site object, it must be constructed beforehand.</param>
		/// <returns>Returns Page object.</returns>
		public Page(Site site)
		{
			this.site = site;
		}

		/// <summary>This constructor creates Page object with specified title using most recently
		/// created Site object.</summary>
		/// <param name="title">Page title as string.</param>
		/// <returns>Returns Page object.</returns>
		public Page(string title)
			: this(Bot.GetMostRecentSiteObject(), title) {}

		/// <summary>This constructor creates Page object with specified page's numeric revision ID
		/// (also called "oldid"). Page title is retrieved automatically
		/// in this constructor.</summary>
		/// <param name="site">Site object, it must be constructed beforehand.</param>
		/// <param name="revisionId">Page's numeric revision ID (also called "oldid").</param>
		/// <returns>Returns Page object.</returns>
		public Page(Site site, Int64 revisionId)
		{
			if (revisionId <= 0)
				throw new ArgumentOutOfRangeException("revisionID",
					Bot.Msg("Revision ID must be positive."));
			this.site = site;
			revision = revisionId.ToString();
			GetTitle();
		}

		/// <summary>This constructor creates Page object with specified page's numeric revision ID
		/// (also called "oldid") using most recently created Site object.</summary>
		/// <param name="revisionId">Page's numeric revision ID (also called "oldid").</param>
		/// <returns>Returns Page object.</returns>
		public Page(Int64 revisionId)
		{
			if (revisionId <= 0)
				throw new ArgumentOutOfRangeException("revisionID",
					Bot.Msg("Revision ID must be positive."));
			this.site = Bot.GetMostRecentSiteObject();
			revision = revisionId.ToString();
			GetTitle();
		}

		/// <summary>This constructor creates Page object without title using most recently
		/// created Site object.</summary>
		/// <returns>Returns Page object.</returns>
		public Page()
		{
			this.site = Bot.GetMostRecentSiteObject();
		}

		/// <summary>Loads page text from live wiki site via raw web interface.
		/// If Page.revision is specified, the function gets that specified
		/// revision. If the page doesn't exist it's text will be empty (""), no exception
		/// is thrown. This function is very fast, but it should be used only when
		/// metadata is not needed and no page modification is required.
		/// In other cases <see cref="Page.Load()"/> function should be used.</summary>
		public void LoadTextOnly()
		{
			if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(revision))
				throw new WikiBotException(Bot.Msg("No title is specified for page to load."));

			string res = site.indexPath + "?title=" + Bot.UrlEncode(title) +
				(string.IsNullOrEmpty(revision) ? "" : "&oldid=" + revision) +
				"&redirect=no&action=raw&ctype=text/plain&dontcountme=s";
			try {
				text = site.GetWebPage(res);
			}
			catch (WebException e) {
				string message = e.Message;
				if (message.Contains(": (404) ")) {    // Not Found
					Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);
					text = "";
					return;
				}
				else
					throw;
			}
			lastLoadTime = DateTime.UtcNow;
			Console.WriteLine(Bot.Msg("Page \"{0}\" loaded successfully."), title);
		}

		/// <summary>Loads page text and metadata (last revision's ID, timestamp, comment, author,
		/// minor edit mark) from wiki site. If the page doesn't exist
		/// it's text will be empty (""), no exception is thrown.</summary>
		public void Load()
		{
			LoadWithMetadata();
		}

		/// <summary>Loads page text and metadata (last revision's ID, timestamp, comment, author,
		/// minor edit mark). Now this function is synonym for <see cref="Page.Load()"/>.
		/// If the page doesn't exist it's text will be empty (""), no exception is thrown.
		/// </summary>
		/// <exclude/>
		public void LoadWithMetadata()
		{
			if (site.useApi) {
				if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(revision))
					throw new WikiBotException(Bot.Msg("No title is specified for page to load."));

				string res = site.apiPath + "?action=query&prop=revisions&format=xml" +
					"&rvprop=content|user|userid|comment|ids|flags|timestamp";
				if (!string.IsNullOrEmpty(revision))
					res += "&revids=" + revision;
				else if (!string.IsNullOrEmpty(title))
					res += "&titles=" + Bot.UrlEncode(title);
				string src = site.GetWebPage(res);
				lastLoadTime = DateTime.UtcNow;

				XElement pageXml =
					XElement.Parse(src).Element("query").Element("pages").Element("page");
				if (pageXml.Attribute("missing") != null) {
					Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);
					text = "";
					return;
				}
				pageId = pageXml.Attribute("pageid").Value;
				title = pageXml.Attribute("title").Value;

				XElement revXml = pageXml.Element("revisions").Element("rev");
				revision = revXml.Attribute("revid").Value;
				timestamp = DateTime.Parse(revXml.Attribute("timestamp").Value);
				timestamp = timestamp.ToUniversalTime();
				lastUser = revXml.Attribute("user").Value;
				lastUserId = revXml.Attribute("userid").Value;
				lastMinorEdit = revXml.Attribute("minor") != null;
				comment = revXml.Attribute("comment").Value;
				text = revXml.Value;
			}
			else {
				if (string.IsNullOrEmpty(title))
					throw new WikiBotException(Bot.Msg("No title is specified for page to load."));
				string res = site.indexPath + "?title=Special:Export/" +
					Bot.UrlEncode(title) + "&action=submit";
				string src = site.GetWebPage(res);
				lastLoadTime = DateTime.UtcNow;
				ParsePageXml(src);
			}
			Console.WriteLine(Bot.Msg("Page \"{0}\" loaded successfully."), title);
		}

		/// <summary>This internal function parses MediaWiki XML export data using XmlDocument
		/// to get page text and metadata.</summary>
		/// <param name="xmlSrc">XML export source code.</param>
		/// <exclude/>
		public void ParsePageXml(string xmlSrc)
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(xmlSrc);
			if (doc.GetElementsByTagName("page").Count == 0) {
				Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);
				text = "";
				return;
			}
			text = doc.GetElementsByTagName("text")[0].InnerText;
			pageId = doc.GetElementsByTagName("id")[0].InnerText;
			if (doc.GetElementsByTagName("username").Count != 0) {
				lastUser = doc.GetElementsByTagName("username")[0].InnerText;
				lastUserId = doc.GetElementsByTagName("id")[2].InnerText;
			}
			else if(doc.GetElementsByTagName("ip").Count != 0)
				lastUser = doc.GetElementsByTagName("ip")[0].InnerText;
			else
				lastUser = "(n/a)";
			revision = doc.GetElementsByTagName("id")[1].InnerText;
			if (doc.GetElementsByTagName("comment").Count != 0)
				comment = doc.GetElementsByTagName("comment")[0].InnerText;
			timestamp = DateTime.Parse(doc.GetElementsByTagName("timestamp")[0].InnerText);
			timestamp = timestamp.ToUniversalTime();
			lastMinorEdit = (doc.GetElementsByTagName("minor").Count != 0) ? true : false;
			if (string.IsNullOrEmpty(title))
				title = doc.GetElementsByTagName("title")[0].InnerText;
			else
				Console.WriteLine(Bot.Msg("Page \"{0}\" loaded successfully."), title);
		}

		/// <summary>Loads page text from the specified UTF8-encoded file.</summary>
		/// <param name="filePathName">Path and name of the file.</param>
		public void LoadFromFile(string filePathName)
		{
			StreamReader strmReader = new StreamReader(filePathName);
			text = strmReader.ReadToEnd();
			strmReader.Close();
			Console.WriteLine(
				Bot.Msg("Text for page \"{0}\" has been loaded from \"{1}\" file."),
				title, filePathName);
		}

		/// <summary>Retrieves the title for this Page object using page's numeric revision ID
		/// (also called "oldid"), stored in "revision" object's property. Make sure that
		/// "revision" property is set before calling this function. Use this function
		/// when working with old revisions to detect if the page was renamed at some
		/// point.</summary>
		public void GetTitle()
		{
			if (string.IsNullOrEmpty(revision))
				throw new WikiBotException(
					Bot.Msg("No revision ID is specified for page to get title for."));
			if (site.useApi) {
				string src = site.GetWebPage(site.apiPath +
					"?action=query&prop=info&format=xml&revids=" + revision);
				var infoXml = XElement.Parse(src).Element("query");
				if (infoXml.Element("badrevids") != null)
					throw new WikiBotException(string.Format(
						"No page revision with ID \"{0}\" was found.", revision));
				title = infoXml.Descendants("page").FirstOrDefault().Attribute("title").Value;
			}
			else {
				string src = site.GetWebPage(site.indexPath + "?oldid=" + revision);
				Match m = Regex.Match(src, "<h1 [^>]*=\"firstHeading\"[^>]*>" +
					"(<span[^>]*>)?(?<title>.+?)(</span>)?</h1>");
				if (string.IsNullOrEmpty(m.Groups["title"].Value))
					throw new WikiBotException(string.Format(
						"No page revision with ID \"{0}\" was found.", revision));
				title =	m.Groups["title"].Value;
			}
		}

		/// <summary>Gets security tokens which are required by MediaWiki to perform page
		/// modifications.</summary>
		/// <param name="action">Type of action, that security token is required for.</param>
		/// <returns>Returns Dictionary object.</returns>
		public Dictionary<string, string> GetSecurityTokens(string action)
		{
			if (string.IsNullOrEmpty(action))
				throw new ArgumentNullException("action");
			if (string.IsNullOrEmpty(title))
				throw new ArgumentNullException("title");

			string src = site.GetWebPage(site.apiPath + "?action=query&prop=info&intoken=" +
				action + "&inprop=protection|watched|watchers|notificationtimestamp|readable" +
				"&format=xml&titles=" + Bot.UrlEncode(title));
			var tokensXml = XElement.Parse(src).Element("query").Element("pages");
			var tokens = (
				from attr in tokensXml.Element("page").Attributes()
				select new {
					attrName = attr.Name.ToString(),
					attrValue = attr.Value
				}
			).ToDictionary(s => s.attrName, s => s.attrValue);

			return tokens;
		}

		/// <summary>Saves contents of <see cref="Page.text"/> to live wiki site. Uses
		/// <see cref="Site.defaultEditComment"/> and <see cref="Site.minorEditByDefault"/>
		/// (true by default).</summary>
		/// <exception cref="InsufficientRightsException">Insufficient rights
		/// to edit this page.</exception>
		/// <exception cref="BotDisallowedException">Bot operation on this page
		/// is disallowed.</exception>
		/// <exception cref="EditConflictException">Edit conflict
		/// detected.</exception>
		/// <exception cref="WikiBotException">Any wiki-related error.</exception>
		public void Save()
		{
			Save(text, site.defaultEditComment, site.minorEditByDefault, false);
		}

		/// <summary>Saves specified text in page on live wiki. Uses
		/// <see cref="Site.defaultEditComment"/> and <see cref="Site.minorEditByDefault"/>
		/// (true by default).</summary>
		/// <param name="newText">New text for this page.</param>
		/// <exception cref="InsufficientRightsException">Insufficient rights to edit
		/// this page.</exception>
		/// <exception cref="BotDisallowedException">Bot operation on this page
		/// is disallowed.</exception>
		/// <exception cref="EditConflictException">Edit conflict was detected.</exception>
		/// <exception cref="WikiBotException">Wiki-related error.</exception>
		public void Save(string newText)
		{
			Save(newText, site.defaultEditComment, site.minorEditByDefault, false);
		}

		/// <summary>Saves <see cref="Page.text"/> contents to live wiki site.</summary>
		/// <param name="comment">Your edit comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
		/// <exception cref="InsufficientRightsException">Insufficient rights to edit
		/// this page.</exception>
		/// <exception cref="BotDisallowedException">Bot operation on this page
		/// is disallowed.</exception>
		/// <exception cref="EditConflictException">Edit conflict was detected.</exception>
		/// <exception cref="WikiBotException">Wiki-related error.</exception>
		public void Save(string comment, bool isMinorEdit)
		{
			Save(text, comment, isMinorEdit, false);
		}

		/// <summary>Saves specified text on page on live wiki.</summary>
		/// <param name="newText">New text for this page.</param>
		/// <param name="comment">Your edit comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
		/// <exception cref="InsufficientRightsException">Insufficient rights to edit
		/// this page.</exception>
		/// <exception cref="BotDisallowedException">Bot operation on this page
		/// is disallowed.</exception>
		/// <exception cref="EditConflictException">Edit conflict was detected.</exception>
		/// <exception cref="WikiBotException">Wiki-related error.</exception>
		public void Save(string newText, string comment, bool isMinorEdit)
		{
			Save(newText, comment, isMinorEdit, false);
		}

		/// <summary>Saves specified text on page on live wiki.</summary>
		/// <param name="newText">New text for this page.</param>
		/// <param name="comment">Your edit comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
		/// <param name="reviewVersion">If true, the page revision after saving is marked
		/// reviewed. Bot's account must have sufficient rights to mark revision reviewed.
		/// Not every wiki site does have this option.</param>
		/// <exception cref="InsufficientRightsException">Insufficient rights to edit
		/// this page.</exception>
		/// <exception cref="BotDisallowedException">Bot operation on this page
		/// is disallowed.</exception>
		/// <exception cref="EditConflictException">Edit conflict was detected.</exception>
		/// <exception cref="WikiBotException">Wiki-related error.</exception>
		public void Save(string newText, string comment, bool isMinorEdit, bool reviewVersion)
		{
			if (string.IsNullOrEmpty(newText) && string.IsNullOrEmpty(text))
				throw new ArgumentNullException("newText",
					Bot.Msg("No text is specified for page to save."));
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg(
					"No title is specified for page to save text to."));
			if (text != null && Regex.IsMatch(text, @"(?is)\{\{(nobots|bots\|(allow=none|" +
				@"deny=(?!none)[^\}]*(" + site.userName + @"|all)|optout=all))\}\}"))
					throw new BotDisallowedException(string.Format(Bot.Msg(
						"Bot action on \"{0}\" page is prohibited " +
						"by \"nobots\" or \"bots|allow=none\" template."), title));
			
			if (site.forceSaveDelay > 0) {
				int secondsPassed = (int) (DateTime.Now - site.lastWriteTime).TotalSeconds;
				if (site.forceSaveDelay > secondsPassed)
					Bot.Wait((site.forceSaveDelay - secondsPassed));
			}

			if (site.useApi && !reviewVersion)
			{
				// Get security token for editing
				string editToken = "";
				if (site.tokens != null && site.tokens.ContainsKey("csrftoken"))
					editToken = site.tokens["csrftoken"];
				else {
					var tokens = GetSecurityTokens("edit");
					if (!tokens.ContainsKey("edittoken") || tokens["edittoken"] == "")
						throw new InsufficientRightsException(string.Format(Bot.Msg(
							"Insufficient rights to edit page \"{0}\"."), title));
					editToken = tokens["edittoken"];
				}

				string postData = string.Format("action=edit&title={0}&summary={1}&text={2}" +
					"&watchlist={3}{4}{5}{6}&bot=1&format=xml&token={7}",
					Bot.UrlEncode(title),
					Bot.UrlEncode(comment),
					Bot.UrlEncode(newText),
					"nochange",
					isMinorEdit ? "&minor=1" : "&notminor=1",
					timestamp != DateTime.MinValue ? "&basetimestamp=" +
						timestamp.ToString("s") + "Z" : "",
					lastLoadTime != DateTime.MinValue ? "&starttimestamp=" +
						lastLoadTime.AddSeconds(site.timeOffsetSeconds).ToString("s") + "Z" : "",
					Bot.UrlEncode(editToken));
				if (Bot.askConfirm) {
					Console.Write("\n\n" +
						Bot.Msg("The following text is going to be saved on page \"{0}\":"), title);
					Console.Write("\n\n" + text + "\n\n");
					if (!Bot.UserConfirms())
						return;
				}
				string respStr = site.PostDataAndGetResult(site.apiPath, postData);

				XElement respXml = XElement.Parse(respStr);
				if (respXml.Element("error") != null) {
					string error = respXml.Element("error").Attribute("code").Value;
					string desc = respXml.Element("error").Attribute("info").Value;
					if (error == "editconflict")
						throw new EditConflictException(string.Format(Bot.Msg(
							"Edit conflict occurred while trying to savе page \"{0}\"."), title));
					else if (error == "noedit")
						throw new InsufficientRightsException(string.Format(Bot.Msg(
							"Insufficient rights to edit page \"{0}\"."), title));
					else
						throw new WikiBotException(desc);
				}
				else if (respXml.Element("edit") != null
					&& respXml.Element("edit").Element("captcha") != null) {
						throw new BotDisallowedException(string.Format(Bot.Msg(
							"Error occurred when saving page \"{0}\": " +
							"Bot operation is not allowed for this account at \"{1}\" site."),
							title, site.address));
				}
			}
			else
			{
				string editToken, lastEditTime, lastEditRevId, editStartTime;		
				string editPageSrc = site.GetWebPage(site.indexPath + "?title=" +
					Bot.UrlEncode(title) + "&action=edit");
				Match m;
				m = site.regexes["editToken"].Match(editPageSrc);
				editToken = m.Success ? m.Groups[1].Value : "";
				m = site.regexes["editTime"].Match(editPageSrc);
				lastEditTime = m.Success ? m.Groups[1].Value : "";
				m = site.regexes["startTime"].Match(editPageSrc);
				if (lastLoadTime == DateTime.MinValue && m.Success)
					editStartTime = m.Groups[1].Value;
				else
					editStartTime = lastLoadTime.AddSeconds(site.timeOffsetSeconds)
						.ToString("yyyyMMddHHmmss");
				m = site.regexes["baseRevId"].Match(editPageSrc);
				if (string.IsNullOrEmpty(revision) && m.Success)
					lastEditRevId = m.Groups[1].Value;
				else
					lastEditRevId = revision;

				// See if page is watched or not
				if (site.watchList == null) {
					site.watchList = new PageList(site);
					site.watchList.FillFromWatchList();
				}
				watched = site.watchList.Contains(this);

				string postData = string.Format(
					"wpSection=&wpStarttime={0}&wpEdittime={1}{2}&wpScrolltop=&wpTextbox1={3}" +
					"&wpSummary={4}&wpSave=Save%20Page&wpEditToken={5}{6}{7}{8}",
					editStartTime,
					lastEditTime,
					string.IsNullOrEmpty(lastEditRevId) ? "&baseRevId=" + lastEditRevId : "",
					Bot.UrlEncode(newText),
					Bot.UrlEncode(comment),
					Bot.UrlEncode(editToken),
					watched ? "&wpWatchthis=1" : "",
					isMinorEdit ? "&wpMinoredit=1" : "",
					reviewVersion ? "&wpReviewEdit=1" : "");
				if (Bot.askConfirm) {
					Console.Write("\n\n" +
						Bot.Msg("The following text is going to be saved on page \"{0}\":"), title);
					Console.Write("\n\n" + text + "\n\n");
					if (!Bot.UserConfirms())
						return;
				}
				string respStr = site.PostDataAndGetResult(site.indexPath + "?title=" +
					Bot.UrlEncode(title) + "&action=submit", postData);
				if (respStr.Contains(" name=\"wpTextbox2\""))
					throw new EditConflictException(string.Format(Bot.Msg(
						"Edit conflict occurred while trying to savе page \"{0}\"."), title));
				if (respStr.Contains("<div class=\"permissions-errors\">"))
					throw new InsufficientRightsException(string.Format(Bot.Msg(
						"Insufficient rights to edit page \"{0}\"."), title));
				if (respStr.Contains("input name=\"wpCaptchaWord\" id=\"wpCaptchaWord\""))
					throw new BotDisallowedException(string.Format(Bot.Msg(
						"Error occurred when saving page \"{0}\": " +
						"Bot operation is not allowed for this account at \"{1}\" site."),
						title, site.address));
			}

			Console.WriteLine(Bot.Msg("Page \"{0}\" saved successfully."), title);
			site.lastWriteTime = DateTime.UtcNow;
			lastLoadTime = DateTime.UtcNow;
			timestamp = DateTime.MinValue;
			text = newText;
		}

		/// <summary>Undoes the last edit, so page text reverts to previous contents.
		/// The function doesn't affect other actions like renaming.</summary>
		/// <param name="comment">Revert comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (pass true for minor edit).</param>
		public void Revert(string comment, bool isMinorEdit)
		{
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for page to revert."));
			PageList pl = new PageList(site);
			pl.FillFromPageHistory(title, 2);
			if (pl.Count() != 2) {
				Console.Error.WriteLine(Bot.Msg("Can't revert page \"{0}\"."), title);
				return;
			}
			pl[1].Load();
			Save(pl[1].text, comment, isMinorEdit);
			Console.WriteLine(Bot.Msg("Page \"{0}\" has been reverted."), title);
		}

		/// <summary>Undoes all last edits made by last contributor.
		/// The function doesn't affect other operations
		/// like renaming or protecting.</summary>
		/// <param name="comment">Comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (pass true for minor edit).</param>
		/// <returns>Returns true if last edits were undone.</returns>
		public bool UndoLastEdits(string comment, bool isMinorEdit)
		{
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for page to revert."));
			PageList pl = new PageList(site);
			string lastEditor = "";
			for (int i = 50; i <= 5000; i *= 10) {
				pl.FillFromPageHistory(title, i);
				lastEditor = pl[0].lastUser;
				foreach (Page p in pl)
					if (p.lastUser != lastEditor) {
						p.Load();
						Save(p.text, comment, isMinorEdit);
						Console.WriteLine(
							Bot.Msg("Last edits of page \"{0}\" by user {1} have been undone."),
							title, lastEditor);
						return true;
					}
				if (pl.pages.Count < i)
					break;
				pl.Clear();
			}
			Console.Error.WriteLine(Bot.Msg("Can't undo last edits of page \"{0}\" by user {1}."),
				title, lastEditor);
			return false;
		}

		/// <summary>Protects or unprotects the page, so only authorized group of users can edit or
		/// rename it. Changing page protection mode requires administrator (sysop)
		/// rights.</summary>
		/// <param name="editMode">Protection mode for editing this page (0 = everyone allowed
		/// to edit, 1 = only registered users are allowed, 2 = only administrators are allowed
		/// to edit).</param>
		/// <param name="renameMode">Protection mode for renaming this page (0 = everyone allowed to
		/// rename, 1 = only registered users are allowed, 2 = only administrators
		/// are allowed).</param>
		/// <param name="cascadeMode">In cascading mode all the pages, included into this page
		/// (e.g., templates or images) are also automatically protected.</param>
		/// <param name="expiryDate">Date and time, expressed in UTC, when protection expires
		/// and page becomes unprotected. Use DateTime.ToUniversalTime() method to convert local
		/// time to UTC, if necessary. Pass DateTime.MinValue to make protection indefinite.</param>
		/// <param name="reason">Reason for protecting this page.</param>
		/// <example><code>
		/// page.Protect(2, 2, false, DateTime.Now.AddDays(20), "persistent vandalism");
		/// </code></example>
		public void Protect(int editMode, int renameMode, bool cascadeMode,
			DateTime expiryDate, string reason)
		{
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for page to protect."));
			string errorMsg =
				Bot.Msg("Only values 0, 1 and 2 are accepted. Please consult documentation.");
			if (editMode > 2 || editMode < 0)
				throw new ArgumentOutOfRangeException("editMode", errorMsg);
			if (renameMode > 2 || renameMode < 0)
				throw new ArgumentOutOfRangeException("renameMode", errorMsg);
			if (expiryDate != DateTime.MinValue	&& expiryDate < DateTime.Now)
				throw new ArgumentOutOfRangeException("expiryDate",
					Bot.Msg("Protection expiry date must be later than now."));

			if (site.useApi) {

				string token = "";
				if (site.tokens != null && site.tokens.ContainsKey("csrftoken"))
					token = site.tokens["csrftoken"];
				else {
					var tokens = GetSecurityTokens("protect");
					if (tokens.ContainsKey("missing"))
						throw new WikiBotException(
							string.Format(Bot.Msg("Page \"{0}\" doesn't exist."), title));
					if (!tokens.ContainsKey("protecttoken") || tokens["protecttoken"] == "") {
						Console.Error.WriteLine(
							Bot.Msg("Unable to change protection mode for page \"{0}\"."), title);
						return;
					}
					token = tokens["protecttoken"];
				}

				string date = Regex.Replace(expiryDate.ToString("u"), "\\D", "");
				string postData = string.Format("token={0}&protections=edit={1}|move={2}" +
					"&cascade={3}&expiry={4}|{5}&reason={6}&watchlist=nochange",
					Bot.UrlEncode(token),
					(editMode == 2 ? "sysop" : editMode == 1 ? "autoconfirmed" : ""),
					(renameMode == 2 ? "sysop" : renameMode == 1 ? "autoconfirmed" : ""),
					(cascadeMode == true ? "1" : ""),
					(expiryDate == DateTime.MinValue ? "" : date),
					(expiryDate == DateTime.MinValue ? "" : date),
					Bot.UrlEncode(reason)
				);

				string respStr = site.PostDataAndGetResult(site.apiPath + "?action=protect" +
					"&title=" + Bot.UrlEncode(title) + "&format=xml", postData);
				if (respStr.Contains("<error"))
					throw new WikiBotException(
						string.Format(Bot.Msg("Failed to delete page \"{0}\"."), title));
			}
			else {
				string respStr = site.GetWebPage(site.indexPath + "?title=" +
					Bot.UrlEncode(title) + "&action=protect");
				Match m = site.regexes["editToken"].Match(respStr);
				string securityToken = string.IsNullOrEmpty(m.Groups[1].Value)
					? m.Groups[2].Value : m.Groups[1].Value;
				if (string.IsNullOrEmpty(securityToken)) {
					Console.Error.WriteLine(
						Bot.Msg("Unable to change protection mode for page \"{0}\"."), title);
					return;
				}

				if (site.watchList == null) {
					site.watchList = new PageList(site);
					site.watchList.FillFromWatchList();
				}
				watched = site.watchList.Contains(this);

				string postData = string.Format(
					"mwProtect-level-edit={0}&mwProtect-level-move={1}" +
					"&mwProtect-reason={2}&wpEditToken={3}&mwProtect-expiry={4}{5}{6}",
					(editMode == 2 ? "sysop" : editMode == 1 ? "autoconfirmed" : ""),
					(renameMode == 2 ? "sysop" : renameMode == 1 ? "autoconfirmed" : ""),
					Bot.UrlEncode(reason),
					Bot.UrlEncode(securityToken),
					expiryDate == DateTime.MinValue ? "" : expiryDate.ToString("u"),
						// ToString("u") is like "2010-06-15 20:45:30Z"
					cascadeMode == true ? "&mwProtect-cascade=1" : "",
					watched ? "&mwProtectWatch=1" : "");
				respStr = site.PostDataAndGetResult(site.indexPath +
					"?title=" + Bot.UrlEncode(title) + "&action=protect", postData);

				Regex successMsg = new Regex(
					"<h1[^>]*>(<span[^>]*>)?\\s*" + HttpUtility.HtmlEncode(title) + "\\s*<");
				if (!successMsg.IsMatch(respStr)) {
					throw new WikiBotException(string.Format(
						Bot.Msg("Unable to change protection mode for page \"{0}\"."), title));
				}
			}

			Console.WriteLine(
				Bot.Msg("Protection mode for page \"{0}\" has been changed successfully."), title);
		}

		/// <summary>Adds this page to bot account's watchlist.</summary>
		public void Watch()
		{
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for page to watch."));

			if (site.useApi) {
				string res = site.apiPath + "?format=xml&action=query&meta=tokens&type=watch" +
					"&titles=" + Bot.UrlEncode(title);
				string respStr = site.GetWebPage(res);
				string securityToken = "";
				string titleFallback = "";
				try {
					securityToken = XElement.Parse(respStr).Element("query")
						.Element("tokens").Attribute("watchtoken").Value.ToString();
				}
				catch {    // FALLBACK for older version
					res = site.apiPath + "?format=xml&action=query&prop=info&intoken=watch" +
						"&titles=" + Bot.UrlEncode(title);
					respStr = site.GetWebPage(res);
					securityToken = XElement.Parse(respStr).Element("query").Element("pages")
						.Element("page").Attribute("watchtoken").Value.ToString();
					titleFallback = "&title=" + Bot.UrlEncode(title);
				}
				string postData = string.Format("titles={0}{1}&action=watch&token={2}&format=xml",
					Bot.UrlEncode(title), titleFallback,
					Bot.UrlEncode(securityToken));
				respStr = site.PostDataAndGetResult(site.apiPath, postData);
			}
			else {
				string res = site.indexPath + "?action=watch&title=" +
					Bot.UrlEncode(title);
				string respStr = site.GetWebPage(res);
				string securityToken = "";
				Match m = site.regexes["editToken"].Match(respStr);
				if (m.Success)
				{
					securityToken = string.IsNullOrEmpty(m.Groups[1].Value)
						? m.Groups[2].Value : m.Groups[1].Value;
				}
				else
				{
					Console.Error.WriteLine(Bot.Msg("Can't add page \"{0}\" to watchlist."),
						title);
					return;
				}
				string postData = string.Format("title={0}&action=watch&wpEditToken={1}",
					Bot.UrlEncode(title), Bot.UrlEncode(securityToken));
				respStr = site.PostDataAndGetResult(site.indexPath +
					"?title=" + Bot.UrlEncode(title), postData);
			}

			watched = true;
			if (site.watchList == null)
				site.watchList.FillFromWatchList();
			if (!site.watchList.Contains(this))
				site.watchList.Add(this);
			Console.WriteLine(Bot.Msg("Page \"{0}\" added to watchlist."), title);
		}

		/// <summary>Removes page from bot account's watchlist.</summary>
		public void Unwatch()
		{
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for page to unwatch."));

			if (site.useApi) {
				string res = site.apiPath + "?format=xml&action=query&meta=tokens&type=watch" +
					"&titles=" + Bot.UrlEncode(title);
				string respStr = site.GetWebPage(res);
				string securityToken = "";
				string titleFallback = "";
				try {
					securityToken = XElement.Parse(respStr).Element("query")
						.Element("tokens").Attribute("watchtoken").Value.ToString();
				}
				catch {    // FALLBACK for older version
					res = site.apiPath + "?format=xml&action=query&prop=info&intoken=watch" +
						"&titles=" + Bot.UrlEncode(title);
					respStr = site.GetWebPage(res);
					securityToken = XElement.Parse(respStr).Element("query").Element("pages")
						.Element("page").Attribute("watchtoken").Value.ToString();
					titleFallback = "&title=" + Bot.UrlEncode(title);
				}
				string postData = string.Format("titles={0}{1}&token={2}" +
					"&format=xml&action=watch&unwatch=1", Bot.UrlEncode(title),
					titleFallback, Bot.UrlEncode(securityToken));
				respStr = site.PostDataAndGetResult(site.apiPath, postData);
			}
			else {
				string res = site.indexPath + "?action=unwatch&title=" +
					Bot.UrlEncode(title);
				string respStr = site.GetWebPage(res);
				string securityToken = "";
				Match m = site.regexes["editToken"].Match(respStr);
				if (m.Success) {
					securityToken = string.IsNullOrEmpty(m.Groups[1].Value)
						? m.Groups[2].Value : m.Groups[1].Value;
				}
				else {
					Console.Error.WriteLine(Bot.Msg("Can't remove page \"{0}\" from watchlist."),
						title);
					return;
				}
				string postData = string.Format("title={0}&action=unwatch&wpEditToken={1}",
					Bot.UrlEncode(title), Bot.UrlEncode(securityToken));
				respStr = site.PostDataAndGetResult(site.indexPath +
					"?title=" + Bot.UrlEncode(title), postData);
			}

			watched = false;
			if (site.watchList != null && site.watchList.Contains(this))
				site.watchList.Remove(this.title);
			Console.WriteLine(Bot.Msg("Page \"{0}\" has been removed from watchlist."), title);
		}

		/// <summary>This function opens page text in Microsoft Word for editing.
		/// Just close Word after editing, and the revised text will appear back in
		/// <see cref="Page.text"/> variable.</summary>
		/// <remarks>Appropriate PIAs (Primary Interop Assemblies) for available MS Office
		/// version must be installed and referenced in order to use this function. Follow
		/// instructions in "Compile and Run.bat" file to reference PIAs properly in compilation
		/// command, and then recompile the framework. Redistributable PIAs can be downloaded from
		/// <see href="http://www.microsoft.com/en-us/download/search.aspx?q=Office%20PIA">
		/// Microsoft web site</see>.</remarks>
		public void ReviseInMsWord()
		{
		  #if MS_WORD_INTEROP
			if (string.IsNullOrEmpty(text))
				throw new WikiBotException(Bot.Msg("No text on page to revise in Microsoft Word."));
			Microsoft.Office.Interop.Word.Application app =
				new Microsoft.Office.Interop.Word.Application();
			app.Visible = true;
			object mv = System.Reflection.Missing.Value;
			object template = mv;
			object newTemplate = mv;
			object documentType = Microsoft.Office.Interop.Word.WdDocumentType.wdTypeDocument;
			object visible = true;
			Microsoft.Office.Interop.Word.Document doc =
				app.Documents.Add(ref template, ref newTemplate, ref documentType, ref visible);
			doc.Words.First.InsertBefore(text);
			text = null;
			Microsoft.Office.Interop.Word.DocumentEvents_Event docEvents =
				(Microsoft.Office.Interop.Word.DocumentEvents_Event) doc;
			docEvents.Close +=
				new Microsoft.Office.Interop.Word.DocumentEvents_CloseEventHandler(
					delegate { text = doc.Range(ref mv, ref mv).Text; doc.Saved = true; } );
			app.Activate();
			while (text == null);
			text = Regex.Replace(text, "\r(?!\n)", "\r\n");
			app = null;
			doc = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			Console.WriteLine(
				Bot.Msg("Text of \"{0}\" page has been revised in Microsoft Word."), title);
		  #else
			throw new WikiBotException(Bot.Msg("Page.ReviseInMSWord() function requires MS " +
				"Office PIAs to be installed and referenced. Please see remarks in function's " +
				"documentation in \"Documentation.chm\" file for additional instructions.\n"));
		  #endif
		}

		/// <summary>Uploads local image to wiki site. Function also works with non-image files.
		/// Note: uploaded image title (wiki page title) will be the same as title of this Page
		/// object, not the title of source file.</summary>
		/// <param name="filePathName">Path and name of local file.</param>
		/// <param name="description">File (image) description.</param>
		/// <param name="license">File license type (may be template title). Used only on
		/// some wiki sites. Pass empty string, if the wiki site doesn't require it.</param>
		/// <param name="copyStatus">File (image) copy status. Used only on some wiki sites. Pass
		/// empty string, if the wiki site doesn't require it.</param>
		/// <param name="source">File (image) source. Used only on some wiki sites. Pass
		/// empty string, if the wiki site doesn't require it.</param>
		public void UploadImage(string filePathName, string description,
			string license, string copyStatus, string source)
		{
			if (!File.Exists(filePathName))
				throw new ArgumentNullException("filePathName",
					string.Format(Bot.Msg("Image file \"{0}\" doesn't exist."), filePathName));
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for image to upload."));
			if (Path.GetFileNameWithoutExtension(filePathName).Length < 3)
				throw new WikiBotException(string.Format(Bot.Msg("Name of file \"{0}\" must " +
					"contain at least 3 characters (excluding extension) for successful upload."),
					filePathName));

			Console.WriteLine(Bot.Msg("Uploading image \"{0}\"..."), title);

			var tokens = GetSecurityTokens("edit");    // there is no more specific token type
			if (!tokens.ContainsKey("edittoken") || tokens["edittoken"] == "")
				throw new WikiBotException(
					string.Format(Bot.Msg("Error occurred when uploading image \"{0}\"."), title));

			string targetName = site.RemoveNsPrefix(title, 6);
			targetName = Bot.Capitalize(targetName);

			string res = site.indexPath + "?title=" +
				HttpUtility.HtmlEncode(site.GetNsPrefix(-1)) + "Upload";

			int retryDelaySeconds = 60;
			WebResponse webResp = null;
			for (int errorCounter = 0; true; errorCounter++) {
				HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(res);
				webReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
				webReq.UseDefaultCredentials = true;
				webReq.Method = "POST";
				string boundary = DateTime.Now.Ticks.ToString("x");
				webReq.ContentType = "multipart/form-data; boundary=" + boundary;
				webReq.UserAgent = Bot.botVer;
				webReq.CookieContainer = site.cookies;
				if (Bot.unsafeHttpHeaderParsingUsed == 0) {
					webReq.ProtocolVersion = HttpVersion.Version10;
					webReq.KeepAlive = false;
				}
				webReq.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(
					System.Net.Cache.HttpRequestCacheLevel.Refresh);
				StringBuilder sb = new StringBuilder();
				string paramHead = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"";
				sb.Append(paramHead + "maxlag\"\r\n\r\n" + site.maxLag + "\r\n");
				sb.Append(paramHead + "wpIgnoreWarning\"\r\n\r\n1\r\n");
				sb.Append(paramHead + "wpDestFile\"\r\n\r\n" + targetName + "\r\n");
				sb.Append(paramHead + "wpUploadAffirm\"\r\n\r\n1\r\n");
				sb.Append(paramHead + "wpWatchthis\"\r\n\r\n0\r\n");
				sb.Append(paramHead + "wpEditToken\"\r\n\r\n" + tokens["edittoken"] + "\r\n");
				sb.Append(paramHead + "wpUploadCopyStatus\"\r\n\r\n" + copyStatus + "\r\n");
				sb.Append(paramHead + "wpUploadSource\"\r\n\r\n" + source + "\r\n");
				sb.Append(paramHead + "wpUpload\"\r\n\r\n" + "upload bestand" + "\r\n");
				sb.Append(paramHead + "wpLicense\"\r\n\r\n" + license + "\r\n");
				sb.Append(paramHead + "wpUploadDescription\"\r\n\r\n" + description + "\r\n");
				sb.Append(paramHead + "wpUploadFile\"; filename=\"" +
					Bot.UrlEncode(Path.GetFileName(filePathName)) + "\"\r\n" +
					"Content-Type: application/octet-stream\r\n\r\n");
				byte[] postHeaderBytes = Encoding.UTF8.GetBytes(sb.ToString());
				byte[] fileBytes = File.ReadAllBytes(filePathName);
				byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
				webReq.ContentLength = postHeaderBytes.Length + fileBytes.Length +
					boundaryBytes.Length;
				Stream reqStream = webReq.GetRequestStream();
				reqStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);
				reqStream.Write(fileBytes, 0, fileBytes.Length);
				reqStream.Write(boundaryBytes, 0, boundaryBytes.Length);
				try {
					webResp = (HttpWebResponse)webReq.GetResponse();
					break;
				}
				catch (WebException e) {

					if (webResp == null)
						throw;

					if (e.Message.Contains("Section=ResponseStatusLine")) {   // Known Squid problem
						Bot.SwitchUnsafeHttpHeaderParsing(true);
						UploadImage(filePathName, description, license, copyStatus, source);
						return;
					}

					if (webResp.Headers["Retry-After"] != null) {    // Server is very busy
						if (errorCounter > site.retryTimes)
							throw;
						int seconds;
						Int32.TryParse(webResp.Headers["Retry-After"], out seconds);
						if (seconds > 0)
							retryDelaySeconds = seconds;
						Console.Error.WriteLine(e.Message);
						Console.Error.WriteLine(string.Format(Bot.Msg(
							"Retrying in {0} seconds..."), retryDelaySeconds));
						Bot.Wait(retryDelaySeconds);
					}
					else if (e.Status == WebExceptionStatus.ProtocolError) {
						int code = (int) ((HttpWebResponse)webResp).StatusCode;
						if (code == 500 || code == 502 || code == 503 || code == 504)
						{
							// Remote server problem
							if (errorCounter > site.retryTimes)
								throw;
							Console.Error.WriteLine(e.Message);
							Console.Error.WriteLine(string.Format(Bot.Msg(
								"Retrying in {0} seconds..."), retryDelaySeconds));
							Bot.Wait(retryDelaySeconds);

						}
						else
							throw;
					}
					else
						throw;
				}
			}
			StreamReader strmReader = new StreamReader(webResp.GetResponseStream());
			string respStr = strmReader.ReadToEnd();
			strmReader.Close();
			webResp.Close();
			if (!respStr.Contains(HttpUtility.HtmlEncode(targetName)))
				throw new WikiBotException(string.Format(
					Bot.Msg("Error occurred when uploading image \"{0}\"."), title));
			try {
				if (site.messages == null)
					site.LoadMediawikiMessages(true);
				string errorMessage = site.messages["uploaderror"];
				if (respStr.Contains(errorMessage))
					throw new WikiBotException(string.Format(
						Bot.Msg("Error occurred when uploading image \"{0}\"."), title));
			}
			catch (WikiBotException e) {
				if (!e.Message.Contains("Uploadcorrupt"))    // skip if MediaWiki message not found
					throw;
			}
			title = site.GetNsPrefix(6) + targetName;
			text = description;
			Console.WriteLine(Bot.Msg("Image \"{0}\" has been uploaded successfully."), title);
		}

		/// <summary>Uploads web image to wiki site.</summary>
		/// <param name="imageFileUrl">Full URL of image file on the web.</param>
		/// <param name="description">Image description.</param>
		/// <param name="license">Image license type. Used only in some wiki sites. Pass
		/// empty string, if the wiki site doesn't require it.</param>
		/// <param name="copyStatus">Image copy status. Used only in some wiki sites. Pass
		/// empty string, if the wiki site doesn't require it.</param>
		public void UploadImageFromWeb(string imageFileUrl, string description,
			string license, string copyStatus)
		{
			if (string.IsNullOrEmpty(imageFileUrl))
				throw new ArgumentNullException("imageFileUrl",
					Bot.Msg("No URL specified for image to upload."));
			Uri res = new Uri(imageFileUrl);
			Bot.InitWebClient();
			string imageFileName = imageFileUrl.Substring(imageFileUrl.LastIndexOf("/") + 1);
			try {
				Bot.webClient.DownloadFile(res,
					Bot.cacheDir + Path.DirectorySeparatorChar + imageFileName);
			}
			catch (System.Net.WebException) {
				throw new WikiBotException(string.Format(
					Bot.Msg("Can't access image \"{0}\"."), imageFileUrl));
			}
			if (!File.Exists(Bot.cacheDir + Path.DirectorySeparatorChar + imageFileName))
				throw new WikiBotException(string.Format(
					Bot.Msg("Error occurred when downloading image \"{0}\"."), imageFileUrl));
			UploadImage(Bot.cacheDir + Path.DirectorySeparatorChar + imageFileName,
				description, license, copyStatus, imageFileUrl);
			File.Delete(Bot.cacheDir + Path.DirectorySeparatorChar + imageFileName);
		}

		/// <summary>Downloads image, audio or video file, pointed by this page's title,
		/// from the wiki site to local computer. Redirection is resolved automatically.</summary>
		/// <param name="filePathName">Path and name of local file to save image to.</param>
		public void DownloadImage(string filePathName)
		{
			string res = site.indexPath + "?title=" + Bot.UrlEncode(title);
			string src = "";
			try {
				src = site.GetWebPage(res);
			}
			catch (WebException e) {
				string message = e.Message;
				if (message.Contains(": (404) ")) {    // Not found
					Console.Error.WriteLine(Bot.Msg("Page \"{0}\" doesn't exist."), title);
					text = "";
					return;
				}
				else
					throw;
			}
			Regex fileLinkRegex1 = new Regex("<a href=\"([^\"]+?)\" class=\"internal\"");
			Regex fileLinkRegex2 =
				new Regex("<div class=\"fullImageLink\" id=\"file\"><a href=\"([^\"]+?)\"");
			string fileLink = "";
			if (fileLinkRegex1.IsMatch(src))
				fileLink = fileLinkRegex1.Match(src).Groups[1].ToString();
			else if (fileLinkRegex2.IsMatch(src))
				fileLink = fileLinkRegex2.Match(src).Groups[1].ToString();
			else
				throw new WikiBotException(string.Format(
					Bot.Msg("Image \"{0}\" doesn't exist."), title));
			if (!fileLink.StartsWith("http"))
				fileLink = new Uri(new Uri(site.address), fileLink).ToString();
			Bot.InitWebClient();
			Console.WriteLine(Bot.Msg("Downloading image \"{0}\"..."), title);
			Bot.webClient.DownloadFile(fileLink, filePathName);
			Console.WriteLine(Bot.Msg("Image \"{0}\" has been downloaded successfully."), title);
		}

		/// <summary>Saves page text to the specified file. If the target file already exists
		/// it is overwritten.</summary>
		/// <param name="filePathName">Path and name of the file.</param>
		public void SaveToFile(string filePathName)
		{
			if (IsEmpty()) {
				Console.Error.WriteLine(Bot.Msg("Page \"{0}\" contains no text to save."), title);
				return;
			}
			File.WriteAllText(filePathName, text, Encoding.UTF8);
			Console.WriteLine(Bot.Msg("Text of \"{0}\" page has been saved to \"{1}\" file."),
				title, filePathName);
		}

		/// <summary>Saves <see cref="Page.text"/> to the ".txt" file in current directory.
		/// Use Directory.SetCurrentDirectory() function to change the current directory (but don't
		/// forget to change it back after saving file). The name of the file is constructed
		/// from the title of the article. Forbidden characters in filenames are replaced
		/// with their Unicode numeric codes (also known as numeric character references
		/// or NCRs).</summary>
		public void SaveToFile()
		{
			string fileTitle = title;
			//Path.GetInvalidFileNameChars();
			fileTitle = fileTitle.Replace("\"", "&#x22;");
			fileTitle = fileTitle.Replace("<", "&#x3c;");
			fileTitle = fileTitle.Replace(">", "&#x3e;");
			fileTitle = fileTitle.Replace("?", "&#x3f;");
			fileTitle = fileTitle.Replace(":", "&#x3a;");
			fileTitle = fileTitle.Replace("\\", "&#x5c;");
			fileTitle = fileTitle.Replace("/", "&#x2f;");
			fileTitle = fileTitle.Replace("*", "&#x2a;");
			fileTitle = fileTitle.Replace("|", "&#x7c;");
			SaveToFile(fileTitle + ".txt");
		}

		/// <summary>Returns true, if <see cref="Page.text"/> field is empty. Don't forget
		/// to call <see cref="Page.Load()"/> before using this function.</summary>
		/// <returns>Returns bool value.</returns>
		public bool IsEmpty()
		{
			return string.IsNullOrEmpty(text);
		}

		/// <summary>Returns true, if <see cref="Page.text"/> field is not empty. Don't forget
		/// to call <see cref="Page.Load()"/> before using this function.</summary>
		/// <returns>Returns bool value.</returns>
		public bool Exists()
		{
			return (string.IsNullOrEmpty(text) == true) ? false : true;
		}

		/// <summary>Returns true, if page redirects to another page. Don't forget to load
		/// actual page contents from live wiki <see cref="Page.Load()"/> before using this
		/// function.</summary>
		/// <returns>Returns bool value.</returns>
		public bool IsRedirect()
		{
			if (!Exists())
				return false;
			return site.regexes["redirect"].IsMatch(text);
		}

		/// <summary>Returns redirection target. Don't forget to load
		/// actual page contents from live wiki <see cref="Page.Load()"/> before using this
		/// function.</summary>
		/// <returns>Returns redirection target page title. Returns empty string, if this
		/// Page is not a redirection.</returns>
		public string RedirectsTo()
		{
			if (IsRedirect())
				return site.regexes["redirect"].Match(text).Groups[1].ToString().Trim();
			else
				return string.Empty;
		}

		/// <summary>If this page is a redirection, this function loads the title and text
		/// of redirection target page into this Page object.</summary>
		public void ResolveRedirect()
		{
			if (IsRedirect()) {
				revision = null;
				title = RedirectsTo();
				Load();
			}
		}

		/// <summary>Returns true, if this page is a disambiguation page. This function
		/// automatically recognizes disambiguation templates on Wikipedia sites in
		/// different languages. But in order to be used on other sites, <see cref="Site.disambig"/>
		/// variable must be manually set before this function is called.
		/// <see cref="Site.disambig"/> should contain local disambiguation template's title or
		/// several titles delimited by '|' character, letters case doesn't matter, e.g.
		/// "disambiguation|disambig|disam". Page text
		/// will be loaded from wiki if it was not loaded prior to function call.</summary>
		/// <returns>Returns bool value.</returns>
		public bool IsDisambig()
		{
			if (string.IsNullOrEmpty(text))
				Load();
			if (!string.IsNullOrEmpty(site.disambig))
				return Regex.IsMatch(text, @"(?i)\{\{(" + site.disambig + ")}}");

			if (!site.address.Contains(".wikipedia.org"))
				throw new ArgumentNullException("site.disambigStr", Bot.Msg("You need to " +
					"manually set site.disambigStr variable before calling this function." +
					"Please, refer to documentation for details."));

			Console.WriteLine(Bot.Msg("Loading disambiguation template tags..."));
			var disambigTemplate = "";

			// Try to get template, that English Wikipedia's "Disambiguation" interwiki points to
			if (site.address.Contains("//en.wikipedia.org"))
				disambigTemplate = "Template:Disambiguation";
			else {
				string src = site.GetWebPage(site.apiPath + "?format=xml&action=query" +
					"&list=langbacklinks&lbllang=en&lbltitle=Template%3ADisambiguation");
				var xdoc = XDocument.Parse(src);
				try {
					disambigTemplate = xdoc.Descendants("ll").First().Attribute("title").Value;
				}
				catch {
					throw new ArgumentNullException("site.disambigStr", Bot.Msg("You need to " +
						"manually set site.disambigStr variable before calling this function." +
						"Please, refer to documentation for details."));
				}
			}
			site.disambig = site.RemoveNsPrefix(disambigTemplate, 10);

			// Get local aliases - templates that redirect to discovered disambiguation template
			string src2 = site.GetWebPage(site.apiPath + "?format=xml&action=query" +
				"&list=backlinks&bllimit=500&blfilterredir=redirects&bltitle=" +
				Bot.UrlEncode(disambigTemplate));
			var xdoc2 = XDocument.Parse(src2);
			try {
				var disambigRedirects = (
					from link in xdoc2.Descendants("bl")
					select link.Attribute("title").Value
				).ToList();
				foreach (var disambigRedirect in disambigRedirects)
					site.disambig += '|' + site.RemoveNsPrefix(disambigRedirect, 10);
			}
			catch {}    // silently continue if no alias was found

			return Regex.IsMatch(text, @"(?i)\{\{(" + site.disambig + ")}}");
		}

		/// <summary>Changes default English namespace prefixes to correct local prefixes
		/// (e.g. for German wiki sites it changes "Category:..." to "Kategorie:...").</summary>
		public void CorrectNsPrefix()
		{
			title = site.CorrectNsPrefix(title);
		}

		/// <summary>Returns the list of strings, containing all wikilinks ([[...]])
		/// found in page's text, excluding links in image descriptions, but including
		/// interlanguage links, links to sister wiki projects, categories, images, etc.</summary>
		/// <returns>Returns untouched links in a List.</returns>
		public List<string> GetAllLinks()
		{
			MatchCollection matches = site.regexes["wikiLink"].Matches(text);
			List<string> matchStrings = new List<string>();
			foreach (Match m in matches)
				matchStrings.Add(m.Groups["title"].Value.Trim());
			return matchStrings;
		}

		/// <summary>Finds all wikilinks in page text, excluding interwiki
		/// links, categories, embedded images and links in image descriptions.</summary>
		/// <returns>Returns the PageList object, in which page titles are the wikilinks,
		/// found in text.</returns>
		public PageList GetLinks()
		{
			MatchCollection matches = site.regexes["wikiLink"].Matches(text);
			var exclLinks = GetSisterwikiLinks();
			exclLinks.AddRange(GetInterLanguageLinks());
			PageList pl = new PageList(site);
			for(int i = 0; i < matches.Count; i++) {
				string str = matches[i].Groups["title"].Value;
				if (str.StartsWith(site.GetNsPrefix(6), true, site.langCulture) ||    // image
					str.StartsWith(site.GetEnglishNsPrefix(6), true, site.langCulture) ||
					str.StartsWith(site.GetNsPrefix(14), true, site.langCulture) ||    // category
					str.StartsWith(site.GetEnglishNsPrefix(14), true, site.langCulture))
						continue;
				str = str.TrimStart(':');
				if (exclLinks.Contains(str))
					continue;
				int fragmentPosition = str.IndexOf("#");
				if (fragmentPosition == 0)    // in-page section link
					continue;
				else if (fragmentPosition != -1)
					str = str.Substring(0, fragmentPosition);
				pl.Add(new Page(site, str));
			}
			return pl;
		}

		/// <summary>Returns the list of strings which contains external links
		/// found in page's text.</summary>
		/// <returns>Returns the List object.</returns>
		public List<string> GetExternalLinks()
		{
			MatchCollection matches = site.regexes["webLink"].Matches(text);
			List<string> matchStrings = new List<string>();
			foreach (Match m in matches)
				matchStrings.Add(m.Value);
			return matchStrings;
		}

		/// <summary>Gets interlanguage links for pages on WikiMedia Foundation's
		/// projects.</summary>
		/// <remarks>WARNING: Because of WikiMedia software bug, this function does not work
		/// properly on main pages of WikiMedia Foundation's projects.</remarks>
		/// <returns>Returns Listof strings. Each string contains page title 
		/// prefixed with language code and colon, e.g. "de:Stern".</returns>
		public List<string> GetInterLanguageLinks()
		{
			string src = site.GetWebPage(site.apiPath +
				"?format=xml&action=query&prop=langlinks&lllimit=500&titles=" +
				Bot.UrlEncode(title));
			var xdoc = XDocument.Parse(src);
			var langLinks = (
				from link in xdoc.Descendants("ll")
				select link.Attribute("lang").Value + ':' + link.Value
			).ToList();
			return langLinks;
		}

		/// <summary>Returns links to sister wiki projects, found in this page's text. These may
		/// include interlanguage links but only those embedded in text, not those located 
		/// on wikidata.org</summary>
		/// <returns>Returns the List&lt;string&gt; object.</returns>
		public List<string> GetSisterwikiLinks()
		{
			string src = site.GetWebPage(site.apiPath +
				"?action=query&prop=iwlinks&format=xml&iwlimit=5000&titles=" +
				Bot.UrlEncode(title));
			var xdoc = XDocument.Parse(src);
			var links = (
				from el in xdoc.Descendants("ns")
				select el.Attribute("prefix").Value + '|' + el.Value
			).ToList();
			return links;
		}

		/// <summary>For pages of Wikimedia foundation projects this function returns
		/// interlanguage links located on <see href="https://wikidata.org">
		/// Wikidata.org</see>.</summary>
		/// <returns>Returns the List&lt;string&gt; object.</returns>
		public List<string> GetWikidataLinks()
		{
			string src = site.GetWebPage(site.indexPath + "?title=" + Bot.UrlEncode(title));
			List<string> list = new List<string>();
			if (!src.Contains("<li class=\"interlanguage-link "))
				return list;
			src = "<ul>" + Bot.GetSubstring(src, "<li class=\"interlanguage-link ", "</ul>");
			MatchCollection matches = Regex.Matches(src, "interlanguage-link interwiki-([^\" ]+)");
			foreach (Match m in matches)
				list.Add(m.Groups[1].Value);
			return list;
		}

		/// <summary>For pages that have associated items on <see href="https://wikidata.org">
		/// Wikidata.org</see> this function returns
		/// XElement object with all information provided by Wikidata.
		/// If page is not associated with a Wikidata item null is returned.</summary>
		/// <returns>Returns XElement object or null.</returns>
		/// <example><code>
		/// Page p = new Page(enWikipedia, "Douglas Adams");
		/// XElement wikidataItem = p.GetWikidataItem();
		/// string description = (from desc in wikidataItem.Descendants("description")
		///				          where desc.Attribute("language").Value == "en"
		///				          select desc.Attribute("value").Value).FirstOrDefault();
		/// </code></example>
		public XElement GetWikidataItem()
		{
			string src = site.GetWebPage(site.indexPath + "?title=" + Bot.UrlEncode(title));
			Match m = Regex.Match(src, "href=\"//www\\.wikidata\\.org/wiki/(Q\\d+)");
			if (!m.Success)    // fallback
				m = Regex.Match(src, "\"wgWikibaseItemId\"\\:\"(Q\\d+)\"");
			if (!m.Success) {
				Console.WriteLine(string.Format(Bot.Msg(
					"No Wikidata item is associated with page \"{0}\"."), title));
				return null;
			}
			string item = m.Groups[1].Value;
			string xmlSrc = site.GetWebPage("http://www.wikidata.org/wiki/Special:EntityData/" +
				Bot.UrlEncode(item) + ".xml");    // raises "404: Not found" if not found
			XElement xml = XElement.Parse(xmlSrc);
			Console.WriteLine(string.Format(Bot.Msg(
				"Wikidata item {0} associated with page \"{1}\" was loaded successfully."),
				item, title));
			return xml;
		}

		/// <summary>Function converts basic HTML markup in this page's text to wiki
		/// markup, except for tables markup. Use
		/// <see cref="Page.ConvertHtmlTablesToWikiTables()"/> function to convert HTML
		/// tables markup to wiki format.</summary>
		public void ConvertHtmlMarkupToWikiMarkup()
		{
			text = Regex.Replace(text, "(?is)n?<(h1)( [^/>]+?)?>(.+?)</\\1>n?", "\n= $3 =\n");
			text = Regex.Replace(text, "(?is)n?<(h2)( [^/>]+?)?>(.+?)</\\1>n?", "\n== $3 ==\n");
			text = Regex.Replace(text, "(?is)n?<(h3)( [^/>]+?)?>(.+?)</\\1>n?", "\n=== $3 ===\n");
			text = Regex.Replace(text, "(?is)n?<(h4)( [^/>]+?)?>(.+?)</\\1>n?", "\n==== $3 ====\n");
			text = Regex.Replace(text, "(?is)n?<(h5)( [^/>]+?)?>(.+?)</\\1>n?",
				"\n===== $3 =====\n");
			text = Regex.Replace(text, "(?is)n?<(h6)( [^/>]+?)?>(.+?)</\\1>n?",
				"\n====== $3 ======\n");
			text = Regex.Replace(text, "(?is)\n?\n?<p( [^/>]+?)?>(.+?)</p>", "\n\n$2");
			text = Regex.Replace(text, "(?is)<a href ?= ?[\"'](http:[^\"']+)[\"']>(.+?)</a>",
				"[$1 $2]");
			text = Regex.Replace(text, "(?i)</?(b|strong)>", "'''");
			text = Regex.Replace(text, "(?i)</?(i|em)>", "''");
			text = Regex.Replace(text, "(?i)\n?<hr ?/?>\n?", "\n----\n");
			text = Regex.Replace(text, "(?i)<(hr|br)( [^/>]+?)? ?/?>", "<$1$2 />");
		}

		/// <summary>Function converts HTML table markup in this page's text to wiki
		/// table markup.</summary>
		/// <seealso cref="Page.ConvertHtmlMarkupToWikiMarkup()"/>
		public void ConvertHtmlTablesToWikiTables()
		{
			if (!text.Contains("</table>"))
				return;
			text = Regex.Replace(text, ">\\s+<", "><");
			text = Regex.Replace(text, "<table( ?[^>]*)>", "\n{|$1\n");
			text = Regex.Replace(text, "</table>", "|}\n");
			text = Regex.Replace(text, "<caption( ?[^>]*)>", "|+$1 | ");
			text = Regex.Replace(text, "</caption>", "\n");
			text = Regex.Replace(text, "<tr( ?[^>]*)>", "|-$1\n");
			text = Regex.Replace(text, "</tr>", "\n");
			text = Regex.Replace(text, "<th([^>]*)>", "!$1 | ");
			text = Regex.Replace(text, "</th>", "\n");
			text = Regex.Replace(text, "<td([^>]*)>", "|$1 | ");
			text = Regex.Replace(text, "</td>", "\n");
			text = Regex.Replace(text, "\n(\\||\\|\\+|!) \\| ", "\n$1 ");
			text = text.Replace("\n\n|", "\n|");
		}

		/// <summary>Returns the list of strings which contains category names found in
		/// page's text, with namespace prefix, without sorting keys. You can use the resultant
		/// strings to call <see cref="PageList.FillFromCategory(string)"/>
		/// or <see cref="PageList.FillFromCategoryTree(string)"/>
		/// function. Categories added by templates are not returned. Use GetAllCategories()
		/// function to get such categories too.</summary>
		/// <returns>Returns the List object.</returns>
		public List<string> GetCategories()
		{
			return GetCategories(true, false);
		}

		/// <summary>Returns the list of strings which contains categories' names found in
		/// page text. Categories added by templates are not returned. Use
		/// <see cref="Page.GetAllCategories()"/>
		/// function to get categories added by templates too.</summary>
		/// <param name="withNameSpacePrefix">If true, function returns strings with
		/// namespace prefix like "Category:Stars", not just "Stars".</param>
		/// <param name="withSortKey">If true, function returns strings with sort keys,
		/// if found. Like "Stars|D3" (in [[Category:Stars|D3]]).</param>
		/// <returns>Returns the List object.</returns>
		public List<string> GetCategories(bool withNameSpacePrefix, bool withSortKey)
		{
			MatchCollection matches = site.regexes["wikiCategory"].Matches(
				Regex.Replace(text, "(?is)<nowiki>.+?</nowiki>", ""));
			List<string> matchStrings = new List<string>();
			foreach (Match m in matches) {
				string str = m.Groups[4].Value.Trim();
				if (withSortKey)
					str += m.Groups[5].Value.Trim();
				if (withNameSpacePrefix)
					str = site.GetNsPrefix(14) + str;
				matchStrings.Add(str);
			}
			return matchStrings;
		}

		/// <summary>Returns list of strings, containing category names found in
		/// page's text and added by page's templates.</summary>
		/// <returns>Category names with namespace prefixes (e.g. "Category:Art").</returns>
		public List<string> GetAllCategories()
		{
			string uri;
			string xpathQuery;
			if (site.useApi) {
				uri = site.apiPath + "?action=query&prop=categories" +
					"&clprop=sortkey|hidden&cllimit=5000&format=xml&titles=" +
					Bot.UrlEncode(title);
				xpathQuery = "//categories/cl/@title";
			}
			else {
				uri = site.indexPath + "?title=" + Bot.UrlEncode(title) + "&redirect=no";
				xpathQuery = "//ns:div[ @id='mw-normal-catlinks' or @id='mw-hidden-catlinks' ]" +
					"/ns:ul/ns:li/ns:a";
			}

			string src = site.GetWebPage(uri);
			if (site.useApi) {
				int startPos = src.IndexOf("<!-- start content -->");
				int endPos = src.IndexOf("<!-- end content -->");
				if (startPos != -1 && endPos != -1 && startPos < endPos)
					src = src.Remove(startPos, endPos - startPos);
				else {
					startPos = src.IndexOf("<!-- bodytext -->");
					endPos = src.IndexOf("<!-- /bodytext -->");
					if (startPos != -1 && endPos != -1 && startPos < endPos)
						src = src.Remove(startPos, endPos - startPos);
				}
			}

			XmlNamespaceManager xmlNs = new XmlNamespaceManager(new NameTable());
			xmlNs.AddNamespace("ns", "http://www.w3.org/1999/xhtml");
			XPathNodeIterator iterator = Bot.GetXMLIterator(src, xpathQuery, xmlNs);
			List<string> matchStrings = new List<string>();
			iterator.MoveNext();
			for (int i = 0; i < iterator.Count; i++) {
				matchStrings.Add(site.GetNsPrefix(14) +
					site.RemoveNsPrefix(HttpUtility.HtmlDecode(iterator.Current.Value), 14));
				iterator.MoveNext();
			}

			return matchStrings;
		}

		/// <summary>Adds the page to the specified category by adding a
		/// link to that category to the very end of page's text.
		/// If the link to the specified category
		/// already exists, the function silently does nothing.</summary>
		/// <param name="categoryName">Category name, with or without prefix.
		/// Sort key can also be included after "|", like "Category:Stars|D3".</param>
		public void AddToCategory(string categoryName)
		{
			if (string.IsNullOrEmpty(categoryName))
				throw new ArgumentNullException("categoryName");
			categoryName = site.RemoveNsPrefix(categoryName, 14);
			string cleanCategoryName = !categoryName.Contains("|") ? categoryName.Trim()
				: categoryName.Substring(0, categoryName.IndexOf('|')).Trim();
			List<string> categories = GetCategories(false, false);
			foreach (string category in categories)
				if (category == Bot.Capitalize(cleanCategoryName) ||
					category == Bot.Uncapitalize(cleanCategoryName))
						return;
			text += (categories.Count == 0 ? "\n" : "") +
				"\n[[" + site.GetNsPrefix(14) + categoryName + "]]\n";
			text = text.TrimEnd("\r\n".ToCharArray());
		}

		/// <summary>Removes the page from category by deleting link to that category in
		/// page text.</summary>
		/// <param name="categoryName">Category name, with or without prefix.</param>
		public void RemoveFromCategory(string categoryName)
		{
			if (string.IsNullOrEmpty(categoryName))
				throw new ArgumentNullException("categoryName");
			categoryName = site.RemoveNsPrefix(categoryName, 14).Trim();
			categoryName = !categoryName.Contains("|") ? categoryName
				: categoryName.Substring(0, categoryName.IndexOf('|'));
			List<string> categories = GetCategories(false, false);
			if (!categories.Contains(Bot.Capitalize(categoryName)) &&
				!categories.Contains(Bot.Uncapitalize(categoryName)))
				return;
			string regexCategoryName = Regex.Escape(categoryName);
			regexCategoryName = regexCategoryName.Replace("_", "\\ ").Replace("\\ ", "[_\\ ]");
			int firstCharIndex = (regexCategoryName[0] == '\\') ? 1 : 0;
			regexCategoryName = "[" + char.ToLower(regexCategoryName[firstCharIndex]) + 
				char.ToUpper(regexCategoryName[firstCharIndex]) + "]" +
				regexCategoryName.Substring(firstCharIndex + 1);
			text = Regex.Replace(text, @"\[\[((?i)" + site.GetNsPrefixes(14) + "): ?" +
				regexCategoryName + @"(\|.*?)?]]\r?\n?", "");
			text = text.TrimEnd("\r\n".ToCharArray());
		}

		/// <summary>Returns the templates found in text of this page (those inside double
		/// curly brackets {{...}} ). MediaWiki's
		/// <see href="https://www.mediawiki.org/wiki/Help:Magic_words">"magic words"</see>
		/// are not returned as templates.</summary>
		/// <param name="withParameters">If set to true, everything inside curly brackets is
		/// returned with all parameters untouched.</param>
		/// <param name="includePages">If set to true, titles of "transcluded" pages are returned as
		/// templates and messages with "msgnw:" prefix are also returned as templates. See
		/// <see href="https://www.mediawiki.org/wiki/Transclusion">this page</see> for details.
		/// Default is false.</param>
		/// <returns>Returns the List object.</returns>
		public List<string> GetTemplates(bool withParameters, bool includePages)
		{
			// Blank unsuitable regions with '_' char for easiness
			string str = site.regexes["noWikiMarkup"].Replace(text,
				match => new string('_', match.Value.Length));
			if (GetNamespace() == 10)    // template
				str = Regex.Replace(str, @"\{\{\{.*?}}}",    // remove template parameters
					match => new string('_', match.Value.Length));

			Dictionary<int, int> templPos = new Dictionary<int, int>();
			List<string> templates = new List<string>();
			int startPos, endPos, len = 0;
			// Find all templates positions, blank found templates with '_' char for easiness
			while ((startPos = str.LastIndexOf("{{")) != -1) {
				endPos = str.IndexOf("}}", startPos);
				len = (endPos != -1) ? endPos - startPos + 2 : 2;
				if (len != 2)
					templPos.Add(startPos, len);
				str = str.Remove(startPos, len);
				str = str.Insert(startPos, new String('_', len));
			}

			// Collect templates using found positions, remove non-templates
			foreach (KeyValuePair<int, int> pos in templPos) {
				str = text.Substring(pos.Key + 2, pos.Value - 4).Trim();
				if (str == "" || str[0] == '#')
					continue;
				if (site.regexes["magicWordsAndVars"].IsMatch(str))
					continue;
				if (!withParameters) {
					endPos = str.IndexOf('|');
					if (endPos != -1)
						str = str.Substring(0, endPos);
					if (str == "")
						continue;
				}
				if (!includePages) {
					if (str[0] == ':'
						|| site.regexes["allNsPrefixes"].IsMatch(str)
						|| str.StartsWith("msgnw:")
						|| str.StartsWith("MSGNW:"))
							continue;
				} else {
					if (str[0] == ':')
						str = str.Remove(0, 1);
					else if(str.StartsWith("msgnw:") || str.StartsWith("MSGNW:"))
						str = str.Remove(0, 6);
				}
				templates.Add(str);
			}

			templates.Reverse();
			return templates;
		}

		/// <summary>Adds a specified template to the end of the page text,
		/// but before categories.</summary>
		/// <param name="templateText">Complete template in double brackets,
		/// e.g. "{{TemplateTitle|param1=val1|param2=val2}}".</param>
		public void AddTemplate(string templateText)
		{
			if (string.IsNullOrEmpty(templateText))
				throw new ArgumentNullException("templateText");
			Regex templateInsertion = new Regex("([^}]\n|}})\n*\\[\\[((?i)" +
				site.GetNsPrefixes(14) + "):");
			if (templateInsertion.IsMatch(text))
				text = templateInsertion.Replace(text, "$1\n" + templateText + "\n\n[[" +
					site.GetNsPrefix(14), 1);
			else {
				text += "\n\n" + templateText;
				text = text.TrimEnd("\r\n".ToCharArray());
			}
		}

		/// <summary>Removes all instances of a specified template from page text.</summary>
		/// <param name="templateTitle">Title of template to remove.</param>
		public void RemoveTemplate(string templateTitle)
		{
			if (string.IsNullOrEmpty(templateTitle))
				throw new ArgumentNullException("templateTitle");
			templateTitle = Regex.Escape(templateTitle);
			templateTitle = "(" + Char.ToUpper(templateTitle[0]) + "|" +
				Char.ToLower(templateTitle[0]) + ")" +
				(templateTitle.Length > 1 ? templateTitle.Substring(1) : "");
			text = Regex.Replace(text, @"(?s)\{\{\s*" + templateTitle +
				@"(.*?)}}\r?\n?", "");
		}

		/// <summary>Returns specified parameter of a specified template. If several instances
		/// of specified template are found in text of this page, all parameter values
		/// are returned.</summary>
		/// <param name="templateTitle">Title of template to get parameter of.</param>
		/// <param name="templateParameter">Title of template's parameter. If parameter is
		/// untitled, specify it's number as string. If parameter is titled, but it's number is
		/// specified, the function will return empty List &lt;string&gt; object.</param>
		/// <returns>Returns the List &lt;string&gt; object with strings, containing values of
		/// specified parameters in all found template instances. Returns empty List &lt;string&gt;
		/// object if no specified template parameters were found.</returns>
		public List<string> GetTemplateParameter(string templateTitle,
			string templateParameter)
		{
			if (string.IsNullOrEmpty(templateTitle))
				throw new ArgumentNullException("templateTitle");
			if (string.IsNullOrEmpty(templateParameter))
				throw new ArgumentNullException("templateParameter");
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("text");

			List<string> parameterValues = new List<string>();
			Dictionary <string, string> parameters;
			templateTitle = templateTitle.Trim();
			templateParameter = templateParameter.Trim();
			Regex templateTitleRegex = new Regex("^\\s*(" +
				Bot.Capitalize(Regex.Escape(templateTitle)) + "|" +
				Bot.Uncapitalize(Regex.Escape(templateTitle)) +
				")\\s*\\|");
			foreach (string template in GetTemplates(true, false)) {
				if (templateTitleRegex.IsMatch(template)) {
					parameters = Page.ParseTemplate(template);
					if (parameters.ContainsKey(templateParameter))
						parameterValues.Add(parameters[templateParameter]);
				}
			}
			return parameterValues;
		}

		/// <summary>This helper method returns specified parameter of a first found instance of
		/// specified template. If no such template or no such parameter was found,
		/// empty string "" is returned.</summary>
		/// <param name="templateTitle">Title of template to get parameter of.</param>
		/// <param name="templateParameter">Title of template's parameter. If parameter is
		/// untitled, specify it's number as string. If parameter is titled, but it's number is
		/// specified, the function will return empty List &lt;string&gt; object.</param>
		/// <returns>Returns parameter as string or empty string "".</returns>
		/// <remarks>Thanks to Eyal Hertzog and metacafe.com team for idea of this
		/// function.</remarks>
		public string GetFirstTemplateParameter(string templateTitle,
			string templateParameter)
		{
			List<string> paramsList = GetTemplateParameter(templateTitle, templateParameter);
			if (paramsList.Count == 0)
				return "";
			else
				return paramsList[0];
		}

		/// <summary>Sets the specified parameter of the specified template to new value.
		/// If several instances of specified template are found in text of this page, either
		/// first value can be set, or all values in all instances.</summary>
		/// <param name="templateTitle">Title of template.</param>
		/// <param name="templateParameter">Title of template's parameter.</param>
		/// <param name="newParameterValue">New value to set the parameter to.</param>
		/// <param name="firstTemplateOnly">When set to true, only first found template instance
		/// is modified. When set to false, all found template instances are modified.</param>
		/// <returns>Returns the number of modified values.</returns>
		/// <remarks>Thanks to Eyal Hertzog and metacafe.com team for idea of this
		/// function.</remarks>
		public int SetTemplateParameter(string templateTitle, string templateParameter,
			string newParameterValue, bool firstTemplateOnly)
		{
			if (string.IsNullOrEmpty(templateTitle))
				throw new ArgumentNullException("templateTitle");
			if (string.IsNullOrEmpty(templateParameter))
				throw new ArgumentNullException("templateParameter");
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("text");

			int i = 0;
			Dictionary <string, string> parameters;
			templateTitle = templateTitle.Trim();
			templateParameter = templateParameter.Trim();
			Regex templateTitleRegex = new Regex("^\\s*(" +
				Bot.Capitalize(Regex.Escape(templateTitle)) + "|" +
				Bot.Uncapitalize(Regex.Escape(templateTitle)) +
				")\\s*\\|");
			foreach (string template in GetTemplates(true, false)) {
				if (templateTitleRegex.IsMatch(template)) {
					parameters = Page.ParseTemplate(template);
					string newTemplate;
					if (newParameterValue != null)
						parameters[templateParameter] = newParameterValue;
					else
						parameters.Remove(templateParameter);
					newTemplate = Page.FormatTemplate(templateTitle, parameters, template);
					Regex oldTemplate = new Regex(Regex.Escape(template));
					newTemplate = newTemplate.Substring(2, newTemplate.Length - 4);
					newTemplate = newTemplate.TrimEnd("\n".ToCharArray());
					text = oldTemplate.Replace(text, newTemplate, 1);
					i++;
					if (firstTemplateOnly == true)
						break;
				}
			}
			return i;
		}

		/// <summary>Removes the specified parameter of the specified template.
		/// If several instances of specified template are found in text of this page, either
		/// first instance can be affected or all instances.</summary>
		/// <param name="templateTitle">Title of template.</param>
		/// <param name="templateParameter">Title of template's parameter.</param>
		/// <param name="firstTemplateOnly">When set to true, only first found template instance
		/// is modified. When set to false, all found template instances are modified.</param>
		/// <returns>Returns the number of removed instances.</returns>
		public int RemoveTemplateParameter(string templateTitle, string templateParameter,
			bool firstTemplateOnly)
		{
			return SetTemplateParameter(templateTitle, templateParameter, null, firstTemplateOnly);
		}

		/// <summary>Parses the provided template body and returns the key/value pairs of it's
		/// parameters titles and values. Everything inside the double braces must be passed to
		/// this function, so first goes the template's title, then '|' character, and then go the
		/// parameters. Please, see the usage example.</summary>
		/// <param name="template">Complete template's body including it's title, but not
		/// including double braces.</param>
		/// <returns>Returns the Dictionary &lt;string, string&gt; object, where keys are parameters
		/// titles and values are parameters values. If parameter is untitled, it's number is
		/// returned as the (string) dictionary key. If parameter value is set several times in the
		/// template (normally that shouldn't occur), only the last value is returned. Template's
		/// title is not returned as a parameter.</returns>
		/// <example><code>
		/// Dictionary &lt;string, string&gt; parameters1 =
		/// 	site.ParseTemplate("TemplateTitle|param1=val1|param2=val2");
		/// string[] templates = page.GetTemplates(true, false);
		/// Dictionary &lt;string, string&gt; parameters2 = site.ParseTemplate(templates[0]);
		/// parameters1["param2"] = "newValue";
		/// </code></example>
		public static Dictionary<string, string> ParseTemplate(string template)
		{
			if (string.IsNullOrEmpty(template))
				throw new ArgumentNullException("template");
			if (template.StartsWith("{{"))
				template = template.Substring(2, template.Length - 4);

			int startPos, endPos, len = 0;
			string str = template;

			while ((startPos = str.LastIndexOf("{{")) != -1)
			{
				endPos = str.IndexOf("}}", startPos);
				len = (endPos != -1) ? endPos - startPos + 2 : 2;
				str = str.Remove(startPos, len);
				str = str.Insert(startPos, new String('_', len));
			}

			while ((startPos = str.LastIndexOf("[[")) != -1)
			{
				endPos = str.IndexOf("]]", startPos);
				len = (endPos != -1) ? endPos - startPos + 2 : 2;
				str = str.Remove(startPos, len);
				str = str.Insert(startPos, new String('_', len));
			}

			List<int> separators = Bot.GetMatchesPositions(str, "|", false);
			if (separators == null || separators.Count == 0)
				return new Dictionary<string, string>();
			List<string> parameters = new List<string>();
			endPos = template.Length;
			for (int i = separators.Count - 1; i >= 0; i--)
			{
				parameters.Add(template.Substring(separators[i] + 1, endPos - separators[i] - 1));
				endPos = separators[i];
			}
			parameters.Reverse();

			Dictionary<string, string> templateParams = new Dictionary<string, string>();
			for (int pos, i = 0; i < parameters.Count; i++)
			{
				pos = parameters[i].IndexOf('=');
				if (pos == -1)
					templateParams[i.ToString()] = parameters[i].Trim();
				else
					templateParams[parameters[i].Substring(0, pos).Trim()] =
						parameters[i].Substring(pos + 1).Trim();
			}
			return templateParams;
		}

		/// <summary>Formats a template with the specified title and parameters. Default formatting
		/// options are used.</summary>
		/// <param name="templateTitle">Template's title.</param>
		/// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;
		/// object, where keys are parameters titles and values are parameters values.</param>
		/// <returns>Returns the complete template in double braces.</returns>
		public static string FormatTemplate(string templateTitle,
			Dictionary<string, string> templateParams)
		{
			return FormatTemplate(templateTitle, templateParams, false, false, 0);
		}

		/// <summary>Formats a template with the specified title and parameters. Formatting
		/// options are got from provided reference template. That function is usually used to
		/// format modified template as it was in it's initial state, though absolute format
		/// consistency can not be guaranteed.</summary>
		/// <param name="templateTitle">Template's title.</param>
		/// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;
		/// object, where keys are parameters titles and values are parameters values.</param>
		/// <param name="referenceTemplate">Full template body to detect formatting options in.
		/// With or without double braces.</param>
		/// <returns>Returns the complete template in double braces.</returns>
		public static string FormatTemplate(string templateTitle,
			Dictionary<string, string> templateParams, string referenceTemplate)
		{
			if (string.IsNullOrEmpty(referenceTemplate))
				throw new ArgumentNullException("referenceTemplate");

			bool inline = false;
			bool withoutSpaces = false;
			int padding = 0;

			if (!referenceTemplate.Contains("\n|") && !referenceTemplate.Contains("\n |"))
				inline = true;
			if (!referenceTemplate.Contains("| ") && !referenceTemplate.Contains("= "))
				withoutSpaces = true;
			if (!inline && referenceTemplate.Contains("  ="))
				padding = -1;

			return FormatTemplate(templateTitle, templateParams, inline, withoutSpaces, padding);
		}

		/// <summary>Formats a template with the specified title and parameters, allows extended
		/// format options to be specified.</summary>
		/// <param name="templateTitle">Template's title.</param>
		/// <param name="templateParams">Template's parameters in Dictionary &lt;string, string&gt;
		/// object, where keys are parameters titles and values are parameters values.</param>
		/// <param name="inline">When set to true, template is formatted in one line, without any
		/// line breaks. Default value is false.</param>
		/// <param name="withoutSpaces">When set to true, template is formatted without spaces.
		/// Default value is false.</param>
		/// <param name="padding">When set to positive value, template parameters titles are padded
		/// on the right with specified number of spaces, so "=" characters could form a nice
		/// straight column. When set to -1, the number of spaces is calculated automatically.
		/// Default value is 0 (no padding). The padding will occur only when "inline" option
		/// is set to false and "withoutSpaces" option is also set to false.</param>
		/// <returns>Returns the complete template in double braces.</returns>
		public static string FormatTemplate(string templateTitle,
			Dictionary<string, string> templateParams, bool inline, bool withoutSpaces, int padding)
		{
			if (string.IsNullOrEmpty(templateTitle))
				throw new ArgumentNullException("templateTitle");
			if (templateParams == null || templateParams.Count == 0)
				throw new ArgumentNullException("templateParams");

			if (inline != false || withoutSpaces != false)
				padding = 0;
			if (padding == -1)
				foreach (KeyValuePair<string, string> kvp in templateParams)
					if (kvp.Key.Length > padding)
						padding = kvp.Key.Length;

			string paramBreak = "|";
			string equalsSign = "=";
			if (!inline)
				paramBreak = "\n|";
			if (!withoutSpaces)
			{
				equalsSign = " = ";
				paramBreak += " ";
			}

			int i = 1;
			string template = "{{" + templateTitle;
			foreach (KeyValuePair<string, string> kvp in templateParams)
			{
				template += paramBreak;
				if (padding <= 0)
				{
					if (kvp.Key == i.ToString())
						template += kvp.Value;
					else
						template += kvp.Key + equalsSign + kvp.Value;
				}
				else
				{
					if (kvp.Key == i.ToString())
						template += kvp.Value.PadRight(padding + 3);
					else
						template += kvp.Key.PadRight(padding) + equalsSign + kvp.Value;
				}
				i++;
			}
			if (!inline)
				template += "\n";
			template += "}}";

			return template;
		}

		/// <summary>Returns a list of files, embedded in this page.</summary>
		/// <returns>Returns the List object. Returned file names contain namespace prefixes.
		/// The list can be empty. Strings in list may recur
		/// indicating that file was embedded several times.</returns>
		public List<string> GetImages()
		{
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("text");
			string nsPrefixes = site.GetNsPrefixes(6);
			MatchCollection matches =
				Regex.Matches(text, @"\[\[\s*((?i)" + nsPrefixes + @"):(?<filename>[^|\]]+)");
			List<string> matchStrings = new List<string>();
			foreach (Match m in matches) {
				matchStrings.Add(site.GetNsPrefix(6) + m.Groups["filename"].Value.Trim());
			}
			if (Regex.IsMatch(text, "(?i)<gallery>")) {
				matches = Regex.Matches(text,
					@"^\s*((?i)" + nsPrefixes + "):(?<filename>[^|\\]\r?\n]+)");
				foreach (Match m in matches) {
					matchStrings.Add(site.GetNsPrefix(6) + m.Groups["filename"].Value.Trim());
				}
			}
			return matchStrings;
		}

		/// <summary>Identifies the namespace of the page.</summary>
		/// <returns>Returns the integer key of the namespace.</returns>
		public int GetNamespace()
		{
			return site.GetNamespace(title);
		}

		/// <summary>Sends page title to console.</summary>
		public void ShowTitle()
		{
			Console.Write("\n" + Bot.Msg("The title of this page is \"{0}\".") + "\n", title);
		}

		/// <summary>Sends page text to console.</summary>
		public void ShowText()
		{
			Console.Write("\n" + Bot.Msg("The text of \"{0}\" page:"), title);
			Console.Write("\n\n" + text + "\n\n");
		}

		/// <summary>Renames the page. Redirection from old title to new title is
		/// automatically created.</summary>
		/// <param name="newTitle">New title of this page.</param>
		/// <param name="reason">Reason for renaming.</param>
		public void RenameTo(string newTitle, string reason)
		{
			RenameTo(newTitle, reason, false, false);
		}

		/// <summary>Renames the page. Redirection from old title to new title is
		/// automatically created.</summary>
		/// <param name="newTitle">New title of this page.</param>
		/// <param name="reason">Reason for renaming.</param>
		/// <param name="renameTalkPage">If true, corresponding talk page will
		/// also be renamed.</param>
		/// <param name="renameSubPages">If true, subpages (like User:Me/Subpage)
		/// will also be renamed.</param>
		public void RenameTo(string newTitle, string reason, bool renameTalkPage,
			bool renameSubPages)
		{
			if (string.IsNullOrEmpty(newTitle))
				throw new ArgumentNullException("newTitle");
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for page to rename."));

			if (Bot.askConfirm) {
				Console.Write("\n\n" +
					Bot.Msg("The page \"{0}\" is going to be renamed to \"{1}\".\n"),
					title, newTitle);
				if(!Bot.UserConfirms())
					return;
			}

			if (site.useApi) {

				string token = "";
				if (site.tokens != null && site.tokens.ContainsKey("csrftoken"))
					token = site.tokens["csrftoken"];
				else {
					var tokens = GetSecurityTokens("move");
					if (tokens.ContainsKey("missing"))
						throw new WikiBotException(
							string.Format(Bot.Msg("Page \"{0}\" doesn't exist."), title));
					if (!tokens.ContainsKey("movetoken") || tokens["movetoken"] == "")
						throw new WikiBotException(string.Format(
							Bot.Msg("Unable to rename page \"{0}\" to \"{1}\"."), title, newTitle));
					token = tokens["movetoken"];
				}

				string postData = string.Format("from={0}&to={1}&reason={2}{3}{4}&token={5}",
					Bot.UrlEncode(title),
					Bot.UrlEncode(newTitle),
					Bot.UrlEncode(reason),
					renameTalkPage ? "&movetalk=1" : "",
					renameSubPages ? "&movesubpages=1" : "",
					Bot.UrlEncode(token));
				string respStr = site.PostDataAndGetResult(site.apiPath + "?action=move" +
					"&format=xml", postData);
				if (respStr.Contains("<error"))
					throw new WikiBotException(string.Format(
						Bot.Msg("Failed to rename page \"{0}\" to \"{1}\"."), title, newTitle));
			}
			else {
				string respStr = site.GetWebPage(site.indexPath + "?title=Special:Movepage/" +
					Bot.UrlEncode(title));
				Match m = site.regexes["editToken"].Match(respStr);
				string securityToken = string.IsNullOrEmpty(m.Groups[1].Value)
					? m.Groups[2].Value : m.Groups[1].Value;
				if (string.IsNullOrEmpty(securityToken)) {
					Console.Error.WriteLine(
						Bot.Msg("Unable to rename page \"{0}\" to \"{1}\"."), title, newTitle);
					return;
				}

				if (site.watchList == null) {
					site.watchList = new PageList(site);
					site.watchList.FillFromWatchList();
				}
				watched = site.watchList.Contains(this);

				string postData = string.Format("wpNewTitle={0}&wpOldTitle={1}&wpEditToken={2}" +
					"&wpReason={3}{4}{5}{6}",
					Bot.UrlEncode(newTitle),
					Bot.UrlEncode(title),
					Bot.UrlEncode(securityToken),
					Bot.UrlEncode(reason),
					renameTalkPage ? "&wpMovetalk=1" : "",
					renameSubPages ? "&wpMovesubpages=1" : "",
					watched ? "&wpWatch=1" : "");
				respStr = site.PostDataAndGetResult(site.indexPath +
					"?title=Special:Movepage&action=submit", postData);

				if (site.messages == null)
					site.LoadMediawikiMessages(true);
				Regex successMsg = new Regex(
					"<h1[^>]*>(<span[^>]*>)?\\s*" + site.messages["pagemovedsub"] + "\\s*<");
				if (!successMsg.IsMatch(respStr))
					throw new WikiBotException(string.Format(
						Bot.Msg("Failed to rename page \"{0}\" to \"{1}\"."), title, newTitle));
			}

			title = newTitle;
			Console.WriteLine(
				Bot.Msg("Page \"{0}\" has been successfully renamed to \"{1}\"."), title, newTitle);
		}

		/// <summary>Deletes the page. Administrator's rights are required
		/// for this action.</summary>
		/// <param name="reason">Reason for deletion.</param>
		public void Delete(string reason)
		{
			if (string.IsNullOrEmpty(title))
				throw new WikiBotException(Bot.Msg("No title is specified for page to delete."));

			if (Bot.askConfirm) {
				Console.Write("\n\n" + Bot.Msg("The page \"{0}\" is going to be deleted.\n"),
					title);
				if(!Bot.UserConfirms())
					return;
			}

			if (site.useApi) {

				string token = "";
				if (site.tokens != null && site.tokens.ContainsKey("csrftoken"))
					token = site.tokens["csrftoken"];
				else {
					var tokens = GetSecurityTokens("delete");
					if (tokens.ContainsKey("missing"))
						throw new WikiBotException(
							string.Format(Bot.Msg("Page \"{0}\" doesn't exist."), title));
					if (!tokens.ContainsKey("deletetoken") || tokens["deletetoken"] == "")
						throw new WikiBotException(
							string.Format(Bot.Msg("Unable to delete page \"{0}\"."), title));
					token = tokens["deletetoken"];
				}

				string postData = string.Format("reason={0}&token={1}",
					Bot.UrlEncode(reason), Bot.UrlEncode(token));
				string respStr = site.PostDataAndGetResult(site.apiPath + "?action=delete" +
					"&title=" + Bot.UrlEncode(title) + "&format=xml", postData);
				if (respStr.Contains("<error"))
					throw new WikiBotException(
						string.Format(Bot.Msg("Failed to delete page \"{0}\"."), title));
			}
			else {
				string respStr = site.GetWebPage(site.indexPath + "?title=" +
					Bot.UrlEncode(title) + "&action=delete");
				Match m = site.regexes["editToken"].Match(respStr);
				string securityToken = string.IsNullOrEmpty(m.Groups[1].Value)
					? m.Groups[2].Value : m.Groups[1].Value;
				if (string.IsNullOrEmpty(securityToken)) {
					Console.Error.WriteLine(
						Bot.Msg("Unable to delete page \"{0}\"."), title);
					return;
				}

				string postData = string.Format("wpReason={0}&wpEditToken={1}",
					Bot.UrlEncode(reason), Bot.UrlEncode(securityToken));
				respStr = site.PostDataAndGetResult(site.indexPath + "?title=" +
					Bot.UrlEncode(title) + "&action=delete", postData);

				if (site.messages == null)
					site.LoadMediawikiMessages(true);
				Regex successMsg = new Regex(
					"<h1[^>]*>(<span[^>]*>)?\\s*" + site.messages["actioncomplete"] + "\\s*<");
				if (!successMsg.IsMatch(respStr))
					throw new WikiBotException(
						string.Format(Bot.Msg("Failed to delete page \"{0}\"."), title));
			}

			Console.WriteLine(Bot.Msg("Page \"{0}\" has been successfully deleted."), title);
			title = "";
		}
	}



	/// <summary>Class defines a set of wiki pages. List&lt;Page&gt; object is used internally
	/// for pages storing.</summary>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	[Serializable]
	public class PageList
	{
		/// <summary>Internal generic List that contains collection of pages.</summary>
		public List<Page> pages = new List<Page>();
		/// <summary>Site, on which the pages are located.</summary>
		public Site site;

		/// <summary>This constructor creates PageList object with specified Site object and fills
		/// it with <see cref="Page"/> objects with specified titles. When constructed, new
		/// <see cref="Page"/> in PageList doesn't contain text. Use <see cref="PageList.Load()"/>
		/// method to get texts and metadata from live wiki.</summary>
		/// <param name="site">Site object, it must be constructed beforehand.</param>
		/// <param name="pageNames">Page titles as array of strings.</param>
		/// <returns>Returns the PageList object.</returns>
		public PageList(Site site, string[] pageNames)
		{
			this.site = site;
			foreach (string pageName in pageNames)
				pages.Add(new Page(site, pageName));
			CorrectNsPrefixes();
		}

		/// <summary>This constructor creates PageList object with specified Site object and fills
		/// it with <see cref="Page"/> objects with specified titles. When constructed, new 
		/// <see cref="Page"/> in PageList don't contain text. Use <see cref="PageList.Load()"/>
		/// method to get texts and metadata from live wiki.</summary>
		/// <param name="site">Site object, it must be constructed beforehand.</param>
		/// <param name="pageNames">Page titles in List object.</param>
		/// <returns>Returns the PageList object.</returns>
		public PageList(Site site, List<string> pageNames)
		{
			this.site = site;
			foreach (string pageName in pageNames)
				pages.Add(new Page(site, pageName));
			CorrectNsPrefixes();
		}

		/// <summary>This constructor creates empty PageList object with specified
		/// Site object.</summary>
		/// <param name="site">Site object, it must be constructed beforehand.</param>
		/// <returns>Returns the PageList object.</returns>
		public PageList(Site site)
		{
			this.site = site;
		}

		/// <summary>This constructor creates empty PageList object using most recently
		/// created <see cref="Site"/> object.</summary>
		/// <returns>Returns the PageList object.</returns>
		public PageList()
		{
			site = Bot.GetMostRecentSiteObject();
		}

		/// <summary>This index allows to call pageList[i] instead of pageList.pages[i].</summary>
		/// <param name="index">Zero-based index.</param>
		/// <returns>Returns the Page object.</returns>
		public Page this[int index]
		{
			get { return pages[index]; }
			set { pages[index] = value; }
		}

		/// <summary>This function allows to access individual pages in this PageList.
		/// But it's better to use simple pageList[i] syntax.</summary>
		/// <param name="index">Zero-based index.</param>
		/// <returns>Returns the Page object.</returns>
		public Page GetPage(int index)
		{
			return pages[index];
		}

		/// <summary>This function allows to set individual pages in this PageList.
		/// But it's better to use simple pageList[i] syntax.</summary>
		/// <param name="page">Page object to set in this PageList.</param>
		/// <param name="index">Zero-based index.</param>
		/// <returns>Returns the Page object.</returns>
		public void SetPageAtIndex(Page page, int index)
		{
			pages[index] = page;
		}

		/// <summary>This index allows to use pageList["title"] syntax. Don't forget to use correct
		/// local namespace prefixes. Call <see cref="CorrectNsPrefixes()"/> function to correct
		/// namespace prefixes in a whole PageList at once.</summary>
		/// <param name="index">Title of page to get.</param>
		/// <returns>Returns the Page object, or null if there is no page with the specified
		/// title in this PageList.</returns>
		public Page this[string index]
		{
			get {
				foreach (Page p in pages)
					if (p.title == index)
						return p;
				return null;
			}
			set {
				for (int i=0; i < pages.Count; i++)
					if (pages[i].title == index)
						pages[i] = value;
			}
		}

		/// <summary>This function allows to iterate over <see cref="Page"/> objects in this
		/// PageList using "foreach" loop.</summary>
		/// <returns>Returns IEnumerator object.</returns>
		public IEnumerator GetEnumerator()
		{
			return pages.GetEnumerator();
		}

		/// <summary>This function adds specified page to the end of this PageList.</summary>
		/// <param name="page">Page object to add.</param>
		public void Add(Page page)
		{
			pages.Add(page);
		}

		/// <summary>Inserts an element into this PageList at the specified index.</summary>
		/// <param name="page">Page object to insert.</param>
		/// <param name="index">Zero-based index.</param>
		public void Insert(Page page, int index)
		{
			pages.Insert(index, page);
		}

		/// <summary>This function returns true, if this PageList contains page with the same title
		/// and same revision ID with page passed as a parameter. Before comparison this function 
		/// corrects all namespace prefixes in this PageList and in title of Page passed
		/// as a parameter.</summary>
		/// <param name="page">Page object to search for in this PageList.</param>
		/// <returns>Returns bool value.</returns>
		public bool Contains(Page page)
		{
			page.CorrectNsPrefix();
			CorrectNsPrefixes();
			foreach (Page p in pages) {
				if (p.title == page.title
					&& (p.revision == null || page.revision == null || p.revision == page.revision))
						return true;
			}
			return false;
		}

		/// <summary>This function returns true, if a page with specified title exists
		/// in this PageList. This function corrects all namespace prefixes in this PageList
		/// before comparison.</summary>
		/// <param name="title">Title of page to check.</param>
		/// <returns>Returns bool value.</returns>
		public bool Contains(string title)
		{
			title = site.CorrectNsPrefix(title);
			CorrectNsPrefixes();
			foreach (Page p in pages)
				if (p.title == title)
					return true;
			return false;
		}

		/// <summary>This function returns the number of pages in PageList.</summary>
		/// <returns>Number of pages as positive integer value.</returns>
		public int Count()
		{
			return pages.Count;
		}

		/// <summary>Removes page at specified index from PageList.</summary>
		/// <param name="index">Zero-based index.</param>
		public void RemovePage(int index)
		{
			pages.RemoveAt(index);
		}

		/// <summary>Removes a page with specified title from this PageList.</summary>
		/// <param name="title">Title of page to remove.</param>
		public void Remove(string title)
		{
			for(int i = 0; i < Count(); i++)
				if (pages[i].title == title)
					pages.RemoveAt(i);
		}

		/// <summary>Gets page titles for this PageList from "Special:Allpages" MediaWiki page.
		/// That means a list of pages in alphabetical order.</summary>
		/// <param name="firstPageTitle">Title of page to start enumerating from. The title
		/// must have no namespace prefix (like "Talk:"). Alternatively just a few first letters
		/// can be specified instead of full real title. Pass the empty string or null
		/// to start from the very beginning.</param>
		/// <param name="neededNSpace">The key of namespace to get pages
		/// from. Zero is a key of default namespace.</param>
		/// <param name="acceptRedirects">Set this to "false" to exclude redirects.</param>
		/// <param name="limit">Maximum allowed limit of pages to get.</param>
		public void FillFromAllPages(string firstPageTitle, int neededNSpace, bool acceptRedirects,
			int limit)
		{
			FillFromAllPages(firstPageTitle, neededNSpace, acceptRedirects, limit, "");
		}

		/// <summary>Gets page titles for this PageList from "Special:Allpages" MediaWiki page.
		/// That means a list of pages in alphabetical order.</summary>
		/// <param name="firstPageTitle">Title of page to start listing from. The title
		/// must have no namespace prefix (like "Talk:"). Just a few first letters
		/// can be specified instead of full real title. Pass the empty string or null
		/// to start from the very beginning.</param>
		/// <param name="neededNSpace">The key of namespace to get pages
		/// from. Zero is a key of default namespace.</param>
		/// <param name="acceptRedirects">Set this to "false" to exclude redirects.</param>
		/// <param name="limit">Maximum allowed limit of pages to get.</param>
		/// <param name="lastPageTitle">Title of page to stop listing at.
		/// To get all pages with some prefix use the following method: 
		/// <c>FillFromAllPages("Prefix",0,false,100,"Prefix~")</c></param>
		public void FillFromAllPages(string firstPageTitle, int neededNSpace, bool acceptRedirects,
			int limit, string lastPageTitle)
		{
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit");

			if (site.useApi) {
				FillFromCustomApiQuery("list=allpages", "apnamespace=" + neededNSpace +
					(acceptRedirects ? "" : "&apfilterredir=nonredirects") +
					(string.IsNullOrEmpty(firstPageTitle) ? "" : "&apfrom=" +
					Bot.UrlEncode(firstPageTitle)) +
					(string.IsNullOrEmpty(lastPageTitle) ? "" : "&apto=" +
					Bot.UrlEncode(lastPageTitle)), limit);
			}
			else {
				Console.WriteLine(
					Bot.Msg("Getting {0} page titles from \"Special:Allpages\" MediaWiki page..."),
					limit);
				int count = pages.Count;
				limit += pages.Count;
				Regex linkToPageRegex;
				if (acceptRedirects)
					linkToPageRegex = new Regex("<td[^>]*>(?:<div class=\"allpagesredirect\">)?" +
						"<a href=\"[^\"]*?\" (?:class=\"mw-redirect\" )?title=\"([^\"]*?)\">");
				else
					linkToPageRegex =
						new Regex("<td[^>]*><a href=\"[^\"]*?\" title=\"([^\"]*?)\">");
				MatchCollection matches;
				do {
					string res = site.indexPath + "?title=Special:Allpages" +
						"&from=" + Bot.UrlEncode(
							string.IsNullOrEmpty(firstPageTitle) ? "!" : firstPageTitle) +
						Bot.UrlEncode(
							string.IsNullOrEmpty(lastPageTitle) ? "" : ("&to=" + lastPageTitle)) +
						"&namespace=" + neededNSpace.ToString();
					matches = linkToPageRegex.Matches(site.GetWebPage(res));
					if (matches.Count < 2)
						break;
					for (int i = 1; i < matches.Count; i++)
						pages.Add(new Page(site,
							HttpUtility.HtmlDecode(matches[i].Groups[1].Value)));
					firstPageTitle = site.RemoveNsPrefix(pages[pages.Count - 1].title,
						neededNSpace) + "!";
				}
				while (pages.Count < limit);
				if (pages.Count > limit)
					pages.RemoveRange(limit, pages.Count - limit);
				Console.WriteLine(Bot.Msg("PageList has been filled with {0} page titles from " +
					"\"Special:Allpages\" MediaWiki page."), (pages.Count - count).ToString());
			}
		}

		/// <summary>Gets page titles for this PageList from specified special page.
		/// The following special pages are supported (other were not tested):<b><i>
		/// Ancientpages, BrokenRedirects, Deadendpages, Disambiguations, DoubleRedirects,
		/// Listredirects, Lonelypages, Longpages, Mostcategories, Mostimages, Mostlinkedcategories,
		/// Mostlinkedtemplates, Mostlinked, Mostrevisions, Fewestrevisions, Shortpages,
		/// Uncategorizedcategories, Uncategorizedpages, Uncategorizedimages,
		/// Uncategorizedtemplates, Unusedcategories, Unusedimages, Wantedcategories, Wantedfiles,
		/// Wantedpages, Wantedtemplates, Unwatchedpages, Unusedtemplates, Withoutinterwiki.</i></b>
		/// The function doesn't filter namespaces and does not clear PageList,
		/// so new pages will be added to existing pages.</summary>
		/// <param name="pageTitle">Title of special page, e.g. "Deadendpages".</param>
		/// <param name="limit">Maximum number of page titles to get.</param>
		public void FillFromCustomSpecialPage(string pageTitle, int limit)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit");
			Console.WriteLine(Bot.Msg("Getting {0} page titles from \"Special:{1}\" page..."),
				limit, pageTitle);

			int preexistingPages = this.Count();

			if (site.useApi) {
				FillFromCustomApiQuery("list=querypage", "qppage=" + pageTitle, limit);
			}
			else {

				bool fallback = false;

				// TO DO: paging
				string res = site.indexPath + "?title=Special:" +
					Bot.UrlEncode(pageTitle) + "&limit=" + limit.ToString();
				string src = site.GetWebPage(res);
				MatchCollection matches;
				if (pageTitle == "Unusedimages" || pageTitle == "Uncategorizedimages" ||
					pageTitle == "UnusedFiles" || pageTitle == "UncategorizedFiles")
					matches = site.regexes["linkToImage2"].Matches(src);
				else
					matches = site.regexes["titleLinkShown"].Matches(src);
				if (matches.Count == 0) {
					fallback = true;
				}
				else {
					foreach (Match match in matches)
						pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
				}

				if (fallback) {    // FALLBACK, use alternative parsing way, XPath
					src = Bot.GetXMLSubstring(src, "<!-- bodytext -->", "<!-- /bodytext -->", true);
					XmlNamespaceManager xmlNs = new XmlNamespaceManager(new NameTable());
					xmlNs.AddNamespace("ns", "http://www.w3.org/1999/xhtml");
					XPathNodeIterator ni = Bot.GetXMLIterator(src,
						"//ns:ol/ns:li/ns:a[@title != '']", xmlNs);
					if (ni.Count == 0)
						throw new WikiBotException(string.Format(
							Bot.Msg("Nothing was found on \"Special:{0}\" page."), pageTitle));
					while (ni.MoveNext())
						pages.Add(new Page(site,
							HttpUtility.HtmlDecode(ni.Current.GetAttribute("title", ""))));
				}
			}

			Console.WriteLine(Bot.Msg("PageList has been filled with {0} page titles from " +
				"\"Special:{1}\" page."), this.Count() - preexistingPages, pageTitle);
		}

		/// <summary>Gets page titles for this PageList from specified MediaWiki events log.
		/// The following logs are supported:<b><i>
		/// block, protect, rights, delete, upload, move, import, patrol, merge, suppress,
		/// review, stable, spamblacklist, gblblock, renameuser, globalauth, gblrights,
		/// abusefilter, newusers.</i></b>
		/// The function does not filter namespaces and does not clear the
		/// existing PageList, so new pages will be added to existing pages.</summary>
		/// <param name="logType">Type of log, it could be: "block" for blocked users log;
		/// "protect" for protected pages log; "rights" for users rights log; "delete" for
		/// deleted pages log; "upload" for uploaded files log; "move" for renamed pages log;
		/// "import" for transwiki import log; "renameuser" for renamed accounts log;
		/// "newusers" for new users log; "makebot" for bot status assignment log.</param>
		/// <param name="userName">Select log entries only for specified account. Pass empty
		/// string, if that restriction is not needed.</param>
		/// <param name="pageTitle">Select log entries only for specified page. Pass empty
		/// string, if that restriction is not needed.</param>
		/// <param name="limit">Maximum number of page titles to get.</param>
		public void FillFromCustomLog(string logType, string userName, string pageTitle,
			int limit)
		{
			if (string.IsNullOrEmpty(logType))
				throw new ArgumentNullException("logType");
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit");
			Console.WriteLine(Bot.Msg("Getting {0} page titles from \"{1}\" log..."),
				limit.ToString(), logType);

			int preexistingPages = this.Count();

			if (site.useApi) {
				var queryXml =
					from el in Bot.commonDataXml.Element("ApiOptions").Descendants("query")
					where el.Value == logType
					select el;
				if (queryXml == null)
					throw new WikiBotException(
						string.Format(Bot.Msg("The log \"{0}\" is not supported."), logType));
				string parameters = "letype=" + logType;
				if (!string.IsNullOrEmpty(userName))
					parameters += "&leuser=" + Bot.UrlEncode(userName);
				if (!string.IsNullOrEmpty(pageTitle))
					parameters += "&letitle=" + Bot.UrlEncode(pageTitle);
				FillFromCustomApiQuery("list=logevents", parameters, limit);
			}
			else {
				// TO DO: paging
				string res = site.indexPath + "?title=Special:Log&type=" +
					 logType + "&user=" + Bot.UrlEncode(userName) + "&page=" +
					 Bot.UrlEncode(pageTitle) + "&limit=" + limit.ToString();
				string src = site.GetWebPage(res);
				MatchCollection matches = site.regexes["titleLinkShown"].Matches(src);
				if (matches.Count == 0)
					throw new WikiBotException(string.Format(
						Bot.Msg("Log \"{0}\" does not contain page titles."), logType));
				foreach (Match match in matches)
					pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
			}

			Console.WriteLine(
				Bot.Msg("PageList has been filled with {0} page titles from \"{1}\" log."),
				this.Count() - preexistingPages, logType);
		}

		/// <summary>Gets page titles for this PageList from results of specified custom API query.
		/// Not all queries are supported and can be parsed automatically. The function does not
		/// clear the existing PageList, so new titles will be added to existing.</summary>
		/// <param name="query">Type of query, e.g. "list=allusers" or "list=allpages".</param>
		/// <param name="queryParams">Additional query parameters, specific to the
		/// query, e.g. "cmtitle=Category:Physical%20sciences&amp;cmnamespace=0|2".
		/// Parameter values must be URL-encoded with Bot.UrlEncode() function
		/// before calling this function.</param>
		/// <param name="limit">Maximum number of resultant strings to fetch.</param>
		/// <example><code>
		/// pageList.FillFromCustomApiQuery("list=categorymembers",
		/// 	"cmcategory=Physical%20sciences&amp;cmnamespace=0|14",
		/// 	int.MaxValue);
		/// </code></example>
		public void FillFromCustomApiQuery(string query, string queryParams, int limit)
		{
			var titles = site.GetApiQueryResult(query, queryParams, limit);
			foreach (var title in titles) {
				if (title.ContainsKey("_Target"))
					pages.Add(new Page(site, title["_Target"]));
			}

			// Show message only if the function was called by user, not by other bot function
			if (!string.IsNullOrEmpty(Environment.StackTrace)
				&& !Environment.StackTrace.Contains("FillFrom")
				&& !Environment.StackTrace.Contains("FillAllFrom"))
					Console.WriteLine(string.Format(
						Bot.Msg("PageList has been filled with {0} page " +
							"titles from \"{1}\" bot interface list."), titles.Count, query));
		}

		/// <summary>Gets page titles for this PageList from recent changes page,
		/// "Special:Recentchanges". File uploads, page deletions and page renamings are
		/// not included, use
		/// <see cref="PageList.FillFromCustomLog(string,string,string,int)"/>
		/// function instead to fill from respective logs.
		/// The function does not clear the existing PageList, so new titles will be added.
		/// Use <see cref="PageList.FilterNamespaces(int[])"/> or
		/// <see cref="PageList.RemoveNamespaces(int[])"/> functions to remove
		/// pages from unwanted namespaces.</summary>
		/// <param name="hideMinor">Ignore minor edits.</param>
		/// <param name="hideBots">Ignore bot edits.</param>
		/// <param name="hideAnons">Ignore anonymous users edits.</param>
		/// <param name="hideLogged">Ignore logged-in users edits.</param>
		/// <param name="hideSelf">Ignore edits of this bot account.</param>
		/// <param name="hideReviewed">Ignore checked edits.</param>
		/// <param name="limit">Maximum number of changes to get.</param>
		/// <param name="days">Get changes for this number of recent days.</param>
		public void FillFromRecentChanges(bool hideMinor, bool hideBots, bool hideAnons,
			bool hideLogged, bool hideSelf, bool hideReviewed, int limit, int days)
		{
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
			if (days <= 0)
				throw new ArgumentOutOfRangeException("days",
					Bot.Msg("Number of days must be positive."));
			Console.WriteLine(Bot.Msg("Getting {0} page titles from " +
				"\"Special:Recentchanges\" page..."), limit);
			string uri = string.Format("{0}?title=Special:Recentchanges" +
				"&hideminor={1}&hidebots={2}&hideanons={3}&hideliu={4}&hidemyself={5}" +
				"&hideReviewed={6}&limit={7}&days={8}",
				site.indexPath,
				hideMinor ? "1" : "0",
				hideBots ? "1" : "0",
				hideAnons ? "1" : "0",
				hideLogged ? "1" : "0",
				hideSelf ? "1" : "0",
				hideReviewed ? "1" : "0",
				limit.ToString(),
				days.ToString());
			string respStr = site.GetWebPage(uri);
			MatchCollection matches = site.regexes["titleLinkShown"].Matches(respStr);
			foreach (Match match in matches)
				pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
			Console.WriteLine(Bot.Msg("PageList has been filled with {0} page titles from " +
				"\"Special:Recentchanges\" page."), matches.Count);
		}

		/// <summary>Fills this PageList with pages from specified category page. Subcategories are
		/// not included, call <see cref="PageList.FillAllFromCategory(string)"/> function instead
		/// to get category contents with subcategories.</summary>
		/// <param name="categoryName">Category name, with or without namespace prefix.</param>
		public void FillFromCategory(string categoryName)
		{
			int count = pages.Count;
			PageList pl = new PageList(site);
			pl.FillAllFromCategory(categoryName);
			pl.RemoveNamespaces(new int[] {14});
			pages.AddRange(pl.pages);
			if (pages.Count != count)
				Console.WriteLine(
					Bot.Msg("PageList has been filled with {0} page titles found in \"{1}\"" +
						" category."), (pages.Count - count).ToString(), categoryName);
			else
				Console.Error.WriteLine(
					Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);
		}

		/// <summary>This function fills this PageList with pages from specified
		/// category page, subcategories are also included.</summary>
		/// <param name="categoryName">Category name, with or without prefix.</param>
		public void FillAllFromCategory(string categoryName)
		{
			if (string.IsNullOrEmpty(categoryName))
				throw new ArgumentNullException("categoryName");
			categoryName = categoryName.Trim("[]\f\n\r\t\v ".ToCharArray());
			categoryName = site.RemoveNsPrefix(categoryName, 14);
			categoryName = site.GetNsPrefix(14) + categoryName;
			Console.WriteLine(Bot.Msg("Getting category \"{0}\" contents..."), categoryName);
			//RemoveAll();
			if (site.useApi) {
				FillFromCustomApiQuery("list=categorymembers", "cmtitle=" +
					Bot.UrlEncode(categoryName), int.MaxValue);
			}
			else {    // TO DO: paging
				string src = "";
				MatchCollection matches;
				Regex nextPortionRegex = new Regex("&(?:amp;)?from=([^\"=]+)\" title=\"");
				do {
					string res = site.indexPath + "?title=" +
						Bot.UrlEncode(categoryName) +
						"&from=" + nextPortionRegex.Match(src).Groups[1].Value;
					src = site.GetWebPage(res);
					src = Bot.GetSubstring(src,
						" id=\"mw-subcategories\"", " id=\"mw-normal-catlinks\"");
					string relativeIndexPath =
						site.indexPath.Substring(site.indexPath.IndexOf('/', 10));
					Regex linkRegex = new Regex(" href=\"(?:" +
						(!string.IsNullOrEmpty(site.shortPath) ?
							Regex.Escape(site.shortPath) + "|" : "") +
						Regex.Escape(relativeIndexPath) + "\\?title=)" +
						"(?<title>[^\"]+)");
					matches = linkRegex.Matches(src);
					foreach (Match match in matches)
						pages.Add(new Page(site,
							HttpUtility.UrlDecode(match.Groups["title"].Value)));
				}
				while (nextPortionRegex.IsMatch(src));
			}
		}

		/// <summary>Gets all levels of subcategories of some wiki category (that means
		/// subcategories, sub-subcategories, and so on) and fills this PageList with titles
		/// of all pages, found in all levels of subcategories. The multiplicates of recurring pages
		/// are removed. Subcategory pages are excluded from resultant list, call
		/// <see cref="PageList.FillAllFromCategoryTree(string)"/> function instead to get PageList
		/// with subcategories on board.
		/// This operation may be very time-consuming and traffic-consuming.
		/// The function clears the PageList before filling begins.</summary>
		/// <param name="categoryName">Category name, with or without prefix.</param>
		public void FillFromCategoryTree(string categoryName)
		{
			FillAllFromCategoryTree(categoryName);
			RemoveNamespaces(new int[] {14});
			if (pages.Count != 0)
				Console.WriteLine(
					Bot.Msg("PageList has been filled with {0} page titles found in \"{1}\"" +
						" category."), Count().ToString(), categoryName);
			else
				Console.Error.WriteLine(
					Bot.Msg("Nothing was found in \"{0}\" category."), categoryName);
		}

		/// <summary>Gets all levels of subcategories of some wiki category (that means
		/// subcategories, sub-subcategories, and so on) and fills this PageList with titles
		/// of all pages, found in all levels of subcategories. The multiplicates of recurring pages
		/// are removed. Subcategory pages are included.
		/// This operation may be very time-consuming and traffic-consuming.
		/// The function clears the PageList before filling begins.</summary>
		/// <param name="categoryName">Category name, with or without prefix.</param>
		public void FillAllFromCategoryTree(string categoryName)
		{
			Clear();
			categoryName = site.CorrectNsPrefix(categoryName);
			List<string> doneCats = new List<string>();
			FillAllFromCategory(categoryName);
			doneCats.Add(categoryName);
			for (int i = 0; i < Count(); i++)
				if (pages[i].GetNamespace() == 14 && !doneCats.Contains(pages[i].title)) {
					FillAllFromCategory(pages[i].title);
					doneCats.Add(pages[i].title);
				}
			RemoveRecurring();
		}

		/// <summary>Gets page history and fills this PageList with specified number of recent page
		/// revisions. Pre-existing pages will be removed from this PageList.
		/// Only revision identifiers, user names, timestamps and comments are
		/// loaded, not the texts. Call <see cref="PageList.Load()"/> to load the texts of page
		/// revisions. PageList[0] will be the most recent revision.</summary>
		/// <param name="pageTitle">Page to get history of.</param>
		/// <param name="limit">Number of last page revisions to get.</param>
		public void FillFromPageHistory(string pageTitle, int limit)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit");
			Console.WriteLine(
				Bot.Msg("Getting {0} last revisons of \"{1}\" page..."), limit, pageTitle);

			Clear();    // remove pre-existing pages

			if (site.useApi) {
				string queryUri = site.apiPath + "?action=query&prop=revisions&titles=" +
					Bot.UrlEncode(pageTitle) + "&rvprop=ids|user|comment|timestamp" +
					"&format=xml&rvlimit=" + limit.ToString();
				string src = site.GetWebPage(queryUri);
				Page p;
				using (XmlReader reader = XmlReader.Create(new StringReader(src))) {
					reader.ReadToFollowing("api");
					reader.Read();
					if (reader.Name == "error")
						Console.Error.WriteLine(Bot.Msg("Error: {0}"), reader.GetAttribute("info"));
					while (reader.ReadToFollowing("rev")) {
						p = new Page(site, pageTitle);
						p.revision = reader.GetAttribute("revid");
						p.lastUser = reader.GetAttribute("user");
						p.comment = reader.GetAttribute("comment");
						p.timestamp =
							DateTime.Parse(reader.GetAttribute("timestamp")).ToUniversalTime();
						pages.Add(p);
					}
				}
			}
			else {
				// TO DO: paging
				string res = site.indexPath + "?title=" +
					Bot.UrlEncode(pageTitle) + "&limit=" + limit.ToString() +
						"&action=history";
				string src = site.GetWebPage(res);
				src = src.Substring(src.IndexOf("<ul id=\"pagehistory\">"));
				src = src.Substring(0, src.IndexOf("</ul>") + 5);
				Page p = null;
				using (XmlReader reader = Bot.GetXMLReader(src)) {
					while (reader.Read()) {
						if (reader.Name == "li" && reader.NodeType == XmlNodeType.Element) {
							p = new Page(site, pageTitle);
							p.lastMinorEdit = false;
							p.comment = "";
						}
						else if (reader.Name == "span"
							&& reader["class"] == "mw-history-histlinks") {
								reader.ReadToFollowing("a");
								p.revision = reader["href"].Substring(
									reader["href"].IndexOf("oldid=") + 6);
								DateTime.TryParse(reader.ReadString(),
									site.regCulture, DateTimeStyles.AssumeLocal, out p.timestamp);
						}
						else if (reader.Name == "span" && reader["class"] == "history-user") {
							reader.ReadToFollowing("a");
							p.lastUser = reader.ReadString();
						}
						else if (reader.Name == "abbr")
							p.lastMinorEdit = true;
						else if (reader.Name == "span" && reader["class"] == "history-size")
							int.TryParse(Regex.Replace(reader.ReadString(), @"[^-+\d]", ""),
								out p.lastBytesModified);
						else if (reader.Name == "span" && reader["class"] == "comment") {
							p.comment = Regex.Replace(reader.ReadInnerXml().Trim(), "<.+?>", "");
							p.comment = p.comment.Substring(1, p.comment.Length - 2);    // brackets
						}
						if (reader.Name == "li" && reader.NodeType == XmlNodeType.EndElement)
							pages.Add(p);
					}
				}
			}

			Console.WriteLine(
				Bot.Msg("PageList has been filled with {0} last revisons of \"{1}\" page..."),
				pages.Count, pageTitle);
		}

		/// <summary>Gets page titles for this PageList from links in some wiki page. All links
		/// will be retrieved, from all namespaces, except interwiki links to other
		/// sites. Use <see cref="PageList.FilterNamespaces(int[])"/> or
		/// <see cref="PageList.RemoveNamespaces(int[])"/> function to remove pages from
		/// unwanted namespaces (categories, images, etc.)</summary>
		/// <param name="pageTitle">Page title as string.</param>
		/// <example><code>pageList.FillFromAllPageLinks("Art");</code></example>
		public void FillFromPageLinks(string pageTitle)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			Regex wikiLinkRegex = new Regex(@"\[\[ *:*(.+?)(]]|\|)");
			Page page = new Page(site, pageTitle);
			page.Load();
			MatchCollection matches = wikiLinkRegex.Matches(page.text);
			Regex outWikiLink = new Regex("^(" + site.generalData["interwiki"] + "):");
			foreach (Match match in matches) {
				string link = match.Groups[1].Value;
				if (outWikiLink.IsMatch(link))
					continue;
				if (link[0] == '/')    // relative link
					link = pageTitle + link;
				if (link.Contains('_'))
					link = link.Replace(' ', '_');
				if (!this.Contains(link))
					pages.Add(new Page(site, link));
			}
			Console.WriteLine(
				Bot.Msg("PageList has been filled with links found on \"{0}\" page."), pageTitle);
		}

		/// <summary>Gets titles of pages which link to specified page. Results include redirects,
		/// call <see cref="PageList.RemoveRedirects()"/> to get rid of them. The
		/// function does not clear the existing PageList, so new titles will be added.</summary>
		/// <param name="pageTitle">Page title as string.</param>
		public void FillFromLinksToPage(string pageTitle)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			FillFromCustomApiQuery("list=backlinks",
				"bltitle=" + Bot.UrlEncode(pageTitle), int.MaxValue);
			Console.WriteLine(
				Bot.Msg("PageList has been filled with titles of pages referring to \"{0}\" page."),
				pageTitle);
		}

		/// <summary>Gets titles of pages which transclude (embed) the specified page. The function
		/// does not clear the existing PageList, so new titles will be added.</summary>
		/// <param name="pageTitle">Page title as string.</param>
		public void FillFromTransclusionsOfPage(string pageTitle)
		{
			if (string.IsNullOrEmpty(pageTitle))
				throw new ArgumentNullException("pageTitle");
			if (site.useApi) {
				FillFromCustomApiQuery("list=embeddedin", "eititle=" +
					Bot.UrlEncode(pageTitle), int.MaxValue);
			}
			else {    // TO DO: paging
				string res = site.indexPath + "?title=Special:Whatlinkshere/" +
					Bot.UrlEncode(pageTitle) + "&limit=5000&hidelinks=1&hideredirs=1";
				string src = site.GetWebPage(res);
				src = Bot.GetSubstring(src,
					" id=\"mw-whatlinkshere-list\"", " class=\"printfooter\"");
				MatchCollection matches = site.regexes["titleLinkInList"].Matches(src);
				foreach (Match match in matches)
					pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups["title"].Value)));
				Console.WriteLine(Bot.Msg("PageList has been filled with titles of pages, which" +
					" transclude \"{0}\" page."), pageTitle);
			}
		}

		/// <summary>Gets titles of pages, in which the specified image file is included.
		/// Function also works with non-image files.</summary>
		/// <param name="imageFileTitle">File title. With or without "Image:" or
		/// "File:" prefix.</param>
		public void FillFromPagesUsingImage(string imageFileTitle)
		{
			if (string.IsNullOrEmpty(imageFileTitle))
				throw new ArgumentNullException("imageFileTitle");
			int pagesCount = Count();
			imageFileTitle = site.RemoveNsPrefix(imageFileTitle, 6);

			if (site.useApi) {
				FillFromCustomApiQuery("list=imageusage", "iutitle=" +
					Bot.UrlEncode(site.GetNsPrefix(6)) +
					Bot.UrlEncode(imageFileTitle), int.MaxValue);
			}
			else {    // TO DO: paging
				string res = site.indexPath + "?title=" +
					Bot.UrlEncode(site.GetNsPrefix(6)) +
					Bot.UrlEncode(imageFileTitle);
				string src = site.GetWebPage(res);
				try {
					src = Bot.GetSubstring(src, "<h2 id=\"filelinks\"", "<h2 id=\"globalusage\"");
				}
				catch (ArgumentOutOfRangeException) {
					Console.Error.WriteLine(
						Bot.Msg("No page contains the image \"{0}\"."), imageFileTitle);
					return;
				}
				MatchCollection matches = site.regexes["titleLink"].Matches(src);
				foreach (Match match in matches)
					pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups["title"].Value)));
			}
			if (pagesCount == Count())
				Console.Error.WriteLine(Bot.Msg("No page contains the image \"{0}\"."),
					imageFileTitle);
			else
				Console.WriteLine(
					Bot.Msg("PageList has been filled with titles of pages containing \"{0}\"" +
						" image."), imageFileTitle);
		}

		/// <summary>Gets page titles for this PageList from user contributions
		/// of specified user. The function does not remove redirecting
		/// pages from the results. Call <see cref="PageList.RemoveRedirects()"/> manually,
		/// if you require it. And the function does not clears the existing PageList,
		/// so new titles will be added.</summary>
		/// <param name="userName">User's name.</param>
		/// <param name="limit">Maximum number of page titles to get.</param>
		public void FillFromUserContributions(string userName, int limit)
		{
			if (string.IsNullOrEmpty(userName))
				throw new ArgumentNullException("userName");
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
			string res = site.indexPath +
				"?title=Special:Contributions&target=" + Bot.UrlEncode(userName) +
				"&limit=" + limit.ToString();
			string src = site.GetWebPage(res);
			MatchCollection matches = site.regexes["titleLinkShown"].Matches(src);
			foreach (Match match in matches)
				pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
			Console.WriteLine(
				Bot.Msg("PageList has been filled with user \"{0}\"'s contributions."), userName);
		}

		/// <summary>Gets page titles for this PageList from watchlist
		/// of bot account. The function does not remove redirecting
		/// pages from the results. Call <see cref="PageList.RemoveRedirects()"/> manually,
		/// if you need that. And the function neither filters namespaces, nor clears the
		/// existing PageList, so new titles will be added to the existing in PageList.</summary>
		public void FillFromWatchList()
		{
			string src = site.GetWebPage(site.indexPath + "?title=Special:Watchlist/edit");
			MatchCollection matches = site.regexes["titleLinkShown"].Matches(src);
			if (site.watchList == null)
				site.watchList = new PageList(site);
			else
				site.watchList.Clear();
			foreach (Match match in matches) {
				string title = HttpUtility.HtmlDecode(match.Groups[1].Value);
				pages.Add(new Page(site, title));
				site.watchList.Add(new Page(site, title));
			}
			Console.WriteLine(Bot.Msg("PageList has been filled with bot account's watchlist."));
		}

		/// <summary>Gets page titles for this PageList from list of recently changed
		/// watched articles (watched by bot account). The function does not internally
		/// remove redirecting pages from the results. Call <see cref="PageList.RemoveRedirects()"/>
		/// manually, if you need it. And the function neither filters namespaces, nor clears
		/// the existing PageList, so new titles will be added to the existing
		/// in PageList.</summary>
		public void FillFromChangedWatchedPages()
		{
			string src = site.GetWebPage(site.indexPath + "?title=Special:Watchlist/edit");
			MatchCollection matches = site.regexes["titleLinkShown"].Matches(src);
			foreach (Match match in matches)
				pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups[1].Value)));
			Console.WriteLine(
				Bot.Msg("PageList has been filled with changed pages from bot account's" +
					" watchlist."));
		}

		/// <summary>Gets page titles for this PageList from site's internal search results.
		/// The function does not filter namespaces. And the function does not clear
		/// the existing PageList, so new titles will be added.</summary>
		/// <param name="searchStr">String to search.</param>
		/// <param name="limit">Maximum number of page titles to get.</param>
		public void FillFromSearchResults(string searchStr, int limit)
		{
			if (string.IsNullOrEmpty(searchStr))
				throw new ArgumentNullException("searchStr");
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
			if (site.useApi) {
				FillFromCustomApiQuery("list=search", "srsearch=" +
					Bot.UrlEncode(searchStr), limit);
			}
			else {    // TO DO: paging
				string res = site.indexPath + "?title=Special:Search&fulltext=Search&search=" +
					Bot.UrlEncode(searchStr) + "&limit=" + limit.ToString();
				string src = site.GetWebPage(res);
				src = Bot.GetSubstring(src, "<ul class='mw-search-results'>", "</ul>");
				MatchCollection matches = site.regexes["titleLink"].Matches(src);
				foreach (Match match in matches)
					pages.Add(new Page(site, HttpUtility.HtmlDecode(match.Groups["title"].Value)));
			}
			Console.WriteLine(Bot.Msg("PageList has been filled with search results."));
		}

		/// <summary>Gets page titles for this PageList from Google search results.
		/// The function gets pages of all namespaces and it does not clear
		/// the existing PageList, so new pages will be added.</summary>
		/// <param name="searchStr">Words to search for. Use quotes to find exact phrases.</param>
		/// <param name="limit">Maximum number of page titles to get.</param>
		public void FillFromGoogleSearchResults(string searchStr, int limit)
		{
			if (string.IsNullOrEmpty(searchStr))
				throw new ArgumentNullException("searchStr");
			if (limit <= 0)
				throw new ArgumentOutOfRangeException("limit", Bot.Msg("Limit must be positive."));
			// TO DO: paging
			Uri res = new Uri("http://www.google.com/search?q=" + Bot.UrlEncode(searchStr) +
				"+site:" + site.address.Substring(site.address.IndexOf("://") + 3) +
				"&num=" + limit.ToString());
			string src = Bot.GetWebResource(res, "");
			string relativeIndexPath = site.indexPath.Substring(site.indexPath.IndexOf('/', 10));
			string googleLinkToPagePattern = "<h3[^>]*><a href=\"(?<double_escape>/url\\?q=)?" +
				Regex.Escape(site.address).Replace("https:", "https?:") + "(?:" +
				(!string.IsNullOrEmpty(site.shortPath) ?
					Regex.Escape(site.shortPath) + "|" : "") +
				Regex.Escape(relativeIndexPath) + "\\?title=)?" + "(?<title>[^&\"]+)";
			Regex GoogleLinkToPageRegex = new Regex(googleLinkToPagePattern);
			MatchCollection matches = GoogleLinkToPageRegex.Matches(src);
			foreach (Match match in matches) {
				string title = HttpUtility.UrlDecode(match.Groups["title"].Value);
				if (title == "/") {
					if (site.messages == null)
						site.LoadMediawikiMessages(true);
					string mainPageTitle = site.messages["mainpage"];
					Page p = new Page(site, mainPageTitle);
					p.ResolveRedirect();
					pages.Add(p);
				}
				else {
					if (!string.IsNullOrEmpty(match.Groups["double_escape"].Value))
						title = HttpUtility.UrlDecode(title);
					pages.Add(new Page(site, title));
				}
			}
			Console.WriteLine(
				Bot.Msg("PageList has been filled with www.google.com search results."));
		}

		/// <summary>Gets page titles from UTF8-encoded file. Each title must be on a new line.
		/// The function does not clear the existing PageList, new pages will be added.</summary>
		/// <param name="filePathName">Full file path and name.</param>
		public void FillFromFile(string filePathName)
		{
			//RemoveAll();
			StreamReader strmReader = new StreamReader(filePathName);
			string input;
			while ((input = strmReader.ReadLine()) != null) {
				input = input.Trim(" []".ToCharArray());
				if (string.IsNullOrEmpty(input) != true)
					pages.Add(new Page(site, input));
			}
			strmReader.Close();
			Console.WriteLine(
				Bot.Msg("PageList has been filled with titles found in \"{0}\" file."),
					filePathName);
		}

		/// <summary>Protects or unprotects all pages in this PageList, so only chosen category
		/// of users can edit or rename it. Changing page protection modes requires administrator
		/// (sysop) rights on target wiki.</summary>
		/// <param name="editMode">Protection mode for editing this page (0 = everyone allowed
		/// to edit, 1 = only registered users are allowed, 2 = only administrators are allowed 
		/// to edit).</param>
		/// <param name="renameMode">Protection mode for renaming this page (0 = everyone allowed to
		/// rename, 1 = only registered users are allowed, 2 = only administrators
		/// are allowed).</param>
		/// <param name="cascadeMode">In cascading mode, all the pages, included into this page
		/// (e.g., templates or images) are also fully automatically protected.</param>
		/// <param name="expiryDate">Date ant time, expressed in UTC, when the protection expires
		/// and page becomes fully unprotected. Use DateTime.ToUniversalTime() method to convert
		/// local time to UTC, if necessary. Pass DateTime.MinValue to make protection
		/// indefinite.</param>
		/// <param name="reason">Reason for protecting this page.</param>
		public void Protect(int editMode, int renameMode, bool cascadeMode,
			DateTime expiryDate, string reason)
		{
			if (IsEmpty())
				throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to protect."));
			foreach (Page p in pages)
				p.Protect(editMode, renameMode, cascadeMode, expiryDate, reason);
		}

		/// <summary>Adds all pages in this PageList to bot account's watchlist.</summary>
		public void Watch()
		{
			if (IsEmpty())
				throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to watch."));
			foreach (Page p in pages)
				p.Watch();
		}

		/// <summary>Removes all pages in this PageList from bot account's watchlist.</summary>
		public void Unwatch()
		{
			if (IsEmpty())
				throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to unwatch."));
			foreach (Page p in pages)
				p.Unwatch();
		}

		/// <summary>Removes pages that are not in the given namespaces.</summary>
		/// <param name="neededNSs">Array of integers, presenting keys of namespaces
		/// to retain.</param>
		/// <example><code>pageList.FilterNamespaces(new int[] {0,3});</code></example>
		public void FilterNamespaces(int[] neededNSs)
		{
			for (int i=pages.Count-1; i >= 0; i--) {
				if (Array.IndexOf(neededNSs, pages[i].GetNamespace()) == -1)
					pages.RemoveAt(i); }
		}

		/// <summary>Removes the pages, that are in given namespaces.</summary>
		/// <param name="needlessNSs">Array of integers, presenting keys of namespaces
		/// to remove.</param>
		/// <example><code>pageList.RemoveNamespaces(new int[] {2,4});</code></example>
		public void RemoveNamespaces(int[] needlessNSs)
		{
			for (int i=pages.Count-1; i >= 0; i--) {
				if (Array.IndexOf(needlessNSs, pages[i].GetNamespace()) != -1)
					pages.RemoveAt(i); }
		}

		/// <summary>This function sorts all pages in PageList by titles.</summary>
		public void Sort()
		{
			if (IsEmpty())
				throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to sort."));
			pages.Sort(ComparePagesByTitles);
		}

		/// <summary>Compares pages by titles in language-specific manner. This is required to
		/// compare titles in Japanese, Chinese, etc. properly</summary>
		/// <param name="x">First page.</param>
		/// <param name="y">Second page.</param>
		/// <returns>Returns 1 if x is greater (alphabetically), -1 if y is greater, 0 if equal.
		/// </returns>
		public int ComparePagesByTitles(Page x, Page y)
		{
			int r = string.Compare(x.title, y.title, false, site.langCulture);
			return (r != 0) ? r/Math.Abs(r) : 0;
		}

		/// <summary>Removes all pages in PageList from specified category by deleting
		/// links to that category in pages texts.</summary>
		/// <param name="categoryName">Category name, with or without prefix.</param>
		public void RemoveFromCategory(string categoryName)
		{
			foreach (Page p in pages)
				p.RemoveFromCategory(categoryName);
		}

		/// <summary>Adds all pages in PageList to the specified category by adding
		/// links to that category in pages texts.</summary>
		/// <param name="categoryName">Category name, with or without prefix.</param>
		public void AddToCategory(string categoryName)
		{
			foreach (Page p in pages)
				p.AddToCategory(categoryName);
		}

		/// <summary>Adds a specified template to the end of all pages in PageList.</summary>
		/// <param name="templateText">Template text, like "{{template_name|...|...}}".</param>
		public void AddTemplate(string templateText)
		{
			foreach (Page p in pages)
				p.AddTemplate(templateText);
		}

		/// <summary>Removes a specified template from all pages in PageList.</summary>
		/// <param name="templateTitle">Title of template  to remove.</param>
		public void RemoveTemplate(string templateTitle)
		{
			foreach (Page p in pages)
				p.RemoveTemplate(templateTitle);
		}

		/// <summary>Loads text for pages in PageList from site via common web interface.
		/// Please, don't use this function when going to edit big amounts of pages on
		/// popular public wikis, as it compromises edit conflict detection. In that case,
		/// each page's text should be loaded individually right before its processing
		/// and saving.</summary>
		public void Load()
		{
			if (IsEmpty())
				throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to load."));
			foreach (Page page in pages)
				page.Load();
		}

		/// <summary>Loads texts and metadata (revision ID, timestamp, last comment,
		/// last contributor, minor edit mark) for pages in this PageList.
		/// Non-existent pages will be automatically removed from the PageList.
		/// Please, don't use this function when going to edit big amount of pages on
		/// popular public wikis, as it compromises edit conflict detection. In that case,
		/// each page's text should be loaded individually right before its processing
		/// and saving.</summary>
		/// <exclude/>
		public void LoadWithMetadata()
		{
			if (IsEmpty())
				throw new WikiBotException(Bot.Msg("The PageList is empty. Nothing to load."));
			Console.WriteLine(Bot.Msg("Loading {0} pages..."), pages.Count);

			string res = site.indexPath + "?title=Special:Export&action=submit";
			string postData = "curonly=True&pages=";
			foreach (Page page in pages)
				postData += Bot.UrlEncode(page.title) + "\r\n";
			string src = site.PostDataAndGetResult(res, postData);
			XmlReader reader = XmlReader.Create(new StringReader(src));
			PageList pl = new PageList(site);
			while (reader.ReadToFollowing("page")) {
				Page p = new Page(site);
				p.ParsePageXml(reader.ReadOuterXml());
				pl.Add(p);
			}
			reader.Close();
			if (pages.Count > 0) {
				Clear();
				pages = pl.pages;
				return;
			}
			else {    // FALLBACK, use alternative parsing way, XPath
				Console.WriteLine(
					Bot.Msg("XML parsing failed, switching to alternative parser..."), pages.Count);
				src = Bot.RemoveXMLRootAttributes(src);
				StringReader strReader = new StringReader(src);
				XPathDocument doc = new XPathDocument(strReader);
				strReader.Close();
				XPathNavigator nav = doc.CreateNavigator();
				foreach (Page page in pages) {
					if (page.title.Contains("'")) {    // There's no good way to escape "'" in XPath
						page.LoadWithMetadata();
						continue;
					}
					string query = "//page[title='" + page.title + "']/";
					try {
						page.text =
							nav.SelectSingleNode(query + "revision/text").InnerXml;
					}
					catch (System.NullReferenceException) {
						continue;
					}
					page.text = HttpUtility.HtmlDecode(page.text);
					page.pageId = nav.SelectSingleNode(query + "id").InnerXml;
					try {
						page.lastUser = nav.SelectSingleNode(query +
							"revision/contributor/username").InnerXml;
						page.lastUserId = nav.SelectSingleNode(query +
							"revision/contributor/id").InnerXml;
					}
					catch (System.NullReferenceException) {
						page.lastUser = nav.SelectSingleNode(query +
							"revision/contributor/ip").InnerXml;
					}
					page.lastUser = HttpUtility.HtmlDecode(page.lastUser);
					page.revision = nav.SelectSingleNode(query + "revision/id").InnerXml;
					page.lastMinorEdit = (nav.SelectSingleNode(query +
						"revision/minor") == null) ? false : true;
					try {
						page.comment = nav.SelectSingleNode(query + "revision/comment").InnerXml;
						page.comment = HttpUtility.HtmlDecode(page.comment);
					}
					catch (System.NullReferenceException) {;}
					page.timestamp =
						nav.SelectSingleNode(query + "revision/timestamp").ValueAsDateTime;
				}

				if (string.IsNullOrEmpty(pages[0].text)) {    // FALLBACK 2, load pages one-by-one
					foreach (Page page in pages)
						page.LoadWithMetadata();
				}
			}
		}

		/// <summary>Gets page titles and page text from local XML dump.
		/// This function consumes much resources.</summary>
		/// <param name="filePathName">The path to and name of the XML dump file as string.</param>
		public void FillAndLoadFromXmlDump(string filePathName)
		{
			Console.WriteLine(Bot.Msg("Loading pages from XML dump..."));
			XmlReader reader = XmlReader.Create(filePathName);
			while (reader.ReadToFollowing("page")) {
				Page p = new Page(site);
				p.ParsePageXml(reader.ReadOuterXml());
				pages.Add(p);
			}
			reader.Close();
			Console.WriteLine(Bot.Msg("XML dump has been loaded successfully."));
		}

		/// <summary>Gets page titles and page texts from all ".txt" files in the specified
		/// directory (folder). Each file becomes a page. Page titles are constructed from
		/// file names. Page text is read from file contents. If any Unicode numeric codes
		/// (also known as numeric character references or NCRs) of the forbidden characters
		/// (forbidden in filenames) are recognized in filenames, those codes are converted
		/// to characters (e.g. "&#x7c;" is converted to "|").</summary>
		/// <param name="dirPath">The path and name of a directory (folder)
		/// to load files from.</param>
		public void FillAndLoadFromFiles(string dirPath)
		{
			foreach (string fileName in Directory.GetFiles(dirPath, "*.txt")) {
				Page p = new Page(site, Path.GetFileNameWithoutExtension(fileName));
				p.title = p.title.Replace("&#x22;", "\"");
				p.title = p.title.Replace("&#x3c;", "<");
				p.title = p.title.Replace("&#x3e;", ">");
				p.title = p.title.Replace("&#x3f;", "?");
				p.title = p.title.Replace("&#x3a;", ":");
				p.title = p.title.Replace("&#x5c;", "\\");
				p.title = p.title.Replace("&#x2f;", "/");
				p.title = p.title.Replace("&#x2a;", "*");
				p.title = p.title.Replace("&#x7c;", "|");
				p.LoadFromFile(fileName);
				pages.Add(p);
			}
		}

		/// <summary>Saves all pages in PageList to live wiki site. Uses 
		/// <see cref="Site.defaultEditComment"/> and <see cref="Site.minorEditByDefault"/>
		/// settings. This function doesn't limit the saving speed, so in case of working on
		/// public wiki, it's better to use <see cref="PageList.SaveSmoothly()"/> function in order
		/// to decrease server load.</summary>
		public void Save()
		{
			Save(site.defaultEditComment, site.minorEditByDefault);
		}

		/// <summary>Saves all pages in PageList to live wiki site. This function
		/// doesn't limit the saving speed, so in case of working on public wiki it's better
		/// to use <see cref="PageList.SaveSmoothly(int,string,bool)"/>
		/// function in order to decrease server load.</summary>
		/// <param name="comment">Your edit comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
		public void Save(string comment, bool isMinorEdit)
		{
			foreach (Page page in pages)
				page.Save(page.text, comment, isMinorEdit);
		}

		/// <summary>Saves all pages in PageList to live wiki site. The function waits for
		/// <see cref="Site.forceSaveDelay"/> seconds (or for 5 seconds if
		/// <see cref="Site.forceSaveDelay"/> equals zero, the default)
		/// between each page save operation in order to decrease server load. It uses
		/// <see cref="Site.defaultEditComment"/> and <see cref="Site.minorEditByDefault"/>
		/// settings.</summary>
		public void SaveSmoothly()
		{
			SaveSmoothly(site.forceSaveDelay > 0 ? site.forceSaveDelay : 5,
				site.defaultEditComment, site.minorEditByDefault);
		}

		/// <summary>Saves all pages in PageList to live wiki site. The function waits for specified
		/// number of seconds between each page save operation in order to decrease server load.
		/// It uses <see cref="Site.defaultEditComment"/> and <see cref="Site.minorEditByDefault"/>
		/// settings.</summary>
		/// <param name="intervalSeconds">Number of seconds to wait between each
		/// save operation.</param>
		public void SaveSmoothly(int intervalSeconds)
		{
			SaveSmoothly(intervalSeconds, site.defaultEditComment, site.minorEditByDefault);
		}

		/// <summary>Saves all pages in PageList to live wiki site. The function waits for specified
		/// number of seconds between each page save operation in order to decrease server load.
		/// </summary>
		/// <param name="intervalSeconds">Number of seconds to wait between each
		/// save operation.</param>
		/// <param name="comment">Edit comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
		public void SaveSmoothly(int intervalSeconds, string comment, bool isMinorEdit)
		{
			foreach (Page page in pages) {
				Bot.Wait(intervalSeconds);
				page.Save(page.text, comment, isMinorEdit);
			}
		}

		/// <summary>Undoes the last edit of every page in this PageList, so every page text reverts
		/// to previous contents. The function doesn't affect other operations
		/// like renaming.</summary>
		/// <param name="comment">Your edit comment.</param>
		/// <param name="isMinorEdit">Minor edit mark (true = minor edit).</param>
		public void Revert(string comment, bool isMinorEdit)
		{
			foreach (Page page in pages)
				page.Revert(comment, isMinorEdit);
		}

		/// <summary>Saves titles of all pages in PageList to the specified file. Each title
		/// on a separate line. If the target file already exists, it is overwritten.</summary>
		/// <param name="filePathName">The path to and name of the target file as string.</param>
		public void SaveTitlesToFile(string filePathName)
		{
			StringBuilder titles = new StringBuilder();
			foreach (Page page in pages)
				titles.Append(page.title + "\r\n");
			File.WriteAllText(filePathName, titles.ToString().Trim(), Encoding.UTF8);
			Console.WriteLine(Bot.Msg("Titles in PageList have been saved to \"{0}\" file."),
				filePathName);
		}

		/// <summary>Saves the contents of all pages in pageList to ".txt" files in specified
		/// directory. Each page is saved to separate file, the name of that file is constructed
		/// from page title. Forbidden characters in filenames are replaced with their
		/// Unicode numeric codes (also known as numeric character references or NCRs).
		/// If the target file already exists, it is overwritten.</summary>
		/// <param name="dirPath">The path and name of a directory (folder)
		/// to save files to.</param>
		public void SaveToFiles(string dirPath)
		{
			string curDirPath = Directory.GetCurrentDirectory();
			if (!Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);
			Directory.SetCurrentDirectory(dirPath);
			foreach (Page page in pages)
				page.SaveToFile();
			Directory.SetCurrentDirectory(curDirPath);
		}

		/// <summary>Loads the contents of all pages in this pageList from live wiki site via XML
		/// export interface and saves the retrieved XML content to the specified file.
		/// The functions just dumps data, it does not load pages in PageList itself,
		/// call <see cref="PageList.Load()"/> or
		/// <see cref="PageList.FillAndLoadFromXmlDump(string)"/> to do that.
		/// Note that on some sites MediaWiki messages
		/// from standard namespace 8 are not available for export.</summary>
		/// <param name="filePathName">The path to and name of the target file.</param>
		public void SaveXmlDumpToFile(string filePathName)
		{
			Console.WriteLine(Bot.Msg("Loading {0} pages for XML dump..."), this.pages.Count);
			string res = site.indexPath + "?title=Special:Export&action=submit";
			string postData = "catname=&curonly=true&action=submit&pages=";
			foreach (Page page in pages)
				postData += Bot.UrlEncode(page.title + "\r\n");
			string rawXML = site.PostDataAndGetResult(res, postData);
			rawXML = Bot.RemoveXMLRootAttributes(rawXML).Replace("\n", "\r\n");
			if (File.Exists(filePathName))
				File.Delete(filePathName);
			FileStream fs = File.Create(filePathName);
			byte[] XMLBytes = new System.Text.UTF8Encoding(true).GetBytes(rawXML);
			fs.Write(XMLBytes, 0, XMLBytes.Length);
			fs.Close();
			Console.WriteLine(
				Bot.Msg("XML dump has been saved to \"{0}\" file."), filePathName);
		}

		/// <summary>Removes all empty pages from PageList. But firstly don't forget to load
		/// the pages from site using pageList.LoadWithMetadata().</summary>
		public void RemoveEmpty()
		{
			for (int i=pages.Count-1; i >= 0; i--)
				if (pages[i].IsEmpty())
					pages.RemoveAt(i);
		}

		/// <summary>Removes all recurring pages from PageList. Only one page with some title will
		/// remain in PageList. This makes all page elements in PageList unique.</summary>
		public void RemoveRecurring()
		{
			for (int i=pages.Count-1; i >= 0; i--)
				for (int j=i-1; j >= 0; j--)
					if (pages[i].title == pages[j].title) {
						pages.RemoveAt(i);
						break;
					}
		}

		/// <summary>Removes all redirecting pages from PageList. But firstly don't forget to load
		/// the pages from site using <see cref="PageList.Load()"/>.</summary>
		public void RemoveRedirects()
		{
			for (int i=pages.Count-1; i >= 0; i--)
				if (pages[i].IsRedirect())
					pages.RemoveAt(i);
		}

		/// <summary>For all redirecting pages in this PageList, this function loads the titles and
		/// texts of redirected-to pages.</summary>
		public void ResolveRedirects()
		{
			foreach (Page page in pages) {
				if (page.IsRedirect() == false)
					continue;
				page.title = page.RedirectsTo();
				page.Load();
			}
		}

		/// <summary>Removes all disambiguation pages from PageList. But firstly don't
		/// forget to load the pages from site using <see cref="PageList.Load()"/>.</summary>
		public void RemoveDisambigs()
		{
			for (int i=pages.Count-1; i >= 0; i--)
				if (pages[i].IsDisambig())
					pages.RemoveAt(i);
		}


		/// <summary>Removes all pages from PageList.</summary>
		public void RemoveAll()
		{
			pages.Clear();
		}

		/// <summary>Removes all pages from PageList.</summary>
		public void Clear()
		{
			pages.Clear();
		}

		/// <summary>Function changes default English namespace prefixes to correct local prefixes
		/// (e.g. for German wiki-sites it changes "Category:..." to "Kategorie:...").</summary>
		public void CorrectNsPrefixes()
		{
			foreach (Page p in pages)
				p.CorrectNsPrefix();
		}

		/// <summary>Shows if there are any Page objects in this PageList.</summary>
		/// <returns>Returns bool value.</returns>
		public bool IsEmpty()
		{
			return (pages.Count == 0) ? true : false;
		}

		/// <summary>Sends titles of all contained pages to console.</summary>
		public void ShowTitles()
		{
			Console.WriteLine("\n" + Bot.Msg("Pages in this PageList:"));
			foreach (Page p in pages)
				Console.WriteLine(p.title);
			Console.WriteLine("\n");
		}

		/// <summary>Sends texts of all contained pages to console.</summary>
		public void ShowTexts()
		{
			Console.WriteLine("\n" + Bot.Msg("Texts of all pages in this PageList:"));
			Console.WriteLine("--------------------------------------------------");
			foreach (Page p in pages) {
				p.ShowText();
				Console.WriteLine("--------------------------------------------------");
			}
			Console.WriteLine("\n");
		}
	}



	/// <summary>Class establishes custom application exceptions.</summary>
	/// <exclude/>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	[Serializable]
	public class WikiBotException : System.Exception
	{
		/// <exclude/>
		public WikiBotException() {}
		/// <exclude/>
		public WikiBotException(string msg) : base(msg) {
			Console.Beep();
			//Console.ForegroundColor = ConsoleColor.Red;
		}
		/// <exclude/>
		public WikiBotException(string msg, System.Exception inner) : base(msg, inner) { }
		/// <exclude/>
		protected WikiBotException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
			: base (info, context) {}
		/// <exclude/>
		~WikiBotException() {}
	}
	/// <summary>Exceptions for handling wiki edit conflicts.</summary>
	/// <exclude/>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class EditConflictException : WikiBotException
	{
		/// <exclude/>
		public EditConflictException() { }
		/// <exclude/>
		public EditConflictException(string msg) : base(msg) { }
		/// <exclude/>
		public EditConflictException(string msg, Exception inner) : base(msg, inner) { }
	}
	/// <summary>Exception for handling errors due to insufficient rights.</summary>
	/// <exclude/>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class InsufficientRightsException : WikiBotException
	{
		/// <exclude/>
		public InsufficientRightsException() { }
		/// <exclude/>
		public InsufficientRightsException(string msg) : base(msg) { }
		/// <exclude/>
		public InsufficientRightsException(string msg, Exception inner) : base(msg, inner) { }
	}
	/// <summary>Exception for handling situations when bot operation is disallowed.</summary>
	/// <exclude/>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class BotDisallowedException : WikiBotException
	{
		/// <exclude/>
		public BotDisallowedException() { }
		/// <exclude/>
		public BotDisallowedException(string msg) : base(msg) { }
		/// <exclude/>
		public BotDisallowedException(string msg, Exception inner) : base(msg, inner) { }
	}

	/// <summary>Class defines custom XML URL resolver, that has a caching capability. See
	/// <see href="http://www.w3.org/blog/systeam/2008/02/08/w3c_s_excessive_dtd_traffic">this page</see>
	/// for details.</summary>
	/// <exclude/>
	//[PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	class XmlUrlResolverWithCache : XmlUrlResolver
	{
		/// <summary>List of cached files absolute URIs.</summary>
		static string[] cachedFilesURIs = {
			"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-lat1.ent",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-symbol.ent",
			"http://www.w3.org/TR/xhtml1/DTD/xhtml-special.ent"
		};
		/// <summary>List of cached files names.</summary>
		static string[] cachedFiles = {
			"xhtml1-transitional.dtd",
			"xhtml-lat1.ent",
			"xhtml-symbol.ent",
			"xhtml-special.ent"
		};
		/// <summary>Local cache directory.</summary>
		static string cacheDir = Bot.cacheDir + Path.DirectorySeparatorChar;

		/// <summary>Overriding GetEntity() function to implement local cache.</summary>
		/// <param name="absoluteUri">Absolute URI of requested entity.</param>
		/// <param name="role">User's role for accessing specified URI.</param>
		/// <param name="ofObjectToReturn">Type of object to return.</param>
		/// <returns>Returns object or requested type.</returns>
		public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
		{
			if (absoluteUri.ToString().EndsWith("-/W3C/DTD XHTML 1.0 Transitional/EN"))
				return new FileStream(XmlUrlResolverWithCache.cacheDir + "xhtml1-transitional.dtd",
					FileMode.Open, FileAccess.Read, FileShare.Read);
			if (absoluteUri.ToString().EndsWith("-//W3C//ENTITIES Latin 1 for XHTML//EN"))
				return new FileStream(XmlUrlResolverWithCache.cacheDir + "xhtml-lat1.ent",
					FileMode.Open, FileAccess.Read, FileShare.Read);
			if (absoluteUri.ToString().EndsWith("-//W3C//ENTITIES Symbols for XHTML//EN"))
				return new FileStream(XmlUrlResolverWithCache.cacheDir + "xhtml-symbol.ent",
					FileMode.Open, FileAccess.Read, FileShare.Read);
			if (absoluteUri.ToString().EndsWith("-//W3C//ENTITIES Special for XHTML//EN"))
				return new FileStream(XmlUrlResolverWithCache.cacheDir + "xhtml-special.ent",
					FileMode.Open, FileAccess.Read, FileShare.Read);
			for (int i = 0; i < XmlUrlResolverWithCache.cachedFilesURIs.Length; i++)
				if (absoluteUri.ToString().EndsWith(XmlUrlResolverWithCache.cachedFiles[i]))
					return new FileStream(XmlUrlResolverWithCache.cacheDir +
						XmlUrlResolverWithCache.cachedFiles[i],
						FileMode.Open, FileAccess.Read, FileShare.Read);
			return base.GetEntity(absoluteUri, role, ofObjectToReturn);
		}
	}



	/// <summary>Class defines a Bot object, it contains most general configuration settings
	/// and some auxiliary functions.</summary>
	[ClassInterface(ClassInterfaceType.AutoDispatch)]
	public class Bot
	{
		/// <summary>Title and description of this bot as a web agent.</summary>
		public static readonly string botVer = "DotNetWikiBot";
		/// <summary>Version of DotNetWikiBot Framework.</summary>
		public static readonly Version version = new Version("3.15");
		/// <summary>Local cache directory. Adjust it if required.</summary>
		public static string cacheDir;
		/// <summary>Last Site object constructed by the framework.</summary>
		public static Site lastSite = null;
		/// <summary>Some unparsed supplementary data. You can see it
		/// <see href="https://sourceforge.net/p/dotnetwikibot/svn/HEAD/tree/cache/CommonData.xml">
		/// here.</see></summary>
		public static XElement commonDataXml;
		/// <summary>If true the bot asks user to confirm next Save(), RenameTo() or Delete()
		/// operation. False by default.</summary>
		/// <example><code>Bot.askConfirm = true;</code></example>
		public static bool askConfirm = false;
		/// <summary>Shortcut for Environment.NewLine property.
		/// It's "\r\n" on Windows and "\n" on Unix-like systems.</summary>
		public static string nl = Environment.NewLine;
		/// <summary>Dictionary containing localized DotNetWikiBot interface messages.</summary>
		public static SortedDictionary<string, string> messages =
			new SortedDictionary<string, string>();
		/// <summary>If true, assembly is running on Mono framework. If false,
		/// it is running on Microsoft .NET Framework. This variable is set
		/// automatically, don't change it's value.</summary>
		public static readonly bool isRunningOnMono = (Type.GetType("Mono.Runtime") != null);

		/// <summary>Current bot's console messages language (ISO 639-1 language code). Use
		/// <see cref="LoadLocalizedMessages(string)"/> function to change language.
		/// </summary>
		/// <exclude/>
		public static string botMessagesLang = null;
		/// <summary>If true the bot reports errors and warnings only.
		/// Call <see cref="Bot.EnableSilenceMode()"/>
		/// function to enable that mode, don't change this variable's value directly.</summary>
		/// <exclude/>
		private static bool silenceMode = false;
		/// <summary>If set to some file name (e.g. "DotNetWikiBot.Report.txt"), the bot
		/// writes all output to that file instead of a console. If no path was specified,
		/// the bot creates that file in it's current directory. File is encoded in UTF-8.
		/// Call <see cref="Bot.EnableLogging(string)"/> function to enable log writing,
		/// don't change this variable's value directly.</summary>
		/// <exclude/>
		private static string logFile = null;
		/// <summary>Initial state of boolean HttpWebRequestElement.UseUnsafeHeaderParsing
		/// configuration setting: 0 means true, 1 means false, 2 means unchanged. This is
		/// internal variable.</summary>
		/// <exclude/>
		internal static int unsafeHttpHeaderParsingUsed = 2;
		/// <summary>Auxillary internal web client that is used to access the web.</summary>
		/// <exclude/>
		public static WebClient webClient = new WebClient();
		

		/// <summary>This constructor is used to initialize Bot object.</summary>
		/// <returns>Returns Bot object.</returns>
		static Bot()
		{
			// Display version information
			Console.Write(botVer + " " + version + "\n" +
				"Copyright (c) Iaroslav Vassiliev, 2006-2016, GNU General Public License 2.0\n\n");

			// Format full version string
			botVer += "/" + version + " (" + Environment.OSVersion.VersionString + "; ";
			if (isRunningOnMono) {
				botVer += "Mono";
				try {
					Type type = Type.GetType("Mono.Runtime");
					string v = "";
					if (type != null) {
						MethodInfo displayName = type.GetMethod("GetDisplayName",
							BindingFlags.NonPublic | BindingFlags.Static);
						if (displayName != null)
							v = displayName.Invoke(null, null).ToString();
							botVer += " " + v.Substring(0, v.IndexOf(' '));
					}
				}
				catch (Exception) {}    // ignore failure silently
				botVer += "; ";
			}
			botVer += ".NET CLR " + Environment.Version.ToString() + ")";

			// Find suitable directory for cache where all required permissions are present
			char dirSep = Path.DirectorySeparatorChar;
			cacheDir = Path.GetFullPath("Cache");
			try {
				if (!Directory.Exists(cacheDir))
					Directory.CreateDirectory(cacheDir);
				// Try if write and delete permissions are set for the folder
				File.WriteAllText(cacheDir + dirSep + "test.dat", "test");
				File.Delete(cacheDir + dirSep + "test.dat");
			}
			catch (Exception) {    // occurs if permissions are missing
				// Try one more location
				cacheDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
					dirSep + "DotNetWikiBot" + dirSep + "Cache";
						// on Mono framework ApplicationData location is "/home/ibboard/.config"
				try {
					if (!Directory.Exists(cacheDir))
						Directory.CreateDirectory(cacheDir);
					// Try if write and delete permissions are set for the folder
					File.WriteAllText(cacheDir + dirSep + "test.dat", "test");
					File.Delete(cacheDir + dirSep + "test.dat");
				}
				catch (Exception) {    // occurs if permissions are missing
					throw new WikiBotException(string.Format(Msg("Read/write permissions are " +
						"required for \"{0}\" directory."), Path.GetFullPath("Cache")));
				}
				Console.WriteLine(string.Format(Msg(
					"Now using \"{0}\" directory for cache."), cacheDir));
			}

			// Load localized messages if available
			if (botMessagesLang == null)
				botMessagesLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
			if (botMessagesLang != "en" && botMessagesLang != "iv")    // iv = invariant culture
				if (!LoadLocalizedMessages(botMessagesLang))
					botMessagesLang = "en";

			// Disable SSL/TLS server certificate validation on Mono
			if (isRunningOnMono)
				ServicePointManager.ServerCertificateValidationCallback = Validator;

			// Don't strip trailing dots in URIs, see function description for details
			DisableCanonicalizingUriAsFilePath();

			// Disable 100-continue behaviour, it's not supported on WMF servers (as of 2012)
			ServicePointManager.Expect100Continue = false;

			// Check for updates
			try {
				string verInfo = GetWebResource(
					new Uri("http://dotnetwikibot.sourceforge.net/info.php"), "");
				Match currentVer = Regex.Match(verInfo, "(?i)stable version: (([^ ]+)[^<]+)");
				if (new Version(currentVer.Groups[2].Value) > version)
					Console.WriteLine("*** " + Msg("New version is available") + ": " +
						currentVer.Groups[1].Value + " ***\n");
			}
			catch (Exception) {}    // ignore failure silently

			// Download cache files from web if missing
			string[] cacheFiles = { "CommonData.xml", "xhtml1-transitional.dtd", "xhtml-lat1.ent",
				"xhtml-special.ent", "xhtml-symbol.ent" };
			foreach (string cacheFile in cacheFiles)
			{
				if (!File.Exists(cacheDir + dirSep + cacheFile))
				{
					string src = GetWebResource(new Uri("http://sourceforge.net/p/dotnetwikibot/" +
						"svn/HEAD/tree/cache/" + cacheFile + "?format=raw"), "");
					File.WriteAllText(cacheDir + dirSep + cacheFile, src);
				}
			}
			// Load general info cache
			using (StreamReader reader = File.OpenText(cacheDir + dirSep + "CommonData.xml"))
				commonDataXml = XElement.Load(reader);
		}

		/// <summary>The destructor is used to unset Bot object.</summary>
		/// <exclude/>
		~Bot()
		{
			//if (unsafeHttpHeaderParsingUsed != 2)
				//SwitchUnsafeHttpHeaderParsing(unsafeHttpHeaderParsingUsed == 1 ? true : false);
		}

		/// <summary>Call this function to make bot write all output to the specified file
		/// instead of a console. If only error logging is desirable, first call this
		/// function and after that call <see cref="Bot.EnableSilenceMode()"/> function.</summary>
		/// <param name="logFileName">Path and name of a file to write output to.
		/// If no path was specified, the bot creates that file in it's current directory.
		/// File is encoded in UTF-8.</param>
		public static void EnableLogging(string logFileName)
		{
			logFile = logFileName;
			StreamWriter log = File.AppendText(logFile);
			log.AutoFlush = true;
			Console.SetError(log);
			if (!silenceMode)
				Console.SetOut(log);
		}

		/// <summary>Call this function to make bot report only errors and warnings,
		/// no other messages will be displayed or logged.</summary>
		/// <seealso cref="Bot.DisableSilenceMode()"/>
		public static void EnableSilenceMode()
		{
			silenceMode = true;
			Console.SetOut(new StringWriter());
		}

		/// <summary>Call this function to disable silent mode previously enabled by
		/// <see cref="Bot.EnableSilenceMode()"/> function.</summary>
		public static void DisableSilenceMode()
		{
			silenceMode = false;
			StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
			standardOutput.AutoFlush = true;
			Console.SetOut(standardOutput);
		}

		/// <summary>Function loads localized bot's interface messages from 
		/// <see href="http://sourceforge.net/p/dotnetwikibot/svn/HEAD/tree/DotNetWikiBot.i18n.xml">
		/// "DotNetWikiBot.i18n.xml"</see> file. Function is called in Bot class constructor, 
		/// but it can also be called manually to change interface language at runtime.</summary>
		/// <param name="language">Desired language's ISO 639-1 code (e.g., "fr").</param>
		/// <returns>Returns false if messages for specified language were not found.
		/// Returns true on success.</returns>
		public static bool LoadLocalizedMessages(string language)
		{
			if (!File.Exists("DotNetWikiBot.i18n.xml")) {
				Console.Error.WriteLine("Localization file \"DotNetWikiBot.i18n.xml\" is missing.");
				Console.Error.Write("\n");
				return false;
			}
			using (XmlReader reader = XmlReader.Create("DotNetWikiBot.i18n.xml")) {
				if (!reader.ReadToFollowing(language)) {
					Console.Error.WriteLine("\nLocalized messages not found for language \"{0}\"." +
						"\nYou can help DotNetWikiBot project by translating the messages in\n" +
						"\"DotNetWikiBot.i18n.xml\" file and sending it to developers for " +
						"distribution.\n", language);
					return false;
				}
				if (!reader.ReadToDescendant("msg"))
					return false;
				else {
					if (messages.Count > 0)
						messages.Clear();
					messages[reader["id"]] = reader.ReadString();
				}
				while (reader.ReadToNextSibling("msg"))
					messages[reader["id"]] = reader.ReadString();
			}
			return true;
		}

		/// <summary>Gets localized (translated) version of specified bot's
		/// interface message.</summary>
		/// <param name="message">Message in English. Placeholders for substituted parameters must
		/// be denoted in curly brackets: {0}, {1}, {2}, etc.</param>
		/// <returns>Returns localized version of the specified message,
		/// or English version if localized edition was not found.</returns>
		public static string Msg(string message)
		{
			if (botMessagesLang == "en")
				return message;
			try {
				return messages[message];
			}
			catch (KeyNotFoundException) {
				return message;
			}
		}

		/// <summary>Gets most recent <see cref="Site"/> object constructed by framework.</summary>
		/// <returns>Returns <see cref="Site"/> object.</returns>
		public static Site GetMostRecentSiteObject()
		{
			if (lastSite != null)
				return lastSite;
			throw new WikiBotException(Bot.Msg("No default Site object is available."));
		}

		/// <summary>This function asks user to confirm next action. The message
		/// "Would you like to proceed (y/n/a)? " is displayed and user response is
		/// evaluated. Make sure to set <see cref="Bot.askConfirm"/> variable to "true" before
		/// calling this function.</summary>
		/// <returns>Returns true, if user has confirmed the action.</returns>
		/// <example><code>
		/// if (Bot.askConfirm) {
		///     Console.Write("Some action on live wiki is going to occur.\n\n");
		///     if(!Bot.UserConfirms())
		///         return;
		/// }
		/// </code></example>
		public static bool UserConfirms()
		{
			if (!askConfirm)
				return true;
			ConsoleKeyInfo k;
			Console.Write(Bot.Msg("Would you like to proceed (y/n/a)?") + " ");
			k = Console.ReadKey();
			Console.Write("\n");
			if (k.KeyChar == 'y')
				return true;
			else if (k.KeyChar == 'a') {
				askConfirm = false;
				return true;
			}
			else
				return false;
		}

		/// <summary>This auxiliary function counts the occurrences of specified string
		/// in specified text. This count is often required, but strangely there is no
		/// such function in .NET Framework's String class.</summary>
		/// <param name="text">String to look in.</param>
		/// <param name="str">String to look for.</param>
		/// <param name="ignoreCase">Pass "true" if you require case-insensitive search.
		/// Case-sensitive search is faster.</param>
		/// <returns>Returns the number of found occurrences.</returns>
		/// <example>
		/// <code>int m = Bot.CountMatches("Bot Bot bot", "Bot", false); // m=2</code>
		/// </example>
		public static int CountMatches(string text, string str, bool ignoreCase)
		{
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("text");
			if (string.IsNullOrEmpty(str))
				throw new ArgumentNullException("str");
			int matches = 0;
			int position = 0;
			StringComparison rule = ignoreCase
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
			while ((position = text.IndexOf(str, position, rule)) != -1) {
				matches++;
				position++;
			}
			return matches;
		}

		/// <summary>This auxiliary function returns the zero-based indexes of all occurrences
		/// of specified string in specified text.</summary>
		/// <param name="text">String to look in.</param>
		/// <param name="str">String to look for.</param>
		/// <param name="ignoreCase">Pass "true" if you require case-insensitive search.
		/// Case-sensitive search is faster.</param>
		/// <returns>Returns the List&lt;int&gt; object containing zero-based indexes of all found 
		/// occurrences or empty List&lt;int&gt; if nothing was found.</returns>
		public static List<int> GetMatchesPositions(string text, string str, bool ignoreCase)
		{
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException("text");
			if (string.IsNullOrEmpty(str))
				throw new ArgumentNullException("str");
			List<int> positions = new List<int>();
			StringComparison rule = ignoreCase
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
			int position = 0;
			while ((position = text.IndexOf(str, position, rule)) != -1) {
				positions.Add(position);
				position++;
			}
			return positions;
		}

		/// <summary>This auxiliary function returns part of the string which begins
		/// with some specified substring and ends with some specified substring.</summary>
		/// <param name="src">Source string.</param>
		/// <param name="startTag">Substring with which the resultant string
		/// must begin. Can be null or empty, in this case the source string is returned
		/// from the very beginning.</param>
		/// <param name="endTag">Substring that the resultant string
		/// must end with. Can be null or empty, in this case the source string is returned
		/// to the very end.</param>
		/// <returns>Portion of the source string.</returns>
		public static string GetSubstring(string src, string startTag, string endTag)
		{
			return GetSubstring(src, startTag, endTag, false, false, true);
		}

		/// <summary>This auxiliary function returns part of the string which begins
		/// with some specified substring and ends with some specified substring.</summary>
		/// <param name="src">Source string.</param>
		/// <param name="startTag">Substring that the resultant string
		/// must begin with. Can be null or empty, in this case the source string is returned
		/// from the very beginning.</param>
		/// <param name="endTag">Substring that the resultant string
		/// must end with. Can be null or empty, in this case the source string is returned
		/// to the very end.</param>
		/// <param name="removeStartTag">If true, startTag is not included into returned substring.
		/// Default is false.</param>
		/// <param name="removeEndTag">If true, endTag is not included into returned substring.
		/// Default is false.</param>
		/// <param name="raiseExceptionIfTagNotFound">When set to true, raises
		/// ArgumentOutOfRangeException if specified startTag or endTag was not found.
		/// Default is true.</param>
		/// <returns>Part of the source string.</returns>
		public static string GetSubstring(string src, string startTag, string endTag,
			bool removeStartTag, bool removeEndTag, bool raiseExceptionIfTagNotFound)
		{
			if (string.IsNullOrEmpty(src))
				throw new ArgumentNullException("src");
			int startPos = 0;
			int endPos = src.Length;

			if (!string.IsNullOrEmpty(startTag)) {
				startPos = src.IndexOf(startTag);
				if (startPos == -1) {
					if (raiseExceptionIfTagNotFound == true)
						throw new ArgumentOutOfRangeException("startPos");
					else
						startPos = 0;
				}
				else if (removeStartTag)
					startPos += startTag.Length;
			}

			if (!string.IsNullOrEmpty(endTag)) {
				endPos = src.IndexOf(endTag, startPos);
				if (endPos == -1) {
					if (raiseExceptionIfTagNotFound == true)
						throw new ArgumentOutOfRangeException("endPos");
					else
						endPos = src.Length;
				}
				else if (!removeEndTag)
					endPos += endTag.Length;
			}

			return src.Substring(startPos, endPos - startPos);
		}

		/// <summary>This helper function deletes everything before <paramref name="startTag"/>
		/// and everything after <paramref name="endTag"/> in the provided XML/XHTML source code
		/// and then inserts back the deleted DOCTYPE definition and root element of XML/XHTML
		/// document.</summary>
		/// <remarks>This function is very basic, it's not a true parser and thus it must not
		/// be used to parse documents generated outside MediaWiki software.</remarks>
		/// <param name="text">Source text.</param>
		/// <param name="startTag">A tag which identifies the beginning of target content.</param>
		/// <param name="endTag">A tag which identifies the end of target content.</param>
		/// <param name="removeTags">If true, specified startTag and endTag will be
		/// removed from resultant string.</param>
		/// <returns>Returns part of the source XML markup.</returns>
		public static string GetXMLSubstring(string text, string startTag, string endTag,
			bool removeTags)
		{
			if (string.IsNullOrEmpty(startTag))
				throw new ArgumentNullException("startTag");
			if (string.IsNullOrEmpty(endTag))
				throw new ArgumentNullException("endTag");

			int cursor = 0;

			string headerText = "";
			string footerText = "";

			while (cursor < text.Length && (text[cursor] == ' ' || text[cursor] == '\n'
				|| text[cursor] == '\r' || text[cursor] == '\t'))
					cursor++;    // skip whitespaces
			if (text.StartsWith("<?xml ")) {
				cursor += text.IndexOf("?>", cursor) + 2;
				while (cursor < text.Length && (text[cursor] == ' ' || text[cursor] == '\n'
					|| text[cursor] == '\r' || text[cursor] == '\t'))
						cursor++;    // skip whitespaces
			}
			if (text.StartsWith("<!DOCTYPE ")) {
				cursor += text.IndexOf('>', cursor) + 1;
				while (cursor < text.Length && (text[cursor] == ' ' || text[cursor] == '\n'
					|| text[cursor] == '\r' || text[cursor] == '\t'))
						cursor++;    // skip whitespaces
			}
			if (text.StartsWith("<!--"))    // comment
				cursor += text.IndexOf("-->", cursor) + 3;
			
			int headerEndPos = text.IndexOf('>', cursor) + 1;
			if (headerEndPos > 0)
				headerText = text.Substring(0, headerEndPos);

			int footerPos = text.LastIndexOf('<', cursor);
			if (footerPos > 0 && footerPos > headerEndPos)
				footerText = text.Substring(footerPos);

			return headerText +
				GetSubstring(text, startTag, endTag, removeTags, removeTags, true) + footerText;
		}

		/// <summary>This wrapper function encodes string for use in URI.
		/// The function is necessary because Mono framework doesn't support HttpUtility.UrlEncode()
		/// method and Uri.EscapeDataString() method doesn't support long strings, so a loop is
		/// required. By the way HttpUtility.UrlDecode() is supported by Mono, and a functions
		/// pair Uri.EscapeDataString()/HttpUtility.UrlDecode() is commonly recommended for
		/// encoding/decoding. Although there is another trouble with Uri.EscapeDataString():
		/// prior to .NET 4.5 it doesn't support RFC 3986, only RFC 2396.
		/// </summary>
		/// <param name="str">String to encode.</param>
		/// <returns>Encoded string.</returns>
		public static string UrlEncode(string str)
		{
			int limit = 32766;    // 32766 is the longest string allowed in Uri.EscapeDataString()
			if (str.Length <= limit) {
				return Uri.EscapeDataString(str);
			}
			else {
				StringBuilder sb = new StringBuilder(str.Length);
				int portions = str.Length / limit;
				for (int i = 0; i <= portions; i++) {
					if (i < portions)
						sb.Append(Uri.EscapeDataString(str.Substring(limit * i, limit)));
					else
						sb.Append(Uri.EscapeDataString(str.Substring(limit * i)));
				}
				return sb.ToString();
			}
		}

		/// <summary>This auxiliary function makes the first letter in specified string upper-case.
		/// This is often required, but strangely there is no such function in .NET Framework's
		/// String class.</summary>
		/// <param name="str">String to capitalize.</param>
		/// <returns>Capitalized string.</returns>
		public static string Capitalize(string str)
		{
			if (char.IsUpper(str[0]))
				return str;
			return char.ToUpper(str[0]) + str.Substring(1);
		}

		/// <summary>This auxiliary function makes the first letter in specified string lower-case.
		/// This is often required, but strangely there is no such function in .NET Framework's
		/// String class.</summary>
		/// <param name="str">String to uncapitalize.</param>
		/// <returns>Returns uncapitalized string.</returns>
		public static string Uncapitalize(string str)
		{
			if (char.IsLower(str[0]))
				return str;
			return char.ToLower(str[0]) + str.Substring(1);
		}

		/// <summary>Suspends execution for specified number of seconds.</summary>
		/// <param name="seconds">Number of seconds to wait.</param>
		public static void Wait(int seconds)
		{
			Thread.Sleep(seconds * 1000);
		}

		/// <summary>This internal function switches unsafe HTTP headers parsing on or off.
		/// Because there are many misconfigured servers on the web it is often required
		/// to ignore minor HTTP protocol violations.</summary>
		/// <exclude />
		public static void SwitchUnsafeHttpHeaderParsing(bool enabled)
		{
			System.Configuration.Configuration config =
				System.Configuration.ConfigurationManager.OpenExeConfiguration(
					System.Configuration.ConfigurationUserLevel.None);
			System.Net.Configuration.SettingsSection section =
				(System.Net.Configuration.SettingsSection)config.GetSection("system.net/settings");
			if (unsafeHttpHeaderParsingUsed == 2)
				unsafeHttpHeaderParsingUsed = section.HttpWebRequest.UseUnsafeHeaderParsing ? 1 : 0;
			section.HttpWebRequest.UseUnsafeHeaderParsing = enabled;
			config.Save();
			System.Configuration.ConfigurationManager.RefreshSection("system.net/settings");
		}

		/// <summary>This internal function clears the CanonicalizeAsFilePath attribute in
		/// .NET UriParser to fix a major .NET bug which makes System.Uri incorrectly strip trailing 
		/// dots in URIs.</summary>
		/// <exclude />
		public static void DisableCanonicalizingUriAsFilePath()
		{
			// See https://connect.microsoft.com/VisualStudio/feedback/details/386695/system-uri-in
			MethodInfo getSyntax = typeof(UriParser).GetMethod("GetSyntax",
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
			FieldInfo flagsField = typeof(UriParser).GetField("m_Flags",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			if (getSyntax != null && flagsField != null)
			{
				foreach (string scheme in new string[] { "http", "https" })
				{
					UriParser parser = (UriParser)getSyntax.Invoke(null, new object[] { scheme });
					if (parser != null)
					{
						int flagsValue = (int)flagsField.GetValue(parser);
						// Clear the CanonicalizeAsFilePath attribute
						if ((flagsValue & 0x1000000) != 0)
							flagsField.SetValue(parser, flagsValue & ~0x1000000);
					}
				}
			}
		}

		/// <summary>
		/// This internal function is used to disable SSL/TLS server certificate validation on Mono.
		/// See <see href="http://www.mono-project.com/docs/faq/security/">this page</see> and
		/// <see href="http://www.mono-project.com/archived/usingtrustedrootsrespectfully/">
		/// this page</see>.
		/// </summary>
		/// <exclude />
		public static bool Validator(object sender,
			System.Security.Cryptography.X509Certificates.X509Certificate certificate,
			System.Security.Cryptography.X509Certificates.X509Chain chain,
			System.Net.Security.SslPolicyErrors sslPolicyErrors)
		{
			return true;    // validate every certificate
		}

		/// <summary>This helper function removes all attributes of root XML/XHTML element
		/// (XML namespace declarations, schema links, etc.) to ease processing.</summary>
		/// <param name="xmlSource">XML source code.</param>
		/// <returns>Corrected XML source code.</returns>
		public static string RemoveXMLRootAttributes(string xmlSource)
		{
			int startPos = ((xmlSource.StartsWith("<!") || xmlSource.StartsWith("<?"))
				&& xmlSource.IndexOf('>') != -1) ? xmlSource.IndexOf('>') + 1 : 0;
			int firstSpacePos = xmlSource.IndexOf(' ', startPos);
			int firstCloseTagPos = xmlSource.IndexOf('>', startPos);
			if (firstSpacePos != -1 && firstCloseTagPos != -1 && firstSpacePos < firstCloseTagPos)
				return xmlSource.Remove(firstSpacePos, firstCloseTagPos - firstSpacePos);
			return xmlSource;
		}

		/// <summary>This helper function constructs XPathDocument object, makes XPath query and
		/// returns XPathNodeIterator object for selected nodes.</summary>
		/// <param name="xmlSource">Source XML data.</param>
		/// <param name="xpathQuery">XPath query to select specific nodes in XML data.</param>
		/// <param name="xmlNs">XML namespace manager.</param>
		/// <returns>XPathNodeIterator object.</returns>
		public static XPathNodeIterator GetXMLIterator(string xmlSource, string xpathQuery,
			XmlNamespaceManager xmlNs)
		{
			XmlReader reader = GetXMLReader(xmlSource);
			XPathDocument doc = new XPathDocument(reader);
			XPathNavigator nav = doc.CreateNavigator();
			return nav.Select(xpathQuery, xmlNs);
		}

		/// <summary>This helper function constructs XmlReader object
		/// using provided XML source code.</summary>
		/// <param name="xmlSource">Source XML data.</param>
		/// <returns>XmlReader object.</returns>
		public static XmlReader GetXMLReader(string xmlSource)
		{
			if (xmlSource.Contains("<!DOCTYPE html>")) {
				xmlSource = xmlSource.Replace("<!DOCTYPE html>", "<!DOCTYPE html PUBLIC " +
					"\"-//W3C//DTD XHTML 1.0 Transitional//EN\" " +
					"\"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
			}
			if (!xmlSource.Contains("<html xmlns=")) {
				xmlSource = xmlSource.Replace("<html",
					"<html xmlns=\"http://www.w3.org/1999/xhtml\"");
			}

			StringReader strReader = new StringReader(xmlSource);
			XmlReaderSettings settings = new XmlReaderSettings();
			settings.XmlResolver = new XmlUrlResolverWithCache();
			settings.CheckCharacters = false;
			settings.IgnoreComments = true;
			settings.IgnoreProcessingInstructions = true;
			settings.IgnoreWhitespace = true;
				
			// For .NET 4.0 and higher DtdProcessing property should be used instead of ProhibitDtd
			if (settings.GetType().GetProperty("DtdProcessing") != null) {
				Type t = typeof(XmlReaderSettings).GetProperty("DtdProcessing").PropertyType;
				settings.GetType().InvokeMember("DtdProcessing",
					BindingFlags.DeclaredOnly | BindingFlags.Public |
					BindingFlags.Instance | BindingFlags.SetProperty, null, settings,
					new Object[] { Enum.Parse(t, "2") });    // 2 is a value of DtdProcessing.Parse
			}
			else if (settings.GetType().GetProperty("ProhibitDtd") != null) {
				settings.GetType().InvokeMember("ProhibitDtd",
					BindingFlags.DeclaredOnly | BindingFlags.Public |
					BindingFlags.Instance | BindingFlags.SetProperty,
					null, settings, new Object[] { false });
			}

			return XmlReader.Create(strReader, settings);
		}

		/// <summary>This internal function initializes web client before it accesses
		/// web resources.</summary>
		/// <exclude />
		public static void InitWebClient()
		{
			if (!Bot.isRunningOnMono)
				webClient.UseDefaultCredentials = true;
			webClient.Encoding = Encoding.UTF8;
			webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
			webClient.Headers.Add("Accept-Encoding", "identity");    // disallow traffic compression
			webClient.Headers.Add("User-agent", botVer);
		}

		/// <summary>This wrapper function gets web resource in a fault-tolerant manner.
		/// It should be used only in simple cases, because it sends no cookies, it doesn't support
		/// traffic compression and it lacks other useful features.</summary>
		/// <param name="address">Web resource's URI.</param>
		/// <param name="postData">Data to post with web request,
		/// it can be empty string or null.</param>
		/// <returns>Returns web resource as text.</returns>
		public static string GetWebResource(Uri address, string postData)
		{
			string webResourceText = null;
			for (int errorCounter = 0; true; errorCounter++) {
				try {
					Bot.InitWebClient();
					if (string.IsNullOrEmpty(postData))
						webResourceText = Bot.webClient.DownloadString(address);
					else
						webResourceText = Bot.webClient.UploadString(address, postData);
					break;
				}
				catch (WebException e) {
					if (errorCounter > 3)    // retry 3 times by default
						throw;
					if (Regex.IsMatch(e.Message, ": \\(50[0234]\\) ")) {
						// Remote server problem, retry
						Console.Error.WriteLine(e.Message);
						Console.Error.WriteLine(string.Format(Bot.Msg(
							"Retrying in {0} seconds..."), 60));
						Bot.Wait(60);
					}
					else if (e.Message.Contains("Section=ResponseStatusLine")) {
						// Known Squid problem
						SwitchUnsafeHttpHeaderParsing(true);
						return GetWebResource(address, postData);
					}
					else
						throw;
				}
			}
			return webResourceText;
		}
	}
}