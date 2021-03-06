﻿using System;
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
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using ScrollOrientation = Xamarin.Forms.ScrollOrientation;
using ScrollToMode = Xamarin.Forms.ScrollToMode;
using Point = Xamarin.Forms.Point;
using VisualElement = Xamarin.Forms.VisualElement;
using Android.Animation;

using ScrollView = Ao3TrackReader.Controls.ScrollView;
using XScrollView = Xamarin.Forms.ScrollView;
using AScrollView = Android.Widget.ScrollView;
using AView = Android.Views.View;
using System.ComponentModel;
using Android.Graphics;

[assembly: Xamarin.Forms.ExportRenderer(typeof(Ao3TrackReader.Controls.ScrollView), typeof(Ao3TrackReader.Droid.ScrollViewRenderer))]
namespace Ao3TrackReader.Droid
{
    public class ScrollViewRenderer : AScrollView, IVisualElementRenderer, IEffectControlProvider
    {
        public ScrollState ScrollState { get; private set; } = ScrollState.Idle;

        ScrollViewContainer _container;
        AHorizontalScrollView _hScrollView;
        bool _isAttached;
        internal bool ShouldSkipOnTouch;
        bool _isBidirectional;
        ScrollView _view;
        int _previousBottom;
        bool _isEnabled;

        public ScrollViewRenderer(Context context) : base(context)
        {
            SmoothScrollingEnabled = false;
        }

        [Obsolete()]
        public ScrollViewRenderer() : base(Forms.Context)
        {
            SmoothScrollingEnabled = false;
        }


        protected IScrollViewController Controller
        {
            get { return (IScrollViewController)Element; }
        }

        internal float LastX { get; set; }

        internal float LastY { get; set; }

        public VisualElement Element
        {
            get { return _view; }
        }

        public event EventHandler<VisualElementChangedEventArgs> ElementChanged;

        event EventHandler<PropertyChangedEventArgs> ElementPropertyChanged;
        event EventHandler<PropertyChangedEventArgs> IVisualElementRenderer.ElementPropertyChanged
        {
            add { ElementPropertyChanged += value; }
            remove { ElementPropertyChanged -= value; }
        }

        public SizeRequest GetDesiredSize(int widthConstraint, int heightConstraint)
        {
            Measure(widthConstraint, heightConstraint);
            return new SizeRequest(new Size(MeasuredWidth, MeasuredHeight), new Size(40, 40));
        }

        public void SetElement(VisualElement element)
        {
            ScrollView oldElement = _view;
            _view = (ScrollView)element;

            if (oldElement != null)
            {
                oldElement.PropertyChanged -= HandlePropertyChanged;
                _view.ScrollToRequested -= OnScrollToRequested;
            }
            if (element != null)
            {
                OnElementChanged(new VisualElementChangedEventArgs(oldElement, element));

                if (_container == null)
                {
                    Tracker = new VisualElementTracker(this);
                    _container = new ScrollViewContainer(_view, Context);
                }

                _view.PropertyChanged += HandlePropertyChanged;
                _view.ScrollToRequested += OnScrollToRequested;

                LoadContent();
                UpdateBackgroundColor();
                UpdateOrientation();
                UpdateIsEnabled();

                //element.SendViewInitialized(this);

                if (!string.IsNullOrEmpty(element.AutomationId))
                    ContentDescription = element.AutomationId;
            }

            Xamarin.Forms.Internals.EffectUtilities.RegisterEffectControlProvider(this, oldElement, element);
        }

        public VisualElementTracker Tracker { get; private set; }

        public void UpdateLayout()
        {
            if (Tracker != null)
                Tracker.UpdateLayout();
        }

        public ViewGroup ViewGroup => this;

        AView IVisualElementRenderer.View => this;

        public override void Draw(Canvas canvas)
        {
            canvas.ClipRect(canvas.ClipBounds);

            base.Draw(canvas);
        }


        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (Element.InputTransparent)
                return false;

