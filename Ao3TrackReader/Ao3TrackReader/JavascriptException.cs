using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader
{
    public class JavascriptException : Exception
    {
        public JavascriptException() : base()
        {

        }
        public JavascriptException(string message, string stackTrace) : base(message)
        {
            this.stackTrace = stackTrace;
        }
        public string Name { get; set; }
        public string Url { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        string stackTrace = null;
        public override string StackTrace => stackTrace;
    }
}
