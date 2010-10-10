using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PlanetWars.Utility;
using PlanetWarsShared;

namespace PlanetWars.UI
{
    class TabbedControls : System.Windows.Forms.Control
    {
        readonly Dictionary<ToolStripButton, System.Windows.Forms.Control> controls;

        readonly OKCancelBar okCancelBar = new OKCancelBar();

        public TabbedControls(Dictionary<ToolStripButton, System.Windows.Forms.Control> controls)
        {
            this.controls = controls;
            controls.Keys.ForEach(b => b.ImageAlign = ContentAlignment.MiddleLeft);
            ToolTabs = new ToolTabs(controls.Keys.ToArray())
            {Stretch = true, GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Left};
            base.Dock = DockStyle.Fill;
            ToolTabs.ActiveButtonChanged += toolTabs_ActiveButtonChanged;
            Controls.AddRange(new System.Windows.Forms.Control[] {OkCancelBar, ToolTabs});
            controls.Values.ForEach(g => Controls.Add(g));
            controls.Values.ForEach(c => c.BringToFront());
            ToolTabs.ActiveButton = controls.Keys.First();
        }

        public ToolTabs ToolTabs { get; private set; }

        public ToolStripButton ActiveButton
        {
            get { return ToolTabs.ActiveButton; }
            set { ToolTabs.ActiveButton = value; }
        }

        public bool ClearButtonVisible
        {
            get { return OkCancelBar.ClearButtonVisible; }
            set { OkCancelBar.ClearButtonVisible = value; }
        }

        public string ClearButtonText
        {
            get { return OkCancelBar.ClearButtonText; }
            set { OkCancelBar.ClearButtonText = value; }
        }

        public OKCancelBar OkCancelBar
        {
            get { return okCancelBar; }
        }

        void toolTabs_ActiveButtonChanged(object sender, ChangeEventArgs<ToolStripButton> e)
        {
            controls.ForEach(kvp => kvp.Value.Visible = e.NewValue == kvp.Key);
        }

        public event EventHandler OK
        {
            add { OkCancelBar.OK += value; }
            remove { OkCancelBar.OK -= value; }
        }

        public event EventHandler Cancel
        {
            add { OkCancelBar.Cancel += value; }
            remove { OkCancelBar.Cancel -= value; }
        }

        public event EventHandler Clear
        {
            add { OkCancelBar.Clear += value; }
            remove { OkCancelBar.Clear -= value; }
        }
    }
}