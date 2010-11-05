using System.Windows.Controls;
using System.Windows.Input;

namespace ZeroKLobby
{
	public class SubmitTextBox: TextBox
	{
		public SubmitTextBox()
		{
			PreviewKeyDown += SubmitTextBox_PreviewKeyDown;
			GotMouseCapture += SubmitTextBox_GotMouseCapture;
		}

		void SubmitTextBox_GotMouseCapture(object sender, MouseEventArgs e)
		{
			SelectAll();
		}

		void SubmitTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				var be = GetBindingExpression(TextProperty);
				if (be != null) be.UpdateSource();
				
			}
		}
	}
}