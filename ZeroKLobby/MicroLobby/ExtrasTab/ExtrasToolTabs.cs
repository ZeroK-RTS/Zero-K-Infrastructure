using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
    public class ExtrasToolTabs : UserControl
    {
        ToolStripButton activeButton;
        readonly Dictionary<string, Control> controls = new Dictionary<string, Control>();
        ToolStripItem lastHoverItem;
        readonly Panel panel = new Panel { Dock = DockStyle.Fill };
        readonly ToolStrip toolStrip = new ToolStrip
                                       {
                                        Dock = DockStyle.Left,
                                        Stretch = false,
                                        GripStyle = ToolStripGripStyle.Hidden,
                                        ShowItemToolTips = false,
                                        Tag = HiliteLevel.None,
                                        RenderMode = ToolStripRenderMode.System,
                                        //AutoSize = false,
                                        //Width = 155,
                                        AutoSize = true, //auto reduce space usage
                                        MaximumSize = new Size(155,4000),
                                        MinimumSize = new Size(100,0),
                                       };

        public ToolStripButton ActiveButton
        {
            get { return activeButton; }
            set
            {
                if (activeButton == value) return;
                if (activeButton != null) activeButton.Checked = false;
                value.Checked = true;
                activeButton = value;
                foreach (Control control in panel.Controls) control.Visible = control == controls[activeButton.Name];
            }
        }

        public IEnumerable<Control> Tabs { get { return controls.Values; } }


        public ExtrasToolTabs()
        {
            toolStrip.BackColor = Program.Conf.BgColor; //Color.White;
            toolStrip.ForeColor = Program.Conf.TextColor;
            panel.BackColor = Program.Conf.BgColor;
            panel.ForeColor = Program.Conf.TextColor;
            BackColor = Program.Conf.BgColor;
            ForeColor = Program.Conf.TextColor;
            Controls.Add(panel);
            Controls.Add(toolStrip);

            var timer = new Timer { Interval = 1000 };

            timer.Tick += (s, e) =>
                {
                    foreach (var button in toolStrip.Items.OfType<ToolStripButton>())
                    {
                        if (button.Tag is HiliteLevel && ((HiliteLevel)button.Tag) == HiliteLevel.Flash) button.BackColor = button.BackColor == Color.SkyBlue ? Color.Empty : Color.SkyBlue;
                    }
                };

            timer.Start();
        }


        public void DisposeAllTabs()
        {
            foreach (var control in controls.Values) control.Dispose();
            ClearTabs();
        }

        public void AddTab(string name, string title, Control control, Image icon, string tooltip, int sortImportance)
        {
            var button = new ToolStripButton(name, icon)
                         {
                            Name = name,
                            Alignment = ToolStripItemAlignment.Left,
                            TextAlign = ContentAlignment.MiddleLeft,
                            ImageAlign = ContentAlignment.MiddleLeft,
                            AutoToolTip = false,
                            ToolTipText = tooltip,
                            Tag = sortImportance,
                            Text = title,
                         };
            if (control is SkirmishControl) button.Height = button.Height*2;
            button.MouseHover += button_MouseHover;
            button.MouseDown += (s, e) =>
                {
                   
                };

            var added = false;
            var insertItemText = sortImportance + Name;
            for (var i = 0; i < toolStrip.Items.Count; i++)
            {
                var existingItemText = (int)toolStrip.Items[i].Tag + toolStrip.Items[i].Text;
                if (String.Compare(existingItemText, insertItemText) < 0)
                {
                    toolStrip.Items.Insert(i, button);
                    added = true;
                    break;
                }
            }
            if (!added) toolStrip.Items.Add(button);

            button.Click += (s, e) =>
                {
                    try
                    {
                        NavigationControl.Instance.Path = "spextended/" + name;
                    } catch(Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                };
            control.Dock = DockStyle.Fill;
            control.Visible = false;
            controls.Add(name, control);
            panel.Controls.Add(control);
        }

        public void ClearTabs()
        {
            controls.Clear();
            toolStrip.Items.Clear();
            panel.Controls.Clear();
        }


        public bool SetHilite(string tabName, HiliteLevel level)
        {
            if (!toolStrip.Items.ContainsKey(tabName)) return false;
            var button = (ToolStripButton)toolStrip.Items[tabName];
            HiliteLevel? current = button.Tag as HiliteLevel?;
            if (current != null && level == HiliteLevel.Bold && current.Value == HiliteLevel.Flash) return false; // dont change from flash to bold
            button.Tag = level;
            var oldFont = button.Font;
            switch (level)
            {
                    case HiliteLevel.None:
                    button.BackColor = Color.Empty;
                    button.Font = new Font(oldFont.FontFamily, oldFont.SizeInPoints, FontStyle.Regular, oldFont.Unit, oldFont.GdiCharSet);
                    //oldFont.Dispose();
                    break;
                    case HiliteLevel.Bold:
                    button.BackColor = Color.Empty;
                    button.Font = new Font(oldFont.FontFamily, oldFont.SizeInPoints, FontStyle.Bold | FontStyle.Italic, oldFont.Unit, oldFont.GdiCharSet);
                    //oldFont.Dispose();
                    break;
                    case HiliteLevel.Flash:
                    button.Font = new Font(oldFont.FontFamily, oldFont.SizeInPoints, FontStyle.Bold, oldFont.Unit, oldFont.GdiCharSet);
                    //oldFont.Dispose();

                    break;
            }
            return true;
        }

        public void SelectTab(string name)
        {
            ActiveButton = (ToolStripButton)toolStrip.Items[name];
        }

        public string GetNextTabPath()
        {
            return this.GetAdjTabPath(true);
        }
        public string GetPrevTabPath()
        {
            return this.GetAdjTabPath(false);
        }
        private string GetAdjTabPath(bool next)
        {
            var path = "extra/";

            var nextControl = this.GetNextControl(controls[activeButton.Name], next);
            var nextButtonName = nextControl.Name;
            if (nextButtonName != "")
            {
                path += nextButtonName;
            }
            return path;
        }

        public void SetIcon(string tabName, Image icon)
        {
            var button = (ToolStripButton)toolStrip.Items[tabName];
            button.Image = icon;
        }

        void button_MouseHover(object sender, EventArgs e)
        {
            var item = (ToolStripItem)sender;
            if (item != lastHoverItem)
            {
                lastHoverItem = null;
                Program.ToolTip.SetText(toolStrip, item.ToolTipText);
            }
        }
    }
}
