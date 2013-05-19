using System;
using System.ComponentModel;
using System.Linq;

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
                if (changed) InvokePropertyChanged("IsAlerting");
            }
        }
        public bool IsSelected {
            get { return isSelected; }
            set {
                var changed = isSelected != value;
                isSelected = value;
                if (changed) InvokePropertyChanged("IsSelected");
            }
        }
        public string Label { get; set; }
        /// <summary>
        /// If true, lobby wont remember subpath for this button and instead go directly to target location
        /// </summary>
        public bool LinkBehavior;
        public string TargetPath;
        public bool Visible { get; set; }

        public ButtonInfo() {
            Visible = true;
        }

        void InvokePropertyChanged(string name) {
            var changed = PropertyChanged;
            if (changed != null) changed(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
    }
}