using System;
using System.Globalization;
using System.Windows.Forms;
using LuaManagerLib;

//using System.Linq;

namespace ZeroKLobby.LuaMgr
{
    public partial class Changelog: Form
    {

        public Changelog(int nameId)
        {
            InitializeComponent();

            /////////////////////
            var versionLuas = WidgetHandler.fetcher.getLuasByNameId(nameId);
            var sorted = versionLuas.getAsSortedByVersion();

            var nl = Environment.NewLine;
            textBox1.Text = "Version History:" + nl;
            textBox1.Text += "===========" + nl;
            textBox1.Text += nl;

            var ienum = sorted.GetEnumerator();
            while (ienum.MoveNext())
            {
                var info = (WidgetInfo)ienum.Current;

                textBox1.Text += "v" + info.version.ToString("G29", CultureInfo.InvariantCulture) + nl;
                textBox1.Text += "------" + nl;

                textBox1.Text += info.changelog + nl;
                textBox1.Text += nl + nl;
            }

            textBox1.Select(0, 0);
        }
    }
}