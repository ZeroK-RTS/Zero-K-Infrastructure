using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NUnit.Framework;
using PlanetWars;
using PlanetWarsShared;
using System.IO;

namespace Tests
{
	[TestFixture]
	public class LinkDrawingTests
	{
		#region Setup/Teardown

		[SetUp]
		public void SetUp()
		{
			GlobalSettings.DefaultFloatingPointTolerance = 0.0001;
		}

		#endregion

		[Test]
		public void GetImageBounds1()
		{
			Galaxy gal = new Galaxy();
			Planet a = new Planet(0, 0.1f, 0.1f);
			Planet b = new Planet(1, 0.2f, 0.2f);
			gal.Planets = new List<Planet> {a, b};
			var size = new Size(100, 100);
			var l = new LinkImageGenerator(size, gal, Directory.GetCurrentDirectory()) {Padding = 2};
			var bounds = l.GetImageBounds(new Link(0, 1));
			var side = (int)Math.Round(0.2f*100 - 0.1f*100 + 2*2);
			var point = (int)Math.Round(0.1f*100 - 2);
			Assert.AreEqual(new Rectangle(point, point, side, side), bounds);
		}

		[Test]
		public void Length1()
		{
			var link = new LinkDrawing(PointF.Empty, new PointF(1, 1));
			Assert.AreEqual(Math.Sqrt(2), link.Length);
		}

		[Test]
		public void Length2()
		{
			var side = (float)(Math.Sqrt(2)/10);
			var link = new LinkDrawing(new PointF(side, side), PointF.Empty);
			var l = link.Length;
			Assert.AreEqual(l, 0.2);
		}

		[Test]
		public void Location1()
		{
			var link = new LinkDrawing(new PointF(0.3f, 0.4f), new PointF(0.3f, 0.12f));
			link.Location = new PointF(0, 0);
			Assert.AreEqual(new PointF(0, 0), link.Location);
		}

		[Test]
		public void Location2()
		{
			var link = new LinkDrawing(new PointF(0.3f, 0.4f), new PointF(0.3f, 0.12f));
			link.Location = new PointF(0.5f, 0.24f);
			Assert.AreEqual(new PointF(0.5f, 0.24f), link.Location);
		}

		[Test]
		public void Location3()
		{
			var link = new LinkDrawing(new PointF(0.24f, 0.123f), new PointF(0.11f, 0.2f));
			link.Location = new PointF(0.5f, 0.24f);
			Assert.AreEqual(new PointF(0.5f, 0.24f), link.Location);
		}

		[Test]
		public void Location4()
		{
			var link = new LinkDrawing(new PointF(0.24f, 0.123f), new PointF(0.11f, 0.2f));
			link.Location = new PointF(0.24f, 0.24f);
			Assert.AreEqual(new PointF(0.24f, 0.24f), link.Location);
		}

		[Test]
		public void ToString1()
		{
			var link = new LinkDrawing(new PointF(0.24f, 0.123f), new PointF(0.11f, 0.2f));
			for (int i = 0; i < 50; i++) {
				Assert.AreEqual(link.ToString(), new LinkDrawing(new PointF(0.24f, 0.123f), new PointF(0.11f, 0.2f)).ToString());
			}
		}

		[Test, Description("Checks if LinkDrawing.ToString returns a unique enough string.")]
		public void ToString2()
		{
			const int Iterations = 1000;
			const int SourceMult = 100;
			const float Range = Iterations*SourceMult;
			var tipAbscissae = Enumerable.Range(0, Iterations*SourceMult).TakeRandom(Iterations);
			var tipOrdinates = Enumerable.Range(0, Iterations*SourceMult).TakeRandom(Iterations);
			var endAbscissae = Enumerable.Range(0, Iterations*SourceMult).TakeRandom(Iterations);
			var endOrdinates = Enumerable.Range(0, Iterations*SourceMult).TakeRandom(Iterations);
			string[] strings = new string[Iterations];
			for (int i = 0; i < Iterations; i++) {
				var tip = new PointF(tipAbscissae[i]/Range, tipOrdinates[i]/Range);
				var end = new PointF(endAbscissae[i]/Range, endOrdinates[i]/Range);
				strings[i] = new LinkDrawing(tip, end).ToString();
			}
			Assert.AreEqual(Iterations, strings.Distinct().Count());
		}

		[Test]
		public void ToString3()
		{
			string[] strings = new string[3];
			var link = new LinkDrawing(new PointF(0.24f, 0.123f), new PointF(0.11f, 0.2f));
			strings[0] = link.ToString();
			strings[1] = new LinkDrawing(new PointF(0.24f, 0.123f), new PointF(0.11f, 0.2f)) {ArrowHeigth = 2}.ToString();
			link.IsArrow = true;
			strings[2] = link.ToString();
			Assert.AreEqual(3, strings.Distinct().Count());
		}
	}
}