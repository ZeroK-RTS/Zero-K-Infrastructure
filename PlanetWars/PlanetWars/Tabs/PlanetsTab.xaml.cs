using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using PlanetWars.ServiceReference;

namespace PlanetWars.Tabs
{
    public partial class PlanetsTab: UserControl
    {
        IEnumerable<CelestialObject> bodies;

        PropertyGroupDescription ownerGroupDescription = new PropertyGroupDescription("OwnerName");
        PropertyGroupDescription ownerSystemGroupDescription = new PropertyGroupDescription("Owner.SystemID");
        PropertyGroupDescription parentGroupDescription = new PropertyGroupDescription("Parent.Name");
        PropertyGroupDescription systemGroupDescription = new PropertyGroupDescription("ParentStar.Name");
        PagedCollectionView view;

        public PlanetsTab()
        {
            InitializeComponent();
            MainPage.Instance.TabChanged += Instance_TabChanged;
            App.Service.GetMapDataCompleted += Service_GetMapDataCompleted;
        }

        void FilterBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            view.Filter = null;
            view.Filter = delegate(object obj)
                {
                    var body = (CelestialObject)obj;
                    return body.Name.ToUpper().Contains(FilterBox.Text.ToUpper());
                };
        }

        void Instance_TabChanged(object sender, EventArgs<Control> e)
        {
            if (e.Value != this) return;
            App.Service.GetMapDataAsync(App.UserName, App.Password);
        }

        void OwnerCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Add(ownerGroupDescription);
        }

        void OwnerCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Remove(ownerGroupDescription);
        }

        void OwnerSystemCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Add(ownerSystemGroupDescription);
        }

        void OwnerSystemCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Remove(ownerSystemGroupDescription);
        }

        void ParentCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Add(parentGroupDescription);
        }

        void ParentCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Remove(parentGroupDescription);
        }

        void PlanetIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var image = (StackPanel)sender;
            PlanetGrid.ScrollIntoView(image.DataContext, PlanetGrid.Columns[0]);
            PlanetGrid.SelectedItem = image.DataContext;
        }

        void Service_GetMapDataCompleted(object sender, GetMapDataCompletedEventArgs e)
        {
            bodies = e.Result.CelestialObjects;
            view = new PagedCollectionView(bodies);
            PlanetGrid.ItemsSource = view;
        }

        void SystemCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Add(systemGroupDescription);
        }

        void SystemCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            view.GroupDescriptions.Remove(systemGroupDescription);
        }
    }
}