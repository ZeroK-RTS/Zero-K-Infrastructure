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

        PlayerListItem hoverItem = null; // TODO: Implement

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

        public PlayerListItem HoverItem
        {
            get { return this.hoverItem; }
            set
            {
                this.hoverItem = value;
                Program.ToolTip.SetUser(this, HoverItem?.UserName);
            }
        }

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

            var itemsToPaint = GetItemsToPaintInOrder();

            foreach (var item in this.Items)
            {
                this.PaintItem(currentDrawY, item, graphics);
                currentDrawY += item.Height;
            }

            var maxDrawY = currentDrawY;

            this.AutoScrollMinSize = new Size(0, maxDrawY);
        }

        IEnumerable<PlayerListItem> GetItemsToPaintInOrder()
        {
            if (this.IsSorted)
            {
                return Items.OrderBy(user => user.GetSortingKey()).ToArray();
            }
            else
            {
                return Items;
            }
        }

        void PaintItem(int currentDrawY, PlayerListItem item, Graphics graphics)
        {
            var currentDrawPosition = new Point(0, currentDrawY);
            var itemSize = new Size(ClientSize.Width, item.Height);
            var itemBounds = new Rectangle(currentDrawPosition, itemSize);
            item.DrawPlayerLine(graphics, itemBounds, Color.White, item.IsGrayedOut, IsBattle);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            isMouseDown = true;
            UpdateHoverItem(e);
        }

        void UpdateHoverItem(MouseEventArgs mouseEventArgs)
        {
            var cursorPoint = new Point(mouseEventArgs.X, mouseEventArgs.Y);

            // No need to update if the cursor hasn't moved.
            if (cursorPoint == previousCursorLocation) return;
            previousCursorLocation = cursorPoint;

            var currentMeasureY = 0;

            var itemsToPaint = GetItemsToPaintInOrder();

            foreach (var item in this.Items)
            {
                currentMeasureY += item.Height;
                if (cursorPoint.Y < currentMeasureY)
                {
                    HoverItem = item; // Found it!
                    return;
                }
            }

            HoverItem = null; // The cursor is hovering over an empty area of control.
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            this.isMouseDown = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            this.isMouseDown = false;
            if (!this.isMouseDown) UpdateHoverItem(e);
        }


        protected override void OnPaintBackground(PaintEventArgs e)
        {
            this.RenderParentsBackgroundImage(e);
        }

        public ObservableCollection<PlayerListItem> Items = new ObservableCollection<PlayerListItem>();
        bool isUpdating = false;
        bool isMouseDown;
        private Point previousCursorLocation;
    }
}
