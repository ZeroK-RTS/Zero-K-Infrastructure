using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class RepeatOrder : OrderTypeIconMode, IOrder
	{
		public RepeatOrder(int mode) : base(mode)
		{
		}

		public override string Name
		{
			get { return (Mode == 1 ? "Enable" : "Disable") + "Repeat Mode"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public override string OrderType
		{
			get { return "REPEAT"; }
		}
	}
}