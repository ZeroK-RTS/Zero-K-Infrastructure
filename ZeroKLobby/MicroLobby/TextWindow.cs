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
using ZkData;
using ZeroKLobby;
using System.Collections.Generic;

namespace ZeroKLobby.MicroLobby
{
    public partial class TextWindow: UserControl
    {
        private const int defaultMaxLines = 495; //about 10 pages
        private const int HardMaximumLines = 29950; //absolute maximum to avoid extreme case.
        private int MaxTextLines = 1; //this size is not fixed. It expand when detected spam, and maintain size when new line are added at slow interval.
        private int MaxDisplayLines = 1;
        //old URL regex for reference: WwwMatch = @"((https?|www\.|zk://)[^\s,]+)";
        //Regex note: [^<>()] : exclude <<< or >>> or ( or ) at end of URL
        //Regex note: [\w\d:#@%/!;$()~_?\+-=\\\.&]* : include any sort of character 1 or more times
        //Reference: http://en.wikipedia.org/wiki/Regular_expression#Basic_concepts
        public const string WwwMatch = @"(www\.|www\d\.|(https?|ftp|irc|zk):((//)|(\\\\)))+[\w\d:#@%/!;$()~_?\+-=\\\.&]*[^<>()]"; //from http://icechat.codeplex.com/SourceControl/latest#532131
        int backColor;
        int curHighChar;
        int curHighLine;
        //NOTE: using List<T> instead of Array because or resize-able ability. List's "item[index]" access time is O(1) same as Array.
        //If we don't use "foreach" is probably okay.
        //Ref: http://programmers.stackexchange.com/questions/221892/should-i-use-a-list-or-an-array
        //Ref2: http://msmvps.com/blogs/jon_skeet/archive/2009/01/29/for-vs-foreach-on-arrays-and-lists.aspx
        readonly List<DisplayLine> displayLines; 
        int foreColor;
        bool hideScroll;
        string hoveredWord = "";

        string previousWord;
        private long previousAppendText = 0; //timer to check for spam (message from bots or copy-pasting humans).

        bool reformatLines;
        int showMaxLines;
        bool singleLine;
        int startHighChar;
        int startHighLine = -1;
        readonly List<TextLine> textLines;
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
        public int TotalDisplayLines { get; private set; }
        public event EventHandler FocusInputRequested = delegate { };

        public string DefaultTooltip { get; set; }

        public TextWindow()
        {
            SuspendLayout();
            InitializeComponent();
            ShowUnreadLine = true;

            vScrollBar.Scroll += OnScroll;

            DoubleBuffered = true;

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();

            LoadTextSizes();
            ResumeLayout();

            displayLines = new List<DisplayLine>(MaxDisplayLines);
            textLines = new List<TextLine>(MaxTextLines);
            InitializeTextLines();
            InitializeDisplayLines();
        }

        public void AppendText(string newLine)
        {
            AppendText(newLine, TextColor.text);
        }

        public void ClearTextWindow()
        {
            //clear the text window of all its lines
            InitializeTextLines();
            InitializeDisplayLines();

            Invalidate();
        }

