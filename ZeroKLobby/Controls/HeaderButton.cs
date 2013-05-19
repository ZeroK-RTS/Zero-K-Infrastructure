using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.Controls
{
    public class HeaderButton: Button
    {
        public bool IsAlerting { get; set; }
        public bool IsSelected { get; set; }

        public string Label { get; set; }
        static HeaderButton() {}
    }
}