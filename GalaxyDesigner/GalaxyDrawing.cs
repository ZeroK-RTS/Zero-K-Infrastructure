using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PlasmaShared;
using ZkData;

namespace GalaxyDesigner
{
	public class GalaxyDrawing: INotifyPropertyChanged
	{
		public const int galaxyNumber = 1;

		Canvas canvas;

		ImageSource imageSource;
		string mapNames;
		public Canvas Canvas
		{
			get { return canvas; }
			set
			{
				canvas = value;
				//Galaxy = galaxy;
			}
		}
		public ImageSource ImageSource
		{
			get { return imageSource; }
			set
			{
				imageSource = value;
				PropertyChanged(this, new PropertyChangedEventArgs("ImageSource"));
			}
		}
		public List<LinkDrawing> LinkDrawings { get; set; }

		public int MapCount { get { return mapNames != null ? ToLines(mapNames).Length : 0; } }
		public string MapNames
		{
			get { return mapNames; }
			set
			{
				mapNames = value;
				PropertyChanged(this, new PropertyChangedEventArgs("MapNames"));
				PropertyChanged(this, new PropertyChangedEventArgs("MapCount"));
			}
		}

		public int PlanetCount { get { return PlanetDrawings != null ? PlanetDrawings.Count : 0; } }

		public List<PlanetDrawing> PlanetDrawings { get; set; }
		public ListBox WarningList { get; set; }

		public GalaxyDrawing()
		{
			/*if (File.Exists("galaxy.jpg")) {
                ImageSource = new BitmapImage(new Uri("galaxy.jpg", UriKind.Relative));
            } else {
                AskForImageSource();
            }*/
			//LoadGalaxy();
			//AskForGalaxy();
			/*
            if (File.Exists("Data/galaxy.xml")) {
                Galaxy = Galaxy.FromFile("Data/galaxy.xml");
            } else {
                AskForGalaxy();
            }*/
		}

		public void AddLink(PlanetDrawing planet1, PlanetDrawing planet2)
		{
			if (planet1 == planet2 && LinkDrawings.Any(l => (l.Planet1 == planet1 && l.Planet2 == planet2) || (l.Planet2 == planet1 && l.Planet1 == planet2))) return;
			var d = new LinkDrawing(planet1, planet2);
			LinkDrawings.Add(d);
			canvas.Children.Add(d);
			Panel.SetZIndex(d, 1);
			GalaxyUpdated();
		}

		public PlanetDrawing AddPlanet(Point pos, string name)
		{
			var d = new PlanetDrawing(pos, name);
			PlanetDrawings.Add(d);
			canvas.Children.Add(d);
			Panel.SetZIndex(d, 2);
			GalaxyUpdated();
			return d;
		}

