using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
    class GdiListBox: ScrollableControl
    {
        public bool IsSorted { get; set; }
        protected List<GdiListBoxItem> items = new List<GdiListBoxItem>();

        public GdiListBox()
        {
            AutoScroll = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            BackColor = Color.White;
        }


        public void AddItem(GdiListBoxItem item)
        {
            items.Add(item);
            if (IsSorted) SortItems();
            Invalidate();
        }

        public void ClearItems()
        {
            items.Clear();
            Invalidate();
        }

        public void Remove(GdiListBoxItem item)
        {
            items.Remove(item);
            Invalidate();
        }

        public void RemoveItem(GdiListBoxItem item)
        {
            items.Add(item);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            foreach (var item in items) if (item.HitTest(e.Y)) item.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);
                e.Graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
                var y = 0;
                foreach (var item in items) item.Draw(e.Graphics, ref y, AutoScrollPosition.Y);
                AutoScrollMinSize = new Size(0, y);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error in drawing GdiListBox: " + ex);
            }
        }

        protected virtual void SortItems() {}
    }
}