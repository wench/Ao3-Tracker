using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.UWP;
using XButton = Xamarin.Forms.Button;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Resources.Core;

[assembly: ExportRenderer(typeof(Ao3TrackReader.Controls.Button), typeof(Ao3TrackReader.UWP.ButtonRenderer))]
namespace Ao3TrackReader.UWP
{
    class ButtonRenderer : Xamarin.Forms.Platform.UWP.ButtonRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<XButton> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                FixImageSize();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == XButton.TextProperty.PropertyName || e.PropertyName == XButton.ImageProperty.PropertyName)
            {
                FixImageSize();
            }
        }

        void FixImageSize()
        {
            Image image = Control.Content as Image;
            if (image == null)
            {
                StackPanel container = Control.Content as StackPanel;
                if (container == null) return;
                foreach (var c in container.Children)
                {
                    image = c as Image;
                    if (c != null) break;
                }
            }

            if (image == null) return;

            BitmapImage bmp = image.Source as BitmapImage;
            ResourceMap resMap = ResourceManager.Current.MainResourceMap.GetSubtree("Files");
            ResourceContext resContext = ResourceContext.GetForCurrentView();
            var res = resMap.GetValue(bmp.UriSource.AbsolutePath.TrimStart('/'), resContext);
            int scale = 100;
            if (!int.TryParse(res?.GetQualifierValue("scale"), out scale)) scale = 100;

            var element = Element as Ao3TrackReader.Controls.Button;

            if (element.ImageWidth > 0) image.Width = element.ImageWidth;
            if (element.ImageHeight > 0) image.Height = element.ImageHeight;

            bmp.ImageOpened += (sender, args) =>
            {
                if (element.ImageWidth > 0) image.Width = element.ImageWidth;
                else image.Width = bmp.PixelWidth * 100 / scale;
                if (element.ImageHeight > 0) image.Height = element.ImageHeight;
                else image.Height = bmp.PixelHeight * 100 / scale;
                (Element as Xamarin.Forms.IVisualElementController).InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
            };
        }
    }
}
