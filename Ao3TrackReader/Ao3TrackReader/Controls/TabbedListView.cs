using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Ao3TrackReader.Controls
{
    [Xamarin.Forms.ContentProperty(null)]
	public class TabbedListView : TabbedLayout
	{
        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create("ItemsSource", typeof(IEnumerable<IEnumerable>), typeof(TabbedListView), defaultValue: null, 
            propertyChanged: (obj, o, n)=>(obj as TabbedListView).OnItemsSourceChanged((IEnumerable<IEnumerable>)o, (IEnumerable<IEnumerable>)n));
        public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create("SelectedItem", typeof(object), typeof(TabbedListView), defaultValue: null, 
            propertyChanged: (obj, o, n) => (obj as TabbedListView).OnSelectedItemChanged(o, n));

        // These are forwarded straight to the lists
        List<(BindableProperty src, BindableProperty listprop)> ListViewBindings = new List<(BindableProperty src, BindableProperty listprop)> {
            ( ItemTemplateProperty, ListView.ItemTemplateProperty ),
            ( HeaderTemplateProperty, ListView.HeaderTemplateProperty ),
            ( FooterTemplateProperty, ListView.FooterTemplateProperty ),
            ( HasUnevenRowsProperty, ListView.HasUnevenRowsProperty ),
            ( RowHeightProperty, ListView.RowHeightProperty ),
            ( GroupHeaderTemplateProperty, ListView.GroupHeaderTemplateProperty ),
            ( IsGroupingEnabledProperty, ListView.IsGroupingEnabledProperty ),
            ( SeparatorVisibilityProperty, ListView.SeparatorVisibilityProperty ),
            ( SeparatorColorProperty, ListView.SeparatorColorProperty )
        };

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create("ItemTemplate", typeof(DataTemplate), typeof(TabbedListView), defaultValue: null);
        public static readonly BindableProperty HeaderTemplateProperty = BindableProperty.Create("HeaderTemplate", typeof(DataTemplate), typeof(TabbedListView), defaultValue: null);
        public static readonly BindableProperty FooterTemplateProperty = BindableProperty.Create("FooterTemplate", typeof(DataTemplate), typeof(TabbedListView), defaultValue: null);
        public static readonly BindableProperty HasUnevenRowsProperty = BindableProperty.Create("HasUnevenRows", typeof(bool), typeof(TabbedListView), defaultValue: false);
        public static readonly BindableProperty RowHeightProperty = BindableProperty.Create("RowHeight", typeof(int), typeof(TabbedListView), defaultValue: -1);
        public static readonly BindableProperty GroupHeaderTemplateProperty = BindableProperty.Create("GroupHeaderTemplate", typeof(DataTemplate), typeof(TabbedListView), defaultValue: null);
        public static readonly BindableProperty IsGroupingEnabledProperty = BindableProperty.Create("IsGroupingEnabled", typeof(bool), typeof(TabbedListView), defaultValue: false);
        public static readonly BindableProperty SeparatorVisibilityProperty = BindableProperty.Create("SeparatorVisibility", typeof(SeparatorVisibility), typeof(TabbedListView), defaultValue: SeparatorVisibility.Default);
        public static readonly BindableProperty SeparatorColorProperty = BindableProperty.Create("SeparatorColor", typeof(Color), typeof(TabbedListView), defaultValue: Color.Default);

        public IEnumerable<IEnumerable> ItemsSource
        {
            get { return (IEnumerable<IEnumerable>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public DataTemplate FooterTemplate
        {
            get { return (DataTemplate)GetValue(FooterTemplateProperty); }
            set { SetValue(FooterTemplateProperty, value); }
        }

        BindingBase _groupDisplayBinding;
        public BindingBase GroupDisplayBinding
        {
            get { return _groupDisplayBinding; }
            set
            {
                if (_groupDisplayBinding == value)
                    return;

                OnPropertyChanging();
                _groupDisplayBinding = value;

                foreach (var tab in Tabs)
                {
                    var listview = tab.Content as ListView;
                    listview.GroupDisplayBinding = value;
                }

                OnPropertyChanged();
            }
        }

        public DataTemplate GroupHeaderTemplate
        {
            get { return (DataTemplate)GetValue(GroupHeaderTemplateProperty); }
            set { SetValue(GroupHeaderTemplateProperty, value); }
        }

        BindingBase _groupShortNameBinding;
        public BindingBase GroupShortNameBinding
        {
            get { return _groupShortNameBinding; }
            set
            {
                if (_groupShortNameBinding == value)
                    return;

                OnPropertyChanging();
                _groupShortNameBinding = value;
                foreach (var tab in Tabs)
                {
                    var listview = tab.Content as ListView;
                    listview.GroupShortNameBinding = value;
                }
                OnPropertyChanged();
            }
        }

        public bool HasUnevenRows
        {
            get { return (bool)GetValue(HasUnevenRowsProperty); }
            set { SetValue(HasUnevenRowsProperty, value); }
        }

        public DataTemplate HeaderTemplate
        {
            get { return (DataTemplate)GetValue(HeaderTemplateProperty); }
            set { SetValue(HeaderTemplateProperty, value); }
        }

        public bool IsGroupingEnabled
        {
            get { return (bool)GetValue(IsGroupingEnabledProperty); }
            set { SetValue(IsGroupingEnabledProperty, value); }
        }


        public int RowHeight
        {
            get { return (int)GetValue(RowHeightProperty); }
            set { SetValue(RowHeightProperty, value); }
        }

        public object SelectedItem
        {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public Color SeparatorColor
        {
            get { return (Color)GetValue(SeparatorColorProperty); }
            set { SetValue(SeparatorColorProperty, value); }
        }

        public SeparatorVisibility SeparatorVisibility
        {
            get { return (SeparatorVisibility)GetValue(SeparatorVisibilityProperty); }
            set { SetValue(SeparatorVisibilityProperty, value); }
        }


        BindingBase CloneBinding(BindingBase bindingbase)
        {
            if (bindingbase is Binding binding)
                 return new Binding(binding.Path, binding.Mode) { Converter = binding.Converter, ConverterParameter = binding.ConverterParameter, StringFormat = binding.StringFormat, Source = binding.Source, UpdateSourceEventName = binding.UpdateSourceEventName };

            var type = bindingbase.GetType();
            var method = type.GetMethod("Clone", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (BindingBase)method.Invoke(bindingbase, Type.EmptyTypes);
        }


        Binding _tabTitleBinding;
        public BindingBase TabTitleBinding
        {
            get { return _tabTitleBinding; }
            set
            {
                if (_tabTitleBinding == value)
                    return;

                OnPropertyChanging();
                _tabTitleBinding = value as Binding;

                foreach (var tab in Tabs)
                {
                    if (_tabTitleBinding != null) tab.SetBinding(TabView.TitleProperty, CloneBinding(_tabTitleBinding));
                    else tab.RemoveBinding(TabView.TitleProperty);
                }

                OnPropertyChanged();
            }
        }

        Binding _tabIconBinding;
        public BindingBase TabIconBinding
        {
            get { return _tabIconBinding; }
            set
            {
                if (_tabIconBinding == value)
                    return;

                OnPropertyChanging();
                _tabIconBinding = value as Binding;

                foreach (var tab in Tabs)
                {
                    if (_tabIconBinding != null) tab.SetBinding(TabView.IconProperty, CloneBinding(_tabIconBinding));
                    else tab.RemoveBinding(TabView.IconProperty);
                }

                OnPropertyChanged();
            }
        }


        public event EventHandler<ItemVisibilityEventArgs> ItemAppearing;

        public event EventHandler<ItemVisibilityEventArgs> ItemDisappearing;

        public event EventHandler<SelectedItemChangedEventArgs> ItemSelected;

        public event EventHandler<ItemTappedEventArgs> ItemTapped;


        public TabbedListView ()
		{
		}

        void OnItemsSourceChanged(IEnumerable<IEnumerable> oldValue, IEnumerable<IEnumerable> newValue)
        { 
            // Clear tabs
            if (oldValue is INotifyCollectionChanged o)
            {
                o.CollectionChanged -= Notify_CollectionChanged;
            }

            ClearTabs();

            if (newValue is null) return;

            // Create tabs
            if (newValue is INotifyCollectionChanged n)
            {
                n.CollectionChanged += Notify_CollectionChanged;
            }

            int index = 0;
            foreach (var enumerable in newValue as IEnumerable<IEnumerable>)
            {
                AddTab(enumerable, index++);
            }
        }

        void Notify_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            int i;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    ClearTabs();
                    break;

                case NotifyCollectionChangedAction.Add:
                    i = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        AddTab(item as IEnumerable, i++);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        RemoveTab(item as IEnumerable);
                    }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        RemoveTab(item as IEnumerable);
                    }
                    i = e.NewStartingIndex;
                    foreach (var item in e.NewItems)
                    {
                        AddTab(item as IEnumerable, i++);
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    UpdateTabs();
                    break;
            }

        }

        void ClearTabs()
        {
            Tabs.Clear();
        }



        void AddTab(IEnumerable source, int index)
        {
            var tab = new TabView { Scroll = false, BindingContext = source };
            if (TabTitleBinding != null) tab.SetBinding(TabView.TitleProperty, CloneBinding(_tabTitleBinding));
            if (TabIconBinding != null) tab.SetBinding(TabView.IconProperty, CloneBinding(_tabIconBinding));

            var list = new ListView();
            tab.Content = list;
            tab.SizeChanged += Tab_SizeChanged;

            if (GroupShortNameBinding != null) list.GroupShortNameBinding = CloneBinding(GroupShortNameBinding);
            if (GroupDisplayBinding != null) list.GroupDisplayBinding = CloneBinding(GroupDisplayBinding);

            SetAllListProperties(list);

            list.ItemAppearing += ListView_ItemAppearing;
            list.ItemDisappearing += ListView_ItemDisappearing;
            list.ItemSelected += ListView_ItemSelected;
            list.ItemTapped += ListView_ItemTapped;

            list.Header = source;
            list.ItemsSource = source;

            if (source is INotifyCollectionChanged ncc)
            {
                var col = source as ICollection;
                list.IsVisible = false;

                ncc.CollectionChanged += Ncc_CollectionChanged;
            }

            Tabs.Insert(index, tab);

        }

        private void Tab_SizeChanged(object sender, EventArgs e)
        {
            var tab = (TabView)sender;
            var listview = tab.Content as ListView;
            var col = listview.ItemsSource as ICollection;
            listview.IsVisible = tab.Width > 0 && col?.Count > 0;
        }

        private void Ncc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            for (int t = 0; t < Tabs.Count; t++)
            {
                var tab = Tabs[t];
                var listview = tab.Content as ListView;
                if (listview?.ItemsSource == sender)
                {
                    var col = sender as ICollection;
                    listview.IsVisible = tab.Width > 0 && col?.Count > 0;
                    return;
                }
            }
        }

        void RemoveTab(IEnumerable source)
        {
            for (int t = 0; t < Tabs.Count; t++)
            {
                var tab = Tabs[t];
                var listview = tab.Content as ListView;
                if (listview?.ItemsSource == source)
                {
                    if (source is INotifyCollectionChanged ncc)
                    {
                        ncc.CollectionChanged -= Ncc_CollectionChanged;
                    }

                    listview.ItemAppearing -= ListView_ItemAppearing;
                    listview.ItemDisappearing -= ListView_ItemDisappearing;
                    listview.ItemSelected -= ListView_ItemSelected;
                    listview.ItemTapped -= ListView_ItemTapped;
                    tab.SizeChanged -= Tab_SizeChanged;
                    Tabs.RemoveAt(t);
                    return;
                }
            }
            // Something something throw execption???
        }


        void UpdateTabs()
        {
            int counter = 0;
            foreach (var listSource in ItemsSource)
            {
                int i = counter++;
                bool found = false;
                for (int t = i; t < Tabs.Count; t++)
                {
                    var tab = Tabs[t];
                    var listview = tab.Content as ListView;
                    if (listview?.ItemsSource == listSource)
                    {
                        if (i != t) MoveTab(tab, i);
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                // Create a tab at index i
                AddTab(listSource, i);
            }

            // Remove all tabs after counter
            while (counter < Tabs.Count)
            {
                var list = Tabs[Tabs.Count - 1].Content as ListView;
                list.ItemAppearing -= ListView_ItemAppearing;
                list.ItemDisappearing -= ListView_ItemDisappearing;
                list.ItemSelected -= ListView_ItemSelected;
                list.ItemTapped -= ListView_ItemTapped;
                Tabs.RemoveAt(Tabs.Count-1);
            }
            
        }

        void OnSelectedItemChanged(object oldValue, object newValue)
        {
            for (int t = 0; t < Tabs.Count; t++)
            {
                var tab = Tabs[t];
                var listview = tab.Content as ListView;
                if (listview.ItemsSource != null)
                {
                    var enumerable = listview.ItemsSource.OfType<object>();

                    if (enumerable.Contains(newValue))
                    {
                        listview.SelectedItem = newValue;
                    }
                    else
                    {
                        listview.SelectedItem = null;
                    }
                }
            }

        }

        void SetAllListProperties(ListView list)
        {
            foreach (var pair in ListViewBindings)
            {
                list.SetValue(pair.listprop, GetValue(pair.src));
            }
        }

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (string.IsNullOrEmpty(propertyName))
            {
                foreach (var tab in Tabs)
                {
                    var list = tab.Content as ListView;
                    SetAllListProperties(list);
                }
            }
            else
            {
                foreach (var pair in ListViewBindings)
                {
                    if (pair.src.PropertyName == propertyName)
                    {
                        foreach (var tab in Tabs)
                        {
                            var list = tab.Content as ListView;
                            list.SetValue(pair.listprop, GetValue(pair.src));
                        }
                    }
                }
            }
        }


        void ListView_ItemAppearing(object sender, ItemVisibilityEventArgs args)
        {
            ItemAppearing?.Invoke(this, args);
        }

        void ListView_ItemDisappearing (object sender, ItemVisibilityEventArgs args)
        {
            ItemDisappearing?.Invoke(this, args);
        }

        void ListView_ItemSelected (object sender, SelectedItemChangedEventArgs args)
        {
            ItemSelected?.Invoke(this, args);
        }

        void ListView_ItemTapped (object sender, ItemTappedEventArgs args)
        {
            ItemTapped?.Invoke(this, args);
        }

    }
}