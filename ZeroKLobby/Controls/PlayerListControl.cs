using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby.Controls
{
    public partial class PlayerListControl : ScrollableControl
    {
        public PlayerListControl()
        {
            InitializeComponent();

            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            AutoScroll = false;

            if (this.IsInDesignMode()) return;

            this.Items.CollectionChanged += Items_CollectionChanged;
            this.Scroll += PlayerListControl_Scroll;
            this.MouseWheel += PlayerListControl_MouseWheel;

        }

        private void PlayerListControl_MouseWheel(object sender, MouseEventArgs e)
        {
            Invalidate();
        }

        private void PlayerListControl_Scroll(object sender, ScrollEventArgs e)
        {
            Invalidate();
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!this.isUpdating)
            {
                Invalidate();
            }
        }

        public bool IsBattle { get; set; }

        public bool IsSorted { get; set; }

        public int TopIndex = 0; // TODO: Implement

        public PlayerListItem HoverItem = null; // TODO: Implement

        public string[] GetUserNames()
        {
            return Items.Select(user => user.UserName).Where(user => user != null).ToArray();
        }

        public void SelectUser(string userName)
        {
            // TODO: Implement
        }

        public void BeginUpdate()
        {
            isUpdating = true;
            this.SuspendLayout();
        }

        public void EndUpdate()
        {
            isUpdating = false;
            this.ResumeLayout();
            Invalidate();
        }

        public void AddItemRange(IEnumerable<PlayerListItem> items)
        {
            foreach (var i in items) this.Items.Add(i);
        }

        public PlayerListItem SelectedItem { get; set; }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.IsInDesignMode()) return;
            try
            {
                base.OnPaint(e);
                var graphics = e.Graphics;
                graphics.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);
                PaintAllItems(graphics);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error in painting PlayerListControl: " + ex);
            }
        }

        void PaintAllItems(Graphics graphics)
        {
            var currentDrawY = 0;

            IEnumerable<PlayerListItem> itemsToPaint;
            if (this.IsSorted)
            {
                itemsToPaint = Items.OrderBy(user => user.GetSortingKey()).ToArray();
            }
            else
            {
                itemsToPaint = Items;
            }

            foreach (var item in this.Items)
            {
                this.PaintItem(currentDrawY, item, graphics);
                currentDrawY += item.Height;
            }

            var maxDrawY = currentDrawY;

            this.AutoScrollMinSize = new Size(0, maxDrawY);
        }

        void PaintItem(int currentDrawY, PlayerListItem item, Graphics graphics)
        {
            var currentDrawPosition = new Point(0, currentDrawY);
            var itemSize = new Size(ClientSize.Width, item.Height);
            var itemBounds = new Rectangle(currentDrawPosition, itemSize);
            item.DrawPlayerLine(graphics, itemBounds, Color.White, item.IsGrayedOut, this.IsBattle);
        }


        protected override void OnPaintBackground(PaintEventArgs e)
        {
            this.RenderParentsBackgroundImage(e);
        }

        public ObservableCollection<PlayerListItem> Items = new ObservableCollection<PlayerListItem>();
        bool isUpdating = false;
    }
}
