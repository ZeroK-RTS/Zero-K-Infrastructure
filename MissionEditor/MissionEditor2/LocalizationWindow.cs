using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for LocalizationWindow.xaml
    /// </summary>
    public partial class LocalizationWindow : Window
    {
        LocalizationControl control;
        DockPanel dock;

        public LocalizationWindow()
        {
            InitializeComponent();
            control = new LocalizationControl();
            DockPanel.SetDock(control, Dock.Top);
            dock = (DockPanel)FindName("LocalizationWindow_DockPanel");
            dock.Children.Add(control);
        }

        public void SizeChanged(object sender, EventArgs e)
        {
            //control.InvalidateVisual();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            MissionEditor2.MainWindow.Instance.ExportLocalizationFile();
        }
    }
}