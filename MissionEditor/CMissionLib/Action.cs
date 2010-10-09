using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public abstract class Action : TriggerLogic
	{
		public Action(string name) : base(name)
		{
		}

		string Category
		{
			get { return "Actions"; }
		}
	}
}