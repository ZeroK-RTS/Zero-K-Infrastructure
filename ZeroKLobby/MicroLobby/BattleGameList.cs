using System.Linq;
using System.Windows.Forms;

namespace SpringDownloader.MicroLobby
{
    class BattleGameList: GdiListBox
    {
        public new const int Width = 150;

        public BattleGameList()
        {
            base.Width = Width;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var gameButtons = items.Cast<BattleGameListItem>();
            var selectedItem = gameButtons.SingleOrDefault(i => i.HitTest(e.Y));
            if (selectedItem != null) foreach (var g in gameButtons) g.IsSelected = selectedItem == g;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = items.Any(i => i.HitTest(e.Y)) ? Cursors.Hand : Cursors.Default;
        }
    }
}