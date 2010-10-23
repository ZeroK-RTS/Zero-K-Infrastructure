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
		public ExecuteRandomTriggerAction()
			: base("Execute Random Trigger") { }
	}
}
