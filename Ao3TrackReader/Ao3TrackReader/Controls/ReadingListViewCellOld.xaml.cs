using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Ao3TrackReader.Controls
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ReadingListViewCellOld : ViewCell
    {
		public ReadingListViewCellOld()
		{
			InitializeComponent ();
		}
	}

#if WINDOWS_UWP_OLD
   public class ReadingListViewCell : ReadingListViewCellOld
    {
    }
#endif
}