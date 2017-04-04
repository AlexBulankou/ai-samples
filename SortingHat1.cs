namespace SortingHat1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface IItemWithKey<TKey>
    {
        TKey Key { get; set; }
    }

    public interface IEntryLookup<TKey>
    {
        IItemWithKey<TKey> GetEntryByKey(TKey key);

    }

    public class EntryLookupList<TKey> : IEntryLookup<TKey>
           where TKey : IEquatable<TKey>
    {
        private List<IItemWithKey<TKey>> entries;
        public EntryLookupList(IEnumerable<IItemWithKey<TKey>> entries)
        {
            this.entries = new List<IItemWithKey<TKey>>(entries);
        }

        public IItemWithKey<TKey> GetEntryByKey(TKey key)
        {
            return entries.First(item => item.Key.Equals(key));
        }
    }

    public class EntryLookupBinarySearch<TKey, TItem> : IEntryLookup<TKey>, IComparer<TItem>
           where TKey : IEquatable<TKey>, IComparable<TKey>
           where TItem : IItemWithKey<TKey>, new()
    {
        private List<TItem> entries;
        public EntryLookupBinarySearch(IEnumerable<TItem> entries)
        {
            this.entries = new List<TItem>(entries);
        }

        public int Compare(TItem x, TItem y)
        {
            return x.Key.CompareTo(y.Key);
        }

        public IItemWithKey<TKey> GetEntryByKey(TKey key)
        {
            var entryToSearch = new TItem();
            entryToSearch.Key = key;
            var index = entries.BinarySearch(entryToSearch, this);
            return this.entries[index];

        }
    }

    public class EntryLookupDictionary<TKey> : IEntryLookup<TKey>, IEqualityComparer<TKey>
           where TKey : IEquatable<TKey>
    {

        private List<IItemWithKey<TKey>> entries;

        public EntryLookupDictionary(IEnumerable<IItemWithKey<TKey>> entries)
        {
            this.entries = new List<IItemWithKey<TKey>>(entries);
        }

        public IItemWithKey<TKey> GetEntryByKey(TKey key)
        {
            return this.entries.ToDictionary(entry => entry.Key, this)[key];
        }

        public bool Equals(TKey x, TKey y)
        {
            return x.Equals(y);
        }

        public int GetHashCode(TKey obj)
        {
            return obj.GetHashCode();
        }
    }
}
