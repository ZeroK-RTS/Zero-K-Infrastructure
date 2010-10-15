using System.Windows;
using System.Windows.Shapes;
using CMissionLib;

namespace MissionEditor2
{
	class RectangleDragInfo : AreaDragInfo
	{
		public RectangularArea Area;
		public Rectangle Rectangle;
		public Point StartPoint;
	}
}