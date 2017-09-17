using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(Ao3TrackReader.Controls.ScrollView), typeof(Ao3TrackReader.UWP.ScrollViewRenderer))]

namespace Ao3TrackReader.UWP
{
    public class ScrollViewRenderer : Xamarin.Forms.Platform.UWP.ScrollViewRenderer
    {        
        protected override void OnElementChanged(ElementChangedEventArgs<ScrollView> e)
        {
            bool controlWasNull = Control == null;

            base.OnElementChanged(e);

            if (Control != null && controlWasNull)
                Control.ViewChanged += Control_ViewChanged;
        }

        private void Control_ViewChanged(object sender, Windows.UI.Xaml.Controls.ScrollViewerViewChangedEventArgs e)
        {
            if (!e.IsIntermediate) (Element as Ao3TrackReader.Controls.ScrollView).SetScrollEnd();
        }
    }
}
