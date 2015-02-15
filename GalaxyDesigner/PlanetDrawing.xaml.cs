using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using ZkData;

namespace GalaxyDesigner
{
	/// <summary>
	/// Interaction logic for PlanetDrawing.xaml
	/// </summary>
	public partial class PlanetDrawing: INotifyPropertyChanged
	{
		readonly Planet planet = new Planet();

		public bool IsHighlighted
		{
			set
			{
				var key = value ? "RedGradient" : "BlueGradient";
				Ellipse.Fill = (RadialGradientBrush)FindResource(key);
			}
		}
		public Planet Planet { get { return planet; } }

		public Point Position
		{
			get { return new Point(Canvas.GetLeft(this) + Width/2, Canvas.GetTop(this) + Height/2); }
			set
			{
				Canvas.SetLeft(this, value.X - Width/2);
				Canvas.SetTop(this, value.Y - Height/2);
				PropertyChanged(this, new PropertyChangedEventArgs("Position"));
			}
		}


		public PlanetDrawing(Planet planet, double galaxyWidth, double galaxyHeight)
		{
			this.planet = planet;
			InitializeComponent();
			Position = new Point(planet.X*galaxyWidth, planet.Y*galaxyHeight);
			UpdateData(planet.PlanetStructures.Select(x => x.StructureType.Name));
		}

		public void UpdateData(IEnumerable<string> structureNames)
		{
			lbName.Content = planet.Name;
			img.SnapsToDevicePixels = true;
			img.Source = new BitmapImage(new Uri(string.Format(GlobalConst.BaseSiteUrl + "/img/planets/{0}", planet.Resource.MapPlanetWarsIcon)));

			//Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
			//double dpiFactor = 1 / m.M11;
			var width = planet.Resource.PlanetWarsIconSize;
			img.Width = width;
			Canvas.SetLeft(img, -width / 2.0);
			Canvas.SetTop(img, -width / 2.0);

			Structs.Content = string.Join(",", structureNames.ToArray());
		
		}

		public PlanetDrawing(Point pos, string name)
		{
			InitializeComponent();
			Position = pos;
			planet.Name = name;
			lbName.Content = name;
		}

		public void Grow()
		{
			BringIntoView();
			const int mult = 10;
			const double duration = 0.1;
			RenderTransform = new TranslateTransform();
			var translateAnimation = new DoubleAnimation { From = 0, To = (-Width*mult/2), Duration = TimeSpan.FromSeconds(duration), AutoReverse = true };
			RenderTransform.BeginAnimation(TranslateTransform.XProperty, translateAnimation);
			RenderTransform.BeginAnimation(TranslateTransform.YProperty, translateAnimation);
			Ellipse.RenderTransform = new ScaleTransform();
			var scaleAnimation = new DoubleAnimation { From = 1, To = mult, Duration = TimeSpan.FromSeconds(duration), AutoReverse = true };
			Ellipse.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
			Ellipse.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

			//(RadialGradientBrush)FindResource("RedGradient")
			//var colorTransform = new ColorAnimation {To = Colors.Red, Duration=TimeSpan.FromSeconds(0.1), AutoReverse= true};
			//var brush = (RadialGradientBrush)FindResource("RedGradient");
			//brush.BeginAnimation();
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
	}
}