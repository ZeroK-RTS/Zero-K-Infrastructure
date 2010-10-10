using System;
using System.Collections.Generic;
using System.Text;

namespace PlasmaShared
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
}
