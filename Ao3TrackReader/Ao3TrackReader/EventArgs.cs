using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader
{
    public class EventArgs<T> : System.EventArgs
    {
        public T Value { get; private set; }

        public EventArgs(T value)
        {
            Value = value;
        }

        public new static EventArgs<T> Empty => new EventArgs<T>(default(T));

    }
}
