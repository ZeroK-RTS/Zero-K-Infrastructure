using System.Threading;
using System.Windows;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for LoadingDialog.xaml
	/// </summary>
	public partial class LoadingDialog : Window
	{
		public LoadingDialog()
		{
			InitializeComponent();
		}

		public string Text
		{
			get { return textBlock.Text; }
			set { Dispatcher.Invoke(new ThreadStart(() => textBlock.Text = value)); }
		}
	}
}