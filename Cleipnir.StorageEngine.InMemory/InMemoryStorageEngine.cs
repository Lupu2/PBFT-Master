using System;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.StorageEngine.InMemory
{
    public class InMemoryStorageEngine : IStorageEngine
    {
        private Dictionary<ObjectIdAndKey, StorageEntry> _entries = new();
        private readonly Dictionary<long, Type> _serializerTypes = new();
        private readonly List<long> _garbageCollectableIds = new();
        private readonly bool _performGarbageCollection;

        public InMemoryStorageEngine(bool performGarbageCollection = true) 
            => _performGarbageCollection = performGarbageCollection;
        
        private readonly object _sync = new();

        public IEnumerable<Type> SerializerTypes
        {
            get
            {
                lock (_sync)
                    return _serializerTypes.Values.ToList();
            }
        }

        public IEnumerable<StorageEntry> Entries
        {
            get
            {
                lock (_sync)
                    return _entries.Values.ToList();
            }
        }

        public IEnumerable<long> GarbageCollectableIds
        {
            get
            {
                lock (_sync)
                    return _garbageCollectableIds.ToList();
            }
        }

        public void Persist(DetectedChanges detectedChanges)
        {
            lock (_sync)
            {
                foreach (var newEntry in detectedChanges.NewEntries)
                    _entries[new ObjectIdAndKey(newEntry.ObjectId, newEntry.Key)] = newEntry;
             
                foreach (var objectIdAndKey in detectedChanges.RemovedEntries)
                    _entries.Remove(objectIdAndKey);

                foreach (var (objectId, serializerType) in detectedChanges.NewSerializerTypes)
                    _serializerTypes[objectId] = serializerType;

                _garbageCollectableIds.AddRange(detectedChanges.GarbageCollectableIds);

                if (!_performGarbageCollection) return;
                
                foreach (var garbageCollectableId in _garbageCollectableIds)
                    _serializerTypes.Remove(garbageCollectableId);

                _entries = _entries
                    .Where(kv => !detectedChanges.GarbageCollectableIds.Contains(kv.Key.ObjectId))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }
        }

        public StoredState Load()
        {
            lock (_sync)
            {
                var entries = _entries
                    .Values
                    .GroupBy(e => e.ObjectId)
                    .ToDictionary(g => g.Key, g => g.AsEnumerable()
                );
            
                return new StoredState(_serializerTypes, entries);    
            }
        }

        public void Dispose() { }
    }
}
