//PopupNotify - A MSN-style tray popup notification control.
//Copyright (C) 2005 Benjamin Hollis
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//
//PopupNotify is based loosely on the NotifyWindow control on CodeProject
//written by Robert Misiak http://www.codeproject.com/cs/miscctrl/RobMisNotifyWindow.asp
//
//Please email me at brh@numbera.com if you change this code, so that
//I can integrate the changes or at least give you a link and some credit!
//
//Download more Windows Forms controls at http://brh.numbera.com/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using JetBrains.Annotations;

namespace ZeroKLobby.MicroLobby
{
    /// <summary>
    /// Summary description for PopupNotify.
    /// Note: No properties may be changed after Show - don't do it! It won't cause an error but it'll mess you up.
    /// </summary>
    public class PopupNotify: Form
    {
        const int ABE_BOTTOM = 3;
        const int ABE_LEFT = 0;
        const int ABE_RIGHT = 2;
        const int ABE_TOP = 1;
        const int ABM_GETTASKBARPOS = 5;
        const int ABM_QUERYPOS = 0x00000002;
        const Int32 HWND_TOPMOST = -1;
        const Int32 SW_SHOWNOACTIVATE = 4;
        const Int32 SWP_NOACTIVATE = 0x0010;
        LinearGradientBrush bBackground = null;
        PictureBox closeButton;

        static Bitmap closeCold = null;
        static Bitmap closeDown = null;
        static Bitmap closeHot = null;
        IContainer components;
        Timer displayTimer;
        PictureBox iconBox;
        Label NotifyMessage;
        Label NotifyTitle;
        static List<PopupNotify> openPopups = new List<PopupNotify>();
        SystemTrayLocation sysLoc;
        /// <summary>
        /// Gets or sets the amount of time the slide in/out animations take, in ms.
        /// </summary>
        public int AnimateTime;
        /// <summary>
        /// An EventHandler called when the NotifyWindow main text is clicked.
        /// </summary>
        //public event System.EventHandler TextClicked;
        /// <summary>
        /// An EventHandler called when the NotifyWindow title text is clicked.
        /// </summary>
        //public event System.EventHandler TitleClicked;
        /// <summary>
        /// Gets or sets the gradient color which will be blended in drawing the background.
        /// </summary>
        public Color GradientColor;
        public int IconHeight = 48;
        public Image IconImage { get { return iconBox.Image; } set { iconBox.Image = new Bitmap(value); } }
        public int IconWidth = 48;
        /// <summary>
        /// Gets or sets the message text to be displayed in the NotifyWindow.
        /// </summary>
        public string Message { get { return NotifyMessage.Text; } set { NotifyMessage.Text = value; } }
        /// <summary>
        /// Gets or sets the title text to be displayed in the NotifyWindow.
        /// </summary>
        public string Title { get { return NotifyTitle.Text; } set { NotifyTitle.Text = value; } }
        /// <summary>
        /// Gets or sets a value specifiying whether or not the window should continue to be displayed if the mouse cursor is inside the bounds
        /// of the NotifyWindow.
        /// </summary>
        public bool WaitOnMouseOver;
        /// <summary>
        /// Gets or sets the amount of milliseconds to display the NotifyWindow for.
        /// </summary>
        public int WaitTime;

        public PopupNotify(): this("", "") {}

        public PopupNotify(string titleText, string messageText)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            foreach (Control c in Controls) c.Font = SystemFonts.MessageBoxFont;
            NotifyTitle.Font = new Font(SystemFonts.MessageBoxFont.Name, 12f, FontStyle.Regular, GraphicsUnit.Point);
            //this.NotifyMessage.Font = new Font(SystemFonts.MessageBoxFont.Name, 10f, FontStyle.Regular, GraphicsUnit.Point);

            Title = titleText;
            Message = messageText;

            if (closeCold == null) closeCold = DrawCloseButton(CloseButtonState.Normal);
            if (closeHot == null) closeHot = DrawCloseButton(CloseButtonState.Hot);
            if (closeDown == null) closeDown = DrawCloseButton(CloseButtonState.Pushed);