        public new void FontChanged()
        {
            LoadTextSizes();

            InitializeDisplayLines();

            TotalDisplayLines = FormatLines(totalLines, 1, 0);
            UpdateScrollBar(TotalDisplayLines,0);
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
                    if (vScrollBar.Value < TotalDisplayLines) //scroll down until reach maximum
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
        public void ScrollWindowPage(bool scrollUp) //seems unused
        {
            try
            {
                if (vScrollBar.Enabled == false) return;

                if (scrollUp)
                {
                    if (vScrollBar.Value > vScrollBar.LargeChange)
                        vScrollBar.Value = vScrollBar.Value - vScrollBar.LargeChange;
                    else
                        vScrollBar.Value = 1; //reach top
                    Invalidate();
                }
                else
                {
                    if (vScrollBar.Value < (TotalDisplayLines - vScrollBar.LargeChange))
                        vScrollBar.Value = vScrollBar.Value + vScrollBar.LargeChange;
                    else 
                        vScrollBar.Value = TotalDisplayLines;//reach bottom
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
                startHighLine = ((Height + (LineSize/2)) - e.Y)/LineSize; //which row
                startHighLine = TotalDisplayLines - startHighLine; //which row from below
                startHighLine = (startHighLine - (TotalDisplayLines - vScrollBar.Value)); //which row considering scroll position

                startHighChar = ReturnChar(startHighLine, e.Location.X); //the character index on the line

                //first need to see what word we clicked, if not, run a command
                if (HoveredWord.Length > 0)
                {
                    //check if it is a URL
                    var matches = wwwRegex.Matches(HoveredWord);
                    string clickedWord;
                    if (matches.Count > 0)
                    {
                        clickedWord = matches[0].ToString();
                        try
                        {
                            startHighLine = -1; // reset selection
                            Program.MainWindow.navigationControl.Path = clickedWord; //request URL be opened (in internal browser or external depending on which is appropriate)
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
                // Get the line count from the bottom... (relative to screen)
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
                Program.ToolTip.SetText(this, DefaultTooltip);
                if (Cursor != Cursors.IBeam) Cursor = Cursors.IBeam;
            }
            else //change mouse cursor
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
                    Program.ToolTip.SetText(this, DefaultTooltip);
                    if (Cursor != Cursors.IBeam) Cursor = Cursors.IBeam;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            try {
                if (startHighLine > -1 && curHighLine > -1) {
                    if (curHighLine < startHighLine || (curHighLine == startHighLine && curHighChar < startHighChar)) {
                        int sw = startHighLine; //shift up/down role
                        startHighLine = curHighLine;
                        curHighLine = sw;
                        sw = startHighChar;
                        startHighChar = curHighChar;
                        curHighChar = sw;
                    }

                    var buildString = new StringBuilder();
                    var tl = displayLines[startHighLine].TextLine;
                    for (var curLine = startHighLine; curLine <= curHighLine; ++curLine) //if have selected multiple line
                    {
                        if (tl != displayLines[curLine].TextLine) {
                            buildString.Append("\r\n");
                            tl = displayLines[curLine].TextLine;
                        }
                        var s = new StringBuilder(displayLines[curLine].Line.StripAllCodes());

                        /* Filter out non-text */
                        if (curLine == curHighLine) s = s.Remove(curHighChar, s.Length - curHighChar);
                        if (curLine == startHighLine) s = s.Remove(0, startHighChar);

                        buildString.Append(s);
                    }

                    if (buildString.Length > 0) {
                        Program.ToolTip.SetText(this, "-----COPIED TO CLIPBOARD-----\n" + buildString.ToString());
                        //notify user that selection is copied
                        Clipboard.SetText(buildString.ToString());
                    }
                }

                // Supress highlighting
                startHighLine = -1;
                if (curHighLine != -1) {
                    curHighLine = -1;
                    Invalidate();
                }

                FocusInputRequested(this, EventArgs.Empty);
            } catch (Exception ex) {
                Trace.TraceError("{0}",ex);
            }
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

            reformatLines = true;

            Invalidate();
        }

        void AppendText(string newLine, int color)
        {
            try
            {
                //adds a new line to the Text Window
                if (newLine.Length == 0) return;
                bool isSpam = (DateTime.Now.Ticks - previousAppendText) <= 2000000; //2,000,000 ticks is 200milisecond.
                previousAppendText = DateTime.Now.Ticks;

                if (!Visible) unreadMarker++;
                else if (unreadMarker > 0) unreadMarker++;

                newLine = newLine.Replace("\n", " "); //Tips: don't use newline char for newline, split line into diff row (call AppendText() multiple time) instead
                newLine = newLine.Replace("&#x3;", TextColor.ColorChar.ToString()); //color start. &#x3 is 0x003
                newLine = newLine.Replace("&#x3", TextColor.ColorChar.ToString()); //color end
                newLine = ParseUrl(newLine);

                //get the color from the line
                if (newLine[0] == TextColor.ColorChar)
                {
                    if (Char.IsNumber(newLine[1]) && Char.IsNumber(newLine[2])) foreColor = Convert.ToInt32(newLine[1].ToString() + newLine[2]);
                    else if (Char.IsNumber(newLine[1]) && !Char.IsNumber(newLine[2])) foreColor = Convert.ToInt32(newLine[1].ToString());

                    //check of foreColor is less then 32     
                    if (foreColor > TextColor.colorRange)
                    {
                        foreColor = TextColor.text;
                        //foreColor = foreColor - 32; //fixme: keep this magic shift 32? why is 32 important? might need to recheck
                        //if (foreColor > 31) foreColor = foreColor - 32;
                    }
                }
                else foreColor = color;

                newLine = NoColorMode ? TextColor.StripCodes(newLine) : RedefineColorCodes(newLine);

                totalLines++;

                if (singleLine) totalLines = 1;

                bool needSpace = totalLines >= MaxTextLines;
                bool isExpanding = MaxTextLines < defaultMaxLines;
                bool isFull = MaxTextLines >= HardMaximumLines;
                if (needSpace && !isExpanding && (isFull || !isSpam))
                { //remove old text, create space for new text (recycle existing row)
                    int toRemove = (MaxTextLines / 10) + 1; //remove 10% of old text

                    //calculate scrollbar offset:
                    int lineCount = 0;
                    int wrapCount = 0;
                    while (lineCount < toRemove + 1)
                    {
                        if (displayLines[wrapCount + lineCount].Previous)
                            wrapCount++;
                        else
                            lineCount++;
                    }
                    int vScrollBarOffset = wrapCount + lineCount - 1;
                    vScrollBarOffset = Math.Min(vScrollBarOffset, vScrollBar.Value - 1);

                    //shift existing texts upward:
                    var x = 0;
                    for (int i = toRemove; i < totalLines; i++)
                    {
                        textLines[x].TotalLines = textLines[i].TotalLines;
                        textLines[x].Width = textLines[i].Width;
                        textLines[x].Line = textLines[i].Line;
                        textLines[x].TextColor = textLines[i].TextColor;
                        x++;
                    }

                    //fill new space with empty string
                    for (var i = totalLines - toRemove; i < MaxTextLines; i++)
                    {
                        textLines[i].TotalLines = 0;
                        textLines[i].Line = "";
                        textLines[i].Width = 0;
                    }

                    //set draw line to new space
                    totalLines = totalLines - toRemove - 1;

                    if (Height != 0)
                    {
                        TotalDisplayLines = FormatLines(totalLines, 1, 0);
                        UpdateScrollBar(TotalDisplayLines, vScrollBarOffset * -1);
                        Invalidate();
                    }

                    totalLines++;
                }
                else if (needSpace && !isFull && (isExpanding || isSpam))
                    AddNewTextLines(); //create new space for new text (new row)

                textLines[totalLines].Line = newLine;

                var g = CreateGraphics();
                //properly measure for bold characters needed
                var sf = StringFormat.GenericTypographic;
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                textLines[totalLines].Width = (int)g.MeasureString(TextColor.StripCodes(newLine), Font, 0, sf).Width;

                g.Dispose();

                textLines[totalLines].TextColor = foreColor;

                int addedLines = FormatLines(totalLines, totalLines, TotalDisplayLines); //split text into multi-line, and put into "displayLines[]"
                addedLines -= TotalDisplayLines;

                textLines[totalLines].TotalLines = addedLines;

                for (var i = TotalDisplayLines + 1; i < TotalDisplayLines + addedLines; i++) 
                    displayLines[i].TextLine = totalLines; //identify to current "textLines[]" index

                TotalDisplayLines += addedLines;

                if (singleLine)
                {
                    totalLines = 1;
                    TotalDisplayLines = 1;
                    displayLines[1].TextLine = 1;
                    textLines[1].TotalLines = 1;
                }

                UpdateScrollBar(TotalDisplayLines,0);
                Invalidate();
            }
            catch (Exception e)
            {
                Trace.WriteLine("AppendText Error:" + e.Message, e.StackTrace);
            }
        }


            public static Color ColorInvert(Color colorIn)
            {
                return Color.FromArgb(colorIn.A, (colorIn.R + 128) % 256, (colorIn.G + 128) % 256, (colorIn.B + 128) % 256);
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
                        if (line >= MaxDisplayLines)
                            AddNewDisplayLines();
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine("FormatLines Error1:" + startLine + ":" + endLine + ":" + ii + ":" + currentLine + ":" + textLines.Capacity,
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
                        float emotSpace = 0.0f;
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
                                    buildString.Append(curLine.Substring(i, 4));//make emot icon not disappear for multiline sentences
                                    var emotNumber = Convert.ToInt32(curLine.Substring(i+1, 3)); //take account for extra space caused by emotIcon, use it to find an accurate point where to split line
                                    var bm = TextImage.GetImage(emotNumber);
                                    emotSpace += bm.Width;

                                    i = i + 3;
                                    break;
                                default:
                                    //check if there needs to be a linewrap
                                    if (g.MeasureString(buildString.ToString().StripAllCodes(), Font, 0, sf).Width + emotSpace > displayWidth)
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
                                        if (line >= MaxDisplayLines)
                                            AddNewDisplayLines();

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
                        Trace.WriteLine("FormatLines Error2:" + startLine + ":" + endLine + ":" + ii + ":" + currentLine + ":" + textLines.Capacity,
                                        e.StackTrace);
                    }

                    //get the remainder
                    if (lineSplit) displayLines[line].Line = lastColor + buildString;
                    else displayLines[line].Line = buildString.ToString();

                    displayLines[line].TextLine = currentLine;
                    displayLines[line].TextColor = textLines[currentLine].TextColor;
                    line++;
                    if (line >= MaxDisplayLines)
                        AddNewDisplayLines();
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
                vScrollBar.LargeChange = Math.Max(showMaxLines - 3 , 1); //include previous 3 lines for continuity
            }
        }

        /// <summary>
        /// Method used to draw the actual text data for the Control
        /// </summary>
        void OnDisplayText(PaintEventArgs e)
        {
            if (reformatLines)
            {
                InitializeDisplayLines();
                TotalDisplayLines = FormatLines(totalLines, 1, 0);
                UpdateScrollBar(TotalDisplayLines,0);
                reformatLines = false;
            }

            try
            {
                using (var buffer = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb))
                using (var sf = StringFormat.GenericTypographic)
                using (var g = Graphics.FromImage(buffer)) //using "using" allow auto dispose
                {
                    sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                    int startY;
                    float startX = 0;
                    int linesToDraw;

                    var buildString = new StringBuilder();
                    int textSize;

                    int curLine;
                    int curForeColor, curBackColor;
                    int pastForeColor=-1; //remember what is the text color before highlighting. Hint: the code is looping from left char to right char
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

                        linesToDraw = Math.Min(showMaxLines, val); //Math.Min is faster and more readable than if-else, Reference:http://stackoverflow.com/questions/5478877/math-max-vs-inline-if-what-are-the-differences

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

                        while (lineCounter < linesToDraw) //iterate over all rows
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

                            //check if in a url, cancel underline if not
                            if (!isInUrl)
                            {
                                underline = false;
                                font.SafeDispose();
                                font = new Font(Font.Name, Font.Size, Font.Style);
                            }

                            if (line.Length > 0)
                            {
                                do //iterate over every character in a line
                                {
                                    using (SolidBrush backColorBrush = new SolidBrush(TextColor.GetColor(curBackColor))) //refresh backcolor
                                    {
                                        ch = line.ToString().Substring(i, 1).ToCharArray();
                                        switch (ch[0])
                                        {
                                            case TextColor.EmotChar: //this header is added by SaidLine.cs & SaidLineEx.cs
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
                                                        g.FillRectangle(backColorBrush, r); //draw white (or black) rectangle
                                                    }

                                                    g.DrawImage(bm, //draw an emoticon
                                                                startX + g.MeasureString(buildString.ToString(), Font, 0, sf).Width,
                                                                startY,
                                                                16,
                                                                16);

                                                    using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                        g.DrawString(buildString.ToString(), //draw text (that wasn't yet drawn up to *this* point)
                                                                        Font,
                                                                        brush,
                                                                        startX,
                                                                        startY,
                                                                        sf);
                                                    }

                                                    startX += bm.Width + g.MeasureString(buildString.ToString(), Font, 0, sf).Width;

                                                    buildString.Clear(); //reset the content (because we already draw it for user)
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
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor)); //is slow for slow gpu IMO

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString.Clear();

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
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor)); //is slow for slow gpu IMO
                                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                    g.DrawString(buildString.ToString(),
                                                                    font,
                                                                    brush,
                                                                    startX,
                                                                    startY,
                                                                    sf);
                                                }

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString.Clear();

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
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor)); //is slow for slow gpu IMO
                                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                    g.DrawString(buildString.ToString(),
                                                                    font,
                                                                    brush,
                                                                    startX,
                                                                    startY,
                                                                    sf);
                                                }

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString.Clear();

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
                                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor)); //is slow for slow gpu IMO
                                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                    g.DrawString(buildString.ToString(),
                                                                    font,
                                                                    brush,
                                                                    startX,
                                                                    startY,
                                                                    sf);
                                                }

                                                startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                buildString.Clear();

                                                //remove whats drawn from string
                                                line.Remove(0, i);

                                                //get the new fore and back colors
                                                if (!highlight)
                                                {
                                                    curForeColor = Convert.ToInt32(line.ToString().Substring(1, 2));
                                                    curBackColor = Convert.ToInt32(line.ToString().Substring(3, 2));

                                                    //check to make sure that FC and BC are in range 0-32
                                                    if (curForeColor > TextColor.colorRange) curForeColor = displayLines[curLine].TextColor;
                                                    if (curBackColor > TextColor.colorRange) curBackColor = backColor;
                                                }
                                                else //if highlighting then:
                                                {
                                                    pastForeColor = Convert.ToInt32(line.ToString().Substring(1, 2)); //remember what color this text suppose to be (will be restored to the text on the right if highlighting only happen to text on the left) 
                                                    
                                                    //check to make sure that FC and BC are in range 0-32
                                                    if (pastForeColor > TextColor.colorRange) pastForeColor = displayLines[curLine].TextColor; //only happen on exceptional case (this is only for safety, no significant whatsoever)
                                                }

                                                //remove the color codes from the string
                                                line.Remove(0, 5);
                                                i = -1;
                                                break;

                                            default:
                                                //random symbol safety check (symbols to skip drawing)
                                                if((int)ch[0] > Int16.MaxValue) //random symbol can mess up graphic object "g" in Linux! which cause nothing to be drawn after the symbol
                                                {
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
                                                    startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width;
                                                    float symbolWidth;
                                                    using (var tmpBuffer = new Bitmap(LineSize,LineSize, PixelFormat.Format32bppPArgb))
                                                    using (var tmpG = Graphics.FromImage(tmpBuffer)) //temporary graphic object that can be messed up independently
                                                    {
                                                        var randomChar = ch[0].ToString();
                                                        symbolWidth = tmpG.MeasureString(randomChar, font, 0, sf).Width;
                                                        if (symbolWidth>0) //skip when symbol width == 0 (symptom obtained from trial-n-error)
                                                            using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                                g.DrawString(randomChar,
                                                                             font,
                                                                             brush,
                                                                             startX,
                                                                             startY,
                                                                             sf);
                                                             }
                                                    }
                                                    startX += symbolWidth;

                                                    line.Remove(0, i);
                                                    line.Remove(0, 1);
                                                    i= -1;

                                                    buildString.Clear();

                                                    break;
                                                }
                                                //curLine is "the line being processed here" (not user's line)
                                                //startHightLine is "the line where highlight start"
                                                //curHighLine is "the line where highlight end"
                                                //startHighChar is "the char where highlight start"
                                                //curHighChar is "the char where highlight end"
                                                //j is "the char being processed here"
                                                if (startHighLine >= 0 && //highlight is active
                                                    ((curLine >= startHighLine && curLine <= curHighLine) || //processing in between highlight (if highlight is upward)
                                                        (curLine <= startHighLine && curLine >= curHighLine)))  //processing in between highlight (if highlight is downward)
                                                {
                                                    if ((curLine > startHighLine && curLine < curHighLine) ||
                                                        (curLine == startHighLine && j >= startHighChar && (curLine <= curHighLine && j < curHighChar || curLine < curHighLine)) ||
                                                        (curLine == curHighLine && j < curHighChar && (curLine >= startHighLine && j >= startHighChar || curLine > startHighLine)))
                                                    {
                                                        highlight = true;
                                                    }
                                                    else if ((curLine < startHighLine && curLine > curHighLine) ||
                                                                (curLine == startHighLine && j < startHighChar && (curLine >= curHighLine && j >= curHighChar || curLine > curHighLine)) ||
                                                                (curLine == curHighLine && j >= curHighChar && (curLine <= startHighLine && j < startHighChar || curLine < startHighLine)))
                                                    {
                                                        highlight = true;
                                                    }
                                                    else highlight = false;
                                                }
                                                else highlight = false;
                                                ++j;

                                                if (highlight != oldHighlight) //at highlight border (where left & right is highlight or not highlight)
                                                {
                                                    oldHighlight = highlight;

                                                    //draw whats previously in the string                                
                                                    if (curBackColor != backColor)
                                                    {
                                                        textSize = (int)g.MeasureString(buildString.ToString(), font, 0, sf).Width + 1;
                                                        var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                                        g.FillRectangle(backColorBrush, r); //draw black (or white) rectangle
                                                    }
                                                    /* //GDI example:
                                                        TextRenderer.DrawText(g,
                                                                        buildString.ToString(),
                                                                        font,
                                                                        new Point((int)startX, startY),
                                                                        TextColor.GetColor(curForeColor),
                                                                        TextColor.GetColor(curBackColor));
                                                    */
                                                    using (var solidBrush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                                        g.DrawString(buildString.ToString(), //draw text (that wasn't yet drawn up to *this* point)
                                                                        font,
                                                                        solidBrush,
                                                                        startX,
                                                                        startY,
                                                                        sf);
                                                    }

                                                    startX += g.MeasureString(buildString.ToString(), font, 0, sf).Width; //textSizes[32]

                                                    buildString.Clear();//reset the content (because we already draw it for user)

                                                    //remove whats drawn from string
                                                    line.Remove(0, i);
                                                    i = 0;
                                                    if (highlight)
                                                    {
                                                        pastForeColor = curForeColor; //remember previous text color
                                                        curForeColor = TextColor.background; //white (defined in TextColor.cs)
                                                        curBackColor = TextColor.text; //black (defined in TextColor.cs)
                                                    }
                                                    else
                                                    {
                                                        curForeColor = pastForeColor; //restore intended text color
                                                        curBackColor = backColor;
                                                    }
                                                }
                                                buildString.Append(ch[0]);
                                                break;
                                        }
                                        i++;
                                    }
                                } while (line.Length > 0 && i != line.Length); //loop character
                            }

                            //draw anything that is left over                
                            if (i == line.Length && line.Length > 0)
                            {
                                if (curBackColor != backColor)
                                {
                                    textSize = (int)g.MeasureString(buildString.ToString(), font, 0, sf).Width + 1;
                                    var r = new Rectangle((int)startX, startY, textSize + 1, LineSize + 1);
                                    using (var brush = new SolidBrush(TextColor.GetColor(curBackColor))) g.FillRectangle(brush, r);
                                }
                                // TextRenderer.DrawText(g, buildString.ToString(), font, new Point((int)startX, startY), TextColor.GetColor(curForeColor), TextColor.GetColor(curBackColor));
                                using (var brush = new SolidBrush(TextColor.GetColor(curForeColor))) {
                                    g.DrawString(buildString.ToString(), font, brush, startX, startY, sf);
                                }
                            }

                            startY += LineSize;
                            startX = 0;
                            curLine++;
                            buildString.Clear();
                        } //loop line
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
                    buildString = null;
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
            //redefine the irc server colors to own standard (ie: don't have comma)
            // go from \x0003xx,xx to \x0003xxxx
            const string parseBackColor = @"\x03([0-9]{1,2}),([0-9]{1,2})";
            const string parseForeColor = @"\x03[0-9]{1,2}";
            const string parseColorChar = @"\x03";
            const string parseColorResetChar = @"\x0F";

            var parseIrcCodes = new Regex(parseBackColor + "|" + parseForeColor + "|" + parseColorChar + "|" + parseColorResetChar);

            var sLine = new StringBuilder();
            sLine.Append(line);

            var currentBackColor = -1;

            int[] foreColorNest = new int[32]; //try to record/remember up to 32 color nest
            int[] backColorNest = new int[32];
            int foreColorInd = 0;
            int backColorInd = 0;
            foreColorNest[0] = TextColor.text;
            backColorNest[0] = TextColor.background;

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
                    if (foreColorInd < 32)
                    {
                        foreColorInd++;
                        backColorInd++;
                        foreColorNest[foreColorInd] = fc;
                        backColorNest[backColorInd] = bc;
                    }
                    oldLen--;
                }
                else if (Regex.Match(m.Value, parseForeColor).Success)
                {
                    var fc = int.Parse(m.Value.Remove(0, 1));

                    if (currentBackColor > -1) sLine.Insert(m.Index, TextColor.NewColorChar + fc.ToString("00") + currentBackColor.ToString("00"));
                    else
                        sLine.Insert(m.Index, TextColor.NewColorChar + fc.ToString("00") + "99"); //note: any background_color > 32 automatically use current backcolor
                        //sLine.Insert(m.Index, newColorChar.ToString() + fc.ToString("00") + backColor.ToString("00"));

                    if (foreColorInd < 32)
                    {
                        foreColorInd++;
                        backColorInd++;
                        foreColorNest[foreColorInd] = fc;
                        backColorNest[backColorInd] = currentBackColor > -1? currentBackColor:99;
                    }
                }
                else if (Regex.Match(m.Value, parseColorChar).Success)
                {
                    if (foreColorInd > 0) foreColorInd--;
                    if (backColorInd > 0) backColorInd--;
                    int prevForeColor = foreColorNest[foreColorInd];
                    int backForeColor = backColorNest[backColorInd];
                    sLine.Insert(m.Index, TextColor.NewColorChar + prevForeColor.ToString("00") + backForeColor.ToString("00"));
                    currentBackColor = backForeColor;

                    //currentBackColor = -1;
                    //sLine.Insert(m.Index, newColorChar.ToString() + foreColor.ToString("00") + backColor.ToString("00"));
                    //sLine.Insert(m.Index, TextColor.NewColorChar + foreColor.ToString("00") + "99"); //note: any background_color > 32 automatically use current backcolor
                }
                else if (Regex.Match(m.Value, parseColorResetChar).Success)
                {
                    foreColorInd = 0;
                    backColorInd = 0;
                    currentBackColor = -1;
                    sLine.Insert(m.Index, TextColor.NewColorChar + TextColor.text.ToString("00") + TextColor.background.ToString("00"));
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

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                var sf = StringFormat.GenericTypographic;
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                var lineEmot = displayLines[lineNumber].Line.StripAllCodesExceptEmot();
                var line = displayLines[lineNumber].Line.StripAllCodes(); //get all character of the line (Note: StripAllCodes() is a function in TextColor.cs)

                //do line-width check once if "x" is greater than line width, else check every character for the correct position where "x" is pointing at. 
                //int width = (int)g.MeasureString(line, Font, 0, sf).Width; //<-- you can uncomment this and comment the next line. The bad thing is: it underestimate end position if there's emotIcon, it cut some char during selection
                int width = (int)g.MeasureString(lineEmot, Font, 0, sf).Width;
                if (x > width)
                {
                    g.Dispose();
                    return line.Length; //end of line
                }

                //check every character INCLUDING icon position for the correct position where "x" is pointing at.
                float lookWidth = 0;
                for (var i = 0; i < line.Length; i++) //check every character in a line
                {
                    if ((char)lineEmot[i] == TextColor.EmotChar)
                    {
                        var emotNumber = Convert.ToInt32(lineEmot.ToString().Substring(i + 1, 3));
                        lineEmot = lineEmot.Remove(i, 4);
                        var bm = TextImage.GetImage(emotNumber);
                        lookWidth += bm.Width;
                        i--; //halt pointer position for this time once (at second try the emot char will be gone, its size is added and continue checking the other stuff)
                        continue;
                    }
                    float charWidth = g.MeasureString(line[i].ToString(), Font, 0, sf).Width;
                    lookWidth += charWidth;
                    if ((int)lookWidth >= (x + (int)charWidth/2)) //check whether this character is on cursor position or not.  Note: char checking & x-coordinate is checked from left to right (everything is from left to right)
                    {
                        g.Dispose();
                        return i;
                    }
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
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;

                var sf = StringFormat.GenericTypographic;
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                var lineEmot = displayLines[lineNumber].Line.StripAllCodesExceptEmot();
                var line = displayLines[lineNumber].Line.StripAllCodes();

                //do line-width check once if "x" is greater than line width,
                int width = (int)g.MeasureString(line, Font, 0, sf).Width;
                //int width = (int)g.MeasureString(lineEmot, Font, 0, sf).Width; / //<-- you can uncomment this and comment the previous line. The bad thing is: will overestimate hyperlinks ending, which make you able to click empty space
                if (x > width)
                {
                    g.Dispose();
                    return "";
                }
                if (x <= 0)
                {
                    g.Dispose();
                    return "";
                }

                var space = 0;
                var foundSpace = false;
                float lookWidth = 0;
                for (var i = 0; i < line.Length; i++)
                {
                    if ((char)lineEmot[i] == TextColor.EmotChar) //equal to an emot icon
                    {
                        int emotNumber = Convert.ToInt32(lineEmot.ToString().Substring(i + 1, 3));
                        lineEmot = lineEmot.Remove(i, 4); //clear emot char from lineEmot
                        Image bm = TextImage.GetImage(emotNumber);
                        lookWidth += bm.Width; //add emot size
                        i--; //halt pointer position for this time once (at second try the emot char will be gone, its size is added and continue checking the other stuff)
                        continue;
                    }

                    if ((int)lookWidth >= x && foundSpace)
                    {
                        if (displayLines[lineNumber].Previous && lineNumber > 0 && space == 0)
                        {
                            // this line wraps from the previous one. 
                            var prevline = displayLines[lineNumber - 1].Line.StripAllCodes();
                            int prevwidth = (int)g.MeasureString(prevline, Font, 0, sf).Width;
                            g.Dispose();
                            return ReturnWord(lineNumber - 1, prevwidth);
                        }
                        g.Dispose();
                        return line.Substring(space, i - space); //Substring(space, i - space), in example: xxx__Yxxxx_T_xxx OR Yxx_T_xxxxx__xxx (where T is pointing at spaces, Y pointing at 1st letter)
                    }

                    if (line[i] == (char)32) //equal to "space"
                    {
                        if (!foundSpace)
                        {
                            if ((int)lookWidth >= x)
                            {
                                foundSpace = true; //current position, in example: xxx__xxxxx_T_xxx (where T is pointing at space on right)
                                i--; //halt pointer position for this time once (at second loop the mid-code will be executed to return the Substring)
                            }
                            else space = i + 1; //i + 1 position, in example: xxx__Yxxxx__xxx (Y at 1st letter after a space)
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
                        int prevwidth = (int)g.MeasureString(prevline, Font, 0, sf).Width;
                        g.Dispose();
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
                        g.Dispose();
                        return line.Substring(space) + extra;
                    }
                }
                g.Dispose();
            }
            return "";
        }

        /// <summary>
        /// Updates the scrollbar to the "newValue" when scrollbar is at bottom,
        /// or offset the scrollbar position by the "offsetValue" if its not at bottom.
        /// </summary>
        /// <param name="newValue">New line number to be displayed</param>
        /// <param name="offsetValue">Offset to the current line number</param>
        /// <returns></returns>
        void UpdateScrollBar(int newValue, int offsetValue)
        {
            showMaxLines = (Height/LineSize) + 1;
            if (InvokeRequired)
            {
                ScrollValueDelegate s = UpdateScrollBar;
                Invoke(s, new object[] { newValue, offsetValue });
            }
            else
            {
                bool nearBottom = vScrollBar.Value + 5 >= TotalDisplayLines;
                bool isBottom = vScrollBar.Value >= (1 + vScrollBar.Maximum - vScrollBar.LargeChange); //exactly at UI's scrollbar bottom

                if (showMaxLines < TotalDisplayLines)
                {
                    vScrollBar.LargeChange = Math.Max(showMaxLines - 3, 1); //include previous 3 lines for continuity
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
                    vScrollBar.Maximum = Math.Max(-1 + TotalDisplayLines + vScrollBar.LargeChange,1); // maximum value that can be reached through UI is: 1 + Maximum - LargeChange. Ref: http://msdn.microsoft.com/en-us/library/system.windows.forms.scrollbar.maximum(v=vs.110).aspx

                    if (!vScrollBar.Enabled || !vScrollBar.Visible) isBottom = true;
                    if (isBottom || hideScroll || nearBottom) vScrollBar.Value = newValue; //always set to TotalDisplayLines to make scrollbar follow new chat message feed 
                    else vScrollBar.Value = vScrollBar.Value + offsetValue; //this fix scrollbar lost user's position when all text is being shifted upward to make room for new chat message
                }
            }
        }

        void OnScroll(object sender, EventArgs e)
        {
            var scrollBar = (VScrollBar)sender;
            scrollBar.Value = Math.Max(scrollBar.Value , 1); //Note: Math.Max is used for clamping value (to avoid OutOfRange message in MONO)

            Invalidate();
        }

        //NOTE: using LIST require us to use  "private class" instead of "struct". Reference: http://forums.whirlpool.net.au/archive/1282624
        //example of "struct" used for ARRAY:
        //struct DisplayLine
        //{
        //    public string Line;
        //    public bool Previous;
        //    public int TextColor;
        //    public int TextLine;
        //    public bool Wrapped;
        //}

        private class DisplayLine
        {
            public string Line { get; set; }
            public bool Previous { get; set; }
            public int TextColor { get; set; }
            public int TextLine { get; set; }
            public bool Wrapped { get; set; }
        }

        delegate void ScrollValueDelegate(int value,int offset);

        private class TextLine
        {
            public string Line { get; set; }
            public int TextColor { get; set; }
            public int TotalLines { get; set; }
            public int Width { get; set; }
        }

        private void AddNewTextLines()
        {
            textLines.Add(new TextLine());
            MaxTextLines = MaxTextLines + 1;
        }

        private void AddNewDisplayLines()
        {
            displayLines.Add(new DisplayLine());
            MaxDisplayLines = MaxDisplayLines + 1;
        }

        private void InitializeTextLines()
        {
            textLines.Clear();
            MaxTextLines = 1;
            totalLines = 0;
            textLines.Add(new TextLine());
            textLines.TrimExcess();
        }

        private void InitializeDisplayLines()
        {
            displayLines.Clear();
            MaxDisplayLines = 1;
            TotalDisplayLines = 0;
            displayLines.Add(new DisplayLine());
            displayLines.TrimExcess();
        }
    }
}