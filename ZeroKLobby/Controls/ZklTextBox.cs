using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    internal class ZklTextBox: UserControl
    {
        private const int BorderSize = 2;
        private readonly TextBox textBox = new TextBox();

        public TextBox TextBox => textBox;

        public ZklTextBox() {
            ZklBaseControl.Init(this);
            ZklBaseControl.Init(textBox);
            textBox.BorderStyle = BorderStyle.None;
            textBox.ForeColor = Config.TextColor;
            textBox.KeyDown += (sender, args) => { OnKeyDown(args); };
            textBox.KeyUp += (sender, args) => { OnKeyUp(args); };
            textBox.MouseDown += (sender, args) => { OnMouseDown(args); };
            textBox.MouseUp += (sender, args) => { OnMouseUp(args); };
            textBox.Click += (sender, args) => { OnClick(args); };
            textBox.TextChanged += (sender, args) => { OnTextChanged(args); };
            Controls.Add(textBox);
        }


        public override Font Font { get { return textBox.Font; } set { textBox.Font = value; } }

        public override string Text { get { return textBox.Text; } set { textBox.Text = value; } }

        public void SelectAll() {
            textBox.SelectAll();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            textBox.Size = new Size(Width - BorderSize*2, Height - BorderSize*2);
            textBox.Location = new Point(BorderSize, BorderSize);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            using (var pen = new Pen(Config.TextBoxBorderColor, BorderSize)) e.Graphics.DrawRectangle(pen, ClientRectangle);
        }
    }
}