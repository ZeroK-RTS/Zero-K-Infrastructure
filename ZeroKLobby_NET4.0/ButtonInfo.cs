using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby
{
    public class ButtonInfo: INotifyPropertyChanged
    {
        bool isAlerting;
        bool isSelected;
        public bool IsAlerting {
            get { return isAlerting; }
            set {
                var changed = isAlerting != value;
                isAlerting = value;
                button.BackColor = isAlerting ? Color.Red : Color.Transparent;
                if (changed) InvokePropertyChanged("IsAlerting");
            }
        }
        public bool IsSelected {
            get { return isSelected; }
            set {
                var changed = isSelected != value;
                isSelected = value;
                //button.BackColor = isSelected ? Color.PowderBlue : SystemColors.ButtonFace;
                button.ForeColor = isSelected ? Color.Aqua: Color.White;
                if (changed) InvokePropertyChanged("IsSelected");
            }
        }
        public int Height { get; set; }
        public int Width { get; set; }
        public string Label { get; set; }
        /// <summary>
        /// If true, lobby wont remember subpath for this button and instead go directly to target location
        /// </summary>
        public string TargetPath;
        Button button;
        public bool Visible { get; set; }
        public DockStyle Dock { get; set; }

        public Bitmap Icon { get; set; }

        public ButtonInfo() {
            Visible = true;
            Width = 100;
            Height = 25;
            Dock = DockStyle.Left;
        }

        void InvokePropertyChanged(string name) {
            var changed = PropertyChanged;
            if (changed != null) changed(this, new PropertyChangedEventArgs(name));
        }

        public Control GetButton() {
            button = new BitmapButton();
            //button.AutoSize = true;
            //button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Height = Height;
            button.Width = Width;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Text = Label;
            button.Margin = new Padding(0, 0, 0, 3);
            button.Cursor = Cursors.Hand;
            //button.Dock = Dock;
            if (Icon != null) {
                button.Image = Icon;
                //button.ImageAlign = ContentAlignment.MiddleLeft;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.TextImageRelation = TextImageRelation.ImageBeforeText;
            }
            button.Click += (sender, args) =>
                { Program.MainWindow.navigationControl.SwitchTab(TargetPath); };
            return button;

        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}