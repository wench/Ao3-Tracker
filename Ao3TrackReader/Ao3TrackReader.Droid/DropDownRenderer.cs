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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.Android;
using DropDown = Ao3TrackReader.Controls.DropDown;
using System.ComponentModel;
using Xamarin.Forms;
using Android.Content.Res;
using Android.Graphics;
using Android.Text;
using Android.Util;
using Android.Widget;
using Android.Runtime;
using Android.Text.Style;
using Android.Views;
using System.Collections.Specialized;

[assembly: ExportRenderer(typeof(DropDown), typeof(Ao3TrackReader.Droid.DropDownRenderer))]
namespace Ao3TrackReader.Droid
{
    class DropDownRenderer : ViewRenderer<DropDown, Spinner>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<DropDown> e)
        {
            base.OnElementChanged(e);

            if (Control == null)
            {
                SetNativeControl(new Spinner(Context));
                Control.ItemSelected += Control_ItemSelected;
            }

            if (e.OldElement is DropDown old)
            {
                old.SelectedIndexChanged -= Element_ItemSelected;
                if (ncc != null) ncc.CollectionChanged -= ItemSource_CollectionChanged;
                Control.Adapter = null;
                itemSource = null;
            }

            if (e.NewElement != null)
            {
                UpdateItemSource();
                Element.SelectedIndexChanged += Element_ItemSelected;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == DropDown.ItemsSourceProperty.PropertyName)
            {
                UpdateItemSource();
            }
        }

        INotifyCollectionChanged ncc;
        System.Collections.IList itemSource;
        void UpdateItemSource()
        {
            if (ncc != null) ncc.CollectionChanged -= ItemSource_CollectionChanged;
            if (Element?.ItemsSource == null)
            {
                ncc = null;
                itemSource = null;
                Control.Adapter = null;
                return;
            }
            ncc = Element.ItemsSource as INotifyCollectionChanged;
            if (ncc != null) ncc.CollectionChanged += ItemSource_CollectionChanged;

            FillControl();
        }

        void FillControl()
        {
            itemSource = Element.ItemsSource as System.Collections.IList;
            if (itemSource == null)
            {
                itemSource = new System.Collections.ArrayList();
                foreach (var o in Element.ItemsSource) itemSource.Add(o);
            }

            var adapter = new ArrayAdapter(Context, Android.Resource.Layout.SimpleSpinnerItem, itemSource);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            Control.Adapter = adapter;

            int index = itemSource.IndexOf(Element.SelectedItem);
            if (index != -1) Control.SetSelection(index);
        }

        private void ItemSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FillControl();
        }

        private void Element_ItemSelected(object sender, EventArgs e)
        {
            if (itemSource != null)
            {
                int index = itemSource.IndexOf(Element.SelectedItem);
                if (index != -1 && index != Control.SelectedItemPosition)
                    Control.SetSelection(index);
            }           
        }

        private void Control_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            if (Element != null && itemSource != null && e.Position >= 0 && e.Position < itemSource.Count && e.Position != Element.SelectedIndex)
                Element.SelectedIndex = e.Position;
        }
    }
}
