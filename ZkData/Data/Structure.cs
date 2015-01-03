using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Mime;

namespace ZkData
{
	partial class PlanetStructure
	{
		public string GetImageUrl()
		{
			if (!IsActive) return string.Format((string)"/img/structures/{0}", StructureType.DisabledMapIcon); 
			else return string.Format((string)"/img/structures/{0}", StructureType.MapIcon);
		}

		public string GetPathResized(int size)
		{
			return "/img/structures/" + GetFileNameResized(size);
		}

		public string GetFileNameResized(int size)
		{
			var fileName = !IsActive ? StructureType.DisabledMapIcon : StructureType.MapIcon;
			var extension = Path.GetExtension(fileName);
			return String.Format("{0}_r_{1}{2}", Path.GetFileNameWithoutExtension(fileName), size, extension);
		}


		public void GenerateResized(int size, string folder)
		{
			GenerateResized(size, folder, true);
			GenerateResized(size, folder, false);
		}



		public void GenerateResized(int size, string folder, bool destroyed)
		{
			using (var image = Image.FromFile(folder + "/" + (!IsActive ? StructureType.DisabledMapIcon : StructureType.MapIcon)))
			{
				using (var resized = image.GetResized(size, size, InterpolationMode.HighQualityBilinear))
				{
					var fileNameResized = GetFileNameResized(size);
					resized.Save(folder + "/" + fileNameResized);
				}				
			}
		}


	}
}
