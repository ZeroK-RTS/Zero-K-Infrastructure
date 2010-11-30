using System;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Floe.UI
{
	public partial class ChatPresenter : ChatBoxBase, IScrollInfo
	{
		private bool _isSelecting;
		private int _selStart = -1, _selEnd = -1;
		private bool _isDragging;

		protected int SelectionStart
		{
			get
			{
				return Math.Min(_selStart, _selEnd);
			}
		}

		protected int SelectionEnd
		{
			get
			{
				return Math.Max(_selStart, _selEnd);
			}
		}

		protected bool IsSelecting
		{
			get
			{
				return _isSelecting && this.SelectionEnd > this.SelectionStart;
			}
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (!_isDragging && !_isSelecting)
			{
				var p = e.GetPosition(this);
				if (Math.Abs(p.X - (this.ColumnWidth + SeparatorPadding)) < SeparatorPadding / 2.0 && this.UseTabularView)
				{
					_isDragging = true;
					this.CaptureMouse();
				}
				else
				{
					int idx = this.GetCharIndexAt(p);
					if (idx >= 0 && idx < int.MaxValue)
					{
						_isSelecting = true;
						this.CaptureMouse();
						_selStart = idx;
					}
				}
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			var p = e.GetPosition(this);

			if (_isSelecting)
			{
				Mouse.OverrideCursor = Cursors.IBeam;
				_selEnd = this.GetCharIndexAt(e.GetPosition(this));
				this.InvalidateVisual();
				e.Handled = true;
			}
			else if (_isDragging)
			{

				this.ColumnWidth = Math.Max(0.0, Math.Min(this.ViewportWidth / 2.0, p.X));
				this.InvalidateAll(false);
			}
			else if (this.UseTabularView && Math.Abs(p.X - (this.ColumnWidth + SeparatorPadding)) < SeparatorPadding / 2.0)
			{
				Mouse.OverrideCursor = Cursors.SizeWE;
			}
			else
			{
				Mouse.OverrideCursor = null;
				this.SelectedLink = this.HitTest(p);
				if (this.SelectedLink != null)
				{
					Mouse.OverrideCursor = Cursors.Hand;
				}
			}

			base.OnMouseMove(e);
		}

		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (_isDragging)
			{
				_isDragging = false;
				this.ReleaseMouseCapture();
				Mouse.OverrideCursor = null;
				this.InvalidateAll(false);
			}
			else if (_isSelecting)
			{
				if (_selStart >= 0 && _selEnd >= 0)
				{
					string selText = this.GetSelectedText();
					if (selText.Length >= this.MinimumCopyLength)
					{
						Clipboard.SetText(selText);
					}
				}

				this.ReleaseMouseCapture();
				if (Mouse.OverrideCursor == Cursors.IBeam)
				{
					Mouse.OverrideCursor = null;
				}
				_isSelecting = false;
				_selStart = -1;
				_selEnd = -1;

				this.InvalidateVisual();
			}
			base.OnMouseMove(e);
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			if (!_isSelecting && !_isDragging)
			{
				Mouse.OverrideCursor = null;
				this.SelectedLink = null;
			}
			base.OnMouseLeave(e);
		}

		protected override void OnLostMouseCapture(MouseEventArgs e)
		{
			_isSelecting = false;
			_isDragging = false;
			this.InvalidateVisual();
			base.OnLostMouseCapture(e);
		}

		public string HitTest(Point p)
		{
			var block = this.GetBlockAt(p.Y);
			if (block != null)
			{
				if (p.Y >= block.Y && p.Y < block.Y + block.Nick.Height &&
					p.X >= block.NickX && p.X < block.NickX + block.Nick.Width &&
					block.Source.Nick != null)
				{
					return block.Source.Nick;
				}
				if (block.Source.Links.Length > 0)
				{
					int line = (int)(p.Y - block.Y) / (int)_lineHeight;
					if (line >= 0 && line < block.Text.Length && p.X >= block.TextX && p.X < block.TextX + block.Text[line].Width)
					{
						var ch = block.Text[line].GetCharacterHitFromDistance(p.X - block.TextX);
						foreach (var l in block.Source.Links)
						{
							if (ch.FirstCharacterIndex >= l.Start && ch.FirstCharacterIndex < l.End)
							{
								return block.Source.Text.Substring(l.Start, l.End - l.Start);
							}
						}
					}
				}
			}
			return null;
		}

		private Block GetBlockAt(double y)
		{
			if (_bottomBlock == null)
			{
				return null;
			}
			var node = _bottomBlock;
			do
			{
				if (y >= node.Value.Y && y < node.Value.Y + node.Value.Height)
				{
					return node.Value;
				}
			}
			while ((node = node.Previous) != null && y >= 0.0);
			return null;
		}

		private int GetCharIndexAt(Point p)
		{
			if (_blocks.Count < 1 || _bottomBlock == null)
			{
				return -1;
			}

			p.Y = Math.Min(this.ActualHeight - 1, Math.Max(0, p.Y));
			Block block = null;
			var node = _bottomBlock;
			do
			{
				if (p.Y >= node.Value.Y && p.Y < node.Value.Y + node.Value.Height)
				{
					break;
				}
			}
			while (node.Previous != null && p.Y >= 0.0 && (node = node.Previous) != null);
			block = node.Value;
			p.Y = Math.Max(block.Y, p.Y);
			if (block.Text == null)
			{
				return -1;
			}

			int line = (int)(p.Y - block.Y) / (int)_lineHeight;
			if (line >= block.Text.Length)
			{
				line = block.Text.Length - 1;
			}
			int idx = 0;
			if (line > 0 || p.X >= block.TextX)
			{
				idx += block.TimeString.Length + block.NickString.Length;
				var ch = block.Text[line].GetCharacterHitFromDistance(p.X - block.TextX);
				idx += Math.Min(ch.FirstCharacterIndex, block.Source.Text.Length - 1);
			}
			else if (p.X >= block.NickX || block.Time == null)
			{
				idx += block.TimeString.Length;
				var ch = block.Nick.GetCharacterHitFromDistance(p.X - block.NickX);
				idx += Math.Min(ch.FirstCharacterIndex, block.NickString.Length - 1);
			}
			else
			{
				var ch = block.Time.GetCharacterHitFromDistance(p.X);
				idx += Math.Min(ch.FirstCharacterIndex, block.TimeString.Length - 1);
			}
			return idx + block.CharStart;
		}

		private void FindSelectedArea(int idx, int txtLen, int txtOffset, double x, TextLine line, ref double start, ref double end)
		{
			int first = Math.Max(txtOffset, this.SelectionStart - idx);
			int last = Math.Min(txtLen - 1 + txtOffset, this.SelectionEnd - idx);
			if (last >= first)
			{
				start = Math.Min(start, line.GetDistanceFromCharacterHit(new CharacterHit(first, 0)) + x);
				end = Math.Max(end, line.GetDistanceFromCharacterHit(new CharacterHit(last, 1)) + x);
			}
		}

		private static Lazy<Brush> _selectBrush = new Lazy<Brush>(() =>
			{
				var c = SystemColors.HighlightColor;
				c.A = 102;
				return new SolidColorBrush(c);
			});

		private void DrawSelectionHighlight(DrawingContext dc, Block block)
		{
			if (this.SelectionEnd < block.CharStart || this.SelectionStart >= block.CharEnd ||
				this.SelectionStart >= this.SelectionEnd)
			{
				return;
			}

			int idx = block.CharStart, txtOffset = 0;
			double y = block.Y;
			for (int i = 0; i < block.Text.Length; i++)
			{
				double start = double.MaxValue, end = double.MinValue;
				if (i == 0)
				{
					if (block.Time != null)
					{
						this.FindSelectedArea(idx, block.TimeString.Length, 0, 0.0, block.Time, ref start, ref end);
						idx += block.TimeString.Length;
					}
					this.FindSelectedArea(idx, block.NickString.Length, 0, block.NickX, block.Nick, ref start, ref end);
					idx += block.NickString.Length;
				}
				this.FindSelectedArea(idx, block.Text[i].Length, txtOffset, block.TextX, block.Text[i], ref start, ref end);
				txtOffset += block.Text[i].Length;

				if (end > start)
				{
					dc.DrawRectangle(_selectBrush.Value, null,
						new Rect(new Point(start, y), new Point(end, y + _lineHeight)));
				}
				y += _lineHeight;
			}
		}

		private string GetSelectedText()
		{
			var output = new StringBuilder();
			foreach (var block in _blocks)
			{
				if (this.SelectionEnd < block.CharStart || this.SelectionStart >= block.CharEnd)
				{
					continue;
				}

				int idx = block.CharStart;
				bool start, end;
				output.Append(this.GetSelectedText(idx, block.TimeString, output, out start, out end));
				idx += block.TimeString.Length;
				string nick = this.GetSelectedText(idx, block.NickString, output, out start, out end);
				if (start && this.UseTabularView && block.Source.Nick != null)
				{
					output.Append('<');
				}
				output.Append(nick);
				if (nick.Length > 0 && end && this.UseTabularView)
				{
					if (block.Source.Nick != null)
					{
						output.Append('>');
					}
					output.Append(' ');
				}
				idx += block.NickString.Length;
				output.Append(this.GetSelectedText(idx, block.Source.Text, output, out start, out end));
				if (this.SelectionEnd >= block.CharEnd)
				{
					output.AppendLine();
				}
			}
			return output.ToString();
		}

		private string GetSelectedText(int idx, string s, StringBuilder output, out bool start, out bool end)
		{
			int first = Math.Max(0, this.SelectionStart - idx);
			int last = Math.Min(s.Length - 1, this.SelectionEnd - idx);
			start = first == 0;
			end = last >= s.Length - 1;
			return last >= first ? s.Substring(first, last - first + 1) : "";
		}
	}
}
