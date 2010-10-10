using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SpringDownloader.MicroLobby
{
	public class ToolTabs: UserControl
	{
		ToolStripButton activeButton;
		readonly Dictionary<string, Control> controls = new Dictionary<string, Control>();
		readonly List<string> flashList = new List<string>();
		ToolStripItem lastHoverItem;
		readonly Panel panel = new Panel { Dock = DockStyle.Fill };
		readonly ToolStrip toolStrip = new ToolStrip
		                               {
		                               	Dock = DockStyle.Left,
		                               	Stretch = false,
		                               	GripStyle = ToolStripGripStyle.Hidden,
		                               	ShowItemToolTips = false,
		                               	AutoSize = false,
		                               	RenderMode = ToolStripRenderMode.System,
		                               	Width = 155,
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


		public ToolTabs()
		{
			toolStrip.BackColor = Color.White;
			Controls.Add(panel);
			Controls.Add(toolStrip);
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
			if (control is BattleChatControl) button.Height = button.Height*2;
			button.MouseHover += button_MouseHover;
			button.MouseDown += (s, e) =>
				{
					if (e.Button == MouseButtons.Right)
					{
						var point = new Point(button.Bounds.Location.X + e.X, button.Bounds.Location.Y + e.Y);
						Program.ToolTip.Visible = false;
						if (control is ChatControl) ContextMenus.GetChannelContextMenu((ChatControl)control).Show(toolStrip, point);
						else if (control is PrivateMessageControl) ContextMenus.GetPrivateMessageContextMenu((PrivateMessageControl)control).Show(toolStrip, point);
						Program.ToolTip.Visible = true;
					}
					else if (e.Button == MouseButtons.Middle && control is ChatControl)
					{
						var chatControl = (ChatControl)control;
						if (chatControl.CanLeave) ActionHandler.CloseChannel(chatControl.ChannelName);
					}
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

			button.Click += (s, e) => ActiveButton = (ToolStripButton)s;
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

		public bool Flash(string tabName)
		{
			if (!toolStrip.Items.ContainsKey(tabName)) return false;
			var button = (ToolStripButton)toolStrip.Items[tabName];
			if (ActiveButton == button) return false;

			if (flashList.Contains(tabName)) return false;
			flashList.Add(tabName);
			var timer = new Timer { Interval = 1000 };

			timer.Tick += (s, e) => button.BackColor = button.BackColor == Color.SkyBlue ? Color.Empty : Color.SkyBlue;
			timer.Start();
			button.Click += (s, e) =>
				{
					timer.Stop();
					flashList.Remove(tabName);
					button.BackColor = Color.Empty;
				};
			return true;
		}

		public Control GetTab(string name)
		{
			Control control;
			controls.TryGetValue(name, out control);
			return control;
		}

		public bool Hilite(string tabName)
		{
			if (!toolStrip.Items.ContainsKey(tabName)) return false;
			var button = (ToolStripButton)toolStrip.Items[tabName];
			if (ActiveButton == button) return false;

			var oldFont = button.Font;
			button.Font = new Font(oldFont.FontFamily, oldFont.SizeInPoints, FontStyle.Bold | FontStyle.Italic, oldFont.Unit);
			// button.ForeColor = Color.Red;
			button.Click += (s, e) =>
				{
					button.Font = button.Font = new Font(oldFont.FontFamily, oldFont.SizeInPoints, FontStyle.Regular, oldFont.Unit);
					//button.ForeColor = Color.Empty;
				};
			return true;
		}

		public void RemoveTab(string key)
		{
			panel.Controls.RemoveByKey(key);
			controls.Remove(key);
			toolStrip.Items.RemoveByKey(key);
		}

		public void SelectTab(string name)
		{
			ActiveButton = (ToolStripButton)toolStrip.Items[name];
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