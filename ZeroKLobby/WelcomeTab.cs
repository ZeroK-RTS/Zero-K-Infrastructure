using System.Windows.Forms;
using ZeroKLobby.Controls;
using ZkData;

namespace ZeroKLobby
{
    public class WelcomeTab: ZklBaseControl, INavigatable
    {
        public string PathHead => "welcome";

        public bool TryNavigate(params string[] path) {
            return path.Length > 0 && path[0] == PathHead;
        }

        public bool Hilite(HiliteLevel level, string path) {
            return false;
        }

        public string GetTooltip(params string[] path) {
            return null;
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            BackgroundImage = BgImages.bg_battle.GetResized(ClientRectangle.Width, ClientRectangle.Height);
            base.OnPaintBackground(e);
        }

        public void Reload() {}

        public bool CanReload => false;
        public bool IsBusy => false;
    }
}