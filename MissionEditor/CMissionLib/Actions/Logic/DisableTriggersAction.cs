using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class DisableTriggersAction : TriggersAction
	{

		public override string GetDefaultName()
		{
			return "Disable Triggers";
		}
	}
}