            foreach (var listview in this.GetChildrenOfType<ListViewRenderer>())
            {
                if (listview.ScrollState == ScrollState.TouchScroll)
                    return false;
            }

            if (_view.Orientation == ScrollOrientation.Horizontal)
                return false;

            // set the start point for the bidirectional scroll; 
            // Down is swallowed by other controls, so we'll just sneak this in here without actually preventing
            // other controls from getting the event.			
            if (_isBidirectional && ev.Action == MotionEventActions.Down)
            {
                LastY = ev.RawY;
                LastX = ev.RawX;
            }

            return base.OnInterceptTouchEvent(ev);
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            if (!_isEnabled)
                return false;

            foreach (var listview in this.GetChildrenOfType<ListViewRenderer>())
            {
                if (listview.ScrollState == ScrollState.TouchScroll)
                    return false;
            }

            if (ev.ActionIndex == 0)
            {
                if (ev.Action == MotionEventActions.Down || ev.Action == MotionEventActions.Move)
                {
                    touching = true;
                }
                else if (ev.Action == MotionEventActions.Up)
                {
                    touching = false;
                    ScrollState = ScrollState.Fling;
                    OnScrollAction();
                }
            }

            if (_view.Orientation == ScrollOrientation.Horizontal)
                return false;

            if (ShouldSkipOnTouch)
            {
                ShouldSkipOnTouch = false;
                return false;
            }



            // The nested ScrollViews will allow us to scroll EITHER vertically OR horizontally in a single gesture.
            // This will allow us to also scroll diagonally.
            // We'll fall through to the base event so we still get the fling from the ScrollViews.
            // We have to do this in both ScrollViews, since a single gesture will be owned by one or the other, depending
            // on the initial direction of movement (i.e., horizontal/vertical).
            if (_isBidirectional && !Element.InputTransparent)
            {
                float dX = LastX - ev.RawX;
                float dY = LastY - ev.RawY;
                LastY = ev.RawY;
                LastX = ev.RawX;
                if (ev.Action == MotionEventActions.Move)
                {
                    ScrollBy(0, (int)dY);
                    foreach (AHorizontalScrollView child in this.GetChildrenOfType<AHorizontalScrollView>())
                    {
                        child.ScrollBy((int)dX, 0);
                        break;
                    }
                }
            }
            return base.OnTouchEvent(ev);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            SetElement(null);

            if (disposing)
            {
                Tracker.Dispose();
                Tracker = null;
                RemoveAllViews();
                _container.Dispose();
                _container = null;
            }
        }

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

        protected virtual void OnElementChanged(VisualElementChangedEventArgs e)
        {
            EventHandler<VisualElementChangedEventArgs> changed = ElementChanged;
            if (changed != null)
                changed(this, e);
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            // If the scroll view has changed size because of soft keyboard dismissal
            // (while WindowSoftInputModeAdjust is set to Resize), then we may need to request a 
            // layout of the ScrollViewContainer
            bool requestContainerLayout = bottom > _previousBottom;
            _previousBottom = bottom;

            base.OnLayout(changed, left, top, right, bottom);
            if (_view.Content != null && _hScrollView != null)
                _hScrollView.Layout(0, 0, right - left, Math.Max(bottom - top, (int)Context.ToPixels(_view.Content.Height)));
            else if (_view.Content != null && requestContainerLayout)
                _container?.RequestLayout();
        }

        protected override void OnScrollChanged(int l, int t, int oldl, int oldt)
        {
            base.OnScrollChanged(l, t, oldl, oldt);
            var context = Context;
            UpdateScrollPosition(context.FromPixels(l), context.FromPixels(t));
        }

        internal void UpdateScrollPosition(double x, double y)
        {
            if (_view != null)
            {
                if (_view.Orientation == ScrollOrientation.Both)
                {
                    var context = Context;

                    if (x == 0)
                        x = context.FromPixels(_hScrollView.ScrollX);

                    if (y == 0)
                        y = context.FromPixels(ScrollY);
                }

                Controller.SetScrolledPosition(x, y);
            }
        }

