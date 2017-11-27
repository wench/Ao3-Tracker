/*
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
using Button = Xamarin.Forms.Button;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Xaml.Media;

using Color = Xamarin.Forms.Color;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Xamarin.Forms;

using WThickness = Windows.UI.Xaml.Thickness;
using WButton = Windows.UI.Xaml.Controls.Button;
using WImage = Windows.UI.Xaml.Controls.Image;


[assembly: ExportRenderer(typeof(Ao3TrackReader.Controls.Button), typeof(Ao3TrackReader.WinRT.ButtonRenderer))]
namespace Ao3TrackReader.WinRT
{

    public class ActivatableButton : FormsButton
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
            nameof(IsActive), typeof(Boolean),
            typeof(ActivatableButton),
            new PropertyMetadata(false,(d,o)=>((ActivatableButton)d).PropertyChanged())
            );

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }


        public ActivatableButton()
        {
            IsEnabledChanged += (sender, args) => PropertyChanged();
            Loaded += (sender, args) => PropertyChanged();
        }

        private void PropertyChanged()
        {
            string state = "Ao3TNormal";

            if (IsActive)
                state = "Ao3TActive";
            else if (!IsEnabled)
                state = "Ao3TDisabled";
            else if (IsPressed)
                state = "Ao3TPressed";
            else if (IsPointerOver)
                state = "Ao3TPointerOver";

            VisualStateManager.GoToState(this, state, false);
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            PropertyChanged();
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            PropertyChanged();
        }
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
            PropertyChanged();
        }
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            PropertyChanged();
        }
    }


    class ButtonRenderer : ViewRenderer<Button, ActivatableButton>
    {
        bool _fontApplied;

        protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (Control == null)
                {
                    var button = new ActivatableButton();
                    button.Click += OnButtonClick;
                    button.AddHandler(PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
                    SetNativeControl(button);
                }

                var padding = (Element as Ao3TrackReader.Controls.Button).Padding;
                Control.Padding = new WThickness(padding.Left, padding.Top, padding.Right, padding.Bottom);

                UpdateContent();

                if (Element.BackgroundColor != Color.Default)
                    UpdateBackground();

                if (Element.TextColor != Color.Default)
                    UpdateTextColor();

                if (Element.BorderColor != Color.Default)
                    UpdateBorderColor();

                if (Element.BorderWidth != (double)Button.BorderWidthProperty.DefaultValue)
                    UpdateBorderWidth();

                if (Element.BorderRadius != (int)Button.BorderRadiusProperty.DefaultValue)
                    UpdateBorderRadius();

                UpdateFont();

                UpdateActive();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == Button.TextProperty.PropertyName || e.PropertyName == Button.ImageProperty.PropertyName || e.PropertyName == Controls.Button.ImageWidthProperty.PropertyName || e.PropertyName == Controls.Button.ImageHeightProperty.PropertyName)
            {
                UpdateContent();
            }
            else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
            {
                UpdateBackground();
            }
            else if (e.PropertyName == Button.TextColorProperty.PropertyName)
            {
                UpdateTextColor();
            }
            else if (e.PropertyName == Button.FontProperty.PropertyName)
            {
                UpdateFont();
            }
            else if (e.PropertyName == Button.BorderColorProperty.PropertyName)
            {
                UpdateBorderColor();
            }
            else if (e.PropertyName == Button.BorderWidthProperty.PropertyName)
            {
                UpdateBorderWidth();
            }
            else if (e.PropertyName == Button.BorderRadiusProperty.PropertyName)
            {
                UpdateBorderRadius();
            }
            else if (e.PropertyName == Controls.Button.IsActiveProperty.PropertyName)
            {
                UpdateActive();
            }
            else if (e.PropertyName == Controls.Button.PaddingProperty.PropertyName)
            {
                var padding = (Element as Ao3TrackReader.Controls.Button).Padding;
                Control.Padding = new WThickness(padding.Left, padding.Top, padding.Right, padding.Bottom);
            }
        }

        protected override void UpdateBackgroundColor()
        {
            // Button is a special case; we don't want to set the Control's background
            // because it goes outside the bounds of the Border/ContentPresenter, 
            // which is where we might change the BorderRadius to create a rounded shape.
            return;
        }

        protected override bool PreventGestureBubbling { get; set; } = true;

        void OnButtonClick(object sender, RoutedEventArgs e)
        {
            var buttonController = (IButtonController)Element;
            
            ((IButtonController)Element)?.SendReleased();
            ((IButtonController)Element)?.SendClicked();
        }

        void OnPointerPressed(object sender, RoutedEventArgs e)
        {
            ((IButtonController)Element)?.SendPressed();
        }

        void UpdateBackground()
        {
            Control.BackgroundColor = Element.BackgroundColor != Color.Default ? Element.BackgroundColor.ToWindowsBrush() : (Brush)Windows.UI.Xaml.Application.Current.Resources["ButtonBackgroundThemeBrush"];
        }

        void UpdateBorderColor()
        {
            Control.BorderBrush = Element.BorderColor != Color.Default ? Element.BorderColor.ToWindowsBrush() : (Brush)Windows.UI.Xaml.Application.Current.Resources["ButtonBorderThemeBrush"];
        }

        void UpdateBorderRadius()
        {
            Control.BorderRadius = Element.BorderRadius;
        }

        void UpdateBorderWidth()
        {
            Control.BorderThickness = Element.BorderWidth == (double)Button.BorderWidthProperty.DefaultValue ? new WThickness(3) : new WThickness(Element.BorderWidth);
        }

        void UpdateContent()
        {
            var text = Element.Text;
            var elementImage = Element.Image;

            // No image, just the text
            if (elementImage == null)
            {
                Control.Content = text;
                return;
            }

            // if (UWP.App.UniversalApi >= 4)
            //CompositionSurfaceBrush imageBrush = compositor.CreateSurfaceBrush();

            //LoadedImageSurface loadedSurface = LoadedImageSurface.StartLoadFromUri(new Uri("ms-appx:///Assets/myPic.jpg"), new Size(200.0, 400.0));

            FrameworkElement image;
            var element = Element as Ao3TrackReader.Controls.Button;

            ResourceMap resMap = ResourceManager.Current.MainResourceMap.GetSubtree("Files");
            ResourceContext resContext = ResourceContext.GetForCurrentView();
            var res = resMap.GetValue(elementImage.File, resContext);
            int scale = 100;
            try
            {
                scale = int.Parse(res?.GetQualifierValue("scale"));
            }
            catch
            {
            }

#if True
            var bmpicon = new BitmapIcon
            {
                UriSource = new Uri("ms-appx:///" + elementImage.File)
            };
            image = bmpicon;

            if (element.ImageWidth > 0) image.Width = element.ImageWidth;
            if (element.ImageHeight > 0) image.Height = element.ImageHeight;

            bmpicon.SizeChanged += (sender, e) =>
            {
                (Element as Xamarin.Forms.IVisualElementController).InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
            };
            UpdateIconColor();
#else
            var bmp = new BitmapImage(new Uri("ms-appx:///" + elementImage.File));

            image = new WImage
            {
                Source = bmp,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Stretch = Stretch.Uniform
            };

            if (element.ImageWidth > 0) image.Width = element.ImageWidth;
            if (element.ImageHeight > 0) image.Height = element.ImageHeight;

            bmp.ImageOpened += (sender, args) => {
                if (element.ImageWidth > 0) image.Width = element.ImageWidth;
                else image.Width = bmp.PixelWidth * 100 / scale;
                if (element.ImageHeight > 0) image.Height = element.ImageHeight;
                else image.Height = bmp.PixelHeight * 100 / scale;

                (Element as Xamarin.Forms.IVisualElementController).InvalidateMeasure(Xamarin.Forms.Internals.InvalidationTrigger.RendererReady);
            };

#endif
            // No text, just the image
            if (string.IsNullOrEmpty(text))
            {
                Control.Content = image;
                return;
            }

            // Both image and text, so we need to build a container for them
            Control.Content = CreateContentContainer(Element.ContentLayout, image, text);
        }

        static StackPanel CreateContentContainer(Button.ButtonContentLayout layout, FrameworkElement image, string text)
        {
            var container = new StackPanel();
            var textBlock = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var spacing = layout.Spacing;

            container.HorizontalAlignment = HorizontalAlignment.Center;
            container.VerticalAlignment = VerticalAlignment.Center;

            switch (layout.Position)
            {
                case Button.ButtonContentLayout.ImagePosition.Top:
                    container.Orientation = Orientation.Vertical;
                    image.Margin = new WThickness(0, 0, 0, spacing);
                    container.Children.Add(image);
                    container.Children.Add(textBlock);
                    break;
                case Button.ButtonContentLayout.ImagePosition.Bottom:
                    container.Orientation = Orientation.Vertical;
                    image.Margin = new WThickness(0, spacing, 0, 0);
                    container.Children.Add(textBlock);
                    container.Children.Add(image);
                    break;
                case Button.ButtonContentLayout.ImagePosition.Right:
                    container.Orientation = Orientation.Horizontal;
                    image.Margin = new WThickness(spacing, 0, 0, 0);
                    container.Children.Add(textBlock);
                    container.Children.Add(image);
                    break;
                default:
                    // Defaults to image on the left
                    container.Orientation = Orientation.Horizontal;
                    image.Margin = new WThickness(0, 0, spacing, 0);
                    container.Children.Add(image);
                    container.Children.Add(textBlock);
                    break;
            }

            return container;
        }

        void UpdateFont()
        {
            if (Control == null || Element == null)
                return;

            if (Element.Font == Font.Default && !_fontApplied)
                return;

            Font fontToApply = Element.Font == Font.Default ? Font.SystemFontOfSize(NamedSize.Medium) : Element.Font;

            Control.ApplyFont(fontToApply);
            _fontApplied = true;
        }

        void UpdateTextColor()
        {
            var element = Element as Ao3TrackReader.Controls.Button;
            //if (element.IsActive)
            //    Control.Foreground = Ao3TrackReader.Resources.Colors.Highlight.High.ToWindowsBrush();
            //else
                Control.Foreground = Element.TextColor != Color.Default ? Element.TextColor.ToWindowsBrush() : (Brush)Windows.UI.Xaml.Application.Current.Resources["DefaultTextForegroundThemeBrush"];
        }

        void UpdateIconColor()
        {
            BitmapIcon image = Control.Content as BitmapIcon;
            if (image == null)
            {
                StackPanel container = Control.Content as StackPanel;
                if (container == null) return;
                foreach (var c in container.Children)
                {
                    image = c as BitmapIcon;
                    if (image != null) break;
                }
            }
            if (image != null)
            {
                var element = Element as Ao3TrackReader.Controls.Button;
                if (element.IsActive)
                    image.Foreground = Ao3TrackReader.Resources.Colors.Highlight.High.ToWindowsBrush();
                else
                    image.Foreground = null;
            }
        }

        void UpdateActive()
        {
            Control.IsActive = (Element as Controls.Button).IsActive;
            UpdateIconColor();
        }
    }
}
