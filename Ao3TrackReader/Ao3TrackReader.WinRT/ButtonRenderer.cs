﻿/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
using XButtonRenderer = Xamarin.Forms.Platform.UWP.ButtonRenderer;
#else
using Xamarin.Forms.Platform.WinRT;
using XButtonRenderer = Xamarin.Forms.Platform.WinRT.ButtonRenderer;
#endif
using XButton = Xamarin.Forms.Button;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Resources.Core;

[assembly: ExportRenderer(typeof(Ao3TrackReader.Controls.Button), typeof(Ao3TrackReader.ButtonRenderer))]
namespace Ao3TrackReader
{
    class ButtonRenderer : XButtonRenderer
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
            try
            {
                scale = int.Parse(res?.GetQualifierValue("scale"));
            }
            catch
            {
            }

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