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
