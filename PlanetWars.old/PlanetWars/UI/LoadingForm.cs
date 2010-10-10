using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using PlanetWars.Properties;
using PlanetWarsShared;

namespace PlanetWars.UI
{
	partial class LoadingForm : Form
	{
		readonly GalaxyLoader loader = new GalaxyLoader(false);
		List<Map> alreadyLoaded;

		public LoadingForm()
		{
			InitializeComponent();
			base.Text = "Loading";
			loader.ProgressChanged += loader_ProgressChanged;
			loader.RunWorkerCompleted += loader_RunWorkerCompleted;
			alreadyLoaded = GalaxyMap.Instance.Maps;
			loader.RunWorkerAsync();
		}

		public static void UpdateGalaxy()
		{
			var lastChanged = Program.Server.LastChanged;
			if (lastChanged < Program.LastUpdate)
			{
				return;
			}
			new LoadingForm().Show();
		}

		void loader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null) {
				throw e.Error;
			}
			GalaxyLoader.LoadUpdateDoneFinalizeMaps(alreadyLoaded);
			Close();
		}

		void loader_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressBar1.Value = e.ProgressPercentage;
			var mapInfo = e.UserState as string[];
			if (mapInfo != null)
			{
				Text = mapInfo[0];
			}
		}
	}
}