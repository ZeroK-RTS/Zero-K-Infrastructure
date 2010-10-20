using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
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
            var gameButtons = Items.Cast<BattleGameListItem>();
            var selectedItem = gameButtons.SingleOrDefault(i => i.HitTest(e.Y));
            SelectItem(selectedItem);

        }

    	public void SelectItem(BattleGameListItem selectedItem)
    	{
			var gameButtons = Items.Cast<BattleGameListItem>();
    		if (selectedItem != null) foreach (var g in gameButtons) g.IsSelected = selectedItem == g;
			Invalidate();
    	}

    	protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = Items.Any(i => i.HitTest(e.Y)) ? Cursors.Hand : Cursors.Default;
        }
    }
}