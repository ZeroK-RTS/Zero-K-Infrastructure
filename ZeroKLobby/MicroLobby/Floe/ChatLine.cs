using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Floe.UI
{
	public class ChatLine : IChatSpanProvider
	{
		public DateTime Time { get; private set; }
		public string ColorKey { get; private set; }
		public int NickHashCode { get; private set; }
		public string Nick { get; private set; }
		public string RawText { get; private set; }
		public string Text { get; private set; }
		public ChatMarker Marker { get; set; }
		public ChatSpan[] Spans { get; private set; }
		public ChatLink[] Links { get; private set; }

		public ChatLine(string colorKey, DateTime time, int nickHashCode, string nick, string text, ChatMarker decoration)
		{
			this.ColorKey = colorKey;
			this.Time = time;
			this.NickHashCode = nickHashCode;
			this.Nick = nick;
			this.Marker = decoration;
			this.RawText = text;
			this.Process(text);
		}

		public ChatLine(string colorKey, int nickHashCode, string nick, string text, ChatMarker decoration)
			: this(colorKey, DateTime.Now, nickHashCode, nick, text, decoration)
		{
		}

		public ChatSpan GetSpan(int idx)
		{
			return this.Spans.Where((s) => idx >= s.Start && idx < s.End).FirstOrDefault();
		}

		public void Process(string raw)
		{
			var text = new StringBuilder();
			var spans = new List<ChatSpan>();
			var span = new ChatSpan();

			int last = raw.Length - 1;
			int idx = 0;
			for (int i = 0; i < raw.Length; i++)
			{
				int ichar = (int)raw[i];
				if (ichar == 2 || ichar == 3 || ichar == 15 || ichar == 22 || ichar == 31)
				{
					span.End = idx;
					spans.Add(span);
					span.Start = idx;
				}
				switch (ichar)
				{
					case 2:
						span.Flags ^= ChatSpanFlags.Bold;
						break;
					case 3:
						if (i == last || (raw[i + 1] > '9' || raw[i + 1] < '0'))
						{
							span.Flags &= ~ChatSpanFlags.Foreground;
							span.Flags &= ~ChatSpanFlags.Background;
							break;
						}
						span.Flags |= ChatSpanFlags.Foreground;
						int c = (int)(raw[++i] - '0');
						if (i < last && (
							(c == 0 && raw[i + 1] >= '0' && raw[i + 1] <= '9') ||
							(c == 1 && raw[i + 1] >= '0' && raw[i + 1] <= '5')
							))
						{
							c *= 10;
							c += (int)raw[++i] - '0';
						}
						span.Foreground = (byte)Math.Min(15, c);
						if (i == last || i + 1 == last || raw[i + 1] != ',' || raw[i + 2] < '0' || raw[i + 2] > '9')
						{
							break;
						}
						span.Flags |= ChatSpanFlags.Background;
						++i;
						c = (int)(raw[++i] - '0');
						if (i < last && (
							(c == 0 && raw[i + 1] >= '0' && raw[i + 1] <= '9') ||
							(c == 1 && raw[i + 1] >= '0' && raw[i + 1] <= '5')
							))
						{
							c *= 10;
							c += (int)raw[++i] - '0';
						}
						span.Background = (byte)Math.Min(15, c);
						break;
					case 15:
						span.Flags = ChatSpanFlags.None;
						break;
					case 22:
						span.Flags ^= ChatSpanFlags.Reverse;
						break;
					case 31:
						span.Flags ^= ChatSpanFlags.Underline;
						break;
					default:
						text.Append(raw[i]);
						idx++;
						break;
				}
			}
			span.End = idx;
			spans.Add(span);
			this.Text = text.ToString();
			this.Spans = spans.Where((s) => s.End > s.Start).ToArray();
			this.Links = (from Match m in Constants.UrlRegex.Matches(this.Text)
						 select new ChatLink { Start = m.Index, End = m.Index + m.Length }).ToArray();
		}
	}

	public interface IChatSpanProvider
	{
		ChatSpan GetSpan(int idx);
	}

	[Flags]
	public enum ChatSpanFlags
	{
		None,
		Bold = 1,
		Reverse = 2,
		Underline = 4,
		Foreground = 8,
		Background = 16
	}

	public struct ChatSpan
	{
		public int Start;
		public int End;
		public ChatSpanFlags Flags;
		public byte Foreground;
		public byte Background;
	}

	public struct ChatLink
	{
		public int Start;
		public int End;
	}
}
