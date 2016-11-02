using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Ao3TrackReader
{
    public interface IGroupable
    {
        string Group { get; }
    }

    public class GroupSubList<T> : ObservableCollection<T>
    {
        public GroupSubList(string group)
        {
            Group = group;
        }

        public string Group { get; protected set; }
    }

    public class GroupList<T> : ObservableCollection<GroupSubList<T>> where T : IGroupable, INotifyPropertyChanged, INotifyPropertyChanging
    { 
        public void Add(T item)
        {
            // Add the item to the correct list
            item.PropertyChanging += Item_PropertyChanging;
            item.PropertyChanged += Item_PropertyChanged;

            AddToGroup(item);   
        }

        public void Remove(T item)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            item.PropertyChanging -= Item_PropertyChanging;

            RemoveFromGroup(item);
        }

        private void AddToGroup(T item)
        {
            string groupName = item.Group ?? "";
            GroupSubList<T> g = null;

            int i = 0;
            for (; i < Count; i++)
            {
                int c = String.Compare(this[i].Group, groupName);
                if (c == 0) {
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
                Insert(i, g = new GroupSubList<T>(groupName));
            }

            g.Add(item);
        }

        private void RemoveFromGroup(T item)
        {
            var g = this.Where((l) => l.Group == item.Group).FirstOrDefault();
            if (g != null)
            {
                g.Remove(item);
                if (g.Count == 0) Remove(g);
            }
        }

        private void Item_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Group") {
                // Remove from its group
                RemoveFromGroup((T)sender);
            }
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Group") {
                // Add into group
                AddToGroup((T)sender);
            }
        }
    }
}
