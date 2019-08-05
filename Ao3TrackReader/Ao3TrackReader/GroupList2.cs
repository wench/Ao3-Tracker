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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using PropertyChangingEventHandler = Xamarin.Forms.PropertyChangingEventHandler;
using PropertyChangingEventArgs = Xamarin.Forms.PropertyChangingEventArgs;
#endif

namespace Ao3TrackReader
{
    public interface IGroupable2 : IGroupable
    {
        int? Unread { get; }
        bool Favourite { get; }
    }


    public class SubGroupContents<T> : ObservableCollection<T>
        where T : IGroupable2
    {
        public SubGroupContents(string title, string shortcut)
        {
            Unread = -1;
            Group = title;
            Shortcut = shortcut;
        }
        public SubGroupContents(int? unread)
        {
            Unread = unread;
            if (unread == null)
            {
                Shortcut = "-";
                Group = "No chapters";
            }
            else
            {
                Shortcut = unread.ToString();
                Group = unread.ToString() + " unread chapter" + (unread!=1?"s":"");
            }
        }

        public int? Unread { get; }
        public string Group { get; }
        public string Shortcut { get; }

        public void AddSorted(T item)
        {
            var comparable = item as IComparable<T>;
            if (comparable == null)
            {
                Add(item);
                return;
            }

            int i = 0;
            for (; i < Count; i++)
            {
                if (Object.ReferenceEquals(item, this[i]))
                {
                    throw new ArgumentException("Attempting to add item already in group", "iten");
                }
                else if (comparable.CompareTo(this[i]) < 0)
                {
                    break;
                }
            }

            InsertItem(i, item);
        }

        public void ResortItem(T item)
        {
            var comparable = item as IComparable<T>;
            if (comparable == null)
            {
                return;
            }

            int oldindex = -1;
            int newindex = -1;
            for (int i = 0; i < Count && (newindex == -1 || oldindex == -1); i++)
            {
                if (oldindex == -1 && Object.ReferenceEquals(item, this[i]))
                {
                    oldindex = i;
                }
                else if (newindex == -1 && comparable.CompareTo(this[i]) < 0)
                {
                    newindex = i;
                }
            }
            if (oldindex == -1)
            {
                InsertItem(newindex, item);
                return;
            }
            if (newindex == -1)
            {
                newindex = Count;
            }

            if (newindex > oldindex) newindex--;
            if (newindex == oldindex) return;
            MoveItem(oldindex, newindex);
        }
    }

    public class GroupContents<T> : ObservableCollection<SubGroupContents<T>>, INotifyPropertyChanged, INotifyPropertyChanging
        where T : IGroupable2, INotifyPropertyChanged
    {
        SubGroupContents<T> hidden = new SubGroupContents<T>(null, null);
        SubGroupContents<T> favourites = new SubGroupContents<T>("\u2B50 Favorites", "\u2B50");
        Dictionary<T, SubGroupContents<T>> allItems = new Dictionary<T, SubGroupContents<T>>();

        public string Group { get; }
        string grouptype;
        public string GroupType
        {
            get { return grouptype; }
            set
            {
                if (grouptype == value) return;
                OnPropertyChanging(new PropertyChangingEventArgs("GroupType"));
                OnPropertyChanging(new PropertyChangingEventArgs("HasGroupType"));
                grouptype = value;
                OnPropertyChanged(new PropertyChangedEventArgs("HasGroupType"));
                OnPropertyChanged(new PropertyChangedEventArgs("GroupType"));
            }
        }
        public bool HasGroupType => !String.IsNullOrWhiteSpace(GroupType);

        public event PropertyChangingEventHandler PropertyChanging;
        void OnPropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChanging?.Invoke(this, args);
        }

        public void Add(T item)
        {
            AddToSubGroup(item);
        }

        public bool Remove(T item)
        {            
            return RemoveFromSubGroup(item);
        }

        public T Find(Predicate<T> pred)
        {
            foreach (var g in this)
            {
                foreach (var e in g)
                {
                    if (pred(e))
                        return e;
                }
            }
            return default(T);
        }
        public bool Contains(T obj)
        {
            foreach (var g in this)
            {
                if (g.Contains(obj)) return true;
            }
            return false;
        }
        public T FindInAll(Predicate<T> pred)
        {
            foreach (var e in allItems.Keys)
            {
                if (pred(e))
                    return e;
            }
            return default(T);
        }
        public void ForEach(Action<T> action)
        {
            foreach (var g in this)
            {
                foreach (var e in g)
                {
                    action(e);
                }
            }
        }
        public void ForEachInAll(Action<T> action)
        {
            foreach (var e in allItems.Keys)
            {
                action(e);
            }
        }
        public void ForEachInAll(Func<T, bool> func)
        {
            foreach (var e in allItems.Keys)
            {
                if (func(e) == true)
                    break;
            }
        }

