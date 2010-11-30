using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Floe.UI
{
	public class ChatFormatter : TextSource
	{
		#region Nested classes

		private class CustomTextRunProperties : TextRunProperties
		{
			private Typeface _typeface;
			private double _fontSize;
			private Brush _foreground;
			private Brush _background;
			private TextDecorationCollection _decorations;

			public override double FontHintingEmSize { get { return _fontSize; } }
			public override TextDecorationCollection TextDecorations { get { return _decorations; } }
			public override TextEffectCollection TextEffects { get { return null; } }
			public override CultureInfo CultureInfo { get { return CultureInfo.InvariantCulture; } }
			public override Typeface Typeface { get { return _typeface; } }
			public override double FontRenderingEmSize { get { return _fontSize; } }
			public override Brush BackgroundBrush { get { return _background; } }
			public override Brush ForegroundBrush { get { return _foreground; } }

			public CustomTextRunProperties(Typeface typeface, double fontSize, Brush foreground, Brush background, bool underline)
			{
				_typeface = typeface;
				_fontSize = fontSize;
				_foreground = foreground;
				_background = background;
				if (underline)
				{
					_decorations = new TextDecorationCollection(1);
					_decorations.Add(System.Windows.TextDecorations.Underline);
				}
			}
		}

		private class CustomParagraphProperties : TextParagraphProperties
		{
			private TextRunProperties _defaultProperties;
			private TextWrapping _textWrapping;

			public override FlowDirection FlowDirection { get { return FlowDirection.LeftToRight; } }
			public override TextAlignment TextAlignment { get { return TextAlignment.Left; } }
			public override double LineHeight { get { return 0.0; } }
			public override bool FirstLineInParagraph { get { return false; } }
			public override TextWrapping TextWrapping { get { return _textWrapping; } }
			public override TextMarkerProperties TextMarkerProperties { get { return null; } }
			public override TextRunProperties DefaultTextRunProperties { get { return _defaultProperties; } }
			public override double Indent { get { return 0.0; } }

			public CustomParagraphProperties(TextRunProperties defaultTextRunProperties)
			{
				_defaultProperties = defaultTextRunProperties;
				_textWrapping = TextWrapping.Wrap;
			}
		}

		#endregion

		private string _text;
		private CustomTextRunProperties _runProperties;
		private CustomParagraphProperties _paraProperties;
		private TextFormatter _formatter;
		private IChatSpanProvider _spans;
		private Brush _background;
		private IDictionary<string,Brush> _palette;

		public ChatFormatter(Typeface typeface, double fontSize, Brush foreground, IDictionary<string,Brush> palette)
		{
			_runProperties = new CustomTextRunProperties(typeface, fontSize, foreground, Brushes.Transparent, false);
			_paraProperties = new CustomParagraphProperties(_runProperties);
			_formatter = TextFormatter.Create(TextFormattingMode.Display);
			_palette = palette;
		}

		public IEnumerable<TextLine> Format(string text, IChatSpanProvider spans, double width, Brush foreground, Brush background,
			TextWrapping textWrapping)
		{
			_text = text;
			_spans = spans;
			_background = background;
			_runProperties = new CustomTextRunProperties(_runProperties.Typeface, _runProperties.FontRenderingEmSize, foreground,
				Brushes.Transparent, false);
			_paraProperties = new CustomParagraphProperties(_runProperties);
			if (width < 0)
			{
				width = 0;
				text = "";
			}

			int idx = 0;
			while(idx < _text.Length)
			{
				var line = _formatter.FormatLine(this, idx, width, _paraProperties, null);
				idx += line.Length;
				yield return line;
			}
		}

		public override TextRun GetTextRun(int idx)
		{
			if (idx >= _text.Length)
			{
				return new TextEndOfLine(1);
			}
			var props = _runProperties;
			int end = _text.Length;
			if (_spans != null)
			{
				var span = _spans.GetSpan(idx);
				if (span.Flags > 0)
				{
					props = new CustomTextRunProperties(
						new Typeface(_runProperties.Typeface.FontFamily,
							_runProperties.Typeface.Style,
							(span.Flags & ChatSpanFlags.Bold) > 0 ? FontWeights.Bold : FontWeights.Normal,
							_runProperties.Typeface.Stretch),
							_runProperties.FontRenderingEmSize,
							(span.Flags & ChatSpanFlags.Reverse) > 0 ? _background : 
							((span.Flags & ChatSpanFlags.Foreground) > 0 ? _palette["Color"+span.Foreground] : _runProperties.ForegroundBrush),
							(span.Flags & ChatSpanFlags.Reverse) > 0 ? _runProperties.ForegroundBrush :
							((span.Flags & ChatSpanFlags.Background) > 0 ? _palette["Color"+span.Background] : _runProperties.BackgroundBrush),
							(span.Flags & ChatSpanFlags.Underline) > 0);
				}
				end = span.End;
			}
			return new TextCharacters(_text, idx, end - idx, props);
		}

		public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
		{
			throw new NotImplementedException();
		}

		public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
		{
			throw new NotImplementedException();
		}
	}
}
