using System.Windows.Forms;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Notifications
{
    public partial class WarningBar: UserControl, INotifyBar
    {
        protected WarningBar(string text)
        {
            InitializeComponent();
            lbText.Text = text;
        }

        public static WarningBar DisplayWarning(string text)
        {
            var bar = new WarningBar(text);
            Program.NotifySection.AddBar(bar);
            return bar;
        }


        public void AddedToContainer(NotifyBarContainer container)
        {
            container.btnDetail.Enabled = false;
            container.btnDetail.BackgroundImage = ZklResources.warning;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container) {}

        public Control GetControl()
        {
            return this;
        }
    }
}