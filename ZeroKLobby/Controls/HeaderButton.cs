using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ZeroKLobby.Controls {

    public class HeaderButton : Button {
        static HeaderButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderButton), new FrameworkPropertyMetadata(typeof(HeaderButton)));
        }

        #region IsSelected

        /// <summary>
        /// IsSelected Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(HeaderButton),
                new PropertyMetadata((bool)false));

        /// <summary>
        /// Gets or sets the IsSelected property.
        /// </summary>
        public bool IsSelected {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        #endregion

        #region IsAlerting

        /// <summary>
        /// IsAlerting Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsAlertingProperty =
            DependencyProperty.Register("IsAlerting", typeof(bool), typeof(HeaderButton),
                new PropertyMetadata((bool)false));

        /// <summary>
        /// Gets or sets the IsAlerting property.
        /// </summary>
        public bool IsAlerting {
            get { return (bool)GetValue(IsAlertingProperty); }
            set { SetValue(IsAlertingProperty, value); }
        }

        #endregion
    }
}
