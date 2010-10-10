using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Collections;
using System.IO;

namespace LuaAdmin
{
    public partial class LuaFileUploadDlg : Form
    {
        protected int luaId;
        public LuaFileUploadDlg( int luaId )
        {
            InitializeComponent();
            this.luaId = luaId;
           
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog fa = new OpenFileDialog();
            fa.Multiselect = false; //later maybe
            fa.ShowDialog();
            if (fa.FileName.Length > 0 )
            {
                this.textBox2.Text = fa.FileName;

                string t = fa.FileName.Replace("\\", "/");

                int idx = textBox1.Text.IndexOf("spring");
                if (idx > 0 )
                {
                    t = t.Substring(idx + 7);
                }
                
                textBox1.Text = t;
            }
        }

        private void buttonUpload_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0 || textBox2.Text.Length == 0)
            {
                MessageBox.Show("Missing Data!");
            }

            String path = textBox1.Text.Replace("\\", "/");
            if ( !path.StartsWith("/") )
            {
                path = "/" + path;
            }
            Program.fetcher.addLuaFile( path, textBox2.Text, this.luaId);

            this.Close();
        }

        private void buttonBrowseDir_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fb = new FolderBrowserDialog();
            fb.ShowDialog();

            if (fb.SelectedPath.Length > 0)
            {
                this.textBoxDirName.Text = fb.SelectedPath;

                string t = fb.SelectedPath.Replace("\\", "/");

                int idx = textBoxDirName.Text.IndexOf("spring");
                if (idx > 0)
                {
                    t = t.Substring(idx + 7);
                }

                textBoxDirNameLocal.Text = t;
            }
        }

        private void buttonUploadDir_Click(object sender, EventArgs e)
        {
            if (textBoxDirName.Text.Length == 0 || textBoxDirNameLocal.Text.Length == 0)
            {
                MessageBox.Show("Missing Data!");
                return;
            }

            string[] files = Directory.GetFiles( textBoxDirName.Text, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string curFile = ((string)files[i]).Replace('\\', '/' );

                int start = curFile.IndexOf( textBoxDirNameLocal.Text );
                if (start < 0)
                {
                    MessageBox.Show("Your selected local path has to be entirely included in the source path!");
                    return;
                }

                string localP = textBoxDirNameLocal.Text;
                //localP += "/";
                localP += curFile.Substring( start + textBoxDirNameLocal.Text.Length );

                Program.fetcher.addLuaFile(localP, curFile, this.luaId);
            }

            this.Close();
        }

    }
}
