using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Ao3TrackReader.UWP
{
    public class FormsCommandBar : Xamarin.Forms.Platform.UWP.FormsCommandBar
    {
        public FormsCommandBar() : base()
        {
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (availableSize.Width == 0 || availableSize.Height == 0)
            {
                return new Size(0, 0);
            }
            return base.MeasureOverride(availableSize);
        }
    }
}
