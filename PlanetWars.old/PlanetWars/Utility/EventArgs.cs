using System;
using System.ComponentModel;

namespace PlanetWars.Utility
{
    public class ChangeEventArgs<T> : EventArgs
    {
        readonly T newValue;
        readonly T oldValue;

        public ChangeEventArgs(T oldValue, T newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
        }

        public T OldValue
        {
            get { return oldValue; }
        }

        public T NewValue
        {
            get { return newValue; }
        }
    }

    public class EventArgs<T> : EventArgs
    {
        readonly T value;

        public EventArgs(T value)
        {
            this.value = value;
        }

        public T Value
        {
            get { return value; }
        }
    }

    public class DoWorkEventArgs<TIn, TOut> : CancelEventArgs
    {
        readonly TIn arg;

        public DoWorkEventArgs(TIn argument)
        {
            arg = argument;
        }

        public TIn Argument
        {
            get { return arg; }
        }

        public TOut Result { get; set; }

        public new bool Cancel { get; set; }
    }

    public class RunWorkerCompletedEventArgs<T> : AsyncCompletedEventArgs
    {
        readonly T result;

        public RunWorkerCompletedEventArgs(T result, Exception error, bool canceled) : base(error, canceled, null)
        {
            this.result = result;
        }

        public T Result
        {
            get
            {
                RaiseExceptionIfNecessary();
                return result;
            }
        }

        public new object UserState { get; set; }
    }
}