using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
namespace ZeroKLobby
{
    /// <summary>
    /// possible button states
    /// </summary>
    public enum BtnState
    {
        /// <summary>
        /// The button is disabled.
        /// </summary>		
        Inactive = 0,
        /// <summary>
        /// The button is in it normal unpressed state
        /// </summary>
        Normal = 1,
        /// <summary>
        /// The location of the mouse is over the button
        /// </summary>
        MouseOver = 2,
        /// <summary>
        /// The button is currently being pressed
        /// </summary>
        Pushed = 3,
    }
    /// <summary>
    /// The purpose of this class is to continue to provide the regular functionality of button class with
    /// some additional bitmap enhancments. These enhancements allow the specification of bitmaps for each 
    /// state of the button. In addition, it makes use of the alignment properties already provided by the 
    /// base button class. Since this class renders the image, it should appear similar accross platforms.	
    /// </summary>
    public class BitmapButton : System.Windows.Forms.Button
    {
        #region Private Variables

        private System.Drawing.Image _ImageNormal = null;
        private System.Drawing.Image _ImageFocused = null;
        private System.Drawing.Image _ImagePressed = null;
        private System.Drawing.Image _ImageMouseOver = null;
        private System.Drawing.Image _ImageInactive = null;
        private System.Drawing.Color _BorderColor = System.Drawing.Color.DarkBlue;
        private System.Drawing.Color _InnerBorderColor = System.Drawing.Color.LightGray;
        private System.Drawing.Color _InnerBorderColor_Focus = System.Drawing.Color.LightBlue;
        private System.Drawing.Color _InnerBorderColor_MouseOver = System.Drawing.Color.Gold;
        private System.Drawing.Color _ImageBorderColor = System.Drawing.Color.Chocolate;
        private bool _StretchImage = false;
        private bool _TextDropShadow = true;
        private int _Padding = 5;
        private bool _OffsetPressedContent = true;
        private bool _ImageBorderEnabled = true;
        private bool _ImageDropShadow = true;
        private bool _FocusRectangleEnabled = true;
        private BtnState btnState = BtnState.Normal;
        private bool CapturingMouse = false;

