using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class DisableTriggersAction : TriggersAction
	{
		public DisableTriggersAction()
			: base("Disable Triggers") {}
	}
}