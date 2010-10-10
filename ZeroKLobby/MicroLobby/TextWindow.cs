/******************************************************************************\
 * IceChat 2009 Internet Relay Chat Client
 *
 * Copyright (C) 2010 Paul Vanderzee <snerf@icechat.net>
 *                                    <www.icechat.net> 
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 * Please consult the LICENSE.txt file included with this project for
 * more details
 *
\******************************************************************************/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using JetBrains.Annotations;
using PlasmaShared;
using SpringDownloader;

namespace SpringDownloader.MicroLobby
{
    public partial class TextWindow: UserControl
    {
        const int MaxTextLines = 500;
        public const string WwwMatch = @"((https?|www\.)[^ \n\t\r]+)";
        int backColor;
        int curHighChar;
        int curHighLine;
        readonly DisplayLine[] displayLines;
        int foreColor;
        bool hideScroll;
        string hoveredWord = "";

        string previousWord;


        bool reformatLines;
        int showMaxLines;
        bool singleLine;
        int startHighChar;
        int startHighLine = -1;
        readonly TextLine[] textLines;
        int totalLines;

        int unreadMarker; // Unread marker
        readonly Regex wwwRegex = new Regex(WwwMatch);
        public int ChatBackgroundColor
        {
            get { return backColor; }
            set
            {
                backColor = value;
                Invalidate();
            }
        }
        public bool HideScroll
        {
            get { return hideScroll; }
            set
            {
                hideScroll = value;
                vScrollBar.Visible = !value;
            }
        }
        public string HoveredWord { get { return hoveredWord; } }

        public int IRCForeColor
        {
            get { return foreColor; }
            set
            {
                foreColor = value;
                Invalidate();
            }
        }
        public int LineSize { get; private set; }

        public bool NoColorMode { get; set; }
        public bool ShowUnreadLine { get; set; }

        public bool SingleLine
        {
            get { return singleLine; }
            set
            {
                singleLine = value;
                vScrollBar.Visible = !value && !hideScroll;
                Invalidate();
            }
        }
        public int TotalDisplayLines { get; set; }
        public bool UseTopicBackground { get; set; }
        public event EventHandler FocusInputRequested = delegate { };

        public TextWindow()
        {
            InitializeComponent();
            ShowUnreadLine = true;
            displayLines = new DisplayLine[MaxTextLines*4];
            textLines = new TextLine[MaxTextLines];

            vScrollBar.Scroll += OnScroll;

            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            LoadTextSizes();
        }

        public void AppendText(string newLine)
        {
            AppendText(newLine, 1);
        }

        public void ClearTextWindow()
        {
            //clear the text window of all its lines
            displayLines.Initialize();
            textLines.Initialize();

            totalLines = 0;
            TotalDisplayLines = 0;

            Invalidate();
        }

        public new void FontChanged()
        {
            LoadTextSizes();

            displayLines.Initialize();

            TotalDisplayLines = FormatLines(totalLines, 1, 0);
            UpdateScrollBar(TotalDisplayLines);
        }

        public void ResetUnread()
        {
            unreadMarker = 0;
        }

        public void ScrollToBottom()
        {
            vScrollBar.Value = TotalDisplayLines;
        }

