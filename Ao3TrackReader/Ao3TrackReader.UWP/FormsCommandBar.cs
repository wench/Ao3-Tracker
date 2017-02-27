using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Ao3TrackReader.UWP
{
    public class FormsCommandBar : Xamarin.Forms.Platform.UWP.FormsCommandBar
    {
        public FormsCommandBar() : base()
        {
            IsDynamicOverflowEnabled = true;
            PrimaryCommands.VectorChanged += VectorChanged;
            SecondaryCommands.VectorChanged += VectorChanged;
        }

        private void VectorChanged(Windows.Foundation.Collections.IObservableVector<ICommandBarElement> sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
        {
            if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
            {
                var item = sender[(int) args.Index] as AppBarButton;
                if (item == null) return;
                var xitem = item.DataContext as Xamarin.Forms.ToolbarItem;
                if (xitem == null) return;
                item.ClearValue(AppBarButton.IconProperty);
                var uri = new Uri("ms-appx:///" + xitem.Icon.File);
                item.Icon = new BitmapIcon() { UriSource = uri};

                var aitem = item.DataContext as Ao3TrackReader.Controls.ToolbarItem;
                if (aitem != null) {
                    // Create the binding description.
                    Binding b = new Binding();
                    b.Mode = BindingMode.OneWay;
                    b.Path = new PropertyPath("Foreground");
                    b.Converter = new ColorConverter();

                    item.SetBinding(AppBarButton.ForegroundProperty, b);
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size res = availableSize;
            try
            {
                res = base.MeasureOverride(availableSize);
            }
            catch(System.Runtime.InteropServices.COMException e)
            {
                res.Height = 48;
            }
            return res;
        }
    }
}
