using System.Windows.Controls;
using PlanetWars.ServiceReference;

namespace PlanetWars.Tabs
{
    public partial class PlayersTab: UserControl
    {
        public PlayersTab()
        {
            InitializeComponent();
            MainPage.Instance.TabChanged += Instance_TabChanged;
            App.Service.GetMapDataCompleted += Service_GetMapDataCompleted;
            App.Service.GetPlayerListCompleted += Service_GetPlayerListCompleted;
        }

        void Instance_TabChanged(object sender, EventArgs<Control> e)
        {
            if (e.Value != this) return;
            App.Service.GetPlayerListAsync();
        }

        void Service_GetMapDataCompleted(object sender, GetMapDataCompletedEventArgs e)
        {
            PlanetGrid.ItemsSource = e.Result.Players;
        }

        void Service_GetPlayerListCompleted(object sender, GetPlayerListCompletedEventArgs e)
        {
            PlanetGrid.ItemsSource = e.Result;
        }
    }
}