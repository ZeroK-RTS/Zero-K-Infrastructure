#region using

using System;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Xml;
using System.Xml.Linq;

#endregion

namespace ModelBase
{
	public class RssHandler : IHttpHandler
	{
		#region Other methods

		private static string FormatTime(DateTime time)
		{
			return time.ToString("R");
		}

		#endregion

		#region IHttpHandler Members

		public void ProcessRequest(HttpContext context)
		{
			Cache cache = HttpRuntime.Cache;
			string key = "cb_news_rss";
			XDocument xml;
			if (cache[key] == null) {
				xml =
					new XDocument(new XElement("rss",
					                           new XAttribute("version", "2.0"),
					                           new XElement("channel",
					                                        new XElement("title", "ModelBase News"),
					                                        new XElement("link", Global.BaseUrl),
					                                        new XElement("description", "Spring models and units"),
					                                        //new XElement("copyright", String.Format("(c){0}, POP World Media, LLC. All rights reserved", DateTime.Now.Year))
					                                        new XElement("ttl", "15"),
					                                        from item in Global.Db.Events.ToList()
					                                        select
					                                        	new XElement("item",
					                                        	             new XElement("title", item.Summary),
					                                        	             new XElement("description", Global.Linkify(item.Text) 
																				 + (item.SvnLog != null? "\n" + item.SvnLog : null)
																				 ),
					                                        	             new XElement("link", item.Url),
					                                        	             new XElement("pubDate", FormatTime(item.Time)),
																			 new XElement("author", item.User!= null? item.User.Login : "admin")
																			 ))));
																			
				cache.Insert(key, xml, null, DateTime.Now.AddMinutes(15), Cache.NoSlidingExpiration);
			} else xml = (XDocument) cache[key];

			context.Response.ContentType = "text/xml";
			XmlTextWriter writer = new XmlTextWriter(context.Response.OutputStream, Encoding.UTF8);
			xml.WriteTo(writer);
			writer.Close();
		}

		public bool IsReusable
		{
			get { return true; }
		}

		#endregion
	}
}