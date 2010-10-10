using System.Linq;
using System.Windows.Controls;
using PlanetWars.ServiceReference;

namespace PlanetWars.Tabs
{
    public partial class StarSystemTab: UserControl
    {
        public StarSystemTab()
        {
            InitializeComponent();
            MainPage.Instance.TabChanged += Instance_TabChanged;
            App.Service.GetPlayerListCompleted += Service_GetPlayerListCompleted;
        }


        void Instance_TabChanged(object sender, EventArgs<Control> e)
        {
            if (e.Value != this) return;
            App.Service.GetPlayerListAsync();
        }

        void Service_GetPlayerListCompleted(object sender, GetPlayerListCompletedEventArgs e)
        {
            if (App.Player == null) return;
            PlayerGrid.ItemsSource = e.Result.Where(p => p.SystemID == App.Player.SystemID);
        }
    }
}