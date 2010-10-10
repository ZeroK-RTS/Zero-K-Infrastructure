using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PlanetWarsShared;
using System.Drawing;

namespace Tests {

	[TestFixture]
	public class LinkTests {

		Galaxy galaxy;

		[SetUp]
		public void SetUp()
		{
			galaxy = new Galaxy();
			galaxy.Planets.Add(new Planet(0, 0, 0, "barack", "Arm"));
			galaxy.Planets.Add(new Planet(1, 0, 0, "john", "Core"));
			galaxy.Planets.Add(new Planet(2, 0, 0));
			galaxy.Planets.Add(new Planet(3, 0, 0));
			galaxy.Factions.Add(new Faction("Arm", Color.Blue));
		}

		[Test]
		public void GetFileName1()
		{
			var link = new Link(0, 1);
			Assert.AreEqual("0_1_Arm_Core_Arm.png", link.GetFileName(galaxy));
		}

		[Test]
		public void GetFileName2()
		{
			var link = new Link(0, 2);
			Assert.AreEqual("0_2_Arm_neutral_.png", link.GetFileName(galaxy));
		}

		[Test]
		public void GetFileName3()
		{
			var link = new Link(3, 2);
			Assert.AreEqual("3_2_neutral_neutral_.png", link.GetFileName(galaxy));
		}

	}
}
