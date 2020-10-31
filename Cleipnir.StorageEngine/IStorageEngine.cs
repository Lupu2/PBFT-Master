using System;
using System.Collections.Generic;

namespace Cleipnir.StorageEngine
{
    public interface IStorageEngine : IDisposable
    {
        void Persist(DetectedChanges detectedChanges);
        IEnumerable<StorageEntry> Load();
        bool Exist { get; }
    }
}
