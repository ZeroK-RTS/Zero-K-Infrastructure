using System;
using System.Collections;
using System.Windows.Forms;
using LuaManagerLib;
using ZeroKLobby.LuaMgr;

namespace ZeroKLobby
{
    class ListViewTagSorter: IComparer
    {
        public int Compare(object x, object y)
        {
            var t1 = (((ListViewItem)x).Tag as ListViewTagData).sorting;
            var t2 = (((ListViewItem)y).Tag as ListViewTagData).sorting;
            //Console.WriteLine( t1.GetType().Name );

            if (t1.GetType().Name == "String") return String.Compare((string)t1, (string)t2);
            else if (t1.GetType().Name == "Int32")
            {
                if ((int)t1 < (int)t2) return 1;
                else if ((int)t1 > (int)t2) return -1;
                else return 0;
            }
            else if (t1.GetType().Name == "Single")
            {
                if ((float)t1 < (float)t2) return 1;
                else if ((float)t1 > (float)t2) return -1;
                else return 0;
            }
            else if (t1.GetType().Name == "Double")
            {
                if ((Double)t1 < (Double)t2) return 1;
                else if ((Double)t1 > (Double)t2) return -1;
                else return 0;
            }
            else if (t1.GetType().Name == "DateTime") return -1*DateTime.Compare((DateTime)t1, (DateTime)t2);
            else if (t1.GetType().Name == "Boolean")
            {
                var b1 = (bool)t1;
                var b2 = (bool)t2;
                if (b2 && !b1) return 1;
                else if (b1 && !b2) return -1;
                return 0;
            }
            else if (t1.GetType().Name == "WidgetState")
            {
                var w1 = (WidgetState)t1;
                var w2 = (WidgetState)t2;

                if (w1 > w2) return 1;
                else if (w1 < w2) return -1;
                else return 0;
            }
            else return 0;
        }
    }
}