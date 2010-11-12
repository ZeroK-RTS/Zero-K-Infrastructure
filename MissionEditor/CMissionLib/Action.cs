using System.Runtime.Serialization;

namespace CMissionLib
{
	[DataContract]
	public abstract class Action : TriggerLogic
	{
		string Category
		{
			get { return "Actions"; }
		}
	}
}