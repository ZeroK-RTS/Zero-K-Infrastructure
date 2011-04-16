using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZkData;

namespace GalaxyDesigner
{
	/// <summary>
	/// Interaction logic for PlanetDrawing.xaml
	/// </summary>
	public partial class PlanetDrawing: INotifyPropertyChanged
	{
		readonly Planet planet = new Planet();

		string planetName;
		public bool IsHighlighted
		{
			set
			{
				var key = value ? "RedGradient" : "BlueGradient";
				Ellipse.Fill = (RadialGradientBrush)FindResource(key);
			}
		}
		public Planet Planet { get
		{
			planet.Name = planetName;
			return planet;
		} }

		public string PlanetName
		{
			get { return planetName; }
			set
			{
				planetName = value;
				PropertyChanged(this, new PropertyChangedEventArgs("PlanetName"));
			}
		}

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
			PlanetName = planet.Name;
		}

		public PlanetDrawing(Point pos, string name)
		{
			InitializeComponent();
			Position = pos;
			PlanetName = name;
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