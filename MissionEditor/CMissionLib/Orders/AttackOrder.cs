using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class AttackOrder : OrderTypeIconUnitMap, IOrder
	{
		public override string Name
		{
			get { return "Attack"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public AttackOrder(double x, double y)
			: base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "ATTACK"; }
		}
	}
}