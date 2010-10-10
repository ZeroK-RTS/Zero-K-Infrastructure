using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using System.Collections;
using System.Globalization;
//using LuaManagerLib..FileInfo;

namespace LuaManagerLib
{
    public class WidgetBrowser
    {
        protected readonly String m_baseUrl;// = "http://widgetdb.springrts.de/";
        protected readonly String m_targetUrl;// = "http://widgetdb.springrts.de/lua_manager.php";

        public WidgetBrowser(string serverUrl)
        {
            this.m_baseUrl = serverUrl;
            this.m_targetUrl = this.m_baseUrl + "/lua_manager.php";
        }
        //Array of modInfos
        public Dictionary<string, ModInfoDb> getActivationMods()
        {
            XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=11");
            ArrayList ar = WidgetBrowser.xml2ModList(xml);

            Dictionary<string, ModInfoDb> result = new Dictionary<string, ModInfoDb>();
            foreach (ModInfoDb mod in ar)
            {
                try
                {
                    WidgetList modWidgets = this.getModWidgetsByModId(mod.id);
                    mod.modWidgets = modWidgets;
                    result.Add( mod.abbreviation, mod );
                }
                catch
                {
                    //hm what now?
                    //do nothing, nobody will ever know *hrhrhrhrhr*
                }
            }

            return result;
        }

        public Dictionary<int,Category> getCategories()
        {
            var list = new List<Category>();
            XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=20");
            return WidgetBrowser.xml2Categories(xml);
        }

        //WidgetList. But only ID, headerName and headerDescription filled!
        public WidgetList getModWidgetsAll()
        {
            XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=12");
            return WidgetBrowser.xml2ModWidgets(xml);
        }

        //WidgetList. But only ID, headerName and headerDescription filled!
        public WidgetList getModWidgetsByModId(int modId)
        {
            XmlDocument xml = this.doRequestXml( m_targetUrl + "?m=13&id=" + modId);
            return WidgetBrowser.xml2ModWidgets(xml);
        }

        public String getZipDownloadUrl( int luaId )
        {
            return m_targetUrl + "?m=10&id=" + luaId;
        }

        public void downloadOriginalLuaFolder(string name)
        {
            System.IO.File.Delete(name);

            WebClient wc = new WebClient() {Proxy=null};
            wc.DownloadFile(m_baseUrl + "/files/original/latest.zip", name);
        }

        public void incDownloadCounter( int luaId )
        {
            this.doRequest( m_targetUrl + "?m=5&id=" + luaId );
        }

        public WidgetList getOverviewList()
        {
            XmlDocument xml = this.doRequestXml( m_targetUrl + "?m=0");
            return WidgetBrowser.xml2WidgetList(xml);
        }

        public WidgetList getAllLuasActive()
        {
            XmlDocument xml = this.doRequestXml( m_targetUrl + "?m=3");
            return WidgetBrowser.xml2WidgetList(xml);
        }

        public WidgetList getLuasByNameId(int nameId)
        {
            XmlDocument xml = this.doRequestXml( m_targetUrl + "?m=6&id=" + nameId);
            return WidgetBrowser.xml2WidgetList(xml);
        }

        public WidgetInfo getLuaById(int luaId)
        {
            XmlDocument xml = this.doRequestXml( m_targetUrl + "?m=2&id=" + luaId);
            return xml2WidgetInfo(xml.DocumentElement.FirstChild);
        }

        public LinkedList<FileInfo> getFileListByLuaId(int luaId)
        {
            XmlDocument xml = this.doRequestXml( m_targetUrl + "?m=1&id=" + luaId);
            return xml2FileInfoList(xml);
        }

        public LinkedList<FileInfo> getFileListByLuaIds(List<int> luaIds)
        {
            string idStr = "";
            foreach (int id in luaIds)
            {
                idStr += id.ToString();
                idStr += ",";
            }
            idStr = idStr.Remove(idStr.Length - 1); //remove the last comma

            //post the ids
            XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=17", "ids=" + idStr);
            return xml2FileInfoList(xml);
        }

        public LinkedList<FileInfo> getImagesByNameId(int nameId)
        {
            XmlDocument xml = this.doRequestXml( m_targetUrl + "?m=4&id=" + nameId);
            return xml2FileInfoList(xml);
        }

