using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SpringDownloader.Common
{
	/// <summary>
	/// Interaction logic for UcRedButton.xaml
	/// </summary>
	public partial class UcRedButton: ToggleButton
	{
		public static readonly DependencyProperty IsCheckingEnabledProperty = DependencyProperty.Register("IsCheckingEnabled",
		                                                                                                  typeof(bool),
		                                                                                                  typeof(UcRedButton));

		public bool IsCheckingEnabled
		{
			get { return (bool)GetValue(IsCheckingEnabledProperty); }
			set
			{
				SetValue(IsCheckingEnabledProperty, value);
				if (!value) IsChecked = true;
			}
		}


		public UcRedButton()
		{
			InitializeComponent();
		}

		protected override void OnChecked(RoutedEventArgs e)
		{
			if (IsCheckingEnabled) base.OnChecked(e);
			else IsChecked = true;
		}

		protected override void OnUnchecked(RoutedEventArgs e)
		{
			if (IsCheckingEnabled) base.OnUnchecked(e);
			else IsChecked = true;
		}

		void ToggleButton_Loaded(object sender, RoutedEventArgs e)
		{
			if (!IsCheckingEnabled) IsChecked = true;
		}
	}
}