        bool showHidden = false;
        private bool doSorting;

        public GroupContents(string group, bool doSorting, bool showHidden)
        {
            this.Group = group;
            this.doSorting = doSorting;
            this.showHidden = showHidden;
        }

        private bool IsHidden(T item)
        {
            return (!showHidden && item.ShouldHide);
        }

        private void ResortHidden()
        {
            List<T> toHide = new List<T>();
            foreach (var g in this.ToArray())
            {
                foreach (var e in g.ToArray())
                {
                    if (IsHidden(e))
                    {
                        g.Remove(e);
                        toHide.Add(e);
                    }
                }
                if (g.Count == 0) Remove(g);
            }
            foreach (var e in hidden.ToArray())
            {
                if (!IsHidden(e))
                {
                    hidden.Remove(e);
                    AddToSubGroup(e);
                }
            }
            foreach (var e in toHide)
            {
                allItems[e] = hidden;
                hidden.Add(e);
            }
        }

        public bool ShowHidden
        {
            get { return showHidden; }
            set
            {
                if (showHidden != value)
                {
                    showHidden = value;
                    ResortHidden();
                }
            }
        }

        int SubGroupCompare(int? left, int? right)
        {
            int catl = 0, catr = 0;

            if (left is null) catl = 1;
            else if (left < 0) catl = 2;
            if (right is null) catr = 1;
            else if (right < 0) catr = 2;

            if (catl < catr) return 1;
            else if (catl > catr) return -1;

            if (left is null) return right is null ? 0 : 1;
            else if (right is null) return -1;

            if (left < right) return 1;
            if (left > right) return -1;
            return 0;
        }

        private SubGroupContents<T> GetSubGroupForItem(T item)
        {
            int? unread = item.Unread;
            if (unread < 0) unread = 0;

            int i = 0;

            if (!(unread is null)) unread = Math.Max(0, (int)unread);

            if (IsHidden(item))
            {
                return hidden;
            }
            else for (; i < Count; i++)
                {
                    int c = SubGroupCompare(this[i].Unread, unread);
                    if (c == 0)
                    {
                        return this[i];
                    }
                    else if (doSorting && c > 0)
                    {
                        break;
                    }
                }

            var g = new SubGroupContents<T>(unread);
            Insert(i, g);
            return g;
        }

        private void AddToSubGroup(T item)
        {
            var g = GetSubGroupForItem(item);

            if (allItems.TryGetValue(item, out var oldgroup))
            {
                if (oldgroup == g) return;
                RemoveFromSubGroup(item);
            }

            if (doSorting) g.AddSorted(item);
            else g.Add(item);

            allItems[item] = g;

            if (item.Favourite)
            {
                if (!Contains(favourites)) Insert(0, favourites);
                favourites.AddSorted(item);
            }
        }

        private bool RemoveFromSubGroup(T item)
        {
            if (!allItems.TryGetValue(item, out var g))
                return false;

            allItems.Remove(item);

            bool res = g.Remove(item);
            if (g.Count == 0) Remove(g);

            if (favourites.Remove(item) && favourites.Count == 0)
            {
                Remove(favourites);
            }

            return res;
        }

        internal void ResortItem(T item)
        {
            if (allItems.TryGetValue(item, out var oldgroup))
            {
                var g = GetSubGroupForItem(item);
                if (oldgroup != g)
                {
                    AddToSubGroup(item);
                }
                else
                {
                    if (doSorting) g.ResortItem(item);
                }

                if (!item.Favourite)
                {
                    if (favourites.Remove(item) && favourites.Count == 0)
                    {
                        Remove(favourites);
                    }
                }
                else if (favourites.Contains(item))
                {
                    if (doSorting) favourites.ResortItem(item);
                }
                else
                {
                    if (!Contains(favourites)) Insert(0, favourites);
                    favourites.AddSorted(item);
                }
            }
        }

        internal IEnumerable<SubGroupContents<T>> AllGroups
        {
            get
            {
                return this.Concat(new[] { hidden });
            }
        }

