using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.StorageEngine;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.GarbageCollectionTests
{
    [TestClass]
    public class PrimitiveGcTests
    {
        [TestMethod]
        public void CorrectEntriesAreCollected()
        {
            var gc = new GarbageCollector();

            var rootEntry = new StorageEntry(RootsInstance.PersistableId, "Next", 1);
            var node_1 = new StorageEntry(1, "Next", 2);
            var node_2 = new StorageEntry(2, "Next", 3);
            var node_3 = new StorageEntry(3, "Next", 4);

            var gcs = new StorageEntry(10, "SomeKey", "SomeValue");

            var collectables = gc.Collect(
                new List<StorageEntry> {rootEntry, node_1, node_2, node_3, gcs},
                Enumerable.Empty<ObjectIdAndKey>()
            ).ToArray();

            collectables.Length.ShouldBe(1);
            collectables[0].ShouldBe(10);

            collectables = gc.Collect(
                new List<StorageEntry>() {new StorageEntry(2, "Next", null)},
                Enumerable.Empty<ObjectIdAndKey>()
            ).OrderBy(_ => _).ToArray();

            collectables.Length.ShouldBe(2);
            collectables[0].ShouldBe(3);
            collectables[1].ShouldBe(4);

            collectables = gc.Collect(
                    new List<StorageEntry>() { new StorageEntry(RootsInstance.PersistableId, "Next", null) },
                    Enumerable.Empty<ObjectIdAndKey>()
                ).OrderBy(_ => _).ToArray();

            collectables.Length.ShouldBe(2);
            collectables[0].ShouldBe(1);
            collectables[1].ShouldBe(2);
        }

        [TestMethod]
        public void DoubleReferenceCreateOneRemoveAndObjectStillNotGarbageCollected()
        {
            var gc = new GarbageCollector();

            var rootEntry = new StorageEntry(RootsInstance.PersistableId, "Next", 1);
            var node_1a = new StorageEntry(1, "Next_A", 2);
            var node_1b = new StorageEntry(1, "Next_B", 3);
            var node_2 = new StorageEntry(2, "Next", 3);

            var collectables = gc.Collect(
                new List<StorageEntry> {rootEntry, node_1a, node_1b, node_2},
                Enumerable.Empty<ObjectIdAndKey>()
            ).ToArray();

            collectables.Length.ShouldBe(0);

            collectables = gc.Collect(
                new List<StorageEntry>() {new StorageEntry(2, "Next", null)},
                Enumerable.Empty<ObjectIdAndKey>()
            ).ToArray();
           
            collectables.Length.ShouldBe(0);
        }

        private class StorageEngine : IStorageEngine
        {
            public Synced<IReadOnlyList<long>> Garbage { get; private set; } = new Synced<IReadOnlyList<long>>();
            public void Persist(DetectedChanges detectedChanges) { }

            public IEnumerable<StorageEntry> Load() => Enumerable.Empty<StorageEntry>();

            public bool Exist { get; } = false;

            public void Dispose() { }
        }
    }
}
