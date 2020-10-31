using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.Persistency
{
    public class StateMap 
    {
        private readonly Serializers _serializers;
        private readonly IDictionary<string, Entry> _entries;

        private Dictionary<string, SerializerOrValue> _changedEntries = new Dictionary<string, SerializerOrValue>();
        
        private readonly Dictionary<string, ISerializer> _serializables = new Dictionary<string, ISerializer>();
        private readonly HashSet<string> _removedKeys = new HashSet<string>();

        internal StateMap(Serializers serializers)
        {
            _serializers = serializers;
            _entries = new Dictionary<string, Entry>();
        } 

        internal StateMap(Serializers serializers, IReadOnlyDictionary<string, object> currentState)
        {
            _serializers = serializers;
            _entries = new Dictionary<string, Entry>();

            foreach (var (key, value) in currentState)
                    _entries[key] = new Entry(value);

            _serializables =  currentState 
                .Where(kv => kv.Value != null && serializers.IsSerializable(kv.Value))
                .ToDictionary(kv => kv.Key, kv => serializers.AddAndWrapUp(kv.Value));
        }

        internal IEnumerable<SerializerOrValue> PullChangedEntries()
        {
            var changedEntries = _changedEntries;
            _changedEntries = new Dictionary<string, SerializerOrValue>();
            return changedEntries.Values;
        }

        internal IEnumerable<string> PullRemovedKeys()
        {
            if (_removedKeys.Count == 0) return Enumerable.Empty<string>();

            var toReturn = _removedKeys.ToList();
            _removedKeys.Clear();
            
            return toReturn;
        }

        internal IEnumerable<ISerializer> GetReferencedSerializers() => _serializables.Values;

        internal object Get(string key) => _entries[key].Value;

        public bool ContainsKey(string key) => _entries.ContainsKey(key);

        public void Set(string key, object value)
        {
            if (!IsPrimitiveOrIPersistable(value))
                throw new SerializationException($"Unable to serialize value of type {value.GetType().Name}");

            _removedKeys.Remove(key);

            if (!_entries.ContainsKey(key))
                _entries[key] = new Entry(value);
            else
            {
                var changed = _entries[key].Set(value);
                if (!changed) return;
            }

            if (value != null && _serializers.IsSerializable(value))
            {
                var serializer = _serializers.AddAndWrapUp(value);
                _serializables[key] = serializer;
                _changedEntries[key] = SerializerOrValue.CreateSerializer(key, serializer);
            }
            else
            {
                _changedEntries[key] = SerializerOrValue.CreateValue(key, value);
                _serializables.Remove(key);
            }
        }

        public void Remove(string key)
        {
            if (!_entries.ContainsKey(key)) return;
            
            _serializables.Remove(key);
            _changedEntries.Remove(key);
            _removedKeys.Add(key);
        }

        public void Set<T>(string key, Action<T> action)
        {
            if (action == null) { SetNull(key); return; }

            if (_entries.ContainsKey(key) && !_entries[key].Set(action)) return; //no change detected

            var (serializer, objectId) = _serializers.GetSerializerOrNextObjectIndex(action);
            if (serializer == null)
            {
                serializer = new ActionSerializer<T>(objectId, action);
                _serializers.Add(serializer);
            }

            if (!_entries.ContainsKey(key))
                _entries[key] = new Entry(action);

            _serializables[key] = serializer;
            _changedEntries[key] = SerializerOrValue.CreateSerializer(key, serializer);
        }

        private void SetNull(string key)
        {
            if (_entries.ContainsKey(key) && !_entries[key].Set(null)) return;

            _changedEntries[key] = SerializerOrValue.CreateValue(key, null);
            _serializables.Remove(key);
        }

        private bool IsPrimitiveOrIPersistable(object o) 
            => o == null || o.GetType().IsPrimitive || o is DateTime || o is string || o is Guid  || _serializers.IsSerializable(o);

        internal class Entry
        {
            public object Value { get; private set; }

            public Entry() { }
            
            public Entry(object value) => Value = value;

            /// <summary>
            /// Sets the entry's
            /// </summary>
            /// <param name="newValue">The entry's new value</param>
            /// <returns>Returns true if the entry's value changed otherwise false</returns>
            public bool Set(object newValue)
            {
                if (Value == null && newValue == null)
                    return false;

                if (Value != null && newValue != null && Value.Equals(newValue))
                    return false;

                Value = newValue;
                return true;
            }
        }

        public class SerializerOrValue
        {
            public string Key { get; }

            public bool HoldsSerializer => Serializer != null;
            public ISerializer Serializer { get; }
            public object Value { get; }

            private SerializerOrValue(string key, ISerializer serializer)
            {
                Key = key;
                Serializer = serializer;
                Value = null;
            }

            private SerializerOrValue(string key, object value)
            {
                Key = key;
                Serializer = null;
                Value = value;
            }

            public static SerializerOrValue CreateValue(string key, object value) => new SerializerOrValue(key, value);
            public static SerializerOrValue CreateSerializer(string key, ISerializer serializer) => new SerializerOrValue(key, serializer);
        }
    }
}