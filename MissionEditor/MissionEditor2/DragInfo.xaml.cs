using System.Collections.Generic;
using System.Windows;

namespace MissionEditor2
{
	class DragInfo
	{
		public List<FrameworkElement> Elements;
		public Dictionary<FrameworkElement, Point> ElementOrigins;
		public Point MouseOrigin;
	}
}