using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using LuaManagerLib;
using System.Xml;
using System.Net;
using System.Globalization;


namespace LuaManagerLib 
{
    public class WidgetBrowserAdmin : LuaManagerLib.WidgetBrowser
    {
        protected readonly String m_baseAdminUrl;//"http://widgetdb.springrts.de/";
        protected String m_baseAdminUrlPoint;

        protected string login;
        protected string passwordMd5;

        public WidgetBrowserAdmin(string serverUrl) : base(serverUrl)
        {
            this.m_baseAdminUrl = serverUrl;
            this.m_baseAdminUrlPoint = m_baseAdminUrl + "/admin.php?";
        }

        public void setLoginData( string username, string password )
        {
            this.login = username;
            this.passwordMd5 = Tools.Md5Cached.string2Md5Uncached(password);

            m_baseAdminUrlPoint = m_baseAdminUrl + "/admin.php?" + "lname=" + username + "&pwstr=" + this.passwordMd5 + "&";
        }

        /*
         * There should be a file "version.txt" on the webserver
         * It should only contain one line which gives info about latest version:
         * "0.45:LuaAdmin045.zip"
         */
        public void getLatestVersionInfo(out Decimal version, out string url, out string filename)
        {
            string res = this.doRequest(m_baseAdminUrl + "/admin/version.txt");
            string[] split = res.Split(':');

            CultureInfo culture = new CultureInfo("en-US"); //force dotted decimal fraction

            version = Decimal.Parse(split[0], culture); 
            filename = split[1];
            url = m_baseAdminUrl + "/admin/" + filename;
        }

        public int getUserId()
        {
            return int.Parse( this.doRequest(m_baseAdminUrlPoint + "m=8" ) ) ;
        }

        /// <summary>
        /// Deleters
        /// </summary>
        /// <returns></returns>
        public void deleteName(int nameId)
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=13&id=" + nameId.ToString());
            return;
        }

        public void deleteImage(int imageId)
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=12&id=" + imageId.ToString());
            return;
        }

        public void deleteLua(int luaId)
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=11&id=" + luaId.ToString());
            return;
        }

        public void deleteFile(int fileId)
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=10&id=" + fileId.ToString());
            return;
        }

        public void deleteMod(int modId)
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=17&id=" + modId.ToString());
            return;
        }

        public void deleteModWidget(int modWidgetId)
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=18&id=" + modWidgetId.ToString());
            return;
        }
        /// END OF DELETE
        /// 

        public SortedList<string, WidgetName> getNames()
        {
            XmlDocument xml = this.doRequestXml(m_baseAdminUrlPoint + "m=0");
            return WidgetBrowserAdmin.xml2NamesList(xml);
        }

        /*
         * there is no command for this (yet), so do manually
         */
        public WidgetName getNameByWidgetName(string name)
        {
            SortedList<string, WidgetName> names = this.getNames();

            return names[name];
        }

        public void addCategory(string name)
        {
#pragma warning disable 612,618
            this.doRequest(m_baseAdminUrlPoint + "m=22&n=" + Utils.encodeUrl(name));

            return;
        }

        public void removeCategory(int id)
        {
            this.doRequest(m_baseAdminUrlPoint + "m=21&id=" + id);
            return;
        }

        public void addMod(string name)
        {
            this.doRequest(m_baseAdminUrlPoint + "m=15&name=" + Utils.encodeUrl(name));
            return;
        }

        public void addModWidget(string name, int modId )
        {
            this.doRequest(m_baseAdminUrlPoint + "m=16&n=" + Utils.encodeUrl(name) + "&id=" + modId.ToString() );
            return;
        }

        public void updateMod(int modId, string name, string orderFilename)
        {
            this.doRequest(m_baseAdminUrlPoint + "m=20&n=" + Utils.encodeUrl(name) + "&o=" + orderFilename + "&id=" + modId.ToString());
            return;
        }

        /*
         * This is the only a case (ATM) where a POST is used to send data (description text), i think there is a limit for parameters in URLs
         */
        public void updateName(int id, string name, string author, string mods, string description, bool hidden, int categoryId)
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=2&n=" + Utils.encodeUrl(name) +
                "&a=" + Utils.encodeUrl(author) + "&mo=" + Utils.encodeUrl(mods) + "&c=" + categoryId + "&id=" + id.ToString()+ "&h=" + Convert.ToString(Convert.ToInt32(hidden)),
                "d=" + Utils.encodeUrl(description) );
            return;
        }

        public void updateModWidget(int id, string name, string description )
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=19&n=" + Utils.encodeUrl(name) +
                "&d=" + Utils.encodeUrl(description) + "&id=" + id.ToString() );
            return;
        }

        public void updateLuaVersion(int id, string changelog, int active )
        {
            string result = this.doRequest(m_baseAdminUrlPoint + "m=4&c=" + Utils.encodeUrl(changelog) +
                "&a=" + active.ToString() + "&id=" + id.ToString() );
            return;
        }

        public void addName(string name)
        {
            this.doRequest(m_baseAdminUrlPoint + "m=1&name=" + Utils.encodeUrl(name));
            return;
        }

