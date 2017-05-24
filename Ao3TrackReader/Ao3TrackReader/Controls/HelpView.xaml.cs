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
using Ao3TrackReader.Models;

namespace Ao3TrackReader.Controls
{

    public partial class HelpView : PaneView
	{
        public static readonly Xamarin.Forms.BindableProperty TextProperty =
          Xamarin.Forms.BindableProperty.CreateAttached("Text", typeof(string), typeof(HelpView), defaultValue: null);

        public static readonly Xamarin.Forms.BindableProperty DescriptionProperty =
          Xamarin.Forms.BindableProperty.CreateAttached("Description", typeof(Text.TextEx), typeof(HelpView), defaultValue: null);

        public static readonly Xamarin.Forms.BindableProperty GroupProperty =
          Xamarin.Forms.BindableProperty.CreateAttached("Group", typeof(string), typeof(HelpView), defaultValue: null);

        GroupList<IHelpInfo> helpBacking;

        public HelpView ()
		{
			InitializeComponent ();

            helpBacking = new GroupList<IHelpInfo>(false);
        }

        public void Init()
        {
            if (helpBacking.Count == 0)
            {
                foreach (var item in wvp.HelpItems)
                    helpBacking.Add(item);

                foreach (var item in wvp.ReadingList.HelpItems)
                    helpBacking.Add(item);

                ListView.ItemsSource = helpBacking;
                App.Database.GetVariableEvents("LogFontSizeUI").Updated += LogFontSizeUI_Updated;
            }
        }

        private void LogFontSizeUI_Updated(object sender, Ao3TrackDatabase.VariableUpdatedEventArgs e)
        {
            wvp.DoOnMainThreadAsync(() =>
            {
                ListView.ItemsSource = null;
                ListView.ItemsSource = helpBacking;
            }).ConfigureAwait(false);
        }

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null) ListView.SelectedItem = null;
        }

        public static string GetText(BindableObject view)
        {
            return (string)view.GetValue(TextProperty);
        }

        public static void SetText(BindableObject view, string value)
        {
            view.SetValue(TextProperty, value);
        }

        public static Text.TextEx GetDescription(BindableObject view)
        {
            return (Text.TextEx)view.GetValue(DescriptionProperty);
        }

        public static void SetDescription(BindableObject view, Text.TextEx value)
        {
            view.SetValue(DescriptionProperty, value);
        }

        public static string GetGroup(BindableObject view)
        {
            return (string)view.GetValue(GroupProperty);
        }

        public static void SetGroup(BindableObject view, string value)
        {
            view.SetValue(GroupProperty, value);
        }
    }
}
