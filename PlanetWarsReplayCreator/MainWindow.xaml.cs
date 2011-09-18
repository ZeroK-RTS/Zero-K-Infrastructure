using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Gif.Components;
using ZkData;
using System.IO;
using System.Net;
using System.Diagnostics;

namespace PlanetWarsReplayCreator
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{

			InitializeComponent();

		}
		private double planetIconSize;
		private Dictionary<int, Ellipse> planetIcons = new Dictionary<int, Ellipse>();
		ZkDataContext db = new ZkDataContext();
		int turn = 0;

		private void mapCanvas_Loaded(object sender, RoutedEventArgs e)
		{

				foreach (var link in db.Links)
				{
					var line = new Line
					{
						X1 = link.PlanetByPlanetID1.X,
						Y1 = link.PlanetByPlanetID1.Y,
						X2 = link.PlanetByPlanetID2.X,
						Y2 = link.PlanetByPlanetID2.Y,
						Stroke = Brushes.White,
						StrokeThickness = 0.01,
					};
					mapCanvas.Children.Add(line);
				}
				foreach (var planet in db.Planets)
				{
					planetIconSize = 0.03;
					var planetIcon = new Ellipse { Width = planetIconSize, Height = planetIconSize, Fill = Brushes.White };
					Canvas.SetLeft(planetIcon, planet.X - planetIcon.Width/ 2);
					Canvas.SetTop(planetIcon, planet.Y - planetIcon.Height/ 2);
					mapCanvas.Children.Add(planetIcon);
					planetIcons.Add(planet.PlanetID, planetIcon);
				}

		}

		Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();

		BitmapImage CreateImageSynchronously(string uri)
		{
			BitmapImage image;
			if (imageCache.TryGetValue(uri, out image)) return image;
			var webClient = new WebClient();
			image = ImageFromBuffer(webClient.DownloadData(uri));
			imageCache[uri] = image;
			return image;
		}

		BitmapImage ImageFromBuffer(Byte[] bytes)
		{
			var stream = new MemoryStream(bytes);
			BitmapImage image = new BitmapImage();
			image.BeginInit();
			image.StreamSource = stream;
			image.EndInit();
			return image;
		}

		private void ApplyHistoryItem(PlanetOwnerHistory chapter)
		{
			if (chapter.Clan != null)
			{
				var clanImageUrl = chapter.Clan.GetImageUrl();
				var icon = planetIcons[chapter.PlanetID];
				icon.Width = planetIconSize * 2;
				icon.Height = planetIconSize * 2;
				Canvas.SetLeft(icon, chapter.Planet.X - icon.Width / 2);
				Canvas.SetTop(icon, chapter.Planet.Y - icon.Height / 2);
				icon.Fill = new ImageBrush { ImageSource = CreateImageSynchronously("http://zero-k.info/" + clanImageUrl) };
			}
		}
		private void nextButton_Click(object sender, RoutedEventArgs e)
		{
			turn++;
			var chapters = db.PlanetOwnerHistories.Where(h => h.Turn == turn);
			foreach (var chapter in chapters) ApplyHistoryItem(chapter);
		}

		private void exportButton_Click(object sender, RoutedEventArgs e)
		{
#if false

			var encoder = new GifBitmapEncoder();
			for (var i = 0; i <= 100; i++)
			{
				var chapters = db.PlanetOwnerHistories.Where(h => h.Turn == i);
				foreach (var chapter in chapters) ApplyHistoryItem(chapter);
				mapCanvas.UpdateLayout();

				var bitmap = new RenderTargetBitmap((int)viewBox.ActualWidth, (int)viewBox.ActualHeight, 96 * (int)viewBox.ActualWidth, 96 * (int)viewBox.ActualHeight, PixelFormats.Pbgra32);
				bitmap.Render(mapCanvas);
				encoder.Frames.Add(BitmapFrame.Create(bitmap));
				Debug.WriteLine(i);
			}
			string path = @"C:\Users\Nubtron\Desktop\replay.gif";
			var fileStream = new FileStream(path, FileMode.Create);
			encoder.Save(fileStream);
			fileStream.Close();
#endif
#if true			
			
                var anim = new AnimatedGifEncoder();
                anim.SetSize(512, 512);
                anim.SetDelay(200);
                anim.Start(File.OpenWrite(@"c:\temp\pw_replay.gif"));    
            foreach (var chapters in db.PlanetOwnerHistories.GroupBy(x=>x.Turn).OrderBy(x=>x.Key))
			    {
                    foreach (var chapter in chapters) ApplyHistoryItem(chapter);
			        mapCanvas.UpdateLayout();

			        var bitmap = new RenderTargetBitmap(512, 512, 96*512, 96*512, PixelFormats.Pbgra32);
			        bitmap.Render(mapCanvas);

			        var encoder = new BmpBitmapEncoder();
			        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    var ms = new MemoryStream();
                    encoder.Save(ms);
                    anim.AddFrame(System.Drawing.Image.FromStream(ms));
			    }
            anim.Finish();
					
#endif
		}


		
	}
}