        public String getThumbnailUrl(int luaId)
        {
            String url = m_baseUrl + "/thumbnails/" + luaId; // +".jpg";
            return url;
        }

        public void addRating(int nameId, float rating, string username, string password)
        {
            string ratStr = rating.ToString(CultureInfo.CreateSpecificCulture("en-US").NumberFormat);
            String answ = this.doRequest(m_targetUrl + "?m=14&id=" + nameId + "&uname=" + username + "&pw=" + password + "&r=" + ratStr);
            if (answ == "Rejected!")
            {
                throw new Exception("Rating was rejected! Check your login credentials!");
            }
        }

        private String ints2commaString(List<int> arr)
        {
            String str = "";
            foreach (Object o in arr)
            {
                if ( str.Length > 0 )
                {
                    str += ",";
                }
                str += o;
            }
            return str;
        }

        private String strings2commaString(List<string> arr)
        {
            String str = "";
            foreach (Object o in arr)
            {
                if (str.Length > 0)
                {
                    str += ",";
                }
                str += o;
            }
            return str;
        }

        public void setProfileInstallation(List<int> nameIds, string username, string password)
        {
            string nameIdStr = ints2commaString(nameIds);
            String answ = this.doRequest(m_targetUrl + "?m=25" + "&uname=" + username + "&pw=" + password, "ids=" + nameIdStr );
            if (answ == "Rejected!")
            {
                throw new Exception("Rating was rejected! Check your login credentials!");
            }
        }

        public void setProfileActivation(List<String> names, int modId, string username, string password)
        {
            string nameIdStr = strings2commaString(names);
            String answ = this.doRequest(m_targetUrl + "?m=26" + "&modId=" + modId + "&uname=" + username + "&pw=" + password, "names=" + nameIdStr);
            if (answ == "Rejected!")
            {
                throw new Exception("Profile was rejected! Check your login credentials!");
            }
        }


        public List<int> getProfileInstallation(string username, string password)
        {
            XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=18" + "&uname=" + username + "&pw=" + password );
            return WidgetBrowser.xml2NameIds(xml);
        }

        public List<string> getProfileActivation(string username, string password, int modId)
        {
            XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=19" + "&uname=" + username + "&pw=" + password + "&modId=" + modId);
            return WidgetBrowser.xml2WidgetNames(xml);
        }

        public Nullable<float> getUserRating(string username, string password, int nameId)
        {
            try
            {
                XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=21" + "&uname=" + username + "&pw=" + password + "&id=" + nameId);
                var list = WidgetBrowser.xml2Ratings(xml);
                return list[0];
            }
            catch
            {
                return null;
            }
        }


        /*
        * POST is used to send data (comment text), i think there is a limit for parameters in URLs
        */
        public void addComment(int nameId, string comment, string username, string password)
        {
#pragma warning disable 612,618
            string result = this.doRequest(m_targetUrl + "?m=15&id=" + nameId + "&uname=" + username + "&pw=" + password, "c=" + Utils.encodeUrl(comment) );
#pragma warning restore 612,618
            if (result == "Rejected!")
            {
                throw new Exception("Comment was rejected! Check your login credentials!");
            }
        }

        public List<Comment> getCommentsByNameId(int nameId)
        {
            XmlDocument xml = this.doRequestXml(m_targetUrl + "?m=16&id=" + nameId);
            return WidgetBrowser.xml2Comments(xml);
        }

        #region XmlConversion
        static public LinkedList<FileInfo> xml2FileInfoList(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode fileXml;

            LinkedList<FileInfo> list = new LinkedList<FileInfo>();
            while (ienum.MoveNext())
            {
                fileXml = (XmlNode)ienum.Current;
                FileInfo info = WidgetBrowser.xml2FileInfo(fileXml);
                list.AddLast( info);
            }
            return list;
        }

