using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Floe.UI
{
	public partial class ChatPresenter : ChatBoxBase, IScrollInfo
	{
		private int _bufferLines, _scrollPos;
		private bool _isAutoScrolling = true;

		public bool CanHorizontallyScroll { get { return false; } set { } }
		public bool CanVerticallyScroll { get { return true; } set { } }
		public double ExtentHeight { get { return _lineHeight * _bufferLines; } }
		public double ExtentWidth { get { return this.ActualWidth; } }
		public ScrollViewer ScrollOwner { get { return _viewer; } set { _viewer = value; } }
		public double ViewportHeight { get { return this.ActualHeight; } }
		public double ViewportWidth { get { return this.ActualWidth; } }
		public double HorizontalOffset { get { return 0.0; } }
		public double VerticalOffset { get { return (_bufferLines - _scrollPos) * _lineHeight - this.ActualHeight; } }

		public void LineUp()
		{
			this.ScrollTo(_scrollPos + 1);
		}

		public void LineDown()
		{
			this.ScrollTo(_scrollPos - 1);
		}

		public void MouseWheelUp()
		{
			this.ScrollTo(_scrollPos + SystemParameters.WheelScrollLines);
		}

		public void MouseWheelDown()
		{
			this.ScrollTo(_scrollPos - SystemParameters.WheelScrollLines);
		}

		public void PageUp()
		{
			this.ScrollTo(_scrollPos + this.VisibleLineCount - 1);
		}

		public void PageDown()
		{
			this.ScrollTo(_scrollPos - this.VisibleLineCount + 1);
		}

		public void ScrollTo(int pos)
		{
			pos = Math.Max(0, Math.Min(_bufferLines - this.VisibleLineCount + 1, pos));

			var delta = (pos - _scrollPos) * _lineHeight;
			_scrollPos = pos;

			this.InvalidateVisual();
			this.InvalidateScrollInfo();

			_isAutoScrolling = _scrollPos == 0;
		}

		public void SetVerticalOffset(double offset)
		{
			int pos = _bufferLines - (int)((offset + this.ViewportHeight) / _lineHeight);
			this.ScrollTo(pos);
		}

		public void LineLeft()
		{
			throw new NotImplementedException();
		}

		public void LineRight()
		{
			throw new NotImplementedException();
		}

		public void PageLeft()
		{
			throw new NotImplementedException();
		}

		public void PageRight()
		{
			throw new NotImplementedException();
		}

		public void MouseWheelLeft()
		{
		}

		public void MouseWheelRight()
		{
		}

		public void SetHorizontalOffset(double offset)
		{
			throw new NotImplementedException();
		}

		public Rect MakeVisible(Visual visual, Rect rectangle)
		{
			return Rect.Empty;
		}

		public void ScrollToEnd()
		{
			_scrollPos = 0;
		}

		public void InvalidateScrollInfo()
		{
			if (_viewer != null)
			{
				_viewer.InvalidateScrollInfo();
			}
		}

		public int VisibleLineCount
		{
			get
			{
				return _lineHeight == 0.0 ? 0 : (int)Math.Ceiling(this.ActualHeight / _lineHeight);
			}
		}
	}
}
