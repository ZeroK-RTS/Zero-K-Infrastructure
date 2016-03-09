using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    internal class ZklTextBox: UserControl
    {
        private readonly TextBox textBox = new TextBox();

        public ZklTextBox() {
            ZklBaseControl.Init(this);
            ZklBaseControl.Init(textBox);
            textBox.BorderStyle = BorderStyle.None;
            textBox.ForeColor = Config.TextColor;

            Paint += UserControl1_Paint;
            Resize += UserControl1_Resize;

            Controls.Add(textBox);
        }

        public override Font Font { get { return textBox.Font; } set { textBox.Font = value; } }

        public override string Text { get { return textBox.Text; } set { textBox.Text = value; } }

        public void SelectAll() {
            textBox.SelectAll();
        }

        private void UserControl1_Resize(object sender, EventArgs e) {
            textBox.Size = new Size(Width - 4, Height - 4);
            textBox.Location = new Point(2, 2);
        }

        private void UserControl1_Paint(object sender, PaintEventArgs e) {
            using (var pen = new Pen(Config.TextBoxBorderColor, 2)) e.Graphics.DrawRectangle(pen, ClientRectangle);
        }
    }
}