        /// <summary>
        /// Used to scroll the Text Window a Single Line at a Time
        /// </summary>
        /// <param name="scrollUp"></param>
        public void ScrollWindow(bool scrollUp)
        {
            try
            {
                if (vScrollBar.Enabled == false) return;

                if (scrollUp)
                {
                    if (vScrollBar.Value > 1)
                    {
                        vScrollBar.Value--;
                        Invalidate();
                    }
                }
                else
                {
                    if (vScrollBar.Value <= vScrollBar.Maximum - vScrollBar.LargeChange)
                    {
                        vScrollBar.Value++;
                        Invalidate();
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("ScrollWindow:" + e);
            }
        }

        /// <summary>
        /// Used to scroll the Text Window a Page at a Time
        /// </summary>
        /// <param name="scrollUp"></param>
        public void ScrollWindowPage(bool scrollUp)
        {
            try
            {
                if (vScrollBar.Enabled == false) return;

                if (scrollUp)
                {
                    if (vScrollBar.Value > vScrollBar.LargeChange)
                    {
                        vScrollBar.Value = vScrollBar.Value - (vScrollBar.LargeChange - 1);
                        Invalidate();
                    }
                }
                else
                {
                    if (vScrollBar.Value <= vScrollBar.Maximum - (vScrollBar.LargeChange*2)) vScrollBar.Value = vScrollBar.Value + (vScrollBar.LargeChange - 1);
                    else vScrollBar.Value = vScrollBar.Maximum - vScrollBar.LargeChange + 1;
                    Invalidate();
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine("ScrollWindowPage:" + e);
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            if (displayLines == null) return;
            FontChanged();

            Invalidate();
        }

        protected override void OnMouseDown([NotNull] MouseEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (e.Button == MouseButtons.Left)
            {
                //get the current character the mouse is over. 
                startHighLine = ((Height + (LineSize/2)) - e.Y)/LineSize;
                startHighLine = TotalDisplayLines - startHighLine;
                startHighLine = (startHighLine - (TotalDisplayLines - vScrollBar.Value));

                startHighChar = ReturnChar(startHighLine, e.Location.X);

                //first need to see what word we clicked, if not, run a command
                if (HoveredWord.Length > 0)
                {
                    //check if it is a URL
                    var re = new Regex(WwwMatch);
                    var matches = re.Matches(HoveredWord);
                    string clickedWord;
                    if (matches.Count > 0)
                    {
                        clickedWord = matches[0].ToString();
                        try
                        {
                            startHighLine = -1; // reset selection
                            Process.Start(clickedWord);
                        }
                        catch (Win32Exception ex)
                        {
                            Trace.WriteLine("Failed to start browser: " + ex);
                        }
                        return;
                    }
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            //get the current line mouse is over
            var line = 0;

            if (!SingleLine)
            {
                // Get the line count from the bottom... 
                line = ((Height + (LineSize/2)) - e.Y)/LineSize;

                // Then, convert it to count from the top. 
                line = vScrollBar.Value - line;
            }

            hoveredWord = ReturnWord(line, e.Location.X);

            //get the current character the mouse is over. 
            curHighLine = ((Height + (LineSize/2)) - e.Y)/LineSize;
            curHighLine = TotalDisplayLines - curHighLine;
            curHighLine = (curHighLine - (TotalDisplayLines - vScrollBar.Value));
            curHighChar = ReturnChar(line, e.Location.X);

            if (startHighLine != -1) Invalidate();
            base.OnMouseMove(e);

            var word = HoveredWord.Trim();
            if (word == previousWord) return;
            previousWord = word;
            if (word.Length == 0)
            {
                Program.ToolTip.SetText(this, null);
                if (Cursor != Cursors.IBeam) Cursor = Cursors.IBeam;
            }
            else
            {
                var matches = wwwRegex.Matches(word);
                if (matches.Count > 0)
                {
                    if (Cursor != Cursors.Hand) Cursor = Cursors.Hand;
                }
                else if (Program.TasClient.GetUserCaseInsensitive(word) != null)
                {
                    Program.ToolTip.SetUser(this, word);
                    if (Cursor != Cursors.Hand) Cursor = Cursors.Hand;
                }
                else
                {
                    Program.ToolTip.SetText(this, null);
                    if (Cursor != Cursors.IBeam) Cursor = Cursors.IBeam;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (startHighLine > -1 && curHighLine > -1)
            {
                if (curHighLine < startHighLine || (curHighLine == startHighLine && curHighChar < startHighChar))
                {
                    var sw = startHighLine;
                    startHighLine = curHighLine;
                    curHighLine = sw;
                    sw = startHighChar;
                    startHighChar = curHighChar;
                    curHighChar = sw;
                }

                var buildString = new StringBuilder();
                var tl = displayLines[startHighLine].TextLine;
                for (var curLine = startHighLine; curLine <= curHighLine; ++curLine)
                {
                    if (tl != displayLines[curLine].TextLine)
                    {
                        buildString.Append("\r\n");
                        tl = displayLines[curLine].TextLine;
                    }
                    var s = new StringBuilder(displayLines[curLine].Line.StripAllCodes());

                    /* Filter out non-text */
                    if (curLine == curHighLine) s = s.Remove(curHighChar, s.Length - curHighChar);
                    if (curLine == startHighLine) s = s.Remove(0, startHighChar);

                    buildString.Append(s);
                }

                if (buildString.Length > 0) Clipboard.SetText(buildString.ToString());
            }

            // Supress highlighting
            startHighLine = -1;
            if (curHighLine != -1)
            {
                curHighLine = -1;
                Invalidate();
            }

            FocusInputRequested(this, EventArgs.Empty);
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            ScrollWindow(e.Delta > 0);
            ScrollWindow(e.Delta > 0);
            ScrollWindow(e.Delta > 0);
        }

        protected override void OnPaint([NotNull] PaintEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            base.OnPaint(e);
            if (!e.ClipRectangle.IsEmpty) OnDisplayText(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Height == 0 || totalLines == 0) return;

            displayLines.Initialize();

            reformatLines = true;

            Invalidate();
        }

        void AppendText(string newLine, int color)
        {
            try
            {
                //adds a new line to the Text Window
                if (newLine.Length == 0) return;

                if (!Visible) unreadMarker++;
                else if (unreadMarker > 0) unreadMarker++;

                newLine = newLine.Replace("\n", " ");
                newLine = newLine.Replace("&#x3;", TextColor.ColorChar.ToString());
                newLine = ParseUrl(newLine);

                //get the color from the line
                if (newLine[0] == TextColor.ColorChar)
                {
                    if (Char.IsNumber(newLine[1]) && Char.IsNumber(newLine[2])) foreColor = Convert.ToInt32(newLine[1].ToString() + newLine[2]);
                    else if (Char.IsNumber(newLine[1]) && !Char.IsNumber(newLine[2])) foreColor = Convert.ToInt32(newLine[1].ToString());

                    //check of foreColor is less then 32     
                    if (foreColor > 31)
                    {
                        foreColor = foreColor - 32;
                        if (foreColor > 31) foreColor = foreColor - 32;
                    }
                }
                else foreColor = color;

                newLine = NoColorMode ? TextColor.StripCodes(newLine) : RedefineColorCodes(newLine);

                totalLines++;

                if (singleLine) totalLines = 1;

                if (totalLines >= (MaxTextLines - 5))
                {
                    var x = 1;
                    for (var i = totalLines - (MaxTextLines - 50); i <= totalLines - 1; i++)
                    {
                        textLines[x].TotalLines = textLines[i].TotalLines;
                        textLines[x].Width = textLines[i].Width;
                        textLines[x].Line = textLines[i].Line;
                        textLines[x].TextColor = textLines[i].TextColor;
                        x++;
                    }

                    for (var i = (MaxTextLines - 49); i < MaxTextLines; i++)
                    {
                        textLines[i].TotalLines = 0;
                        textLines[i].Line = "";
                        textLines[i].Width = 0;
                    }

                    totalLines = MaxTextLines - 50;

                    if (Height != 0)
                    {
                        TotalDisplayLines = FormatLines(totalLines, 1, 0);
                        UpdateScrollBar(TotalDisplayLines);
                        Invalidate();
                    }

                    totalLines++;
                }

                textLines[totalLines].Line = newLine;

                var g = CreateGraphics();
                //properly measure for bold characters needed
                var sf = StringFormat.GenericTypographic;
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                textLines[totalLines].Width = (int)g.MeasureString(TextColor.StripCodes(newLine), Font, 0, sf).Width;

                g.Dispose();

                textLines[totalLines].TextColor = foreColor;

                var addedLines = FormatLines(totalLines, totalLines, TotalDisplayLines);
                addedLines -= TotalDisplayLines;

                textLines[totalLines].TotalLines = addedLines;

                for (var i = TotalDisplayLines + 1; i < TotalDisplayLines + addedLines; i++) displayLines[i].TextLine = totalLines;

                TotalDisplayLines += addedLines;

                if (singleLine)
                {
                    totalLines = 1;
                    TotalDisplayLines = 1;
                    displayLines[1].TextLine = 1;
                    textLines[1].TotalLines = 1;
                }

                UpdateScrollBar(TotalDisplayLines);

                Invalidate();
            }
            catch (Exception e)
            {
                Trace.WriteLine("AppendText Error:" + e.Message, e.StackTrace);
            }
        }


			public static Color ColorInvert(Color colorIn)
			{
				return Color.FromArgb(colorIn.A, (colorIn.R + 128) % 256, (colorIn.G +
128) % 256, (colorIn.B + 128) % 256);
			}

        /// <summary>
        /// Format the text for each line to show in the Text Window
        /// </summary>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        int FormatLines(int startLine, int endLine, int line)
        {
            //this formats each line and breaks it up, to fit onto the current display
            var displayWidth = ClientRectangle.Width - vScrollBar.Width - 10;

            if (displayWidth <= 0) return 0;

            if (totalLines == 0) return 0;

            string lastColor;
            var nextColor = "";

            var ii = line;
            var g = CreateGraphics();

            var sf = StringFormat.GenericTypographic;
            sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            for (var currentLine = endLine; currentLine <= startLine; currentLine++)
            {
                lastColor = "";
                displayLines[line].Previous = false;
                displayLines[line].Wrapped = false;

                //check of the line width is the same or less then the display width            
                if (textLines[currentLine].Width <= displayWidth)
                {
                    try
                    {
                        //System.Diagnostics.Debug.WriteLine("fits 1 line");
                        displayLines[line].Line = textLines[currentLine].Line;
                        displayLines[line].TextLine = currentLine;
                        displayLines[line].TextColor = textLines[currentLine].TextColor;
                        line++;
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("FormatLines Error1:" + startLine + ":" + endLine + ":" + ii + ":" + currentLine + ":" + textLines.Length,
                                        e.StackTrace);
                    }
                }
                else
                {
                    var lineSplit = false;
                    var curLine = textLines[currentLine].Line;

                    var buildString = new StringBuilder();

                    try
                    {
                        for (var i = 0; i < curLine.Length; i++)
                        {
                            var ch = curLine.Substring(i, 1).ToCharArray();
                            switch (ch[0])
                            {
                                    //case boldChar:
                                    //    break;
                                case TextColor.NewColorChar:
                                    buildString.Append(curLine.Substring(i, 5));
                                    if (lastColor.Length == 0) lastColor = curLine.Substring(i, 5);
                                    else nextColor = curLine.Substring(i, 5);

                                    i = i + 4;
                                    break;
                                case TextColor.EmotChar:
                                    i = i + 3;
                                    break;
                                default:
                                    //check if there needs to be a linewrap
                                    if ((int)g.MeasureString(buildString.ToString().StripAllCodes(), Font, 0, sf).Width > displayWidth)
                                    {
                                        if (lineSplit) displayLines[line].Line = lastColor + buildString;
                                        else displayLines[line].Line = buildString.ToString();

                                        displayLines[line].TextLine = currentLine;
                                        displayLines[line].Wrapped = true;
                                        displayLines[line].TextColor = textLines[currentLine].TextColor;

                                        lineSplit = true;
                                        if (nextColor.Length != 0)
                                        {
                                            lastColor = nextColor;
                                            nextColor = "";
                                        }
                                        line++;
                                        displayLines[line].Previous = true;
                                        buildString = null;
                                        buildString = new StringBuilder();
                                        buildString.Append(ch[0]);
                                    }
                                    else buildString.Append(ch[0]);
                                    break;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("Line:" + curLine.Length + ":" + curLine);
                        Trace.WriteLine("FormatLines Error2:" + startLine + ":" + endLine + ":" + ii + ":" + currentLine + ":" + textLines.Length,
                                        e.StackTrace);
                    }

                    //get the remainder
                    if (lineSplit) displayLines[line].Line = lastColor + buildString;
                    else displayLines[line].Line = buildString.ToString();

                    displayLines[line].TextLine = currentLine;
                    displayLines[line].TextColor = textLines[currentLine].TextColor;
                    line++;
                }
            }

            sf.Dispose();
            g.Dispose();

            return line;
        }

        void LoadTextSizes()
        {
            using (var g = CreateGraphics())
            {
                LineSize = Convert.ToInt32(Font.GetHeight(g));

                showMaxLines = (Height/LineSize) + 1;
                vScrollBar.LargeChange = showMaxLines;
            }
        }

        /// <summary>
        /// Method used to draw the actual text data for the Control
        /// </summary>
        void OnDisplayText(PaintEventArgs e)
        {
            if (reformatLines)
            {
                TotalDisplayLines = FormatLines(totalLines, 1, 0);
                UpdateScrollBar(TotalDisplayLines);
                reformatLines = false;
            }

            try
            {
                using (var buffer = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb))
                using (var sf = StringFormat.GenericTypographic)
                using (var g = Graphics.FromImage(buffer))
                {
                    sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                    int startY;
                    float startX = 0;
                    int linesToDraw;

                    var buildString = new StringBuilder();
                    int textSize;

                    int curLine;
                    int curForeColor, curBackColor;
                    char[] ch;

                    var displayRect = new Rectangle(0, 0, Width, Height);
                    //Bitmap buffer = new Bitmap(this.Width, this.Height, e.Graphics);

                    //g.Clear(IrcColor.colors[backColor]);

                    if (BackgroundImage != null)
                    {
                        g.FillRectangle(new SolidBrush(TextColor.GetColor(backColor)), displayRect);
                        g.DrawImage(BackgroundImage, displayRect.Left, displayRect.Top, displayRect.Width, displayRect.Height);
                    }
                    else g.FillRectangle(new SolidBrush(TextColor.GetColor(backColor)), displayRect);

                    g.InterpolationMode = InterpolationMode.Low;
                    g.SmoothingMode = SmoothingMode.HighSpeed;
                    g.PixelOffsetMode = PixelOffsetMode.None;
                    g.CompositingQuality = CompositingQuality.HighSpeed;
                    g.TextRenderingHint = TextRenderingHint.SystemDefault;

                    if (totalLines == 0) e.Graphics.DrawImageUnscaled(buffer, 0, 0);
                    else
                    {
                        var val = vScrollBar.Value;

                        linesToDraw = (showMaxLines > val ? val : showMaxLines);

                        curLine = val - linesToDraw;

                        if (singleLine)
                        {
                            startY = 0;
                            linesToDraw = 1;
                            curLine = 0;
                        }
                        else startY = Height - (LineSize*linesToDraw) - (LineSize/2);

                        var lineCounter = 0;

                        var underline = false;
                        var isInUrl = false;
                        var font = new Font(Font.Name, Font.Size, Font.Style);

                        var redline = -1;
                        if (ShowUnreadLine)
                        {
                            for (int i = TotalDisplayLines - 1, j = 0; i >= 0; --i)
                            {
                                if (!displayLines[i].Previous)
                                {
                                    ++j;
                                    if (j == unreadMarker)
                                    {
                                        redline = i;
                                        break;
                                    }
                                }
                            }
                        }

                        while (lineCounter < linesToDraw)
                        {
                            int i = 0, j = 0;
                            var highlight = false;
                            var oldHighlight = false;

                            if (redline == curLine)
                            {
                                using (var p = new Pen(Color.Red)) {
                                    g.DrawLine(p, 0, startY, Width, startY);
                                }
                            }

                            lineCounter++;

                            curForeColor = displayLines[curLine].TextColor;
                            var line = new StringBuilder();

                            line.Append(displayLines[curLine].Line);
                            curBackColor = backColor;

                            //check if in a url
                            if (!isInUrl)
                            {
                                underline = false;
                                font.SafeDispose();
                                font = new Font(Font.Name, Font.Size, Font.Style);
                            }
                            using (var backColorBrush = new SolidBrush(TextColor.GetColor(curBackColor))) {
                                if (line.Length > 0)
                                {
                                    do
                                    {
                                        ch = line.ToString().Substring(i, 1).ToCharArray();
                                        switch (ch[0])
                                        {
                                            case TextColor.EmotChar:
                                                //draws an emoticon
                                                //[]001
                                                var emotNumber = Convert.ToInt32(line.ToString().Substring(i + 1, 3));

                                                line.Remove(0, 3);
                                                if (!isInUrl)
                                                {
                                                    //select the emoticon here
                                                    var bm = TextImage.GetImage(emotNumber);

                                                    if (curBackColor != backColor)
                                                    {
                                                        textSize = (int)g.MeasureString(buildString.ToString(), Font, 0, sf).Width + 1;
                                                        var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                                        g.FillRectangle(backColorBrush, r);
                                                    }

                                                    g.DrawImage(bm,
                                                                startX + (int)g.MeasureString(buildString.ToString(), Font, 0, sf).Width,
                                                                startY,
                                                                16,
                                                                16);

                                                    using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                        g.DrawString(buildString.ToString(),
                                                                     Font,
                                                                     brush,
                                                                     startX,
                                                                     startY,
                                                                     sf);
                                                    }

                                                    startX += bm.Width + (int)g.MeasureString(buildString.ToString(), Font, 0, sf).Width;

                                                    buildString = new StringBuilder();
                                                }
                                                break;
                                            case TextColor.UrlStart:
                                                if (curBackColor != backColor)
                                                {
                                                    textSize = (int)g.MeasureString(buildString.ToString(), Font, 0, sf).Width + 1;
                                                    var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                                    g.FillRectangle(backColorBrush, r);
                                                }
                                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                    g.DrawString(buildString.ToString(),
                                                                 font,
                                                                 brush,
                                                                 startX,
                                                                 startY,
                                                                 sf);
                                                }
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor));

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString = new StringBuilder();

                                                //remove whats drawn from string
                                                line.Remove(0, i);
                                                line.Remove(0, 1);
                                                i = -1;
                                                font.SafeDispose();
                                                font = new Font(Font.Name, Font.Size, Font.Style | FontStyle.Underline);
                                                isInUrl = true;
                                                break;

                                            case TextColor.UrlEnd:
                                                if (curBackColor != backColor)
                                                {
                                                    textSize = (int)g.MeasureString(buildString.ToString(), font, 0, sf).Width + 1;
                                                    var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                                    g.FillRectangle(backColorBrush, r);
                                                }
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor));
                                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                    g.DrawString(buildString.ToString(),
                                                                 font,
                                                                 brush,
                                                                 startX,
                                                                 startY,
                                                                 sf);
                                                }

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString = new StringBuilder();

                                                //remove whats drawn from string
                                                line.Remove(0, i);
                                                line.Remove(0, 1);
                                                i = -1;
                                                font.SafeDispose();
                                                font = new Font(Font.Name, Font.Size, Font.Style);
                                                isInUrl = false;
                                                break;
                                            case TextColor.UnderlineChar:
                                                if (curBackColor != backColor)
                                                {
                                                    textSize = (int)g.MeasureString(buildString.ToString(), font, 0, sf).Width + 1;
                                                    var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                                    g.FillRectangle(backColorBrush, r);
                                                }
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor));
                                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                    g.DrawString(buildString.ToString(),
                                                                 font,
                                                                 brush,
                                                                 startX,
                                                                 startY,
                                                                 sf);
                                                }

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString = new StringBuilder();

                                                //remove whats drawn from string
                                                line.Remove(0, i);
                                                line.Remove(0, 1);
                                                i = -1;

                                                underline = !underline;
                                                font.SafeDispose();
                                                if (underline) font = new Font(Font.Name, Font.Size, Font.Style | FontStyle.Underline);
                                                else {font = new Font(Font.Name, Font.Size, Font.Style);}
                                                break;
                                            case TextColor.NewColorChar:
                                                //draw whats previously in the string
                                                if (curBackColor != backColor)
                                                {
                                                    textSize = (int)g.MeasureString(buildString.ToString(), font, 0, sf).Width + 1;
                                                    var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                                    g.FillRectangle(backColorBrush, r);
                                                }
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor));
                                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                    g.DrawString(buildString.ToString(),
                                                                 font,
                                                                 brush,
                                                                 startX,
                                                                 startY,
                                                                 sf);
                                                }

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString = new StringBuilder();

                                                //remove whats drawn from string
                                                line.Remove(0, i);

                                                //get the new fore and back colors
                                                if (!highlight)
                                                {
                                                    curForeColor = Convert.ToInt32(line.ToString().Substring(1, 2));
                                                    curBackColor = Convert.ToInt32(line.ToString().Substring(3, 2));

                                                    //check to make sure that FC and BC are in range 0-31
                                                    if (curForeColor > 31) curForeColor = displayLines[curLine].TextColor;
                                                    if (curBackColor > 31) curBackColor = backColor;
                                                }

                                                //remove the color codes from the string
                                                line.Remove(0, 5);
                                                i = -1;
                                                break;

                                            default:
                                                if (startHighLine >= 0 &&
                                                    ((curLine >= startHighLine && curLine <= curHighLine) ||
                                                     (curLine <= startHighLine && curLine >= curHighLine)))
                                                {
                                                    if ((curLine > startHighLine && curLine < curHighLine) ||
                                                        (curLine == startHighLine && j >= startHighChar &&
                                                         (curLine <= curHighLine && j < curHighChar || curLine < curHighLine)) ||
                                                        (curLine == curHighLine && j < curHighChar &&
                                                         (curLine >= startHighLine && j >= startHighChar || curLine > startHighLine))) highlight = true;
                                                    else if ((curLine < startHighLine && curLine > curHighLine) ||
                                                             (curLine == startHighLine && j < startHighChar &&
                                                              (curLine >= curHighLine && j >= curHighChar || curLine > curHighLine)) ||
                                                             (curLine == curHighLine && j >= curHighChar &&
                                                              (curLine <= startHighLine && j < startHighChar || curLine < startHighLine))) highlight = true;
                                                    else highlight = false;
                                                }
                                                else highlight = false;
                                                ++j;

                                                if (highlight != oldHighlight)
                                                {
                                                    oldHighlight = highlight;

                                                    //draw whats previously in the string                                
                                                    if (curBackColor != backColor)
                                                    {
                                                        textSize = (int)g.MeasureString(buildString.ToString(), font, 0, sf).Width + 1;
                                                        var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                                        g.FillRectangle(backColorBrush, r);
                                                    }
                                                    /*
                                                TextRenderer.DrawText(g,
                                                                      buildString.ToString(),
                                                                      font,
                                                                      new Point((int)startX, startY),
                                                                      TextColor.GetColor(curForeColor),
                                                                      TextColor.GetColor(curBackColor));
                                                 */
                                                    using (var solidBrush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                        g.DrawString(buildString.ToString(),
                                                                     font,
                                                                     solidBrush,
                                                                     startX,
                                                                     startY,
                                                                     sf);
                                                    }

                                                    startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                    buildString = new StringBuilder();

                                                    //remove whats drawn from string
                                                    line.Remove(0, i);
                                                    i = 0;
                                                    if (highlight)
                                                    {
                                                    		curForeColor = 0;
                                                    		curBackColor = 2;
                                                    }
                                                    else
                                                    {
                                                        curForeColor = displayLines[curLine].TextColor;
                                                        curBackColor = backColor;
                                                    }
                                                }
                                                buildString.Append(ch[0]);
                                                break;
                                        }

                                        i++;
                                    } while (line.Length > 0 && i != line.Length);
                                }

                                //draw anything that is left over                
                                if (i == line.Length && line.Length > 0)
                                {
                                    if (curBackColor != backColor)
                                    {
                                        textSize = (int)g.MeasureString(buildString.ToString(), font, 0, sf).Width + 1;
                                        var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
																				using (var brush = new SolidBrush(TextColor.GetColor(curBackColor)))  g.FillRectangle(brush, r);
                                    }
                                    // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor));
                                    using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                        g.DrawString(buildString.ToString(), font, brush, startX, startY, sf);
                                    }
                                }
                            }

