using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using PlanetWars.Utility;

namespace PlanetWars.UI
{
	public class ScrollablePictureBox : ScrollableControl
	{
		const int DefaultZoomFactor = 1;
		float maxZoomFactor;
		Size originalSize;
		float zoomFactor = DefaultZoomFactor;

		public ScrollablePictureBox()
		{
			MaxSize = Int32.MaxValue;
			MaxZoomFactor = 10;
			MinZoomFactor = 0.01f;
			ScrollWheelSensitivity = 0.01f;
			PictureBox = new PictureBox {SizeMode = PictureBoxSizeMode.StretchImage};
			Controls.Add(PictureBox);                                                  
		}

		public Point ScrollPosition
		{
			get { return new Point(HorizontalScroll.Value, VerticalScroll.Value); }
			set
			{
				HorizontalScroll.Value = value.X.Constrain(HorizontalScroll.Minimum, HorizontalScroll.Maximum);
				VerticalScroll.Value = value.Y.Constrain(VerticalScroll.Minimum, VerticalScroll.Maximum);
			}
		}

		public int MaxSize { get; set; }

		public float MaxZoomFactor
		{
			get { return maxZoomFactor; }
			set { maxZoomFactor = Math.Min(value, 10); } // more than 10 may cause a crash
		}

		public float MinZoomFactor { get; set; }
		public float ScrollWheelSensitivity { get; set; }
		public PictureBox PictureBox { get; set; }

		public float ZoomFactor
		{
			get { return zoomFactor; }
			set
			{
				value = value.Constrain(MinZoomFactor, MaxZoomFactor);
				PictureBox.Size = originalSize.Scale(value).Cap(MaxSize);
				AutoScrollMinSize = PictureBox.Size;
				zoomFactor = value;
			}
		}

		public Image Image
		{
			get { return PictureBox.Image; }
			set
			{
				if (value == null) {
					throw new ArgumentNullException("value");
				}
				AutoScroll = true;
				originalSize = value.Size;
				PictureBox.Size = value.Size.Scale(zoomFactor);
				AutoScrollMinSize = PictureBox.Size;
				PictureBox.Image = value;
			}
		}

		public void Redraw()
		{
			if (!InvokeRequired) {
				PictureBox.Invalidate();
				PictureBox.Update();
			} else {
				Invoke(new ThreadStart(Redraw));
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			if (e.Delta > 0) {
				ZoomFactor *= e.Delta*ScrollWheelSensitivity;
			} else {
				ZoomFactor /= -e.Delta*ScrollWheelSensitivity;
			}
		}

		public void FitToScreen()
		{
			ZoomFactor = Math.Min((float)Size.Width/Image.Width, (float)Size.Height/Image.Height);
		}
	}
}