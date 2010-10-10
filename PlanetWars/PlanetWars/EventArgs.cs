using System;

namespace PlanetWars
{
    public class EventArgs<T>: EventArgs
    {
        T value;

        public T Value
        {
            get { return value; }
        }

        public EventArgs(T value)
        {
            this.value = value;
        }
    }
}