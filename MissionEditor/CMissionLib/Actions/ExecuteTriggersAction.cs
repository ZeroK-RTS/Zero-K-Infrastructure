using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ExecuteTriggersAction : TriggersAction
	{
		public ExecuteTriggersAction()
			: base("Execute Triggers") {}
	}
}