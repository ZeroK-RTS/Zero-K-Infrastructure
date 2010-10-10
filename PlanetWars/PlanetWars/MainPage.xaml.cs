using System;
using System.Linq;
using System.Windows.Controls;
using PlanetWars.ServiceReference;

namespace PlanetWars
{
    public partial class MainPage: UserControl
    {
        StarMap mapData;
        public Control CurrentContent
        {
            get { return (Control)TabControl.SelectedContent; }
        }


        public static MainPage Instance { get; set; }
        public event EventHandler<EventArgs<Control>> TabChanged = delegate { };

        public MainPage()
        {
            Instance = this;
            InitializeComponent();
            LoginWindow.SuccessfulLogin += LoginWindow_SuccessfulLogin;
            LoginWindow.NoLogin += LoginWindow_NoLogin;
            LoginPopup.IsOpen = true;
            App.Service.GetMapDataCompleted += Service_GetMapDataCompleted;
            App.Service.GetPlayerDataCompleted += Service_GetPlayerDataCompleted;
            App.PlayerChanged += (s, e) => DataContext = App.Player;
        }


        void LoginWindow_NoLogin(object sender, EventArgs e)
        {
            LoginPopup.IsOpen = false;
        }

        void LoginWindow_SuccessfulLogin(object sender, EventArgs e)
        {
            LoginPopup.IsOpen = false;
            if (mapData != null) App.Player = mapData.Players.SingleOrDefault(p => p.Name == App.UserName);
            App.Service.GetMapDataAsync(App.UserName, App.Password);
        }

        void Service_GetMapDataCompleted(object sender, GetMapDataCompletedEventArgs e)
        {
            mapData = e.Result;
            if (App.UserName != null) {
                App.Player = mapData.Players.SingleOrDefault(p => p.Name == App.UserName); //todo: don't use linear search?
                if (App.Player == null) App.Service.GetMapDataAsync(App.UserName, App.Password);
            }
        }

        void Service_GetPlayerDataCompleted(object sender, GetPlayerDataCompletedEventArgs e)
        {
            App.Player = e.Result;
        }

        void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabControl = (TabControl)sender;
            TabChanged(this, new EventArgs<Control>((Control)tabControl.SelectedContent));
        }
    }
}