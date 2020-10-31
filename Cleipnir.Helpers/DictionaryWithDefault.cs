using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ObjectDB.Helpers.DataStructures
{
    public class DictionaryWithDefault<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly IDictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        private readonly Func<TKey, TValue> _defaultFactory;

        public DictionaryWithDefault(Func<TKey, TValue> defaultFactory) => _defaultFactory = defaultFactory;

        public DictionaryWithDefault(Func<TKey, TValue> defaultFactory, IDictionary<TKey, TValue> innerDictionary)
        {
            _defaultFactory = defaultFactory;
            _dictionary = innerDictionary;
        } 

        public TValue this[TKey key]
        {
            get
            {
                if (_dictionary.ContainsKey(key))
                    return _dictionary[key];

                _dictionary[key] = _defaultFactory(key);
                return _dictionary[key];
             }
            set => _dictionary[key] = value;
        }

        public IEnumerable<TValue> Values => _dictionary.Values;
        public IEnumerable<TKey> Keys => _dictionary.Keys;

        public IDictionary<TKey, TValue> ToDictionary() => _dictionary.ToDictionary(p => p.Key, p => p.Value);

        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

        public void AddIfNotExists(TKey key)
        {
            if (_dictionary.ContainsKey(key))
                return;

            _dictionary[key] = _defaultFactory(key);
        }

        public bool Remove(TKey key) => _dictionary.Remove(key);

        public void Clear() => _dictionary.Clear();

        public DictionaryWithDefault<TKey, TValue> Clone() =>
            new DictionaryWithDefault<TKey, TValue>(
                _defaultFactory,
                _dictionary.ToDictionary(kv => kv.Key, kv => kv.Value)
            );

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary
            .Select(kv => new KeyValuePair<TKey,TValue>(kv.Key, kv.Value))
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
