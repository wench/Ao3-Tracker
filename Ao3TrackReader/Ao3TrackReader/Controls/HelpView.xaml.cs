using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public partial class HelpView : PaneView
	{
		public HelpView ()
		{
			InitializeComponent ();
		}

        protected override void OnWebViewPageSet()
        {
            base.OnWebViewPageSet();

            ToolBarListView.ItemsSource = wvp.ToolbarItems;
        }

        private void ToolBarListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null) ToolBarListView.SelectedItem = null;
        }
    }
}
