#region using

using System;
using System.Data.Linq;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;

#endregion

namespace ModelBase
{
	public partial class Model
	{
		#region Public methods

		public string GetIconUrl()
		{
			return string.Format("modelicons/{0}.png", ModelID);
		}


		#endregion

		#region Other methods

		partial void OnValidate(ChangeAction action)
		{
			if (action == ChangeAction.Update || action == ChangeAction.Insert) {
				if (ModelProgress < 0) ModelProgress = 0;
				if (ModelProgress > 100) ModelProgress = 100;

				if (TextureProgress < 0) TextureProgress = 0;
				if (TextureProgress > 100) TextureProgress = 100;

				if (ScriptProgress < 0) ScriptProgress = 0;
				if (ScriptProgress > 100) ScriptProgress = 100;


				OverallProgress = (int) (40*(ModelProgress/100.0) + 40*(TextureProgress/100.0) + 20*(ScriptProgress/100.0));
			}
		}

		#endregion


		public void UpdateIcon()
		{
			DetermineIconFile();
			GenerateIconThumbnail();
		}

		private void DetermineIconFile() {
			string icon = string.Format("svn/{0}/{1}/icon.png", User.Login, Name);
			if (File.Exists(HttpContext.Current.Server.MapPath(icon))) {
				IconFile = "icon.png";
				return;
			}

			if (IconFile != null)
			{
				icon = string.Format("svn/{0}/{1}/{2}", User.Login, Name, IconFile);
				if (File.Exists(HttpContext.Current.Server.MapPath(icon))) return;
			}


			string file = Directory.GetFiles(HttpContext.Current.Server.MapPath(string.Format("svn/{0}/{1}", User.Login, Name)), "*.jpg").FirstOrDefault();
			if (file != null) {
				IconFile = Path.GetFileName(file);
				return;
			}

			string screens = HttpContext.Current.Server.MapPath(string.Format("svn/{0}/{1}/screenshots", User.Login, Name));

			if (Directory.Exists(screens))
			{
				file = Directory.GetFiles(screens).FirstOrDefault();
				if (file != null) {
					IconFile = "screenshots/" + Path.GetFileName(file);
					return;
				}
			}
		}


		internal void GenerateIconThumbnail()
		{
			string file;
			if (IconFile == null) {
				file = string.Format("svn/{0}/{1}/icon.png", User.Login, Name);
			} else {
				file = string.Format("svn/{0}/{1}/{2}", User.Login, Name, IconFile);
			}
			file = HttpContext.Current.Server.MapPath(file);
			if (File.Exists(file)) {
				var orig = new Bitmap(file);
				var newW = 96;
				var newH = 96;
				if (orig.Width > orig.Height) {
					newH = newH * orig.Height/orig.Width;
				} else if (orig.Height > orig.Width) {
					newW = newW * orig.Width/orig.Height;
				}
				var thumb = new Bitmap(orig, newW, newH);
				thumb.Save(HttpContext.Current.Server.MapPath(string.Format("~/modelicons/{0}.png", ModelID)));
			}
		}
	}
}

