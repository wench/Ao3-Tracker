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
            IsDynamicOverflowEnabled = true;
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
