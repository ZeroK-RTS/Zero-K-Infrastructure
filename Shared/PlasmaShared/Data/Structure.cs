using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using PlasmaShared;

namespace ZkData
{
	partial class PlanetStructure
	{
		public string GetImageUrl()
		{
			if (IsDestroyed) return string.Format("/img/structures/{0}", StructureType.DestroyedMapIcon); 
			else return string.Format("/img/structures/{0}", StructureType.MapIcon);
		}

		public string GetPathResized(int size)
		{
			return "/img/structures/" + GetFileNameResized(size);
		}

		public string GetFileNameResized(int size)
		{
			var fileName = IsDestroyed ? StructureType.DestroyedMapIcon : StructureType.MapIcon;
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
			using (var image = Image.FromFile(folder + "/" + (IsDestroyed ? StructureType.DestroyedMapIcon : StructureType.MapIcon)))
			{
				using (var resized = image.GetResized(size, size, InterpolationMode.HighQualityBilinear))
				{
					resized.Save(folder + "/"  + GetFileNameResized(size));
				}				
			}
		}


	}
}
