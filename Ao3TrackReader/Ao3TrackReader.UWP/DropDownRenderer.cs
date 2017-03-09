using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.UWP;
using Windows.UI.Xaml.Controls;
using DropDown = Ao3TrackReader.Controls.DropDown;
using System.ComponentModel;
using Xamarin.Forms;
using Windows.Foundation;

[assembly: ExportRenderer(typeof(DropDown), typeof(Ao3TrackReader.UWP.DropDownRenderer))]
namespace Ao3TrackReader.UWP
{
    class DropDownRenderer : ViewRenderer<DropDown, ComboBox>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<DropDown> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(new Windows.UI.Xaml.Controls.ComboBox());
                Control.SelectionChanged += Control_SelectionChanged;
            }

            if (e.OldElement != null)
            {
                Element.ItemSelected -= Element_ItemSelected;
                Control.ItemsSource = null;
            }

            if (e.NewElement != null)
            {
                Control.ItemsSource = Element.ItemsSource;
                Control.SelectedItem = Element.SelectedItem;
                Element.ItemSelected += Element_ItemSelected;
            }
        }

        private void Element_ItemSelected(object sender, Xamarin.Forms.SelectedItemChangedEventArgs e)
        {
            Control.SelectedItem = e.SelectedItem;
        }

        private void Control_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Element != null) Element.OnItemSelected(Control.SelectedItem);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == DropDown.ItemsSourceProperty.PropertyName)
            {
                Control.ItemsSource = Element.ItemsSource;
            }
        }

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            var ret = base.GetDesiredSize(widthConstraint, heightConstraint);
            return ret;
        }

        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            var ret = base.MeasureOverride(availableSize);
            return ret;
        }
        protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
        {
            var ret = base.ArrangeOverride(finalSize);
            return ret;
        }
    }
}
