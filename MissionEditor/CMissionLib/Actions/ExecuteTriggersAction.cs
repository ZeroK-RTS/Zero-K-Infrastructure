using System;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ExecuteTriggersAction : TriggersAction
	{
		public override string GetDefaultName()
		{
			return "Execute Triggers";
		}
	}
}