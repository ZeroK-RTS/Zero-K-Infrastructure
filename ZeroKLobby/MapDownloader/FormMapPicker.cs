using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MapDownloader
{
    public partial class FormMapPicker: Form
    {
        public List<ResourceData> Options = new List<ResourceData>();
        public List<string> Result = new List<string>();


        public FormMapPicker()
        {
            InitializeComponent();
        }

        public void LoadList(bool force)
        {
            if (!force && Options.Count > 0) return;
            using (var service = new PlasmaService { Proxy = null }) {
                service.GetResourceListCompleted += (s, e) =>
                    {
                        if (e.Error == null && !e.Cancelled && e.Result != null)
                        {
                            Options = new List<ResourceData>(e.Result);
                            FilterOptions();
                        }
                        progressBar1.Visible = false;
                        btnReload.Enabled = true;
                    };
                progressBar1.Visible = true;
                btnReload.Enabled = false;
                service.GetResourceListAsync();
            }
        }


        void FilterOptions()
        {
            var words = textBox1.Text.ToLower().Split(' ');
            var res = (from p in Options
                       where rbMods.Checked || p.ResourceType != ResourceType.Mod
                       where rbMaps.Checked || p.ResourceType != ResourceType.Map
                       where !words.Any(t => !String.IsNullOrEmpty(t) && !p.InternalName.ToLower().Contains(t))
                       select p.InternalName).ToList();
            res.Sort((a, b) => a.CompareTo(b));

            var prevSel = listBox1.SelectedItems.Cast<object>().Select(o => o as string).ToList();

            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (var p in res) {
                listBox1.Items.Add(p);
                if (prevSel.Contains(p)) listBox1.SelectedItems.Add(p);
            }
            listBox1.EndUpdate();
        }

        void btnAll_Click(object sender, EventArgs e)
        {
            Result = new List<string>(Options.Select(x => x.InternalName));
            Close();
        }

        void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        void btnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Result.Clear();
            foreach (var o in listBox1.SelectedItems) Result.Add(o as string);
            Close();
        }

        void btnReload_Click(object sender, EventArgs e)
        {
            LoadList(true);
        }

        void FormMapPicker_Load(object sender, EventArgs e)
        {
            Icon = Resources.ZkIcon;
            FilterOptions();
        }

        void rbMaps_CheckedChanged(object sender, EventArgs e)
        {
            FilterOptions();
        }

        void rbMods_CheckedChanged(object sender, EventArgs e)
        {
            FilterOptions();
        }

        void textBox1_TextChanged(object sender, EventArgs e)
        {
            FilterOptions();
        }
    }
}