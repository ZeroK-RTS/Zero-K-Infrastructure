using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZkData;

namespace ZeroKLobby
{
    public partial class SelectWritableFolder : Form
    {
        public SelectWritableFolder()
        {
            InitializeComponent();
        }

        public string SelectedPath { get { return tbFolder.Text; } set { tbFolder.Text = value; } }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            try {
                Directory.CreateDirectory(tbFolder.Text);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void SelectWritableFolder_Load(object sender, EventArgs e) {
            Icon = ZklResources.ZkIcon;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(tbFolder.Text)) {
                if (
                    MessageBox.Show("Folder does not exist, do you want to create it?",
                                    "Create folder?",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question) == DialogResult.No) return;
            }

            if (!SpringPaths.IsDirectoryWritable(tbFolder.Text)) {
                MessageBox.Show(string.Format("Directory {0} is not writable", tbFolder.Text));
            }
            else {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void browseFolderbutton_Click(object sender, EventArgs e)
        {
            //copied from: http://stackoverflow.com/questions/13126008/how-can-i-get-path-folder-browser-dialog
            using(var dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    tbFolder.Text = dialog.SelectedPath;    
                }
            }
        }
    }
}