        void IEffectControlProvider.RegisterEffect(Effect effect)
        {
            var platformEffect = effect as PlatformEffect;
            if (platformEffect != null)
                OnRegisterEffect(platformEffect);
        }

        void OnRegisterEffect(PlatformEffect effect)
        {
            effect.SetContainer(this);
            effect.SetControl(this);
        }

        static int GetDistance(double start, double position, double v)
        {
            return (int)(start + (position - start) * v);
        }

        void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ElementPropertyChanged?.Invoke(this, e);

            if (e.PropertyName == "Content")
                LoadContent();
            else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
                UpdateBackgroundColor();
            else if (e.PropertyName == ScrollView.OrientationProperty.PropertyName)
                UpdateOrientation();
            else if (e.PropertyName == VisualElement.IsEnabledProperty.PropertyName)
                UpdateIsEnabled();
            else if (e.PropertyName == Xamarin.Forms.ScrollView.ScrollXProperty.PropertyName || e.PropertyName == Xamarin.Forms.ScrollView.ScrollYProperty.PropertyName)
                OnScrollAction();
        }

        void UpdateIsEnabled()
        {
            if (Element == null)
            {
                return;
            }

            _isEnabled = Element.IsEnabled;
        }

        void LoadContent()
        {
            _container.ChildView = _view.Content;
        }

        private async void OnScrollToRequested(object sender, Controls.ScrollToRequestedEventArgs e)
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
                ScrollState = ScrollState.Fling;

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                Device.StartTimer(TimeSpan.FromMilliseconds(16), () =>
                {
                    if (_view == null || e.Notifier.Task.IsCanceled == true)
                    {
                        sw.Stop();
                        return false;
                    }

                    var v = Math.Min(sw.ElapsedMilliseconds / 250.0, 1.0);

                    int distX = GetDistance(currentX, x, v);
                    int distY = GetDistance(currentY, y, v);

                    distY = Math.Max(0,Math.Min(distY, (int)context.ToPixels(_view.ContentSize.Height) - Height));
                    distX = Math.Max(0, Math.Min(distX, (int)context.ToPixels(_view.ContentSize.Width) - Width));
                    
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

                    if (v == 1.0)
                    {
                        ScrollState = ScrollState.Idle;
                        e.Notifier.TrySetResult(true);
                        return false;
                    }
                    return true;
                });
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
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() => e.Notifier.TrySetResult(true));
            }
        }

        bool touching = false;
        int actionIndex = 0;

        private async void OnScrollAction()
        {
            var idx = ++actionIndex;

            if (touching) ScrollState = ScrollState.TouchScroll;

            await Task.Delay(100);

            if (idx != actionIndex)
            {
                return;
            }

            if (!touching)
            {
                ScrollState = ScrollState.Idle;

                if (Element is Controls.ScrollView sv)
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(() => sv.SetScrollEnd());
                }
            }
        }

        void IVisualElementRenderer.SetLabelFor(int? id)
        {
        }

        void UpdateBackgroundColor()
        {
            SetBackgroundColor(Element.BackgroundColor.ToAndroid(Xamarin.Forms.Color.Transparent));
        }

        void UpdateOrientation()
        {
            if (_view.Orientation == ScrollOrientation.Horizontal || _view.Orientation == ScrollOrientation.Both)
            {
                if (_hScrollView == null)
                {
                    _hScrollView = new AHorizontalScrollView(Context, this);
                    _hScrollView.SmoothScrollingEnabled = false;
                }

                (_hScrollView).IsBidirectional = _isBidirectional = _view.Orientation == ScrollOrientation.Both;

                if (_hScrollView.Parent != this)
                {
                    _container.RemoveFromParent();
                    _hScrollView.AddView(_container);
                    AddView(_hScrollView);
                }
            }
            else
            {
                if (_container.Parent != this)
                {
                    _container.RemoveFromParent();
                    if (_hScrollView != null)
                        _hScrollView.RemoveFromParent();
                    AddView(_container);
                }
            }
        }
    }
}