using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ZeroKLobby.MicroLobby
{
	public class PlayerListBox: ListBox
	{
		Point previousLocation;
        int previousHoverIndex;
	    ObservableCollection<PlayerListItem> realItems;
	    Timer timer;
	    public PlayerListItem HoverItem { get; set; }
        public override int ItemHeight{
            get {
                DpiMeasurement.DpiXYMeasurement(this);
                if (DesignMode || base.Items.Count==0) return 10;
                return DpiMeasurement.ScaleValueY(((PlayerListItem)base.Items[0]).Height); //in MONO the ListBox's size doesn't seem to be calculated from OnMeasureItem() but from ItemHeight property, so we return the size here for MONO compatibility
            }
        }

		public bool IsBattle { get; set; }
	    const int stagingMs = 200; // staging only on linux
	    DateTime lastChange = DateTime.UtcNow;
	    bool useStaging = Environment.OSVersion.Platform == PlatformID.Unix;

		public PlayerListBox()
		{
			DrawMode = DrawMode.OwnerDrawVariable;
            this.BackColor = Color.DimGray;
			SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		    realItems = new ObservableCollection<PlayerListItem>();
            realItems.CollectionChanged += RealItemsOnCollectionChanged;

		    timer = new Timer() { Interval = stagingMs, };
		    timer.Tick += (sender, args) =>
		        {
		            try {
		                BeginUpdate();
		                int currentScroll = base.TopIndex;

		                base.Items.Clear();
		                base.Items.AddRange(realItems.ToArray());

		                base.TopIndex = currentScroll;
		                EndUpdate();

		                timer.Stop();
		            } catch (Exception ex) {
		                Trace.TraceError("Error updating list: {0}",ex);
		            }
		        };
		    IntegralHeight = false; //so that the playerlistBox completely fill the edge (not snap to some item size)
		}

	    void RealItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args) {
	        try {
	            if (useStaging && DateTime.UtcNow.Subtract(lastChange).TotalMilliseconds < stagingMs) {
	                lastChange = DateTime.UtcNow;
	                timer.Stop();
	                timer.Start();
	            }
	            else {
	                timer.Stop();
	                lastChange = DateTime.UtcNow;

	                BeginUpdate();
	                int currentScroll = base.TopIndex; //read current scroll index. Ref: http://stackoverflow.com/questions/14318069/setting-the-scrollbar-position-of-listbox

	                if (args.Action == NotifyCollectionChangedAction.Add) {
	                    foreach (var item in args.NewItems) {
	                        base.Items.Add(item);
	                    }
	                }
	                else if (args.Action == NotifyCollectionChangedAction.Remove) {
	                    foreach (var item in args.OldItems) {
	                        base.Items.Remove(item);
	                    }
	                }
	                else {
	                    base.Items.Clear();
	                    base.Items.AddRange(realItems.ToArray());
	                }

	                base.TopIndex = currentScroll;
	                EndUpdate();
	            }

	        } catch (Exception ex) {
	            Trace.TraceError("Error updating list:{0}",ex);
	        }

	    }

	    public new ObservableCollection<PlayerListItem> Items { get { return realItems; } }
	    public void AddItemRange(IEnumerable<PlayerListItem> items) {
            foreach (var i in items) realItems.Add(i);

            //BeginUpdate();
            //int currentScroll = base.TopIndex;

            //base.Items.Clear();
            //base.Items.AddRange(realItems.ToArray());

            //base.TopIndex = currentScroll;
            //EndUpdate();
	    }

	    public string[] GetUserNames()
		{
			return Items.Select(u => u.UserName).Where(u => u != null).ToArray();
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
				if (e.Index >= 0 && e.Index <= base.Items.Count)
				{
					e.DrawBackground();
					e.DrawFocusRectangle();
					base.OnDrawItem(e);
					var item = (PlayerListItem)base.Items[e.Index];
					item.DrawPlayerLine(e.Graphics, e.Bounds, e.ForeColor, e.BackColor, item.IsGrayedOut, IsBattle);
				}
			}
			catch (Exception ex)
			{
				var item = base.Items[e.Index] as PlayerListItem;
				var name = "";
				if (item != null) name = item.UserName;
				Trace.TraceError("Error rendering player {0}: {1}", name, ex);
			}
		}

		protected override void OnMeasureItem(MeasureItemEventArgs e)
		{
			base.OnMeasureItem(e);
            DpiMeasurement.DpiXYMeasurement(this);
            if (DesignMode) return;
            if (e.Index > -1 && e.Index < base.Items.Count)
                e.ItemHeight = DpiMeasurement.ScaleValueY(((PlayerListItem)base.Items[e.Index]).Height); //GetItemRectangle() will measure the size of item for drawing, so we return a custom Height defined in PlayerListItems.cs
		}

        bool mouseIsDown = false;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            mouseIsDown = true;
            UpdateHoverItem(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp (e);
            mouseIsDown = false;
        }

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (!mouseIsDown) UpdateHoverItem(e);
		}

		void UpdateHoverItem(MouseEventArgs e)
		{
			var cursorPoint = new Point(e.X, e.Y);
			
			if (cursorPoint == previousLocation) return;
			previousLocation = cursorPoint;
			
			var hoverIndex = IndexFromPoint(cursorPoint);
			bool isOnEmpty = hoverIndex == ListBox.NoMatches;
			
			//if (isOnEmpty) hoverIndex = base.Items.Count; //we'll use this number when cursor is outside the list //no, it looks ridiculous
			
			if (previousHoverIndex == hoverIndex) return;
			previousHoverIndex = hoverIndex;
			
			if (isOnEmpty)
			//if (hoverIndex < 0 || hoverIndex >= base.Items.Count)
			{
				HoverItem = null; //outside the list
				Program.ToolTip.SetUser(this, null);
			}
			else
			{
				HoverItem = (PlayerListItem)base.Items[hoverIndex];
				if (HoverItem.UserName != null) Program.ToolTip.SetUser(this, HoverItem.UserName);
			}
		}


		// only draw if item is on-screen
		protected override void OnPaint(PaintEventArgs e)
		{
			var itemRegion = new Region(e.ClipRectangle);
			e.Graphics.FillRegion(new SolidBrush(BackColor), itemRegion);
			if (base.Items.Count > 0)
			{
				for (var i = 0; i < base.Items.Count; ++i)
				{
					var itemRectangle = GetItemRectangle(i);
					if (e.ClipRectangle.IntersectsWith(itemRectangle))
					{
						if ((SelectionMode == SelectionMode.One && SelectedIndex == i) || (SelectionMode == SelectionMode.MultiSimple && SelectedIndices.Contains(i)) ||
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