using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UWP;
using ScrollOrientation = Xamarin.Forms.ScrollOrientation;
using XScrollView = Xamarin.Forms.ScrollView;
using Ao3TrackReader.Controls;
using Windows.UI.Xaml.Controls;

[assembly: ExportRenderer(typeof(ScrollView), typeof(Ao3TrackReader.UWP.ScrollViewRenderer))]

namespace Ao3TrackReader.UWP
{
    public class ScrollViewRenderer : Xamarin.Forms.Platform.UWP.ScrollViewRenderer
    {        
        protected override void OnElementChanged(ElementChangedEventArgs<XScrollView> e)
        {
            bool controlWasNull = Control == null;

            base.OnElementChanged(e);

            if (Control != null && controlWasNull)
            {
                Control.ViewChanged += Control_ViewChanged;
                UpdateOrientation();
            }
        }

        private void Control_ViewChanged(object sender, Windows.UI.Xaml.Controls.ScrollViewerViewChangedEventArgs e)
        {
            if (!e.IsIntermediate) (Element as Ao3TrackReader.Controls.ScrollView).SetScrollEnd();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == ScrollView.OrientationProperty.PropertyName || e.PropertyName == ScrollView.HideScrollbarsProperty.PropertyName)
            {
                UpdateOrientation();
            }
        }

        void UpdateOrientation()
        {
            var elem = Element as ScrollView;
            if (elem.Orientation == ScrollOrientation.Horizontal || elem.Orientation == ScrollOrientation.Both)
            {

                Control.HorizontalScrollBarVisibility = elem.HideScrollbars ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto;
            }
            else
            {
                Control.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

            if (elem.Orientation == ScrollOrientation.Vertical || elem.Orientation == ScrollOrientation.Both)
            {
                Control.VerticalScrollBarVisibility = elem.HideScrollbars ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Auto;
            }
            else
            {
                Control.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            }

        }
    }
}
