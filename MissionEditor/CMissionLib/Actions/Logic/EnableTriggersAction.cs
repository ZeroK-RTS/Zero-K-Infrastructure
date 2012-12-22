using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class EnableTriggersAction : TriggersAction
	{
		public override string GetDefaultName()
		{
			return "Enable Triggers";
		}
	}
}