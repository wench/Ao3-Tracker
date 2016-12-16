using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader
{
    public class ListConverter<T, Tbase> : IList<T>
        where T : Tbase
    {
        class Enumerator : IEnumerator<T>
        {
            private readonly IEnumerator<Tbase> enumerator;

            public Enumerator(IEnumerator<Tbase> enumerator)
            {
                this.enumerator = enumerator;
            }
            public T Current
            {
                get
                {
                    return (T)enumerator.Current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return (enumerator as IEnumerator).Current;
                }
            }

            public void Dispose()
            {
                enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }

            public override string ToString()
            {
                return enumerator.ToString();
            }

            public override int GetHashCode()
            {
                return enumerator.GetHashCode();
            }
        }

        private readonly IList<Tbase> list;

        public ListConverter(IList<Tbase> list)
        {
            this.list = list;
        }
        public T this[int index]
        {
            get
            {
                return (T)list[index];
            }

            set
            {
                list[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return list.IsReadOnly;
            }
        }

        public void Add(T item)
        {
            list.Add(item);
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), "arrayIndex is less than 0.");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "arrayIndex is less than 0.");
            if (arrayIndex + Count > array.Length)
                throw new ArgumentException("The number of elements in the source ICollection<T> is greater than the available space from arrayIndex to the end of the destination array");

            foreach (var i in this)
            {
                array[arrayIndex++] = i;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(list.GetEnumerator());
        }

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            list.Insert(index, item);
        }

        public bool Remove(T item)
        {
            return list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (list as IEnumerable).GetEnumerator();
        }
        public override string ToString()
        {
            return list.ToString();
        }

        public override int GetHashCode()
        {
            return list.GetHashCode();
        }
    }
}
