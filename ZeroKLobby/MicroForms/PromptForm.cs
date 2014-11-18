using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ZeroKLobby.MicroForms
{
    public partial class PromptForm : Form
    {
        const int formHeight = 248;
        const int detailHeight = 101;
        const int formWidth = 216;

        public PromptForm()
        {
            InitializeComponent();
        }

        private void detailBox_VisibleChanged(object sender, EventArgs e)
        {
            if (DesignMode) return;
            if (!detailBox.Visible)
            {
                DpiMeasurement.DpiXYMeasurement();
                Size = new Size(DpiMeasurement.ScaleValueX(formWidth), DpiMeasurement.ScaleValueY(formHeight - detailHeight));
            }
            else
            {
                DpiMeasurement.DpiXYMeasurement();
                Size = new Size(DpiMeasurement.ScaleValueX(formWidth), DpiMeasurement.ScaleValueY(formHeight));
            }
        }
    }
}