                            startY += LineSize;
                            startX = 0;
                            curLine++;
                            buildString = new StringBuilder();
                        }
                        font.Dispose();
                        e.Graphics.DrawImageUnscaled(buffer, 0, 0);
                    }
                    // var coords = displayRect
                    ButtonRenderer.DrawButton(g,
                                              new Rectangle(displayRect.Right - 200, displayRect.Top - 50, 100, 30),
                                              "Hide",
                                              Font,
                                              false,
                                              PushButtonState.Normal);
                }
            }
            catch (Exception ee)
            {
                Trace.WriteLine("TextWindow OnDisplayText Error:" + ee.Message, ee.StackTrace);
            }
        }

        static string ParseUrl(string data)
        {
            var re = new Regex(WwwMatch);
            var matches = re.Matches(data);
            foreach (Match m in matches) data = data.Replace(TextColor.StripCodes(m.Value), TextColor.UrlStart + TextColor.StripCodes(m.Value) + TextColor.UrlEnd);

            return data;
        }

        string RedefineColorCodes(string line)
        {
            //redefine the irc server colors to own standard
            // go from \x0003xx,xx to \x0003xxxx
            const string parseBackColor = @"\x03([0-9]{1,2}),([0-9]{1,2})";
            const string parseForeColor = @"\x03[0-9]{1,2}";
            const string parseColorChar = @"\x03";

            var parseIrcCodes = new Regex(parseBackColor + "|" + parseForeColor + "|" + parseColorChar);

            var sLine = new StringBuilder();
            sLine.Append(line);

            var currentBackColor = -1;

            var m = parseIrcCodes.Match(sLine.ToString());
            while (m.Success)
            {
                var oldLen = sLine.Length;
                sLine.Remove(m.Index, m.Length);

                if (Regex.Match(m.Value, parseBackColor).Success)
                {
                    var rem = m.Value.Remove(0, 1);
                    var intstr = rem.Split(new[] { ',' });
                    //get the fore color
                    var fc = int.Parse(intstr[0]);
                    //get the back color
                    var bc = int.Parse(intstr[1]);

                    currentBackColor = bc;

                    sLine.Insert(m.Index, TextColor.NewColorChar + fc.ToString("00") + bc.ToString("00"));
                    oldLen--;
                }
                else if (Regex.Match(m.Value, parseForeColor).Success)
                {
                    var fc = int.Parse(m.Value.Remove(0, 1));

                    if (currentBackColor > -1) sLine.Insert(m.Index, TextColor.NewColorChar + fc.ToString("00") + currentBackColor.ToString("00"));
                    else
                        //sLine.Insert(m.Index, newColorChar.ToString() + fc.ToString("00") + backColor.ToString("00"));
                        sLine.Insert(m.Index, TextColor.NewColorChar + fc.ToString("00") + "99");
                }
                else if (Regex.Match(m.Value, parseColorChar).Success)
                {
                    currentBackColor = -1;
                    //sLine.Insert(m.Index, newColorChar.ToString() + foreColor.ToString("00") + backColor.ToString("00"));
                    sLine.Insert(m.Index, TextColor.NewColorChar + foreColor.ToString("00") + "99");
                }
                m = parseIrcCodes.Match(sLine.ToString(), sLine.Length - oldLen);
            }
            return sLine.ToString();
        }

        int ReturnChar(int lineNumber, int x)
        {
            if (lineNumber < TotalDisplayLines && lineNumber >= 0)
            {
                var g = CreateGraphics();
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var sf = StringFormat.GenericTypographic;
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                var line = displayLines[lineNumber].Line.StripAllCodes();

                var width = (int)g.MeasureString(line, Font, 0, sf).Width;

                if (x > width) return line.Length;

                float lookWidth = 0;
                for (var i = 0; i < line.Length; i++)
                {
                    lookWidth += g.MeasureString(line[i].ToString(), Font, 0, sf).Width;
                    if (lookWidth >= x) return i;
                }
                g.Dispose();
                return line.Length;
            }
            return 0;
        }

        string ReturnWord(int lineNumber, int x)
        {
            if (lineNumber < TotalDisplayLines && lineNumber >= 0)
            {
                var g = CreateGraphics();
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var sf = StringFormat.GenericTypographic;
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                var line = displayLines[lineNumber].Line.StripAllCodes();

                var width = (int)g.MeasureString(line, Font, 0, sf).Width;

                if (x > width) return "";

                var space = 0;
                var foundSpace = false;
                float lookWidth = 0;
                for (var i = 0; i < line.Length; i++)
                {
                    if (lookWidth >= x && foundSpace)
                    {
                        if (displayLines[lineNumber].Previous && lineNumber > 0 && space == 0)
                        {
                            // this line wraps from the previous one. 
                            var prevline = displayLines[lineNumber - 1].Line.StripAllCodes();
                            var prevwidth = (int)g.MeasureString(prevline, Font, 0, sf).Width;
                            return ReturnWord(lineNumber - 1, prevwidth);
                        }

                        return line.Substring(space, i - space);
                    }

                    if (line[i] == (char)32)
                    {
                        if (!foundSpace)
                        {
                            if (lookWidth >= x) foundSpace = true;
                            else space = i + 1;
                        }
                    }

                    lookWidth += g.MeasureString(line[i].ToString(), Font, 0, sf).Width;
                }
                if (displayLines[lineNumber].Previous && lineNumber > 0 && space == 0)
                {
                    // this line wraps from the previous one. 
                    var prevline = displayLines[lineNumber - 1].Line.StripAllCodes();
                    if (prevline[prevline.Length - 1] != ' ')
                    {
                        var prevwidth = (int)g.MeasureString(prevline, Font, 0, sf).Width;
                        return ReturnWord(lineNumber - 1, prevwidth);
                    }
                }

                if (!foundSpace && space < line.Length)
                {
                    //wrap to the next line
                    if (lineNumber < TotalDisplayLines)
                    {
                        var extra = "";
                        var currentLine = displayLines[lineNumber].TextLine;

                        while (lineNumber < TotalDisplayLines)
                        {
                            lineNumber++;
                            if (displayLines[lineNumber].TextLine != currentLine) break;

                            extra += displayLines[lineNumber].Line.StripAllCodes();
                            if (extra.IndexOf(' ') > -1)
                            {
                                extra = extra.Substring(0, extra.IndexOf(' '));
                                break;
                            }
                        }

                        return line.Substring(space) + extra;
                    }
                }

                g.Dispose();
            }
            return "";
        }

        /// <summary>
        /// Updates the scrollbar to the given line. 
        /// </summary>
        /// <param name="newValue">Line number to be displayed</param>
        /// <returns></returns>
        void UpdateScrollBar(int newValue)
        {
            showMaxLines = (Height/LineSize) + 1;
            if (InvokeRequired)
            {
                ScrollValueDelegate s = UpdateScrollBar;
                Invoke(s, new object[] { newValue });
            }
            else
            {
                var isBottom = vScrollBar.Value + vScrollBar.LargeChange - 1 >= vScrollBar.Maximum;

                if (showMaxLines < TotalDisplayLines)
                {
                    vScrollBar.LargeChange = showMaxLines;
                    vScrollBar.Enabled = true;
                }
                else
                {
                    vScrollBar.LargeChange = TotalDisplayLines;
                    vScrollBar.Enabled = false;
                }

                if (newValue != 0)
                {
                    vScrollBar.Minimum = 1;
                    vScrollBar.Maximum = newValue + vScrollBar.LargeChange - 1;

                    if (!vScrollBar.Enabled || !vScrollBar.Visible) isBottom = true;
                    if (isBottom || hideScroll) vScrollBar.Value = newValue;
                }
            }
        }

        void OnScroll(object sender, EventArgs e)
        {
            var scrollBar = (VScrollBar)sender;
            if (scrollBar.Value <= 1) scrollBar.Value = 1;

            Invalidate();
        }

        struct DisplayLine
        {
            public string Line;
            public bool Previous;
            public int TextColor;
            public int TextLine;
            public bool Wrapped;
        }

        delegate void ScrollValueDelegate(int value);

        struct TextLine
        {
            public string Line;
            public int TextColor;
            public int TotalLines;
            public int Width;
        }
    }
}