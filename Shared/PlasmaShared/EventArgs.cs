using System;
using System.ComponentModel;

namespace PlasmaShared
{
    public class EventArgs<T>: EventArgs
    {
        readonly T eventData;

        public T Data { get { return eventData; } }

        public EventArgs(T data)
        {
            eventData = data;
        }
    }

    public class CancelEventArgs<T>: CancelEventArgs
    {
        readonly T eventData;

        public T Data { get { return eventData; } }

        public CancelEventArgs(T data)
        {
            eventData = data;
        }
    }
}