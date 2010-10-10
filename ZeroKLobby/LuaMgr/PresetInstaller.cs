using System;
using System.Windows.Forms;
using JetBrains.Annotations;
using LuaManagerLib;

namespace ZeroKLobby.LuaMgr
{
    public partial class PresetInstaller: Form
    {
        readonly WidgetList widgets;

        public PresetInstaller([NotNull] WidgetList widgets)
        {
            if (widgets == null) throw new ArgumentNullException("widgets");
            InitializeComponent();

            this.widgets = widgets;

            foreach (WidgetInfo curWidget in this.widgets.Values) listViewWidgets.Items.Add(curWidget.name);
        }

        void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        void buttonInstall_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}