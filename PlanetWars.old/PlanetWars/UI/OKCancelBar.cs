using System;
using System.Windows.Forms;
using PlanetWars.Properties;

namespace PlanetWars.UI
{
    class OKCancelBar : ToolStrip
    {
        readonly ToolStripButton cancelButton = new ToolStripButton
        {Text = "Cancel", Alignment = ToolStripItemAlignment.Right, Image = Resources.button_cancel,};

        readonly ToolStripButton clearButton = new ToolStripButton
        {Text = "Clear", Image = Resources.editclear, Visible = false,};

        readonly ToolStripButton okButton = new ToolStripButton
        {Text = "OK", Alignment = ToolStripItemAlignment.Right, Image = Resources.apply,};

        public OKCancelBar()
        {
            base.Items.AddRange(new[] {clearButton, cancelButton, okButton});
            Stretch = true;
            GripStyle = ToolStripGripStyle.Hidden;
            base.Dock = DockStyle.Bottom;
        }

        public bool ClearButtonVisible
        {
            get { return clearButton.Visible; }
            set { clearButton.Visible = value; }
        }

        public string ClearButtonText
        {
            get { return clearButton.Text; }
            set { clearButton.Text = value; }
        }

        public event EventHandler OK
        {
            add { okButton.Click += value; }

            remove { okButton.Click -= value; }
        }

        public event EventHandler Cancel
        {
            add { cancelButton.Click += value; }

            remove { cancelButton.Click -= value; }
        }

        public event EventHandler Clear
        {
            add { clearButton.Click += value; }

            remove { clearButton.Click -= value; }
        }
    }
}