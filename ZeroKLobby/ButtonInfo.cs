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
                button.ForeColor = isAlerting ? Color.Red : SystemColors.MenuText;
                if (changed) InvokePropertyChanged("IsAlerting");
            }
        }
        public bool IsSelected {
            get { return isSelected; }
            set {
                var changed = isSelected != value;
                isSelected = value;
                button.BackColor = isSelected ? Color.PowderBlue : SystemColors.ButtonFace;
                if (changed) InvokePropertyChanged("IsSelected");
            }
        }
        public string Label { get; set; }
        /// <summary>
        /// If true, lobby wont remember subpath for this button and instead go directly to target location
        /// </summary>
        public bool LinkBehavior;
        public string TargetPath;
        Button button;
        public bool Visible { get; set; }

        public ButtonInfo() {
            Visible = true;
        }

        void InvokePropertyChanged(string name) {
            var changed = PropertyChanged;
            if (changed != null) changed(this, new PropertyChangedEventArgs(name));
        }

        public Control GetButton() {
            button = new Button();
            button.Text = Label;
            button.Click += (sender, args) =>
                { Program.MainWindow.navigationControl.Path = TargetPath; };
            return button;

        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}