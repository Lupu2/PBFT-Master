using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Serialization.Serializers
{
    internal class SerializersTypeEntries
    {
        private readonly IDictionary<long, Type> _idToType;
        private List<StorageEntry> _added = new List<StorageEntry>();
        private List<ObjectIdAndKey> _removed = new List<ObjectIdAndKey>();

        public static long PersistableId { get; } = -1;
        
        public SerializersTypeEntries() => _idToType = new Dictionary<long, Type>();

        internal SerializersTypeEntries(IEnumerable<StorageEntry> logEntries)
        {
            _idToType = logEntries
                .Where(e => e.ObjectId == PersistableId)
                .ToDictionary(e => long.Parse(e.Key), e =>
                {
                    var value = e.Value.ToString();
                    var type = Type.GetType(value);
                    return type; 
                });
        }

        public Type this[long id]
        {
            get => _idToType[id];
            set => Set(id, value);
        }

        public void Set(long id, Type type)
        {
            if (_idToType.ContainsKey(id))
                return;
            
            _idToType[id] = type;
            _added.Add(new StorageEntry(PersistableId, id.ToString(), type.SimpleQualifiedName()));
        }

        public IEnumerable<StorageEntry> PullChanges()
        {
            var toReturn = _added;
            _added = new List<StorageEntry>();
            return toReturn;
        }

        public IEnumerable<ObjectIdAndKey> PullRemoved()
        {
            if (_removed.Count == 0)
                return Enumerable.Empty<ObjectIdAndKey>();

            var toReturn = _removed;
            _removed = new List<ObjectIdAndKey>();
            return toReturn;
        }

        public void Remove(long id)
        {
            _idToType.Remove(id);
            _removed.Add(new ObjectIdAndKey(PersistableId, id.ToString()));
        } 

        public bool ContainsObjectId(long id) => _idToType.ContainsKey(id);

        public IEnumerable<Tuple<long, Type>> GetAll() => _idToType.Select(kv => Tuple.Create(kv.Key, kv.Value));
    }
}
