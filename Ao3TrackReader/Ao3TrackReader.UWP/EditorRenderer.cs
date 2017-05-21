using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(Editor), typeof(Ao3TrackReader.UWP.EditorRenderer))]
namespace Ao3TrackReader.UWP
{
    class EditorRenderer : Xamarin.Forms.Platform.UWP.EditorRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    base.OnElementChanged(e);
                    Control.Style = Windows.UI.Xaml.Application.Current.Resources["MLFormsTextBoxStyle"] as Windows.UI.Xaml.Style;
                    return;
                }
            }

            base.OnElementChanged(e);
        }
    }
}
