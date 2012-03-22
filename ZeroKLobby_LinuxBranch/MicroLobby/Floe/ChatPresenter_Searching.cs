using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Floe.UI
{
	public partial class ChatPresenter : ChatBoxBase, IScrollInfo
	{
		private LinkedListNode<Block> _curSearchBlock;
		private List<Tuple<int, int>> _curSearchMatches;

		private static Lazy<Brush> _searchBrush = new Lazy<Brush>(() =>
		{
			var c = SystemColors.HotTrackColor;
			c.A = 102;
			return new SolidColorBrush(c);
		});

		public void Search(Regex pattern, SearchDirection dir)
		{
			var node = _curSearchBlock;

			// No search in progress; set current node to the bottom visible block
			if (node == null)
			{
				node = _bottomBlock != null ? _bottomBlock : _blocks.Last;
			}
			else
			{
				// Move back to the previous node. If we're at the top or bottom, do nothing.
				node = dir == SearchDirection.Previous ? _curSearchBlock.Previous : _curSearchBlock.Next;
				if (node == null)
				{
					return;
				}
			}

			while (node != null)
			{
				var matches = (from Match m in pattern.Matches(node.Value.Source.Text)
									 select new Tuple<int,int>(m.Index, m.Index + m.Length)).ToList();
				if(matches.Count > 0)
				{
					_curSearchBlock = node;
					_curSearchMatches = matches;
					break;
				}
				node = dir == SearchDirection.Previous ? node.Previous : node.Next;
			}

			if (_curSearchBlock != null)
			{
				this.ScrollIntoView(_curSearchBlock);
				this.InvalidateVisual();
			}
		}

		public void ClearSearch()
		{
			_curSearchBlock = null;
			this.InvalidateVisual();
		}

		private void ScrollIntoView(LinkedListNode<Block> targetNode)
		{
			int pos = 0;
			var node = _blocks.Last;
			while (node != null && node != targetNode)
			{
				pos += node.Value.Text.Length;
				node = node.Previous;
			}
			_scrollPos = Math.Max(
				Math.Min(_scrollPos, Math.Max(0, pos - this.VisibleLineCount / 2)),
				Math.Min(_bufferLines - this.VisibleLineCount + 1, pos - this.VisibleLineCount / 2 + node.Value.Text.Length));
			this.InvalidateScrollInfo();
		}

		private void DrawSearchHighlight(DrawingContext dc, Block block)
		{
			foreach (var pair in _curSearchMatches)
			{
				int txtOffset = 0;
				double y = block.Y;

				for (int i = 0; i < block.Text.Length; i++)
				{
					int start = Math.Max(txtOffset, pair.Item1);
					int end = Math.Min(txtOffset + block.Text[i].Length, pair.Item2);

					if (end > start)
					{
						double x1 = block.Text[i].GetDistanceFromCharacterHit(new CharacterHit(start, 0)) + block.TextX;
						double x2 = block.Text[i].GetDistanceFromCharacterHit(new CharacterHit(end, 0)) + block.TextX;

						dc.DrawRectangle(_searchBrush.Value, null,
							new Rect(new Point(x1, y), new Point(x2, y + _lineHeight)));
					}

					y += _lineHeight;
					txtOffset += block.Text[i].Length;
				}
			}
		}
	}
}
