using System;

namespace Floe.UI
{
	[Flags]
	public enum ChatMarker
	{
		None = 0,
		NewMarker = 1,
		OldMarker = 2,
		Attention = 4
	}
}
