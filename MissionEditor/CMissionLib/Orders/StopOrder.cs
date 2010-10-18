using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public class StopOrder : OrderTypeIcon
	{
		public override string Name
		{
			get { return "Stop"; }
		}

		public override string ToString()
		{
			return Name;
		}

		public override string OrderType
		{
			get { return "STOP"; }
		}
	}
}