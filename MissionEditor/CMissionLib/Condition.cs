using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public abstract class Condition : TriggerLogic
	{
		public Condition(string name) : base(name) {}

		public string Category
		{
			get { return "Conditions"; }
		}
	}
}