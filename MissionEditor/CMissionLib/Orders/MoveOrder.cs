using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class MoveOrder : OrderTypeIconMap, IOrder
	{
		public override string Name
		{
			get { return "Move"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public MoveOrder(double x, double y) : base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "MOVE"; }
		}
	}
}