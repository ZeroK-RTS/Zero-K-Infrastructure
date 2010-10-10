using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Collections;
using System.Xml;
using System.ComponentModel;

namespace LuaManagerLib
{

    /*internal class DescendingComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            try
            {
                return System.Convert.ToInt32(x).CompareTo
                    (System.Convert.ToInt32(y)) * -1;
            }
            catch (Exception)
            {
                return x.ToString().CompareTo(y.ToString());
            }
        }
    }*/
    [Serializable]
    public class WidgetList : Hashtable
    {
        public WidgetList()
        {
        }

        public WidgetList(WidgetList list)
        {
            foreach( DictionaryEntry elem in list )
            {
                this.Add(elem.Key, elem.Value);
            }            
        }
        /*  public SortedList<string, WidgetInfo> getAsSortedByName()
          {
              SortedList<string, WidgetInfo> newList = new SortedList<string, WidgetInfo>();

              IEnumerator ienum = this.Values.GetEnumerator();
              while (ienum.MoveNext())
              {
                  WidgetInfo info = (WidgetInfo)ienum.Current;
                  newList.Add( info.name, info );
              }

              return newList;
          }

          public ArrayList getAsSortedByDownloadCount()
          {
              ArrayList newList = new ArrayList();

              IEnumerator ienum = this.Values.GetEnumerator();
              while (ienum.MoveNext())
              {
                  WidgetInfo info = (WidgetInfo)ienum.Current;
                  newList.Add((info.downloadCount, info);
              }

              return newList;
          }

          public SortedList<DateTime, WidgetInfo> getAsSortedByEntryDate()
          {
              SortedList<DateTime, WidgetInfo> newList = new SortedList<DateTime, WidgetInfo>();

              IEnumerator ienum = this.Values.GetEnumerator();
              while (ienum.MoveNext())
              {
                  WidgetInfo info = (WidgetInfo)ienum.Current;
                  newList.Add(info.entry, info);
              }

              return newList;
          }
          */
        public ArrayList getAsSortedByVersion()
        {
            SortedList<decimal, WidgetInfo> newList = new SortedList<decimal, WidgetInfo>();

            IEnumerator ienum = this.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                WidgetInfo info = (WidgetInfo)ienum.Current;
                newList.Add(info.version, info);
            }

            ArrayList rsortList = new ArrayList();
            ienum = newList.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                WidgetInfo info = (WidgetInfo)ienum.Current;
                rsortList.Add(info);
            }
            // Re-sort the list.
            rsortList.Reverse();
         //   rsortList.Sort();

            return rsortList;
        }

        /*
         * returns -1 if not found in any widget
         */
        public int getIdByContainingFilename(String fullFilename, String springPath)
        {
            String normalizedFile = System.IO.Path.GetFullPath(fullFilename);
            
            //iterate all widgets
            IEnumerator ienum = this.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                //iterate all files belonging to that current widget
                WidgetInfo winfo = (WidgetInfo)ienum.Current;
                if (!winfo.dbIsAvail)
                {
                    continue;
                }

                IEnumerator ienum2 = winfo.fileList.GetEnumerator();
                while( ienum2.MoveNext() )
                {
                    FileInfo finfo = (FileInfo)ienum2.Current;
                    
                    //check if this is the wanted file
                    String curFile = System.IO.Path.GetFullPath( springPath + finfo.localPath );
                    if ( curFile.Equals( normalizedFile, StringComparison.CurrentCultureIgnoreCase ) )
                    {
                        return winfo.id;
                    }
                }
            }

            //not found
            return -1;
        }

        /*
         * Merges another list into this list
         */
        public void mergeAnotherList(WidgetList blist)
        {
            IDictionaryEnumerator ienum = blist.GetEnumerator();

            while (ienum.MoveNext())
            {
                int key = (int)ienum.Key;

                WidgetInfo info = (WidgetInfo)ienum.Value;
                if ( !this.ContainsKey(key))
                {
                    this.Add( key, info);
                }
            }
        }

        /*
         * Gets first widget with that nameId (There should be only one though)
         */
        public WidgetInfo getByNameId(int nameId)
        {
            IEnumerator ienum = this.Values.GetEnumerator();

            while (ienum.MoveNext())
            {
                WidgetInfo info = (WidgetInfo)ienum.Current;
                if (info.nameId == nameId)
                {
                    return info;
                }
            }
            return null;
        }

        public WidgetInfo getByHeaderName(String name)
        {
            IEnumerator ienum = this.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                WidgetInfo info = (WidgetInfo)ienum.Current;
                if (info.headerName == name)
                {
                    return info;
                }
            }
            return null;
        }


        public WidgetInfo getByName(String name)
        {
            IEnumerator ienum = this.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                WidgetInfo info = (WidgetInfo)ienum.Current;
                if (info.name == name)
                {
                    return info;
                }
            }
            return null;
        }

        /*
         * Assuming for the widget there is a newer version available, get its ID
         */
        public WidgetInfo getLatestVersionFromWidget(int nameId)
        {
            IEnumerator ienum = this.Values.GetEnumerator();

            WidgetInfo result = null;
            decimal latestVersion = decimal.MinValue;
            while (ienum.MoveNext())
            {
                WidgetInfo info = (WidgetInfo)ienum.Current;
                if ( ( info.nameId == nameId ) && ( info.version > latestVersion ) )
                {
                    latestVersion = info.version;
                    result = info;
                }
            }

            return result;
        }

        public int getNextFreeId()
        {
            int id = 0;
            while( this.ContainsKey( id ) )
            {
                id++;
                if (id == 0)
                {
                    //holy shit!? overflow!
                    throw new Exception("Free ID search overflow! Awesome!");
                }
            }
            return id;
        }

        public int getHighestId()
        {
            int id = 0;
            IEnumerator ienum = this.Values.GetEnumerator();
            while (ienum.MoveNext())
            {
                WidgetInfo winfo = (WidgetInfo)ienum.Current;
                id = Math.Max(winfo.id, id);
            }

            return id;
        }

        public WidgetInfo getOlderVersion(WidgetInfo latestVersion )
        {
            //XmlNode nameIdXml = r.SelectSingleNode("NameId");
            //int nameId = int.Parse( nameIdXml.InnerText );

            //XmlNode versionXml = r.SelectSingleNode("Version");
            //decimal version = decimal.Parse( versionXml.InnerText );

            IEnumerator ienum = this.Values.GetEnumerator();
            WidgetInfo widget;

            while (ienum.MoveNext())
            {
                widget = (WidgetInfo)ienum.Current;
                //XmlNode idItem = widget.Attributes.GetNamedItem("ID");
                //XmlNode name = widget.SelectSingleNode("Name");
                //XmlNode curNameIdXml = widget.SelectSingleNode("NameId");
                //int curNameId = int.Parse( curNameIdXml.InnerText );

                //XmlNode curVersionXml = widget.SelectSingleNode( "Version" );
                //decimal curVersion = decimal.Parse( curVersionXml.InnerText );

                if ((widget.nameId == latestVersion.nameId) && (widget.version <= latestVersion.version))
                {
                    return widget;
                }
            }
            return null;
        }
    }
}
