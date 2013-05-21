using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby
{
    class MultiPagePanel: Panel
    {
        int _currentPageIndex;
        public int CurrentPageIndex {
            get { return _currentPageIndex; }
            set {
                if (value >= 0 && value < (Controls.Count - 1)) {
                    Controls[value].BringToFront();
                    _currentPageIndex = value;
                }
            }
        }

        public void AddPage(Control page) {
            page.Dock = DockStyle.Fill;
            Controls.Add(page);
        }
    }
}