using System.Linq;
using System.Windows.Forms;
using PlasmaShared;

namespace ZeroKLobby.Notifications
{
    public partial class NewVersionBar: UserControl, INotifyBar
    {
        NotifyBarContainer container;


        public NewVersionBar(SelfUpdater selfupdater) {
            InitializeComponent();
            selfupdater.ProgramUpdated += s => { Program.MainWindow.InvokeFunc(() => { Program.NotifySection.AddBar(this); }); };
        }


        public void AddedToContainer(NotifyBarContainer container) {
            this.container = container;
            container.btnDetail.Enabled = true;
            container.btnDetail.Text = "Restart";
        }

        public void CloseClicked(NotifyBarContainer container) {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container) {
            Application.Restart();
        }

        public Control GetControl() {
            return this;
        }
    }
}