            closeButton.Image = closeCold;

            // Default values
            BackColor = Color.SkyBlue;
            GradientColor = Color.WhiteSmoke;
            WaitOnMouseOver = true;
            WaitTime = 4000;
            AnimateTime = 250;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }

                if (iconBox.Image != null)
                {
                    iconBox.Image.Dispose();
                    iconBox.Image = null;
                }

                if (bBackground != null)
                {
                    bBackground.Dispose();
                    bBackground = null;
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnPaintBackground([NotNull] PaintEventArgs e)
        {
            if (e == null) throw new ArgumentNullException("e");
            if (bBackground == null)
            {
                var rBackground = new Rectangle(0, 0, Width, Height);
                bBackground = new LinearGradientBrush(rBackground, BackColor, GradientColor, 90f);
            }

            // Getting the graphics object
            var g = e.Graphics;

            var windowRect = new Rectangle(0, 0, Width, Height);
            windowRect.Inflate(-1, -1);

            // Draw the gradient onto the form
            g.FillRectangle(bBackground, windowRect);

            if (BackgroundImage != null)
            {
                var DestRect = new Rectangle(windowRect.Right - BackgroundImage.Width,
                                             windowRect.Bottom - BackgroundImage.Height,
                                             BackgroundImage.Width,
                                             BackgroundImage.Height);
                e.Graphics.DrawImage(BackgroundImage, DestRect);
            }

            // Next draw borders...
            DrawBorder(e.Graphics);
        }

        void AnimateWindow(bool positive, bool hide)
        {
            var flags = AnimateWindowFlags.AW_SLIDE;

            if (positive) flags |= AnimateWindowFlags.AW_VER_POSITIVE;
            else flags |= AnimateWindowFlags.AW_VER_NEGATIVE;

            if (hide) flags |= AnimateWindowFlags.AW_HIDE;

            //Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

            try
            {
                //#warning this locking is still not correct
                //                lock (iconBox.Image)
                //                {
                AnimateWindow(Handle, AnimateTime, flags);
                //                }
            }
            catch (ArgumentException ae)
            {
                Debug.WriteLine("Got an exception: " + ae.Message);
            }
        }

        [DllImport("user32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        static extern bool AnimateWindow(IntPtr hwnd, int time, AnimateWindowFlags flags);

        void Collapse()
        {
            lock (openPopups)
            {
                var thisIndex = openPopups.IndexOf(this);

                for (var i = thisIndex - 1; i >= 0; i--)
                {
                    var popup = (PopupNotify)openPopups[i];

                    if (sysLoc == SystemTrayLocation.BottomLeft || sysLoc == SystemTrayLocation.BottomLeft) popup.Top += Height;
                    else popup.Top -= Height;
                }

                openPopups.RemoveAt(thisIndex);
            }
        }

        protected virtual void DrawBorder([NotNull] Graphics fx)
        {
            if (fx == null) throw new ArgumentNullException("fx");
            fx.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
        }


        protected Bitmap DrawCloseButton(CloseButtonState state)
        {
            if (VisualStyleRenderer.IsSupported) return DrawThemeCloseButton(state);
            else return DrawLegacyCloseButton(state);
        }

        /// <summary>
        /// Draw a Windows 95 style close button.
        /// </summary>
        protected Bitmap DrawLegacyCloseButton(CloseButtonState state)
        {
            var output = new Bitmap(closeButton.Width, closeButton.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (var fx = Graphics.FromImage(output))
                {
                    var rClose = new Rectangle(0, 0, closeButton.Width, closeButton.Height);

                    ButtonState bState;
                    if (state == CloseButtonState.Pushed) bState = ButtonState.Pushed;
                    else // the Windows 95 theme doesn't have a "hot" button
                        bState = ButtonState.Normal;
                    ControlPaint.DrawCaptionButton(fx, rClose, CaptionButton.Close, bState);

                    fx.DrawImage(output, rClose);
                }
            }
            catch
            {
                output.Dispose();
                throw;
            }

            return output;
        }

        /// <summary>
        /// Draw a Windows XP style close button.
        /// </summary>
        protected Bitmap DrawThemeCloseButton(CloseButtonState state)
        {
            var output = new Bitmap(closeButton.Width, closeButton.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (var fx = Graphics.FromImage(output))
                {
                    var rect = closeButton.ClientRectangle;

                    VisualStyleElement vse = null;

                    switch (state)
                    {
                        case CloseButtonState.Hot:
                            vse = VisualStyleElement.Window.CloseButton.Hot;
                            break;
                        case CloseButtonState.Normal:
                            vse = VisualStyleElement.Window.CloseButton.Normal;
                            break;
                        case CloseButtonState.Pushed:
                            vse = VisualStyleElement.Window.CloseButton.Pressed;
                            break;
                    }

                    var vsr = new VisualStyleRenderer(vse);
                    vsr.DrawBackground(fx, rect);
                }
            } 
            catch
            {
                output.Dispose();
                throw;
            }

            return output;
        }

        static SystemTrayLocation FindSystemTray(Rectangle rcWorkArea)
        {
            var appBarData = APPBARDATA.Create();
            if (SHAppBarMessage(ABM_GETTASKBARPOS, ref appBarData) != IntPtr.Zero)
            {
                var taskBarLocation = appBarData.rc;

                var TaskBarHeight = taskBarLocation.Bottom - taskBarLocation.Top;
                var TaskBarWidth = taskBarLocation.Right - taskBarLocation.Left;

                if (TaskBarHeight > TaskBarWidth)
                {
                    //	Taskbar is vertical
                    if (taskBarLocation.Right > rcWorkArea.Right) return SystemTrayLocation.BottomRight;
                    else return SystemTrayLocation.BottomLeft;
                }
                else
                {
                    //	Taskbar is horizontal
                    if (taskBarLocation.Bottom > rcWorkArea.Bottom) return SystemTrayLocation.BottomRight;
                    else return SystemTrayLocation.TopRight;
                }
            }
            else return SystemTrayLocation.BottomRight; //oh well, let's just go default
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        void InitializeComponent()
        {
            components = new Container();
            closeButton = new PictureBox();
            iconBox = new PictureBox();
            NotifyTitle = new Label();
            NotifyMessage = new Label();
            displayTimer = new Timer(components);
            ((ISupportInitialize)(closeButton)).BeginInit();
            ((ISupportInitialize)(iconBox)).BeginInit();
            SuspendLayout();
            // 
            // closeButton
            // 
            closeButton.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
            closeButton.BackColor = Color.Transparent;
            closeButton.Location = new Point(280, 8);
            closeButton.Name = "closeButton";
            closeButton.Size = new Size(16, 16);
            closeButton.TabIndex = 0;
            closeButton.TabStop = false;
            closeButton.MouseLeave += new EventHandler(closeButton_MouseLeave);
            closeButton.Click += new EventHandler(closeButton_Click);
            closeButton.MouseDown += new MouseEventHandler(closeButton_MouseDown);
            closeButton.MouseEnter += new EventHandler(closeButton_MouseEnter);
            // 
            // iconBox
            // 
            iconBox.BackColor = Color.Transparent;
            iconBox.Location = new Point(8, 8);
            iconBox.Name = "iconBox";
            iconBox.Size = new Size(50, 50);
            iconBox.SizeMode = PictureBoxSizeMode.StretchImage;
            iconBox.TabIndex = 1;
            iconBox.TabStop = false;
            // 
            // NotifyTitle
            // 
            NotifyTitle.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            NotifyTitle.AutoEllipsis = true;
            NotifyTitle.AutoSize = true;
            NotifyTitle.BackColor = Color.Transparent;
            NotifyTitle.ForeColor = Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(68)))), ((int)(((byte)(135)))));
            NotifyTitle.Location = new Point(72, 8);
            NotifyTitle.Name = "NotifyTitle";
            NotifyTitle.Size = new Size(23, 13);
            NotifyTitle.TabIndex = 2;
            NotifyTitle.Text = "title";
            // 
            // NotifyMessage
            // 
            NotifyMessage.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) | AnchorStyles.Left) | AnchorStyles.Right)));
            NotifyMessage.BackColor = Color.Transparent;
            NotifyMessage.Location = new Point(72, 40);
            NotifyMessage.Name = "NotifyMessage";
            NotifyMessage.Size = new Size(224, 10);
            NotifyMessage.TabIndex = 3;
            // 
            // displayTimer
            // 
            displayTimer.Tick += new EventHandler(displayTimer_Tick);
            // 
            // PopupNotify
            // 
            AutoScaleBaseSize = new Size(5, 13);
            ClientSize = new Size(304, 66);
            ControlBox = false;
            Controls.Add(NotifyMessage);
            Controls.Add(NotifyTitle);
            Controls.Add(iconBox);
            Controls.Add(closeButton);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PopupNotify";
            ShowInTaskbar = false;
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.Manual;
            Text = "PopupNotify";
            TopMost = true;
            Closing += new CancelEventHandler(PopupNotify_Closing);
            Load += new EventHandler(PopupNotify_Load);
            ((ISupportInitialize)(closeButton)).EndInit();
            ((ISupportInitialize)(iconBox)).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        void MakeRoom()
        {
            var spaceBetweenPopups = 1;
            lock (openPopups)
            {
                foreach (var popup in openPopups)
                {
                    if (sysLoc == SystemTrayLocation.BottomLeft || sysLoc == SystemTrayLocation.BottomRight) popup.Top -= Height + spaceBetweenPopups;
                    else popup.Top += Height + spaceBetweenPopups;
                }
            }
        }

        void Notify()
        {
            if (IconImage == null) iconBox.Visible = false;

            SetLayout();

            var rScreen = Screen.PrimaryScreen.WorkingArea;

            sysLoc = FindSystemTray(rScreen);

            if (sysLoc == SystemTrayLocation.BottomRight)
            {
                Top = rScreen.Bottom - Height;
                Left = rScreen.Right - Width;
                MakeRoom();
                SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE);
                AnimateWindow(false, false);
            }
            else if (sysLoc == SystemTrayLocation.TopRight)
            {
                Top = rScreen.Top;
                Left = rScreen.Right - Width;
                MakeRoom();
                SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE);
                AnimateWindow(true, false);
            }
            else if (sysLoc == SystemTrayLocation.BottomLeft)
            {
                Top = rScreen.Bottom - Height;
                Left = rScreen.Left;
                MakeRoom();
                SetWindowPos(Handle, HWND_TOPMOST, Left, Top, Width, Height, SWP_NOACTIVATE);
                AnimateWindow(false, false);
            }

            lock (openPopups)
            {
                openPopups.Add(this);
            }

            displayTimer.Interval = WaitTime;
            displayTimer.Start();
        }

        void SetLayout()
        {
            var padding = 8;
            var iconRightPadding = 0;
            var border = 1;

            iconBox.Left = padding + border;
            iconBox.Top = padding + border;
            iconBox.Width = IconWidth;
            iconBox.Height = IconHeight;

            Height = iconBox.Height + 2*padding + 2*border;

            closeButton.Left = Width - padding - border - closeButton.Width + 3;
            closeButton.Top = padding + border - 3;

            NotifyTitle.Top = iconBox.Top - 5; //fudge factor
            NotifyTitle.Left = iconBox.Right + iconRightPadding;

            NotifyMessage.Left = NotifyTitle.Left + 1; //fudgy
            NotifyMessage.Width = Width - NotifyMessage.Left - padding - border;
            NotifyMessage.Top = NotifyTitle.Bottom;
            NotifyMessage.Height = Height - NotifyMessage.Top - padding - border;
        }

        [DllImport("user32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, uint uFlags);

        // Shell32.dll
        [DllImport("shell32.dll")]
        static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, Int32 flags);

        void UnNotify()
        {
            if (sysLoc == SystemTrayLocation.BottomRight || sysLoc == SystemTrayLocation.BottomLeft) AnimateWindow(true, true);
            else if (sysLoc == SystemTrayLocation.TopRight) AnimateWindow(false, true);

            Close();
        }

        void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        void closeButton_MouseDown(object sender, MouseEventArgs e)
        {
            closeButton.Image = closeDown;
        }

        void closeButton_MouseEnter(object sender, EventArgs e)
        {
            closeButton.Image = closeHot;
        }

        void closeButton_MouseLeave(object sender, EventArgs e)
        {
            closeButton.Image = closeCold;
        }

        void displayTimer_Tick(object sender, EventArgs e)
        {
            if (WaitOnMouseOver && Bounds.Contains(Cursor.Position)) displayTimer.Interval = 1000; //try every second, now
            else
            {
                displayTimer.Stop();
                UnNotify();
            }
        }

        void PopupNotify_Closing(object sender, CancelEventArgs e)
        {
            Collapse();
        }

        void PopupNotify_Load(object sender, EventArgs e)
        {
            Notify();
        }

        [Flags]
        enum AnimateWindowFlags
        {
            AW_HOR_POSITIVE = 0x00000001,
            AW_HOR_NEGATIVE = 0x00000002,
            AW_VER_POSITIVE = 0x00000004,
            AW_VER_NEGATIVE = 0x00000008,
            AW_CENTER = 0x00000010,
            AW_HIDE = 0x00010000,
            AW_ACTIVATE = 0x00020000,
            AW_SLIDE = 0x00040000,
            AW_BLEND = 0x00080000
        }

        [StructLayout(LayoutKind.Sequential)]
        struct APPBARDATA
        {
            public static APPBARDATA Create()
            {
                var appBarData = new APPBARDATA();
                appBarData.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
                return appBarData;
            }

            int cbSize;
            IntPtr hWnd;
            uint uCallbackMessage;
            uint uEdge;
            public RECT rc;
            int lParam;
        }

        protected enum CloseButtonState
        {
            Normal,
            Hot,
            Pushed
        }

        [StructLayout(LayoutKind.Explicit)]
        struct RECT
        {
            [FieldOffset(0)]
            public Int32 Left;
            [FieldOffset(4)]
            public Int32 Top;
            [FieldOffset(8)]
            public Int32 Right;
            [FieldOffset(12)]
            public Int32 Bottom;

            public RECT(Rectangle bounds)
            {
                Left = bounds.Left;
                Top = bounds.Top;
                Right = bounds.Right;
                Bottom = bounds.Bottom;
            }

            public static implicit operator Rectangle(RECT rect)
            {
                return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            public static implicit operator RECT(Rectangle rect)
            {
                return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            public RECT(int left_, int top_, int right_, int bottom_)
            {
                Left = left_;
                Top = top_;
                Right = right_;
                Bottom = bottom_;
            }

            public int Height { get { return Bottom - Top + 1; } }
            public int Width { get { return Right - Left + 1; } }
            public Size Size { get { return new Size(Width, Height); } }

            public Point Location { get { return new Point(Left, Top); } }

            // Handy method for converting to a System.Drawing.Rectangle
            public Rectangle ToRectangle()
            {
                return Rectangle.FromLTRB(Left, Top, Right, Bottom);
            }

            public static RECT FromRectangle(Rectangle rectangle)
            {
                return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
            }

            public override int GetHashCode()
            {
                return Left ^ ((Top << 13) | (Top >> 0x13)) ^ ((Width << 0x1a) | (Width >> 6)) ^ ((Height << 7) | (Height >> 0x19));
            }
        }

        enum SystemTrayLocation
        {
            BottomLeft,
            BottomRight,
            TopRight
        } ;
    }
}