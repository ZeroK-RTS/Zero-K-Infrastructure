using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ExecuteRandomTriggerAction : TriggersAction
	{
		public override string GetDefaultName()
		{
			return "Execute Random Trigger";
		}
	}
}
