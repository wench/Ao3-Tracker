using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using Ao3TrackReader.Models;

namespace Ao3TrackReader
{
    public class PageEx : BindableObject
    {
        public static readonly BindableProperty TitleExProperty =
          BindableProperty.CreateAttached("TitleEx", typeof(TextTree), typeof(WVPNavigationPage), null, propertyChanged: TitleExPropertChanged);

        private static void TitleExPropertChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var val = newValue as TextTree;
            var page = bindable as Page;

            if (page != null)
            {
                page.Title = val?.ToString();
            }
        }

        public static TextTree GetTitleEx(BindableObject view)
        {
            return (TextTree)view.GetValue(TitleExProperty);
        }

        public static void SetTitleEx(BindableObject view, Models.TextTree value)
        {
            view.SetValue(TitleExProperty, value);
        }
    }

    public interface IPageEx
    {
        TextTree TitleEx { get; }
        string Title { get; set; }
    }

}
