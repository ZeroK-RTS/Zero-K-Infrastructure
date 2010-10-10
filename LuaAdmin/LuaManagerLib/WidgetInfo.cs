using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Collections;

namespace LuaManagerLib
{
    public enum WidgetState
    {
        INSTALLED,
        OUTDATED,
        UNKNOWN_VERSION,
        NOT_INSTALLED,
        INSTALLED_LOCALLY
    }

    [Serializable]
    public class WidgetInfo
    {
        public WidgetInfo()
        {
            this.state = WidgetState.NOT_INSTALLED;
            this.activatedState = new Dictionary<String,bool>();
            this.dbIsAvail = false;
            this.headerIsAvail = false;
            this.oderIsAvail = false;
            this.downloadCount = 0;
            this.entry = new DateTime(1997, 9, 30);
            this.headerName = "";
            this.orderName = null;
        }

        public void addFileHeaderInfo(Dictionary<String,Object> ar )
        {
            headerAuthor = ar["author"] as String;
            headerDate = ar["date"] as String;
            headerLayer = Convert.ToInt32(ar["layer"]);
            headerLicense = ar["license"] as String;
            headerName = ar["name"] as String;
            headerDefaultEnable = Convert.ToBoolean(ar["enabled"]);
            headerDescription = ar["desc"] as String;
        }

        public void addFileHeaderInfo(WidgetInfo winfo)
        {
            headerIsAvail = winfo.headerIsAvail;
            headerName = winfo.headerName;
            headerAuthor = winfo.headerAuthor;
            headerDate = winfo.headerDate;
            headerLicense = winfo.headerLicense;
            headerDescription = winfo.headerDescription;
            headerLayer = winfo.headerLayer;
            headerDefaultEnable = winfo.headerDefaultEnable;
            //headerSourceFile = winfo.headerSourceFile;
           // activatedState = winfo.activatedState;
        }


        //DB members
        public bool dbIsAvail;
        public int active;
        public string name;
        public int nameId;
        public decimal version;
        public int id;
        public string author;
        public string description;
        public string changelog;
        public string supportedMods;
        public bool hidden;
        public int downloadCount;
        public float downsPerDay;
        public DateTime entry;
        public int imageCount;
        public LinkedList<FileInfo> fileList;
        public float rating;
        public int voteCount;
        public int commentCount;
        public int CategoryId;
        //custom info
        public WidgetState state;




        //widget file info header
        public bool headerIsAvail;
        public String headerName;
        public String headerAuthor;
        public String headerDate;
        public String headerLicense;
        public String headerDescription;
        public int headerLayer;
        public bool headerDefaultEnable;
        //public String headerSourceFile;

        //name as seen in order_config.lua
        public bool oderIsAvail;
        public String orderName;
        public String modName;  //null or abbreviation of the mod

        //is widget active -> F11 menu
        public Dictionary<String,bool> activatedState; //key is modShortName
    }
}