        static public FileInfo xml2FileInfo(XmlNode xml)
        {
            FileInfo winfo = new FileInfo();

            winfo.id = int.Parse(xml.Attributes.GetNamedItem("ID").InnerText);
            winfo.Url = xml.SelectSingleNode("Url").InnerText;

            if (xml.SelectSingleNode("LocalPath") != null)
            {
                winfo.localPath = xml.SelectSingleNode("LocalPath").InnerText;
            }

            if (xml.SelectSingleNode("LuaId") != null)
            {
                winfo.luaId = int.Parse( xml.SelectSingleNode("LuaId").InnerText );
            }

            if (xml.SelectSingleNode("MD5") != null)
            {
                winfo.Md5 = xml.SelectSingleNode("MD5").InnerText;
            }

            return winfo;
        }

        static public WidgetInfo xml2WidgetInfo(XmlNode xml)
        {
            WidgetInfo winfo = new WidgetInfo();
            winfo.dbIsAvail = true;

            winfo.id = int.Parse(xml.Attributes.GetNamedItem("ID").InnerText);
            //winfo.id = int.Parse(idItem.InnerText);
            winfo.name = xml.SelectSingleNode("Name").InnerText;
            winfo.supportedMods = xml.SelectSingleNode("Mods").InnerText;
            winfo.nameId = int.Parse( xml.SelectSingleNode("NameId").InnerText );

            NumberFormatInfo finfo = new NumberFormatInfo();
            finfo.NumberDecimalSeparator = ".";

            winfo.version = decimal.Parse(xml.SelectSingleNode("Version").InnerText, finfo );
            winfo.author = xml.SelectSingleNode("Author").InnerText;
            winfo.description = xml.SelectSingleNode("Description").InnerText;
            winfo.changelog = xml.SelectSingleNode("Changelog").InnerText;
            winfo.downloadCount = int.Parse( xml.SelectSingleNode("DownloadCount").InnerText );
            winfo.downsPerDay = float.Parse(xml.SelectSingleNode("DownsPerDay").InnerText, CultureInfo.CreateSpecificCulture("en-US").NumberFormat);
            winfo.entry = DateTime.Parse(xml.SelectSingleNode("Entry").InnerText);
            winfo.hidden = (int.Parse(xml.SelectSingleNode("Hidden").InnerText) == 1);

            if (xml.SelectSingleNode("Rating").InnerText.Length == 0)
            {
                winfo.rating = 0.0f;
            }
            else
            {
                string t = xml.SelectSingleNode("Rating").InnerText;
                winfo.rating = float.Parse(xml.SelectSingleNode("Rating").InnerText, CultureInfo.CreateSpecificCulture("en-US").NumberFormat );
            }
            winfo.voteCount = int.Parse(xml.SelectSingleNode("VoteCount").InnerText);
            winfo.commentCount = int.Parse(xml.SelectSingleNode("CommentCount").InnerText);
            int.TryParse(xml.SelectSingleNode("CategoryId").InnerText, out winfo.CategoryId);
            int.TryParse(xml.SelectSingleNode("ImageCount").InnerText, out winfo.imageCount);

            if (xml.SelectSingleNode("Active") != null)
            {
                winfo.active = int.Parse(xml.SelectSingleNode("Active").InnerText);
            }

            return winfo;
        }

        static public WidgetInfo xml2WidgetInfo(String xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode root = doc.DocumentElement;
            return WidgetBrowser.xml2WidgetInfo(root.FirstChild);
        }

        static public List<int> xml2NameIds(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            var list = new List<int>();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                int nameId = int.Parse(widgetXml.SelectSingleNode("NameId").InnerText);
                list.Add(nameId);
            }
            return list;
        }

