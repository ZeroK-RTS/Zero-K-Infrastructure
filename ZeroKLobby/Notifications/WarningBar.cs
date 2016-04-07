using System.Windows.Forms;
using ZeroKLobby.Controls;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Notifications
{
    public partial class WarningBar: INotifyBar
    {
        private string title;

        protected WarningBar(string text)
        {
            InitializeComponent();
            lbText.Text = text;
        }

        public static WarningBar DisplayWarning(string text, string title="Warning")
        {
            var bar = new WarningBar(text);
            bar.title = title;

            Program.NotifySection.AddBar(bar);
            return bar;
        }


/*        public void AddedToContainer(NotifyBarContainer container)
        {
            container.btnDetail.Enabled = false;
            container.btnDetail.BackgroundImage = ZklResources.warning;
            container.Title = title;
        }*/


        public Control GetControl()
        {
            return this;
        }

    }
}