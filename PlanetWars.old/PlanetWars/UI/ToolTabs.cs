using System;
using System.Windows.Forms;
using PlanetWars.Utility;

namespace PlanetWars.UI
{
    public class ToolTabs : ToolStrip
    {
        ToolStripButton activeButton;

        public ToolTabs(params ToolStripButton[] buttons)
        {
            foreach (ToolStripButton button in buttons) {
                base.Items.Add(button);
                button.Click += (s, e) => ActiveButton = (ToolStripButton)s;
            }
        }

        public ToolStripButton ActiveButton
        {
            get { return activeButton; }
            set
            {
                if (activeButton == value) {
                    return;
                }
                if (activeButton != null) {
                    activeButton.Checked = false;
                }
                value.Checked = true;
                ToolStripButton oldButton = activeButton;
                activeButton = value;
                OnActiveButtonChanged(oldButton, activeButton);
            }
        }

        public event EventHandler<ChangeEventArgs<ToolStripButton>> ActiveButtonChanged;

        void OnActiveButtonChanged(ToolStripButton oldButton, ToolStripButton newButton)
        {
            if (ActiveButtonChanged != null) {
                ActiveButtonChanged(this, new ChangeEventArgs<ToolStripButton>(oldButton, newButton));
            }
        }
    }
}