        #endregion
        #region Public Properties
        [Browsable(false)]
        new public System.Drawing.Image Image
        {
            get { return base.Image; }
            set { base.Image = value; }
        }
        [Browsable(false)]
        new public System.Windows.Forms.ImageList ImageList
        {
            get { return base.ImageList; }
            set { base.ImageList = value; }
        }
        [Browsable(false)]
        new public int ImageIndex
        {
            get { return base.ImageIndex; }
            set { base.ImageIndex = value; }
        }
        /// <summary>
        /// Enable the shadowing of the button text
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("enables the text to cast a shadow"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public bool TextDropShadow
        {
            get { return _TextDropShadow; }
            set { _TextDropShadow = value; }
        }
        /// <summary>
        /// Enables the dashed focus rectangle
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("enables the focus rectangle"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public bool FocusRectangleEnabled
        {
            get { return _FocusRectangleEnabled; }
            set { _FocusRectangleEnabled = value; }
        }

        /// <summary>
        /// Enable the shadowing of the image in the button
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("enables the image to cast a shadow"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public bool ImageDropShadow
        {
            get { return _ImageDropShadow; }
            set { _ImageDropShadow = value; }
        }
        /// <summary>
        /// This specifies the color of image border. Note, this is only valid if ImageBorder is enabled.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Color of the border around the image"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Color ImageBorderColor
        {
            get { return _ImageBorderColor; }
            set { _ImageBorderColor = value; }
        }
        /// <summary>
        /// This enables/disables the bordering of the image.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Enables the bordering of the image"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public bool ImageBorderEnabled
        {
            get { return _ImageBorderEnabled; }
            set { _ImageBorderEnabled = value; }
        }
        /// <summary>
        /// Color of the border around the button
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Color of the border around the button"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Color BorderColor
        {
            get { return _BorderColor; }
            set { _BorderColor = value; }
        }
        /// <summary>
        /// Color of the inner border when the button has focus
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Color of the inner border when the button has focus"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Color InnerBorderColor_Focus
        {
            get { return _InnerBorderColor_Focus; }
            set { _InnerBorderColor_Focus = value; }
        }
        /// <summary>
        /// Color of the inner border when the button does not have focus
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Color of the inner border when the button does not hvae focus"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Color InnerBorderColor
        {
            get { return _InnerBorderColor; }
            set { _InnerBorderColor = value; }
        }
        /// <summary>
        /// Color of the inner border when the mouse is over the button.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("color of the inner border when the mouse is over the button"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Color InnerBorderColor_MouseOver
        {
            get { return _InnerBorderColor_MouseOver; }
            set { _InnerBorderColor_MouseOver = value; }
        }
        /// <summary>
        /// Stretches the image across the button
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("stretch the impage to the size of the button"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public bool StretchImage
        {
            get { return _StretchImage; }
            set { _StretchImage = value; }
        }
        /// <summary>
        /// Specifies the padding in units of pixels around the button button elements. Currently,
        /// the button elements consist of the image and text.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("padded pixels around the image and text"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public int Padding
        {
            get { return _Padding; }
            set { _Padding = value; }
        }
        /// <summary>
        /// Set to true if to offset button elements when button is pressed
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Set to true if to offset image/text when button is pressed"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public bool OffsetPressedContent
        {
            get
            {
                return
                    _OffsetPressedContent;
            }
            set { _OffsetPressedContent = value; }
        }
        /// <summary>
        /// Image to be displayed while the button state is in normal state. If the other
        /// states do not specify an image, this image is used as a substitute.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Image to be displayed while the button state is in normal state"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Image ImageNormal
        {
            get { return _ImageNormal; }
            set { _ImageNormal = value; }
        }
        /// <summary>
        /// Specifies an image to use while the button has focus.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Image to be displayed while the button has focus"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Image ImageFocused
        {
            get { return _ImageFocused; }
            set { _ImageFocused = value; }
        }
        /// <summary>
        /// Specifies an image to use while the button is enactive.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Image to be displayed while the button is inactive"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Image ImageInactive
        {
            get { return _ImageInactive; }
            set { _ImageInactive = value; }
        }
        /// <summary>
        /// Specifies an image to use while the button is pressed.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Image to be displayed while the button state is pressed"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Image ImagePressed
        {
            get { return _ImagePressed; }
            set { _ImagePressed = value; }
        }
        /// <summary>
        /// Specifies an image to use while the mouse is over the button.
        /// </summary>
        [Browsable(true),
        CategoryAttribute("Appearance"),
        Description("Image to be displayed while the button state is MouseOver"),
        System.ComponentModel.RefreshProperties(RefreshProperties.Repaint)
        ]
        public System.Drawing.Image ImageMouseOver
        {
            get { return _ImageMouseOver; }
            set { _ImageMouseOver = value; }
        }
        #endregion
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        /// <summary>
        /// The BitmapButton constructor
        /// </summary>
        public BitmapButton()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            // TODO: Add any initialization after the InitComponent call			
            //LoadGraphics();
        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion


        #region Paint Methods
        /// <summary>
        /// This method paints the button in its entirety.
        /// </summary>
        /// <param name="e">paint arguments use to paint the button</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            CreateRegion(0);
            paint_Background(e);
            paint_Text(e);
            paint_Image(e);
            paint_Border(e);
            paint_InnerBorder(e);
            paint_FocusBorder(e);
        }
        /// <summary>
        /// This method fills the background of the button.
        /// </summary>
        /// <param name="e">paint arguments use to paint the button</param>
        private void paint_Background(PaintEventArgs e)
        {
            if (e == null)
                return;
            if (e.Graphics == null)
                return;

            Graphics g = e.Graphics;
            System.Drawing.Rectangle rect = new Rectangle(0, 0, Size.Width, Size.Height);
            //
            // get color of background
            //			
            System.Drawing.Color color = this.BackColor; ;
            if (btnState == BtnState.Inactive)
                color = System.Drawing.Color.LightGray;
            //
            // intialize ColorArray and Positions Array
            //
            Color[] ColorArray = null;
            float[] PositionArray = null;
            //
            // initialize color array for a button that is pushed
            //			
            if (btnState == BtnState.Pushed)
            {
                ColorArray = new Color[]{
									 Blend(this.BackColor,System.Drawing.Color.White,80),
									 Blend(this.BackColor,System.Drawing.Color.White,40),
									 Blend(this.BackColor,System.Drawing.Color.Black,0),
									 Blend(this.BackColor,System.Drawing.Color.Black,0),
									 Blend(this.BackColor,System.Drawing.Color.White,40),
									 Blend(this.BackColor,System.Drawing.Color.White,80),								 
				};
                PositionArray = new float[] { 0.0f, .05f, .40f, .60f, .95f, 1.0f };
            }
            //
            // else, initialize color array for a button that is normal or disabled
            //			
            else
            {
                ColorArray = new Color[]{
										 Blend(color,System.Drawing.Color.White,80),
										 Blend(color,System.Drawing.Color.White,90),
										 Blend(color,System.Drawing.Color.White,30),
										 Blend(color,System.Drawing.Color.White,00),               
										 Blend(color,System.Drawing.Color.Black,30),
										 Blend(color,System.Drawing.Color.Black,20),
				};
                PositionArray = new float[] { 0.0f, .15f, .40f, .65f, .80f, 1.0f };
            }
            //
            // create blend variable for the interpolate the colors
            //
            System.Drawing.Drawing2D.ColorBlend blend = new System.Drawing.Drawing2D.ColorBlend();
            blend.Colors = ColorArray;
            blend.Positions = PositionArray;
            //
            // create vertical gradient brush
            //
            System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, this.BackColor, Blend(this.BackColor, this.BackColor, 10), System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            brush.InterpolationColors = blend;
            //
            // fill the rectangle
            //
            g.FillRectangle(brush, rect);
            //
            // release resources
            //
            brush.Dispose();
        }
        /// <summary>
        /// paints the 1 pixel border around the button. The color of
        /// the border is defined by BorderColor
        /// </summary>
        /// <param name="e">paint arguments use to paint the button</param>
        private void paint_Border(PaintEventArgs e)
        {
            if (e == null)
                return;
            if (e.Graphics == null)
                return;
            //
            // create the pen
            //
            Pen pen = new Pen(this.BorderColor, 1);
            //
            // get the points for the border
            //
            Point[] pts = border_Get(0, 0, this.Width - 1, this.Height - 1);
            //
            // paint the border
            //
            e.Graphics.DrawLines(pen, pts);
            //
            // release resources
            //
            pen.Dispose();
        }
        /// <summary>
        /// paints the focus rectangle. 
        /// </summary>
        /// <param name="e">paint arguments use to paint the button</param>		 
        private void paint_FocusBorder(PaintEventArgs e)
        {
            if (e == null)
                return;
            if (e.Graphics == null)
                return;
            //
            // if the button has focus, and focus rectangle is enabled,
            // draw the focus box
            //
            if (this.Focused)
            {
                if (FocusRectangleEnabled)
                {
                    ControlPaint.DrawFocusRectangle(e.Graphics, new Rectangle(3, 3, this.Width - 6, this.Height - 6), System.Drawing.Color.Black, this.BackColor);
                }
            }
        }
        /// <summary>
        /// paint the inner border of the button.
        /// </summary>
        /// <param name="e">paint arguments use to paint the button</param>		 
        private void paint_InnerBorder(PaintEventArgs e)
        {
            if (e == null)
                return;
            if (e.Graphics == null)
                return;

            Graphics g = e.Graphics;
            System.Drawing.Rectangle rect = new Rectangle(0, 0, Size.Width, Size.Height);

            System.Drawing.Color color = this.BackColor;
            //
            // get color of inner border
            //
            switch (btnState)
            {
                case BtnState.Inactive:
                    color = System.Drawing.Color.Gray;
                    break;
                case BtnState.Normal:
                    if (this.Focused)
                        color = this.InnerBorderColor_Focus;
                    else
                        color = this.InnerBorderColor;
                    break;
                case BtnState.Pushed:
                    color = Blend(InnerBorderColor_Focus, System.Drawing.Color.Black, 10);
                    break;
                case BtnState.MouseOver:
                    color = InnerBorderColor_MouseOver;
                    break;
            }
            //
            // populate color and position arrays
            //			
            Color[] ColorArray = null;
            float[] PositionArray = null;
            if (btnState == BtnState.Pushed)
            {
                ColorArray = new System.Drawing.Color[] {
									   Blend(color,System.Drawing.Color.Black,20),
                                       Blend(color,System.Drawing.Color.Black,10),
                                       Blend(color,System.Drawing.Color.White,00),               
                                       Blend(color,System.Drawing.Color.White,50),
									   Blend(color,System.Drawing.Color.White,85),
									   Blend(color,System.Drawing.Color.White,90),
				};
                PositionArray = new float[] { 0.0f, .20f, .50f, .60f, .90f, 1.0f };
            }
            else
            {
                ColorArray = new System.Drawing.Color[] {
									   Blend(color,System.Drawing.Color.White,80),
									   Blend(color,System.Drawing.Color.White,60),
									   Blend(color,System.Drawing.Color.White,10),
									   Blend(color,System.Drawing.Color.White,00),               
									   Blend(color,System.Drawing.Color.Black,20),
									   Blend(color,System.Drawing.Color.Black,50),
				};
                PositionArray = new float[] { 0.0f, .20f, .50f, .60f, .90f, 1.0f };
            }
            //
            // create blend object
            //
            System.Drawing.Drawing2D.ColorBlend blend = new System.Drawing.Drawing2D.ColorBlend();
            blend.Colors = ColorArray;
            blend.Positions = PositionArray;
            //
            // create gradient brush and pen
            //
            System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, this.BackColor, Blend(this.BackColor, this.BackColor, 10), System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            brush.InterpolationColors = blend;
            System.Drawing.Pen pen0 = new System.Drawing.Pen(brush, 1);
            //
            // get points array to draw the line
            //
            Point[] pts = border_Get(0, 0, this.Width - 1, this.Height - 1);
            //
            // draw line 0
            //
            this.border_Contract(1, ref pts);
            e.Graphics.DrawLines(pen0, pts);
            //
            // draw line 1
            //			
            this.border_Contract(1, ref pts);
            e.Graphics.DrawLines(pen0, pts);
            //
            // release resources
            //
            pen0.Dispose();
            brush.Dispose();
        }
        /// <summary>
        /// This method paints the text and text shadow for the button.
        /// </summary>
        /// <param name="e">paint arguments use to paint the button</param>		 
        private void paint_Text(PaintEventArgs e)
        {
            if (e == null)
                return;
            if (e.Graphics == null)
                return;
            System.Drawing.Rectangle rect = GetTextDestinationRect();
            //
            // do offset if button is pushed
            //
            if ((btnState == BtnState.Pushed) && (OffsetPressedContent))
                rect.Offset(1, 1);
            //
            // caculate bounding rectagle for the text
            //
            System.Drawing.SizeF size = txt_Size(e.Graphics, this.Text, this.Font);
            //
            // calculate the starting location to paint the text
            //
            System.Drawing.Point pt = Calculate_LeftEdgeTopEdge(this.TextAlign, rect, (int)size.Width, (int)size.Height);
            //
            // If button state is inactive, paint the inactive text
            //
            if (btnState == BtnState.Inactive)
            {
                e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(System.Drawing.Color.White), pt.X + 1, pt.Y + 1);
                e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(System.Drawing.Color.FromArgb(50, 50, 50)), pt.X, pt.Y);
            }
            //
            // else, paint the text and text shadow
            //
            else
            {
                //
                // paint text shadow
                //
                if (TextDropShadow)
                {
                    System.Drawing.Brush TransparentBrush0 = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(50, System.Drawing.Color.Black));
                    System.Drawing.Brush TransparentBrush1 = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(20, System.Drawing.Color.Black));

                    e.Graphics.DrawString(this.Text, this.Font, TransparentBrush0, pt.X, pt.Y + 1);
                    e.Graphics.DrawString(this.Text, this.Font, TransparentBrush0, pt.X + 1, pt.Y);

                    e.Graphics.DrawString(this.Text, this.Font, TransparentBrush1, pt.X + 1, pt.Y + 1);
                    e.Graphics.DrawString(this.Text, this.Font, TransparentBrush1, pt.X, pt.Y + 2);
                    e.Graphics.DrawString(this.Text, this.Font, TransparentBrush1, pt.X + 2, pt.Y);

                    TransparentBrush0.Dispose();
                    TransparentBrush1.Dispose();
                }
                //
                // paint text
                //
                e.Graphics.DrawString(this.Text, this.Font, new SolidBrush(this.ForeColor), pt.X, pt.Y);
            }
        }
        /// <summary>
        /// Paints a border around the image. If Image drop shadow is enabled,
        /// a shodow is drawn too.
        /// </summary>
        /// <param name="g">The graphics object</param>
        /// <param name="ImageRect">the rectangle region of the image</param>
        private void paint_ImageBorder(System.Drawing.Graphics g, System.Drawing.Rectangle ImageRect)
        {
            System.Drawing.Rectangle rect = ImageRect;

            //
            // If ImageDropShadow = true, draw shadow
            //
            if (ImageDropShadow)
            {
                System.Drawing.Pen p0 = new System.Drawing.Pen(System.Drawing.Color.FromArgb(80, 0, 0, 0));
                System.Drawing.Pen p1 = new System.Drawing.Pen(System.Drawing.Color.FromArgb(40, 0, 0, 0));
                g.DrawLine(p0, new Point(rect.Right, rect.Bottom), new Point(rect.Right + 1, rect.Bottom));
                g.DrawLine(p0, new Point(rect.Right + 1, rect.Top + 1), new Point(rect.Right + 1, rect.Bottom + 0));
                g.DrawLine(p1, new Point(rect.Right + 2, rect.Top + 2), new Point(rect.Right + 2, rect.Bottom + 1));
                g.DrawLine(p0, new Point(rect.Left + 1, rect.Bottom + 1), new Point(rect.Right + 0, rect.Bottom + 1));
                g.DrawLine(p1, new Point(rect.Left + 1, rect.Bottom + 2), new Point(rect.Right + 1, rect.Bottom + 2));
            }
            //
            // Draw Image Border
            //
            if (ImageBorderEnabled)
            {
                Color[] ColorArray = null;
                float[] PositionArray = null;
                System.Drawing.Color color = this.ImageBorderColor;
                if (!this.Enabled)
                    color = System.Drawing.Color.LightGray;
                //
                // initialize color and position array
                //
                ColorArray = new Color[]{
											Blend(color,System.Drawing.Color.White,40),
											Blend(color,System.Drawing.Color.White,20),
											Blend(color,System.Drawing.Color.White,30),
											Blend(color,System.Drawing.Color.White,00),               
											Blend(color,System.Drawing.Color.Black,30),
											Blend(color,System.Drawing.Color.Black,70),
				};
                PositionArray = new float[] { 0.0f, .20f, .50f, .60f, .90f, 1.0f };
                //
                // create blend object
                //
                System.Drawing.Drawing2D.ColorBlend blend = new System.Drawing.Drawing2D.ColorBlend();
                blend.Colors = ColorArray;
                blend.Positions = PositionArray;
                //
                // create brush and pens
                //
                System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, this.BackColor, Blend(this.BackColor, this.BackColor, 10), System.Drawing.Drawing2D.LinearGradientMode.Vertical);
                brush.InterpolationColors = blend;
                System.Drawing.Pen pen0 = new System.Drawing.Pen(brush, 1);
                System.Drawing.Pen pen1 = new System.Drawing.Pen(System.Drawing.Color.Black);
                //
                // calculate points to draw line
                //
                rect.Inflate(1, 1);
                Point[] pts = border_Get(rect.Left, rect.Top, rect.Width, rect.Height);
                this.border_Contract(1, ref pts);
                //
                // draw line 0
                //
                g.DrawLines(pen1, pts);
                //
                // draw line 1
                //
                this.border_Contract(1, ref pts);
                g.DrawLines(pen0, pts);
                //
                // release resources
                //
                pen1.Dispose();
                pen0.Dispose();
                brush.Dispose();
            }
        }
        /// <summary>
        /// Paints the image on the button.
        /// </summary>
        /// <param name="e"></param>

        private void paint_Image(PaintEventArgs e)
        {

            if (e == null)
                return;
            if (e.Graphics == null)
                return;
            Image image = GetCurrentImage(btnState);

            if (image != null)
            {
                Graphics g = e.Graphics;
                System.Drawing.Rectangle rect = GetImageDestinationRect();

                if ((btnState == BtnState.Pushed) && (_OffsetPressedContent))
                    rect.Offset(1, 1);
                if (this.StretchImage)
                {
                    g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                }
                else
                {
                    System.Drawing.Rectangle r = GetImageDestinationRect();
                    //g.DrawImage(image,rect.Left,rect.Top);
                    g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                }
                paint_ImageBorder(g, rect);
            }
        }
        #endregion
        #region Helper Methods
        /// <summary>
        /// Calculates the required size to draw a text string
        /// </summary>
        /// <param name="g">the graphics object</param>
        /// <param name="strText">string to calculate text region</param>
        /// <param name="font">font to use for the string</param>
        /// <returns>returns the size required to draw a text string</returns>
        private System.Drawing.SizeF txt_Size(Graphics g, string strText, Font font)
        {
            System.Drawing.SizeF size = g.MeasureString(strText, font);
            return (size);
        }
        /// <summary>
        /// Calculates the rectangular region used for text display.
        /// </summary>
        /// <returns>returns the rectangular region for the text display</returns>
        private System.Drawing.Rectangle GetTextDestinationRect()
        {
            System.Drawing.Rectangle ImageRect = GetImageDestinationRect();
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, 0, 0);
            switch (this.ImageAlign)
            {
                case System.Drawing.ContentAlignment.BottomCenter:
                    rect = new System.Drawing.Rectangle(0, 0, this.Width, ImageRect.Top);
                    break;
                case System.Drawing.ContentAlignment.BottomLeft:
                    rect = new System.Drawing.Rectangle(0, 0, this.Width, ImageRect.Top);
                    break;
                case System.Drawing.ContentAlignment.BottomRight:
                    rect = new System.Drawing.Rectangle(0, 0, this.Width, ImageRect.Top);
                    break;
                case System.Drawing.ContentAlignment.MiddleCenter:
                    rect = new System.Drawing.Rectangle(0, ImageRect.Bottom, this.Width, this.Height - ImageRect.Bottom);
                    break;
                case System.Drawing.ContentAlignment.MiddleLeft:
                    rect = new System.Drawing.Rectangle(ImageRect.Right, 0, this.Width - ImageRect.Right, this.Height);
                    break;
                case System.Drawing.ContentAlignment.MiddleRight:
                    rect = new System.Drawing.Rectangle(0, 0, ImageRect.Left, this.Height);
                    break;
                case System.Drawing.ContentAlignment.TopCenter:
                    rect = new System.Drawing.Rectangle(0, ImageRect.Bottom, this.Width, this.Height - ImageRect.Bottom);
                    break;
                case System.Drawing.ContentAlignment.TopLeft:
                    rect = new System.Drawing.Rectangle(0, ImageRect.Bottom, this.Width, this.Height - ImageRect.Bottom);
                    break;
                case System.Drawing.ContentAlignment.TopRight:
                    rect = new System.Drawing.Rectangle(0, ImageRect.Bottom, this.Width, this.Height - ImageRect.Bottom);
                    break;
            }
            rect.Inflate(-this.Padding, -this.Padding);
            return (rect);
        }
        /// <summary>
        /// Calculates the rectangular region used for image display.
        /// </summary>
        /// <returns>returns the rectangular region used to display the image</returns>
        private System.Drawing.Rectangle GetImageDestinationRect()
        {
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, 0, 0);
            System.Drawing.Image image = GetCurrentImage(this.btnState);
            if (image != null)
            {
                if (this.StretchImage)
                {
                    rect.Width = this.Width;
                    rect.Height = this.Height;
                }
                else
                {
                    rect.Width = image.Width;
                    rect.Height = image.Height;
                    System.Drawing.Rectangle drect = new System.Drawing.Rectangle(0, 0, this.Width, this.Height);
                    drect.Inflate(-this.Padding, -this.Padding);
                    System.Drawing.Point pt = Calculate_LeftEdgeTopEdge(this.ImageAlign, drect, image.Width, image.Height);
                    rect.Offset(pt);
                }
            }
            return (rect);
        }
        /// <summary>
        /// This method is used to retrieve the image used by the button for the given state.
        /// </summary>
        /// <param name="btnState">holds the state of the button</param>
        /// <returns>returns the button Image</returns>
        private System.Drawing.Image GetCurrentImage(BtnState btnState)
        {
            System.Drawing.Image image = ImageNormal;
            switch (btnState)
            {
                case BtnState.Normal:
                    if (this.Focused)
                    {
                        if (this.ImageFocused != null)
                            image = this.ImageFocused;
                    }
                    break;
                case BtnState.MouseOver:
                    if (ImageMouseOver != null)
                        image = ImageMouseOver;
                    break;
                case BtnState.Pushed:
                    if (ImagePressed != null)
                        image = ImagePressed;
                    break;
                case BtnState.Inactive:
                    if (ImageInactive != null)
                        image = ImageInactive;
                    else
                    {
                        if (image != null)
                        {
                            ImageInactive = ConvertToGrayscale(new Bitmap(ImageNormal));
                        }
                        image = ImageNormal;
                    }
                    break;
            }
            return (image);
        }
        /// <summary>
        /// converts a bitmap image to grayscale
        /// </summary>
        /// <param name="source">bitmap source</param>
        /// <returns>returns a grayscaled bitmap</returns>
        public Bitmap ConvertToGrayscale(Bitmap source)
        {
            Bitmap bm = new Bitmap(source.Width, source.Height);
            for (int y = 0; y < bm.Height; y++)
            {
                for (int x = 0; x < bm.Width; x++)
                {
                    Color c = source.GetPixel(x, y);
                    int luma = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    bm.SetPixel(x, y, Color.FromArgb(luma, luma, luma));
                }
            }
            return bm;
        }
        /// <summary>
        /// calculates the left/top edge for content.
        /// </summary>
        /// <param name="Alignment">the alignment of the content</param>
        /// <param name="rect">rectagular region to place content</param>
        /// <param name="nWidth">with of content</param>
        /// <param name="nHeight">height of content</param>
        /// <returns>returns the left/top edge to place content</returns>
        private System.Drawing.Point Calculate_LeftEdgeTopEdge(System.Drawing.ContentAlignment Alignment, System.Drawing.Rectangle rect, int nWidth, int nHeight)
        {
            System.Drawing.Point pt = new System.Drawing.Point(0, 0);
            switch (Alignment)
            {
                case System.Drawing.ContentAlignment.BottomCenter:
                    pt.X = (rect.Width - nWidth) / 2;
                    pt.Y = rect.Height - nHeight;
                    break;
                case System.Drawing.ContentAlignment.BottomLeft:
                    pt.X = 0;
                    pt.Y = rect.Height - nHeight;
                    break;
                case System.Drawing.ContentAlignment.BottomRight:
                    pt.X = rect.Width - nWidth;
                    pt.Y = rect.Height - nHeight;
                    break;
                case System.Drawing.ContentAlignment.MiddleCenter:
                    pt.X = (rect.Width - nWidth) / 2;
                    pt.Y = (rect.Height - nHeight) / 2;
                    break;
                case System.Drawing.ContentAlignment.MiddleLeft:
                    pt.X = 0;
                    pt.Y = (rect.Height - nHeight) / 2;
                    break;
                case System.Drawing.ContentAlignment.MiddleRight:
                    pt.X = rect.Width - nWidth;
                    pt.Y = (rect.Height - nHeight) / 2;
                    break;
                case System.Drawing.ContentAlignment.TopCenter:
                    pt.X = (rect.Width - nWidth) / 2;
                    pt.Y = 0;
                    break;
                case System.Drawing.ContentAlignment.TopLeft:
                    pt.X = 0;
                    pt.Y = 0;
                    break;
                case System.Drawing.ContentAlignment.TopRight:
                    pt.X = rect.Width - nWidth;
                    pt.Y = 0;
                    break;
            }
            pt.X += rect.Left;
            pt.Y += rect.Top;
            return (pt);
        }
        /// <summary>
        /// creates the region for the control. The region will have curved edges. 
        /// This prevents any drawing outside of the region.
        /// </summary>
        private void CreateRegion(int nContract)
        {
            Point[] points = border_Get(0, 0, this.Width, this.Height);
            border_Contract(nContract, ref points);
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddLines(points);
            this.Region = new Region(path);
        }
        /// <summary>
        /// contract the array of points that define the border.
        /// </summary>
        /// <param name="nPixel">number of pixels to conract</param>
        /// <param name="pts">array of points that define the border</param>
        private void border_Contract(int nPixel, ref Point[] pts)
        {
            int a = nPixel;
            pts[0].X += a; pts[0].Y += a;
            pts[1].X -= a; pts[1].Y += a;
            pts[2].X -= a; pts[2].Y += a;
            pts[3].X -= a; pts[3].Y += a;
            pts[4].X -= a; pts[4].Y -= a;
            pts[5].X -= a; pts[5].Y -= a;
            pts[6].X -= a; pts[6].Y -= a;
            pts[7].X += a; pts[7].Y -= a;
            pts[8].X += a; pts[8].Y -= a;
            pts[9].X += a; pts[9].Y -= a;
            pts[10].X += a; pts[10].Y += a;
            pts[11].X += a; pts[10].Y += a;
        }
        /// <summary>
        /// calculates the array of points that make up a border
        /// </summary>
        /// <param name="nLeftEdge">left edge of border</param>
        /// <param name="nTopEdge">top edge of border</param>
        /// <param name="nWidth">width of border</param>
        /// <param name="nHeight">height of border</param>
        /// <returns>returns an array of points that make up the border</returns>
        private Point[] border_Get(int nLeftEdge, int nTopEdge, int nWidth, int nHeight)
        {
            int X = nWidth;
            int Y = nHeight;
            Point[] points = 
			{
				new Point(1   , 0  ),
				new Point(X-1 , 0  ),
				new Point(X-1 , 1  ),
				new Point(X   , 1  ),
				new Point(X   , Y-1),
				new Point(X-1 , Y-1),
				new Point(X-1 , Y  ),
				new Point(1   , Y  ),
				new Point(1   , Y-1),
				new Point(0   , Y-1),
				new Point(0   , 1  ),
				new Point(1   , 1  )
			};
            for (int i = 0; i < points.Length; i++)
            {
                points[i].Offset(nLeftEdge, nTopEdge);
            }
            return points;
        }
        /// <summary>
        /// Increments or decrements the red/green/blue values of a color. It enforces that the
        /// values do not go out of the bounds of 0..255
        /// </summary>
        /// <param name="SColor">source color</param>
        /// <param name="RED">red shift</param>
        /// <param name="GREEN">green shift</param>
        /// <param name="BLUE">blue shift</param>
        /// <returns>returns the calculated color</returns>
        private static Color Shade(Color SColor, int RED, int GREEN, int BLUE)
        {
            int r = SColor.R;
            int g = SColor.G;
            int b = SColor.B;

            r += RED;
            if (r > 0xFF) r = 0xFF;
            if (r < 0) r = 0;

            g += GREEN;
            if (g > 0xFF) g = 0xFF;
            if (g < 0) g = 0;

            b += BLUE;
            if (b > 0xFF) b = 0xFF;
            if (b < 0) b = 0;

            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// Calculates a blended color using the specified parameters. 
        /// </summary>
        /// <param name="SColor">Source Color (color moving from)</param>
        /// <param name="DColor">Dest Color (color movving towards)</param>
        /// <param name="Percentage">Percentage of Dest Color (0..100)</param>
        /// <returns></returns>
        private static Color Blend(Color SColor, Color DColor, int Percentage)
        {
            int r = SColor.R + ((DColor.R - SColor.R) * Percentage) / 100;
            int g = SColor.G + ((DColor.G - SColor.G) * Percentage) / 100;
            int b = SColor.B + ((DColor.B - SColor.B) * Percentage) / 100;
            return Color.FromArgb(r, g, b);
        }
        #endregion
        #region Events Methods
        /// <summary>
        /// Mouse Down Event:
        /// set BtnState to Pushed and Capturing mouse to true
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.Capture = true;
            this.CapturingMouse = true;
            btnState = BtnState.Pushed;
            this.Invalidate();
        }
        /// <summary>
        /// Mouse Up Event:
        /// Set BtnState to Normal and set CapturingMouse to false
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            btnState = BtnState.Normal;
            this.Invalidate();
            this.CapturingMouse = false;
            this.Capture = false;
            this.Invalidate();
        }
        /// <summary>
        /// Mouse Leave Event:
        /// Set BtnState to normal if we CapturingMouse = true
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!CapturingMouse)
            {
                btnState = BtnState.Normal;
                this.Invalidate();
            }
        }
        /// <summary>
        /// Mouse Move Event:
        /// If CapturingMouse = true and mouse coordinates are within button region, 
        /// set BtnState to Pushed, otherwise set BtnState to Normal.
        /// If CapturingMouse = false, then set BtnState to MouseOver
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (CapturingMouse)
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.Width, this.Height);
                btnState = BtnState.Normal;
                if ((e.X >= rect.Left) && (e.X <= rect.Right))
                {
                    if ((e.Y >= rect.Top) && (e.Y <= rect.Bottom))
                    {
                        btnState = BtnState.Pushed;
                    }
                }
                this.Capture = true;
                this.Invalidate();
            }
            else
            {
                //if(!this.Focused)
                {
                    if (btnState != BtnState.MouseOver)
                    {
                        btnState = BtnState.MouseOver;
                        this.Invalidate();
                    }
                }
            }
        }
        /// <summary>
        /// Enable/Disable Event:
        /// If button became enabled, set BtnState to Normal
        /// else set BtnState to Inactive
        /// </summary>
        /// <param name="e"></param>
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (this.Enabled)
            {
                this.btnState = BtnState.Normal;
            }
            else
            {
                this.btnState = BtnState.Inactive;
            }
            this.Invalidate();
        }
        /// <summary>
        /// Lose Focus Event:
        /// set btnState to Normal
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (this.Enabled)
            {
                this.btnState = BtnState.Normal;
            }
            this.Invalidate();
        }


        #endregion
    }
}
