using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public partial class ReadingListView : ListView
	{
		static Color GroupTitleColor { get {
				var c = App.Colors["SystemChromeAltLowColor"];
				return new Color(((int)(c.R * 255) ^ 0xFF) / 255.0, ((int)(c.G * 255) ^ 0) / 510.0, ((int)(c.B * 255) ^ 0) / 255.0);
			} }
		static Color GroupTypeColor { get { return App.Colors["SystemChromeHighColor"]; } }


		public ReadingListView ()
		{
			InitializeComponent ();
		}
	}
}
