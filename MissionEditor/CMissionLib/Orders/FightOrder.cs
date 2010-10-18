using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class FightOrder : OrderTypeIconMap, IOrder
	{
		public override string Name
		{
			get { return "Fight"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public FightOrder(double x, double y)
			: base(x, y)
		{
		}

		public override string OrderType
		{
			get { return "FIGHT"; }
		}
	}
}