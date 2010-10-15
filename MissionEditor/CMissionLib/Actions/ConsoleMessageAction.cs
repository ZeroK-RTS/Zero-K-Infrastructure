using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ConsoleMessageAction : Action
	{
		string message;

		public ConsoleMessageAction(string message)
			: base("Console Message")
		{
			this.Message = message;
		}

		[DataMember]
		public string Message
		{
			get { return message; }
			set
			{
				message = value;
				RaisePropertyChanged("Message");
			}
		}

		public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"message", Message},
				};
			return new LuaTable(map);
		}
	}
}