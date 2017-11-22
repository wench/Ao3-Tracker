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
    public interface IGroupable
    {
        string Group { get; }
        string GroupType { get; }
        bool ShouldHide { get; }
    }

    public class GroupSubList<T> : ObservableCollection<T>, INotifyPropertyChanged, INotifyPropertyChanging
        where T : IGroupable
    {
        public GroupSubList(string group)
        {
            Group = group;
        }

        public string Group { get; protected set; }
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

        public void AddSorted(T item)
        {
            if (!WebViewPage.Current.IsMainThread)
                throw new InvalidOperationException();

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
            if (!WebViewPage.Current.IsMainThread)
                throw new InvalidOperationException();

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

        protected override void InsertItem(int index, T item)
        {
            if (!WebViewPage.Current.IsMainThread)
                throw new InvalidOperationException();

            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            if (!WebViewPage.Current.IsMainThread)
                throw new InvalidOperationException();

            base.RemoveItem(index);
        }
    }

    public class GroupList<T> : ObservableCollection<GroupSubList<T>>
        where T : IGroupable, INotifyPropertyChanged
    {
        GroupSubList<T> hidden = new GroupSubList<T>("<Hidden>");
        Dictionary<T, GroupSubList<T>> allItems = new Dictionary<T, GroupSubList<T>>();

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

        public void Remove(T item)
        {
            if (!WebViewPage.Current.IsMainThread)
                throw new InvalidOperationException();

            lock (locker)
            {
                item.PropertyChanged -= Item_PropertyChanged;

                RemoveFromGroup(item);
            }
        }

        public T Find(Predicate<T> pred)
        {
            lock (locker)
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
        }
        public T FindInAll(Predicate<T> pred)
        {
            lock (locker)
            {
                foreach (var e in allItems.Keys)
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
                        action(e);
                    }
                }
            }
        }
        public void ForEachInAll(Action<T> action)
        {
            lock (locker)
            {
                foreach (var e in allItems.Keys)
                {
                    action(e);
                }
            }
        }
        public void ForEachInAll(Func<T, bool> func)
        {
            lock (locker)
            {
                foreach (var e in allItems.Keys)
                {
                    if (func(e) == true)
                        break;
                }
            }
        }

        bool show_hidden = false;
        private bool doSorting;

        public GroupList()
        {
            this.doSorting = true;
        }

        public GroupList(bool doSorting)
        {
            this.doSorting = doSorting;
        }

        private bool IsHidden(T item)
        {
            return (!show_hidden && item.ShouldHide);
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
                    AddToGroup(e);
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
            get { return show_hidden; }
            set
            {
                if (!WebViewPage.Current.IsMainThread)
                    throw new InvalidOperationException();

                lock (locker)
                {
                    if (show_hidden != value)
                    {
                        show_hidden = value;
                        ResortHidden();
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

        private GroupSubList<T> GetGroupForItem(T item)
        {
            string groupName = item.Group;
            if (string.IsNullOrWhiteSpace(groupName)) groupName = "<Other>";
            GroupSubList<T> g = null;

            int i = 0;

            if (IsHidden(item))
            {
                g = hidden;
            }
            else for (; i < Count; i++)
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
                g = new GroupSubList<T>(groupName);
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

                if (doSorting) g.AddSorted(item);
                else g.Add(item);

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
                if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Group" || e.PropertyName == "SortOrder" || e.PropertyName == "ShouldHide")
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
                if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "GroupType")
                {
                    if (!IsHidden(item) && !string.IsNullOrWhiteSpace(item.GroupType))
                    {
                        string groupName = item.Group;
                        if (string.IsNullOrWhiteSpace(groupName)) groupName = "<Other>";
                        var g = this.Where((l) => l.Group == groupName).FirstOrDefault();
                        if (g != null) g.GroupType = item.GroupType;
                    }
                }
            }
        }

        IEnumerable<GroupSubList<T>> AllGroups
        {
            get
            {
                return this.Concat(new[] { hidden });
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
