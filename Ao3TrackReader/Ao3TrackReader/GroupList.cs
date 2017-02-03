using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Ao3TrackReader
{
    public interface IGroupable<in T> : IComparable<T>
    {
        string Group { get; }
        string GroupType { get; }
        bool ShouldHide { get; }
    }

    public class GroupSubList<T> : ObservableCollection<T>, INotifyPropertyChanged, INotifyPropertyChanging
        where T : IGroupable<T>
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
                grouptype = value;
                OnPropertyChanged(new PropertyChangedEventArgs("GroupType"));
            }
        }

        public event PropertyChangingEventHandler PropertyChanging;
        void OnPropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChanging?.Invoke(this, args);
        }

        public void AddSorted(T item)
        {
            int i = 0;
            for (; i < Count; i++)
            {
                if (Object.ReferenceEquals(item,this[i]))
                {
                    throw new ArgumentException("Attempting to add item already in group", "iten");
                }
                else if(item.CompareTo(this[i]) < 0)
                {
                    break;
                }
            }

            InsertItem(i, item);
        }

        public void ResortItem(T item)
        {
            int oldindex = -1;
            int newindex = -1;            
            for (int i = 0; i < Count && (newindex == -1 || oldindex == -1); i++)
            {
                if (oldindex == -1 && Object.ReferenceEquals(item, this[i]))
                {
                    oldindex = i;
                }
                else if (newindex == -1 && item.CompareTo(this[i]) < 0)
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
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            base.RemoveItem(index);
        }
    }

    public class GroupList<T> : ObservableCollection<GroupSubList<T>>
        where T :  IGroupable<T>, INotifyPropertyChanged
    {
        GroupSubList<T> hidden = new GroupSubList<T>("<Hidden>");
        Dictionary<T, GroupSubList<T>> allItems = new Dictionary<T, GroupSubList<T>>();

        private object locker = new object();

        public void Add(T item)
        {
            // Add the item to the correct list
            item.PropertyChanged += Item_PropertyChanged;

            AddToGroup(item);
        }

        public void Remove(T item)
        {
            item.PropertyChanged -= Item_PropertyChanged;

            RemoveFromGroup(item);
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

        bool show_hidden = false;
        private bool IsHidden(T item)
        {
            return (!show_hidden && item.ShouldHide);
        }

        private void ResortHidden()
        {
            lock (locker)
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
        }

        public bool ShowHidden {
            get { return show_hidden; }
            set {
                if (show_hidden != value)
                {
                    show_hidden = value;
                    ResortHidden();
                }
            }
        }

        int GroupCompare(string left,string right)
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
                else if (c > 0)
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

                g.AddSorted(item);
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
            var item = (T)sender;
            if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Group" || e.PropertyName == "SortOrder")
            {
                lock (locker)
                {
                    if (allItems.TryGetValue(item, out var oldgroup))
                    {
                        var g = GetGroupForItem(item);
                        if (oldgroup != g) {
                            AddToGroup(item);
                        }
                        else
                        {
                            g.ResortItem(item);
                        }
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
