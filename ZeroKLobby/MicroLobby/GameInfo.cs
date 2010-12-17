using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows;

namespace ZeroKLobby.MicroLobby
{
	public class GameInfo
	{
		string shortcut;
    public bool IsPrimary { get; set; }
	  public string Channel { get; set; }
		public string FullName { get; set; }
		public string RapidTag { get; set; }
		public Regex Regex { get; set; }
		public string Shortcut
		{
			get { return shortcut; }
			set
			{
				shortcut = value;
				if (shortcut.Contains(" ")) throw new ApplicationException("Shortcut must not contain space");
			}
		}

		public override string ToString()
		{
			return Shortcut;
		}
	}
}