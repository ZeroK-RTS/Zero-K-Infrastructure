using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public abstract class Condition : TriggerLogic
	{

		public string Category
		{
			get { return "Conditions"; }
		}
	}
}