using System.Windows;
using CMissionLib;

namespace MissionEditor2
{
	public class UnitEventArgs : RoutedEventArgs
	{
		public UnitEventArgs(UnitStartInfo unitInfo, RoutedEvent routedEvent) : base(routedEvent)
		{
			UnitInfo = unitInfo;
		}

		public UnitStartInfo UnitInfo { get; set; }
	}
}