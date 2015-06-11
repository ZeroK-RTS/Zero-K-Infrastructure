using System.Drawing;
using System.Security.Cryptography.X509Certificates;

namespace ZeroKLobby.MainPages
{
    public interface IMainPage
    {
        void GoBack();
        string Title { get; }
        Image MainWindowBgImage { get; }
    }
}