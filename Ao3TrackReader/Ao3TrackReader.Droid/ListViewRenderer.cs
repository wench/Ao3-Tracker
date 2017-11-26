using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Xamarin.Forms.Platform.Android;
using Xamarin.Forms;

using AListView = Android.Widget.ListView;
using AView = Android.Views.View;

[assembly: ExportRenderer(typeof(Xamarin.Forms.ListView), typeof(Ao3TrackReader.Droid.ListViewRenderer))]
namespace Ao3TrackReader.Droid
{
    public class MyListView : AListView
    {
        public MyListView(Context context) : base(context)
        {
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {            
            foreach (var scroll in this.GetParentOfType<ScrollViewRenderer>())
            {
                if (scroll.ScrollState == ScrollState.TouchScroll)
                    return false;
            }

            return base.OnInterceptTouchEvent(ev);
        }

        public override bool OnTouchEvent(MotionEvent ev)
        {
            foreach (var scroll in this.GetParentOfType<ScrollViewRenderer>())
            {
                if (scroll.ScrollState == ScrollState.TouchScroll)
                    return false;
            }

            return base.OnTouchEvent(ev);
        }
    }

    public class ListViewRenderer : Xamarin.Forms.Platform.Android.ListViewRenderer, AbsListView.IOnScrollListener
    {
        public ScrollState ScrollState { get; private set; } = ScrollState.Idle;

        public ListViewRenderer(Context context) : base(context)
        {
        }

        [Obsolete]
        public ListViewRenderer()
        {
        }

        protected override AListView CreateNativeControl()
        {
            var listview = new MyListView(Context);

            listview.SetOnScrollListener(this);
            return listview;
        }


        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.ListView> e)
        {
            base.OnElementChanged(e);
        }

        void AbsListView.IOnScrollListener.OnScroll(AbsListView view, int firstVisibleItem, int visibleItemCount, int totalItemCount)
        {
        }

        void AbsListView.IOnScrollListener.OnScrollStateChanged(AbsListView view, ScrollState scrollState)
        {
            ScrollState = scrollState;
        }
    }
}