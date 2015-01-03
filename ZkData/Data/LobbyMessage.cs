using System.Globalization;

namespace ZkData
{
	partial class LobbyMessage
	{
		public string ToLobbyString()
		{
			return string.Format("!pm|{0}|{1}|{2}", SourceName, Created.ToString(CultureInfo.InvariantCulture), Message);
		}
	}
}
