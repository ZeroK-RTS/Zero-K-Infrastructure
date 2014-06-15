using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class FireStateOrder : OrderTypeIconMode, IOrder
	{
        static string[] names = { "Hold Fire", "Return Fire", "Fire At Will" };

		public FireStateOrder(int mode) : base(mode)
		{
		}

		public override string Name
		{
			get { return names[Mode]; }
		}

		public override string ToString()
		{
			return Name;
		}

		public override string OrderType
		{
			get { return "FIRE_STATE"; }
		}
	}
}