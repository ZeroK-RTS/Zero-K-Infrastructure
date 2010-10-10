using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ServiceData;

namespace MapGenerator
{
	public partial class Form1 : Form
	{

		public Form1()
		{
			InitializeComponent();
		}

		private DateTime start = DateTime.Now;
		private List<CelestialObject> co = new List<CelestialObject>();


		private void button1_Click(object sender, EventArgs e)
		{
			var mg = new MapGen();
			mg.NewMap();
			pictureBox1.Refresh();
			var db = new DbDataContext();
			co = db.CelestialObjects.ToList();
		}

		void timer_Tick(object sender, EventArgs e)
		{
			pictureBox1.Refresh();
		}

		private void pictureBox1_Paint(object sender, PaintEventArgs e)
		{
			var time = DateTime.Now.Subtract(start).TotalSeconds*100;
			foreach (var star in co.Where(x=>x.CelestialObjectType== CelestialObjectType.Star)) {
				e.Graphics.FillEllipse(Brushes.Yellow, (int)star.X, (int)star.Y,3,3);

				foreach (var planet in star.ChildCelestialObjects) {

					var ang = planet.OrbitInitialAngle + time*2*Math.PI*planet.OrbitSpeed;
					var x = star.X + planet.OrbitDistance*Math.Cos(ang);
					var y = star.Y + planet.OrbitDistance*Math.Sin(ang);

					e.Graphics.FillEllipse(Brushes.Cyan, (int)x,(int)y, 3, 3);

					foreach (var moon in planet.ChildCelestialObjects) {
						ang = moon.OrbitInitialAngle + time * 2 * Math.PI * moon.OrbitSpeed;
						var mx = x + moon.OrbitDistance * Math.Cos(ang);
						var my = y + moon.OrbitDistance * Math.Sin(ang);

						e.Graphics.FillEllipse(Brushes.White, (int)mx, (int)my, 3, 3);
					}
				}
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			var db = new DbDataContext();
			co = db.CelestialObjects.ToList();
			var timer = new Timer();
			timer.Tick += new EventHandler(timer_Tick);
			timer.Interval = 100;
			timer.Start();

		}

		private void button2_Click(object sender, EventArgs e)
		{
			MessageBox.Show("done");
		}
	}
}