        internal IEnumerable<T> All
        {
            get
            {
                return allItems.Keys;
            }
        }
    }

    public class GroupList2<T> : ObservableCollection<GroupContents<T>>
        where T : IGroupable2, INotifyPropertyChanged
    {
        Dictionary<T, GroupContents<T>> allItems = new Dictionary<T, GroupContents<T>>();

        private object locker = new object();

        public void Add(T item)
        {
            if (!WebViewPage.Current.IsMainThread)
                throw new InvalidOperationException();

            lock (locker)
            {
                // Add the item to the correct list
                item.PropertyChanged += Item_PropertyChanged;

                AddToGroup(item);
            }
        }

        public bool Remove(T item)
        {
            if (!WebViewPage.Current.IsMainThread)
                throw new InvalidOperationException();

            lock (locker)
            {
                item.PropertyChanged -= Item_PropertyChanged;

                return RemoveFromGroup(item);
            }
        }
        public bool Contains(T obj)
        {
            foreach (var g in this)
            {
                if (g.Contains(obj)) return true;
            }
            return false;
        }
        public T Find(Predicate<T> pred)
        {
            lock (locker)
            {
                foreach (var g in this)
                {
                    foreach (var e in g)
                    {
                        foreach (var i in e)
                        {
                            if (pred(i))
                                return i;
                        }
                    }
                }
                return default(T);
            }
        }

        public T FindInAll(Predicate<T> pred)
        {
            lock (locker)
            {
                foreach (var e in All)
                {
                    if (pred(e))
                        return e;
                }
                return default(T);
            }
        }
        public void ForEach(Action<T> action)
        {
            lock (locker)
            {
                foreach (var g in this)
                {
                    foreach (var e in g)
                    {
                        foreach (var i in e)
                        {
                            action(i);
                        }
                    }
                }
            }
        }
        public void ForEachInAll(Action<T> action)
        {
            lock (locker)
            {
                foreach (var e in All)
                {
                    action(e);
                }
            }
        }
        public void ForEachInAll(Func<T, bool> func)
        {
            lock (locker)
            {
                foreach (var e in All)
                {
                    if (func(e) == true)
                        break;
                }
            }
        }

        bool showHidden = false;
        private bool doSorting;

        public GroupList2()
        {
            this.doSorting = true;
        }

        public GroupList2(bool doSorting)
        {
            this.doSorting = doSorting;
        }

        public bool ShowHidden
        {
            get { return showHidden; }
            set
            {
                if (!WebViewPage.Current.IsMainThread)
                    throw new InvalidOperationException();

                lock (locker)
                {
                    if (showHidden != value)
                    {
                        showHidden = value;
                        foreach (var g in this)
                        {
                            g.ShowHidden = value;
                        }
                    }
                }
            }
        }

        int GroupCompare(string left, string right)
        {
            bool lb = left.StartsWith("<");
            bool rb = right.StartsWith("<");
            if (lb != rb)
            {
                return lb.CompareTo(rb);
            }
            return String.Compare(left, right);
        }

        private GroupContents<T> GetGroupForItem(T item)
        {
            string groupName = item.Group;
            if (string.IsNullOrWhiteSpace(groupName)) groupName = "<Other>";
            GroupContents<T> g = null;

            int i = 0;

            for (; i < Count; i++)
                {
                    int c = GroupCompare(this[i].Group, groupName);
                    if (c == 0)
                    {
                        g = this[i];
                        break;
                    }
                    else if (doSorting && c > 0)
                    {
                        break;
                    }
                }

            if (g == null)
            {
                g = new GroupContents<T>(groupName, doSorting, showHidden);
                if (!string.IsNullOrWhiteSpace(item.GroupType)) g.GroupType = item.GroupType;
                Insert(i, g);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(item.GroupType)) g.GroupType = item.GroupType;
            }
            return g;
        }

        private void AddToGroup(T item)
        {
            lock (locker)
            {
                var g = GetGroupForItem(item);

                if (allItems.TryGetValue(item, out var oldgroup))
                {
                    if (oldgroup == g) return;
                    RemoveFromGroup(item);
                }

                g.Add(item);

                allItems[item] = g;
            }
        }

        private bool RemoveFromGroup(T item)
        {
            lock (locker)
            {
                if (!allItems.TryGetValue(item, out var g))
                    return false;

                allItems.Remove(item);

                bool res = g.Remove(item);
                if (g.Count == 0) Remove(g);
                return res;
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            lock (locker)
            {
                var item = (T)sender;
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Group" || e.PropertyName == "SortOrder" || e.PropertyName == "ShouldHide")
                {
                    if (allItems.TryGetValue(item, out var oldgroup))
                    {
                        var g = GetGroupForItem(item);
                        if (oldgroup != g)
                        {
                            AddToGroup(item);
                        }
                        else
                        {
                            if (doSorting) g.ResortItem(item);
                        }
                    }
                }
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "GroupType")
                {
                    if (!string.IsNullOrWhiteSpace(item.GroupType))
                    {
                        string groupName = item.Group;
                        if (string.IsNullOrWhiteSpace(groupName)) groupName = "<Other>";
                        var g = this.Where((l) => l.Group == groupName).FirstOrDefault();
                        if (g != null) g.GroupType = item.GroupType;
                    }
                }
            }
        }


        IEnumerable<T> All
        {
            get
            {
                return allItems.Keys;
            }
        }
        public IEnumerable<T> AllSafe
        {
            get
            {
                lock (locker)
                {
                    return allItems.Keys.ToList();
                }
            }
        }
    }
}
