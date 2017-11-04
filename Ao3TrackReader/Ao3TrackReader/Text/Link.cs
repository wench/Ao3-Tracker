using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Text
{
    public partial class A : Span
    {
        public A() : base()
        {
        }
        public A(IEnumerable<TextEx> from) : base(from)
        {
        }

        public Uri Href { get; set; }

        public void OnClick()
        {
            if (Click != null)
                Click(this, new EventArgs<Uri>(Href));
            else if (Href != null)
                WebViewPage.Current.Navigate(Href);
        }

        public event EventHandler<EventArgs<Uri>> Click;

    }

    public class Link : A
    {
        public Link() : base()
        {
        }
        public Link(IEnumerable<TextEx> from) : base(from)
        {
        }
    }
}
