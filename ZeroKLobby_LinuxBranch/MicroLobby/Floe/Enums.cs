using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Floe.UI
{
	public enum SearchDirection
	{
		Previous,
		Next
	}

	[Flags]
	public enum SearchOptions
	{
		None = 0,
		MatchCase = 1
	}
}
