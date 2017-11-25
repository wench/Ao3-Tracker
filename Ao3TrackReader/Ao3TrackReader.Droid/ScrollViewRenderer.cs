using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms.Platform.Android;
using ScrollOrientation = Xamarin.Forms.ScrollOrientation;
using ScrollToMode = Xamarin.Forms.ScrollToMode;
using Point = Xamarin.Forms.Point;
using VisualElement = Xamarin.Forms.VisualElement;
using Android.Animation;

[assembly: Xamarin.Forms.ExportRenderer(typeof(Ao3TrackReader.Controls.ScrollView), typeof(Ao3TrackReader.Droid.ScrollViewRenderer))]
namespace Ao3TrackReader.Droid
{
    public class ScrollViewRenderer : Xamarin.Forms.Platform.Android.ScrollViewRenderer
    {
        public ScrollViewRenderer(Context context) : base(context)
		{
            (this as IVisualElementRenderer).ElementPropertyChanged += ElementPropertyChanged;
        }

        [Obsolete]
        public ScrollViewRenderer() : base()
        {
            (this as IVisualElementRenderer).ElementPropertyChanged += ElementPropertyChanged;
        }

        Controls.ScrollView _view => Element as Controls.ScrollView;

        HorizontalScrollView HScroll
        {
            get
            {
                for (int i = 0; i < ChildCount; i++)
                {
                    if (GetChildAt(i) is HorizontalScrollView hsv)
                    {
                        return hsv;
                    }
                }
                return null;
            }
        }

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                ((Controls.ScrollView)e.OldElement).ScrollToRequested -= ScrollToRequested;
            }
            if (e.NewElement != null)
            {
                ((Controls.ScrollView)e.NewElement).ScrollToRequested += ScrollToRequested;
            }
        }

        bool _isAttached;

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();

            _isAttached = true;
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();

            _isAttached = false;
        }

        static int GetDistance(double start, double position, double v)
        {
            return (int)(start + (position - start) * v);
        }

        private async void ScrollToRequested(object sender, Controls.ScrollToRequestedEventArgs e)
        {
            if (!_isAttached)
            {
                return;
            }

            // 99.99% of the time simply queuing to the end of the execution queue should handle this case.
            // However it is possible to end a layout cycle and STILL be layout requested. We want to
            // back off until all are done, even if they trigger layout storms over and over. So we back off
            // for 10ms tops then move on.
            var cycle = 0;
            while (IsLayoutRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1));
                cycle++;

                if (cycle >= 10)
                    break;
            }

            var _hScrollView = HScroll;

            var context = Context;
            var x = (int)context.ToPixels(e.ScrollX);
            var y = (int)context.ToPixels(e.ScrollY);
            int currentX = _view.Orientation == ScrollOrientation.Horizontal || _view.Orientation == ScrollOrientation.Both ? _hScrollView.ScrollX : ScrollX;
            int currentY = _view.Orientation == ScrollOrientation.Vertical || _view.Orientation == ScrollOrientation.Both ? ScrollY : _hScrollView.ScrollY;
            if (e.Mode == ScrollToMode.Element)
            {
                Point itemPosition = Controller.GetScrollPositionForElement(e.Element as VisualElement, e.Position);

                x = (int)context.ToPixels(itemPosition.X);
                y = (int)context.ToPixels(itemPosition.Y);
            }
            if (e.ShouldAnimate) 
            {
                ValueAnimator animator = ValueAnimator.OfFloat(0f, 1f);
                animator.SetDuration(500);
                animator.Update += (o, animatorUpdateEventArgs) =>
                {
                    var v = (double)animatorUpdateEventArgs.Animation.AnimatedValue;
                    int distX = GetDistance(currentX, x, v);
                    int distY = GetDistance(currentY, y, v);

                    distY = Math.Max(0,Math.Min(distY, (int)context.ToPixels(_view.ContentSize.Height) - Height));
                    distX = Math.Max(0, Math.Min(distX, (int)context.ToPixels(_view.ContentSize.Width) - Width));
                    
                    if (_view == null || e.Task?.IsCanceled == true)
                    {
                        // This is probably happening because the page with this Scroll View
                        // was popped off the stack during animation
                        animator.Cancel();
                        return;
                    }

                    switch (_view.Orientation)
                    {
                        case ScrollOrientation.Horizontal:
                            _hScrollView.ScrollTo(distX, distY);
                            break;
                        case ScrollOrientation.Vertical:
                            ScrollTo(distX, distY);
                            break;
                        default:
                            _hScrollView.ScrollTo(distX, distY);
                            ScrollTo(distX, distY);
                            break;
                    }
                };
                animator.AnimationEnd += delegate
                {
                    if (_view == null) return;
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() => _view.SendScrollFinished());
                };

                animator.Start();
            }
            else
            {
                y = Math.Max(0, Math.Min(y, (int)context.ToPixels(_view.ContentSize.Height) - Height));
                x = Math.Max(0, Math.Min(x, (int)context.ToPixels(_view.ContentSize.Width) - Width));

                switch (_view.Orientation)
                {
                    case ScrollOrientation.Horizontal:
                        _hScrollView.ScrollTo(x, y);
                        break;
                    case ScrollOrientation.Vertical:
                        ScrollTo(x, y);
                        break;
                    default:
                        _hScrollView.ScrollTo(x, y);
                        ScrollTo(x, y);
                        break;
                }
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() => _view.SendScrollFinished());
            }
        }

        bool touching = false;
        int actionIndex = 0;

        private async void OnScrollAction()
        {
            var idx = ++actionIndex;

            await Task.Delay(100);

            if (idx != actionIndex)
                return;

            if (!touching)
            {
                if (Element is Controls.ScrollView sv)
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() => sv.SetScrollEnd());
                }
            }
        }

        private void ElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Xamarin.Forms.ScrollView.ScrollXProperty.PropertyName || e.PropertyName == Xamarin.Forms.ScrollView.ScrollYProperty.PropertyName)
            {
                var hscroll = HScroll;
                OnScrollAction();
            }
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            if (ev.ActionIndex == 0)
            {
                if (ev.Action == MotionEventActions.Move)
                {
                    touching = true;
                }
                else if (ev.Action == MotionEventActions.Up)
                {
                    touching = false;
                    OnScrollAction();
                }
            }
            return base.OnTouchEvent(ev);
        }
    }
}