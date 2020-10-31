using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ObjectDB.PersistentDataStructures
{
    public class CStateMap : IPersistable
    {
        private readonly Dictionary<string, Action<StateMap>> _changes = new Dictionary<string, Action<StateMap>>();

        internal IReadOnlyDictionary<string, object> DeserializedValues { get; set; }

        public void Set(string key, object value) => _changes[key] = sd => sd.Set(key, value);

        public void Set<T>(string key, Action<T> action) => _changes[key] = sd => sd.Set(key, action);

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_changes.Count == 0) return;

            foreach (var action in _changes.Values)
                action(sd);

            _changes.Clear();
        }

        private static CStateMap Deserialize(IReadOnlyDictionary<string, object> sd) 
            => new CStateMap { DeserializedValues = sd };
    }
}
