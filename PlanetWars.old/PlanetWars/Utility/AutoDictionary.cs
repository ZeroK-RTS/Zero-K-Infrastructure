using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PlanetWarsShared;

namespace PlanetWars.Utility
{
    public class AutoDictionary<TKey, TValue> : KeyedCollection<TKey, TValue>
    {
        readonly Func<TValue, TKey> GetKey;

        public AutoDictionary(Func<TValue, TKey> getKey)
        {
            GetKey = getKey;
        }

        public AutoDictionary(IEnumerable<TValue> collection, Func<TValue, TKey> getKey) : this(getKey)
        {
            collection.ForEach(Add);
        }

        public new IDictionary<TKey, TValue> Dictionary
        {
            get { return base.Dictionary.AsReadOnly(); }
        }

        protected override TKey GetKeyForItem(TValue item)
        {
            return GetKey(item);
        }
    }
}