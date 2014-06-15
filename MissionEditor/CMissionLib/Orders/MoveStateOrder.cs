using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class MoveStateOrder : OrderTypeIconMode, IOrder
	{
        static string[] names = { "Hold Position", "Maneuver", "Roam" };

		public MoveStateOrder(int mode) : base(mode)
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
			get { return "MOVE_STATE"; }
		}
	}
}