using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Ao3TrackReader.Controls
{
#if !WINDOWS_UWP_OLD
    [XamlCompilation(XamlCompilationOptions.Compile)]
#else
    [XamlCompilation(XamlCompilationOptions.Skip)]
#endif
    public partial class ReadingListViewCellNew : ViewCell
    {
		public ReadingListViewCellNew()
		{
			InitializeComponent ();
		}
	}

#if !WINDOWS_UWP_OLD
   public class ReadingListViewCell : ReadingListViewCellNew
    {
    }
#endif
}
