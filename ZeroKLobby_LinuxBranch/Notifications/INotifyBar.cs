using System.Windows.Forms;

namespace ZeroKLobby.Notifications
{
    public interface INotifyBar
    {
        void AddedToContainer(NotifyBarContainer container);
        void CloseClicked(NotifyBarContainer container);
        void DetailClicked(NotifyBarContainer container);
        Control GetControl();
    }
}