        static public List<string> xml2WidgetNames(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            var list = new List<string>();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                String name = widgetXml.SelectSingleNode("WidgetName").InnerText;
                list.Add(name);
            }
            return list;
        }

        static public WidgetList xml2WidgetList(XmlDocument doc)
        { 
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            WidgetList list = new WidgetList();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                WidgetInfo info = WidgetBrowser.xml2WidgetInfo(widgetXml);
                list.Add( info.id, info);
            }
            return list;
        }

        static public float xml2Rating(XmlNode xml)
        {
            return float.Parse(xml.SelectSingleNode("Rating").InnerText, CultureInfo.CreateSpecificCulture("en-US").NumberFormat);
        }

        static public ModInfoDb xml2ModInfo(XmlNode xml)
        {
            ModInfoDb info = new ModInfoDb();
            
            info.abbreviation = xml.SelectSingleNode("Abbreviation").InnerText;
            info.ownerId = int.Parse( xml.SelectSingleNode("OwnerId").InnerText );
            info.id = int.Parse(xml.Attributes.GetNamedItem("ID").InnerText);
            info.configOrderFilename = xml.SelectSingleNode("OrderConfigFilename").InnerText;

            return info;
        }

        static public ModInfoDb xml2ModInfo(String xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode root = doc.DocumentElement;
            return WidgetBrowser.xml2ModInfo(root.FirstChild);
        }

        static public List<float> xml2Ratings(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            var list = new List<float>();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                float rat = WidgetBrowser.xml2Rating(widgetXml);
                list.Add(rat);
            }
            return list;
        }

        static public ArrayList xml2ModList(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            ArrayList list = new ArrayList();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                ModInfoDb info = WidgetBrowser.xml2ModInfo(widgetXml);
                list.Add(info);
            }
            return list;
        }

        static public Category xml2Category(XmlNode xml)
        {
            Category info = new Category();
            info.name = xml.SelectSingleNode("Name").InnerText;
            info.id = int.Parse(xml.Attributes.GetNamedItem("ID").InnerText);
            info.ownerId = int.Parse(xml.SelectSingleNode("OwnerId").InnerText);

            return info;
        }

        static public WidgetInfo xml2ModWidgetInfo(XmlNode xml)
        {
            WidgetInfo info = new WidgetInfo();
            info.headerName = xml.SelectSingleNode("HeaderName").InnerText;       
            info.headerDescription = xml.SelectSingleNode("Description").InnerText;
            info.id = int.Parse(xml.Attributes.GetNamedItem("ID").InnerText);

            return info;
        }

        static public WidgetInfo xml2ModWidgetInfo(String xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode root = doc.DocumentElement;
            return WidgetBrowser.xml2ModWidgetInfo(root.FirstChild);
        }

        static public Dictionary<int,Category> xml2Categories(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            var list = new Dictionary<int, Category>();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                Category info = WidgetBrowser.xml2Category(widgetXml);
                list.Add(info.id, info);
            }
            return list;
        }

        static public WidgetList xml2ModWidgets(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            WidgetList list = new WidgetList();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                WidgetInfo info = WidgetBrowser.xml2ModWidgetInfo(widgetXml);
                list.Add( info.id, info);
            }
            return list;
        }

        static public List<Comment> xml2Comments(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode widgetXml;

            var list = new List<Comment>();
            while (ienum.MoveNext())
            {
                widgetXml = (XmlNode)ienum.Current;
                Comment cmt = WidgetBrowser.xml2Comment(widgetXml);
                list.Add(cmt);
            }
            return list;
        }

        static public Comment xml2Comment(XmlNode xml)
        {
            var cmt = new Comment();
            cmt.comment = xml.SelectSingleNode("Comment").InnerText;
            cmt.username = xml.SelectSingleNode("Username").InnerText;
            cmt.entry = DateTime.Parse(xml.SelectSingleNode("Entry").InnerText);

            return cmt;
        }

        #endregion

        #region RequestFunctions

        protected XmlDocument doRequestXml(String url)
        {
            return doRequestXml(url, null);
        }

        protected XmlDocument doRequestXml(String url, String postData)
        {
            string xmlText = this.doRequest(url, postData);
            if (xmlText == "Rejected!")
            {
                throw new Exception("Rejected! Check your login credentials!");
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);
            return doc;
        }

        protected string doRequest(String url)
        {
            return doRequest(url, null );
        }

        protected string doRequest(String url, string postData )
        {
          /*  NetworkCredential Cred = new NetworkCredential();
            Cred.UserName = "user";
            Cred.Password = "pass";

            */
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            if (postData != null)
            {
                byte[] buffer = Encoding.ASCII.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = buffer.Length;
                Stream str = request.GetRequestStream();
                str.Write(buffer, 0, buffer.Length);
                str.Close();
            }
    
            // If required by the server, set the credentials.
            request.Credentials = CredentialCache.DefaultCredentials;
            // Get the response.
            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            
            //catch exception if not able to connect
            
            // Display the status.
           // Console.WriteLine(response.StatusDescription);
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            //Console.WriteLine(responseFromServer);
            // Cleanup the streams and the response.
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

        #endregion
    }
}
