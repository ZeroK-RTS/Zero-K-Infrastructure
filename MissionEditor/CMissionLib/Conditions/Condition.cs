using System.Runtime.Serialization;

namespace CMissionLib.Conditions
{
	[DataContract]
	public abstract class Condition : TriggerLogic
	{
		public Condition(string name) : base(name) {}

		public string Category
		{
			get { return "Coditions"; }
		}
	}
}