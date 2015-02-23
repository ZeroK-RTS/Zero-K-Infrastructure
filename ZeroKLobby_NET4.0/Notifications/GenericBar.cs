using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace ZeroKLobby.Notifications
{
	public partial class GenericBar: UserControl, INotifyBar
	{
		public NotifyBarContainer BarContainer { get; private set; }
		/// <summary>
		/// must be set before adding to notify area
		/// </summary>
		public string DetailButtonLabel { get; set; }
		public override string Text { get { return lbText.Text; } set { lbText.Text = value; } }
		public event EventHandler<CancelEventArgs> CloseButtonClicked = delegate { };
		public event EventHandler DetailButtonClicked = delegate { };

		public GenericBar()
		{
			InitializeComponent();
		}


		public void AddedToContainer(NotifyBarContainer container)
		{
			BarContainer = container;
			container.btnDetail.Text = DetailButtonLabel;
		    container.Title = "Notice";
		}

		public void CloseClicked(NotifyBarContainer container)
		{
			var args = new CancelEventArgs();
			CloseButtonClicked(this, args);
			if (!args.Cancel) Program.NotifySection.RemoveBar(this);
		}

		public void DetailClicked(NotifyBarContainer container)
		{
			DetailButtonClicked(this, EventArgs.Empty);
		}

		public Control GetControl()
		{
			return this;
		}
	}
}