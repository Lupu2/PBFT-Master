using System.Collections.Generic;

namespace Cleipnir.StorageEngine
{
    public class DetectedChanges
    {
        public IReadOnlyList<StorageEntry> StorageEntries { get; }
        public IReadOnlyList<ObjectIdAndKey> RemovedEntries { get; }
        public IEnumerable<long> GarbageCollectables { get; } //todo make IReadOnlySet

        public DetectedChanges(IReadOnlyList<StorageEntry> storageEntries, IReadOnlyList<ObjectIdAndKey> removedEntries = null, IEnumerable<long> garbageCollectables = null)
        {
            StorageEntries = storageEntries;
            GarbageCollectables = garbageCollectables;
            RemovedEntries = removedEntries ?? new ObjectIdAndKey[0];
        }
    }
}
