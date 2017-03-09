using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

using Xamarin.Forms;

namespace Ao3TrackReader.Controls
{
	public class DropDown : View
	{
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create("ItemsSource", typeof(IEnumerable), typeof(DropDown), null);

        public DropDown ()
		{
		}

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        object _selectedItem;
        public object SelectedItem
        {
            get { return _selectedItem; }
            set {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnItemSelected(value);
                }
            }
        }

        public virtual void OnItemSelected(object item)
        {
            ItemSelected?.Invoke(this, new SelectedItemChangedEventArgs(item));

        }

        public event EventHandler<SelectedItemChangedEventArgs> ItemSelected;
    }
}
