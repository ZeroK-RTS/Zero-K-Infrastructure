using System;
using System.Runtime.Serialization;

namespace CMissionLib.UnitSyncLib
{
	[Serializable]
	public class UnitSyncException : Exception
	{
		public UnitSyncException() {}

		public UnitSyncException(string message) : base(message) {}

		public UnitSyncException(string message, Exception exception) : base(message, exception) {}

		protected UnitSyncException(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext) {}
	}
}