using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using LuaManagerLib;

//using System.Linq;

namespace ZeroKLobby
{
    public partial class FilesDisplay: Form
    {
        protected LinkedList<FileInfo> m_fileList;


        public FilesDisplay(LinkedList<FileInfo> files)
        {
            InitializeComponent();

            m_fileList = files;

            //   listView2.GridLines = true;
            // listView2.AllowColumnReorder = true;
            // Display check boxes.
            //listView2.CheckBoxes = true;
            // Select the item and subitems when selection is made.
            //listView2.FullRowSelect = false;
            // listView2.Columns.Add("Name");
            //  listView2.Columns.Add("Status");

            IEnumerator ienum = files.GetEnumerator();
            FileInfo file;
            while (ienum.MoveNext())
            {
                file = (FileInfo)ienum.Current;
                //listView1.Items.Add(file.Url + " -> " + file.localPath + " MD5: " + file.Md5 );
                var item = new ListViewItem(file.localPath);
                // item.SubItems.Add();
                item.SubItems.Add(file.Url);
                item.SubItems.Add(file.Md5);

                listView1.Items.Add(item);
            }
        }
    }
}