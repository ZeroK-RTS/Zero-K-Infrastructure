using System;
using System.Drawing;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using PlanetWarsShared;

namespace Tests
{
	[TestFixture]
	public class ExtensionTests
	{

		[Test]
		public void GetTopLeft1()
		{
			var a = new PointF(1, 3);
			var b = new PointF(4, 7);
			var topLeft = new[] {a, b}.GetTopLeft();
			Assert.That(topLeft, Is.EqualTo(new PointF(1, 3)));
		}

		[Test]
		public void GetTopLeft2()
		{
			var a = new PointF(5, 3);
			var b = new PointF(4, 7);
			var topLeft = new[] { a, b }.GetTopLeft();
			Assert.That(topLeft, Is.EqualTo(new PointF(4, 3)));			
		}

		[Test]
		public void GetTopLeft3()
		{
			var a = new PointF(3, 3);
			var b = new PointF(3, 3);
			var topLeft = new[] { a, b }.GetTopLeft();
			Assert.That(topLeft, Is.EqualTo(new PointF(3, 3)));
		}

		RectangleF rectangle;

		[SetUp]
		public void Setup()
		{
			var a = new PointF(1, 5);
			var b = new PointF(4, 3);
			rectangle = new[] { a, b }.ToRectangleF();			
		}

		[Test]
		public void ToRectangle1()
		{
			Assert.AreEqual(new PointF(1, 3), rectangle.Location);
		}

		[Test]
		public void ToRectangle2()
		{
			Assert.AreEqual(new SizeF(3, 2), rectangle.Size);
		}

		[Test]
		public void TakeRandom1()
		{
			const int Count = 10;
			var range = Enumerable.Range(0, Count - 1).ToArray();
			var picks = range.TakeRandom(Count);
			Array.Sort(picks);
			Assert.IsTrue(picks.SequenceEqual(range));
		}

		[Test]
		public void TakeRandom2()
		{
			var range = Enumerable.Range(0, 9).ToArray();
			var picks = range.TakeRandom(8);
			Assert.AreEqual(8, picks.Length);
		}


		[Test]
		public void TakeRandom3()
		{
			var range = Enumerable.Range(0, 9).ToArray();
			var picks = range.TakeRandom(8);
			Assert.AreEqual(picks.Length, picks.Distinct().Count());
		}

		[Test]
		public void HighQualityResize1()
		{
            var path = string.Format("..{0}Tests{0}Resources{0}PWisAWESOME.png", Path.DirectorySeparatorChar);
			var image = Image.FromFile(path);
			var scaledImage = image.HighQualityResize(2);
			Assert.AreEqual(image.Size, scaledImage.Size.Multiply(0.5f));
			scaledImage.Save("test.png");
		}

		[Test]
		public void PadRectangle1()
		{
			var rect = new Rectangle(1, 1, 1, 1);
			var paddedRect = rect.PadRectangle(1);
			Assert.AreEqual(new Rectangle(0, 0, 3, 3), paddedRect);
		}

		[Test]
		public void Distance1()
		{
			var a = new Point(0, 1);
			var b = new Point(0, 0);
			var distance = (int)a.GetDistance(b);
			Assert.AreEqual(1, distance);
		}
	}
}