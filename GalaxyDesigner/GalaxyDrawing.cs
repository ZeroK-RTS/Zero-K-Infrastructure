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

		Canvas canvas;

		ImageSource imageSource;
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

		public int MapCount { get { return Maps.Count; } }
		public List<Resource> Maps = new List<Resource>();

		public int PlanetCount { get { return PlanetDrawings != null ? PlanetDrawings.Count : 0; } }

		public List<PlanetDrawing> PlanetDrawings { get; set; }

		public List<StructureType> StructureTypes = new List<StructureType>();
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
			Clear();
			var gd = new GalaxyDialog();
			if (gd.ShowDialog() == true) 
			LoadGalaxy(gd.GalaxyNumber);
		}

		public void AskForImageSource()
		{
			var dialog = new OpenFileDialog { Filter = "JPEG files (*.jpg)|*.jpg|All files (*.*)|*.*", Title = "Select a galaxy background image." };
			if (dialog.ShowDialog().Value) ImageSource = new BitmapImage(new Uri(dialog.FileName, UriKind.Relative));
			else if (ImageSource == null) Environment.Exit(1);
		}

		public void Clear()
		{
			if (LinkDrawings != null) LinkDrawings.Clear();
			if (PlanetDrawings != null) PlanetDrawings.Clear();
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
					var item = new ListBoxItem { Background = Brushes.Red, Content = (kvp.Key.Planet.Name ?? "Planet") + " has no link" };
					WarningList.Items.Add(item);
					var planet = kvp.Key;
					item.MouseUp += (s, e) => planet.Grow();
				}
			}


			foreach (var dupMap in PlanetDrawings.GroupBy(x => x.Planet.MapResourceID).Where(x => x.Count() > 1))
			{
				var p = dupMap.First();
				var item = new ListBoxItem { Background = Brushes.Red, Content = string.Format("{0} has duplicate map ({1})", (p.Planet.Name ?? "Planet"), p.Planet.Resource.InternalName) };
				var planet = p;
				item.MouseUp += (s, e) => planet.Grow();
				WarningList.Items.Add(item);
			}


			foreach (var p in PlanetDrawings.Where(x => x.Planet.PlanetStructures == null || !x.Planet.PlanetStructures.Any()))
			{
				var item = new ListBoxItem { Content = (p.Planet.Name ?? "Planet") + " has no structures" };
				var planet = p;
				item.MouseUp += (s, e) => planet.Grow();
				WarningList.Items.Add(item);
			}
			WarningList.Items.Refresh();
			PropertyChanged(this, new PropertyChangedEventArgs("PlanetCount"));
			PropertyChanged(this, new PropertyChangedEventArgs("MapCount"));
		}

		public void LoadGalaxy( int galaxyNumber)
		{
			try
			{
				var db = new ZkDataContext();
				var planetDict = db.Planets.Where(x=>x.GalaxyID == galaxyNumber).ToDictionary(p => p.PlanetID, p => new PlanetDrawing(p, ImageSource.Width, ImageSource.Height));
				PlanetDrawings = planetDict.Values.ToList();
				LinkDrawings = db.Links.Where(x=>x.GalaxyID == galaxyNumber).Select(l => new LinkDrawing(planetDict[l.PlanetID1], planetDict[l.PlanetID2])).ToList();
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

				Maps = db.Resources.Where(x => x.MapPlanetWarsIcon != null && x.FeaturedOrder != null).ToList();
				StructureTypes = db.StructureTypes.ToList();
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

		public void SaveGalaxy(int galaxyNumber)
		{
			try
			{
				var db = new ZkDataContext();
				var gal = db.Galaxies.SingleOrDefault(x => x.GalaxyID == galaxyNumber);
				if (gal.Started != null)
				{
					MessageBox.Show("This galaxy is running, cannot edit it!");
					return;
				}
				if (gal == null || galaxyNumber == 0) {
					gal = new Galaxy();
					db.Galaxies.InsertOnSubmit(gal);
				} else
				{
					db.Links.DeleteAllOnSubmit(gal.Links);
					db.Planets.DeleteAllOnSubmit(gal.Planets);
				}
				db.SubmitChanges();
				galaxyNumber = gal.GalaxyID;

				var maps = Maps.Shuffle();
				var cnt = 0;

				foreach (var d in PlanetDrawings)
				{
					var p = d.Planet;
					p.GalaxyID = galaxyNumber;
					p.X = (float)(Canvas.GetLeft(d)/imageSource.Width);
					p.Y = (float)(Canvas.GetTop(d)/imageSource.Height);
					if (p.MapResourceID == null)
					{
						p.MapResourceID = maps[cnt].ResourceID;
						cnt++;
					}
					else maps.RemoveAll(x => x.ResourceID == p.MapResourceID);

					var clone = p.DbClone();
					clone.PlanetStructures.AddRange(p.PlanetStructures.Select(x => new PlanetStructure() { StructureTypeID = x.StructureTypeID }));
					gal.Planets.Add(clone);
				}
				db.SubmitChanges();

				var linkList =
					LinkDrawings.Select(
						d =>
						new Link()
						{
							GalaxyID = galaxyNumber,
							PlanetID1 = db.Planets.Single(x => x.GalaxyID == galaxyNumber && x.Name == d.Planet1.Planet.Name).PlanetID,
							PlanetID2 = db.Planets.Single(x => x.GalaxyID == galaxyNumber && x.Name == d.Planet2.Planet.Name).PlanetID
						});
				db.Links.InsertAllOnSubmit(linkList);
				db.SubmitChanges();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}


		public event PropertyChangedEventHandler PropertyChanged = delegate { };
	}
}