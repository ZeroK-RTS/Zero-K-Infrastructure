using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Floe.UI
{
	public partial class ChatPresenter : ChatBoxBase, IScrollInfo
	{
		private ScrollViewer _viewer;

		public ChatPresenter()
		{
			this.Loaded += (sender, e) =>
				{
					if (_isAutoScrolling)
					{
						this.ScrollToEnd();
					}
				};
			this.Unloaded += (sender, e) =>
				{
					_isSelecting = false;
					_isDragging = false;
				};
		}

		public void Clear()
		{
			_blocks = new LinkedList<Block>();
			_curBlock = null;
			_bufferLines = 0;
			this.InvalidateScrollInfo();
			this.InvalidateVisual();
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property == Control.FontFamilyProperty ||
				e.Property == Control.FontSizeProperty ||
				e.Property == Control.FontStyleProperty ||
				e.Property == Control.FontWeightProperty ||
				e.Property == ChatBoxBase.PaletteProperty ||
				e.Property == ChatBoxBase.ShowTimestampProperty ||
				e.Property == ChatBoxBase.TimestampFormatProperty ||
				e.Property == ChatBoxBase.UseTabularViewProperty ||
				e.Property == ChatBoxBase.ColorizeNicknamesProperty ||
				e.Property == ChatBoxBase.NewMarkerColorProperty ||
				e.Property == ChatBoxBase.NicknameColorSeedProperty ||
				e.Property == ChatBoxBase.DividerBrushProperty ||
				e.Property == ChatBoxBase.BackgroundProperty)
			{
				this.InvalidateAll(true);
			}
			
			base.OnPropertyChanged(e);
		}
	}
}
