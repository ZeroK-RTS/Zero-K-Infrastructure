using System;

namespace CMissionLib
{
	public class EventArgs<T> : EventArgs
	{
		readonly T eventData;

		public T Data { get { return eventData; } }

		public EventArgs(T data)
		{
			eventData = data;
		}
	}
}