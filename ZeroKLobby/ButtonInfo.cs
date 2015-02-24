using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZeroKLobby.Controls;

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
                if (changed) {
                    button.ForeColor = isSelected ? Color.Aqua : Color.White;
                    button.ButtonStyle = isSelected ? FrameBorderRenderer.StyleType.DarkHiveGlow : FrameBorderRenderer.StyleType.DarkHive;
                    InvokePropertyChanged("IsSelected");
                }
            }
        }
        public int Height { get; set; }
        public int Width { get; set; }
        public string Label { get; set; }
        /// <summary>
        /// If true, lobby wont remember subpath for this button and instead go directly to target location
        /// </summary>
        public string TargetPath;
        BitmapButton button;
        public bool Visible { get; set; }
        public DockStyle Dock { get; set; }

        public Image Icon { get; set; }

        public ButtonInfo() {
            Visible = true;
            Width = 42;
            Height = 42;
            Dock = DockStyle.Left;
        }

        void InvokePropertyChanged(string name) {
            var changed = PropertyChanged;
            if (changed != null) changed(this, new PropertyChangedEventArgs(name));
        }

        public Control GetButton() {
            button = new BitmapButton();
            button.SoundType = SoundPalette.SoundType.Click;
            //button.AutoSize = true;
            //button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Height = Height;
            button.Width = Width;
            button.TextAlign = ContentAlignment.MiddleCenter;
            //button.Text = Label;
            button.Margin = new Padding(0, 0, 0, 3);
            button.Cursor = Cursors.Hand;
            //button.Dock = Dock;
            if (Icon != null) {
                button.Image = Icon;
                //button.ImageAlign = ContentAlignment.MiddleLeft;
                button.TextAlign = ContentAlignment.MiddleCenter;
                button.TextImageRelation = TextImageRelation.ImageBeforeText;
            }
            button.Click += (sender, args) => {
                var navigator = Program.MainWindow.navigationControl;
                var panel = Program.MainWindow.panelRight;
                if (navigator.CurrentNavigatable == navigator.GetNavigatableFromPath(TargetPath)) panel.Visible = !panel.Visible;
                else panel.Visible = true;
                if (panel.Visible) {
                    Program.MainWindow.navigationControl.SwitchTab(TargetPath);
                }
                else {
                    Program.MainWindow.navigationControl.Path = "";
                }
            };
            return button;

        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}