#pragma warning restore 612,618


        public void addLuaFile(string localpath, string filename, int luaId )
        {
            WebClient wc = new WebClient() {Proxy = null};

            byte[] resp = wc.UploadFile(m_baseAdminUrlPoint + "m=5&l=" + localpath + "&lid=" + luaId.ToString(), "POST", Utils.normalizePathname( filename ) );

            return;
        }

        public void addImage(int nameId, string filename )
        {
            WebClient wc = new WebClient() {Proxy = null};

            byte[] resp = wc.UploadFile(m_baseAdminUrlPoint + "m=6&nid=" + nameId.ToString(), "POST", filename);
            return;
        }

        public void addThumb(int luaId, string filename)
        {
            WebClient wc = new WebClient() {Proxy = null};

            byte[] resp = wc.UploadFile(m_baseAdminUrlPoint + "m=7&lid=" + luaId.ToString(), "POST", filename);

            return;
        }

        public WidgetList getAllLuas()
        {
            XmlDocument xml = this.doRequestXml(m_baseAdminUrlPoint + "m=9");
            return WidgetBrowser.xml2WidgetList(xml);
        }

        public WidgetList getOverviewListWithInactive()
        {
            XmlDocument xml = this.doRequestXml(m_baseAdminUrlPoint + "m=14");
            return WidgetBrowser.xml2WidgetList(xml);
        }
     
        /*
         * returns id of added lua
         */
        public int addLuaVersion(decimal version, int nameId)
        {
            string resp = this.doRequest(m_baseAdminUrlPoint + "m=3&v=" + Utils.commaToDot(version.ToString()) + "&nId=" + nameId);
          
            return int.Parse( resp );
        }

        static protected SortedList<string, WidgetName> xml2NamesList(XmlDocument doc)
        {
            XmlNode root = doc.DocumentElement;
            IEnumerator ienum = root.GetEnumerator();
            XmlNode fileXml;

            SortedList<string, WidgetName> table = new SortedList<string, WidgetName>();
            while (ienum.MoveNext())
            {
                fileXml = (XmlNode)ienum.Current;
                WidgetName info = WidgetBrowserAdmin.xml2NameInfo(fileXml);
                table.Add( info.Name, info );
            }

            return table;
        }

        static protected WidgetName xml2NameInfo(XmlNode xml)
        {
            WidgetName winfo = new WidgetName();

            winfo.Id = int.Parse(xml.Attributes.GetNamedItem("ID").InnerText);
            winfo.Name = xml.SelectSingleNode("Name").InnerText;
            winfo.SupportedMods = xml.SelectSingleNode("Mods").InnerText;
            winfo.ownerId = int.Parse(xml.SelectSingleNode("OwnerId").InnerText);
            winfo.Hidden = ( int.Parse(xml.SelectSingleNode("Hidden").InnerText) == 1 );

            winfo.VoteCount = int.Parse(xml.SelectSingleNode("VoteCount").InnerText);
            winfo.CommentCount = int.Parse(xml.SelectSingleNode("CommentCount").InnerText);
            try
            {
                winfo.CategoryId = int.Parse(xml.SelectSingleNode("CategoryId").InnerText);
            }
            catch
            {
                winfo.CategoryId = 0;
            }

            if (xml.SelectSingleNode("Rating").InnerText.Length == 0)
            {
                winfo.Rating = 0.0f;
            }
            else
            {
                string t = xml.SelectSingleNode("Rating").InnerText;
                winfo.Rating = float.Parse(xml.SelectSingleNode("Rating").InnerText, CultureInfo.CreateSpecificCulture("en-US").NumberFormat);
            }

            if (xml.SelectSingleNode("Author") != null)
            {
                winfo.Author = xml.SelectSingleNode("Author").InnerText;
            }

            if (xml.SelectSingleNode("Description") != null)
            {
                winfo.Description = xml.SelectSingleNode("Description").InnerText;
            }

            return winfo;
        }
    }
}
