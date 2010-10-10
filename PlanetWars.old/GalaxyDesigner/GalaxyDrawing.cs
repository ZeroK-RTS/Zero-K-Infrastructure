using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PlanetWarsShared;

namespace GalaxyDesigner
{
    public class GalaxyDrawing : INotifyPropertyChanged
    {
        Canvas canvas;
        Galaxy galaxy;

        ImageSource imageSource;
        string mapNames;
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

        public GalaxyDrawing()
        {
            if (File.Exists("galaxy.jpg")) {
                ImageSource = new BitmapImage(new Uri("galaxy.jpg", UriKind.Relative));
            } else {
                AskForImageSource();
            }
            if (File.Exists("Data/galaxy.xml")) {
                Galaxy = Galaxy.FromFile("Data/galaxy.xml");
            } else {
                AskForGalaxy();
            }
        }

        public int MapCount
        {
            get
            {
                return ToLines(mapNames).Length;
            }
        }

        public int PlanetCount
        {
            get
            {
                return PlanetDrawings.Count;
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

        public Canvas Canvas
        {
            get { return canvas; }
            set
            {
                canvas = value;
                Galaxy = galaxy;
            }
        }

        public ListBox WarningList { get; set; }

        public List<PlanetDrawing> PlanetDrawings { get; set; }
        public List<LinkDrawing> LinkDrawings { get; set; }

        public Galaxy Galaxy
        {
            get { return galaxy; }
            set
            {
                galaxy = value;
                var planetDict = Galaxy.Planets.ToDictionary(p => p.ID, p => new PlanetDrawing(p, ImageSource.Width, ImageSource.Height));
                PlanetDrawings = planetDict.Values.ToList();
                LinkDrawings = Galaxy.Links.Select(l => new LinkDrawing(planetDict[l.PlanetIDs[0]], planetDict[l.PlanetIDs[1]])).ToList();
                if (canvas != null) {
                    foreach (var p in PlanetDrawings) {
                        canvas.Children.Add(p);
                        Panel.SetZIndex(p, 2);
                    }
                    foreach (var l in LinkDrawings) {
                        canvas.Children.Add(l);
                        Panel.SetZIndex(l, 1);
                    }
                }
                MapNames = String.Join("\r\n", galaxy.MapNames.ToArray());
                GalaxyUpdated();
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        public void AskForImageSource()
        {
            var dialog = new OpenFileDialog {Filter = "JPEG files (*.jpg)|*.jpg|All files (*.*)|*.*", Title = "Select a galaxy background image."};
            if (dialog.ShowDialog().Value) {
                ImageSource = new BitmapImage(new Uri(dialog.FileName, UriKind.Relative));
            } else if (ImageSource == null) {
                Environment.Exit(1);
            }
        }

        public void AskForGalaxy()
        {
            var dialog = new OpenFileDialog {Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*", Title = "Select a galaxy xml file."};
            Galaxy = dialog.ShowDialog().Value ? Galaxy.FromFile(dialog.FileName) : new Galaxy();
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

        public void AddLink(PlanetDrawing planet1, PlanetDrawing planet2)
        {
            if (planet1 == planet2 &&
                LinkDrawings.Any(l => (l.Planet1 == planet1 && l.Planet2 == planet2) || (l.Planet2 == planet1 && l.Planet1 == planet2))) {
                return;
            }
            var d = new LinkDrawing(planet1, planet2);
            LinkDrawings.Add(d);
            canvas.Children.Add(d);
            Panel.SetZIndex(d, 1);
            GalaxyUpdated();
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

        public void RemoveLink(LinkDrawing link)
        {
            LinkDrawings.Remove(link);
            canvas.Children.Remove(link);
        }

        public void GalaxyUpdated()
        {
            //if (WarningList != null) {
            //    WarningList.ItemsSource = galaxy.GetWarnings().Select(w => new ListBoxItem {IsEnabled = false, Content = w}).ToArray();
            //}
            if (WarningList == null) {
                return;
            }
            WarningList.Items.Clear();
            var planetCounts = PlanetDrawings.ToDictionary(p => p, p => 0);
            foreach (var l in LinkDrawings) {
                planetCounts[l.Planet1]++;
                planetCounts[l.Planet2]++;
            }
            if (MapCount < PlanetCount) {
                WarningList.Items.Add(new ListBoxItem { Background = Brushes.Red, Content = String.Format("More planets than maps ({0} > {1})", PlanetCount, MapCount) });
            }
            foreach (var kvp in planetCounts) {
                if (kvp.Value == 0) {
                    var item = new ListBoxItem { Background = Brushes.Red, Content = (kvp.Key.PlanetName ?? "Planet") + " has no link" };
                    WarningList.Items.Add(item);
                    var planet = kvp.Key;
                    item.MouseUp += (s, e) => planet.Grow();
                } else if (kvp.Value == 2) {
                    var item = new ListBoxItem {Content = (kvp.Key.PlanetName ?? "Planet") + " has only two links"};
                    var planet = kvp.Key;
                    item.MouseUp += (s, e) => planet.Grow();
                    WarningList.Items.Add(item);
                }
            }
            WarningList.Items.Refresh();
            PropertyChanged(this, new PropertyChangedEventArgs("PlanetCount"));
        }

        public void Clear()
        {
            LinkDrawings.Clear();
            PlanetDrawings.Clear();
            canvas.Children.Clear();
            GalaxyUpdated();
        }

        public void LoadGalaxy(Galaxy newGalaxy)
        {
            Clear();
            Galaxy = newGalaxy;
            GalaxyUpdated();
        }

        public Galaxy ToGalaxy()
        {
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
        }

        string[] ToLines(string textBlock)
        {
            return textBlock.Replace("\r\n", "\n").Split('\n');
        }

        public void SaveGalaxy()
        {
            var saveFileDialog = new SaveFileDialog { DefaultExt = "xml", Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*" };
            if (saveFileDialog.ShowDialog().Value) {
                galaxy.SaveToFile(saveFileDialog.FileName);
            }
        }
    }
}