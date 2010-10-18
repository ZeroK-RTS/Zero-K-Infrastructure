using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class PatrolOrder : OrderTypeIconMap, IOrder
	{
		public override string Name
		{
			get { return "Patrol"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public PatrolOrder(double x, double y)
			: base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "PATROL"; }
		}
	}
}