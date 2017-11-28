using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xamarin.Forms;

using System.Threading.Tasks;

namespace Ao3TrackReader.Controls
{
    public class ScrollToRequestedEventArgs
    {
        public ScrollToRequestedEventArgs(double scrollX, double scrollY, bool shouldAnimate)
        {
            ScrollX = scrollX;
            ScrollY = scrollY;
            ShouldAnimate = shouldAnimate;
            Mode = ScrollToMode.Position;
        }

        public ScrollToRequestedEventArgs(Element element, ScrollToPosition position, bool shouldAnimate)
        {
            Element = element;
            Position = position;
            ShouldAnimate = shouldAnimate;
            Mode = ScrollToMode.Element;
        }

        public Element Element { get; private set; }

        public ScrollToMode Mode { get; private set; }

        public ScrollToPosition Position { get; private set; }

        public double ScrollX { get; private set; }

        public double ScrollY { get; private set; }

        public bool ShouldAnimate { get; private set; }

        public TaskCompletionSource<bool> Notifier { get; set; }

    }

    public partial class ScrollView : Xamarin.Forms.ScrollView
    {
        TaskCompletionSource<bool> _scrollCompletionSource;

        public new Task ScrollToAsync(double x, double y, bool animated)
        {
            var args = new ScrollToRequestedEventArgs(x, y, animated);
            OnScrollToRequested(args);
            return _scrollCompletionSource.Task;
        }

        public new Task ScrollToAsync(Element element, ScrollToPosition position, bool animated)
        {
            if (!Enum.IsDefined(typeof(ScrollToPosition), position))
                throw new ArgumentException("position is not a valid ScrollToPosition", "position");

            if (element == null)
                throw new ArgumentNullException("element");

            if (!CheckElementBelongsToScrollViewer(element))
                throw new ArgumentException("element does not belong to this ScrollView", "element");

            var args = new ScrollToRequestedEventArgs(element, position, animated);
            OnScrollToRequested(args);
            return _scrollCompletionSource.Task;
        }

        bool CheckElementBelongsToScrollViewer(Element element)
        {
            return Equals(element, this) || element.RealParent != null && CheckElementBelongsToScrollViewer(element.RealParent);
        }

        void CheckTaskCompletionSource()
        {
            if (_scrollCompletionSource != null && _scrollCompletionSource.Task.Status == TaskStatus.Running)
            {
                _scrollCompletionSource.TrySetCanceled();
            }
            _scrollCompletionSource = new TaskCompletionSource<bool>();
        }

        void OnScrollToRequested(ScrollToRequestedEventArgs e)
        {
            CheckTaskCompletionSource();

            e.Notifier = _scrollCompletionSource;
            ScrollToRequested?.Invoke(this, e);
        }

        public new event EventHandler<ScrollToRequestedEventArgs> ScrollToRequested;
    }
}