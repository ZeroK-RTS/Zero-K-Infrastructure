using System;
using System.Collections.Generic;
using System.Text;

namespace LuaManagerLib
{
    [Serializable]
    public class ModInfoDb
    {
        public int id;
        public String abbreviation = ""; //CA, BA, S44
        public String configOrderFilename; //e.g. /LuaUI/config/CA_order.lua
       // public String fullname; //Compelete Annihilation
        public int ownerId;
        public WidgetList modWidgets = new WidgetList();
        public List<string> modFilenames = new List<string>();
    }
}
