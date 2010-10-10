using System.Windows.Forms;

namespace SpringDownloader.Notifications
{
    public interface INotifyBar
    {
        void AddedToContainer(NotifyBarContainer container);
        void CloseClicked(NotifyBarContainer container);
        void DetailClicked(NotifyBarContainer container);
        Control GetControl();
    }
}