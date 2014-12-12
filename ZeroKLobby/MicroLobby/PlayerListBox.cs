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
		                base.Items.Clear();
		                base.Items.AddRange(realItems.ToArray());
		                EndUpdate();
		                timer.Stop();
		            } catch (Exception ex) {
		                Trace.TraceError("Error updating list: {0}",ex);
		            }
		        };
		    IntegralHeight = false; //so that the playerlistBox completely fill the edge (not snap to some item size)

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                //dummy item to fix Mono scrollbar always cutout last 3 line
                //https://bugzilla.novell.com/show_bug.cgi?id=475581
                DpiMeasurement.DpiXYMeasurement (this);
                int numberOfDummy = (int)(DpiMeasurement.scaleUpRatioY*3 + 0.9d); //is Math.Ceiling

                for (int i=0; i<numberOfDummy; i++) {
                    PlayerListItem dummyItem = new PlayerListItem () { isOfflineMode = true, isDummy = true, Height = 1, UserName = "ZZ 99 dummy "+i.ToString() }; //sorted to be last
                    realItems.Add (dummyItem);
                }
            }
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
	                EndUpdate();
	            }

	        } catch (Exception ex) {
	            Trace.TraceError("Error updating list:{0}",ex);
	        }

	    }

	    public new ObservableCollection<PlayerListItem> Items { get { return realItems; } }
	    public void AddItemRange(IEnumerable<PlayerListItem> items) {
            foreach (var i in items) realItems.Add(i);
            BeginUpdate();
            base.Items.Clear();
            base.Items.AddRange(realItems.ToArray());
            EndUpdate();
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
                e.ItemHeight = DpiMeasurement.ScaleValueY(((PlayerListItem)base.Items[e.Index]).Height); //GetItemRectangle() will measure the size of item (for drawing). We return a custom Height defined in PlayerListItems.cs
		}


		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			var cursorPoint = new Point(e.X, e.Y);
			if (cursorPoint == previousLocation) return;
			previousLocation = cursorPoint;

			var hoverIndex = IndexFromPoint(cursorPoint);
			if (previousHoverIndex == hoverIndex) return;
			previousHoverIndex = hoverIndex;

			if (hoverIndex < 0 || hoverIndex >= base.Items.Count || !GetItemRectangle(hoverIndex).Contains(cursorPoint))
			{
				HoverItem = null;
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