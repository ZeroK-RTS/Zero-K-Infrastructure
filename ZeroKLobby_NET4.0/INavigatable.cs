﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroKLobby
{
    public interface INavigatable
	{
		string PathHead { get; }
        bool TryNavigate(params string[] path);
		bool Hilite(HiliteLevel level, string path);
        string Title { get; }
	}

	public enum HiliteLevel
	{
		None= 0,
		Bold = 1,
		Flash = 2
	}
}