		public void AskForGalaxy()
		{
			var dialog = new OpenFileDialog { Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*", Title = "Select a galaxy xml file." };
			Clear();
			LoadGalaxy();

			//Galaxy = dialog.ShowDialog().Value ? Galaxy.FromFile(dialog.FileName) : new Galaxy();
		}

		public void AskForImageSource()
		{
			var dialog = new OpenFileDialog { Filter = "JPEG files (*.jpg)|*.jpg|All files (*.*)|*.*", Title = "Select a galaxy background image." };
			if (dialog.ShowDialog().Value) ImageSource = new BitmapImage(new Uri(dialog.FileName, UriKind.Relative));
			else if (ImageSource == null) Environment.Exit(1);
		}

		public void Clear()
		{
			LinkDrawings.Clear();
			PlanetDrawings.Clear();
			canvas.Children.Clear();
			GalaxyUpdated();
		}

		public void GalaxyUpdated()
		{
			//if (WarningList != null) {
			//    WarningList.ItemsSource = galaxy.GetWarnings().Select(w => new ListBoxItem {IsEnabled = false, Content = w}).ToArray();
			//}
			if (WarningList == null) return;
			WarningList.Items.Clear();
			var planetCounts = PlanetDrawings.ToDictionary(p => p, p => 0);
			foreach (var l in LinkDrawings)
			{
				planetCounts[l.Planet1]++;
				planetCounts[l.Planet2]++;
			}
			if (MapCount < PlanetCount)
			{
				WarningList.Items.Add(new ListBoxItem
				                      { Background = Brushes.Red, Content = String.Format("More planets than maps ({0} > {1})", PlanetCount, MapCount) });
			}
			foreach (var kvp in planetCounts)
			{
				if (kvp.Value == 0)
				{
					var item = new ListBoxItem { Background = Brushes.Red, Content = (kvp.Key.PlanetName ?? "Planet") + " has no link" };
					WarningList.Items.Add(item);
					var planet = kvp.Key;
					item.MouseUp += (s, e) => planet.Grow();
				}
				else if (kvp.Value == 2)
				{
					/*var item = new ListBoxItem {Content = (kvp.Key.PlanetName ?? "Planet") + " has only two links"};
                    var planet = kvp.Key;
                    item.MouseUp += (s, e) => planet.Grow();
                    WarningList.Items.Add(item);*/
				}
			}
			WarningList.Items.Refresh();
			PropertyChanged(this, new PropertyChangedEventArgs("PlanetCount"));
		}

		public void LoadGalaxy()
		{
			try
			{
				var db = new ZkDataContext();
				var planetDict = db.Planets.ToDictionary(p => p.PlanetID, p => new PlanetDrawing(p, ImageSource.Width, ImageSource.Height));
				PlanetDrawings = planetDict.Values.ToList();
				LinkDrawings = db.Links.Select(l => new LinkDrawing(planetDict[l.PlanetID1], planetDict[l.PlanetID2])).ToList();
				if (canvas != null)
				{
					foreach (var p in PlanetDrawings)
					{
						canvas.Children.Add(p);
						Panel.SetZIndex(p, 2);
					}
					foreach (var l in LinkDrawings)
					{
						canvas.Children.Add(l);
						Panel.SetZIndex(l, 1);
					}
				}
				MapNames = String.Join("\r\n",
				                       db.Resources.Where(x => x.MapPlanetWarsIcon != null && x.FeaturedOrder != null).Select(x => x.InternalName).ToArray());
				GalaxyUpdated();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public void RemoveLink(LinkDrawing link)
		{
			LinkDrawings.Remove(link);
			canvas.Children.Remove(link);
		}

		public void RemovePlanet(PlanetDrawing planet)
		{
			var links = LinkDrawings.FindAll(l => l.Planet1 == planet || l.Planet2 == planet);
			links.ForEach(l => LinkDrawings.Remove(l));
			links.ForEach(canvas.Children.Remove);
			PlanetDrawings.Remove(planet);
			canvas.Children.Remove(planet);
			GalaxyUpdated();
		}

		public void SaveGalaxy()
		{
			var db = new ZkDataContext();
			var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyNumber);
			db.Links.DeleteAllOnSubmit(gal.Links);
			db.Planets.DeleteAllOnSubmit(gal.Planets);
			db.SubmitChanges();

			var maps = MapNames.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Shuffle();
			var cnt = 0;


			foreach (var d in PlanetDrawings)
			{
				var p = d.Planet;
				p.X = (float)(Canvas.GetLeft(d)/imageSource.Width);
				p.Y = (float)(Canvas.GetTop(d)/imageSource.Height);
				if (p.MapResourceID == null)
				{
					p.MapResourceID = db.Resources.Single(x => x.InternalName == maps[cnt]).ResourceID;
					cnt++;
				}
				else maps.Remove(db.Resources.Single(x => x.ResourceID == p.MapResourceID).InternalName);

				gal.Planets.Add(p.DbClone());
			}
			db.SubmitChanges();

			var linkList =
				LinkDrawings.Select(d => new Link() { GalaxyID = galaxyNumber, PlanetID1 = db.Planets.Single(x => x.GalaxyID == galaxyNumber && x.Name == d.Planet1.PlanetName).PlanetID, PlanetID2 = db.Planets.Single(x => x.GalaxyID == galaxyNumber && x.Name == d.Planet2.PlanetName).PlanetID });
			db.Links.InsertAllOnSubmit(linkList);
			db.SubmitChanges();
		}


		public void ToGalaxy()
		{
			// todo
			/*
            var gal = new Galaxy();
            int id = 0;
            var planetIDs = PlanetDrawings.ToDictionary(
                d => d,
                d =>
                {
                    var temp = id++;
                    return temp;
                });

            gal.Planets = PlanetDrawings.Select(
                d => new Planet(planetIDs[d],
                                (float)(Canvas.GetLeft(d)/imageSource.Width), 
                                (float)(Canvas.GetTop(d)/imageSource.Height))).ToList();
            gal.Links = LinkDrawings.Select(d => new Link(planetIDs[d.Planet1], planetIDs[d.Planet2])).ToList();
            gal.MapNames = ToLines(MapNames).ToList();
            return gal;
					 * */
		}

		string[] ToLines(string textBlock)
		{
			return textBlock.Replace("\r\n", "\n").Split('\n');
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
	}
}