using System;
using System.Windows;
using System.Windows.Controls;
using PlanetWars.ServiceReference;

namespace PlanetWars.Tabs
{
    public partial class MothershipTab: UserControl
    {
        public MothershipTab()
        {
            InitializeComponent();
            MainPage.Instance.TabChanged += Instance_TabChanged;
            App.Service.GetPlayerDataCompleted += Service_GetPlayerDataCompleted;
            App.Service.GetMotherhipModuleBuildOptionsCompleted += Service_GetMotherhipModuleBuildOptionsCompleted;
            App.PlayerChanged += App_PlayerChanged;
        }

        void App_PlayerChanged(object sender, EventArgs e)
        {
            if (App.Player.MothershipStructures == null) return;
            PodList.ItemsSource = App.Player.MothershipStructures;
            ExperienceBar.Minimum = App.Player.ThisLevelXP;
            ExperienceBar.Maximum = App.Player.NextLevelXP;
            ExperienceBar.Value = App.Player.XP - App.Player.ThisLevelXP;
            NextLevelBlock.Text = "Level " + (App.Player.Level + 1);
        }

        void BuyButton_Click(object sender, RoutedEventArgs _e)
        {
            var button = (Button)sender;
            var option = (StructureOption)button.DataContext;
            var service = new PlanetWarsServiceClient();
            service.BuildMothershipModuleCompleted += (s, e) =>
                {
                    MessageBox.Show(e.Result.Response.ToString());
                    if (e.Result.Response == BuildResponse.Ok) {
                        PodBuildList.ItemsSource = e.Result.NewStructureOptions;
                        App.Player = e.Result.Player;
                    }
                };
            service.BuildMothershipModuleAsync(App.UserName, App.Password, option.StructureType.StructureTypeID);
        }


        void Instance_TabChanged(object sender, EventArgs<Control> e)
        {
            if (e.Value != this) return;
            if (App.UserName == null) return;
            App.Service.GetPlayerDataAsync(App.UserName);
            App.Service.GetMotherhipModuleBuildOptionsAsync(App.UserName, App.Password);
        }

        void SellButton_Click(object sender, RoutedEventArgs _e)
        {
            var button = (Button)sender;
            var structure = (MothershipStructure)button.DataContext;
            var service = new PlanetWarsServiceClient();
            service.SellMotherhipModuleCompleted += (s, e) =>
                {
                    MessageBox.Show(e.Result.Response.ToString());
                    if (e.Result.Response == BuildResponse.Ok) {
                        PodBuildList.ItemsSource = e.Result.NewStructureOptions;
                        App.Player = e.Result.Player;
                    }
                };
            service.SellMotherhipModuleAsync(App.UserName, App.Password, structure.StructureType.StructureTypeID);
        }

        void Service_GetMotherhipModuleBuildOptionsCompleted(object sender, GetMotherhipModuleBuildOptionsCompletedEventArgs e)
        {
            PodBuildList.ItemsSource = e.Result;
        }

        void Service_GetPlayerDataCompleted(object sender, GetPlayerDataCompletedEventArgs e)
        {
            DataContext = e.Result;
        }
    }
}