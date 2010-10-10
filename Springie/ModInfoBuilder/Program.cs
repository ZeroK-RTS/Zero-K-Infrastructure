using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Springie;
using Springie.SpringNamespace;

namespace ModInfoBuilder
{
	class Program
	{
		static void Main(string[] args)
		{
			string path;
			if (args.Length ==0) {
				Console.WriteLine("Needs path to spring, assuming im in spring folder");
				path = Directory.GetCurrentDirectory();
			}
			else path = Utils.Glue(args);

			var us = new UnitSync(path);
			BinaryFormatter bf = new BinaryFormatter();
			using (var fs = new FileStream("mapinfo.dat", FileMode.Create)) bf.Serialize(fs, us.MapList);
			using (var fs = new FileStream("modinfo.dat", FileMode.Create)) bf.Serialize(fs, us.ModList);

		}
	}
}
