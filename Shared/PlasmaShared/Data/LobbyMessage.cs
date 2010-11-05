using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ZkData
{
	partial class LobbyMessage
	{
		partial void OnCreated()
		{
			Created = DateTime.UtcNow;
		}

		public string ToLobbyString()
		{
			return string.Format("!pm|{0}|{1}|{2}", SourceName, Created.ToString(CultureInfo.InvariantCulture), Message);
		}
	}
}
