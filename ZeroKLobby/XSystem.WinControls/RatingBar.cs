using System;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace XSystem.WinControls
{
    public enum IconStyle
    {
        Star,
        Heart,
        Misc
    }
    public delegate void OnRateChanged(object sender, RatingBarRateEventArgs e);
    [DefaultEvent("RateChanged")]
    public class RatingBar : Control
    {
        public event OnRateChanged RateChanged;

        byte gap = 1;
        byte iconsCount = 10;
        float rate = 0;
        float tempRateHolder = 0;
        bool readOnly = false;
        bool rateOnce = false;
        bool isVoted = false;

        ContentAlignment alignment = ContentAlignment.MiddleCenter;
        Image iconEmpty;
        Image iconHalf;
        Image iconFull;
        IconStyle iconStyle = IconStyle.Star;
        PictureBox pb;

        public RatingBar()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            pb = new PictureBox();
            pb.BackgroundImageLayout = ImageLayout.None;
            pb.MouseMove += new MouseEventHandler(pb_MouseMove);
            pb.MouseLeave += new EventHandler(pb_MouseLeave);
            pb.MouseClick += new MouseEventHandler(pb_MouseClick);
            pb.Cursor = Cursors.Hand;
            pb.BackColor = Color.DarkGray;
            this.Controls.Add(pb);
            UpdateIcons();
            UpdateBarSize();
            #region --- Drawing Empty Icons ---
            Bitmap tb = new Bitmap(pb.Width, pb.Height);
            Graphics g = Graphics.FromImage(tb);
            DrawEmptyIcons(g, 0, iconsCount);
            g.Dispose();
            pb.BackgroundImage = tb;
            #endregion
        }

        #region --- Mouse Events ---
        void pb_MouseMove(object sender, MouseEventArgs e)
        {
            if (readOnly || (rateOnce && isVoted))
                return;
            Bitmap tb = new Bitmap(pb.Width, pb.Height);
            Graphics g = Graphics.FromImage(tb);
            int x = 0;
            float trate = 0;
            byte ticonsCount = iconsCount; // temporary variable to hold the iconsCount value, because we're decreasing it on each loop
            for (int a = 0; a < iconsCount; a++)
            {
                if (e.X > x && e.X <= x + iconEmpty.Width / 2)
                {
                    g.DrawImage(iconHalf, x, 0, iconEmpty.Width, iconEmpty.Height); // Draw icons with the dimension of iconEmpty, so that they do not look odd
                    x += gap + iconEmpty.Width;
                    trate += 0.5f;
                }
                else if (e.X > x)
                {
                    g.DrawImage(iconFull, x, 0, iconEmpty.Width, iconEmpty.Height); // Draw icons with the dimension of iconEmpty, so that they do not look odd
                    x += gap + iconEmpty.Width;
                    trate += 1.0f;
                }
                else
                    break;
                ticonsCount--;
            }
            tempRateHolder = trate;
            DrawEmptyIcons(g, x, ticonsCount);
            g.Dispose();
            pb.BackgroundImage = tb;

        }
        void pb_MouseLeave(object sender, EventArgs e)
        {
            if (readOnly || (rateOnce && isVoted))
                return;
            Bitmap tb = new Bitmap(pb.Width, pb.Height);
            Graphics g = Graphics.FromImage(tb);
            int x = 0;
            byte ticonsCount = iconsCount;
            float trate = rate;
            while (trate > 0)
            {
                if (trate > 0.5f)
                {
                    g.DrawImage(iconFull, x, 0, iconEmpty.Width, iconEmpty.Height); // Draw icons with the dimension of iconEmpty, so that they do not look odd
                    x += gap + iconEmpty.Width;
                }
                else if (trate == 0.5f)
                {
                    g.DrawImage(iconHalf, x, 0, iconEmpty.Width, iconEmpty.Height); // Draw icons with the dimension of iconEmpty, so that they do not look odd
                    x += gap + iconEmpty.Width;
                }
                else
                    break;
                ticonsCount--;
                trate--;
            }
            DrawEmptyIcons(g, x, ticonsCount);

            g.Dispose();
            pb.BackgroundImage = tb;
        }
        void pb_MouseClick(object sender, MouseEventArgs e)
        {
            //route mouse click to main control - line addded by vbs
            this.OnClick(e);

            float toldRate = rate;
            rate = tempRateHolder;
            isVoted = true;
            if (rateOnce)
                pb.Cursor = Cursors.Default;
            OnRateChanged(new RatingBarRateEventArgs(rate, toldRate));
        }
        #endregion
        #region --- Override Functions ---
        protected override void OnResize(EventArgs e)
        {
            UpdateBarLocation();
            base.OnResize(e);
        }
        #endregion
        #region --- Custom Functions ---
        void DrawIcons()
        {
            Bitmap tb = new Bitmap(pb.Width, pb.Height);
            Graphics g = Graphics.FromImage(tb);
            int x = 0;
            byte ticonsCount = iconsCount;
            float trate = rate;
            while (trate > 0)
            {
                if (trate > 0.5f)
                {
                    g.DrawImage(iconFull, x, 0, iconEmpty.Width, iconEmpty.Height); // Draw icons with the dimension of iconEmpty, so that they do not look odd
                    x += gap + iconEmpty.Width;
                }
                else if (trate == 0.5f)
                {
                    g.DrawImage(iconHalf, x, 0, iconEmpty.Width, iconEmpty.Height); // Draw icons with the dimension of iconEmpty, so that they do not look odd
                    x += gap + iconEmpty.Width;
                }
                else
                    break;
                ticonsCount--;
                trate--;
            }
            DrawEmptyIcons(g, x, ticonsCount);

            g.Dispose();
            pb.BackgroundImage = tb;
        }
        void DrawEmptyIcons(Graphics g, int x, byte count)
        {
            for (byte a = 0; a < count; a++)
            {
                g.DrawImage(iconEmpty, x, 0);
                x += gap + iconEmpty.Width;
            }
        }
        void UpdateBarSize()
        {
            pb.Size = new Size((iconEmpty.Width * iconsCount) + (gap * (iconsCount - 1)), iconEmpty.Height); // Last icon wont need gap, therefore we need to use -1
            UpdateBarLocation();
        }
        void UpdateBarLocation()
        {
            if (alignment == ContentAlignment.TopLeft) { }// Leave it blank, Since we're calling this from Resize Event then we dont need to set same point again and again
            else if (alignment == ContentAlignment.TopRight)
                pb.Location = new Point(this.Width - pb.Width, 0);
            else if (alignment == ContentAlignment.TopCenter)
                pb.Location = new Point(this.Width / 2 - pb.Width / 2, 0);
            else if (alignment == ContentAlignment.BottomLeft)
                pb.Location = new Point(0, this.Height - pb.Height);
            else if (alignment == ContentAlignment.BottomRight)
                pb.Location = new Point(this.Width - pb.Width, this.Height - pb.Height);
            else if (alignment == ContentAlignment.BottomCenter)
                pb.Location = new Point(this.Width / 2 - pb.Width / 2, this.Height - pb.Height);
            else if (alignment == ContentAlignment.MiddleLeft)
                pb.Location = new Point(0, this.Height / 2 - pb.Height / 2);
            else if (alignment == ContentAlignment.MiddleRight)
                pb.Location = new Point(this.Width - pb.Width, this.Height / 2 - pb.Height / 2);
            else if (alignment == ContentAlignment.MiddleCenter)
                pb.Location = new Point(this.Width / 2 - pb.Width / 2, this.Height / 2 - pb.Height / 2);
        }
        void UpdateIcons()
        {
            if (iconStyle == IconStyle.Star)
            {
                iconEmpty = Properties.Resources.iconStarEmpty;
                iconFull = Properties.Resources.iconStarFull;
                iconHalf = Properties.Resources.iconStartHalf;
            }
            else if (iconStyle == IconStyle.Heart)
            {
                iconEmpty = Properties.Resources.iconHeartEmpty;
                iconFull = Properties.Resources.iconHeartFull;
                iconHalf = Properties.Resources.iconHeartHalf;
            }
            else if (iconStyle == IconStyle.Misc)
            {
                iconEmpty = Properties.Resources.iconMiscEmpty;
                iconFull = Properties.Resources.iconMiscFull;
                iconHalf = Properties.Resources.iconMiscHalf;
            }
        }
        #endregion
        #region --- Virtual Functions ---
        protected virtual void OnRateChanged(RatingBarRateEventArgs e)
        {
            if (RateChanged != null && e.NewRate != e.OldRate)
                RateChanged(this, e);
        }
        #endregion
        #region --- Properties ---
        public byte Gap
        {
            get { return gap; }
            set { gap = value; }
        }
        public byte IconsCount
        {
            get { return iconsCount; }
            set { if (value <= 10) { iconsCount = value; UpdateBarSize(); } }
        }
        [DefaultValue(typeof(ContentAlignment), "middlecenter")]
        public ContentAlignment Alignment
        {
            get { return alignment; }
            set
            {
                alignment = value;
                if (value == ContentAlignment.TopLeft)
                    pb.Location = new Point(0, 0);
                else UpdateBarLocation();
            }
        }

        public Image IconEmpty
        {
            get { return iconEmpty; }
            set { iconEmpty = value; }
        }
        public Image IconHalf
        {
            get { return iconHalf; }
            set { iconHalf = value; }
        }
        public Image IconFull
        {
            get { return iconFull; }
            set { iconFull = value; }
        }

        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return readOnly; }
            set { readOnly = value; if (value)pb.Cursor = Cursors.Default; else pb.Cursor = Cursors.Hand; }
        }
        [DefaultValue(false)]
        public bool RateOnce
        {
            get { return rateOnce; }
            set { rateOnce = value; if (!value) pb.Cursor = Cursors.Hand; /* Set hand cursor, if false is set from true*/ }
        }
        [Browsable(false)]
        public float Rate
        {
            get { return rate; }
            set
            {
                if (value >= 0 && value <= (float)iconsCount)
                {
                    float toldRate = rate;
                    rate = value;
                    DrawIcons();
                    OnRateChanged(new RatingBarRateEventArgs(value, toldRate));
                }
                else
                    throw new ArgumentOutOfRangeException("Rate", "Value '" + value + "' is not valid for 'Rate'. Value must be Non-negative and less than or equals to '" + iconsCount + "'");
            }
        }

        public Color BarBackColor
        {
            get { return pb.BackColor; }
            set { pb.BackColor = value; }
        }

        [DefaultValue(typeof(IconStyle), "star")]
        public IconStyle IconStyle
        {
            get { return iconStyle; }
            set { iconStyle = value; UpdateIcons(); }
        }
        
        #endregion

        //vbs: bahh its a bad workaround. i could not get the tooltip to show up for child controls (the picture box in this case)
        public void setToolTipForStars(ToolTip tt, string text)
        {
            tt.SetToolTip(pb, text);
        }
    }
    public class RatingBarRateEventArgs : EventArgs
    {
        public float NewRate;
        public float OldRate;
        public RatingBarRateEventArgs(float newRate, float oldRate)
        {
            NewRate = newRate;
            OldRate = oldRate;
        }
    }
}
