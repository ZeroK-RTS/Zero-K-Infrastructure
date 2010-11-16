using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;

namespace ZeroKLobby.MicroLobby
{
	public class GameInfo
	{
		string shortcut;
    public bool IsPrimary { get; set; }
	  public string Channel { get; set; }
		public string Description { get; set; }
		public string FullName { get; set; }
		public string Image { get; set; }
		public bool IsSelected { get; set; }
		public Image Logo { get { return System.Drawing.Image.FromStream(Application.GetResourceStream(new Uri(Image, UriKind.Relative)).Stream); } }
		public string RapidTag { get; set; }
		public string Regex { get; set; }
		public string Shortcut
		{
			get { return shortcut; }
			set
			{
				shortcut = value;
				if (shortcut.Contains(" ")) throw new ApplicationException("Shortcut must not contain space");
			}
		}
		public string Url { get; set; }

		public override string ToString()
		{
			return Shortcut;
		}
	}
}