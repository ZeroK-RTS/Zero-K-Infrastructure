using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SpringDownloader;

namespace SpringDownloader.MicroLobby
{
    public class PlayerListBox: ListBox
    {
        Point previousLocation;
        public PlayerListItem HoverItem { get; set; }
        public bool IsBattle { get; set; }

        public PlayerListBox()
        {
            DrawMode = DrawMode.OwnerDrawVariable;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        public string[] GetUserNames()
        {
            return Items.Cast<PlayerListItem>().Select(u => u.UserName).Where(u => u != null).ToArray();
        }

        public bool IsUserNameInsensitive(string word)
        {
            return Enumerable.Any<string>(Program.TasClient.ExistingUsers.Keys, x => x.ToString().ToLower() == word.ToLower());
        }

        //case insensitive
        public void SelectUser(string userName)
        {
            var index = FindString(userName);
            if (index >= 0) SetSelected(index, true);
            else
            {
                var toSelect = Items.Cast<PlayerListItem>().FirstOrDefault(x => x.UserName != null && x.UserName.ToLower() == userName.ToLower());
                if (toSelect != null) SelectedItem = toSelect;
            }
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            try
            {
                if (DesignMode) return;
                if (e.Index >= 0 && e.Index <= Items.Count)
                {
                    e.DrawBackground();
                    e.DrawFocusRectangle();
                    base.OnDrawItem(e);
                    var item = (PlayerListItem)Items[e.Index];
                    item.DrawPlayerLine(e.Graphics, e.Bounds, e.ForeColor, e.BackColor, item.IsGrayedOut, IsBattle);
                }
            }
            catch (Exception ex)
            {
                var item = Items[e.Index] as PlayerListItem;
                var name = "";
                if (item != null) name = item.UserName;
                Trace.TraceError("Error rendering player {0}: {1}", name, ex);
            }
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            base.OnMeasureItem(e);
            e.ItemHeight = ((PlayerListItem)Items[e.Index]).Height;
        }


        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var cursorPoint = new Point(e.X, e.Y);
            if (cursorPoint == previousLocation) return;
            previousLocation = cursorPoint;

            var hoverIndex = IndexFromPoint(cursorPoint);
            if (hoverIndex < 0 || hoverIndex >= Items.Count || !GetItemRectangle(hoverIndex).Contains(cursorPoint))
            {
                HoverItem = null;
                Program.ToolTip.SetUser(this, null);
            }
            else
            {
                HoverItem = (PlayerListItem)Items[hoverIndex];
                if (HoverItem.UserName != null) Program.ToolTip.SetUser(this, HoverItem.UserName);
            }
        }


        // only draw if item is on-screen
        protected override void OnPaint(PaintEventArgs e)
        {
            var itemRegion = new Region(e.ClipRectangle);
            e.Graphics.FillRegion(new SolidBrush(BackColor), itemRegion);
            if (Items.Count > 0)
            {
                for (var i = 0; i < Items.Count; ++i)
                {
                    var itemRectangle = GetItemRectangle(i);
                    if (e.ClipRectangle.IntersectsWith(itemRectangle))
                    {
                        if ((SelectionMode == SelectionMode.One && SelectedIndex == i) ||
                            (SelectionMode == SelectionMode.MultiSimple && SelectedIndices.Contains(i)) ||
                            (SelectionMode == SelectionMode.MultiExtended && SelectedIndices.Contains(i))) OnDrawItem(new DrawItemEventArgs(e.Graphics, Font, itemRectangle, i, DrawItemState.Selected, ForeColor, BackColor));
                        else OnDrawItem(new DrawItemEventArgs(e.Graphics, Font, itemRectangle, i, DrawItemState.Default, ForeColor, BackColor));
                        itemRegion.Complement(itemRectangle);
                    }
                }
            }
            base.OnPaint(e);
        }
    }
}