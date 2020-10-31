using System;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.StorageEngine.InMemory
{
    public class InMemoryStorageEngine : IStorageEngine
    {
        private readonly Dictionary<ObjectIdAndKey, StorageEntry> _entries = new Dictionary<ObjectIdAndKey, StorageEntry>();

        private readonly object _sync = new object();

        public void Persist(DetectedChanges detectedChanges)
        {
            lock (_sync)
            {
                foreach (var entry in detectedChanges.StorageEntries)
                    _entries[new ObjectIdAndKey(entry.ObjectId, entry.Key)] = entry;
                foreach (var objectIdAndKey in detectedChanges.RemovedEntries)
                    _entries.Remove(objectIdAndKey);
                var garbageCollectables = detectedChanges.GarbageCollectables.ToHashSet();
               
                foreach (var key in _entries.Keys.ToList())
                {
                    if (garbageCollectables.Contains(key.ObjectId))
                        _entries.Remove(key);
                }
            }
        }

        public IEnumerable<StorageEntry> Load()
        {
            lock (_sync)
                return _entries.Values.ToList();
        } 

        public bool Exist
        {
            get
            {
                lock (_sync)
                    return _entries.Count > 0;
            }
        }

        public void Dispose() { }
    }
}
