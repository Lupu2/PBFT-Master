using System.Collections.Generic;

namespace Cleipnir.StorageEngine
{
    public record DetectedChanges(
        IReadOnlyList<StorageEntry> NewEntries,
        IReadOnlyList<ObjectIdAndKey> RemovedEntries,
        IEnumerable<ObjectIdAndType> NewSerializerTypes,
        IEnumerable<long> GarbageCollectableIds) {}
}
