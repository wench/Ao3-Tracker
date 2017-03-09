using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
    public interface IHelpInfo: IGroupable, INotifyPropertyChanged
    {
        string Text { get; }
        FormattedString Description { get; }
        FileImageSource Icon { get; }
    }
    
    public partial class HelpView : PaneView
	{
        GroupList<IHelpInfo> helpBacking;

        public HelpView ()
		{
			InitializeComponent ();

            helpBacking = new GroupList<IHelpInfo>();
        }

        protected override void OnWebViewPageSet()
        {
            base.OnWebViewPageSet();

            foreach (var item in wvp.AllToolbarItems)
                helpBacking.Add(item.Value);

            ListView.ItemsSource = helpBacking;
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null) ListView.SelectedItem = null;
        }
    }
}
