using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class EnableTriggersAction : TriggersAction
	{
		public EnableTriggersAction()
			: base("Enable Triggers") {}
	}
}